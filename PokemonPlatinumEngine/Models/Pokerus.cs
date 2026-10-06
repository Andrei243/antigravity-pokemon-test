using System;
using System.Linq;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// Pokérus as Platinum spreads it (plan 06 · R10; <c>src/pokemon.c</c>). It is kept in one byte on the Pokémon
/// (<see cref="Pokemon.Pokerus"/>): the strain in the high four bits, the days it has left in the low four. After
/// every battle there are three chances in 65,536 that a Pokémon of the team catches it, and one chance in three
/// that each one carrying it passes it to its neighbours in the party. Each day that passes takes a day off; with
/// none left it is cured, and a cured Pokémon can't catch it again. Caught or cured, it doubles the effort its
/// Pokémon gains (<see cref="EffortRules.Gain"/>). No drawing or input.
/// </summary>
public static class PokerusRules
{
    public static bool Infected(Pokemon p) => (p.Pokerus & 0xF) != 0;

    /// <summary>It had Pokérus and is over it.</summary>
    public static bool Cured(Pokemon p) => (p.Pokerus & 0xF) == 0 && (p.Pokerus & 0xF0) != 0;

    /// <summary>
    /// <c>Pokemon_ApplyPokerus</c>, after a battle: a draw of 65,536 that comes up 16,384, 32,768 or 49,152 gives
    /// one Pokémon of the team, drawn at random, a strain of its own, unless that one has had it already.
    /// </summary>
    /// <param name="random">The original's generator: each <c>LCRNG_Next</c> is a draw of 65,536.</param>
    public static void TryInfect(Party party, Random random)
    {
        int count = party.Count;
        if (count == 0) return;
        int draw = random.Next(65536);
        if (draw is not (16384 or 32768 or 49152)) return;

        var target = party.Members[random.Next(65536) % count];
        if (target.Pokerus != 0) return;

        int strain;
        do strain = random.Next(65536) & 0xFF;
        while ((strain & 0x7) == 0);
        if ((strain & 0xF0) != 0) strain &= 0x7;
        strain |= strain << 4;
        strain &= 0xF3;
        strain++;
        target.Pokerus = strain;
    }

    /// <summary>
    /// <c>Pokemon_ValidatePokerus</c>, after a battle: one time in three, each Pokémon carrying it gives its own
    /// byte to the one before it and the one after it that never had it (passing the one after over, as the
    /// original does).
    /// </summary>
    public static void Spread(Party party, Random random)
    {
        if (random.Next(65536) % 3 != 0) return;
        var members = party.Members;
        for (int i = 0; i < members.Count; i++)
        {
            int carried = members[i].Pokerus;
            if ((carried & 0xF) == 0) continue;
            if (i > 0 && (members[i - 1].Pokerus & 0xF0) == 0) members[i - 1].Pokerus = carried;
            if (i < members.Count - 1 && (members[i + 1].Pokerus & 0xF0) == 0)
            {
                members[i + 1].Pokerus = carried;
                i++;
            }
        }
    }

    /// <summary>
    /// <c>Party_UpdatePokerusStatus</c>, when days have passed: each carrier loses that many days; more days than
    /// it has left, or more than four, cures it. A byte that would come to nothing keeps a strain, so the cure is
    /// remembered.
    /// </summary>
    public static void DaysPass(Party party, int days)
    {
        if (days <= 0) return;
        foreach (var p in party.Members)
        {
            int value = p.Pokerus;
            if ((value & 0xF) == 0) continue;
            if ((value & 0xF) < days || days > 4) value &= 0xF0;
            else value -= days;
            if (value == 0) value = 0x10;
            p.Pokerus = value;
        }
    }

    /// <summary>What the team's screens say of it: PKRS while it is carried, nothing once over it (the original marks a cured one with a face).</summary>
    public static string? Tag(Pokemon p) => Infected(p) ? "PKRS" : null;

    /// <summary>Whether anyone in the party carries it now (the nurse's script asks it).</summary>
    public static bool AnyInfected(Party party) => party.Members.Any(Infected);
}
