using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The Battle Factory in the game (plan 06 · R18b): where its rules (<see cref="BattleFactoryChallenge"/>) meet the
/// screen its rentals are chosen and traded on (<see cref="FactoryScreen"/>, <see cref="GameState.FactoryRentals"/>).
/// Everything else is the frame's (<c>GameEngine.Frontier.cs</c>): a battle is one of the Frontier's own, the
/// trainer who walks into the battle room takes the look of who was drawn, and the board is the Frontier's.
/// </summary>
public partial class GameEngine
{
    public const string FactoryLobbyMap = "BattleFactory";

    /// <summary>The six on offer are opened for three to be chosen (<c>OpenBattleFactoryAppInitial</c>); the script waits.</summary>
    private void OpenFactoryRentals(int count)
    {
        if (flow.Factory == null) return;
        scriptAnswer = 0;
        enteredSlots = Array.Empty<int>();
        flow.FactoryScreen.OpenToRent(flow.Factory.Offer.Select(flow.Factory.Build).ToList(), count);
        flow.State = GameState.FactoryRentals;
    }

    /// <summary>A trade is offered after a battle won (<c>OpenBattleFactoryAppForTrade</c>): the three in hand and the beaten trainer's three.</summary>
    private void OpenFactoryTrade()
    {
        if (flow.Factory == null) return;
        scriptAnswer = 0;
        enteredSlots = Array.Empty<int>();
        // The trainer just beaten is the one before the battle to come
        var beaten = Data.FrontierDatabase.Trainer(flow.Factory.Trainers[Math.Max(0, flow.Factory.Battle - 1)]);
        flow.FactoryScreen.OpenToTrade(flow.Factory.Team.Select(flow.Factory.Build).ToList(), flow.Factory.Foe.Select(flow.Factory.Build).ToList(), beaten.Name);
        flow.State = GameState.FactoryRentals;
    }

    /// <summary>
    /// The round's rentals are asked for as it is drawn at the counter: their models are meshed and their menu
    /// sprites baked over the walk in, so the six are there to see when the corridor shows them.
    /// </summary>
    private void AskForRentalPictures()
    {
        if (flow.Factory == null) return;
        foreach (var rental in flow.Factory.Offer.Concat(flow.Factory.Team).Concat(flow.Factory.Foe).Concat(flow.Factory.NextFoe))
        {
            flow.WantModel(rental.Species);
            Graphics.PokemonSprites.Request(rental.Species);
        }
    }

    private void UpdateFactoryRentals(float dt)
    {
        flow.FactoryScreen.Update(dt);
        if (flow.FactoryScreen.IsActive) return;
        // The script that opened the screen hears what was decided
        enteredSlots = flow.FactoryScreen.Outcome.ToList();
        scriptAnswer = enteredSlots.Count > 0 ? 1 : 0;
        flow.State = GameState.Overworld;
    }

    private sealed partial class FieldHost
    {
        public BattleFactoryChallenge? Factory { get => game.flow.Factory; set => game.flow.Factory = value; }
    }
}
