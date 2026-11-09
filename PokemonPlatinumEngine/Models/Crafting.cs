using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>One thing a recipe takes: so many of a material of the pouch, or of an item of the bag (a berry).</summary>
public sealed class CraftNeed
{
    /// <summary>A material's id (<c>Data/materials.json</c>); null for an item.</summary>
    public string? Material { get; set; }
    /// <summary>An item's name (<c>items.json</c>); null for a material.</summary>
    public string? Item { get; set; }
    [DataRequired] public int Count { get; set; }
}

/// <summary>
/// One recipe of the kit's MAKE tab (plan 39 · CR3; <c>Data/recipes.json</c>'s <c>kit</c>): an id of letters and digits,
/// the item it makes and how many of it at a time, what it takes, and for an item the data gives no price (Hisui's balls)
/// the money it stands for, which its materials' worth is held to as a priced item's is held to its price.
/// </summary>
public sealed class CraftRecipe
{
    [DataRequired] public string Id { get; set; } = "";
    [DataRequired] public string Makes { get; set; } = "";
    public int Count { get; set; } = 1;
    public int Value { get; set; }
    public List<CraftNeed> Takes { get; set; } = new();
}

/// <summary>What <see cref="Crafting.Make"/> did: how many were made, or why none was (and nothing taken).</summary>
public sealed record CraftMade(bool Made, string? Why, int Count)
{
    public static CraftMade Refused(string why) => new(false, why, 0);
}

/// <summary>
/// The kit's MAKE tab (plan 39 · CR3; <c>docs/mechanics/rulings.md</c>, "Materials, crafting and cooking"): a recipe of
/// <c>recipes.json</c>'s <c>kit</c> takes its materials from the pouch and its items from the bag and puts what it makes in
/// the bag, so many times at once. It takes and gives exactly, refuses a recipe short of anything or whose item the bag
/// holds all it can of, and no recipe makes an item for less than its price in its materials' worth (the content check and
/// <c>CraftingKitTests</c> hold the table to it). <b>A made thing is no find and no gift</b>: it is exactly the item the
/// recipe names, so the randomiser (plan 13 · V6, which maps what the world and the scripts give) never sees it, as the TM
/// maker's machines. No drawing or input.
/// </summary>
public static class Crafting
{
    /// <summary>The most of one item the bag holds of a thing the kit makes.</summary>
    public const int MostInBag = 999;

    /// <summary>The most times a recipe is made at once ("how many?").</summary>
    public const int MostAtOnce = 99;

    public static IReadOnlyList<CraftRecipe> Recipes => RecipeTable.Kit;

    public static CraftRecipe? Get(string id) => Recipes.FirstOrDefault(r => r.Id == id);

    /// <summary>The item a recipe makes; null where the data has none (a mistake of the file, which the content check names).</summary>
    public static ItemData? ItemOf(CraftRecipe recipe) => ItemDatabase.Get(recipe.Makes);

    /// <summary>The money a recipe's yield stands for: its item's price for each made, or the recipe's own value where the item has no price.</summary>
    public static int PriceOf(CraftRecipe recipe) => (ItemOf(recipe)?.Price is int price and > 0 ? price : recipe.Value) * Math.Max(1, recipe.Count);

    /// <summary>What a recipe's needs are worth: each material's worth and each item's price, by count.</summary>
    public static int WorthOf(CraftRecipe recipe) => recipe.Takes.Sum(n => n.Count *
        (n.Material != null ? MaterialTable.Get(n.Material)?.Worth ?? 0 : ItemDatabase.Get(n.Item ?? "")?.Price ?? 0));

    /// <summary>How many of a need are held: the pouch's for a material, the bag's for an item.</summary>
    public static int Held(CraftNeed need, Inventory bag, MaterialPouch pouch) =>
        need.Material != null ? pouch.Count(need.Material) : ItemDatabase.Get(need.Item ?? "") is { } item ? bag.GetQuantity(item) : 0;

    /// <summary>The name a need is shown by: the material's, or the item's.</summary>
    public static string NameOf(CraftNeed need) =>
        need.Material != null ? MaterialTable.NameOf(need.Material) : ItemDatabase.Get(need.Item ?? "")?.DisplayName ?? need.Item ?? "";

    /// <summary>The most times the recipe can be made with what is held, the bag's room and <see cref="MostAtOnce"/>; nought for none.</summary>
    public static int MostOf(CraftRecipe recipe, Inventory bag, MaterialPouch pouch)
    {
        if (ItemOf(recipe) is not { } item) return 0;
        int most = MostAtOnce;
        foreach (var need in recipe.Takes)
            if (need.Count > 0) most = Math.Min(most, Held(need, bag, pouch) / need.Count);
        int room = (MostInBag - bag.GetQuantity(item)) / Math.Max(1, recipe.Count);
        return Math.Max(0, Math.Min(most, room));
    }

