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

    /// <summary>Crossing berries (plan 39 · CR5): which pairs cross into which berry, and how often a harvest holds one.</summary>
    public Overworld.CrossRecipes Crosses { get; set; } = new();

    /// <summary>The waiters' and stalls' menus (plan 39 · CR7): a row to a dish served somewhere, with its price.</summary>
    public List<MealStand> Stands { get; set; } = new();
}

/// <summary>
/// One dish a waiter or a stall serves (plan 39 · CR7; <c>recipes.json</c>'s <c>stands</c>): the stand, by the script file
/// it is played in (<see cref="Place"/>), the dish of the kitchen's table and its price. The dish's power is the
/// kitchen's own row, served at a great grade.
/// </summary>
public sealed class MealStand
{
    [DataRequired] public string Place { get; set; } = "";
    [DataRequired] public string Dish { get; set; } = "";
    [DataRequired] public int Price { get; set; }
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

    /// <summary>The dishes served at a stand (plan 39 · CR7), by the stand's script file, in the file's order.</summary>
    public static IReadOnlyList<MealStand> StandAt(string place) => File.Stands.Where(s => s.Place == place).ToList();
}
