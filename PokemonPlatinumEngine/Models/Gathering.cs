using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>The five kinds of gathering spot (plan 39 · CR2): a rock, a fallen log, a herb bush, a heap on the shore, a drift of snow.</summary>
public enum GatherKind { Rock, Wood, Plant, Shore, Snow }

/// <summary>One find of a kind of spot: a field's material, its weight among the kind's finds, and how many of it a picking gives.</summary>
public sealed class GatherFind
{
    [DataRequired] public string Material { get; set; } = "";
    [DataRequired] public int Weight { get; set; }
    public int Least { get; set; } = 1;
    public int Most { get; set; } = 2;
}

/// <summary>A kind of spot and what it gives (<c>materials.json</c>'s <c>gather</c>).</summary>
public sealed class GatherTable
{
    [DataRequired] public GatherKind Kind { get; set; }
    public List<GatherFind> Finds { get; set; } = new();
}

/// <summary>What a tree cut and a rock smashed leave with the kit in the bag (<c>materials.json</c>'s <c>cleared</c>).</summary>
public sealed class ClearedFinds
{
    /// <summary>The material a tree cut leaves, and the one a rock smashed leaves.</summary>
    public string Tree { get; set; } = "timber";
    public string Rock { get; set; } = "quarryStone";
    /// <summary>One time in this many, a tree cut or a rock smashed leaves one of its material.</summary>
    public int OneIn { get; set; } = 3;
}

/// <summary>
/// Gathering in the field (plan 39 · CR2; <c>docs/mechanics/rulings.md</c>, "Materials, crafting and cooking"): a spot of
/// the field bears once a calendar day as an Apricorn tree does (picked by its key and the day, kept in the kit's
/// <see cref="CraftingState.Picked"/>), and a picking gives one of its kind's finds, drawn by weight, so many of it; a
/// tree cut and a rock smashed with the kit in the bag leave wood or stone one time in three. Every draw is on the
/// generator handed in, and only once the kit is held: the spots are hidden until then. No drawing or input.
/// </summary>
public static class Gathering
{
    /// <summary>A kind's table; empty for one the file lacks.</summary>
    public static IReadOnlyList<GatherFind> FindsOf(GatherKind kind) =>
        MaterialTable.File.Gather.FirstOrDefault(t => t.Kind == kind)?.Finds ?? new List<GatherFind>();

    /// <summary>The key a spot is picked by: its place and its id.</summary>
    public static string KeyOf(string place, string id) => $"{place}:{id}";

    /// <summary>Whether a spot has something on it to take on a day: not picked that day.</summary>
    public static bool Bears(CraftingState state, string spot, int day) =>
        !state.Picked.TryGetValue(spot, out int picked) || picked != day;

    /// <summary>
    /// Picks a spot of a kind on a day: one find drawn by weight on <paramref name="rng"/>, its count drawn between its
    /// least and most, put in the pouch, and the spot noted as picked. Nothing (and no draw) for a spot picked that day.
    /// </summary>
    public static MaterialCount? Pick(CraftingState state, string spot, GatherKind kind, int day, Random rng)
    {
        if (!Bears(state, spot, day)) return null;
        var finds = FindsOf(kind);
        int total = finds.Sum(f => Math.Max(0, f.Weight));
        if (total <= 0) return null;
        int roll = rng.Next(total);
        var find = finds.First(f =>
        {
            roll -= Math.Max(0, f.Weight);
            return roll < 0;
        });
        int n = find.Least >= find.Most ? find.Least : rng.Next(find.Least, find.Most + 1);
        state.PickedOn(spot, day);
        int added = state.Pouch.Add(find.Material, n);
        return new MaterialCount(find.Material, added);
    }

    /// <summary>
    /// A tree cut (<paramref name="tree"/>) or a rock smashed: one time in <see cref="ClearedFinds.OneIn"/> one of its
    /// material, into the pouch. One draw on <paramref name="rng"/>; the caller draws nothing without the kit.
    /// </summary>
    public static MaterialCount? Cleared(MaterialPouch pouch, bool tree, Random rng)
    {
        var cleared = MaterialTable.File.Cleared;
        if (rng.Next(Math.Max(1, cleared.OneIn)) != 0) return null;
        string id = tree ? cleared.Tree : cleared.Rock;
        int added = pouch.Add(id, 1);
        return added > 0 ? new MaterialCount(id, added) : null;
    }

    /// <summary>The tables' findings against the materials: a find that is no field material, a kind with no finds.</summary>
    public static IEnumerable<string> Problems(MaterialsFile? file = null)
    {
        file ??= MaterialTable.File;
        foreach (var kind in Enum.GetValues<GatherKind>())
            if (file.Gather.FirstOrDefault(t => t.Kind == kind) is not { Finds.Count: > 0 }) yield return $"gather: the {kind} spot gives nothing";
        foreach (var table in file.Gather)
            foreach (var find in table.Finds)
            {
                if (MaterialTable.Get(find.Material) is not { Kind: MaterialKind.Field }) yield return $"gather: '{find.Material}' is no field material";
                if (find.Weight <= 0 || find.Least < 1 || find.Most < find.Least) yield return $"gather: '{find.Material}' of {table.Kind} has a bad weight or count";
            }
        var cleared = file.Cleared;
        if (cleared.OneIn < 1) yield return "cleared: oneIn is under one";
        foreach (string id in new[] { cleared.Tree, cleared.Rock })
            if (MaterialTable.Get(id) is not { Kind: MaterialKind.Field }) yield return $"cleared: '{id}' is no field material";
    }
}
