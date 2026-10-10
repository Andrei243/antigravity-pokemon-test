using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.PoketchApps;

namespace PokemonPlatinumTests;

/// <summary>
/// The Pokétch's toys (plan 06 · R14b): the calculator, the memo pad, the counter, the coin toss, the roulette, the
/// dot art, the colour changer and the kitchen timer, each held to the original's rules (<c>src/applications/poketch</c>).
/// </summary>
public class PoketchToysTests
{
    private static (Poketch Poketch, PoketchContext Context, T App, List<string> Heard) Open<T>(PoketchApp app, int seed = 1)
        where T : PoketchAppState
    {
        var poketch = new Poketch { Enabled = true };
        poketch.Register(app);
        var heard = new List<string>();
        var context = new PoketchContext { Poketch = poketch, Rng = new Random(seed), Sound = heard.Add };
        return (poketch, context, Assert.IsAssignableFrom<T>(poketch.State), heard);
    }

    private static void Run(PoketchAppState app, PoketchContext context, float seconds, float step = 1f / 60f)
    {
        for (float t = 0; t < seconds; t += step) app.Update(step, context);
    }

    // ------------------------------------------------------------------ the calculator

    private static string Type(CalculatorApp calc, PoketchContext context, string keys)
    {
        foreach (char k in keys)
        {
            int id = k switch
            {
                '.' => CalculatorApp.Decimal,
                '+' => CalculatorApp.Plus,
                '-' => CalculatorApp.Minus,
                '*' => CalculatorApp.Times,
                '/' => CalculatorApp.Divide,
                '=' => CalculatorApp.EqualsKey,
                'C' => CalculatorApp.Clear,
                _ => k - '0'
            };
            calc.Press(id, context);
        }
        return calc.Text;
    }

    [Theory]
    // value.c, worked by hand: the sum, the difference below nought, the product, a quotient cut at the places that
    // fit (1 / 3 leaves room for eight after "0."), decimals aligned and trailing noughts dropped
    [InlineData("12+30=", "42")]
    [InlineData("3-5=", "-2")]
    [InlineData("12*12=", "144")]
    [InlineData("7/2=", "3.5")]
    [InlineData("1/3=", "0.33333333")]
    [InlineData("1.5+2.25=", "3.75")]
    [InlineData("2.5*4=", "10")]
    // = again repeats the operation on the result; an operation after a second operand works out the first
    [InlineData("2+3==", "8")]
    [InlineData("2+3*4=", "20")]
    // An operation then = uses the number entered twice
    [InlineData("3+=", "6")]
    public void TheCalculatorWorksAsTheOriginal(string keys, string shown)
    {
        var (_, context, calc, _) = Open<CalculatorApp>(PoketchApp.Calculator);
        Assert.Equal(shown, Type(calc, context, keys));
    }

    [Fact]
    public void TheCalculatorHoldsTenPlacesAndShowsTheErrorPastThem()
    {
        var (_, context, calc, heard) = Open<CalculatorApp>(PoketchApp.Calculator);
        Assert.Equal("0", calc.Text);
        // An eleventh figure doesn't go in
        Assert.Equal("1111111111", Type(calc, context, "11111111111"));
        Assert.All(heard, s => Assert.Equal("poketch", s));
        Assert.Equal(11, heard.Count);

        // Ten nines and one more is eleven places: the error across the display
        Assert.Equal("EEEEEEEEEE", Type(calc, context, "C9999999999+1="));
        // Only a figure, the point or C go on from it; a figure starts afresh
        Assert.Equal("EEEEEEEEEE", Type(calc, context, "+="));
        Assert.Equal("4", Type(calc, context, "4"));

        // Nought divides nothing
        Assert.Equal("EEEEEEEEEE", Type(calc, context, "C5/0="));
        Assert.Equal("0", Type(calc, context, "C"));
    }

    [Fact]
    public void TheCalculatorShowsTheOperationWaiting()
    {
        var (_, context, calc, _) = Open<CalculatorApp>(PoketchApp.Calculator);
        Type(calc, context, "6*");
        Assert.Equal(CalculatorApp.Times, calc.ShownOperator);
        Type(calc, context, "7=");
        Assert.Equal(CalculatorApp.None, calc.ShownOperator);
        Assert.Equal("42", calc.Text);
    }

    [Fact]
    public void ASumThatIsTheNumberOfAPokemonSeenPlaysItsCry()
    {
        var (_, context, calc, _) = Open<CalculatorApp>(PoketchApp.Calculator);
        var cries = new List<Pokemon>();
        context.Cry = cries.Add;
        context.Pokedex = new Pokedex();
        context.Pokedex.UnlockNational();

        Type(calc, context, "20+5=");
        Assert.Empty(cries);
        context.Pokedex.RegisterSeen(25);
        Type(calc, context, "C20+5=");
        Assert.Equal(25, Assert.Single(cries).Species.DexNumber);
        // Past the National Pokédex's 493, no cry
        Type(calc, context, "C500=");
        Assert.Single(cries);
    }

