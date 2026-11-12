using System;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>The card-flip game (plan 42 · PS3): HeartGold's rules, held to voltorb_flip_game.c.</summary>
public class CardFlipTests
{
    // The Payout column the original writes beside each row of sBoardConfigs, copied to catch a mistyped row
    private static readonly int[] SourcePayouts =
    {
        24, 27, 32, 36, 48, 24, 27, 32, 36, 48, 54, 64, 72, 81, 96, 54, 64, 72, 81, 96,
        108, 128, 144, 162, 192, 108, 128, 144, 162, 192, 216, 243, 256, 288, 324, 216, 243, 256, 288, 324,
        384, 432, 486, 512, 576, 384, 432, 486, 512, 576, 648, 729, 768, 864, 972, 648, 729, 768, 864, 972,
        1152, 1296, 1458, 1536, 1728, 1152, 1296, 1458, 1536, 1728, 2187, 2304, 2592, 2916, 3456, 2187, 2304, 2592, 2916, 3456
    };

    [Fact]
    public void TheEightyBoardsPayWhatTheOriginalWritesBesideThem()
    {
        Assert.Equal(CardFlipLevels.Boards, CardFlipLevels.Configs.Length);
        for (int i = 0; i < CardFlipLevels.Boards; i++) Assert.Equal(SourcePayouts[i], CardFlipLevels.Configs[i].Payout);
        // The two odd rows the original has, copied as they are
        Assert.Equal(new CardFlipConfig(8, 6, 1, 4, 3), CardFlipLevels.Configs[24]);
        Assert.Equal(new CardFlipConfig(8, 3, 3, 4, 3), CardFlipLevels.Configs[30]);
    }

    [Fact]
    public void EachLevelDrawsAmongItsOwnTenBoardsTenInAHundredEach()
    {
        for (int kept = 0; kept < CardFlipLevels.Count; kept++)
        {
            int shown = CardFlipLevels.Shown(kept);
            for (int draw = 0; draw < 100; draw++)
            {
                int board = CardFlipLevels.BoardFor(kept, draw);
                Assert.Equal((shown - 1) * 10 + draw / 10, board);
                Assert.Equal(shown, CardFlipLevels.ShownOfBoard(board));
            }
        }
    }

    [Fact]
    public void EveryBoardHasItsCountsItsSumsAndKeepsItsLimitsWhenADrawHeldThem()
    {
        int kept = 0, broken = 0;
        for (int seed = 0; seed < 4000; seed++)
        {
            var rng = new Random(seed);
            var config = CardFlipLevels.Configs[seed % CardFlipLevels.Boards];
            var board = CardFlipBoard.Deal(config, rng.Next);
            var faces = Enumerable.Range(0, 25).Select(board.Face).ToArray();
            // PlaceCardsOnBoard may leave a kind short after a hundred misses: never by more than its count, and
            // so seldom that over these seeds it never does
            Assert.Equal(config.Busts, faces.Count(f => f == CardFace.Bust));
            Assert.Equal(config.Twos, faces.Count(f => f == CardFace.Two));
            Assert.Equal(config.Threes, faces.Count(f => f == CardFace.Three));
            Assert.DoesNotContain(CardFace.None, faces);
            Assert.Equal(config.Twos + config.Threes, board.MultiplierCards);
            Assert.Equal(Math.Min(config.Payout, CardFlipBoard.MostPayout), board.MaxPayout);
            for (int i = 0; i < 5; i++)
            {
                Assert.Equal(Enumerable.Range(0, 5).Sum(c => CardFlipBoard.ValueOf(faces[i * 5 + c])), board.PointsInRow(i));
                Assert.Equal(Enumerable.Range(0, 5).Sum(r => CardFlipBoard.ValueOf(faces[r * 5 + i])), board.PointsInColumn(i));
                Assert.Equal(Enumerable.Range(0, 5).Count(c => faces[i * 5 + c] == CardFace.Bust), board.BustsInRow(i));
                Assert.Equal(Enumerable.Range(0, 5).Count(r => faces[r * 5 + i] == CardFace.Bust), board.BustsInColumn(i));
            }
            // A board kept before its thousandth draw held the limits: fewer free multipliers than the most allowed
            var (total, rows, columns) = board.FreeMultipliers();
            if (board.Draws < CardFlipBoard.Tries)
            {
                Assert.True(board.HoldsItsLimits);
                Assert.True(total < config.MostFree);
                Assert.All(rows, n => Assert.True(n < config.MostFreeInALine));
                Assert.All(columns, n => Assert.True(n < config.MostFreeInALine));
                kept++;
            }
            else broken++;
        }
        Assert.True(kept > 0);
    }

