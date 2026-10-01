using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Audio;

/// <summary>
/// Every song in <c>Data/music</c>, read once. A song's id is its path there without the extension:
/// <c>common/battle_wild</c>, <c>kanto/pallet</c>, <c>sinnoh/twinleaf</c>. Each region has a folder for its own
/// themes; <c>common</c> holds the ones every region shares until it gets its own.
/// </summary>
public static class MusicLibrary
{
    private static Dictionary<string, Song>? songs;
    private static readonly object loadLock = new();

    public static string Directory => GameDataFiles.PathOf("music");

    private static Dictionary<string, Song> Songs
    {
        get
        {
            lock (loadLock)
            {
                return songs ??= Load();
            }
        }
    }

    public static IReadOnlyCollection<string> Ids => Songs.Keys;

    public static IEnumerable<Song> All => Songs.Values;

    public static bool Exists(string id) => Songs.ContainsKey(id);

    public static Song? Get(string id) => Songs.TryGetValue(id, out var s) ? s : null;

    private static Dictionary<string, Song> Load()
    {
        var result = new Dictionary<string, Song>(StringComparer.OrdinalIgnoreCase);
        if (!System.IO.Directory.Exists(Directory)) return result;
        foreach (var path in System.IO.Directory.EnumerateFiles(Directory, "*.mml", SearchOption.AllDirectories).OrderBy(p => p))
        {
            string id = Path.ChangeExtension(Path.GetRelativePath(Directory, path), null).Replace('\\', '/');
            result[id] = Mml.Parse(id, File.ReadAllText(path));
        }
        return result;
    }
}
