using System.Linq;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The Hall of Fame's end in the game (plan 02 · S14): the scene of the team's entry
/// (<see cref="HallOfFameCeremony"/>, <see cref="GameState.HallOfFameCeremony"/>) and the credits
/// (<see cref="CreditsScreen"/>, <see cref="GameState.Credits"/>), each opened by the Hall of Fame's script
/// (<c>halloffame show</c>, <c>credits</c>), which waits for it; and the Poké Balls the script lays on the room's
/// machine, which the room's own pieces draw.
/// </summary>
public partial class GameEngine
{
    private readonly HallOfFameCeremony ceremony = new();
    private readonly CreditsScreen credits = new();

    /// <summary>
    /// The room's machine shows the balls its own piece has laid on it, plain or lit (plan 09 · L12: the script's
    /// <c>anim halloffame</c>, the original's <c>PlayHallOfFameHealingAnimation</c>): none until the piece begins, one
    /// more every beat of it, and all of them from then until the room is left.
    /// </summary>
    private void KeepTheMachinesBalls()
    {
        if (currentMap.Hall != HallStyle.HallOfFame) return;
        (currentMap.MachineBalls, currentMap.MachineLit) = world.Pieces.Machine(SetPiece.HallOfFame, world.Life.Now);
    }

    /// <summary>The team's models, asked for before a fade and waited on at its midpoint, as a battle's are.</summary>
    private System.Collections.Generic.List<string> ChampionsModels()
    {
        var shown = playerParty.Members.Where(p => !p.IsEgg).Select(p => p.ModelName).Distinct().ToList();
        foreach (string name in shown) PokemonModels.Request(name);
        return shown;
    }

    /// <summary>Fades into the scene of the team's entry into the Hall of Fame; a script waits for it.</summary>
    private void OpenCeremony()
    {
        var shown = ChampionsModels();
        StartTransition(GameState.HallOfFameCeremony, () =>
        {
            AwaitModels(shown);
            ceremony.Begin(playerParty.Members, playerName, trainerId, playTime, PlayerIdentity.Character);
        });
    }

    private void UpdateCeremony(float dt)
    {
        ceremony.Update(dt);
        // It closes to black by itself, and the script that opened it has the field dark behind it
        if (!ceremony.IsActive) currentState = GameState.Overworld;
    }

    /// <summary>Fades into the credits; a script waits for them.</summary>
    private void OpenCredits()
    {
        var shown = ChampionsModels();
        StartTransition(GameState.Credits, () =>
        {
            AwaitModels(shown);
            credits.Begin(playerName, playerParty.Members, note => ModernUi.WrapEvenly(note, CreditsRoll.ColumnWidth, CreditsRoll.NoteSize));
        });
    }

    private void UpdateCredits(float dt)
    {
        credits.Update(dt);
        if (credits.IsActive) return;
        currentState = GameState.Overworld;
        PlayFieldMusic();
    }
}
