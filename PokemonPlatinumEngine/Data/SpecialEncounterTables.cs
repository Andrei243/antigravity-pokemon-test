using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// The region's tables of wild Pokémon that belong to no one area (plan 06 · R13): the file
/// <see cref="WorldEncountersFile"/> that <c>tools/MapImporter --data</c> writes from the original's
/// <c>encdata_ex</c>, read once, with Feebas's tiles taken apart.
/// </summary>
public static class SpecialEncounterTables
{
    private static WorldEncountersFile? sinnoh;
    private static IReadOnlyList<(int X, int Y)>? feebasTiles;

    /// <summary>Sinnoh's tables; empty ones when the file isn't there.</summary>
    public static WorldEncountersFile Sinnoh
    {
        get
        {
            if (sinnoh != null) return sinnoh;
            string path = Path.Combine(World.Folder, "sinnoh", WorldEncountersFile.FileName);
            return sinnoh = File.Exists(GameDataFiles.PathOf(path)) ? GameDataFiles.Load<WorldEncountersFile>(path) : new WorldEncountersFile();
        }
    }

    /// <summary>Every tile of Feebas's lake in the original's order, in tiles of its area's map.</summary>
    public static IReadOnlyList<(int X, int Y)> FeebasTiles => feebasTiles ??= Tiles(Sinnoh.Feebas.Tiles);

    /// <summary>Tiles written <c>"x,z x,z ..."</c>.</summary>
    public static List<(int X, int Y)> Tiles(string text) =>
        text.Split(' ', System.StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split(','))
            .Select(p => (int.Parse(p[0], CultureInfo.InvariantCulture), int.Parse(p[1], CultureInfo.InvariantCulture)))
            .ToList();
}
