using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Poké Jobs (plan 15 · E3; <see cref="JobBoard"/>): a PC's board, opened by the script's <c>jobs</c> in a game played by
/// the modern rules. A Pokémon brought back that a job's levels let evolve does so from the board, as the bag's do, and
/// the board is there again afterwards.
/// </summary>
public partial class GameEngine
{
    internal void OpenJobs()
    {
        flow.JobsScreen.Open(flow.Jobs, flow.Boxes, flow.Bag, Dice.New());
        flow.State = GameState.Jobs;
    }

    private void UpdateJobs(float dt)
    {
        flow.JobsScreen.Update(dt);
        if (flow.JobsScreen.TakeCollected() is { LevelsGained: > 0 } back)
        {
            var evolutions = FindEvolutions(new[] { back.Pokemon }, EvolutionTrigger.LevelUp, cancellable: true);
            if (PlayEvolutions(evolutions, GameState.Jobs)) return;
        }
        if (flow.JobsScreen.IsActive) return;
        flow.State = GameState.Overworld;
    }
}
