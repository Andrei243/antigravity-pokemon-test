using System.Collections.Generic;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Battle.Sim;

// A condition that lasts on a Pokémon (plan 09 · L2): how a line of the rules comes to tell whoever shows the battle
// that a confusion or a love begins or ends, and what a Pokémon brings with it as it takes its place. Nothing here
// changes a line, a roll or anything a turn does: these are events more on lines that are said anyway. The six
// status conditions need nothing new: every line that gives or cures one carries its StatusChanged already.
public sealed partial class BattleCore
{
    /// <summary>
    /// The passing conditions of the Pokémon in a place as they stand now, for the line that tells of the change:
    /// <c>Say(...).With(Passing(b))</c>, after the change is made.
    /// </summary>
    private static VolatileChanged Passing(Battler b) => new(b.Place, b.IsConfused, b.Volatile.InLoveWith != null);

    /// <summary>
    /// The next line said tells that this Pokémon's confusion or love has changed
    /// (<see cref="Effects.IBattleContext.PassingChanged"/>): for a line said elsewhere, by an item's own effect. It
    /// rides as what a line names does (<see cref="ItemOf"/>), and is forgotten with a line that goes unsaid.
    /// </summary>
    public void PassingChanged(Battler b) => named.Add(Passing(b));

    /// <summary>
    /// What a Pokémon brings as it takes its place: the status condition it already had, and a confusion handed on
    /// to it by Baton Pass. The events for the line that sends it out, or to stand beside a wild Pokémon's entry.
    /// </summary>
    private static IEnumerable<BattleEvent> Brought(Battler place)
    {
        if (place.Pokemon is not { } p) yield break;
        if (p.Status is not (StatusCondition.None or StatusCondition.Faint)) yield return new CameInWith(place.Place, p.Status);
        if (place.IsConfused || place.Volatile.InLoveWith != null) yield return Passing(place);
    }

    /// <summary>The line that sends a Pokémon out tells what it brings (<see cref="Brought"/>).</summary>
    private static Said Bringing(Said line, Battler place)
    {
        foreach (var brought in Brought(place)) line.With(brought);
        return line;
    }
}