    /// <summary>
    /// Why the recipe can't be made <paramref name="times"/> times now, in words, or null when it can: a material or an item
    /// short (the first named), or the bag holding all it can of what it makes.
    /// </summary>
    public static string? WhyNot(CraftRecipe recipe, int times, Inventory bag, MaterialPouch pouch)
    {
        if (ItemOf(recipe) is not { } item || times < 1) return Text.Of("crafting.make.why.nothing");
        foreach (var need in recipe.Takes)
            if (Held(need, bag, pouch) < need.Count * times) return Text.Of("crafting.make.why.short", ("name", NameOf(need)));
        if (bag.GetQuantity(item) + recipe.Count * times > MostInBag) return Text.Of("crafting.make.why.full", ("name", item.DisplayName));
        return null;
    }

    /// <summary>Makes the recipe <paramref name="times"/> times: takes its needs and puts what it makes in the bag. Nothing changes when it can't be.</summary>
    public static CraftMade Make(CraftRecipe recipe, int times, Inventory bag, MaterialPouch pouch)
    {
        if (WhyNot(recipe, times, bag, pouch) is { } why) return CraftMade.Refused(why);
        foreach (var need in recipe.Takes)
        {
            if (need.Material != null) pouch.Take(need.Material, need.Count * times);
            else bag.RemoveItem(ItemDatabase.Get(need.Item!)!, need.Count * times);
        }
        int made = recipe.Count * times;
        bag.AddItem(ItemOf(recipe)!, made);
        return new CraftMade(true, null, made);
    }

    // ------------------------------------------------------------------ the apprentice's trade

    /// <summary>How many of a type's common material the apprentice takes for one of its rare.</summary>
    public const int TradeCommons = 3;

    /// <summary>
    /// The common material the apprentice would trade now: of the types whose common one the pouch holds three of and whose
    /// rare one isn't full, the one held most of (the table's order breaks a tie); null for none.
    /// </summary>
    public static MaterialData? Tradable(MaterialPouch pouch) =>
        MaterialTable.All
            .Where(m => m is { Kind: MaterialKind.Pokemon, Rare: false, Type: not null } && pouch.Count(m.Id) >= TradeCommons
                && MaterialTable.OfType(m.Type!.Value, rare: true) is { } rare && !pouch.IsFull(rare.Id))
            .OrderByDescending(m => pouch.Count(m.Id))
            .FirstOrDefault();

    /// <summary>The apprentice's trade: three of the most-held common material for one of its type's rare one; the rare one made, or null and nothing traded.</summary>
    public static MaterialData? Trade(MaterialPouch pouch)
    {
        if (Tradable(pouch) is not { } common || MaterialTable.OfType(common.Type!.Value, rare: true) is not { } rare) return null;
        pouch.Take(common.Id, TradeCommons);
        pouch.Add(rare.Id, 1);
        return rare;
    }

    // ------------------------------------------------------------------ the table held to its rules

    /// <summary>
    /// What is wrong with the kit's recipes of a file, a line each (the content check and the tests ask): an id of letters and
    /// digits named once, an item that exists, a need that names a material or an item that exists (and not both), a count
    /// of one or more, a value only where the item has no price, and materials worth no less than what is made.
    /// </summary>
    public static IEnumerable<(string Name, string What)> Problems(IEnumerable<CraftRecipe> recipes)
    {
        var list = recipes.ToList();
        foreach (var group in list.GroupBy(r => r.Id).Where(g => g.Count() > 1)) yield return (group.Key, "is a recipe twice");
        foreach (var recipe in list)
        {
            string name = recipe.Id.Length == 0 ? recipe.Makes : recipe.Id;
            if (recipe.Id.Length == 0 || !recipe.Id.All(char.IsAsciiLetterOrDigit)) yield return (name, "has no id of letters and digits");
            var item = ItemDatabase.Get(recipe.Makes);
            if (item == null) { yield return (name, $"makes '{recipe.Makes}', which is no item"); continue; }
            if (MoveTeaching.IsMachine(item)) yield return (name, "makes a machine, which is the TM maker's");
            if (recipe.Count < 1) yield return (name, "makes nothing at a time");
            if (recipe.Takes.Count == 0) yield return (name, "takes nothing");
            if (item.Price > 0 && recipe.Value > 0) yield return (name, "gives a value of its own, which only an item with no price has");
            if (item.Price <= 0 && recipe.Value <= 0) yield return (name, $"makes {item.Name}, which has no price, and gives no value its materials are held to");
            foreach (var need in recipe.Takes)
            {
                if ((need.Material == null) == (need.Item == null)) yield return (name, "has a need that names neither or both a material and an item");
                else if (need.Material != null && !MaterialTable.Exists(need.Material)) yield return (name, $"takes '{need.Material}', which is no material");
                else if (need.Item != null && ItemDatabase.Get(need.Item) == null) yield return (name, $"takes '{need.Item}', which is no item");
                if (need.Count < 1) yield return (name, "takes a need of no count");
            }
            if (WorthOf(recipe) < PriceOf(recipe))
                yield return (name, $"takes materials worth {WorthOf(recipe)} for what is worth {PriceOf(recipe)}: cheaper than the Mart");
        }
    }
}
