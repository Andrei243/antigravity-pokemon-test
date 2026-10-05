using System.Text.Json;

namespace DataImporter;

/// <summary>
/// The pret/pokeplatinum decompilation: Platinum's own species, move and item data as JSON, and the constant lists
/// (<c>generated/*.txt</c>) whose line numbers are the games' ids.
/// </summary>
public sealed class Decomp
{
    private readonly string root;

    public Decomp(string root)
    {
        this.root = root;
        if (!File.Exists(Path.Combine(root, "generated", "species.txt")))
            throw new DirectoryNotFoundException($"No pokeplatinum checkout in {root}");
    }

    /// <summary>A constant list: index i is the constant with id i (<c>SPECIES_NONE</c> is 0).</summary>
    public List<string> Constants(string list) =>
        File.ReadAllLines(Path.Combine(root, "generated", list + ".txt")).Select(l => l.Trim()).Where(l => l.Length > 0).ToList();

    public JsonElement Species(string constant) => Read("res", "pokemon", Folder(constant, "SPECIES_"), "data.json");

    /// <summary>A form's own data, for the forms Platinum keeps it for (Deoxys, Wormadam, Giratina, Shaymin, Rotom).</summary>
    public JsonElement SpeciesForm(string species, string form) => Read("res", "pokemon", species, "forms", form, "data.json");

    /// <summary>
    /// Platinum's Sinnoh Pokédex in order: index i is the species with regional number i. Index 0 holds a species
    /// the regional Pokédex never shows (Arceus), as the original's table does.
    /// </summary>
    public List<string> SinnohPokedex() =>
        Read("res", "pokemon", "sinnoh_pokedex.json").EnumerateArray().Select(e => e.GetString()!).ToList();
    public JsonElement Move(string constant) => Read("res", "moves", Folder(constant, "MOVE_"), "data.json");

    public JsonElement? Item(string constant)
    {
        string path = Path.Combine(root, "res", "items", "data", Folder(constant, "ITEM_") + ".json");
        return File.Exists(path) ? Parse(path) : null;
    }

    private static string Folder(string constant, string prefix) => constant[prefix.Length..].ToLowerInvariant();

    private JsonElement Read(params string[] parts) => Parse(Path.Combine(new[] { root }.Concat(parts).ToArray()));

    private static JsonElement Parse(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { AllowTrailingCommas = true });
        return doc.RootElement.Clone();
    }
}
