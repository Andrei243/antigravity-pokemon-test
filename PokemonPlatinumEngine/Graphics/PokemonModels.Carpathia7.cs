using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 7 (plan 46 · CP4 to CP8): Starcel to Liliacon (20 species).
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames7() => new string[0];

    private static PokeBuilder? CreateCarpathia7(string species) => species switch
    {
        _ => null
    };
}
