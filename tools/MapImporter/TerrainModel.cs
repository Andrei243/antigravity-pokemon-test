using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace MapImporter;

/// <summary>
/// What a chunk's terrain model says about its ground, read without keeping any of its art: for every polygon, the
/// name of the texture it is drawn with and where it lies. From that the importer learns what the tile attributes
/// leave out, namely which open tiles are path and which are lawn, and whether a blocked tile holds a tree, a
/// fence or a rock face. Vertices are read only to find the tiles under each polygon.
/// </summary>
public sealed class TerrainModel
{
    /// <summary>One polygon: its texture's name and its corners in the original's units from the chunk's centre.</summary>
    public readonly record struct Polygon(string Texture, string Material, Vector3 A, Vector3 B, Vector3 C)
    {
        public Vector3 Normal => Vector3.Normalize(Vector3.Cross(B - A, C - A));

        /// <summary>True for ground and roofs seen from above; false for walls, trunks and upright cards.</summary>
        public bool Level => MathF.Abs(Normal.Y) > 0.5f;
    }

    public List<Polygon> Polygons { get; } = new();
    public List<string> Textures { get; } = new();

    public static TerrainModel? Read(ReadOnlySpan<byte> file)
    {
        if (file.Length < 0x18 || Encoding.ASCII.GetString(file[..4]) != "BMD0") return null;
        int sections = BinaryPrimitives.ReadUInt16LittleEndian(file[14..]);
        for (int s = 0; s < sections; s++)
        {
            int at = BinaryPrimitives.ReadInt32LittleEndian(file[(16 + s * 4)..]);
            if (at + 8 <= file.Length && Encoding.ASCII.GetString(file.Slice(at, 4)) == "MDL0") return ReadModels(file[at..]);
        }
        return null;
    }

    private static TerrainModel? ReadModels(ReadOnlySpan<byte> mdl)
    {
        var models = Dict.Read(mdl[8..]);
        if (models.Count == 0) return null;
        var model = mdl[BinaryPrimitives.ReadInt32LittleEndian(models.Data(0))..];

        int sbcAt = BinaryPrimitives.ReadInt32LittleEndian(model[4..]);
        int matAt = BinaryPrimitives.ReadInt32LittleEndian(model[8..]);
        int shpAt = BinaryPrimitives.ReadInt32LittleEndian(model[12..]);
        float posScale = LandData.Fx32(model[(20 + 8)..]);

        // Materials by index, and for each the texture bound to it
        var matSet = model[matAt..];
        var materials = Dict.Read(matSet[4..]);
        var textureOf = new string[materials.Count];
        Array.Fill(textureOf, "");
        var textures = Dict.Read(matSet[BinaryPrimitives.ReadUInt16LittleEndian(matSet)..]);
        var result = new TerrainModel();
        for (int t = 0; t < textures.Count; t++)
        {
            result.Textures.Add(textures.Name(t));
            var data = textures.Data(t);
            int listAt = BinaryPrimitives.ReadUInt16LittleEndian(data);
            for (int i = 0; i < data[2]; i++)
            {
                int material = matSet[listAt + i];
                if (material < textureOf.Length) textureOf[material] = textures.Name(t);
            }
        }

        var shpSet = model[shpAt..];
        var shapes = Dict.Read(shpSet);

        // The render commands pair each shape with the material set just before it
        var sbc = model[sbcAt..];
        int current = -1;
        for (int pc = 0; pc < sbc.Length;)
        {
            int op = sbc[pc] & 0x1F, flags = sbc[pc] >> 5;
            switch (op)
            {
                case 0x00: pc += 1; break;
                case 0x01: pc = sbc.Length; break;
                case 0x02: pc += 3; break;
                case 0x03: pc += 2; break;
                case 0x04: current = sbc[pc + 1]; pc += 2; break;
                case 0x05:
                {
                    int shape = sbc[pc + 1];
                    pc += 2;
                    if (shape >= shapes.Count) break;
                    var shp = shpSet[BinaryPrimitives.ReadInt32LittleEndian(shapes.Data(shape))..];
                    int dlAt = BinaryPrimitives.ReadInt32LittleEndian(shp[8..]);
                    int dlSize = BinaryPrimitives.ReadInt32LittleEndian(shp[12..]);
                    string material = current >= 0 && current < materials.Count ? materials.Name(current) : "";
                    string texture = current >= 0 && current < textureOf.Length ? textureOf[current] : "";
                    result.Polygons.AddRange(ReadDisplayList(shp.Slice(dlAt, dlSize), posScale, texture, material));
                    break;
                }
                case 0x06: pc += 4 + (flags & 1) + ((flags >> 1) & 1); break;
                case 0x07 or 0x08: pc += 2 + (flags & 1) + ((flags >> 1) & 1); break;
                case 0x09: pc += 3 + sbc[pc + 2] * 3; break;
                case 0x0A: pc += 9; break;
                case 0x0B: pc += 1; break;
                case 0x0C or 0x0D: pc += 3; break;
                default: return result;   // an unknown command: keep what was read so far
            }
        }
        return result;
    }