    // ------------------------------------------------------------------ the counter

    [Fact]
    public void TheCounterGoesRoundToNoughtAfterItsMost()
    {
        var (_, context, counter, heard) = Open<CounterApp>(PoketchApp.Counter);
        for (int i = 0; i < CounterApp.Most; i++) counter.Press(0, context);
        Assert.Equal(9999, counter.Value);
        counter.Press(0, context);
        Assert.Equal(0, counter.Value);
        Assert.All(heard, s => Assert.Equal("poketch_count", s));
    }

    // ------------------------------------------------------------------ the coin toss

    [Fact]
    public void TheCoinLandsHeadsOrTailsAsLikelyAndBouncesBeforeItLies()
    {
        var (_, context, coin, heard) = Open<CoinTossApp>(PoketchApp.CoinToss, seed: 7);
        Assert.True(coin.Heads);

        var rng = new Random(7);
        int heads = 0;
        for (int i = 0; i < 400; i++)
        {
            heard.Clear();
            coin.Press(0, context);
            Assert.True(coin.Flying);
            Assert.Equal(rng.Next(2) == 1, coin.Heads);
            // A touch while it flies changes nothing
            bool face = coin.Heads;
            coin.Press(0, context);
            Assert.Equal(face, coin.Heads);
            Run(coin, context, 3f);
            Assert.False(coin.Flying);
            Assert.Equal(0f, coin.Height);
            Assert.Equal("coin_flip", heard[0]);
            Assert.True(heard.Count(s => s == "coin_land") >= 3);
            if (coin.Heads) heads++;
        }
        Assert.InRange(heads, 160, 240);
    }

    [Fact]
    public void TheCoinFliesAsTheOriginalsDoes()
    {
        var (_, context, coin, _) = Open<CoinTossApp>(PoketchApp.CoinToss);
        coin.Press(0, context);
        // Up at 10.5 a frame, slowing by 0.6875: after fifteen frames, near the top, 10.5 × 15 - 0.6875 × 105 = 85.3
        // pixels up
        for (int i = 0; i < 15; i++) coin.Update(CoinTossApp.FrameSeconds, context);
        Assert.InRange(coin.Height, 85f, 85.5f);
        // Down at the 32nd frame and up again at 56 hundredths of its speed: flights of 32, 20, 13 and 9 frames, the
        // last landing slower than 2 a frame, so it lies still at about the 74th
        for (int i = 0; i < 120 && coin.Flying; i++) coin.Update(CoinTossApp.FrameSeconds, context);
        Assert.False(coin.Flying);
        Assert.InRange(coin.Frames, 70, 78);
    }

    // ------------------------------------------------------------------ the roulette

    [Fact]
    public void TheRouletteSpinsUpAndSlowsToAStop()
    {
        var (_, context, wheel, heard) = Open<RouletteApp>(PoketchApp.Roulette);
        wheel.Press(RouletteApp.Stop, context);
        Assert.Equal("poketch_beep", heard.Last());
        Assert.False(wheel.Spinning);

        wheel.Press(RouletteApp.Start, context);
        Assert.True(wheel.Spinning);
        Run(wheel, context, 2f);
        Assert.Equal(12288, wheel.Speed);
        // START and CLEAR beep while it turns, and the page can't be drawn on
        wheel.Press(RouletteApp.Start, context);
        Assert.Equal("poketch_beep", heard.Last());
        wheel.Press(PoketchCanvas.IdOf(3, 3), context);
        Assert.False(wheel.Dot(3, 3));

        wheel.Press(RouletteApp.Stop, context);
        // Held to 6656, then 80 slower a frame: under three and a half seconds to a stop
        Run(wheel, context, 3.5f);
        Assert.False(wheel.Spinning);
        Assert.Equal(0, wheel.Speed);
        Assert.Equal("roulette_spin", heard.Last());
    }

    [Fact]
    public void TheRoulettesPageIsDrawnWithAPenAndWipedClean()
    {
        var (_, context, wheel, _) = Open<RouletteApp>(PoketchApp.Roulette);
        wheel.Press(PoketchCanvas.IdOf(0, 0), context);
        wheel.Press(PoketchCanvas.IdOf(35, 34), context);
        Assert.True(wheel.Dot(0, 0));
        Assert.True(wheel.Dot(35, 34));
        // Touching a dot again leaves it drawn: there is no eraser
        wheel.Press(PoketchCanvas.IdOf(0, 0), context);
        Assert.True(wheel.Dot(0, 0));
        wheel.Press(RouletteApp.Clear, context);
        Assert.False(wheel.Dot(0, 0));
        Assert.False(wheel.Dot(35, 34));
    }

