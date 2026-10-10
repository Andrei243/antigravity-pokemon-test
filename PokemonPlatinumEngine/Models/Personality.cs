using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// What a Pokémon's personality value decides (plan 06 · R15), as the original's <c>src/pokemon.c</c> reads it: the
/// nature is its remainder by 25 (<c>Pokemon_GetNatureOf</c>), the gender its lowest byte against the species' share
/// of females in 256ths (<c>SpeciesData_GetGenderOf</c>), the ability its lowest bit when the species has two
/// (<c>sub_02073E18</c>), and whether it is shiny the four halves of it and of its first trainer's 32-bit number,
/// exclusive-or'd together, against 8 (<c>Pokemon_IsPersonalityShiny</c>). Wurmple's evolution reads its high half
/// (<see cref="Evolution"/>). No drawing or input.
/// </summary>
public static class Personality
{
    /// <summary>A personality as the original draws one: two 16-bit draws, the first the low half (<c>LCRNG_Next() | LCRNG_Next() &lt;&lt; 16</c>).</summary>
    public static uint Draw(Random rng)
    {
        uint low = (uint)rng.Next(1 << 16);
        uint high = (uint)rng.Next(1 << 16);
        return low | high << 16;
    }

    /// <summary>The nature: the personality's remainder by 25, in the original's order of natures.</summary>
    public static Nature NatureOf(uint personality) => (Nature)(personality % 25);

    /// <summary>
    /// The species' share of females as the original keeps it, in 256ths: 0 always male, 254 always female, 255
    /// genderless, and between them 31, 63, 127, 191 and 223 (<c>generated/gender_ratios.txt</c>). The data keeps the
    /// share in eighths.
    /// </summary>
    public static int GenderThreshold(PokemonSpecies species) => species.GenderRatio switch
    {
        < 0 => 255,
        0 => 0,
        >= 8 => 254,
        var eighths => eighths * 32 - 1
    };

    /// <summary>The gender: the personality's lowest byte under the species' share of females makes it female.</summary>
    public static Gender GenderOf(PokemonSpecies species, uint personality) => GenderThreshold(species) switch
    {
        255 => Gender.Genderless,
        0 => Gender.Male,
        254 => Gender.Female,
        var ratio => ratio > (personality & 0xFF) ? Gender.Female : Gender.Male
    };

    /// <summary>Whether the species' gender is drawn at all (not always one or the other, nor none).</summary>
    public static bool HasRandomGender(PokemonSpecies species) => GenderThreshold(species) is not (0 or 254 or 255);

    /// <summary>The ability's slot: the second of two when the personality's lowest bit is set.</summary>
    public static int AbilitySlot(uint personality) => (int)(personality & 1);

    /// <summary>The ability a Pokémon with these abilities and this personality has; null when it has none listed.</summary>
    public static string? AbilityOf(IReadOnlyList<string> abilities, uint personality) =>
        abilities.Count == 0 ? null : abilities.Count > 1 && AbilitySlot(personality) == 1 ? abilities[1] : abilities[0];

    /// <summary>
    /// The number the shininess test compares: the trainer's two halves and the personality's two, exclusive-or'd
    /// together. Under 8 is shiny by Platinum's rules (1 in 8,192), under 16 by the modern ones (1 in 4,096).
    /// </summary>
    public static int ShinyValue(uint trainer, uint personality) =>
        (int)((trainer >> 16) ^ (trainer & 0xFFFF) ^ (personality >> 16) ^ (personality & 0xFFFF));

    /// <summary>Whether a Pokémon of this personality whose first trainer has this number is shiny, at these odds.</summary>
    public static bool IsShiny(uint trainer, uint personality, int odds = 8192) =>
        ShinyValue(trainer, personality) < 65536 / Math.Max(1, odds);

    /// <summary>A personality drawn again and again until it gives the nature (<c>sub_02074044</c>: Synchronize's).</summary>
    public static uint ForNature(Nature nature, Random rng)
    {
        uint personality;
        do personality = Draw(rng);
        while (NatureOf(personality) != nature);
        return personality;
    }

    /// <summary>
    /// The personality Cute Charm makes (<c>sub_02074128</c>), built rather than drawn: the nature itself for a
    /// female, and for a male the first multiple of 25 past the species' share of females plus the nature, so its
    /// lowest byte makes it male and its remainder by 25 is the nature. Its high half is nought, which is why Platinum's
    /// Cute Charm Pokémon are shiny for some trainers far more often than for others; kept.
    /// </summary>
    public static uint CuteCharm(PokemonSpecies species, Gender gender, Nature nature)
    {
        int ratio = GenderThreshold(species);
        if (!HasRandomGender(species) || gender != Gender.Male) return (uint)nature;
        return (uint)(25 * (ratio / 25 + 1) + (int)nature);
    }

    /// <summary>
    /// A personality that is shiny for this trainer (<c>Pokemon_FindShinyPersonality</c>: the Poké Radar's sparkling
    /// patch): the lowest three bits of each half drawn, then each of the other thirteen bits set in one half where the
    /// trainer's halves differ and in both or neither where they agree, so the four halves cancel to under 8.
    /// </summary>
    public static uint Shiny(uint trainer, Random rng)
    {
        uint id = ((trainer >> 16) ^ (trainer & 0xFFFF)) >> 3;
        uint low = (uint)rng.Next(1 << 16) & 0x7;
        uint high = (uint)rng.Next(1 << 16) & 0x7;
        for (int i = 0; i < 13; i++)
        {
            uint bit = 1u << (i + 3);
            if ((id & (1u << i)) != 0)
            {
                if ((rng.Next(1 << 16) & 1) != 0) low |= bit;
                else high |= bit;
            }
            else if ((rng.Next(1 << 16) & 1) != 0)
            {
                low |= bit;
                high |= bit;
            }
        }
        return high << 16 | low;
    }

    /// <summary>
    /// A shiny personality that also gives the gender or the nature asked for (<c>CreateWildMonShinyWithGenderOrNature</c>:
    /// the Poké Radar's sparkling patch with Cute Charm or Synchronize at the head of the team): shiny personalities are
    /// found again until one does. Only one is ever asked, as only one ability can be at work.
    /// </summary>
    public static uint ShinyWith(PokemonSpecies species, uint trainer, Random rng, Gender? gender, Nature? nature)
    {
        uint personality = Shiny(trainer, rng);
        if (gender is { } g && HasRandomGender(species))
            while (GenderOf(species, personality) != g) personality = Shiny(trainer, rng);
        else if (nature is { } n)
            while (NatureOf(personality) != n) personality = Shiny(trainer, rng);
        return personality;
    }

    /// <summary>
    /// The next number of the original's other generator (<c>ARNG_Next</c>: × 1,812,433,253 + 1 in 32 bits), which the
    /// Masuda method draws a breeding's personality again with.
    /// </summary>
    public static uint NextAlternate(uint value) => unchecked(value * 1812433253u + 1u);

    /// <summary>
    /// The personality a Pokémon is made with when the caller asks for a gender or a nature (Cute Charm and
    /// Synchronize at the head of the team, <c>CreateWildMon</c>): a gender the species can have is given by Cute
    /// Charm's built personality, with the nature asked for or one drawn; a nature alone is drawn for; nothing asked
    /// is a plain draw.
    /// </summary>
    public static uint Choose(PokemonSpecies species, Random rng, Gender? gender, Nature? nature)
    {
        if (gender is { } g && g != Gender.Genderless && HasRandomGender(species))
            return CuteCharm(species, g, nature ?? (Nature)rng.Next(25));
        return nature is { } n ? ForNature(n, rng) : Draw(rng);
    }
}
