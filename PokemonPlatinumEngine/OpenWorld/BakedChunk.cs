using System;
using System.IO;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>
/// A chunk of an open region's land at one level of detail, as <c>tools/OpenWorldBake</c> writes it and the game
/// loads it (plan 32 · OW2): every vertex in whole millimetres, a normal and a cover each, the triangles (the chunk's
/// own ground first, then its apron, then its skirts), and the water over the chunk. Level 0 is the finest (surface
/// nets at half a metre); levels 1 and 2 are that mesh thinned (decision 16), with every vertex one of level 0's and
/// the chunk's border kept where level 0 has it. The collision mesh is level 0's own ground (its first
/// <see cref="GroundTriangles"/>): the apron is drawn and never collided with, and the skirts hang under the border.
/// GPU-free; the bytes are the same on any machine (<see cref="Write"/>).
/// </summary>
public sealed class BakedChunk
{
    /// <summary>The format's number, the file's fifth byte; a file of another is refused.</summary>
    public const byte Format = 1;

    /// <summary>The cells of the water's grid across a chunk: 2 m each.</summary>
    public const int WaterCells = 16;

    /// <summary>A water cell with no water over it.</summary>
    public const int NoWater = int.MinValue;

    private static ReadOnlySpan<byte> Magic => "OWCK"u8;

    /// <summary>The chunk's column east and row south, as <see cref="OpenMap.ChunkOf"/> counts them.</summary>
    public required int ChunkX { get; init; }
    public required int ChunkZ { get; init; }

    /// <summary>0, 1 or 2.</summary>
    public required int Level { get; init; }

    /// <summary>How far the level may stand off level 0's surface, in millimetres (nought at level 0).</summary>
    public required int ErrorMm { get; init; }

    /// <summary>Three a vertex: x, y and z in millimetres, where they stand in the world.</summary>
    public required int[] Positions { get; init; }

    /// <summary>Three a vertex, each a share of 127.</summary>
    public required sbyte[] Normals { get; init; }

    public required OpenCover[] Covers { get; init; }

    /// <summary>Three a triangle, counter-clockwise seen from the air.</summary>
    public required int[] Indices { get; init; }

    /// <summary>The chunk's own ground: what is walked and collided with at level 0.</summary>
    public required int GroundTriangles { get; init; }

    /// <summary>The apron's: a cell's row west and north that the neighbours there own too, drawn so that two levels overlap where they meet.</summary>
    public required int ApronTriangles { get; init; }

    /// <summary>The water's surface over each 2 m cell of the chunk, west to east and north to south, in millimetres, or <see cref="NoWater"/>.</summary>
    public required int[] Water { get; init; }

    public int VertexCount => Positions.Length / 3;
    public int TriangleCount => Indices.Length / 3;
    public int SkirtTriangles => TriangleCount - GroundTriangles - ApronTriangles;

    /// <summary>A vertex's place.</summary>
    public OpenPos Vertex(int v) => new(Positions[v * 3], Positions[v * 3 + 1], Positions[v * 3 + 2]);

    /// <summary>The water's surface over a 2 m cell (0 to 15 across and down), in millimetres, or <see cref="NoWater"/>.</summary>
    public int WaterAt(int cellX, int cellZ) => Water[cellZ * WaterCells + cellX];

    /// <summary>Whether any of the chunk is under water.</summary>
    public bool HasWater => Array.Exists(Water, w => w != NoWater);

