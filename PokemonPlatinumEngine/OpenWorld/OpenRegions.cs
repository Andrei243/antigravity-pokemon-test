using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>
/// The open maps the game's data makes (plan 33 · GL1, the first region whose file says <c>"field": "open"</c>): each
/// open region's <c>Data/world3d/&lt;folder&gt;/world.json</c>, read for its maps' names alone (cheap, and safe to ask
/// from anywhere: a grid room whose way out leads to an open map is told apart from one that leads to a grid map, which
/// <c>MapDatabase.Get</c> would answer with the map of Sinnoh), and loaded whole with their places and baked land
/// (<see cref="Load"/>, fresh maps each time, as the game's own engine and a test each want theirs, or
/// <see cref="Shared"/>, one set kept for what only reads them: a walk of the whole game). GPU-free.
/// </summary>
public static class OpenRegions
{
    private static readonly object reading = new();
    private static Dictionary<string, string>? names;
    private static Dictionary<string, string>? areas;
    private static Dictionary<string, OpenMap>? shared;

    /// <summary>Where an open region's descriptions and bake live, or null where it has none.</summary>
    public static string? FolderOf(Region region)
    {
        if (region.Field != FieldKind.Open) return null;
        string world = GameDataFiles.PathOf($"world3d/{region.Folder}/world.json");
        return File.Exists(world) ? Path.GetDirectoryName(world) : null;
    }

    /// <summary>Every open map's name, to the id of the region it belongs to.</summary>
    public static IReadOnlyDictionary<string, string> MapNames
    {
        get
        {
            lock (reading)
            {
                if (names != null) return names;
                var found = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var ofArea = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var region in RegionDatabase.All)
                {
                    if (FolderOf(region) is not { } folder) continue;
                    var index = JsonSerializer.Deserialize<OpenRegionIndexFile>(File.ReadAllText(Path.Combine(folder, "world.json")), GameDataFiles.Json);
                    foreach (var map in index?.Maps ?? new())
                    {
                        found[map.Name] = region.Id;
                        foreach (string area in map.Areas) ofArea[area] = map.Name;
                    }
                }
                areas = ofArea;
                return names = found;
            }
        }
    }

    /// <summary>The open map an area of the data's open regions belongs to (an area's key is its script file's name); null for none.</summary>
    public static string? MapOfArea(string area)
    {
        _ = MapNames;
        return areas!.GetValueOrDefault(area);
    }

    /// <summary>Whether a name is an open map's.</summary>
    public static bool IsOpenMap(string? name) => name != null && MapNames.ContainsKey(name);

    /// <summary>The open maps' names of one region, in the order its file lists them.</summary>
    public static IReadOnlyList<string> NamesOf(string region) =>
        MapNames.Where(m => string.Equals(m.Value, region, StringComparison.OrdinalIgnoreCase)).Select(m => m.Key).ToList();

    /// <summary>An open region's maps read afresh, with their places and (where it is baked) their land.</summary>
    public static List<OpenMap> Load(Region region, bool withLand = true) =>
        FolderOf(region) is { } folder ? OpenPlaceFiles.LoadRegion(folder, withLand) : new List<OpenMap>();

    /// <summary>
    /// One open map of the data's, kept for everyone who only reads it (the walk of the whole game, the content check's
    /// questions); null for a name that is no open map. Its baked chunks are read when first asked for, so whoever
    /// walks it holds <see cref="Reading"/> while they do.
    /// </summary>
    public static OpenMap? Shared(string name)
    {
        lock (reading)
        {
            if (shared == null)
            {
                shared = new Dictionary<string, OpenMap>(StringComparer.OrdinalIgnoreCase);
                foreach (var region in RegionDatabase.All)
                    foreach (var map in Load(region)) shared[map.Name] = map;
            }
            return shared.GetValueOrDefault(name);
        }
    }

    /// <summary>The lock a reader of a <see cref="Shared"/> map's land holds (its baked chunks are one thread's cache).</summary>
    public static object Reading => reading;
}
