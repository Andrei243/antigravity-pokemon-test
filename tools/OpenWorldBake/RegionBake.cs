using System.Text.Json;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.OpenWorld;

namespace OpenWorldBake;

/// <summary>
/// A region's bake (plan 32 · OW2): every chunk of its extent meshed by surface nets at half a metre (level 0) and
/// thinned twice (levels 1 and 2, decision 16), each with its cover, normals, apron, skirts and water, written as
/// <see cref="BakedChunk"/>s under <c>baked/</c> beside an index (<see cref="BakedIndexFile"/>). Each chunk is baked
/// alone and single-threaded; the chunks are baked side by side, which changes nothing of their bytes.
/// </summary>
public static class RegionBake
{
    /// <summary>Level 0's cell, in metres.</summary>
    public const double Cell = 0.5;

    /// <summary>Levels 1 and 2: how far each may stand off level 0, and whether the grasses (and the rocks) may be joined.</summary>
    public static readonly Thinning.Level[] Thinned = { new(0.06, false), new(0.3, true) };

    /// <summary>How far each level's skirts hang, in metres: the finest's only (decision 13); a thinned level hangs none.</summary>
    public static readonly double[] Skirts = { 0.5, 0, 0 };

    public static int Levels => Thinned.Length + 1;

    /// <summary>The region's chunks, as OpenMap.ChunkOf counts them, west to east and then north to south.</summary>
    public static IEnumerable<(int X, int Z)> ChunksOf(OpenWorldFile world)
    {
        for (int z = world.Chunks[1]; z <= world.Chunks[3]; z++)
            for (int x = world.Chunks[0]; x <= world.Chunks[2]; x++)
                yield return (x, z);
    }

    /// <summary>A chunk's three levels as ChunkMeshes (what --seams and the pictures read), the finest first.</summary>
    public static ChunkMesh[] Meshes(TerrainWorld land, OpenWorldFile world, int x, int z)
    {
        int cx = x - world.Chunks[0], cz = z - world.Chunks[1];
        var net = SurfaceNets.Net(land, cx, cz, Cell);
        var finest = SurfaceNets.Finish(land, net, 0, Skirts[0]);
        int n = net.VertexCount;
        var normals = new D3[n];
        for (int v = 0; v < n; v++) normals[v] = new D3(finest.Normals[v * 3], finest.Normals[v * 3 + 1], finest.Normals[v * 3 + 2]);
        var covers = finest.Covers[..n];
        var meshes = new ChunkMesh[Levels];
        meshes[0] = finest;
        var thinned = Thinning.Thin(net, covers, Thinned);
        for (int l = 0; l < thinned.Count; l++)
        {
            var (chunk, source) = thinned[l];
            // A thinned level keeps the finest's border rows, so it meets a neighbour of any level on the very same
            // vertices: it needs no apron and no skirts (the finest keeps both, which a neighbour's thinned border
            // rows lie exactly under)
            chunk = new RawChunk
            {
                ChunkX = chunk.ChunkX, ChunkZ = chunk.ChunkZ, Cell = chunk.Cell, N = chunk.N,
                Positions = chunk.Positions, Cells = chunk.Cells, Ground = chunk.Ground, Apron = new List<int>()
            };
            meshes[l + 1] = SurfaceNets.Finish(land, chunk, l + 1, Skirts[l + 1],
                source.Select(s => normals[s]).ToArray(), source.Select(s => covers[s]).ToArray(), Thinned[l].MaxError);
        }
        return meshes;
    }

    /// <summary>
    /// A chunk's level 0 alone as the game loads it (plan 32 · OW3): what the capsule collides with, without the
    /// thinning of the far levels; the same bytes as <see cref="Chunk"/>'s first.
    /// </summary>
    public static BakedChunk Finest(TerrainWorld land, OpenWorldFile world, int x, int z)
    {
        int cx = x - world.Chunks[0], cz = z - world.Chunks[1];
        var net = SurfaceNets.Net(land, cx, cz, Cell);
        return ToBaked(SurfaceNets.Finish(land, net, 0, Skirts[0]), x, z, WaterOf(land, x, z));
    }

    /// <summary>A chunk's three levels as the game loads them.</summary>
    public static BakedChunk[] Chunk(TerrainWorld land, OpenWorldFile world, int x, int z)
    {
        var water = WaterOf(land, x, z);
        return Meshes(land, world, x, z).Select(m => ToBaked(m, x, z, water)).ToArray();
    }

    static BakedChunk ToBaked(ChunkMesh mesh, int x, int z, int[] water)
    {
        var normals = new sbyte[mesh.Normals.Length];
        for (int i = 0; i < normals.Length; i++) normals[i] = (sbyte)Math.Clamp(Math.Round(mesh.Normals[i] * 127.0, MidpointRounding.AwayFromZero), -127, 127);
        return new BakedChunk
        {
            ChunkX = x, ChunkZ = z, Level = mesh.Lod, ErrorMm = Mm.Of(mesh.Error),
            Positions = mesh.Positions, Normals = normals, Covers = mesh.Covers, Indices = mesh.Indices,
            GroundTriangles = mesh.GroundTriangles, ApronTriangles = mesh.ApronTriangles, Water = water
        };
    }

