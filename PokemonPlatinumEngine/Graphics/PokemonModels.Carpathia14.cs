using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 14 (plan 46 · CP8): the 25 regional forms, "<Species>-Carpathia".
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames14() => new string[0];

    private static PokeBuilder? CreateCarpathia14(string species) => species switch
    {
        _ => null
    };
}
