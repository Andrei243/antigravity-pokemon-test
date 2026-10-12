namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>
/// The formats Platinum never had (plan 14; <c>docs/mechanics/rulings.md</c>, "Formats of the later games"), each a
/// flag or a shape the battle is handed as it is made and that nothing changes while it runs. B1: an inverse battle.
/// </summary>
public sealed partial class BattleCore
{
    /// <summary>What an inverse battle says as it opens: a line of our own, since the later games' say nothing.</summary>
    public const string InverseLine = "Every type matchup is turned upside down!";

    /// <summary>
    /// An inverse battle (<see cref="BattleConditions.Inverse"/>): the type chart is turned round from the first
    /// turn to the last. The damage formula, the lines about how a hit did, the immunities of types, Stealth Rock,
    /// Conversion 2, the berries against a type, Anticipation, Wonder Guard and the trainer AI all ask the same
    /// chart (<see cref="Data.TypeChart.GetEffectiveness"/>, through <see cref="FieldState.Inverse"/>), so none has
    /// a rule of its own for it. What an ability or a move's own magnetism stops is still stopped: Levitate and
    /// Magnet Rise keep Ground moves off, as in X and Y.
    /// </summary>
    public bool Inverse => Conditions.Inverse;

    /// <summary>The battle says what format it is once everyone is out, before the weather of the place.</summary>
    private void AnnounceFormat()
    {
        if (Inverse) Say(InverseLine);
    }
}
