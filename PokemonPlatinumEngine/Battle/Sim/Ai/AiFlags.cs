using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Battle.Sim.Ai;

/// <summary>
/// The routines of Platinum's AI script a trainer thinks with (<c>AI_FLAG_*</c>, from each trainer's data), run in
/// the order of their bits. The game's trainers use <see cref="Basic"/>, <see cref="EvalAttack"/>,
/// <see cref="Expert"/>, <see cref="SetupFirstTurn"/>, <see cref="Risky"/> and <see cref="PrioritizeExtremes"/>; a
/// double battle adds <see cref="TagStrategy"/>; a roaming Pokémon thinks with <see cref="Roaming"/> alone.
/// </summary>
[Flags]
public enum AiFlags : uint
{
    None = 0,
    /// <summary>Stay away from moves that would fail or do nothing.</summary>
    Basic = 1u << 0,
    /// <summary>Prefer the strongest hit, and a knockout.</summary>
    EvalAttack = 1u << 1,
    /// <summary>The move-by-move judgement of when each effect is worth using.</summary>
    Expert = 1u << 2,
    /// <summary>Set up on the battle's first turn.</summary>
    SetupFirstTurn = 1u << 3,
    /// <summary>Take chances on moves that might go wrong.</summary>
    Risky = 1u << 4,
    /// <summary>Like moves of variable or fixed damage, and status moves.</summary>
    PrioritizeExtremes = 1u << 5,
    /// <summary>Raise stats to pass them on.</summary>
    BatonPass = 1u << 6,
    /// <summary>Work with a partner in a double battle (added by the battle, not by the trainer).</summary>
    TagStrategy = 1u << 7,
    /// <summary>Choose moves by how much HP either side has left.</summary>
    CheckHp = 1u << 8,
    /// <summary>Set the weather on the first turn.</summary>
    Weather = 1u << 9,
    /// <summary>Like moves that get in the way.</summary>
    Harassment = 1u << 10,
    /// <summary>A roaming Pokémon: it runs unless it is held.</summary>
    Roaming = 1u << 29,
    /// <summary>A Pokémon of the Great Marsh (unused by the original's routine beyond running).</summary>
    Safari = 1u << 30,
    /// <summary>The helper of the catching lesson: it stops attacking once the wild Pokémon is weak.</summary>
    CatchTutorial = 1u << 31
}

public static class AiFlagNames
{
    /// <summary>Reads a trainer's flags by the names the data files give them (<c>Basic</c>, <c>EvalAttack</c>…).</summary>
    public static AiFlags Parse(IEnumerable<string>? names) =>
        names?.Aggregate(AiFlags.None, (all, n) => all | Enum.Parse<AiFlags>(n, ignoreCase: true)) ?? AiFlags.None;
}
