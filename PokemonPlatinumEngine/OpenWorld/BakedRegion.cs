using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>
/// A region's <c>baked/index.json</c> (plan 32 · OW2), written by <c>tools/OpenWorldBake</c> and never by hand: what
/// was baked, at which levels, and from which descriptions (<see cref="Source"/>, a hash of their text, which
/// <c>--check</c> tells as stale). <c>docs/data-files.md</c>, "Open worlds", has it.
/// </summary>
public sealed class BakedIndexFile
{
    /// <summary>The chunks' format (<see cref="BakedChunk.Format"/>).</summary>
    public int Format { get; set; }

    /// <summary>The region's id.</summary>
    public string Region { get; set; } = "";

    /// <summary>A chunk's side in millimetres: always <see cref="OpenMap.ChunkSide"/>.</summary>
    public int ChunkSide { get; set; }

    /// <summary>The finest level's cell, in millimetres.</summary>
    public int CellMm { get; set; }

    /// <summary>Each level's bound on how far it stands off the finest, in millimetres (level 0's is nought).</summary>
    public List<int> LevelErrorsMm { get; set; } = new();

    /// <summary>Every chunk baked, as [x, z], west to east and then north to south; each has a file at every level.</summary>
    public List<int[]> Chunks { get; set; } = new();

    /// <summary>A hash of the descriptions the bake was made from (FNV-1a of their text, carriage returns passed over).</summary>
    public string Source { get; set; } = "";
}

/// <summary>
/// A region's baked land as the game loads it (plan 32 · OW2): the index and the chunks under <c>baked/</c>, each read
/// when first asked for and kept. GPU-free, and one thread's: OW8's streamer loads on its worker and hands the chunks
/// over, as <c>MapScene.Prepare</c> does.
/// </summary>
public sealed class BakedRegion
{
    private readonly Dictionary<(int X, int Z, int Level), BakedChunk> loaded = new();
    private readonly HashSet<(int X, int Z)> chunks;

    public string Folder { get; }
    public BakedIndexFile Index { get; }

    private BakedRegion(string folder, BakedIndexFile index)
    {
        Folder = folder;
        Index = index;
        chunks = index.Chunks.Select(c => (c[0], c[1])).ToHashSet();
    }

    /// <summary>A region's <c>baked</c> folder; one with no index, or of another format, is refused.</summary>
    public static BakedRegion Open(string folder)
    {
        string path = Path.Combine(folder, "index.json");
        if (!File.Exists(path)) throw new FileNotFoundException($"no baked index in {folder}: run tools/OpenWorldBake", path);
        var index = JsonSerializer.Deserialize<BakedIndexFile>(File.ReadAllText(path), GameDataFiles.Json)
            ?? throw new InvalidDataException($"{path}: the file is empty");
        if (index.Format != BakedChunk.Format) throw new InvalidDataException($"{path}: format {index.Format}, not {BakedChunk.Format}: bake the region again");
        if (index.ChunkSide != OpenMap.ChunkSide) throw new InvalidDataException($"{path}: chunks of {index.ChunkSide} mm, not {OpenMap.ChunkSide}");
        if (index.Chunks.Any(c => c.Length != 2)) throw new InvalidDataException($"{path}: a chunk is not [x, z]");
        return new BakedRegion(folder, index);
    }

    /// <summary>How many levels each chunk has.</summary>
    public int Levels => Index.LevelErrorsMm.Count;

    /// <summary>Whether the region has a chunk there.</summary>
    public bool Has(int x, int z) => chunks.Contains((x, z));

    /// <summary>The chunks baked, as the index lists them.</summary>
    public IEnumerable<(int X, int Z)> Chunks => Index.Chunks.Select(c => (c[0], c[1]));

    /// <summary>The chunk at a level, read the first time it is asked for; null where the region has none.</summary>
    public BakedChunk? Chunk(int x, int z, int level)
    {
        if (!Has(x, z) || level < 0 || level >= Levels) return null;
        if (loaded.TryGetValue((x, z, level), out var chunk)) return chunk;
        string path = Path.Combine(Folder, BakedChunk.FileName(x, z, level));
        using (var stream = File.OpenRead(path)) chunk = BakedChunk.Read(stream);
        if (chunk.ChunkX != x || chunk.ChunkZ != z || chunk.Level != level)
            throw new InvalidDataException($"{path}: holds chunk {chunk.ChunkX},{chunk.ChunkZ} level {chunk.Level}");
        loaded[(x, z, level)] = chunk;
        return chunk;
    }

    /// <summary>Lets go of every chunk read (they are read again when next asked for).</summary>
    public void Forget() => loaded.Clear();

    /// <summary>
    /// The region's navigation mesh (plan 32 · OW4), <c>baked/navmesh.bin</c>, read the first time it is asked for;
    /// null where the bake has none (a bake of some chunks alone).
    /// </summary>
    public OpenNavMesh? Nav
    {
        get
        {
            if (!navRead) { nav = OpenNavMesh.Open(Folder); navRead = true; }
            return nav;
        }
    }
    private OpenNavMesh? nav;
    private bool navRead;
}