    // How many 32-bit parameters each geometry command takes
    private static int Parameters(int command) => command switch
    {
        0x10 or 0x12 or 0x13 or 0x14 => 1,
        0x16 => 16, 0x17 => 12, 0x18 => 16, 0x19 => 12, 0x1A => 9, 0x1B or 0x1C => 3,
        0x20 or 0x21 or 0x22 => 1,
        0x23 => 2,
        0x24 or 0x25 or 0x26 or 0x27 or 0x28 or 0x29 or 0x2A or 0x2B => 1,
        0x30 or 0x31 or 0x32 or 0x33 => 1,
        0x34 => 32,
        0x40 => 1,
        0x50 or 0x60 => 1,
        0x70 => 3, 0x71 => 2, 0x72 => 1,
        _ => 0
    };

    /// <summary>
    /// The triangles of one display list. Commands come four to a word, each followed by its parameters; only the
    /// vertex commands and the starts and ends of primitives are acted on. Quads and strips are cut into triangles.
    /// </summary>
    public static List<Polygon> ReadDisplayList(ReadOnlySpan<byte> dl, float posScale, string texture = "", string material = "")
    {
        var polygons = new List<Polygon>();
        int x = 0, y = 0, z = 0, primitive = -1;
        var vertices = new List<Vector3>();
        static int S16(uint v) => (short)(v & 0xFFFF);
        static int S10(uint v) => ((int)(v & 0x3FF) << 22) >> 22;

        void Vertex() => vertices.Add(new Vector3(x, y, z) / 4096f * posScale);

        void Flush()
        {
            void Add(int a, int b, int c) => polygons.Add(new Polygon(texture, material, vertices[a], vertices[b], vertices[c]));
            switch (primitive)
            {
                case 0:
                    for (int i = 0; i + 2 < vertices.Count; i += 3) Add(i, i + 1, i + 2);
                    break;
                case 1:
                    for (int i = 0; i + 3 < vertices.Count; i += 4) { Add(i, i + 1, i + 2); Add(i, i + 2, i + 3); }
                    break;
                case 2:
                    for (int i = 0; i + 2 < vertices.Count; i++)
                        if (i % 2 == 0) Add(i, i + 1, i + 2); else Add(i + 1, i, i + 2);
                    break;
                case 3:
                    for (int i = 0; i + 3 < vertices.Count; i += 2) { Add(i, i + 1, i + 3); Add(i, i + 3, i + 2); }
                    break;
            }
            vertices.Clear();
        }

        for (int at = 0; at + 4 <= dl.Length;)
        {
            // Four command bytes, then the parameters of each in turn
            var commands = dl.Slice(at, 4);
            at += 4;
            for (int c = 0; c < 4; c++)
            {
                int command = commands[c];
                int count = Parameters(command);
                if (at + count * 4 > dl.Length) return polygons;
                uint p0 = count > 0 ? BinaryPrimitives.ReadUInt32LittleEndian(dl[at..]) : 0;
                uint p1 = count > 1 ? BinaryPrimitives.ReadUInt32LittleEndian(dl[(at + 4)..]) : 0;
                at += count * 4;

                switch (command)
                {
                    case 0x40: Flush(); primitive = (int)(p0 & 3); break;
                    case 0x41: Flush(); primitive = -1; break;
                    case 0x23: x = S16(p0); y = S16(p0 >> 16); z = S16(p1); Vertex(); break;
                    case 0x24: x = S10(p0) << 6; y = S10(p0 >> 10) << 6; z = S10(p0 >> 20) << 6; Vertex(); break;
                    case 0x25: x = S16(p0); y = S16(p0 >> 16); Vertex(); break;
                    case 0x26: x = S16(p0); z = S16(p0 >> 16); Vertex(); break;
                    case 0x27: y = S16(p0); z = S16(p0 >> 16); Vertex(); break;
                    case 0x28: x += S10(p0); y += S10(p0 >> 10); z += S10(p0 >> 20); Vertex(); break;
                }
            }
        }
        Flush();
        return polygons;
    }

