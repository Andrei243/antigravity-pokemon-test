using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Contest;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 21 · H12, part o (rulings, "Mauville's roulette wheel"): the roulette table itself (<see cref="RouletteWheel"/>:
/// the slots' order round the wheel, the cursor over the board, the ball rolled, dropped, thrown aside and stuck, Shroomish
/// and Taillow clearing it, a round of six and the board cleared, a seeded sitting played twice the same) and the scripts
/// that sit the player at it.
/// </summary>
[Collection("Shared clocks and dice")]
public class HoennPartOTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;

    // Plays one ball: the wager on whatever the cursor is on, then the ball to its rest, its line closed and the coins
    // counted; returns the square it landed on and whether a helper was sent for
    private static (int Square, RouletteHelper Helper) PlayBall(RouletteWheel w)
    {
        Assert.Equal(RoulettePhase.Betting, w.Phase);
        w.Update(RouletteKeys.Confirm);
        Assert.Equal(RoulettePhase.SlideOut, w.Phase);
        var helper = RouletteHelper.None;
        for (int frame = 0; frame < 20000 && w.Phase != RoulettePhase.Result; frame++)
        {
            w.Update(RouletteKeys.None);
            if (w.Helper != RouletteHelper.None) helper = w.Helper;
        }
        Assert.Equal(RoulettePhase.Result, w.Phase);
        int square = w.WinningSquare;
        w.Update(RouletteKeys.Confirm);
        for (int frame = 0; frame < 2000 && w.Phase == RoulettePhase.Payout && w.PayoutLeft > 0; frame++) w.Update(RouletteKeys.None);
        if (w.Phase == RoulettePhase.Payout) w.Update(RouletteKeys.Confirm);
        if (w.Phase is RoulettePhase.Cleared or RoulettePhase.MostCoins) w.Update(RouletteKeys.Confirm);
        if (w.Phase == RoulettePhase.Ask) w.Update(RouletteKeys.Confirm);
        return (square, helper);
    }

    [Fact]
    public void TheWheelsSlotsGoRoundInEmeraldsOrder()
    {
        // sRouletteSlots: orange Wynaut, green Azurill, purple Skitty, orange Makuhita, green Wynaut, purple Azurill,
        // orange Skitty, green Makuhita, purple Wynaut, orange Azurill, green Skitty, purple Makuhita
        int[] squares = { 0, 5, 10, 3, 4, 9, 2, 7, 8, 1, 6, 11 };
        Assert.Equal(squares, Enumerable.Range(0, RouletteWheel.Slots).Select(RouletteWheel.SquareOfSlot));
        Assert.Equal(30, RouletteWheel.DegreesPerSlot);
    }

    [Fact]
    public void TheCursorWrapsWithinItsColumnAndRow()
    {
        var w = new RouletteWheel(1, 100, 12, false, false, new Random(1));
        // Task_SelectFirstEmptySquare: the orange Wynaut on a fresh board
        Assert.Equal((0, 0), (w.SelectedRow, w.SelectedColumn));
        Assert.Equal(RouletteRound.Kind.Square, w.SelectedKind);
        Assert.Equal(12, w.SelectedOdds);
        // Up to Wynaut's head (a column), up again round to the purple Wynaut
        w.Update(RouletteKeys.Up);
        Assert.Equal((-1, 0), (w.SelectedRow, w.SelectedColumn));
        Assert.Equal(RouletteRound.Kind.Pokemon, w.SelectedKind);
        Assert.Equal(4, w.SelectedOdds);
        w.Update(RouletteKeys.Up);
        Assert.Equal((2, 0), (w.SelectedRow, w.SelectedColumn));
        // Left to purple's head (a row), left again round to the purple Makuhita
        w.Update(RouletteKeys.Left);
        Assert.Equal((2, -1), (w.SelectedRow, w.SelectedColumn));
        Assert.Equal(RouletteRound.Kind.Colour, w.SelectedKind);
        Assert.Equal(3, w.SelectedOdds);
        w.Update(RouletteKeys.Left);
        Assert.Equal((2, 3), (w.SelectedRow, w.SelectedColumn));
        // Along the colours' heads up and down never reaches the Pokémon's row
        w.Update(RouletteKeys.Right);
        Assert.Equal((2, -1), (w.SelectedRow, w.SelectedColumn));
        w.Update(RouletteKeys.Down);
        Assert.Equal((0, -1), (w.SelectedRow, w.SelectedColumn));
        // Along the Pokémon's heads left and right never reaches the colours' column
        w.Update(RouletteKeys.Right);
        w.Update(RouletteKeys.Up);
        Assert.Equal((-1, 0), (w.SelectedRow, w.SelectedColumn));
        w.Update(RouletteKeys.Left);
        Assert.Equal((-1, 3), (w.SelectedRow, w.SelectedColumn));
        Assert.Contains(RouletteCue.Move, w.Cues);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void EveryBallComesToRestInASlotNotYetHit(int minBet)
    {
        int stuckShroomish = 0, stuckTaillow = 0;
        for (int seed = 1; seed <= 40; seed++)
        {
            var w = new RouletteWheel(minBet, 1000, seed % 24, false, false, new Random(seed));
            for (int round = 0; round < 2; round++)
            {
                var seen = new HashSet<int>();
                for (int ball = 0; ball < RouletteRound.BallsPerRound; ball++)
                {
                    var (square, helper) = PlayBall(w);
                    Assert.InRange(square, 0, RouletteRound.Squares - 1);
                    Assert.True(seen.Add(square), $"seed {seed}: the ball landed on square {square} twice in a round");
                    if (helper == RouletteHelper.Shroomish) stuckShroomish++;
                    if (helper == RouletteHelper.Taillow) stuckTaillow++;
                }
                // After the sixth ball the board is cleared
                Assert.Equal(0, w.Board.Balls);
                Assert.Empty(w.RestingBalls);
            }
            Assert.Equal(0, w.Rescued);
            Assert.Equal(RouletteRound.BallsPerRound * 2, w.BallsThrown);
        }
        // Balls do stick, and both helpers come for them
        Assert.True(stuckShroomish > 0, "Shroomish never came");
        Assert.True(stuckTaillow > 0, "Taillow never came");
    }

    [Fact]
    public void AWinPaysTheWagerTimesItsOddsAndALossNothing()
    {
        for (int seed = 1; seed <= 20; seed++)
        {
            var w = new RouletteWheel(3, 100, 15, false, false, new Random(seed));
            // A wager on orange: three Coins at three to one
            w.Update(RouletteKeys.Left);
            Assert.Equal(RouletteRound.Kind.Colour, w.SelectedKind);
            int coins = w.Coins;
            var (square, _) = PlayBall(w);
            bool hit = square / RouletteRound.Pokemon == 0;
            Assert.Equal(hit, w.Won);
            Assert.Equal(coins - 3 + (hit ? 9 : 0), w.Coins);
        }
    }

    [Fact]
    public void AWagerAllHitIsRefused()
    {
        var w = new RouletteWheel(1, 100, 12, false, false, new Random(7));
        var (square, _) = PlayBall(w);
        // Walk the cursor onto the square just hit: it buzzes and the table waits for another wager
        for (int i = 0; i < 20 && w.SelectedRow != square / RouletteRound.Pokemon; i++) w.Update(RouletteKeys.Down);
        for (int i = 0; i < 20 && w.SelectedColumn != square % RouletteRound.Pokemon; i++) w.Update(RouletteKeys.Right);
        Assert.Equal(0, w.SelectedOdds);
        int coins = w.Coins;
        w.Update(RouletteKeys.Confirm);
        Assert.Contains(RouletteCue.Refused, w.Cues);
        Assert.Equal(RoulettePhase.Betting, w.Phase);
        Assert.Equal(coins, w.Coins);
    }

    [Fact]
    public void ASeededSittingPlaysTheSameTwice()
    {
        static List<int> Sit(int seed)
        {
            var w = new RouletteWheel(1, 50, 20, true, true, new Random(seed));
            var squares = new List<int>();
            for (int ball = 0; ball < 12; ball++)
            {
                if (ball % 3 == 1) w.Update(RouletteKeys.Up);
                squares.Add(PlayBall(w).Square);
            }
            squares.Add(w.Coins);
            squares.Add(w.WheelAngle);
            return squares;
        }
        Assert.Equal(Sit(11), Sit(11));
        Assert.NotEqual(Sit(11), Sit(12));
    }

    [Fact]
    public void RunningOutOfCoinsEndsTheSitting()
    {
        for (int seed = 1; seed <= 30; seed++)
        {
            var w = new RouletteWheel(1, 1, 9, false, false, new Random(seed));
            w.Update(RouletteKeys.Confirm);
            for (int frame = 0; frame < 20000 && w.Phase != RoulettePhase.Result; frame++) w.Update(RouletteKeys.None);
            if (w.Won) continue;
            w.Update(RouletteKeys.Confirm);
            Assert.Equal(RoulettePhase.Broke, w.Phase);
            for (int frame = 0; frame < 61; frame++) w.Update(RouletteKeys.None);
            Assert.True(w.Finished);
            return;
        }
        Assert.Fail("every first ball won");
    }

    [Fact]
    public void SayingNoToPlayingOnLeavesTheTable()
    {
        var screen = new RouletteScreen();
        screen.Open(3, 30, 12, false, false, new Random(3));
        Assert.True(screen.IsActive);
        var w = screen.Wheel!;
        screen.Press(RouletteKeys.Confirm);
        for (int i = 0; i < 20000 && w.Phase != RoulettePhase.Result; i++) screen.Frames(1);
        screen.Press(RouletteKeys.Confirm);
        screen.Frames(1);
        for (int i = 0; i < 2000 && w.Phase == RoulettePhase.Payout && w.PayoutLeft > 0; i++) screen.Frames(1);
        if (w.Phase == RoulettePhase.Payout) { screen.Press(RouletteKeys.Confirm); screen.Frames(1); }
        Assert.Equal(RoulettePhase.Ask, w.Phase);
        screen.Press(RouletteKeys.Down);
        screen.Frames(1);
        Assert.Equal(1, w.AskCursor);
        screen.Press(RouletteKeys.Confirm);
        screen.Frames(1);
        Assert.False(screen.IsActive);
        Assert.Equal(27 + (w.Won ? 3 * w.BetOdds : 0), w.Coins);
    }

    [Fact]
    public void EachQualitysContestIsHeldInItsOwnHallOnOneStage()
    {
        Assert.Equal("ContestHallCool", ContestHallStage.MapOf(ContestStat.Cool));
        Assert.Equal("ContestHallTough", ContestHallStage.MapOf(ContestStat.Tough));
        Assert.Equal("ContestHall", ContestHallStage.MapOf(null));
        Assert.Equal(5, Enum.GetValues<ContestStat>().Select(q => ContestHallStage.MapOf(q)).Distinct().Count());
        // Every place is on the hall's floor; the four contestants stand in a row before the judge, who sits beside the host
        Assert.All(ContestHallStage.Spots, s => Assert.InRange(s.X, 0, ContestHallStage.Width - 1));
        Assert.All(ContestHallStage.Spots, s => Assert.InRange(s.Y, 0, ContestHallStage.Height - 1));
        var contestants = ContestHallStage.Spots.Where(s => s.Place == StagePlace.Contestant).ToList();
        Assert.Equal(new[] { 1, 2, 3, 4 }, contestants.Select(s => s.Number));
        Assert.Single(contestants.Select(s => s.Y).Distinct());
        var judge = ContestHallStage.Spots.Single(s => s.Place == StagePlace.Judge);
        var host = ContestHallStage.Spots.Single(s => s.Place == StagePlace.Host);
        Assert.Equal(judge.Y, host.Y);
        Assert.True(contestants[0].Y > judge.Y);
        Assert.Equal(7, ContestHallStage.Spots.Count(s => s.Place == StagePlace.Audience));
    }

    private static readonly Lazy<Dictionary<string, Map>> HoennMaps = new(() =>
        World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn).BuildMaps().ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase));

    [Fact]
    public void TheCuratorShowsTheExhibitHallOnTheFirstVisitOnly()
    {
        var host = new HeadlessScriptHost(new StoryState(), new Party(), new Inventory()) { Map = HoennMaps.Value["LilycoveCity_LilycoveMuseum_2F"] };
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find("OnEnter", "lilycove_city_lilycove_museum_2f")!);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.Equal(1, host.Story.Var("VAR_LILYCOVE_MUSEUM_2F_STATE"));
        Assert.Contains(host.Transcript, l => l.Text.Contains("exhibit hall"));
        int said = host.Transcript.Count;
        var again = new ScriptRunner(Scripts, host);
        again.Start(Scripts.Find("OnEnter", "lilycove_city_lilycove_museum_2f")!);
        again.RunToEnd();
        Assert.Equal(said, host.Transcript.Count);
    }

    [Fact]
    public void TheTablesSitThePlayerDownAtTheirWager()
    {
        static HeadlessScriptHost Host(int coins, bool caseToo = true)
        {
            var bag = new Inventory();
            if (caseToo) bag.AddItem(ItemDatabase.Get("Coin Case")!, 1);
            var host = new HeadlessScriptHost(new StoryState(), new Party(), bag);
            host.Pastimes.Coins.Add(coins);
            return host;
        }
        // Enough Coins, and yes to playing: the table itself is opened at its wager
        var host = Host(30);
        host.Answers.Enqueue(0);
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find("Roulette2", "mauville_city_game_corner")!);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.Contains("pastime wheel 3", host.Log);
        // Too few for the right table
        var poor = Host(2);
        var r2 = new ScriptRunner(Scripts, poor);
        r2.Start(Scripts.Find("Roulette2", "mauville_city_game_corner")!);
        r2.RunToEnd();
        Assert.DoesNotContain("pastime wheel 3", poor.Log);
        Assert.Contains(poor.Transcript, l => l.Text.Contains("3 Coins"));
        // The left table, refused
        var no = Host(5);
        no.Answers.Enqueue(1);
        var r3 = new ScriptRunner(Scripts, no);
        r3.Start(Scripts.Find("Roulette1", "mauville_city_game_corner")!);
        r3.RunToEnd();
        Assert.DoesNotContain("pastime wheel 1", no.Log);
        // No Coin Case
        var none = Host(0, caseToo: false);
        var r4 = new ScriptRunner(Scripts, none);
        r4.Start(Scripts.Find("Roulette1", "mauville_city_game_corner")!);
        r4.RunToEnd();
        Assert.Contains(none.Transcript, l => l.Text.Contains("Without a Coin Case"));
    }
}