    [Fact]
    public void AfterAThousandDrawsTheLastBoardIsKeptAsItFell()
    {
        // No board can hold a limit of nought free multipliers in all: every draw is drawn again, and the last stays
        var config = new CardFlipConfig(6, 3, 1, 3, 0);
        var board = CardFlipBoard.Deal(config, new Random(7).Next);
        Assert.Equal(CardFlipBoard.Tries, board.Draws);
        Assert.False(board.HoldsItsLimits);
        Assert.Equal(6, Enumerable.Range(0, 25).Count(i => board.Face(i) == CardFace.Bust));
    }

    private static CardFlipRound[] Ring(params CardFlipRound[] rounds)
    {
        var ring = new CardFlipRound[5];
        for (int i = 0; i < rounds.Length; i++) ring[i] = rounds[i];
        return ring;
    }

    private static CardFlipRound R(CardFlipOutcome outcome, int flipped, int shownLevel, int boardInLevel = 0) =>
        new(outcome, flipped, (shownLevel - 1) * 10 + boardInLevel, 8 - shownLevel);

    [Fact]
    public void NextLevelFollowsCalcNextLevelsBranches()
    {
        // Nothing played: level 1
        Assert.Equal(7, CardFlipLevels.NextLevel(new CardFlipRound[5], 0));
        // A win at level 8 stays at 8
        Assert.Equal(0, CardFlipLevels.NextLevel(Ring(R(CardFlipOutcome.Won, 9, 8)), 1));
        // From level 5 up, five rounds none lost with eight turned each: 8
        var five = Ring(R(CardFlipOutcome.Quit, 8, 5), R(CardFlipOutcome.Won, 9, 5), R(CardFlipOutcome.Quit, 8, 5),
            R(CardFlipOutcome.Quit, 10, 5), R(CardFlipOutcome.Quit, 8, 5));
        Assert.Equal(0, CardFlipLevels.NextLevel(five, 0));
        // ...but not from level 4, nor with one of them lost or short
        var fromFour = Ring(R(CardFlipOutcome.Quit, 8, 4), R(CardFlipOutcome.Quit, 8, 4), R(CardFlipOutcome.Quit, 8, 4),
            R(CardFlipOutcome.Quit, 8, 4), R(CardFlipOutcome.Quit, 8, 4));
        Assert.Equal(4, CardFlipLevels.NextLevel(fromFour, 0)); // 8 turned at level 4: level 4, no higher (needs a win for 5)
        var oneLost = (CardFlipRound[])five.Clone();
        oneLost[2] = R(CardFlipOutcome.Lost, 8, 5);
        Assert.NotEqual(0, CardFlipLevels.NextLevel(oneLost, 0));
        // Level 7: at 7 with 7 turned, or a win at 6
        Assert.Equal(1, CardFlipLevels.NextLevel(Ring(R(CardFlipOutcome.Lost, 7, 7)), 1));
        Assert.Equal(1, CardFlipLevels.NextLevel(Ring(R(CardFlipOutcome.Won, 3, 6)), 1));
        // Level 6: at 6 with 6 turned, or a win at 5
        Assert.Equal(2, CardFlipLevels.NextLevel(Ring(R(CardFlipOutcome.Lost, 6, 7)), 1));
        Assert.Equal(2, CardFlipLevels.NextLevel(Ring(R(CardFlipOutcome.Won, 2, 5)), 1));
        // Level 5, 4, 3 the same way
        Assert.Equal(3, CardFlipLevels.NextLevel(Ring(R(CardFlipOutcome.Lost, 5, 6)), 1));
        Assert.Equal(3, CardFlipLevels.NextLevel(Ring(R(CardFlipOutcome.Won, 1, 4)), 1));
        Assert.Equal(4, CardFlipLevels.NextLevel(Ring(R(CardFlipOutcome.Quit, 4, 4)), 1));
        Assert.Equal(4, CardFlipLevels.NextLevel(Ring(R(CardFlipOutcome.Won, 1, 3)), 1));
        Assert.Equal(5, CardFlipLevels.NextLevel(Ring(R(CardFlipOutcome.Lost, 3, 8)), 1));
        Assert.Equal(5, CardFlipLevels.NextLevel(Ring(R(CardFlipOutcome.Won, 1, 2)), 1));
        // Level 2: at 2 with 2 turned, or any win
        Assert.Equal(6, CardFlipLevels.NextLevel(Ring(R(CardFlipOutcome.Lost, 2, 2)), 1));
        Assert.Equal(6, CardFlipLevels.NextLevel(Ring(R(CardFlipOutcome.Won, 1, 1)), 1));
        // A loss with fewer turned than the level drops to that many, and one turned or none is level 1
        Assert.Equal(7, CardFlipLevels.NextLevel(Ring(R(CardFlipOutcome.Lost, 1, 8)), 1));
        Assert.Equal(7, CardFlipLevels.NextLevel(Ring(R(CardFlipOutcome.Quit, 0, 5)), 1));
        // The ring's last written is the one before the head, round the end too
        var wrapped = Ring(R(CardFlipOutcome.Lost, 0, 1), R(CardFlipOutcome.Lost, 0, 1), R(CardFlipOutcome.Lost, 0, 1),
            R(CardFlipOutcome.Lost, 0, 1), R(CardFlipOutcome.Won, 1, 1));
        Assert.Equal(6, CardFlipLevels.NextLevel(wrapped, 0));
    }

