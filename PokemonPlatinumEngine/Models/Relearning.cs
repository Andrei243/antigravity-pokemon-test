using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// Remembering a move (plan 12 · Q19; no drawing or input): the level-up moves a Pokémon could have by now and
/// doesn't know, which the party menu's REMEMBER lists and teaches for nothing, and which the Move Relearner of
/// plan 06's "Growth" row asks for a Heart Scale when it is built.
/// </summary>
public static class Relearning
{
    /// <summary>
    /// The moves in its <see cref="Pokemon.Learnset"/> at or below its level that it doesn't know, each once, in
    /// level order (the order the learnset gives within a level). A move learned on evolving (level 0) is
    /// remembered too, from the start of the list.
    /// </summary>
    public static IReadOnlyList<string> MovesFor(Pokemon pokemon)
    {
        if (pokemon.IsEgg) return Array.Empty<string>();
        return pokemon.Learnset
            .Where(m => m.Level <= pokemon.Level)
            .Select((m, i) => (m, i))
            .OrderBy(t => t.m.Level).ThenBy(t => t.i)
            .Select(t => t.m.MoveName)
            .Where(name => !pokemon.Knows(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Teaches one of them: into a free place, or in place of the move at <paramref name="forget"/> (0 to 3) when it
    /// knows four. False, changing nothing, when the move isn't one it may remember or no place is named for a full set.
    /// </summary>
    public static bool Teach(Pokemon pokemon, string move, int forget = -1)
    {
        if (!MovesFor(pokemon).Contains(move, StringComparer.OrdinalIgnoreCase)) return false;
        if (pokemon.TryLearn(move)) return true;
        if (forget < 0 || forget >= pokemon.Moves.Count) return false;
        pokemon.ReplaceMove(forget, move);
        return true;
    }
}
