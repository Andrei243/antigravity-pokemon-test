using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// Affection's perks in battle (plan 10 · F7; docs/mechanics/rulings.md, "Affection's perks"): Generation 6 kept a
/// second count beside friendship and gave a Pokémon that loved its trainer five small favours in battle; from
/// Generation 8 the same favours are read from friendship, and so are they here, where affection was folded into
/// friendship already (rulings, "Friendship"). Two steps: <see cref="Affectionate"/> from friendship 220, the level an
/// evolution by affection asks, and <see cref="Devoted"/> at the most, 255. Only the player's own Pokémon have them,
/// never a partner's, never in the Battle Frontier, and only under rules that say so
/// (<see cref="Ruleset.AffectionPerks"/>), so no battle of Platinum's rules moves. Each chance is rolled on the
/// battle's generator (<see cref="Battle.Sim.RollKind.Affection"/>), and only where the Pokémon has a perk to give, so
/// a battle where nobody does draws exactly what it drew.
/// </summary>
public static class Affection
{
    /// <summary>The friendship from which a Pokémon's affection shows in battle (the evolutions' 220).</summary>
    public const int Affectionate = FriendshipRules.EvolveAt;

    /// <summary>The friendship at which every perk is at its strongest.</summary>
    public const int Devoted = FriendshipRules.Max;

    /// <summary>0 for none, 1 from <see cref="Affectionate"/>, 2 at <see cref="Devoted"/>. An Egg has none.</summary>
    public static int Level(Pokemon p) =>
        p.IsEgg ? 0 : p.Friendship >= Devoted ? 2 : p.Friendship >= Affectionate ? 1 : 0;

    /// <summary>The level a battler's affection counts for in this battle: none for a foe, a partner's, in the Frontier or under rules without the perks.</summary>
    public static int In(Battler b, Ruleset rules) =>
        rules.AffectionPerks && b.IsPlayerSide && b.Trainer == null && b.Field?.Frontier != true && b.Pokemon is { } p ? Level(p) : 0;

    /// <summary>Its EXP, a fifth again (Generation 6's 1.2×), for a Pokémon of the player's with any affection.</summary>
    public const int ExpPercent = 120;

    /// <summary>The chance in a hundred, by level, that a hit that would knock it out leaves it 1 HP.</summary>
    public static int EndureChance(int level) => level switch { 2 => 25, 1 => 10, _ => 0 };

    /// <summary>The chance in a hundred, by level, that it shakes off a status condition at a turn's end.</summary>
    public static int ShakeOffChance(int level) => level switch { 2 => 20, 1 => 10, _ => 0 };

    /// <summary>The chance in a hundred that it dodges a move that would have hit: only at <see cref="Devoted"/>.</summary>
    public static int DodgeChance(int level) => level == 2 ? 10 : 0;

    /// <summary>The stages it adds to its own chance of a critical hit: one, only at <see cref="Devoted"/>.</summary>
    public static int CritStages(int level) => level == 2 ? 1 : 0;
}
