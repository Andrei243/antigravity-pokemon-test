using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Models;

/// <summary>Whether a wild Pokémon met under the Nuzlocke may be caught (plan 13 · V4).</summary>
public enum NuzlockeCatch
{
    /// <summary>The place's chance: a ball may be thrown.</summary>
    Free,
    /// <summary>The place's one Pokémon was met before: no ball.</summary>
    AreaSpent,
    /// <summary>The dupes clause: a line already caught passes, the place's chance kept for the next. No ball.</summary>
    Duplicate
}

/// <summary>
/// What a battle is told of the Nuzlocke (plan 13 · V4; <see cref="Battle.Sim.CoreSetup.Nuzlocke"/>): whether its wild
/// Pokémon may be caught, and that no revive is used. Null for a run without it, which changes nothing.
/// </summary>
public sealed record NuzlockeTerms(NuzlockeCatch Catch = NuzlockeCatch.Free);

/// <summary>
/// The Nuzlocke's rules (plan 13 · V4; <c>docs/mechanics/rulings.md</c>, "Ways to play"), GPU-free: one Pokémon may be
/// caught in each place, the first met there; a Pokémon that faints is gone for good, into the run's graveyard; no
/// revive is ever used. The clauses: a line already caught passes (dupes), a shiny may always be caught (shiny).
/// </summary>
public static class Nuzlocke
{
    /// <summary>The place a wild Pokémon is met in, as the run counts places: the area's key on a streamed map (each of the Great Marsh's six its own), the map's name otherwise.</summary>
    public static string PlaceKeyOf(Map map, int x, int y) => map.AreaAt(x, y)?.Key is { Length: > 0 } key ? key : map.Name;

    /// <summary>A species' line, as the dupes clause counts it: the first of its line (its <see cref="PokemonSpecies.Offspring"/>), the species itself for none.</summary>
    public static string LineOf(PokemonSpecies species) => species.Offspring ?? species.Name;

    /// <summary>Whether a Pokémon of the species' line has been caught (the Pokédex's caught species).</summary>
    public static bool LineCaught(PokemonSpecies species, Pokedex dex)
    {
        string line = LineOf(species);
        return dex.CaughtSpecies.Any(n => PokemonDatabase.GetByDex(n) is { } caught && LineOf(caught) == line);
    }

    /// <summary>
    /// A wild Pokémon met in the field at a place: whether it may be caught, and the place spent when it is its first.
    /// Under a run without the Nuzlocke always <see cref="NuzlockeCatch.Free"/>, and nothing is written. A shiny under
    /// the shiny clause is free and spends nothing; a line already caught under the dupes clause passes and spends
    /// nothing; anything else met where one was met before is refused.
    /// </summary>
    public static NuzlockeCatch Meet(RunRules rules, RunState state, string place, Pokemon wild, Pokedex dex)
    {
        if (!rules.Nuzlocke) return NuzlockeCatch.Free;
        if (rules.ShinyClause && wild.IsShiny) return NuzlockeCatch.Free;
        if (state.HasMet(place)) return NuzlockeCatch.AreaSpent;
        if (rules.DupesClause && LineCaught(wild.Species, dex)) return NuzlockeCatch.Duplicate;
        state.Meet(place);
        return NuzlockeCatch.Free;
    }

    /// <summary>
    /// A Pokémon given to the player (plan 13 · V4's gifts clause, <see cref="RunRules.GiftsCount"/>): under a Nuzlocke with
    /// the clause on it spends the place's chance, as a wild Pokémon met there would have, so a later one met in the same
    /// place can't be caught. Nothing under any other run, which writes nothing.
    /// </summary>
    public static void Gift(RunRules rules, RunState state, string place)
    {
        if (rules.Nuzlocke && rules.GiftsCount) state.Meet(place);
    }

    /// <summary>
    /// A wild Pokémon a script puts in the player's way, a legend or a sleeping Snorlax (the static encounters): under the
    /// gifts clause it is the place's one chance, like any wild Pokémon met there (<see cref="Meet"/>); without it, or under any
    /// other run, it is free to catch and spends nothing.
    /// </summary>
    public static NuzlockeCatch MeetStatic(RunRules rules, RunState state, string place, Pokemon wild, Pokedex dex) =>
        rules.Nuzlocke && rules.GiftsCount ? Meet(rules, state, place, wild, dex) : NuzlockeCatch.Free;

    /// <summary>Whether an item brings a fainted Pokémon round (a Revive, a Max Revive, a Revival Herb, Sacred Ash).</summary>
    public static bool Revives(ItemData item) => item.Use?.ContainsKey("revive") == true || item.Use?.ContainsKey("reviveAll") == true;

    /// <summary>
    /// The team's fallen after a battle go into the graveyard, out of the team for good (an Egg, which counts as fainted,
    /// stays). Returns them, in the team's order.
    /// </summary>
    public static List<Pokemon> Bury(RunState state, Party party, string placeKey, string place, float playTime)
    {
        var fallen = party.Members.Where(p => p.IsFainted && !p.IsEgg).ToList();
        foreach (var p in fallen)
        {
            party.Remove(p);
            state.Graveyard.Add(new FallenPokemon { Pokemon = SavedPokemonData.FromPokemon(p), PlaceKey = placeKey, Place = place, PlayTime = playTime });
        }
        return fallen;
    }
}
