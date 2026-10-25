using System;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The Shiny Charm (plan 13 · V5; the later games' key item): carrying it in the bag gives a wild Pokémon met in the field
/// and a Pokémon the player is given two more rolls at being shiny, three in all, each at the odds in force
/// (<see cref="RunRules.ShinyOdds"/>). GPU-free; the rolls are the Pokémon's (<see cref="Pokemon.RerollForShiny"/>) on the
/// generator the caller hands over. Nothing in the game gives the charm yet (plan 06 · R28's), so a game without it rolls
/// exactly once, as ever.
/// </summary>
public static class ShinyCharm
{
    /// <summary>The item's name in <c>items.json</c>.</summary>
    public const string ItemName = "Shiny Charm";

    /// <summary>How many rolls the charm adds to the Pokémon's own.</summary>
    public const int ExtraRolls = 2;

    /// <summary>Whether the bag holds the charm.</summary>
    public static bool Held(Inventory bag) => bag.AllItems.Any(s => s.Quantity > 0 && s.Name == ItemName);

    /// <summary>
    /// Gives a Pokémon the charm's extra rolls when the bag has it (and draws nothing when it hasn't), keeping the gender and
    /// nature a lead's ability chose. Returns whether the Pokémon is shiny.
    /// </summary>
    public static bool Roll(Pokemon pokemon, Inventory bag, Random rng, Gender? gender = null, Nature? nature = null) =>
        Held(bag) ? pokemon.RerollForShiny(rng, ExtraRolls, gender, nature) : pokemon.IsShiny;
}
