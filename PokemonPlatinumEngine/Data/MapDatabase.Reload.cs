using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Data;

public static partial class MapDatabase
{
    /// <summary>
    /// Reads the maps again while the game runs (plan 16 · T6, the developer's live reload): only the room files named
    /// in <paramref name="rooms"/> (full paths of <c>Data/maps/&lt;Name&gt;.json</c>) when every change was one of those,
    /// otherwise every map, the imported worlds with their overlays among them. Nothing is changed unless all of it
    /// reads: what was wrong is handed back, a line to each problem, and the maps stay as they were. The maps read are
    /// fresh ones, with their people where their files stand them; whoever holds a map of the old set (the engine's
    /// map, a renderer's chunk) swaps it for its namesake (<see cref="TryGet"/>) itself.
    /// </summary>
    public static List<string> Reload(IEnumerable<string>? rooms = null)
    {
        try
        {
            var only = rooms?.ToList();
            if (only is { Count: > 0 } && maps is { } current)
            {
                var copy = new Dictionary<string, Map>(current, StringComparer.OrdinalIgnoreCase);
                foreach (string path in only)
                {
                    // (GameDataFiles reads a full path as it is)
                    var file = GameDataFiles.Load<MapFile>(path);
                    string expected = Path.GetFileNameWithoutExtension(path);
                    if (file.Name != expected)
                        throw new InvalidDataException($"Map file {path} names its map '{file.Name}'; the file must be called {file.Name}.json.");
                    copy[file.Name] = file.ToMap();
                }
                maps = copy;
                return new List<string>();
            }
            Load();
            return new List<string>();
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            return new List<string> { e.Message };
        }
    }
}
