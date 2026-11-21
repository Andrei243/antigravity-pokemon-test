using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 13 (plan 46 · CP4 to CP8): Furtunaur to Nazdravan (9 species).
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames13() => new string[0];

    private static PokeBuilder? CreateCarpathia13(string species) => species switch
    {
        _ => null
    };
}
