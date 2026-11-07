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
