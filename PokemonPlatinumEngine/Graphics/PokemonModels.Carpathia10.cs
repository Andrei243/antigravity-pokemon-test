using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 10 (plan 46 · CP4 to CP8): Stimara to Tramvaior (20 species).
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames10() => new string[0];

    private static PokeBuilder? CreateCarpathia10(string species) => species switch
    {
        _ => null
    };
}