    [Fact]
    public void ARoundsCoinsAreTheProductOfItsCardsAndABustLosesThem()
    {
        for (int seed = 0; seed < 300; seed++)
        {
            var rng = new Random(seed);
            var game = new CardFlipGame(rng.Next, 1000);
            Assert.True(game.Deal());
            var board = game.Board!;
            var order = Enumerable.Range(0, 25).OrderBy(_ => rng.Next()).ToArray();
            int product = 0;
            foreach (int card in order)
            {
                var face = game.Flip(card);
                if (face == CardFace.Bust)
                {
                    Assert.Equal(CardFlipOutcome.Lost, game.Outcome);
                    Assert.Equal(1000, game.Coins);
                    break;
                }
                int v = CardFlipBoard.ValueOf(face);
                product = product == 0 ? v : product * v;
                Assert.Equal(product, game.Payout);
                if (game.Outcome == CardFlipOutcome.Won)
                {
                    Assert.Equal(board.MaxPayout, game.Payout);
                    Assert.Equal(board.MultiplierCards, game.MultipliersFlipped);
                    Assert.Equal(1000 + product, game.Coins);
                    break;
                }
            }
            Assert.NotEqual(CardFlipOutcome.None, game.Outcome);
            Assert.Equal(CardFace.None, game.Flip(order[0])); // nothing more once the round has ended
        }
    }

    [Fact]
    public void QuittingKeepsTheRoundsCoinsAndTheCaseNeverPassesItsMost()
    {
        var rng = new Random(3);
        var game = new CardFlipGame(rng.Next, CoinCase.Most - 2);
        game.Deal();
        var board = game.Board!;
        int card = Enumerable.Range(0, 25).First(i => CardFlipBoard.IsMultiplier(board.Face(i)));
        game.Flip(card);
        int payout = game.Payout;
        Assert.True(payout >= 2);
        Assert.True(game.Quit());
        Assert.Equal(CardFlipOutcome.Quit, game.Outcome);
        Assert.Equal(CoinCase.Most, game.Coins);
        // A full case deals no more rounds
        Assert.True(game.CaseFull);
        Assert.False(game.Deal());
    }

    [Fact]
    public void TheMemoHoldsFourMarksAndTurningACardWipesThem()
    {
        var game = new CardFlipGame(new Random(11).Next, 0);
        game.Deal();
        for (int m = 0; m < CardFlipGame.Marks; m++) Assert.True(game.ToggleMemo(5, m));
        Assert.Equal(0b1111, game.Board!.Memo(5));
        Assert.True(game.ToggleMemo(5, 2));
        Assert.False(game.Board.HasMark(5, 2));
        Assert.False(game.ToggleMemo(5, 4));
        game.Flip(5);
        Assert.Equal(0, game.Board.Memo(5));
    }

