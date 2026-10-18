using System.Collections.Generic;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

// The later held items' bonuses in the damage formula (plan 06 · R28a), by hold effect, each in the chain Pokémon
// Showdown puts it in (data/items.ts at the pinned commit). The modern formula alone reads them, as it does the later
// abilities': a Pokémon of Platinum's rules holds none of them.
public static partial class DamageCalculator
{
    /// <summary>The power's (onBasePower): a Punching Glove's tenth for a punch, a Gem that went off for the move.</summary>
    private static void LaterItemPower(List<int> bp, Battler attacker, Move move, string? myHold)
    {
        if (myHold == "PunchingGlove" && (move.Data.Flags & MoveFlags.Punch) != 0) bp.Add(4506);
        if (attacker.Turn.GemBoosted) bp.Add(5325);
        // Ogerpon's mask: a fifth more for the Ogerpon whose mask it is (plan 06 · R28b; Showdown's onBasePower)
        if (myHold != null && myHold.StartsWith("Mask", System.StringComparison.Ordinal)
            && attacker.Pokemon!.Form?.StartsWith("Ogerpon-" + myHold["Mask".Length..], System.StringComparison.Ordinal) == true) bp.Add(4915);
    }

    /// <summary>The defence's (onModifyDef, onModifySpD): an Eviolite on a Pokémon that can still evolve, an Assault Vest against a special move.</summary>
    private static void LaterItemDefense(List<int> df, Battler defender, string? theirHold, bool defensePhysical)
    {
        if (theirHold == "Eviolite" && CanStillEvolve(defender.Pokemon!)) df.Add(6144);
        if (theirHold == "AssaultVest" && !defensePhysical) df.Add(6144);
    }

    /// <summary>Eviolite's "not fully evolved" (Showdown's <c>nfe</c>): its species evolves into another, whatever the way.</summary>
    internal static bool CanStillEvolve(Pokemon p) => p.Species.Evolutions is { Count: > 0 };

    /// <summary>A Utility Umbrella keeps the sun and the rain's say on the hits its holder takes (sunnyday's and raindance's <c>onWeatherModifyDamage</c>).</summary>
    private static bool UnderAnUmbrella(Battler defender) => BattleEffects.HoldEffectOf(defender) == "UtilityUmbrella";
}
