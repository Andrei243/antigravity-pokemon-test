using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>
/// Affection's perks (plan 10 · F7; <see cref="Affection"/>; docs/mechanics/rulings.md, "Affection's perks"): the two
/// that are the core's own rules, beside the dodge in <c>Hits</c>, the critical stage in
/// <see cref="DamageCalculator.RollsCritical"/> and the EXP in <c>Give</c>. Each rolls only where the Pokémon has a
/// perk to give, so a battle where none has draws what it always drew.
/// </summary>
public sealed partial class BattleCore
{
    /// <summary>A hit that would knock it out leaves it 1 HP instead, by its affection's chance; said as it happens.</summary>
    private bool HoldsOnForTheTrainer(Battler b)
    {
        int chance = Affection.EndureChance(Affection.In(b, Rules));
        if (chance == 0 || rng.Roll(RollKind.Affection, 100) >= chance) return false;
        Announce(Text.Of("battle.affectionHeldOn", ("pokemon", b.Name)));
        return true;
    }

    /// <summary>At a turn's end, by its affection's chance, it shakes off the status condition it has.</summary>
    private void ShakesOffForTheTrainer(Battler b)
    {
        var p = b.Pokemon!;
        if (p.Status is StatusCondition.None or StatusCondition.Faint) return;
        int chance = Affection.ShakeOffChance(Affection.In(b, Rules));
        if (chance == 0 || rng.Roll(RollKind.Affection, 100) >= chance) return;
        CureStatus(b, Text.Of("battle.affectionShookOff", ("pokemon", b.Name), ("condition", BattleText.StatusName(p.Status))));
    }

    /// <summary>
    /// The name a Pokémon of the player's own is sent out under (plan 10 · F9): with the title its trainer chose
    /// ("Go! Starly the Sleepy!"), unless an Illusion hides it. Every other line names it by <see cref="Battler.Name"/>.
    /// </summary>
    private static string GoesOutAs(Battler place, Pokemon incoming) =>
        IllusionFor(place, incoming) ?? EncounterMarks.SentOutAs(incoming, incoming.DisplayName);

    /// <summary>A fifth more EXP for a Pokémon of the player's with any affection, outside the Frontier (which gives none).</summary>
    private int WithAffection(Pokemon p, int exp) =>
        Rules.AffectionPerks && !Frontier && Affection.Level(p) > 0 ? exp * Affection.ExpPercent / 100 : exp;
}