    // ------------------------------------------------------------------ the memo pad

    [Fact]
    public void TheMemoPadWritesWithThePenAndRubsOutWithTheEraser()
    {
        var (_, context, pad, heard) = Open<MemoPadApp>(PoketchApp.MemoPad);
        Assert.True(pad.PenActive);
        // The tool already in hand does nothing
        pad.Press(MemoPadApp.Pen, context);
        Assert.Empty(heard);

        for (int x = 4; x <= 8; x++)
            for (int y = 4; y <= 8; y++)
                pad.Press(PoketchCanvas.IdOf(x, y), context);
        Assert.True(pad.Dot(6, 6));

        pad.Press(MemoPadApp.Eraser, context);
        Assert.False(pad.PenActive);
        Assert.Equal("poketch", Assert.Single(heard));
        // Two blocks by two, up and left of the block touched
        pad.Press(PoketchCanvas.IdOf(7, 7), context);
        Assert.False(pad.Dot(6, 6));
        Assert.False(pad.Dot(7, 6));
        Assert.False(pad.Dot(6, 7));
        Assert.False(pad.Dot(7, 7));
        Assert.True(pad.Dot(8, 8));
        Assert.True(pad.Dot(5, 5));
    }

    // ------------------------------------------------------------------ the dot art

    [Fact]
    public void TheDotArtCyclesItsShadesAndSurvivesASave()
    {
        var (poketch, context, art, _) = Open<DotArtApp>(PoketchApp.DotArt);
        // Untouched, the app's own picture
        for (int y = 0; y < DotArtApp.Height; y++)
            for (int x = 0; x < DotArtApp.Width; x++)
                Assert.Equal(DotArtApp.DefaultDot(x, y), art.Dot(x, y));
        Assert.Null(poketch.Recall(PoketchApp.DotArt));

        int before = art.Dot(0, 0);
        Assert.Equal(1, before);
        var id = DotArtApp.IdOf(0, 0);
        art.Press(id, context);
        Assert.Equal(2, art.Dot(0, 0));
        art.Press(id, context);
        art.Press(id, context);
        Assert.Equal(4, art.Dot(0, 0));
        art.Press(id, context);
        Assert.Equal(1, art.Dot(0, 0));
        art.Press(DotArtApp.IdOf(23, 19), context);
        art.Press(DotArtApp.IdOf(23, 19), context);
        art.Press(DotArtApp.IdOf(5, 2), context);
        int corner = art.Dot(23, 19), other = art.Dot(5, 2);

        var loaded = new Poketch();
        loaded.Load(poketch.Save());
        var again = Assert.IsType<DotArtApp>(loaded.State);
        Assert.Equal(corner, again.Dot(23, 19));
        Assert.Equal(other, again.Dot(5, 2));
        Assert.Equal(1, again.Dot(0, 0));
        Assert.Equal(DotArtApp.DefaultDot(12, 10), again.Dot(12, 10));
    }

    // ------------------------------------------------------------------ the colour changer

    [Fact]
    public void TheColorChangerSetsTheScreensColor()
    {
        var (poketch, context, changer, heard) = Open<ColorChangerApp>(PoketchApp.ColorChanger);
        Assert.Equal(0, poketch.ScreenColor);
        Assert.Equal(Poketch.ScreenColors, changer.Buttons(context).Count);

        changer.Press(3, context);
        Assert.Equal(3, poketch.ScreenColor);
        Assert.Equal("poketch", Assert.Single(heard));
        // The colour it already is changes nothing
        changer.Press(3, context);
        Assert.Single(heard);
        changer.Press(7, context);
        Assert.Equal(7, poketch.ScreenColor);

        var loaded = new Poketch();
        loaded.Load(poketch.Save());
        Assert.Equal(7, loaded.ScreenColor);
    }

    // ------------------------------------------------------------------ the kitchen timer

