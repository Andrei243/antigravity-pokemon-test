using System.Text.Json;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// The first towns and routes as they were hand-made before Sinnoh was imported (plan 01 · M2), and Jubilife
/// City as it was until its area was opened (plan 01 · M5), kept in
/// <c>Fixtures/maps</c> for the tests of the map-file format and of the field's art kits: small maps whose every
/// tile those tests know (a town with fenced gardens, a lake with boulders in it). The game no longer loads them.
/// </summary>
internal static class Fixtures
{
    public static readonly string[] Names = { "TwinleafTown", "Route201", "LakeVerity", "SandgemTown", "Route202", "JubilifeCity" };

    /// <summary>A fresh copy of one of the old maps.</summary>
    public static Map Map(string name)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "maps", name + ".json");
        return JsonSerializer.Deserialize<MapFile>(File.ReadAllText(path), GameDataFiles.Json)!.ToMap();
    }

    /// <summary>A map by name: one of the old maps if it names one, the game's own otherwise.</summary>
    public static Map Any(string name) => Names.Contains(name) ? Map(name) : MapDatabase.Get(name);
}
