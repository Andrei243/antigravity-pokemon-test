using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 6 (plan 46 · CP4 to CP8): Izvoras to Oracul (20 species).
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames6() => new string[0];

    private static PokeBuilder? CreateCarpathia6(string species) => species switch
    {
        _ => null
    };
}
