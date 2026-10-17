using System.Collections.Concurrent;
using System.Text.Json;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// The names of species, categories, forms, moves, abilities, items, types, natures and places in the language the
/// options chose (plan 17 · N3). A language's <c>Data/text/&lt;tag&gt;/names.json</c> is written by <c>tools/DataImporter</c>
/// from PokeAPI's own tables, each entry keyed by the English name the data, the save and the rules go by, so a name
/// is only ever <em>shown</em> through here: <see cref="Models.PokemonSpecies.DisplayName"/> and its fellows. A name
/// the language's file lacks is the English one (Romanian has no file at all, PokeAPI having no Romanian), and the
/// pseudo-language brackets every name as <see cref="Text"/> brackets every string. GPU-free.
/// </summary>
public static class LocalNames
{
    public const string FileName = "names.json";

    private sealed class Names
    {
        public Dictionary<string, string> Species = new(), Categories = new(), Forms = new(), Moves = new(), Abilities = new(),
            Items = new(), Types = new(), Natures = new(), Places = new(), MoveText = new(), AbilityText = new(), ItemText = new();
    }

    private static readonly ConcurrentDictionary<string, Names?> Loaded = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Whether the chosen language is the pseudo-language, which brackets every name.</summary>
    private static bool Bracketed => Text.Language.Pseudo;

    /// <summary>The names file of a language, or null where it has none (English, the pseudo-language, Romanian).</summary>
    private static Names? For(string tag) => Loaded.GetOrAdd(tag, Read);

    private static Names? Current => Text.Language.Pseudo || Text.Current == Text.English ? null : For(Text.Current);

    /// <summary>Whether a language has a names file of its own.</summary>
    public static bool Has(string tag) => tag != Text.English && For(tag) != null;

    /// <summary>The entries of one section of a language's file, keyed by English name (nothing for a language with no file).</summary>
    public static IReadOnlyDictionary<string, string> Section(string tag, string section)
    {
        var names = For(tag);
        return names == null ? new Dictionary<string, string>() : Pick(names, section);
    }

    /// <summary>The sections of a names file, in the order the importer writes them.</summary>
    public static readonly IReadOnlyList<string> Sections = new[] { "species", "categories", "forms", "moves", "abilities", "items", "types", "natures", "places" };

    /// <summary>The descriptions blocks of a names file.</summary>
    public static readonly IReadOnlyList<string> DescriptionSections = new[] { "moves", "abilities", "items" };

    private static Dictionary<string, string> Pick(Names n, string section) => section switch
    {
        "species" => n.Species, "categories" => n.Categories, "forms" => n.Forms, "moves" => n.Moves, "abilities" => n.Abilities,
        "items" => n.Items, "types" => n.Types, "natures" => n.Natures, "places" => n.Places,
        "descriptions.moves" => n.MoveText, "descriptions.abilities" => n.AbilityText, "descriptions.items" => n.ItemText,
        _ => throw new ArgumentException($"No section {section} in a names file")
    };

    // ------------------------------------------------------------------ what is shown

    /// <summary>A species' name: <paramref name="english"/> in English, its own in a language that names it.</summary>
    public static string Species(string english) => Show(Current?.Species, english);

    /// <summary>A form's name: its own where the language names it, else the species' name in front of the English form's words (<c>Dracaufeu-Mega-X</c>).</summary>
    public static string Form(string formName, string speciesName)
    {
        var names = Current;
        if (names != null && names.Forms.TryGetValue(formName, out var own)) return Wrap(own);
        if (names != null && formName.StartsWith(speciesName + "-", StringComparison.Ordinal) && names.Species.TryGetValue(speciesName, out var species))
            return Wrap(species + formName[speciesName.Length..]);
        return Wrap(formName);
    }

    /// <summary>
    /// The whole phrase a species' kind is shown as ("Seed Pokémon"): the language's own category where the importer
    /// found one (PokeAPI's genus, whole), else the language's <c>pokedex.kind</c> around the English category.
    /// </summary>
    public static string Kind(string speciesName, string englishCategory)
    {
        var names = Current;
        if (names != null && names.Categories.TryGetValue(speciesName, out var own)) return own;
        return Text.Of("pokedex.kind", ("category", englishCategory));
    }

    /// <summary>A species' category as the <c>{category}</c> of the language's own kind phrase: the language's genus whole, or the English word.</summary>
    public static string Category(string speciesName, string englishCategory) =>
        Current != null && Current.Categories.TryGetValue(speciesName, out var own) ? own : englishCategory;

    public static string Move(string english) => Show(Current?.Moves, english);
    public static string Ability(string english) => Show(Current?.Abilities, english);
    public static string Item(string english) => Show(Current?.Items, english);
    public static string Place(string english) => Show(Current?.Places, english);

    /// <summary>
    /// A place's name as it is shown (plan 17 · N4), by the first of these a language has: the place's own file
    /// (<c>maps/&lt;Name&gt;.json</c> or an overlay's, <paramref name="place"/> being its map's name or area's key), PokeAPI's
    /// name for it (<c>names.json</c>), the <c>places</c> section of the language's <c>ui.json</c> for a place PokeAPI
    /// doesn't name, and the English. The pseudo-language brackets the English. <c>{player}</c> is left for the caller to fill.
    /// </summary>
    public static string PlaceShown(string? place, string english)
    {
        if (english.Length == 0) return english;
        if (Bracketed) return $"[{english}]";
        if (Text.PlaceName(place, english) is { } own) return own;
        if (Current?.Places is { } places && places.TryGetValue(english, out var named)) return named;
        string name = Models.Trainer.KeyOf(english);
        string key = $"places.{name}";
        return Text.HasOwn(key) ? Text.Of(key) : english;
    }

    /// <summary>A type's name in the language's names file; null where it has none (the string table then names it).</summary>
    public static string? TypeName(PokemonType type) => Current != null && Current.Types.TryGetValue(type.ToString(), out var n) ? n : null;

    /// <summary>A nature's name in the language's names file; null where it has none.</summary>
    public static string? NatureName(Nature nature) => Current != null && Current.Natures.TryGetValue(nature.ToString(), out var n) ? n : null;

    /// <summary>A move's description: PokeAPI's own short prose where the language has it, else the English one.</summary>
    public static string MoveDescription(string move, string english) => Show(Current?.MoveText, move, english);
    public static string AbilityDescription(string ability, string english) => Show(Current?.AbilityText, ability, english);
    public static string ItemDescription(string item, string english) => Show(Current?.ItemText, item, english);

    private static string Show(Dictionary<string, string>? table, string english) => Show(table, english, english);

    private static string Show(Dictionary<string, string>? table, string key, string english) =>
        Wrap(table != null && table.TryGetValue(key, out var own) ? own : english);

    private static string Wrap(string name) => Bracketed ? $"[{name}]" : name;

    // ------------------------------------------------------------------ the files

    private static Names? Read(string tag)
    {
        if (tag == Text.English) return null;
        string path = GameDataFiles.PathOf(Path.Combine("text", tag, FileName));
        if (!File.Exists(path)) return null;
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var names = new Names();
        foreach (var section in document.RootElement.EnumerateObject())
        {
            if (section.Name == "descriptions")
            {
                foreach (var d in section.Value.EnumerateObject()) Fill(Pick(names, "descriptions." + d.Name), d.Value);
                continue;
            }
            Fill(Pick(names, section.Name), section.Value);
        }
        return names;
    }

    private static void Fill(Dictionary<string, string> into, JsonElement section)
    {
        foreach (var entry in section.EnumerateObject()) into[entry.Name] = entry.Value.GetString() ?? "";
    }
}
