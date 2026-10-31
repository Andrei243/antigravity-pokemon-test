using System.Linq;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// New Game+ (plan 13 · V11; <c>docs/mechanics/rulings.md</c>, "New Game+"): a finished save's carry-over laid over a
/// new game. What is carried and what is left is <see cref="NewGamePlus"/>'s to say; this is where it meets the game.
/// The finished save is only read: its slot stays as it was.
/// </summary>
public partial class GameEngine
{
    // The finished save the new game being begun carries from, from the title's choice until StartNewGame has used it
    private SaveData? newGamePlus;

    /// <summary>The region a NEW GAME+ begins in: the one its save finished; null for a plain new game.</summary>
    private Region? PlusRegion => NewGamePlus.FinishedRegion(newGamePlus);

    /// <summary>
    /// Begins NEW GAME+ from a finished save at once, as the title's third answer and the introduction would: for tests and
    /// the harness. The run is the save's a step harder (<see cref="NewGamePlus.Raised"/>).
    /// </summary>
    public void StartNewGamePlus(SaveData finished, string? name, PlayerLook look, string? rival = null)
    {
        newGamePlus = finished;
        StartNewGame(name, look, rival, NewGamePlus.Raised(finished.Run));
    }

    /// <summary>
    /// Lays the finished save's carry-over over the new game just begun (after <c>InitializeNewGame</c>, before any id is
    /// given), and forgets the save. Nothing when the new game is a plain one.
    /// </summary>
    private void CarryOver()
    {
        if (newGamePlus is not { } old) return;
        newGamePlus = null;

        // The same trainer on a second journey: the card's number and its hidden half, so every carried Pokémon is still
        // the player's own (and a shiny one still shines for them), and the count of ids, so none is given twice
        if (old.TrainerId != 0) flow.TrainerId = old.TrainerId;
        if (old.Encounters is { } encounters) flow.Encounters.SecretId = encounters.SecretId;
        KeepTrainerNumber();
        flow.PokemonIdsIssued = old.PokemonIdsIssued;

        // The Pokédex as it was filled: seen, caught, the shiny ones and the looks (not the National Pokédex itself, nor the diplomas)
        foreach (int seen in old.SeenSpecies) flow.Pokedex.RegisterSeen(seen);
        foreach (int caught in old.CaughtSpecies) flow.Pokedex.RegisterCaught(caught);
        foreach (int shiny in old.CaughtShiny ?? new()) flow.Pokedex.RegisterCaughtShiny(shiny);
        flow.Pokedex.RestoreLooks(old.SeenLooks.Select(l => (SavedLook.SpeciesOf(l.Name)?.DexNumber ?? 0, l.Name, l.Gender)).Where(l => l.Item1 != 0), old.SpindaPersonality);

        // The boxes as they were (the National Pokédex's hundred kept), and the team and the Day Care's put into them
        flow.Boxes = old.Boxes?.ToBoxes() ?? PcBoxes.FromList(old.BoxStorage.Select(p => p.ToPokemon()));
        flow.Boxes.Grow(NewGamePlus.BoxCount(old));
        foreach (var carried in NewGamePlus.ToBox(old))
            if (flow.Boxes.Store(carried.ToPokemon()) == null)
                Log.Warn($"New Game+ found no room in the boxes for {carried.SpeciesName}; it was offered only with room.");

        flow.Money = NewGamePlus.Money(old);
        flow.RunState.Clears = NewGamePlus.Clears(old);
        Log.Info($"New Game+ from a save that finished {NewGamePlus.FinishedRegion(old)?.Name}: {flow.Boxes.Count} Pokémon in the boxes, {flow.Pokedex.CaughtCount} caught, {flow.Money} money, clear {flow.RunState.Clears}, run {flow.Run.PresetKey} at {flow.Run.Difficulty}.");
    }
}
