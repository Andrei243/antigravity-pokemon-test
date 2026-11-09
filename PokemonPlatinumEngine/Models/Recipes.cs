using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// One tier of the TM maker's prices (plan 39 · CR4): a move up to <see cref="MaxPower"/> costs this many of its type's
/// common material, this many of its rare one (none for a light move) and this much money.
/// </summary>
public sealed class TmTier
{
    [DataRequired] public string Id { get; set; } = "";
    /// <summary>The most base power the tier holds; a status move has none and falls in the first tier.</summary>
    [DataRequired] public int MaxPower { get; set; }
    [DataRequired] public int Common { get; set; }
    public int Rare { get; set; }
    [DataRequired] public int Money { get; set; }
}

/// <summary>The TM maker's section of <c>Data/recipes.json</c>.</summary>
public sealed class MachineRecipes
{
    /// <summary>The power a damaging move counts as when the data gives it none (Return, Frustration, Gyro Ball: the original's table writes them as variable).</summary>
    public int VariablePower { get; set; } = 80;
    /// <summary>The tiers, lightest first.</summary>
    public List<TmTier> Tiers { get; set; } = new();
}

/// <summary>
/// The file <c>Data/recipes.json</c> (plan 39; ours, written by hand and read strictly): CR4 adds <c>machines</c>, the
/// TM maker's prices; CR3 adds the kit's recipes, CR5 the crosses and CR6 and CR7 the dishes beside it.
/// </summary>
public sealed class RecipesFile
{
    public MachineRecipes Machines { get; set; } = new();

    /// <summary>The kit's MAKE tab (plan 39 · CR3): what the kit makes and what each takes.</summary>
    public List<CraftRecipe> Kit { get; set; } = new();

    /// <summary>The camp's pot (plan 39 · CR6): its dishes and what a grade gives.</summary>
    public KitchenRecipes Kitchen { get; set; } = new();
}

/// <summary>The tables of the recipes (plan 39; <c>Data/recipes.json</c>). No drawing or input.</summary>
public static class RecipeTable
{
    private static readonly Lazy<RecipesFile> file = new(() => GameDataFiles.Load<RecipesFile>("recipes.json", strict: true));

    public static RecipesFile File => file.Value;

    public static MachineRecipes Machines => File.Machines;

    public static IReadOnlyList<TmTier> Tiers => Machines.Tiers;

    /// <summary>The kit's recipes, in the file's order (plan 39 · CR3).</summary>
    public static IReadOnlyList<CraftRecipe> Kit => File.Kit;

    /// <summary>The camp's dishes and grades (plan 39 · CR6).</summary>
    public static KitchenRecipes Kitchen => File.Kitchen;
}
