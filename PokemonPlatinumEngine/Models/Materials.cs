using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>Where a material comes from (plan 39 · CR1): a Pokémon's leavings, the field's finds, or goods bought for cooking (CR6).</summary>
public enum MaterialKind { Pokemon, Field, Pantry }

/// <summary>
/// One material of <c>Data/materials.json</c>: an id of letters and digits (its words are
/// <c>crafting.material.name.&lt;id&gt;</c> and <c>crafting.material.desc.&lt;id&gt;</c> in <c>ui.json</c>), where it comes from, for a
/// Pokémon's the type it is left by and whether it is that type's rare one, and its worth in money, which the recipes
/// are held to (a thing made is never worth less than its Mart price in its materials).
/// </summary>
public sealed class MaterialData
{
    [DataRequired] public string Id { get; set; } = "";
    [DataRequired] public MaterialKind Kind { get; set; }
    public PokemonType? Type { get; set; }
    public bool Rare { get; set; }
    [DataRequired] public int Worth { get; set; }
}

/// <summary>
/// What a wild Pokémon leaves behind, as one table of numbers (plan 39 · CR1; <c>docs/mechanics/rulings.md</c>,
/// "Materials, crafting and cooking"): the common material's count, how the level adds to it, how often the second type's
/// common one comes too, and the rare one's odds, which rise with the level.
/// </summary>
public sealed class MaterialDropRules
{
    /// <summary>Fewest and most of the type's common material one Pokémon leaves, before the level adds to it.</summary>
    public int CommonLeast { get; set; } = 1;
    public int CommonMost { get; set; } = 3;
    /// <summary>Every this many levels of the Pokémon add one more of each common material left, at most <see cref="LevelExtraMost"/>.</summary>
    public int LevelStep { get; set; } = 25;
    public int LevelExtraMost { get; set; } = 2;
    /// <summary>The chance in a hundred that a Pokémon of two types leaves its second type's common material as well.</summary>
    public int SecondTypeInHundred { get; set; } = 50;
    /// <summary>The rare material's chance in a hundred at level 0, what each <see cref="RareLevelStep"/> levels add to it (one), and the most it comes to.</summary>
    public int RareInHundred { get; set; } = 8;
    public int RareLevelStep { get; set; } = 5;
    public int RareInHundredMost { get; set; } = 30;
    /// <summary>From this level a rare material left is two, not one.</summary>
    public int RareDoubleLevel { get; set; } = 60;
    /// <summary>Of a Pokémon of two types, the second type's rare material is the one left this often in three (the first's otherwise).</summary>
    public int SecondRareInThree { get; set; } = 1;
}

/// <summary>The file <c>Data/materials.json</c>.</summary>
public sealed class MaterialsFile
{
    /// <summary>The most of one material the pouch holds.</summary>
    [DataRequired] public int PouchLimit { get; set; } = 999;
    public MaterialDropRules Drops { get; set; } = new();
    public List<MaterialData> Materials { get; set; } = new();
}

/// <summary>
/// The tables of the materials (plan 39 · CR1; <c>Data/materials.json</c>, ours, read strictly): who they are, and the
/// words they are shown in. No drawing or input.
/// </summary>
public static class MaterialTable
{
    private static readonly Lazy<MaterialsFile> file = new(() => GameDataFiles.Load<MaterialsFile>("materials.json", strict: true));

    public static MaterialsFile File => file.Value;

    public static IReadOnlyList<MaterialData> All => File.Materials;

    public static int Limit => File.PouchLimit;

    public static MaterialDropRules Drops => File.Drops;

    public static MaterialData? Get(string id) => All.FirstOrDefault(m => m.Id == id);

    public static bool Exists(string id) => Get(id) != null;

    /// <summary>The material a Pokémon of a type leaves: its common one, or its rare one.</summary>
    public static MaterialData? OfType(PokemonType type, bool rare) =>
        All.FirstOrDefault(m => m.Kind == MaterialKind.Pokemon && m.Type == type && m.Rare == rare);

