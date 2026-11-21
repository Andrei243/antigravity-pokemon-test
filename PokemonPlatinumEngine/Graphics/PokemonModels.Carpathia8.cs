using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 8 (plan 46 · CP4 to CP8): Hatzlet to Rapanel (20 species).
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames8() => new string[0];

    private static PokeBuilder? CreateCarpathia8(string species) => species switch
    {
        _ => null
    };
}
