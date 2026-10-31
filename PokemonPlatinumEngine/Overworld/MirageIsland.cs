using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// Mirage Island (plan 21 · H12, part h; <c>docs/mechanics/rulings.md</c>, "Mirage Island"): Emerald's
/// <c>IsMirageIslandPresent</c>. The island rises off Route 130 on a day whose number matches the low half of the
/// personality of a Pokémon of the team, an Egg included, as Emerald compares its <c>VAR_MIRAGE_RND_H</c> with every
/// party slot that holds a species. Emerald keeps a number of its own that moves on once a day; here the day's number
/// is the one the game already keeps for its other daily draws (<see cref="SpecialEncounters.DailyNumber"/>), its upper
/// half, so nothing new is saved. GPU-free.
/// </summary>
public static class MirageIsland
{
    /// <summary>The story's flag (<i>ours</i>) that lays Route 130's island layout (<c>world.json</c>'s <c>swaps</c>).</summary>
    public const string Flag = "FLAG_HOENN_MIRAGE_ISLAND";

    /// <summary>Emerald's flag that hides the island's berry tree while the island is under the sea.</summary>
    public const string BerryTreeFlag = "FLAG_TEMP_HIDE_MIRAGE_ISLAND_BERRY_TREE";

    /// <summary>The day's number the island is looked for by (Emerald's <c>GetMirageRnd() &gt;&gt; 16</c>).</summary>
    public static int NumberOf(SpecialEncounters encounters) => (int)(encounters.DailyNumber >> 16);

    /// <summary>Whether a Pokémon's personality is the day's: the low half of it equal to the number.</summary>
    public static bool Matches(Pokemon pokemon, int number) => (pokemon.Personality & 0xFFFF) == (uint)number;

    /// <summary>Whether the island stands today for this team (<c>IsMirageIslandPresent</c>).</summary>
    public static bool Present(Party party, SpecialEncounters encounters)
    {
        int number = NumberOf(encounters);
        return party.Members.Any(p => p != null && Matches(p, number));
    }
}
