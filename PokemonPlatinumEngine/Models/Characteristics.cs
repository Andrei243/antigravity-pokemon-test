using System;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// A Pokémon's characteristic (plan 08 · P3): a line about the stat its highest IV is in, which the summary's memo
/// shows. How it is decided is the original's: the stats are looked at in the order HP, Attack, Defense, Speed,
/// Sp. Atk, Sp. Def, starting at the one the personality value gives (its remainder by six), and the first to hold the
/// highest IV wins; the line is chosen by that IV's remainder by five. The thirty lines are our own
/// (<c>summary.characteristic.&lt;stat&gt;.&lt;n&gt;</c> in <c>ui.json</c>).
/// </summary>
public static class Characteristics
{
    /// <summary>The stats in the order the original looks through them.</summary>
    public static readonly StatType[] Order =
    {
        StatType.HP, StatType.Attack, StatType.Defense, StatType.Speed, StatType.SpAttack, StatType.SpDefense
    };

    /// <summary>The stat and the line number (0 to 4) of a Pokémon's characteristic.</summary>
    public static (StatType Stat, int Line) Of(Pokemon pokemon)
    {
        int[] ivs =
        {
            pokemon.IvHP, pokemon.IvAttack, pokemon.IvDefense, pokemon.IvSpeed, pokemon.IvSpAttack, pokemon.IvSpDefense
        };
        int start = (int)(pokemon.Personality % 6);
        int best = -1, at = start;
        for (int i = 0; i < 6; i++)
        {
            int k = (start + i) % 6;
            if (ivs[k] > best) { best = ivs[k]; at = k; }
        }
        return (Order[at], best % 5);
    }

    /// <summary>The characteristic's line, in the language chosen.</summary>
    public static string Line(Pokemon pokemon)
    {
        var (stat, line) = Of(pokemon);
        return Core.Text.Of($"summary.characteristic.{stat}.{line}");
    }
}
