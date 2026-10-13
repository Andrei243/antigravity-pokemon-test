using System.Linq;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The Battle Arcade in the game (plan 06 · R18d): where its rules (<see cref="BattleArcadeChallenge"/>) meet the
/// game board its events are drawn on (<see cref="ArcadeBoardScreen"/>, <see cref="GameState.ArcadeBoard"/>).
/// Everything else is the frame's (<c>GameEngine.Frontier.cs</c>): the three are entered on the team's screen, a
/// battle is one of the Frontier's own, opened under the weather or in the Trick Room the board set
/// (<see cref="FrontierConditions"/>), and the trainer who walks into the room takes the look of who was drawn.
/// </summary>
public partial class GameEngine
{
    public const string ArcadeLobbyMap = "BattleArcade";

    /// <summary>The game board the challenge has brought up is played (<c>OpenBattleArcadeApp</c>); the script waits.</summary>
    private void OpenArcadeBoard()
    {
        if (flow.Arcade?.Board == null) return;
        scriptAnswer = 0;
        // Both threes stand beside the board: their pictures are asked for as it opens
        foreach (var pokemon in flow.Arcade.Team.Members.Concat(flow.Arcade.Foe?.Pokemon ?? Enumerable.Empty<Models.Pokemon>()))
        {
            flow.WantModel(pokemon.ModelName);
            Graphics.PokemonSprites.Request(pokemon.ModelName);
        }
        flow.ArcadeBoardScreen.Open(flow.Arcade);
        flow.State = GameState.ArcadeBoard;
    }

    private void UpdateArcadeBoard(float dt)
    {
        flow.ArcadeBoardScreen.Update(dt);
        if (flow.ArcadeBoardScreen.IsActive) return;
        // The script that opened the board goes on to carry out the event it stopped on
        scriptAnswer = 1;
        flow.State = GameState.Overworld;
    }

    private sealed partial class FieldHost
    {
        public BattleArcadeChallenge? Arcade { get => game.flow.Arcade; set => game.flow.Arcade = value; }
    }
}
