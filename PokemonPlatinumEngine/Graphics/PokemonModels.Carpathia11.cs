using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 11 (plan 46 · CP4 to CP8): Turnulet to Cobzar (20 species).
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames11() => new string[0];

    private static PokeBuilder? CreateCarpathia11(string species) => species switch
    {
        _ => null
    };
}
