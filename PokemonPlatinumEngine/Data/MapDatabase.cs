using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// Every map: the hand-made ones, read from the files in <c>Data/maps</c> (one <see cref="MapFile"/> per map), and
/// the maps of the imported world, made from <c>Data/world</c> (see <see cref="World"/>).
/// </summary>
public static partial class MapDatabase
{
    public const string Folder = "maps";

    /// <summary>The map of Sinnoh's overworld; also what an unknown map name falls back to.</summary>
    public const string StartMap = "Sinnoh";

    // Replaced as a whole on reload, so a reader never sees a half-loaded set
    private static volatile Dictionary<string, Map>? maps;

    private static Dictionary<string, Map> Maps => maps ?? Load();

    /// <summary>(Re)loads every map from its file, with fresh NPCs and trainers.</summary>
    public static void Initialize() => Load();

    private static Dictionary<string, Map> Load()
    {
        string folder = GameDataFiles.PathOf(Folder);
        if (!Directory.Exists(folder))
            throw new DirectoryNotFoundException($"Map folder not found: {folder}");

        var loaded = new Dictionary<string, Map>(System.StringComparer.OrdinalIgnoreCase);
        foreach (string path in Directory.GetFiles(folder, "*.json").OrderBy(p => p, System.StringComparer.Ordinal))
        {
            var file = GameDataFiles.Load<MapFile>(Path.Combine(Folder, Path.GetFileName(path)));
            string expected = Path.GetFileNameWithoutExtension(path);
            if (file.Name != expected)
                throw new InvalidDataException($"Map file {path} names its map '{file.Name}'; the file must be called {file.Name}.json.");
            loaded[file.Name] = file.ToMap();
        }

        foreach (var world in World.LoadAll())
        {
            foreach (var map in world.BuildMaps())
            {
                if (loaded.ContainsKey(map.Name))
                    throw new InvalidDataException($"The world of {world.Index.Region} makes a map called {map.Name}, and so does {folder}.");
                loaded[map.Name] = map;
            }
        }
        if (!loaded.ContainsKey(StartMap))
            throw new InvalidDataException($"No map is called {StartMap}: the world of Sinnoh is missing from {GameDataFiles.PathOf(World.Folder)}.");

        maps = loaded;
        return loaded;
    }

    public static Map Get(string name)
    {
        var all = Maps;
        if (all.TryGetValue(name, out var map)) return map;
        return all[StartMap];
    }

    public static IReadOnlyCollection<string> MapNames => Maps.Keys;
}
