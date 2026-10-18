using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

// What Terastallization changes of a hit (plan 06 · R23; docs/mechanics/rulings.md, "Terastallization"), by Pokémon
// Showdown's sim/battle-actions.ts at the pinned commit: modifyDamage's same-type bonus (the Tera Type's and the own
// types', two where they meet, Stellar's one boost a type) and pokemon.ts' runEffectiveness (a Stellar move against a
// Terastallized target), with Tera Shell's onEffectiveness. The power of 60 is getDamage's, which the core works out
// before it asks for the hit (BattleCore.TeraPower), since only it knows a move of several hits.
public static partial class DamageCalculator
{
    /// <summary>The Pokémon has taken a Tera Type of the chart: its type for defence is that alone.</summary>
    private static bool TeraTyped(Battler b) => b.Pokemon?.Terastallized is { IsStellar: false };

    /// <summary>
    /// A move of the Stellar type (a Stellar Tera Blast, Terapagos's Tera Starstorm): no type stands in its way and
    /// none resists it, and it is super effective against a Terastallized Pokémon (Showdown's <c>runEffectiveness</c>).
    /// </summary>
    private static float StellarEffectiveness(Battler defender) => defender.Pokemon?.Terastallized != null ? 2f : 1f;

    /// <summary>
    /// Tera Shell (Terapagos in its Terastal Form): a damaging move at full HP is not very effective, whatever its type
    /// would do, unless the type can't touch it at all (Showdown's <c>terashell</c> <c>onEffectiveness</c>); Mold
    /// Breaker and its kin go through it.
    /// </summary>
    private static float TeraShell(Battler attacker, Battler defender, Move move, float total)
    {
        if (total == 0f || move.Category == MoveCategory.Status || move.Data == Sim.BattleCore.StruggleData) return total;
        if (defender.Ability?.Name != "Tera Shell" || defender.Pokemon is not { } p || p.CurrentHP < p.MaxHP || attacker == defender) return total;
        if (LaterItems.Breaks(BattleEffects.Of(attacker).Any(e => e.IgnoresTargetAbility) || IgnoresAbilities(move.Data), defender)) return total;
        return 0.5f;
    }

    /// <summary>
    /// The same-type bonus of a Terastallized user, in 4096ths, and whether it counts as the same type (Showdown's
    /// <c>modifyDamage</c>); null for a user that hasn't Terastallized, whose bonus is the formula's as ever.
    /// <list type="bullet">
    /// <item>The bonus is half again for a move of its Tera Type or of one of its own types, and twice where its Tera
    /// Type is one of its own; Adaptability makes it twice, or 2.25 times, but only for its Tera Type.</item>
    /// <item>Stellar: its own types keep their half, and the first use of each type it has is twice for one of its own
    /// types and 1.2 times for another (every use for a Terapagos); no Adaptability.</item>
    /// </list>
    /// </summary>
    private static (int Stab, bool IsStab)? TeraStab(Battler attacker, Move move, PokemonType type, List<BattleEffect> attackerEffects)
    {
        if (attacker.Pokemon is not { Terastallized: { } tera } p) return null;
        bool stellarMove = move.Data.Stellar;
        bool isStab = !stellarMove && (move.Data.ForcesStab || attacker.HasType(type) || attacker.HasTypeBeforeTera(type));
        int stab = isStab ? 6144 : 4096;
        if (tera.IsStellar)
        {
            if (p.StellarThisUse) stab = isStab ? 8192 : 4915;
            return (stab, isStab);
        }
        if (tera.Type == type && attacker.HasTypeBeforeTera(type)) stab = 8192;
        if (attackerEffects.Any(e => e.StabOverride.HasValue) && (move.Data.ForcesStab || attacker.HasType(type)))
            stab = stab == 8192 ? 9216 : 8192;
        return (stab, isStab);
    }
}