    /// <summary>What lies on each tile of a chunk, by texture name: on the ground, and above it.</summary>
    public sealed record Layers(string?[] Ground, string?[] Above, float[] AboveHeight);

    /// <summary>How far, in the original's units, a polygon may sit from the walking surface and still be ground.</summary>
    public const float GroundTolerance = 2f;

    /// <summary>
    /// Sorts the polygons over the middle of each tile into the ground (those lying on the walking surface the
    /// height data describes; the topmost wins) and whatever rises above it: the crown of a tree, a rock, a roof.
    /// </summary>
    public Layers LayersOver(HeightPlates heights)
    {
        int count = LandData.Tiles * LandData.Tiles;
        var ground = new string?[count];
        var above = new string?[count];
        var groundTop = new float[count];
        var aboveTop = new float[count];
        Array.Fill(groundTop, float.NegativeInfinity);
        Array.Fill(aboveTop, float.NegativeInfinity);
        float half = LandData.Tiles * LandData.TileUnits / 2f;

        foreach (var p in Polygons)
        {
            if (MathF.Abs(p.Normal.Y) < 0.2f || float.IsNaN(p.Normal.Y)) continue;   // a wall: it covers no tile
            int x0 = Math.Max(0, (int)MathF.Floor((MathF.Min(p.A.X, MathF.Min(p.B.X, p.C.X)) + half) / LandData.TileUnits));
            int x1 = Math.Min(LandData.Tiles - 1, (int)MathF.Floor((MathF.Max(p.A.X, MathF.Max(p.B.X, p.C.X)) + half) / LandData.TileUnits));
            int z0 = Math.Max(0, (int)MathF.Floor((MathF.Min(p.A.Z, MathF.Min(p.B.Z, p.C.Z)) + half) / LandData.TileUnits));
            int z1 = Math.Min(LandData.Tiles - 1, (int)MathF.Floor((MathF.Max(p.A.Z, MathF.Max(p.B.Z, p.C.Z)) + half) / LandData.TileUnits));
            for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                {
                    float px = (x + 0.5f) * LandData.TileUnits - half, pz = (z + 0.5f) * LandData.TileUnits - half;
                    if (!Inside(p, px, pz, out float height)) continue;
                    int i = z * LandData.Tiles + x;
                    float? surface = heights.HeightAt(px, pz, height);
                    bool onGround = surface == null || MathF.Abs(height - surface.Value) <= GroundTolerance;
                    if (onGround && height > groundTop[i]) { groundTop[i] = height; ground[i] = p.Texture; }
                    if (!onGround && height > aboveTop[i]) { aboveTop[i] = height; above[i] = p.Texture; }
                }
        }
        return new Layers(ground, above, aboveTop);
    }

    private static bool Inside(Polygon p, float x, float z, out float height)
    {
        float d = (p.B.Z - p.C.Z) * (p.A.X - p.C.X) + (p.C.X - p.B.X) * (p.A.Z - p.C.Z);
        height = 0;
        if (MathF.Abs(d) < 1e-6f) return false;
        float u = ((p.B.Z - p.C.Z) * (x - p.C.X) + (p.C.X - p.B.X) * (z - p.C.Z)) / d;
        float v = ((p.C.Z - p.A.Z) * (x - p.C.X) + (p.A.X - p.C.X) * (z - p.C.Z)) / d;
        float w = 1 - u - v;
        const float edge = -0.001f;
        if (u < edge || v < edge || w < edge) return false;
        height = u * p.A.Y + v * p.B.Y + w * p.C.Y;
        return true;
    }

    /// <summary>The name dictionary the model format uses everywhere: a count, fixed-size data per entry, 16-byte names.</summary>
    private readonly ref struct Dict
    {
        private readonly ReadOnlySpan<byte> entries;
        private readonly int unit, names;
        public int Count { get; }

        private Dict(ReadOnlySpan<byte> entries, int count, int unit, int names)
        {
            this.entries = entries;
            Count = count;
            this.unit = unit;
            this.names = names;
        }

        public static Dict Read(ReadOnlySpan<byte> dict)
        {
            int count = dict[1];
            var entries = dict[BinaryPrimitives.ReadUInt16LittleEndian(dict[6..])..];
            return new Dict(entries, count, BinaryPrimitives.ReadUInt16LittleEndian(entries), BinaryPrimitives.ReadUInt16LittleEndian(entries[2..]));
        }

        public ReadOnlySpan<byte> Data(int i) => entries.Slice(4 + i * unit, unit);
        public string Name(int i) => Encoding.ASCII.GetString(entries.Slice(names + i * 16, 16)).TrimEnd('\0');
    }
}
