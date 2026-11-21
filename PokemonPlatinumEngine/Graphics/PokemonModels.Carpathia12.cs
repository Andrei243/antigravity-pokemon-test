using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 12 (plan 46 · CP4 to CP8): Fluieras to Lunacolac (20 species).
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames12() => new string[0];

    private static PokeBuilder? CreateCarpathia12(string species) => species switch
    {
        _ => null
    };
}
