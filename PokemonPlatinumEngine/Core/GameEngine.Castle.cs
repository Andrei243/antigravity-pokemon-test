using System;
using System.Linq;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The Battle Castle in the game (plan 06 · R18c): where its rules (<see cref="BattleCastleChallenge"/>) meet the
/// valet's two counters (<see cref="CastleScreen"/>, <see cref="GameState.CastleCounter"/>). Everything else is the
/// frame's (<c>GameEngine.Frontier.cs</c>): the three are entered on the team's screen, a battle is one of the
/// Frontier's own, fought on the challenge's own three so that what it did to them stays, and the trainer who walks
/// into the throne room takes the look of who was drawn.
/// </summary>
public partial class GameEngine
{
    public const string CastleLobbyMap = "BattleCastle";

    private bool showCastlePoints;

    /// <summary>One of the valet's counters is opened (<c>OpenBattleCastleSelfApp</c>, <c>OpenBattleCastleOpponentApp</c>); the script waits.</summary>
    private void OpenCastleCounter(bool own)
    {
        if (flow.Castle == null) return;
        flow.ScriptAnswer = 0;
        // The trainer to come was drawn since the challenge went in: their pictures are asked for as the counter opens
        AskForCastlePictures();
        flow.CastleScreen.Open(flow.Castle, flow.Frontier, own);
        flow.State = GameState.CastleCounter;
    }

    /// <summary>
    /// The pictures a challenge will want are asked for as it goes in: the three's and the first trainer's, so the
    /// counters have them to show.
    /// </summary>
    private void AskForCastlePictures()
    {
        if (flow.Castle == null) return;
        foreach (var pokemon in flow.Castle.Team.Members.Concat(flow.Castle.Foe?.Pokemon ?? Enumerable.Empty<Models.Pokemon>()))
        {
            flow.WantModel(pokemon.ModelName);
            Graphics.PokemonSprites.Request(pokemon.ModelName);
        }
    }

    private void UpdateCastleCounter(float dt)
    {
        flow.CastleScreen.Update(dt);
        if (flow.CastleScreen.IsActive) return;
        flow.ScriptAnswer = 1;
        flow.State = GameState.Overworld;
    }

    /// <summary>The Castle Points stand over the field while the valet speaks of them, and go as the script ends.</summary>
    private void DrawCastlePoints()
    {
        if (!ScriptRunning || flow.Castle == null) showCastlePoints = false;
        if (showCastlePoints && flow.Castle != null) ModernUi.CastlePointsCount(flow.Castle.Points(flow.Frontier));
    }

    private sealed partial class FieldHost
    {
        public BattleCastleChallenge? Castle { get => game.flow.Castle; set => game.flow.Castle = value; }
    }
}
