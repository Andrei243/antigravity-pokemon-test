using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The Mega Dynamax forms (plan 06 · R20c) that have a sculpt of their own (<c>PokemonModels.MegaMax*.cs</c>): a Pokémon
/// in one of these asks for its form's model, any other asks for its Mega's (<see cref="Pokemon.ModelName"/>). GPU-free,
/// so the Models layer can ask; the sculpts' tests hold this list equal to the models the graphics layer has.
/// </summary>
internal static class MegaDynamaxLooks
{
    /// <summary>Whether <paramref name="form"/> (<c>Charizard-Mega-X-Mmax</c>) has a model of its own.</summary>
    public static bool HasSculpt(string form) => Sculpted.Contains(form);

    /// <summary>The forms with a sculpt, a block to each session that sculpts some; a name is added in the session that builds it.</summary>
    internal static readonly HashSet<string> Sculpted = new(StringComparer.OrdinalIgnoreCase)
    {
        // The second half, from Incineroar to Victreebel (PokemonModels.MegaMax2.cs)
        "Incineroar-Mega-Mmax", "Infernape-Mega-Mmax", "Inteleon-Mega-Mmax", "Kangaskhan-Mega-Mmax", "Lopunny-Mega-Mmax", "Lucario-Mega-Mmax",
        "Manectric-Mega-Mmax", "Mawile-Mega-Mmax", "Medicham-Mega-Mmax", "Meganium-Mega-Mmax", "Meowscarada-Mega-Mmax", "Metagross-Mega-Mmax",
        "Pidgeot-Mega-Mmax", "Pinsir-Mega-Mmax", "Primarina-Mega-Mmax", "Pyroar-Mega-Mmax", "Quaquaval-Mega-Mmax", "Raichu-Mega-X-Mmax",
        "Raichu-Mega-Y-Mmax", "Rillaboom-Mega-Mmax", "Sableye-Mega-Mmax", "Salamence-Mega-Mmax", "Samurott-Mega-Mmax", "Sceptile-Mega-Mmax",
        "Scizor-Mega-Mmax", "Scolipede-Mega-Mmax", "Serperior-Mega-Mmax", "Sharpedo-Mega-Mmax", "Skarmory-Mega-Mmax", "Skeledirge-Mega-Mmax",
        "Slowbro-Mega-Mmax", "Starmie-Mega-Mmax", "Steelix-Mega-Mmax", "Swampert-Mega-Mmax", "Torterra-Mega-Mmax", "Typhlosion-Mega-Mmax",
        "Tyranitar-Mega-Mmax", "Venusaur-Mega-Mmax", "Victreebel-Mega-Mmax"
    };
}
