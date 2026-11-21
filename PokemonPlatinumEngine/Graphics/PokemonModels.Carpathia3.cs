using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 3 (plan 46 · CP4 to CP8): Panusel to Cerbanor (20 species).
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames3() => new string[0];

    private static PokeBuilder? CreateCarpathia3(string species) => species switch
    {
        _ => null
    };
}
