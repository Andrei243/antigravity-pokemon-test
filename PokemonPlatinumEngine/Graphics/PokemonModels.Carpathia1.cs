using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 1 (plan 46 · CP4 to CP8): Lynkit to Aricorn (20 species).
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames1() => new string[0];

    private static PokeBuilder? CreateCarpathia1(string species) => species switch
    {
        _ => null
    };
}
