using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>
/// Sky battles (plan 14 · B2, X and Y's; <c>docs/mechanics/rulings.md</c>, "Sky battles"): a flag of the battle's
/// conditions (<see cref="BattleConditions.InTheAir"/>), never a format, so a sky battle is the single (or the double) it
/// would have been. Who takes part is held at the door: the battle is made with the teams of Pokémon that fly or float
/// alone (<see cref="SkyBattles.CanFly"/>), which the game hands it (<see cref="SkyBattles.Fliers"/>), and refuses to
/// begin with any other; and the moves that need the ground are refused in the one place a move is refused
/// (<see cref="WhyNotMove"/>), so the menus, the trainer AI and a move called by another all go by it.
/// </summary>
public sealed partial class BattleCore
{
    /// <summary>A sky battle (<see cref="BattleConditions.InTheAir"/>).</summary>
    public bool InTheAir => Conditions.InTheAir;

    /// <summary>
    /// A sky battle's teams are of Pokémon that may take part, the player's, a partner's and every trainer's (a wild
    /// Pokémon met in the air too): checked as the battle is made, since nothing outside the battle may hand it one
    /// that can't fly. Throws for one that can't.
    /// </summary>
    private void CheckTheSky()
    {
        if (!InTheAir) return;
        var everyone = PlayerParty.Members
            .Concat(Partner?.Party.Members ?? Enumerable.Empty<Pokemon>())
            .Concat(Trainers.SelectMany(t => t.Party.Members))
            .Concat(WildPokemon);
        if (everyone.FirstOrDefault(p => !SkyBattles.CanFly(p)) is { } grounded)
            throw new ArgumentException($"{grounded.Species.Name} can't take part in a sky battle: only Pokémon that fly or float may (plan 14 · B2)");
    }

    /// <summary>Why a move can't be used in a sky battle (null when it can, or outside one).</summary>
    private string? NotInTheAir(Battler mine, Move move) =>
        InTheAir && SkyBattles.Grounded(move.Data) ? Text.Of("battle.cantUseInTheAir", ("pokemon", mine.Name), ("move", move.DisplayName)) : null;

    /// <summary>What a sky battle says as it opens: a line of our own.</summary>
    public static string SkyLine => Text.Of("battle.skyBattle");
}