    /// <summary>The chunk as the file holds it.</summary>
    public byte[] Bytes()
    {
        using var stream = new MemoryStream();
        Write(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Writes the chunk: "OWCK", the format, then whole numbers only, each a zigzag of seven bits a byte (places as
    /// steps from the vertex before, starting at the chunk's north-west corner; indices as steps from the index
    /// before), so the bytes are the numbers' and the same on any machine.
    /// </summary>
    public void Write(Stream stream)
    {
        Check();
        using var w = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        w.Write(Magic);
        w.Write(Format);
        Signed(w, ChunkX); Signed(w, ChunkZ);
        w.Write7BitEncodedInt(Level);
        w.Write7BitEncodedInt(ErrorMm);

        w.Write7BitEncodedInt(VertexCount);
        int px = ChunkX * OpenMap.ChunkSide, py = 0, pz = ChunkZ * OpenMap.ChunkSide;
        for (int v = 0; v < VertexCount; v++)
        {
            int x = Positions[v * 3], y = Positions[v * 3 + 1], z = Positions[v * 3 + 2];
            Signed(w, x - px); Signed(w, y - py); Signed(w, z - pz);
            (px, py, pz) = (x, y, z);
        }
        foreach (sbyte n in Normals) w.Write(n);
        foreach (var c in Covers) w.Write((byte)c);

        w.Write7BitEncodedInt(TriangleCount);
        w.Write7BitEncodedInt(GroundTriangles);
        w.Write7BitEncodedInt(ApronTriangles);
        int last = 0;
        foreach (int i in Indices) { Signed(w, i - last); last = i; }

        // The water: a byte saying whether there is any, then each cell's level as a step from the cell before
        bool any = HasWater;
        w.Write((byte)(any ? 1 : 0));
        if (any)
        {
            long previous = 0;
            // Nought is a cell with none; a level is its step from the last level written, zigzagged, plus one
            foreach (int level in Water)
            {
                if (level == NoWater) { w.Write7BitEncodedInt64(0); continue; }
                w.Write7BitEncodedInt64(Zig(level - previous) + 1);
                previous = level;
            }
        }
    }

    /// <summary>Reads a chunk written by <see cref="Write"/>; another format, or a file cut short, is refused.</summary>
    public static BakedChunk Read(Stream stream)
    {
        using var r = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        Span<byte> magic = stackalloc byte[4];
        if (r.Read(magic) != 4 || !magic.SequenceEqual(Magic)) throw new InvalidDataException("not a baked chunk (no OWCK)");
        byte format = r.ReadByte();
        if (format != Format) throw new InvalidDataException($"a baked chunk of format {format}, not {Format}: bake the region again");
        int cx = Unsigned(r), cz = Unsigned(r);
        int level = r.Read7BitEncodedInt(), error = r.Read7BitEncodedInt();

        int vertices = r.Read7BitEncodedInt();
        var positions = new int[vertices * 3];
        int px = cx * OpenMap.ChunkSide, py = 0, pz = cz * OpenMap.ChunkSide;
        for (int v = 0; v < vertices; v++)
        {
            px += Unsigned(r); py += Unsigned(r); pz += Unsigned(r);
            positions[v * 3] = px; positions[v * 3 + 1] = py; positions[v * 3 + 2] = pz;
        }
        var normals = new sbyte[vertices * 3];
        for (int i = 0; i < normals.Length; i++) normals[i] = r.ReadSByte();
        var covers = new OpenCover[vertices];
        for (int i = 0; i < vertices; i++) covers[i] = (OpenCover)r.ReadByte();

        int triangles = r.Read7BitEncodedInt(), ground = r.Read7BitEncodedInt(), apron = r.Read7BitEncodedInt();
        var indices = new int[triangles * 3];
        int last = 0;
        for (int i = 0; i < indices.Length; i++) { last += Unsigned(r); indices[i] = last; }

        var water = new int[WaterCells * WaterCells];
        Array.Fill(water, NoWater);
        if (r.ReadByte() == 1)
        {
            long previous = 0;
            for (int i = 0; i < water.Length; i++)
            {
                long coded = r.Read7BitEncodedInt64();
                if (coded == 0) continue;
                long step = Unzig(coded - 1);
                previous += step;
                water[i] = checked((int)previous);
            }
        }
        var chunk = new BakedChunk
        {
            ChunkX = cx, ChunkZ = cz, Level = level, ErrorMm = error,
            Positions = positions, Normals = normals, Covers = covers, Indices = indices,
            GroundTriangles = ground, ApronTriangles = apron, Water = water
        };
        chunk.Check();
        return chunk;
    }

    /// <summary>The file's name in a region's <c>baked</c> folder: <c>0/-2_1.chunk</c>.</summary>
    public static string FileName(int chunkX, int chunkZ, int level) => $"{level}/{chunkX}_{chunkZ}.chunk";

    private void Check()
    {
        int vertices = Positions.Length / 3;
        if (Positions.Length % 3 != 0 || Normals.Length != Positions.Length || Covers.Length != vertices)
            throw new InvalidDataException($"chunk {ChunkX},{ChunkZ} level {Level}: its vertices' arrays differ in length");
        if (Indices.Length % 3 != 0 || GroundTriangles + ApronTriangles > Indices.Length / 3 || GroundTriangles < 0 || ApronTriangles < 0)
            throw new InvalidDataException($"chunk {ChunkX},{ChunkZ} level {Level}: its triangles don't add up");
        foreach (int i in Indices)
            if (i < 0 || i >= vertices) throw new InvalidDataException($"chunk {ChunkX},{ChunkZ} level {Level}: a triangle names vertex {i} of {vertices}");
        if (Water.Length != WaterCells * WaterCells) throw new InvalidDataException($"chunk {ChunkX},{ChunkZ} level {Level}: its water is not {WaterCells} by {WaterCells}");
    }

    private static long Zig(long value) => (value << 1) ^ (value >> 63);
    private static long Unzig(long value) => (long)((ulong)value >> 1) ^ -(value & 1);
    private static void Signed(BinaryWriter w, int value) => w.Write7BitEncodedInt64(Zig(value));
    private static int Unsigned(BinaryReader r) => checked((int)Unzig(r.Read7BitEncodedInt64()));
}
