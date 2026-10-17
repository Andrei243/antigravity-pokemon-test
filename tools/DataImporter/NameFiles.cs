using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace DataImporter;

/// <summary>
/// The names of the game's species, moves, abilities, items, types and natures in every language PokeAPI names them
/// in (plan 17 · N3): one <c>Data/text/&lt;tag&gt;/names.json</c> for each language of <see cref="PokeApi.Languages"/>, every
/// entry keyed by the English name the data and the save go by. Names are PokeAPI's own tables (the same data and
/// licence as the English names imported already); nothing of the games' text is read, and a descriptions block holds
/// PokeAPI's contributors' short prose where the language has it (French for moves, abilities and items, German for
/// part of the abilities). Romanian has no file, because PokeAPI has no Romanian: its names are the English ones.
/// </summary>
public static class NameFiles
{
    public const string FileName = "names.json";

    private static readonly JsonSerializerOptions Quote = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>One language's file as text.</summary>
    public static string Build(PokeApi api, int[] languages, IReadOnlyList<PokemonSpecies> species, IReadOnlyList<MoveData> moves,
        IReadOnlyList<AbilityDatabase.AbilityRecord> abilities, IReadOnlyList<ItemData> items)
    {
        var sections = new List<(string Name, List<(string Key, string Value)> Entries)>();

        // Species, and their categories (PokeAPI's genus whole: "Pokémon Graine"), by the English name
        var speciesNames = api.LocalNames("pokemon_species_names", "pokemon_species_id", languages);
        var genera = api.LocalNames("pokemon_species_names", "pokemon_species_id", languages, "genus");
        sections.Add(("species", species.Where(s => speciesNames.ContainsKey(s.DexNumber)).Select(s => (s.Name, speciesNames[s.DexNumber])).ToList()));
        sections.Add(("categories", species.Where(s => genera.ContainsKey(s.DexNumber)).Select(s => (s.Name, genera[s.DexNumber])).ToList()));
        sections.Add(("forms", Forms(api, languages, species, speciesNames)));

        var moveIds = api.IdsByEnglishName("move_names", "move_id");
        var moveNames = api.LocalNames("move_names", "move_id", languages);
        sections.Add(("moves", Named(moves.Select(m => m.Name), moveIds, moveNames)));

        var abilityIds = api.IdsByEnglishName("ability_names", "ability_id");
        var abilityNames = api.LocalNames("ability_names", "ability_id", languages);
        sections.Add(("abilities", Named(abilities.Select(a => a.Name), abilityIds, abilityNames)));

        var itemIds = api.IdsByEnglishName("item_names", "item_id");
        var itemNames = api.LocalNames("item_names", "item_id", languages);
        sections.Add(("items", Named(items.Select(i => i.Name), itemIds, itemNames)));

        // The eighteen types and the twenty-five natures, by the names the engine's enums spell
        var typeNames = api.LocalNames("type_names", "type_id", languages);
        var types = new List<(string, string)>();
        foreach (var (id, local) in typeNames.OrderBy(t => t.Key))
        {
            try { types.Add((api.Type(id).ToString(), local)); }
            catch (ArgumentException) { } // the ??? type, shadow, Stellar: none of which this game shows
        }
        sections.Add(("types", types));
        var natureIds = api.IdsByEnglishName("nature_names", "nature_id");
        var natureNames = api.LocalNames("nature_names", "nature_id", languages);
        sections.Add(("natures", Enum.GetNames<Nature>().Where(n => natureIds.ContainsKey(n) && natureNames.ContainsKey(natureIds[n])).Select(n => (n, natureNames[natureIds[n]])).ToList()));

        // Places by PokeAPI's English name for them
        var locationIds = api.IdsByEnglishName("location_names", "location_id");
        var locationNames = api.LocalNames("location_names", "location_id", languages);
        sections.Add(("places", locationIds.Where(p => locationNames.ContainsKey(p.Value)).OrderBy(p => p.Value).Select(p => (p.Key, locationNames[p.Value])).ToList()));

        // What PokeAPI's contributors wrote of a move, an ability or an item in the language, where there is any
        var moveText = new List<(string, string)>();
        foreach (var m in moves)
        {
            if (!moveIds.TryGetValue(m.Name, out int id) || !api.Moves.TryGetValue(id, out var row) || row["effect_id"] == "") continue;
            string? text = api.LocalProse("move_effect_prose", "move_effect_id", row.Int("effect_id"), languages, row.IntOrNull("effect_chance"));
            if (text != null) moveText.Add((m.Name, text));
        }
        var abilityText = new List<(string, string)>();
        foreach (var a in abilities)
            if (abilityIds.TryGetValue(a.Name, out int id) && api.LocalProse("ability_prose", "ability_id", id, languages) is { } text) abilityText.Add((a.Name, text));
        var itemText = new List<(string, string)>();
        foreach (var i in items.Where(i => i.TeachesMove == null))
            if (itemIds.TryGetValue(i.Name, out int id) && api.LocalProse("item_prose", "item_id", id, languages) is { } text) itemText.Add((i.Name, text));

        var text2 = new StringBuilder("{\n");
        void Section(string name, List<(string Key, string Value)> entries, string indent, bool last)
        {
            text2.Append(indent).Append('"').Append(name).Append("\": {\n");
            for (int i = 0; i < entries.Count; i++)
                text2.Append(indent).Append("  ").Append(Json(entries[i].Key)).Append(": ").Append(Json(entries[i].Value)).Append(i < entries.Count - 1 ? ",\n" : "\n");
            text2.Append(indent).Append(last ? "}\n" : "},\n");
        }
        foreach (var (name, entries) in sections) Section(name, entries, "  ", false);
        text2.Append("  \"descriptions\": {\n");
        Section("moves", moveText, "    ", false);
        Section("abilities", abilityText, "    ", false);
        Section("items", itemText, "    ", true);
        text2.Append("  }\n}\n");
        return text2.ToString();
    }

