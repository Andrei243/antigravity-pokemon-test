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

    private BattleFactoryChallenge? factory;
    private readonly FactoryScreen factoryScreen = new();

    /// <summary>The six on offer are opened for three to be chosen (<c>OpenBattleFactoryAppInitial</c>); the script waits.</summary>
    private void OpenFactoryRentals(int count)
    {
        if (factory == null) return;
        scriptAnswer = 0;
        enteredSlots = Array.Empty<int>();
        factoryScreen.OpenToRent(factory.Offer.Select(factory.Build).ToList(), count);
        currentState = GameState.FactoryRentals;
    }

    /// <summary>A trade is offered after a battle won (<c>OpenBattleFactoryAppForTrade</c>): the three in hand and the beaten trainer's three.</summary>
    private void OpenFactoryTrade()
    {
        if (factory == null) return;
        scriptAnswer = 0;
        enteredSlots = Array.Empty<int>();
        // The trainer just beaten is the one before the battle to come
        var beaten = Data.FrontierDatabase.Trainer(factory.Trainers[Math.Max(0, factory.Battle - 1)]);
        factoryScreen.OpenToTrade(factory.Team.Select(factory.Build).ToList(), factory.Foe.Select(factory.Build).ToList(), beaten.Name);
        currentState = GameState.FactoryRentals;
    }

    /// <summary>
    /// The round's rentals are asked for as it is drawn at the counter: their models are meshed and their menu
    /// sprites baked over the walk in, so the six are there to see when the corridor shows them.
    /// </summary>
    private void AskForRentalPictures()
    {
        if (factory == null) return;
        foreach (var rental in factory.Offer.Concat(factory.Team).Concat(factory.Foe).Concat(factory.NextFoe))
        {
            Graphics.PokemonModels.Request(rental.Species);
            Graphics.PokemonSprites.Request(rental.Species);
        }
    }

    private void UpdateFactoryRentals(float dt)
    {
        factoryScreen.Update(dt);
        if (factoryScreen.IsActive) return;
        // The script that opened the screen hears what was decided
        enteredSlots = factoryScreen.Outcome.ToList();
        scriptAnswer = enteredSlots.Count > 0 ? 1 : 0;
        currentState = GameState.Overworld;
    }

    private sealed partial class FieldHost
    {
        public BattleFactoryChallenge? Factory { get => game.factory; set => game.factory = value; }
    }
}
