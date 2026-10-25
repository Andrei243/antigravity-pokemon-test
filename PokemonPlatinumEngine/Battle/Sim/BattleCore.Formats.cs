using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>
/// The formats Platinum never had (plan 14; <c>docs/mechanics/rulings.md</c>, "Formats of the later games"), each a
/// flag or a shape the battle is handed as it is made and that nothing changes while it runs. B1: an inverse battle.
/// B3: a horde, one against five.
/// </summary>
public sealed partial class BattleCore
{
    /// <summary>What an inverse battle says as it opens: a line of our own, since the later games' say nothing.</summary>
    public static string InverseLine => Text.Of("battle.everyTypeMatchupIs");

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

    /// <summary>
    /// A horde (plan 14 · B3; <see cref="BattleFormat.Horde"/>): the player's one Pokémon against up to five wild ones,
    /// as X and Y have it, under rules that have hordes (<see cref="Data.Ruleset.Hordes"/>). Each wild Pokémon chooses
    /// for itself (<see cref="TrainerAi"/>: a move at random, at a foe drawn for it), a move that hits every foe hits
    /// all five and each takes three quarters (the spread hit every format has), a ball can't be aimed until one is left,
    /// each foe leaves its EXP as it faints, and running is measured against the fastest of them. What a wild double
    /// battle refuses (Roar, Whirlwind, Teleport, Dragon Tail and Circle Throw driving one off, Emergency Exit's flight) a
    /// horde refuses too: none of them ends a battle with others still on the field.
    /// </summary>
    public bool IsHorde => Format == BattleFormat.Horde;

    /// <summary>What a horde says as it comes out: the species when all of them are one, no names when they aren't.</summary>
    private static string HordeAppears(IReadOnlyList<Battler> foes) =>
        foes.Select(b => b.Pokemon!.Species.Name).Distinct().Count() == 1
            ? Text.Of("battle.aWildHorde", ("pokemon", foes[0].Pokemon!.DisplayName))
            : Text.Of("battle.aMixedHorde");

    /// <summary>
    /// Who a run from a wild battle is measured against: in a horde the fastest of the wild Pokémon still standing (plan
    /// 14 · B3; the first of them where two are as fast), anywhere else the first of them, as Platinum has it.
    /// </summary>
    private Battler RunAgainst()
    {
        var standing = EnemySlots.Where(b => b.IsActive).ToList();
        if (IsHorde && standing.Count > 1) return standing.OrderByDescending(b => b.Pokemon!.Speed).ThenBy(b => b.Slot).First();
        return standing.FirstOrDefault() ?? EnemySlots[0];
    }

    /// <summary>Why a ball can't be aimed while more than one wild Pokémon stands: two of them, or a horde's more.</summary>
    private static string NoAim(int standing) => standing > 2 ? Text.Of("battle.thereAreTooManyPokemon") : Text.Of("battle.thereAreTwoPokemon");
}
