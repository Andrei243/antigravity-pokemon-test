using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Poké Jobs (plan 15 · E3; <see cref="JobBoard"/>): a PC's board, opened by the script's <c>jobs</c> in a game played by
/// the modern rules. A Pokémon brought back that a job's levels let evolve does so from the board, as the bag's do, and
/// the board is there again afterwards; an Egg the pasture's nursery brings back with no cycles left hatches from the
/// board (plan 35 · BN6), and the field comes back after its scene.
/// </summary>
public partial class GameEngine
{
    // The nursery's Egg collected with no cycles left, hatching once its result is read (plan 35 · BN6)
    private Pokemon? hatchingFromTheBoard;

    internal void OpenJobs()
    {
        flow.JobsScreen.Open(flow.Jobs, flow.Boxes, flow.Bag, Dice.New(), Here.Id);
        flow.State = GameState.Jobs;
    }

    private void UpdateJobs(float dt)
    {
        flow.JobsScreen.Update(dt);
        switch (flow.JobsScreen.TakeCollected())
        {
            // (its hatching waits for the result's panel to be read, "The Egg is about to hatch!")
            case { Hatches: true } nursery:
                hatchingFromTheBoard = nursery.Pokemon;
                break;
            case { LevelsGained: > 0 } back:
            {
                var evolutions = FindEvolutions(new[] { back.Pokemon }, EvolutionTrigger.LevelUp, cancellable: true);
                if (PlayEvolutions(evolutions, GameState.Jobs)) return;
                break;
            }
        }
        if (hatchingFromTheBoard is { } egg && flow.JobsScreen.Result == null)
        {
            hatchingFromTheBoard = null;
            flow.JobsScreen.Close();
            if (HatchFromTheBoard(egg)) return;
        }
        if (flow.JobsScreen.IsActive) return;
        // The board by the pasture's shed sends Pokémon off and brings them back to the boxes: the herd is laid afresh (plan 35 · BN5)
        if (flow.Pasture != null) LayHerd();
        flow.State = GameState.Overworld;
    }

    /// <summary>
    /// The nursery's Egg hatches as it comes back (plan 35 · BN6): the hatching's scene, with its Pokémon's model made
    /// while the screen goes dark, as the team's Eggs hatch. False when it is no Egg with no cycles left.
    /// </summary>
    private bool HatchFromTheBoard(Pokemon egg)
    {
        if (!egg.IsEgg || egg.EggCycles != 0) return false;
        var look = PokemonLook.Of(egg.ModelName, egg.Gender == Gender.Female, egg.IsShiny);
        flow.WantModel(look);
        string place = PlaceName();
        var day = GameClock.Today;
        StartTransition(GameState.Hatch, () =>
        {
            AwaitModels(new[] { look });
            flow.HatchScreen.Begin(egg, place, day);
        });
        return true;
    }
}
