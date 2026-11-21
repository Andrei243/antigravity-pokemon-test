using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 4 (plan 46 · CP4 to CP8): Zimbrel to Babut (20 species).
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames4() => new string[0];

    private static PokeBuilder? CreateCarpathia4(string species) => species switch
    {
        _ => null
    };
}
