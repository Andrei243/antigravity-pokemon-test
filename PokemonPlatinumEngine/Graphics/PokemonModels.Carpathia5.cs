using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 5 (plan 46 · CP4 to CP8): Babelith to Muscarita (20 species).
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames5() => new string[0];

    private static PokeBuilder? CreateCarpathia5(string species) => species switch
    {
        _ => null
    };
}
