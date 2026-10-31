using System.Collections.Generic;
using System.Text.Json.Serialization;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// What a run remembers (plan 13 · V4): the places a wild Pokémon has been met under the Nuzlocke, the graveyard, and
/// whether the run has ended. Plain data, kept whole in <c>SaveData.RunState</c> (null when nothing is in it, in a
/// Normal game and in every save from before it, so no save version was raised). The run timer (V10) is here too; a later session adds the clears (V11).
/// </summary>
public sealed class RunState
{
    /// <summary>
    /// The places where the run's one wild Pokémon has been met, by <see cref="Overworld.MapArea.Key"/> on a streamed
    /// map and the map's name otherwise (<see cref="Nuzlocke.PlaceKeyOf"/>): in each, the first Pokémon met was the chance.
    /// </summary>
    public List<string> AreasMet { get; set; } = new();

    /// <summary>The run's fallen, in the order they fell.</summary>
    public List<FallenPokemon> Graveyard { get; set; } = new();

    /// <summary>The run is over (a whiteout under the Nuzlocke): the save stays, to be looked at, and is never written again.</summary>
    public bool Ended { get; set; }

    /// <summary>What ended it: <c>whiteout</c>; null while it goes on.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EndedBy { get; set; }

    /// <summary>The run timer and its splits (plan 13 · V10): kept in every game, whatever the options' RUN TIMER row says; null in a save from before it.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RunTimer? Timer { get; set; }

    /// <summary>Whether there is anything to keep: an empty state is saved as nothing.</summary>
    [JsonIgnore]
    public bool IsEmpty => AreasMet.Count == 0 && Graveyard.Count == 0 && !Ended && Timer == null;

    public bool HasMet(string place) => AreasMet.Contains(place);

    public void Meet(string place)
    {
        if (!AreasMet.Contains(place)) AreasMet.Add(place);
    }
}

/// <summary>
/// A Pokémon the run lost: itself as it fell, where (the place's key and its English name, as the save keeps names; it
/// is shown in the language chosen through <c>LocalNames.PlaceShown</c>) and when (seconds of play).
/// </summary>
public sealed class FallenPokemon
{
    public SavedPokemonData Pokemon { get; set; } = new();
    public string PlaceKey { get; set; } = "";
    public string Place { get; set; } = "";
    public float PlayTime { get; set; }
}
