using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 40 · GE8: the trade board and blind trades (Models/TradeBoard.cs, Data/trade-board.json, common.TradeDesk; rulings,
/// "The trade board and blind trades"). A day's board is the same every time and the next day's another; a request asks its
/// species and level; a blind trade's species is of the regions entered and never legendary; five a day; a run that holds
/// its Pokémon is refused; a Kadabra received would evolve; nothing is in two places.
/// </summary>
public class TradeBoardTests
{
    private static readonly DateTime Day = new(2026, 6, 1);
    private static readonly string[] Entered = { RegionDatabase.Sinnoh, RegionDatabase.Kanto };

    private static Pokemon Mon(string species, int level, int seed = 1) => new(PokemonDatabase.Get(species)!, level, new Random(seed));

    [Fact]
    public void ADaysBoardIsTheSameEveryTimeAndTheNextDaysAnother()
    {
        var first = TradeBoard.Requests(Day, RegionDatabase.Sinnoh, Entered);
        var again = TradeBoard.Requests(Day, RegionDatabase.Sinnoh, Entered);
        Assert.Equal(TradeBoard.RequestsADay, first.Count);
        Assert.Equal(first, again);
        Assert.NotEqual(first, TradeBoard.Requests(Day.AddDays(1), RegionDatabase.Sinnoh, Entered));
        // Each region has its own
        Assert.NotEqual(first.Select(r => r.Wants), TradeBoard.Requests(Day, RegionDatabase.Kanto, Entered).Select(r => r.Wants));
        // Wanted of the region's own Pokédex, offered of the regions entered; never legendary, mythical or ours
        var sinnoh = TradeBoard.SpeciesOf(RegionDatabase.Sinnoh).Select(s => s.Name).ToHashSet();
        var offered = TradeBoard.SpeciesOf(Entered).Select(s => s.Name).ToHashSet();
        for (int d = 0; d < 60; d++)
            foreach (var r in TradeBoard.Requests(Day.AddDays(d), RegionDatabase.Sinnoh, Entered))
            {
                Assert.Contains(r.Wants, sinnoh);
                Assert.Contains(r.Offers, offered);
                Assert.NotEqual(r.Wants, r.Offers);
                Assert.True(TradeBoard.Tradable(PokemonDatabase.Get(r.Offers)!));
                Assert.InRange(r.AtLeast, 5, 40);
            }
    }

    [Fact]
    public void ARequestAsksItsSpeciesAndLevelAndIsMetOnce()
    {
        var board = new TradeBoard();
        var request = TradeBoard.Requests(Day, RegionDatabase.Sinnoh, Entered)[0];
        var party = new Party();
        var right = Mon(request.Wants, request.AtLeast);
        party.Add(Mon("Turtwig", 20));
        party.Add(right);
        Assert.Null(board.WhyNot(Day, request, right, party, runHolds: false));
        Assert.Equal("level", board.WhyNot(Day, request, Mon(request.Wants, request.AtLeast - 1), party, false));
        Assert.Equal("species", board.WhyNot(Day, request, party.Members[0], party, false));
        Assert.Equal("run", board.WhyNot(Day, request, right, party, runHolds: true));
        board.Meet(Day, request);
        Assert.Equal("met", board.WhyNot(Day, request, right, party, false));
        // The next day's board is new
        Assert.False(board.IsMet(Day.AddDays(1), request));
    }

    [Fact]
    public void BlindTradesAreFiveADayOfTheRegionsEnteredAndNeverLegendary()
    {
        var board = new TradeBoard();
        var party = new Party();
        party.Add(Mon("Turtwig", 20));
        party.Add(Mon("Bidoof", 10));
        var regions = TradeBoard.SpeciesOf(Entered).Select(s => s.Name).ToHashSet();
        for (int d = 0; d < 30; d++)
            for (int n = 0; n < TradeBoard.BlindADay; n++)
            {
                var (trainer, species) = TradeBoard.Blind(Day.AddDays(d), n, Entered);
                Assert.Contains(species, regions);
                var s = PokemonDatabase.Get(species)!;
                Assert.False(s.Legendary || s.Mythical || s.IsOurs, species);
                Assert.Contains(trainer.Name, TradeBoard.Table.Names);
            }
        // The same draw every time
        Assert.Equal(TradeBoard.Blind(Day, 2, Entered), TradeBoard.Blind(Day, 2, Entered));
        for (int n = 0; n < TradeBoard.BlindADay; n++)
        {
            Assert.Null(board.WhyNot(Day, null, party.Members[1], party, false));
            board.MakeBlind(Day);
        }
        Assert.Equal("spent", board.WhyNot(Day, null, party.Members[1], party, false));
        Assert.Equal(TradeBoard.BlindADay, board.BlindLeft(Day.AddDays(1)));
        // An Egg and the last able to fight are never sent
        var alone = new Party();
        alone.Add(Mon("Turtwig", 20));
        Assert.Equal("last", new TradeBoard().WhyNot(Day, null, alone.Members[0], alone, false));
    }

    [Fact]
    public void AKadabraReceivedWouldEvolveAndATrainerFromAbroadSpeaksAnotherLanguage()
    {
        var trainer = new BoardTrainer("Wren", "lass", 12345, "German");
        var kadabra = TradeBoard.Make(trainer, "Kadabra", null, 30, Day, new Random(1));
        Assert.Equal("German", kadabra.Language);
        Assert.Equal(new TrainerMark("Wren", 12345, PokemonPlatinumEngine.Core.PlayerLook.Girl), kadabra.OriginalTrainer);
        Assert.Equal(30, kadabra.Level);
        Assert.NotNull(Evolution.Find(kadabra, EvolutionTrigger.Trade, new EvolutionContext()));
        // Some trainers of a long run of days come from abroad, as one in four should
        int abroad = Enumerable.Range(0, 200).Count(d => TradeBoard.Blind(Day.AddDays(d), 0, Entered).Trainer.Language != null);
        Assert.InRange(abroad, 25, 80);
    }

    [Fact]
    public void TheDesksScriptMakesABlindTradeAndNothingIsInTwoPlaces()
    {
        var party = new Party();
        party.Add(Mon("Turtwig", 20));
        var given = Mon("Bidoof", 12);
        party.Add(given);
        var story = new StoryState();
        var host = new HeadlessScriptHost(story, party) { BoardChoice = TradeBoard.RequestsADay + 1, PokemonChoice = 1, RegionsEnteredByTheTest = Entered };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("TradeDesk")!, new NPC { Name = "Trade Receptionist" });
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.Equal(2, party.Count);
        Assert.DoesNotContain(given, party.Members);
        var received = party.Members[1];
        Assert.Equal(12, received.Level);
        Assert.Equal(TradeBoard.Blind(PokemonPlatinumEngine.Core.GameClock.Today, 0, Entered).Species, received.Species.Name);
        Assert.True(story.Has(TradeBoard.FirstTradeFlag));
        Assert.Equal(TradeBoard.BlindADay - 1, host.Board!.BlindLeft(PokemonPlatinumEngine.Core.GameClock.Today));
    }

    [Fact]
    public void TheBoardsScreenChoosesARequestOrTheBlindTradeWithoutInput()
    {
        var requests = TradeBoard.Requests(Day, RegionDatabase.Sinnoh, Entered);
        var screen = new TradeBoardScreen();
        screen.Open(RegionDatabase.Sinnoh, requests, new[] { false, true, false }, 5);
        screen.Move(1, 0);
        screen.Confirm();
        // The second is met: the board stays open
        Assert.True(screen.IsActive);
        screen.Move(1, 0);
        screen.Confirm();
        Assert.Equal(3, screen.Outcome);
        screen.Open(RegionDatabase.Sinnoh, requests, new[] { false, false, false }, 0);
        screen.Move(0, 1);
        screen.Confirm();
        Assert.True(screen.IsActive);
        screen.Open(RegionDatabase.Sinnoh, requests, new[] { false, false, false }, 2);
        screen.Move(0, 1);
        screen.Confirm();
        Assert.Equal(TradeBoard.RequestsADay + 1, screen.Outcome);
    }
}