    /// <summary>The entries of a section, in the data's order: each English name that PokeAPI has an id and a local name for.</summary>
    private static List<(string Key, string Value)> Named(IEnumerable<string> english, Dictionary<string, int> ids, Dictionary<int, string> local) =>
        english.Where(n => ids.TryGetValue(n, out int id) && local.ContainsKey(id)).Select(n => (n, local[ids[n]])).ToList();

    /// <summary>
    /// The forms PokeAPI names in the language (<c>pokemon_form_names</c>' whole name for the Pokémon in it): Burmy's cloaks,
    /// Deoxys's formes. The rest are left to <c>LocalNames.Form</c>, which puts the species' name in front of the English
    /// form's words.
    /// </summary>
    private static List<(string Key, string Value)> Forms(PokeApi api, int[] languages, IReadOnlyList<PokemonSpecies> species, Dictionary<int, string> speciesNames)
    {
        var pokemon = api.Table("pokemon").ToDictionary(r => r.Int("id"));
        var byName = species.ToDictionary(s => s.DexNumber);
        var formNames = api.LocalNames("pokemon_form_names", "pokemon_form_id", languages, "pokemon_name");
        var result = new List<(string, string)>();
        foreach (var f in api.Table("pokemon_forms"))
        {
            if (!formNames.TryGetValue(f.Int("id"), out var local)) continue;
            int speciesId = pokemon[f.Int("pokemon_id")].Int("species_id");
            if (!byName.TryGetValue(speciesId, out var s) || s.Forms == null) continue;
            string identifier = api.Species[speciesId]["identifier"];
            string suffix = f["identifier"].StartsWith(identifier + "-") ? f["identifier"][(identifier.Length + 1)..] : f["form_identifier"];
            if (suffix.Length == 0) continue;
            string name = s.Name + "-" + string.Join("-", suffix.Split('-').Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
            if (s.Forms.Any(x => x.Name == name) && !result.Any(r => r.Item1 == name)) result.Add((name, local));
        }
        return result.OrderBy(r => r.Item1, StringComparer.Ordinal).ToList();
    }

    private static string Json(string text) => JsonSerializer.Serialize(text, Quote);
}
