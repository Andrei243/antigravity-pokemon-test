using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>A material and how many of it: one line of what a Pokémon left, a spot gave or a recipe takes.</summary>
public readonly record struct MaterialCount(string Id, int Count);

/// <summary>
/// What a wild Pokémon leaves behind when it is felled or caught in the field (plan 39 · CR1; <c>docs/mechanics/rulings.md</c>,
/// "Materials, crafting and cooking"; the numbers are <see cref="MaterialTable.Drops"/>). One to three of its type's common
/// material, a second type's too half the time, and now and then its type's rare one, more of both at a high level. The
/// draw is on the generator the caller hands over, in this order: the common count, the second type's chance and count,
/// the rare chance, which type's rare one. A Pokémon of one type draws the first, the rare chance and nothing more.
/// No drawing or input.
/// </summary>
public static class MaterialDrops
{
    /// <summary>What <paramref name="foe"/> leaves, as the rules above say; empty when nothing is left.</summary>
    /// <param name="power">The materials' power in force (plan 39 · CR6, a power of plan 36's table): this many more of each
    /// common material left. It draws nothing more, so the leavings' rolls are the same with it as without.</param>
    public static List<MaterialCount> For(Pokemon foe, Random rng, int power = 0)
    {
        var rules = MaterialTable.Drops;
        var left = new List<MaterialCount>();
        int level = Math.Max(1, foe.Level);
        int extra = Math.Min(rules.LevelExtraMost, level / Math.Max(1, rules.LevelStep)) + Math.Max(0, power);
        PokemonType first = foe.PrimaryType;
        PokemonType? second = foe.SecondaryType is { } s && s != first ? s : null;

        void Common(PokemonType type)
        {
            int n = rng.Next(rules.CommonLeast, rules.CommonMost + 1) + extra;
            if (MaterialTable.OfType(type, rare: false) is { } material) Add(left, material.Id, n);
        }

        Common(first);
        if (second != null && rng.Next(100) < rules.SecondTypeInHundred) Common(second.Value);

        int rareChance = Math.Min(rules.RareInHundredMost, rules.RareInHundred + level / Math.Max(1, rules.RareLevelStep));
        if (rng.Next(100) < rareChance)
        {
            var type = second != null && rng.Next(3) < rules.SecondRareInThree ? second.Value : first;
            if (MaterialTable.OfType(type, rare: true) is { } rare) Add(left, rare.Id, level >= rules.RareDoubleLevel ? 2 : 1);
        }
        return left;
    }

    /// <summary>The leavings of every foe of a battle, one list: the same material from two foes is one line.</summary>
    public static List<MaterialCount> For(IEnumerable<Pokemon> foes, Random rng, int power = 0)
    {
        var all = new List<MaterialCount>();
        foreach (var foe in foes)
            foreach (var (id, n) in For(foe, rng, power)) Add(all, id, n);
        return all;
    }

    private static void Add(List<MaterialCount> list, string id, int n)
    {
        int at = list.FindIndex(m => m.Id == id);
        if (at >= 0) list[at] = list[at] with { Count = list[at].Count + n };
        else list.Add(new MaterialCount(id, n));
    }
}
