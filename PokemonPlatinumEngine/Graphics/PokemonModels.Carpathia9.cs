using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 9 (plan 46 · CP4 to CP8): Rapanor to Stimel (20 species).
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames9() => new string[0];

    private static PokeBuilder? CreateCarpathia9(string species) => species switch
    {
        _ => null
    };
}
