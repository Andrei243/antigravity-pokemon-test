using System;
using System.Linq;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The Battle Dome in the game (plan 14 · B8): where its rules (<see cref="BattleDomeChallenge"/>) meet the bracket
/// they are shown on (<see cref="BracketScreen"/>, <see cref="GameState.Bracket"/>). Everything else is the
/// Frontier's frame (<c>GameEngine.Frontier.cs</c>): the three are entered on the team's screen, a battle is one of
/// the Frontier's own, and the trainer who walks onto the stage takes the look of who was drawn.
/// </summary>
public partial class GameEngine
{
    public const string DomeLobbyMap = "BattleFrontierAnnex", DomeHallMap = "BattleDome";

    /// <summary>The bracket is opened, to be looked at or for two to be sent in; the script waits.</summary>
    private void OpenBracket(bool pick)
    {
        if (flow.Dome is not { } dome) return;
        flow.ScriptAnswer = 0;
        flow.EnteredSlots = Array.Empty<int>();
        flow.BracketScreen.Open(dome, dome.OwnThree(flow.Party), pick);
        flow.State = GameState.Bracket;
    }

    private void UpdateBracket(float dt)
    {
        flow.BracketScreen.Update(dt);
        if (flow.BracketScreen.IsActive) return;
        // The script that opened it hears which two of the three are sent in, when it asked for them
        flow.EnteredSlots = flow.BracketScreen.Outcome.ToList();
        flow.ScriptAnswer = flow.BracketScreen.Answer;
        flow.State = GameState.Overworld;
    }

    private sealed partial class FieldHost
    {
        public BattleDomeChallenge? Dome { get => game.flow.Dome; set => game.flow.Dome = value; }
        // The Battle Palace's and the Battle Arena's round under way (plan 14 · B9): nothing else of theirs is the engine's
        public BattleVenueChallenge? Venue { get => game.flow.Venue; set => game.flow.Venue = value; }
    }
}
