namespace PokemonPlatinumEngine.Battle;

/// <summary>
/// What a move's card says of a foe (plan 12 · Q10): nothing (the option is off, the move is a status move, the
/// species is new, the place is empty), or how the move's type meets the foe's as the battle stands.
/// </summary>
public enum MoveHint { None, Neutral, SuperEffective, NotVeryEffective, NoEffect }

public static class MoveHints
{
    /// <summary>The hint for a matchup (<see cref="DamageCalculator.Effectiveness"/>'s 0, ¼, ½, 1, 2 or 4).</summary>
    public static MoveHint Of(float effectiveness) => effectiveness switch
    {
        0f => MoveHint.NoEffect,
        < 1f => MoveHint.NotVeryEffective,
        > 1f => MoveHint.SuperEffective,
        _ => MoveHint.Neutral
    };

    /// <summary>The words on the pill, or null for a hint that shows nothing.</summary>
    public static string? Words(MoveHint hint) => hint switch
    {
        MoveHint.SuperEffective => "SUPER EFFECTIVE",
        MoveHint.NotVeryEffective => "NOT VERY EFFECTIVE",
        MoveHint.NoEffect => "NO EFFECT",
        _ => null
    };
}
