using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 2 (plan 46 · CP4 to CP8): Ciobipup to Floarsol (20 species).
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames2() => new string[0];

    private static PokeBuilder? CreateCarpathia2(string species) => species switch
    {
        _ => null
    };
}
