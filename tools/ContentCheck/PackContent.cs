using PokemonPlatinumEngine.Data;

namespace ContentCheck;

/// <summary>
/// The content check over the game with packs laid on it (plan 16 · T15): what the game itself reads of the
/// hand-written files (<c>scripts/</c>, <c>maps/</c>, <c>world/</c>, <c>text/</c>) copied into a folder with each pack's
/// files of the same relative name laid over them in load order, as <see cref="DataLayers.FilesIn"/> and
/// <see cref="DataLayers.PathOf"/> lay them in the game (a region's <c>world.json</c> stays the game's, and a pack's
/// <c>pack.json</c>, patches and <c>overrides/</c> are no data files of the folder). The checks are run over both, and
/// what the packs add is what is found with them that isn't found without: a pack is answerable for its own findings,
/// never for the game's.
/// </summary>
public static class PackContent
{
    /// <summary>The folders of a data folder the checks read, and so the ones a merge copies.</summary>
    public static readonly string[] Folders = { "scripts", "maps", "world", "text" };

    /// <summary>Lays the game's checked folders and the packs' files over them into <paramref name="into"/> (emptied first).</summary>
    public static void Merge(string gameRoot, IReadOnlyList<DataLayer> packs, string into)
    {
        if (Directory.Exists(into)) Directory.Delete(into, recursive: true);
        Directory.CreateDirectory(into);
        foreach (string folder in Folders) CopyTree(Path.Combine(gameRoot, folder), Path.Combine(into, folder), fromPack: false);
        foreach (var pack in packs)
            foreach (string folder in Folders) CopyTree(Path.Combine(pack.Root, folder), Path.Combine(into, folder), fromPack: true);
    }

    private static void CopyTree(string from, string to, bool fromPack)
    {
        if (!Directory.Exists(from)) return;
        foreach (string path in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(from, path);
            if (fromPack && (DataLayers.IsFixed(relative) || path.EndsWith(DataLayers.PatchSuffix, StringComparison.OrdinalIgnoreCase))) continue;
            string target = Path.Combine(to, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(path, target, overwrite: true);
        }
    }

    /// <summary>What is found with the packs and not without them (the same file, name and words), in the order found.</summary>
    public static Findings Added(Findings without, Findings with)
    {
        var known = new HashSet<string>(without.Select(f => f.ToString()), StringComparer.Ordinal);
        return new Findings(with.Where(f => !known.Contains(f.ToString())));
    }

    /// <summary>
    /// Reads the packs of these folders (each a pack with its <c>pack.json</c>), in the order given; a folder that is
    /// no pack throws <see cref="PackException"/>.
    /// </summary>
    public static List<DataLayer> Read(IEnumerable<string> folders) =>
        folders.Select(f => new DataLayer(Path.GetFullPath(f), DataLayers.ReadManifest(f))).ToList();

    /// <summary>
    /// The checks over the game with these packs on it, in a scratch folder of its own that is taken away after:
    /// the findings the packs add. The packs' patches are put in use for the run (<see cref="DataLayers.Use"/>), so the
    /// species, moves, items and trainers the files are held against are the patched ones; nothing is put back, since
    /// the tool is a process of its own (a test runs it alone).
    /// </summary>
    public static Findings Check(string gameRoot, IReadOnlyList<DataLayer> packs, string scratch)
    {
        DataLayers.Use(packs);
        PokemonPlatinumEngine.Audio.MusicLibrary.Reload(); // the packs' songs are songs a place may name
        var without = Checks.All(GameContent.Load(gameRoot));
        try
        {
            Merge(gameRoot, packs, scratch);
            return Added(without, Checks.All(GameContent.Load(scratch)));
        }
        finally
        {
            if (Directory.Exists(scratch)) Directory.Delete(scratch, recursive: true);
        }
    }
}