    /// <summary>
    /// The water over a chunk, a level for each 2 m cell: the surface of the highest body over the cell's middle,
    /// where it stands over the land at the cell's middle or any of its corners.
    /// </summary>
    public static int[] WaterOf(TerrainWorld land, int x, int z)
    {
        const int cells = BakedChunk.WaterCells;
        double side = 32.0 / cells;
        var water = new int[cells * cells];
        Array.Fill(water, BakedChunk.NoWater);
        if (land.Water.Count == 0) return water;
        for (int k = 0; k < cells; k++)
            for (int i = 0; i < cells; i++)
            {
                double wx = x * 32.0 + (i + 0.5) * side, wz = z * 32.0 + (k + 0.5) * side;
                if (land.WaterAt(wx, wz) is not { } level) continue;
                double lowest = land.Height(wx, wz);
                foreach (var (dx, dz) in new[] { (-0.5, -0.5), (0.5, -0.5), (-0.5, 0.5), (0.5, 0.5) })
                    lowest = Math.Min(lowest, land.Height(wx + dx * side, wz + dz * side));
                if (level > lowest) water[k * cells + i] = Mm.Of(level);
            }
        return water;
    }

    /// <summary>
    /// Every file of the bake, by its path under <c>baked/</c> (<c>index.json</c> and <c>&lt;level&gt;/&lt;x&gt;_&lt;z&gt;.chunk</c>),
    /// with its bytes; only the chunks <paramref name="only"/> keeps, if given (the index still lists them all).
    /// </summary>
    public static SortedDictionary<string, byte[]> Files(RegionDescription description, Func<int, int, bool>? only = null)
    {
        var world = description.World;
        var land = description.Land();
        var chunks = ChunksOf(world).ToList();
        var baked = new BakedChunk[chunks.Count][];
        Parallel.For(0, chunks.Count, i =>
        {
            var (x, z) = chunks[i];
            if (only == null || only(x, z)) baked[i] = Chunk(land, world, x, z);
        });
        var files = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
        for (int i = 0; i < chunks.Count; i++)
            if (baked[i] != null)
                foreach (var chunk in baked[i]) files[BakedChunk.FileName(chunk.ChunkX, chunk.ChunkZ, chunk.Level)] = chunk.Bytes();
        var index = new BakedIndexFile
        {
            Format = BakedChunk.Format,
            Region = world.Region,
            ChunkSide = OpenMap.ChunkSide,
            CellMm = Mm.Of(Cell),
            LevelErrorsMm = new[] { 0 }.Concat(Thinned.Select(t => Mm.Of(t.MaxError))).ToList(),
            Chunks = chunks.Select(c => new[] { c.X, c.Z }).ToList(),
            Source = description.Source
        };
        files["index.json"] = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(index, GameDataFiles.Json) + "\n");
        return files;
    }

    /// <summary>Writes a bake's files under <paramref name="baked"/>, and takes away any chunk file there the bake has none of.</summary>
    public static void Write(string baked, SortedDictionary<string, byte[]> files, bool partial = false)
    {
        Directory.CreateDirectory(baked);
        foreach (var (name, bytes) in files)
        {
            string path = Path.Combine(baked, name);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            if (!File.Exists(path) || !File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes)) File.WriteAllBytes(path, bytes);
        }
        if (partial) return;
        foreach (string stale in ExistingChunkFiles(baked).Where(f => !files.ContainsKey(f))) File.Delete(Path.Combine(baked, stale));
    }

    /// <summary>Where the files under <paramref name="baked"/> differ from a bake's, a line each; none where they are the same.</summary>
    public static List<string> Differences(string baked, SortedDictionary<string, byte[]> files, bool partial = false)
    {
        var found = new List<string>();
        foreach (var (name, bytes) in files)
        {
            string path = Path.Combine(baked, name);
            if (!File.Exists(path)) found.Add($"{name}: not baked");
            else if (!File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes)) found.Add($"{name}: differs from what the descriptions bake");
        }
        if (!partial)
            foreach (string stale in ExistingChunkFiles(baked).Where(f => !files.ContainsKey(f))) found.Add($"{stale}: baked, but no chunk of the region's");
        return found;
    }

    static IEnumerable<string> ExistingChunkFiles(string baked) =>
        Directory.Exists(baked)
            ? Directory.GetFiles(baked, "*.chunk", SearchOption.AllDirectories).Select(f => Path.GetRelativePath(baked, f).Replace('\\', '/')).OrderBy(f => f, StringComparer.Ordinal)
            : Enumerable.Empty<string>();
}