    /// <summary>A material by the name the player reads (any language's); null for no such.</summary>
    public static MaterialData? ByName(string name) =>
        All.FirstOrDefault(m => string.Equals(NameOf(m.Id), name, StringComparison.OrdinalIgnoreCase) || string.Equals(m.Id, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>The name a material is shown by, in the language chosen.</summary>
    public static string NameOf(string id) => Text.Of($"crafting.material.name.{id}");

    /// <summary>What a material is, in a line or two, in the language chosen.</summary>
    public static string DescriptionOf(string id) => Text.Of($"crafting.material.desc.{id}");

    /// <summary>The group a material is listed under (<c>crafting.material.kind.*</c>).</summary>
    public static string KindName(MaterialKind kind)
    {
        string group = kind.ToString().ToLowerInvariant();
        return Text.Of($"crafting.material.kind.{group}");
    }
}

/// <summary>What the game keeps of the pouch, as a save has it.</summary>
public sealed class CraftingSave
{
    public Dictionary<string, int>? Pouch { get; set; }
}

/// <summary>
/// The pouch (plan 39 · CR1): how many of each material the player holds, 999 of a kind at most as the Apricorn Box
/// keeps its counts. Everything that gives or takes a material goes through <see cref="Add"/> and <see cref="Take"/>,
/// so the limit holds for a drop, a spot, a raid and a recipe alike. No drawing or input.
/// </summary>
public sealed class MaterialPouch
{
    private readonly Dictionary<string, int> counts = new(StringComparer.Ordinal);

    /// <summary>Counts every change, so a screen knows to look again. Not saved.</summary>
    public int Revision { get; private set; }

    public int Count(string id) => counts.GetValueOrDefault(id);

    /// <summary>How many kinds are held.</summary>
    public int Kinds => counts.Count(c => c.Value > 0);

    /// <summary>The materials held, in the table's order.</summary>
    public IEnumerable<MaterialData> Held => MaterialTable.All.Where(m => Count(m.Id) > 0);

    public bool IsFull(string id) => Count(id) >= MaterialTable.Limit;

    /// <summary>
    /// Puts <paramref name="n"/> of a material in the pouch; the pouch keeps no more than its limit, so what fitted is
    /// returned (0 for a full pouch, a material the table lacks or no number).
    /// </summary>
    public int Add(string id, int n)
    {
        if (n <= 0 || !MaterialTable.Exists(id)) return 0;
        int added = Math.Min(n, MaterialTable.Limit - Count(id));
        if (added <= 0) return 0;
        counts[id] = Count(id) + added;
        Revision++;
        return added;
    }

    /// <summary>Whether the pouch holds at least <paramref name="n"/> of a material.</summary>
    public bool Has(string id, int n = 1) => n <= 0 || Count(id) >= n;

    /// <summary>Takes <paramref name="n"/> of a material out; false, and nothing changes, if there are fewer.</summary>
    public bool Take(string id, int n)
    {
        if (n <= 0) return true;
        if (Count(id) < n) return false;
        int left = Count(id) - n;
        if (left == 0) counts.Remove(id);
        else counts[id] = left;
        Revision++;
        return true;
    }

    public void Clear() => Load(null);

    public void Load(Dictionary<string, int>? saved)
    {
        counts.Clear();
        foreach (var (id, n) in saved ?? new())
            if (MaterialTable.Exists(id) && n > 0) counts[id] = Math.Min(n, MaterialTable.Limit);
        Revision++;
    }

    public Dictionary<string, int>? ToSave() =>
        counts.Count == 0 ? null : counts.Where(c => c.Value > 0).OrderBy(c => c.Key, StringComparer.Ordinal).ToDictionary(c => c.Key, c => c.Value);
}

/// <summary>
/// What the crafting kit keeps (plan 39 · CR1): the pouch of materials. The spots picked by day (CR2) and the crosses
/// found (CR5) join it as the sessions that need them land; a save is <c>null</c> while nothing is held, so no version
/// is raised.
/// </summary>
public sealed class CraftingState
{
    public MaterialPouch Pouch { get; } = new();

    public void Clear() => Load(null);

    public void Load(CraftingSave? save) => Pouch.Load(save?.Pouch);

    public CraftingSave? ToSave() => Pouch.ToSave() is { } pouch ? new CraftingSave { Pouch = pouch } : null;
}
