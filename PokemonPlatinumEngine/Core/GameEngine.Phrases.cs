using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Phrases (plan 08 · P9): a script's <c>phrase</c> opens the picker over the field on the words this player has (the
/// Pokémon seen, the team's moves, the types and our own lists), and what is chosen is kept in the story under the
/// script's name (<see cref="Story.StoryState.Phrases"/>), where Jubilife TV reads the interview back.
/// </summary>
public partial class GameEngine
{
    /// <summary>The words a phrase can be made of now: the Pokémon seen in National Pokédex order and the moves the team knows.</summary>
    internal PhraseGroups PhraseGroupsNow()
    {
        var seen = flow.Pokedex.SeenSpecies.OrderBy(n => n)
            .Select(n => PokemonDatabase.GetByDex(n)?.Name)
            .Where(name => name != null)
            .Select(name => name!);
        var moves = flow.Party.Members.Where(p => !p.IsEgg)
            .SelectMany(p => p.Moves)
            .Select(m => m.Data.Name);
        return new PhraseGroups(seen, moves);
    }

    /// <summary>The picker, open on the phrase kept under <paramref name="name"/>; the script waits on it.</summary>
    internal void OpenPhrases(string name)
    {
        flow.PhraseScreen.Open(name, PhraseGroupsNow(), flow.Story.PhraseOf(name));
        flow.State = GameState.Phrases;
    }

    /// <summary>The picker closed: a phrase given is kept and the script hears 1, one backed out of leaves the phrase as it was and the script hears 0.</summary>
    private void ClosePhrases()
    {
        if (flow.PhraseScreen.Outcome is IReadOnlyList<string> phrase && PhraseGroups.IsWhole(phrase))
        {
            flow.Story.SetPhrase(flow.PhraseScreen.Name, phrase);
            flow.ScriptAnswer = 1;
        }
        else flow.ScriptAnswer = 0;
        flow.State = GameState.Overworld;
    }
}
