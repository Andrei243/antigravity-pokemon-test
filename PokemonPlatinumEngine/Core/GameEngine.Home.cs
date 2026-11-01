using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The title's HOME (plan 12 · Q28, Q29; <c>docs/mechanics/rulings.md</c>, "Trades between saves and the Home box"):
/// trades between the player's own saves and the Home box they share, read from the slots' files and written back by
/// <see cref="HomeMoves"/> without a game being loaded; and, as a save is continued, the Pokémon a trade brought into it
/// handed to <see cref="ReceiveTradedPokemon"/>, which registers each and plays its trade evolution there.
/// </summary>
public partial class GameEngine
{
    // The Pokémon a trade brought into the save just continued, each with the species it was traded for, to be welcomed
    private readonly Queue<(Guid Id, string? For)> tradesToTakeUp = new();

    private List<HomeSlot> HomeSlots() =>
        titleSlots.Where(s => s.Taken && s.Save != null && s.Outcome == SaveLoad.Ok).Select(s => new HomeSlot(s.Number, s.Save!)).ToList();

    private void OpenHome()
    {
        var (why, bank) = HomeMoves.ReadBank(SavePaths.Current);
        if (why != HomeRefusal.None) Log.Warn($"The Home box ({SavePaths.HomeBoxName}) can't be used: {why}.");
        flow.HomeScreen.Open(HomeSlots(), bank, why);
        flow.State = GameState.Home;
    }

    private void UpdateHome(float dt)
    {
        flow.HomeScreen.Update(dt);
        if (flow.HomeScreen.TakeRequest() is { } request) CarryOutHome(request);
        else if (!flow.HomeScreen.IsActive) flow.State = GameState.Title;
    }

    /// <summary>
    /// A trade or a move to or from the Home box, carried out on the files; the slots the title holds are read again
    /// afterwards, so that its cards and the screen show the saves as they now are.
    /// </summary>
    internal HomeRefusal CarryOutHome(HomeRequest request)
    {
        var paths = SavePaths.Current;
        var outcome = request.Kind switch
        {
            HomeRequestKind.Trade => HomeMoves.Trade(paths, request.Slot, request.Pokemon, request.OtherSlot, request.Other),
            HomeRequestKind.Deposit => HomeMoves.Deposit(paths, request.Slot, request.Pokemon),
            _ => HomeMoves.Withdraw(paths, request.Slot, request.Pokemon)
        };
        if (outcome != HomeRefusal.None) Log.Warn($"HOME: a {request.Kind} with slot {request.Slot} was not made: {outcome}.");
        titleSlots = SaveSlots.Read(paths);
        var (why, bank) = HomeMoves.ReadBank(paths);
        flow.HomeScreen.Finish(request, outcome, HomeSlots(), bank, why);
        return outcome;
    }

    /// <summary>Notes the Pokémon a trade brought into a save being continued, to be welcomed once the field is up.</summary>
    private void TakeUpTheTrades(SaveData save)
    {
        tradesToTakeUp.Clear();
        foreach (var arrival in save.ArrivedByTrade ?? new()) tradesToTakeUp.Enqueue((arrival.Id, arrival.For));
    }

    /// <summary>
    /// One Pokémon a trade brought in, welcomed: handed to <see cref="ReceiveTradedPokemon"/>, where it is already, so
    /// that it is registered, starts its friendship afresh and evolves if trading was its way. True when its evolution
    /// has taken the screen. The save written next lists it no longer.
    /// </summary>
    private bool HandOverATrade()
    {
        while (tradesToTakeUp.Count > 0)
        {
            var (id, forSpecies) = tradesToTakeUp.Dequeue();
            var arrived = OwnedPokemon().FirstOrDefault(p => p.Id == id);
            if (arrived == null) continue;
            var before = flow.State;
            ReceiveTradedPokemon(arrived, null, forSpecies != null ? PokemonDatabase.Get(forSpecies) : null);
            Log.Info($"{arrived.Species.Name} came by a trade and was taken up.");
            if (flow.State != before) return true;
        }
        return false;
    }

    /// <summary>The trades a continued save still has to welcome (for the tests).</summary>
    internal int TradesWaiting => tradesToTakeUp.Count;
}