    [Fact]
    public void ASittingsLevelsComeFromItsRounds()
    {
        var rng = new Random(21);
        var game = new CardFlipGame(rng.Next, 0);
        game.Deal();
        Assert.Equal(1, game.ShownLevel);
        Assert.Equal(0, game.LevelsGained);
        // Win the first board by turning every card that is no bust
        var board = game.Board!;
        foreach (int i in Enumerable.Range(0, 25).Where(i => board.Face(i) != CardFace.Bust))
            if (game.InPlay) game.Flip(i);
        Assert.Equal(CardFlipOutcome.Won, game.Outcome);
        game.EndRound();
        game.Deal();
        Assert.Equal(2, game.ShownLevel);
        Assert.Equal(1, game.LevelsGained);
        // Bust on the first card turned: back to level 1
        int bust = Enumerable.Range(0, 25).First(i => game.Board!.Face(i) == CardFace.Bust);
        game.Flip(bust);
        game.EndRound();
        game.Deal();
        Assert.Equal(1, game.ShownLevel);
        Assert.Equal(-1, game.LevelsGained);
    }

    [Fact]
    public void TheSameSeedDealsTheSameSitting()
    {
        var a = new CardFlipGame(new Random(99).Next, 0);
        var b = new CardFlipGame(new Random(99).Next, 0);
        a.Deal();
        b.Deal();
        Assert.Equal(a.BoardId, b.BoardId);
        for (int i = 0; i < 25; i++) Assert.Equal(a.Board!.Face(i), b.Board!.Face(i));
    }

    [Fact]
    public void TheScreenPlaysASittingAndThePointerFindsWhatIsDrawn()
    {
        var screen = new PokemonPlatinumEngine.UI.CardFlipScreen();
        screen.Open(500, new Random(5).Next);
        Assert.Equal(PokemonPlatinumEngine.UI.CardFlipPhase.Menu, screen.Phase);
        Assert.NotNull(screen.Game!.Board);
        screen.Flip(); // PLAY
        Assert.Equal(PokemonPlatinumEngine.UI.CardFlipPhase.Board, screen.Phase);
        // The pointer finds each card, each mark and QUIT in the rectangles the drawing uses
        for (int i = 0; i < 25; i++)
        {
            var r = PokemonPlatinumEngine.UI.ModernUi.CardFlipCardRect(i);
            Assert.Equal(i, screen.HitTest(new System.Numerics.Vector2(r.X + r.Width / 2, r.Y + r.Height / 2)));
        }
        for (int m = 0; m < 4; m++)
        {
            var r = PokemonPlatinumEngine.UI.ModernUi.CardFlipMarkRect(m);
            Assert.Equal(PokemonPlatinumEngine.UI.CardFlipScreen.FirstMark + m, screen.HitTest(new System.Numerics.Vector2(r.X + 5, r.Y + 5)));
        }
        var q = PokemonPlatinumEngine.UI.ModernUi.CardFlipQuitRect();
        Assert.Equal(PokemonPlatinumEngine.UI.CardFlipScreen.QuitButton, screen.HitTest(new System.Numerics.Vector2(q.X + 5, q.Y + 5)));
        screen.PointAt(7);
        Assert.Equal(7, screen.Cursor);
        screen.Memo(0);
        Assert.True(screen.Game.Board!.HasMark(7, 0));
        // Turn cards along the rows until the round ends
        for (int i = 0; i < 25 && screen.Phase == PokemonPlatinumEngine.UI.CardFlipPhase.Board; i++)
        {
            screen.PointAt(i);
            screen.Flip();
        }
        Assert.Equal(PokemonPlatinumEngine.UI.CardFlipPhase.Reveal, screen.Phase);
        Assert.True(Enumerable.Range(0, 25).All(screen.Game.Board!.IsFlipped));
        screen.Flip();
        Assert.Equal(PokemonPlatinumEngine.UI.CardFlipPhase.Menu, screen.Phase);
        Assert.True(screen.Outcome >= 0);
        screen.Quit(); // LEAVE
        Assert.False(screen.IsActive);
    }

    [Fact]
    public void GoldenrodsPrizesAreThingsOfTheData()
    {
        Assert.Equal(10, GameCornerPrizes.Goldenrod.Length);
        foreach (var (item, coins) in GameCornerPrizes.Goldenrod)
        {
            Assert.NotNull(ItemDatabase.Get(item));
            Assert.True(coins > 0);
        }
        Assert.Equal(new[] { "Abra", "Ekans", "Dratini" }, GameCornerPrizes.GoldenrodPokemon.Select(p => p.Species));
        foreach (var (species, _) in GameCornerPrizes.GoldenrodPokemon) Assert.NotNull(PokemonDatabase.Get(species));
        Assert.Equal(15, GameCornerPrizes.GoldenrodPokemonLevel);
    }
}
