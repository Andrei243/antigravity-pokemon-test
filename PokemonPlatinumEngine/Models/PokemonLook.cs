using System;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The names a Pokémon's looks are asked for by (plan 09 · L1; style guide, "Pokémon (version 2, G7)", "Shiny
/// Pokémon"): a model's own name (a species' or a form's, <see cref="Pokemon.ModelName"/>) for its usual colours,
/// and the same name with <c>+s</c> after it for its shiny ones (<c>Gyarados+s</c>, <c>Charizard-Mega-X+s</c>). The
/// models, the menu sprites and the field sprites take either; what is read from the data (a size, a cry, a number)
/// goes by the <see cref="Base"/> name. GPU-free.
/// </summary>
public static class PokemonLook
{
    /// <summary>What a shiny look's name ends in.</summary>
    public const string ShinySuffix = "+s";

    /// <summary>Whether a look name is a shiny one.</summary>
    public static bool IsShiny(string name) => name.EndsWith(ShinySuffix, StringComparison.Ordinal);

    /// <summary>The model's own name, with whatever look it was asked in taken off.</summary>
    public static string Base(string name) => IsShiny(name) ? name[..^ShinySuffix.Length] : name;

    /// <summary>The shiny look of a model (a name already shiny stays as it is).</summary>
    public static string Shiny(string name) => IsShiny(name) ? name : name + ShinySuffix;

    /// <summary>A model's look, shiny or not.</summary>
    public static string Of(string modelName, bool shiny) => shiny ? Shiny(modelName) : Base(modelName);
}
