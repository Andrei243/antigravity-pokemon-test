using System;
using System.Linq;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia's hand-built Pokémon (plan 46 · CP4 to CP8, sculpted in C# first: the Blender kit of plan 48 follows). Each batch is a file of
// its own, PokemonModels.Carpathia<N>.cs, with its names (CarpathiaNames<N>) and its builders (CreateCarpathia<N>, by name in capitals,
// null for any other); the forms ("<Species>-Carpathia") are batch 14's. This file only joins them.
internal static partial class PokemonModels
{
    /// <summary>Every Carpathian species and form with a sculpt of its own, in batch order.</summary>
    private static string[] CarpathiaNames() =>
        new string[0]
            .Concat(CarpathiaNames1())
            .Concat(CarpathiaNames2())
            .Concat(CarpathiaNames3())
            .Concat(CarpathiaNames4())
            .Concat(CarpathiaNames5())
            .Concat(CarpathiaNames6())
            .Concat(CarpathiaNames7())
            .Concat(CarpathiaNames8())
            .Concat(CarpathiaNames9())
            .Concat(CarpathiaNames10())
            .Concat(CarpathiaNames11())
            .Concat(CarpathiaNames12())
            .Concat(CarpathiaNames13())
            .Concat(CarpathiaNames14())
            .ToArray();

    /// <summary>The names of the forms among them (batch 14), added to <see cref="Forms"/>.</summary>
    private static string[] CarpathiaFormNames() => CarpathiaNames14();

    /// <summary>The Carpathian sculpt of a species or form, by name in capitals, or null.</summary>
    private static PokeBuilder? CreateCarpathia(string species)
    {
        var s = species.ToUpperInvariant();
        return CreateCarpathia1(s)
            ?? CreateCarpathia2(s)
            ?? CreateCarpathia3(s)
            ?? CreateCarpathia4(s)
            ?? CreateCarpathia5(s)
            ?? CreateCarpathia6(s)
            ?? CreateCarpathia7(s)
            ?? CreateCarpathia8(s)
            ?? CreateCarpathia9(s)
            ?? CreateCarpathia10(s)
            ?? CreateCarpathia11(s)
            ?? CreateCarpathia12(s)
            ?? CreateCarpathia13(s)
            ?? CreateCarpathia14(s);
    }
}
