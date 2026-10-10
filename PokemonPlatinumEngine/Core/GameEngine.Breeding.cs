using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The Day Care and Eggs in the game (plan 06 · R15): the couple walk every step the player does, find Eggs and count
/// the team's down (<see cref="DayCare.Step"/>), and an Egg with no cycles left hatches in a scene of its own
/// (<see cref="HatchScreen"/>, <see cref="GameState.Hatch"/>), after which it is in the Pokédex.
/// </summary>
public partial class GameEngine
{
    /// <summary>Solaceon Town's Day Care (saved).</summary>
    private readonly DayCare dayCare = new();
    private readonly HatchScreen hatchScreen = new();

    /// <summary>
    /// The player's whole number as a Pokémon's shininess reads it: the hidden half the card doesn't show and the card's
    /// ID (<see cref="PlayerIdentity.Number"/>), given out as a game begins or loads.
    /// </summary>
    private void KeepTrainerNumber() => PlayerIdentity.SetNumber(TrainerNumber);

    /// <summary>
    /// The Day Care's part of a step (<c>Field_UpdateDaycare</c>): the couple walk it, and at the end of an egg cycle an
    /// Egg of the team may be ready; then the field's <c>common.HatchEgg</c> runs. True when it did.
    /// </summary>
    private bool DayCareStep()
    {
        if (dayCare.Step(playerParty, fieldRandom, GameClock.Today, Ruleset.Current) == null) return false;
        return StartScript(FieldScripts.HatchEgg);
    }

    /// <summary>
    /// Fades into the hatching of the team's first Egg with no cycles left (<c>Party_GetFirstEgg</c>, the original's
    /// <c>HatchEgg</c>): its Pokémon's model is made while the screen goes dark. False when there is none.
    /// </summary>
    private bool HatchFirstEgg()
    {
        if (playerParty.Members.FirstOrDefault(p => p.IsEgg && p.EggCycles == 0) is not { } egg) return false;
        PokemonModels.Request(egg.ModelName);
        string place = PlaceName();
        var day = GameClock.Today;
        StartTransition(GameState.Hatch, () =>
        {
            AwaitModels(new[] { egg.ModelName });
            hatchScreen.Begin(egg, place, day);
        });
        return true;
    }

    /// <summary>The scene has ended: the Pokémon is in the Pokédex and the Pokétch's history, and the field comes back.</summary>
    private void FinishHatch()
    {
        if (hatchScreen.Pokemon is { IsEgg: false } hatched)
        {
            playerPokedex.RegisterSeen(hatched.Species.DexNumber);
            playerPokedex.RegisterCaught(hatched.Species.DexNumber);
            poketch.Remember(hatched);
            trainerScore = TrainerScore.Add(trainerScore, TrainerScore.HatchedEgg);
        }
        StartTransition(GameState.Overworld, PlayFieldMusic);
    }
}
