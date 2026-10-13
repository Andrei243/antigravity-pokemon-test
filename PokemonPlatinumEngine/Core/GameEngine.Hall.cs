using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The Battle Hall in the game (plan 06 · R18c): where its rules (<see cref="BattleHallChallenge"/>) meet the board
/// its types are chosen on (<see cref="HallBoardScreen"/>, <see cref="GameState.HallBoard"/>). Everything else is
/// the frame's (<c>GameEngine.Frontier.cs</c>): the Pokémon is entered on the team's screen, a battle is one of the
/// Frontier's own, and the trainer who comes onto the stage takes the look of who was drawn.
/// </summary>
public partial class GameEngine
{
    public const string HallLobbyMap = "BattleHall";

    private BattleHallChallenge? hall;
    private readonly HallBoardScreen hallBoard = new();

    /// <summary>The board of types is opened for the battle to come (<c>OpenBattleHallApp</c>); the script waits.</summary>
    private void OpenHallBoard()
    {
        if (hall == null) return;
        scriptAnswer = 0;
        enteredSlots = Array.Empty<int>();
        var entered = hall.Slots.Where(s => s >= 0 && s < flow.Party.Count).Select(s => flow.Party.Members[s]).ToList();
        hallBoard.Open(hall, entered);
        flow.State = GameState.HallBoard;
    }

    private void UpdateHallBoard(float dt)
    {
        hallBoard.Update(dt);
        if (hallBoard.IsActive) return;
        // The script that opened the board hears which type was chosen
        enteredSlots = new List<int> { hallBoard.Outcome };
        scriptAnswer = 1;
        flow.State = GameState.Overworld;
    }

    private sealed partial class FieldHost
    {
        public BattleHallChallenge? Hall { get => game.hall; set => game.hall = value; }
    }
}
