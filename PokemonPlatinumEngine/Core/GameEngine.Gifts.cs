using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The gift box and the trade board in the game (plan 40 · GE7, GE8): where their rules (<see cref="GiftBox"/>,
/// <see cref="TradeBoard"/>, GPU-free) meet their screens (<see cref="UI.GiftBoxScreen"/>, <see cref="UI.TradeBoardScreen"/>)
/// and the scripts' host. The festivals' flags are kept by the clock (<see cref="KeepTheClock"/>, plan 40 · GE9).
/// </summary>
public partial class GameEngine
{
    /// <summary>What the gifts' terms read of the player now.</summary>
    internal GiftFacts GiftFactsNow() => new()
    {
        Today = GameClock.Today,
        HallOfFame = region => RegionDatabase.Get(region) is { } r && flow.Story.Has(r.StoryCompleteFlag),
        Caught = flow.Pokedex.CaughtCount,
        HoursPlayed = flow.PlayTime / 3600.0,
        Started = flow.AdventureStarted,
        Run = flow.Run,
        TeamHasRoom = !flow.Party.IsFull
    };

    /// <summary>The regions the player has entered, in the chain's order (what the board's offers and blind trades are drawn from).</summary>
    internal IReadOnlyList<string> RegionsEnteredNow()
    {
        var entered = RegionDatabase.All.Where(r => r.File != null && !r.IsHub && flow.Story.HasEntered(r.Id)).Select(r => r.Id).ToList();
        if (!entered.Contains(Here.Id) && !Here.IsHub) entered.Add(Here.Id);
        return entered;
    }

    /// <summary>Whether the run under way holds its Pokémon, as the Home box asks of a save.</summary>
    internal bool RunHoldsTradesNow() =>
        TradeBoard.RunHolds(flow.Run, flow.RunState.Ended, flow.HallOfFame.Entries.Count > 0);

    private void OpenGiftBox()
    {
        flow.ScriptAnswer = 0;
        flow.GiftBoxScreen.Open(flow.Gifts, GiftFactsNow());
        flow.State = GameState.GiftBox;
    }

    private void UpdateGiftBox(float dt)
    {
        flow.GiftBoxScreen.Update(dt);
        if (flow.GiftBoxScreen.IsActive) return;
        flow.ScriptAnswer = flow.GiftBoxScreen.Outcome;
        flow.State = GameState.Overworld;
    }

    private void OpenTradeBoard(string region)
    {
        flow.ScriptAnswer = 0;
        var today = GameClock.Today;
        var requests = TradeBoard.Requests(today, region, RegionsEnteredNow());
        flow.TradeBoardScreen.Seen = species => PokemonDatabase.Get(species) is { } s && flow.Pokedex.IsSeen(s.DexNumber);
        flow.TradeBoardScreen.Open(region, requests, requests.Select(r => flow.Board.IsMet(today, r)).ToList(), flow.Board.BlindLeft(today));
        flow.State = GameState.TradeBoard;
    }

    private void UpdateTradeBoard(float dt)
    {
        flow.TradeBoardScreen.Update(dt);
        if (flow.TradeBoardScreen.IsActive) return;
        flow.ScriptAnswer = flow.TradeBoardScreen.Outcome;
        flow.State = GameState.Overworld;
    }
}
