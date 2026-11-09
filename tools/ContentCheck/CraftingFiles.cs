using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace ContentCheck;

/// <summary>
/// The crafting kit's files (plan 39): <c>Data/materials.json</c> held to its rules (read strictly; an id of letters and
/// digits, once; every type of Pokémon with its common and its rare material, whose rare one is worth more; a worth; a name
/// and a description in the table of words). CR3 onward add the recipes' rules here: every item, material, berry and power
/// a recipe names exists, and its needs can be had.
/// </summary>
public static class CraftingFiles
{
    public static void Check(GameContent content, List<Finding> found)
    {
        CheckMaterials(content, found);
        CheckRecipes(content, found);
        CheckPowers(content, found);
    }

    /// <summary>
    /// <c>Data/recipes.json</c> (plan 39 · CR4): read strictly; the TM maker's tiers are named once, climb in power and the
    /// last holds every move; each costs materials and money; and every TM and TR of the items has a cost (its move exists
    /// and its type has a common material, and a rare one where its tier asks for it).
    /// </summary>
    private static void CheckRecipes(GameContent content, List<Finding> found)
    {
        string path = Path.Combine(content.Root, "recipes.json");
        if (!File.Exists(path)) return;
        string file = content.Relative(path);
        RecipesFile data;
        try
        {
            data = GameDataFiles.ReadStrict<RecipesFile>(File.ReadAllText(path), file);
        }
        catch (InvalidDataException e)
        {
            found.Add(new Finding(Kind.Unreadable, file, "recipes", e.Message));
            return;
        }

        void Add(string name, string what) => found.Add(new Finding(Kind.Data, file, name, what));
        var tiers = data.Machines.Tiers;
        if (tiers.Count == 0) Add("machines", "has no tiers: the TM maker prices nothing");
        foreach (var group in tiers.GroupBy(t => t.Id).Where(g => g.Count() > 1)) Add(group.Key, "is a tier twice");
        for (int i = 0; i < tiers.Count; i++)
        {
            var tier = tiers[i];
            if (tier.Id.Length == 0) Add($"tiers[{i}]", "has no id");
            if (tier.Common < 1) Add(tier.Id, "takes no common material");
            if (tier.Rare < 0) Add(tier.Id, "takes a negative number of rare materials");
            if (tier.Money < 1) Add(tier.Id, "costs no money");
            if (i > 0 && tier.MaxPower <= tiers[i - 1].MaxPower) Add(tier.Id, "does not hold more power than the tier before it");
            if (i > 0 && tier.Common < tiers[i - 1].Common) Add(tier.Id, "takes less of the common material than a lighter tier");
        }
        if (tiers.Count > 0 && tiers[^1].MaxPower < 250) Add(tiers[^1].Id, "is the last tier and does not hold every move (its maxPower is under 250)");
        if (data.Machines.VariablePower < 1) Add("variablePower", "is under one");

        // Every machine of the items can be made: it names a move, whose type has the materials its tier asks for
        foreach (var item in ItemDatabase.GetAll().Where(TmMaker.Makeable))
        {
            if (item.TeachesMove == null || !MoveDatabase.Has(item.TeachesMove)) { Add(item.Name, $"teaches {item.TeachesMove}, which is no move"); continue; }
            if (tiers.Count > 0 && TmMaker.CostOf(item) == null) Add(item.Name, "has no cost: its move's type has no common material, or its tier a rare one");
        }

        // The kit's recipes (plan 39 · CR3): named once, what they make and take exists, never cheaper than the Mart
        foreach (var (name, what) in Crafting.Problems(data.Kit)) Add(name, what);
        // The camp's dishes (plan 39 · CR6): the plain dish, four climbing grades, needs of the pantry, powers something reads
        foreach (var (name, what) in Meals.Problems(data.Kitchen)) Add(name, what);
    }

    /// <summary>
    /// <c>Data/powers.json</c> (plan 39 · CR6, in plan 36's shape): read strictly; a length of play; each kind's row once,
    /// with three strengths, and every kind something reads has its row.
    /// </summary>
    private static void CheckPowers(GameContent content, List<Finding> found)
    {
        string path = Path.Combine(content.Root, "powers.json");
        if (!File.Exists(path)) return;
        string file = content.Relative(path);
        PowersFile data;
        try
        {
            data = GameDataFiles.ReadStrict<PowersFile>(File.ReadAllText(path), file);
        }
        catch (InvalidDataException e)
        {
            found.Add(new Finding(Kind.Unreadable, file, "powers", e.Message));
            return;
        }
        void Add(string name, string what) => found.Add(new Finding(Kind.Data, file, name, what));
        if (data.Seconds < 1) Add("seconds", "is under one: a power lasts no time");
        foreach (var group in data.Powers.GroupBy(r => r.Kind).Where(g => g.Count() > 1)) Add(group.Key.ToString(), "has two rows");
        foreach (var row in data.Powers)
            if (row.Strength.Count != PowerTable.TopLevel) Add(row.Kind.ToString(), $"has {row.Strength.Count} strengths, not one for each of the {PowerTable.TopLevel} levels");
        foreach (var kind in PowerTable.Read)
            if (data.Powers.All(r => r.Kind != kind)) Add(kind.ToString(), "is read and has no row");
    }

    private static void CheckMaterials(GameContent content, List<Finding> found)
    {
        string path = Path.Combine(content.Root, "materials.json");
        if (!File.Exists(path)) return;
        string file = content.Relative(path);
        MaterialsFile data;
        try
        {
            data = GameDataFiles.ReadStrict<MaterialsFile>(File.ReadAllText(path), file);
        }
        catch (InvalidDataException e)
        {
            found.Add(new Finding(Kind.Unreadable, file, "materials", e.Message));
            return;
        }

        void Add(string name, string what) => found.Add(new Finding(Kind.Data, file, name, what));
        if (data.PouchLimit < 1) Add("pouchLimit", "is under one: the pouch holds nothing");
        foreach (var group in data.Materials.GroupBy(m => m.Id).Where(g => g.Count() > 1))
            Add(group.Key, "is listed twice");
        foreach (var material in data.Materials)
        {
            if (material.Id.Length == 0 || !material.Id.All(char.IsAsciiLetterOrDigit))
                Add(material.Id, "is no id of letters and digits (its words are crafting.material.name.<id>)");
            if (material.Worth < 1) Add(material.Id, "is worth nothing");
            if ((material.Kind == MaterialKind.Pokemon) != (material.Type != null))
                Add(material.Id, material.Kind == MaterialKind.Pokemon ? "is a Pokémon's and names no type" : "names a type, which only a Pokémon's material has");
            if (!Text.Has($"crafting.material.name.{material.Id}")) Add(material.Id, "has no name (crafting.material.name." + material.Id + " in ui.json)");
            if (!Text.Has($"crafting.material.desc.{material.Id}")) Add(material.Id, "has no description (crafting.material.desc." + material.Id + " in ui.json)");
        }
        foreach (var type in Enum.GetValues<PokemonType>())
        {
            var common = data.Materials.FirstOrDefault(m => m.Kind == MaterialKind.Pokemon && m.Type == type && !m.Rare);
            var rare = data.Materials.FirstOrDefault(m => m.Kind == MaterialKind.Pokemon && m.Type == type && m.Rare);
            if (common == null) Add(type.ToString(), "has no common material");
            if (rare == null) Add(type.ToString(), "has no rare material");
            if (common != null && rare != null && rare.Worth <= common.Worth) Add(rare.Id, $"is the rare material of {type} and not worth more than its common one");
        }
    }
}