    [Fact]
    public void TheKitchenTimerIsSetFigureByFigureUpTo99Minutes59()
    {
        var (_, context, timer, heard) = Open<KitchenTimerApp>(PoketchApp.KitchenTimer);
        Assert.True(timer.StopDown);
        Assert.Equal(11, timer.Buttons(context).Count);

        // Down from nought goes round to the top of each figure: 9, 9, 5 and 9
        timer.Press(KitchenTimerApp.MinutesTensDown, context);
        timer.Press(KitchenTimerApp.MinutesOnesDown, context);
        timer.Press(KitchenTimerApp.SecondsTensDown, context);
        timer.Press(KitchenTimerApp.SecondsOnesDown, context);
        Assert.Equal(KitchenTimerApp.MostSeconds, timer.SecondsShown);
        timer.Press(KitchenTimerApp.SecondsTensUp, context);
        Assert.Equal(0, timer.Figures[2]);
        Assert.Empty(heard);

        timer.Press(KitchenTimerApp.Reset, context);
        Assert.Equal(0, timer.SecondsShown);
        // A time of nought doesn't start; STOP beeps while it is being set
        timer.Press(KitchenTimerApp.Start, context);
        Assert.Equal(KitchenTimerApp.Mode.Editing, timer.State);
        timer.Press(KitchenTimerApp.Stop, context);
        Assert.Equal("poketch_beep", heard.Last());
    }

    [Fact]
    public void TheKitchenTimerCountsDownAndRingsAtNought()
    {
        var (_, context, timer, heard) = Open<KitchenTimerApp>(PoketchApp.KitchenTimer);
        // 01:05
        timer.Press(KitchenTimerApp.MinutesOnesUp, context);
        for (int i = 0; i < 5; i++) timer.Press(KitchenTimerApp.SecondsOnesUp, context);
        Assert.Equal(65, timer.SecondsShown);

        timer.Press(KitchenTimerApp.Start, context);
        Assert.Equal(KitchenTimerApp.Mode.Running, timer.State);
        Assert.True(timer.StartDown);
        Assert.False(timer.StopDown);
        // The arrows go while it counts
        Assert.Equal(3, timer.Buttons(context).Count);

        Run(timer, context, 10.2f, 0.1f);
        Assert.Equal(55, timer.SecondsShown);

        // Paused, it holds; started again, it goes on
        timer.Press(KitchenTimerApp.Stop, context);
        Assert.Equal(KitchenTimerApp.Mode.Paused, timer.State);
        Run(timer, context, 30f, 0.1f);
        Assert.Equal(55, timer.SecondsShown);
        timer.Press(KitchenTimerApp.Start, context);

        Run(timer, context, 54.5f, 0.1f);
        Assert.Equal(1, timer.SecondsShown);
        Assert.DoesNotContain("timer_alarm", heard);
        Run(timer, context, 1f, 0.1f);
        Assert.Equal(KitchenTimerApp.Mode.Sounding, timer.State);
        Assert.Equal(0, timer.SecondsShown);
        // It rings every eight frames of thirty a second, on and on: about seven times in two seconds
        int before = heard.Count(s => s == "timer_alarm");
        Run(timer, context, 2f, 1f / 60f);
        Assert.InRange(heard.Count(s => s == "timer_alarm") - before, 6, 8);

        timer.Press(KitchenTimerApp.Stop, context);
        Assert.Equal(KitchenTimerApp.Mode.SoundingPaused, timer.State);
        int rung = heard.Count(s => s == "timer_alarm");
        Run(timer, context, 2f);
        Assert.Equal(rung, heard.Count(s => s == "timer_alarm"));

        timer.Press(KitchenTimerApp.Reset, context);
        Assert.Equal(KitchenTimerApp.Mode.Editing, timer.State);
        Assert.True(timer.StopDown);
        Assert.False(timer.StartDown);
    }

    // ------------------------------------------------------------------ every toy's buttons

    [Theory]
    [InlineData(PoketchApp.Calculator)]
    [InlineData(PoketchApp.MemoPad)]
    [InlineData(PoketchApp.Counter)]
    [InlineData(PoketchApp.CoinToss)]
    [InlineData(PoketchApp.Roulette)]
    [InlineData(PoketchApp.DotArt)]
    [InlineData(PoketchApp.ColorChanger)]
    [InlineData(PoketchApp.KitchenTimer)]
    public void EachToysButtonsLieOnTheScreenApartAndAreTheirOwn(PoketchApp app)
    {
        var (_, context, state, _) = Open<PoketchAppState>(app);
        var buttons = state.Buttons(context);
        Assert.NotEmpty(buttons);
        Assert.Equal(buttons.Count, buttons.Select(b => b.Id).Distinct().Count());
        var taken = new bool[PoketchAppState.Columns, PoketchAppState.Rows];
        foreach (var b in buttons)
        {
            Assert.True(b.X >= 0 && b.Y >= 0 && b.X + b.W <= PoketchAppState.Columns && b.Y + b.H <= PoketchAppState.Rows, $"{app}: {b}");
            for (int x = b.X; x < b.X + b.W; x++)
                for (int y = b.Y; y < b.Y + b.H; y++)
                {
                    Assert.False(taken[x, y], $"{app}: {b} overlaps another button");
                    taken[x, y] = true;
                }
        }
    }
}
