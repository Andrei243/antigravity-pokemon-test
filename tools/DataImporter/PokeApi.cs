using PokemonPlatinumEngine.Data;

namespace DataImporter;

/// <summary>
/// The PokeAPI CSV files (<c>data/v2/csv</c>), indexed for the importer. English text is language 9. PokeAPI is
/// BSD-licensed; see the importer's README.
/// </summary>
public sealed class PokeApi
{
    public const int English = 9;

    private readonly string folder;
    private readonly Dictionary<string, CsvTable> tables = new();

    public PokeApi(string folder)
    {
        this.folder = folder;
        if (!File.Exists(Path.Combine(folder, "pokemon_species.csv")))
            throw new DirectoryNotFoundException($"No PokeAPI CSV files in {folder}");
    }

    public IReadOnlyList<CsvRow> Table(string name)
    {
        if (!tables.TryGetValue(name, out var table))
        {
            table = CsvTable.Load(Path.Combine(folder, name + ".csv"));
            tables[name] = table;
        }
        return table.Rows;
    }

    private Dictionary<int, CsvRow> ById(string table, string column = "id") =>
        Table(table).GroupBy(r => r.Int(column)).ToDictionary(g => g.Key, g => g.First());

    private Dictionary<int, string> EnglishNames(string table, string idColumn, string nameColumn = "name") =>
        Table(table).Where(r => r.Int("local_language_id") == English)
            .GroupBy(r => r.Int(idColumn)).ToDictionary(g => g.Key, g => Names.Clean(g.First()[nameColumn]));

    // ------------------------------------------------------------------ lookups, built on first use

    private Dictionary<int, string>? typeIds, speciesNames, genera, moveNames, abilityNames, itemNames, locationNames;
    private Dictionary<int, string>? identifiers_eggGroups, identifiers_colors, identifiers_shapes, identifiers_growth;
    private Dictionary<int, CsvRow>? species, defaultPokemon, moves, meta, items, abilities;

    public Dictionary<int, CsvRow> Species => species ??= ById("pokemon_species");
    public Dictionary<int, CsvRow> Moves => moves ??= ById("moves");
    public Dictionary<int, CsvRow> MoveMeta => meta ??= ById("move_meta", "move_id");
    public Dictionary<int, CsvRow> Items => items ??= ById("items");
    public Dictionary<int, CsvRow> Abilities => abilities ??= ById("abilities");

    /// <summary>The default form of each species (the <c>pokemon</c> row with <c>is_default</c>).</summary>
    public Dictionary<int, CsvRow> DefaultPokemon => defaultPokemon ??=
        Table("pokemon").Where(r => r.Bool("is_default")).ToDictionary(r => r.Int("species_id"));

    public string SpeciesName(int id) => (speciesNames ??= EnglishNames("pokemon_species_names", "pokemon_species_id"))[id];
    public string Genus(int id) => (genera ??= EnglishNames("pokemon_species_names", "pokemon_species_id", "genus")).GetValueOrDefault(id, "");
    public string MoveName(int id) => (moveNames ??= EnglishNames("move_names", "move_id"))[id];
    public string AbilityName(int id) => (abilityNames ??= EnglishNames("ability_names", "ability_id"))[id];
    public string ItemName(int id) => (itemNames ??= EnglishNames("item_names", "item_id"))[id];
    public string? LocationName(int id) => (locationNames ??= EnglishNames("location_names", "location_id")).GetValueOrDefault(id);
    public bool HasItemName(int id) => (itemNames ??= EnglishNames("item_names", "item_id")).ContainsKey(id);

    public PokemonType Type(int typeId)
    {
        typeIds ??= Table("types").ToDictionary(r => r.Int("id"), r => r["identifier"]);
        return Enum.Parse<PokemonType>(typeIds[typeId], ignoreCase: true);
    }

    public string EggGroup(int id) => Names.Title((identifiers_eggGroups ??= Identifiers("egg_groups"))[id]);
    public string Color(int id) => Names.Title((identifiers_colors ??= Identifiers("pokemon_colors"))[id]);
    public string Shape(int id) => Names.Title((identifiers_shapes ??= Identifiers("pokemon_shapes"))[id]);

    public GrowthRate Growth(int id) => (identifiers_growth ??= Identifiers("growth_rates"))[id] switch
    {
        "slow" => GrowthRate.Slow,
        "medium" => GrowthRate.MediumFast,
        "fast" => GrowthRate.Fast,
        "medium-slow" => GrowthRate.MediumSlow,
        "slow-then-very-fast" => GrowthRate.Erratic,
        "fast-then-very-slow" => GrowthRate.Fluctuating,
        var other => throw new InvalidDataException($"Unknown growth rate {other}")
    };

    private Dictionary<int, string> Identifiers(string table) => Table(table).ToDictionary(r => r.Int("id"), r => r["identifier"]);

    /// <summary>Short English prose for a move effect, ability or item, with PokeAPI's link markup removed.</summary>
    public string Prose(string table, string idColumn, int id, int? effectChance = null)
    {
        var row = Table(table).FirstOrDefault(r => r.Int(idColumn) == id && r.Int("local_language_id") == English);
        if (row == null) return "";
        string text = Names.StripMarkup(row["short_effect"]);
        if (effectChance != null) text = text.Replace("$effect_chance", effectChance.Value.ToString());
        return text;
    }

    /// <summary>The generation an item first appeared in, from the games' item indices.</summary>
    public int ItemGeneration(int itemId)
    {
        itemGenerations ??= Table("item_game_indices").GroupBy(r => r.Int("item_id")).ToDictionary(g => g.Key, g => g.Min(r => r.Int("generation_id")));
        return itemGenerations.GetValueOrDefault(itemId);
    }

    private Dictionary<int, int>? itemGenerations;

    /// <summary>The PokeAPI item that has this index in the Generation 4 games.</summary>
    public int? ItemByGen4Index(int index)
    {
        gen4Items ??= Table("item_game_indices").Where(r => r.Int("generation_id") == 4)
            .GroupBy(r => r.Int("game_index")).ToDictionary(g => g.Key, g => g.Min(r => r.Int("item_id")));
        return gen4Items.TryGetValue(index, out int id) ? id : null;
    }

    private Dictionary<int, int>? gen4Items;
}
