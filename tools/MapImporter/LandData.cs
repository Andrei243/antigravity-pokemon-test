using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace MapImporter;

/// <summary>
/// One 32×32-tile chunk of Platinum's world, as stored in <c>res/field/maps/data/map_data_NNN.bin</c>. The layout was
/// confirmed against the game's own loader (<c>src/overlay005/land_data.c</c>, <c>map_prop.c</c> and <c>bdhc.c</c>
/// in the decompilation): a header of four sizes, then the tile attributes, the props, the chunk's 3D model and the
/// height data. Of the model only names and the lie of its polygons are read (see <see cref="TerrainModel"/>), never its art.
/// </summary>
public sealed class LandData
{
    /// <summary>Tiles along each side of a chunk.</summary>
    public const int Tiles = 32;

    /// <summary>World units per tile in the original's coordinates; props and heights are measured in them.</summary>
    public const float TileUnits = 16f;

    private const int HeaderSize = 16;
    private const int AttributesSize = Tiles * Tiles * 2;
    private const int PropSize = 48;
    private const ushort CollisionBit = 0x8000;

    /// <summary>One value per tile, row by row from the north-west: the behaviour in the low byte, the collision
    /// flag in the top bit.</summary>
    public ushort[] Attributes { get; private init; } = new ushort[Tiles * Tiles];

    public List<LandProp> Props { get; private init; } = new();

    public HeightPlates Heights { get; private init; } = HeightPlates.Empty;

    /// <summary>The model's internal name (for example <c>map03_27c</c>: the chunk at column 3, row 27 of the overworld).</summary>
    public string ModelName { get; private init; } = "";

    public int ModelBytes => model.Length;

    private byte[] model = Array.Empty<byte>();

    /// <summary>What the chunk's model says about its ground; see <see cref="TerrainModel"/>.</summary>
    public TerrainModel? ReadTerrain() => TerrainModel.Read(model);

    public byte Behaviour(int x, int z) => (byte)(Attributes[z * Tiles + x] & 0xFF);

    public bool Solid(int x, int z) => (Attributes[z * Tiles + x] & CollisionBit) != 0;

    /// <summary>Bits of the attributes that are neither the behaviour nor the collision flag; none are known.</summary>
    public int OtherBits(int x, int z) => Attributes[z * Tiles + x] & 0x7F00;

    public static LandData Parse(ReadOnlySpan<byte> file)
    {
        if (file.Length < HeaderSize) throw new InvalidDataException("Land data is shorter than its header");
        int attributesSize = BinaryPrimitives.ReadInt32LittleEndian(file);
        int propsSize = BinaryPrimitives.ReadInt32LittleEndian(file[4..]);
        int modelSize = BinaryPrimitives.ReadInt32LittleEndian(file[8..]);
        int heightsSize = BinaryPrimitives.ReadInt32LittleEndian(file[12..]);

        if (attributesSize != AttributesSize)
            throw new InvalidDataException($"Land data has {attributesSize} bytes of tile attributes, expected {AttributesSize}");
        // A few files carry stray bytes after their last prop; the game divides and drops the remainder, so do we
        if (propsSize < 0 || modelSize < 0 || heightsSize < 0 || HeaderSize + (long)attributesSize + propsSize + modelSize + heightsSize != file.Length)
            throw new InvalidDataException("Land data sections don't add up to the file's size");

        var attributes = new ushort[Tiles * Tiles];
        for (int i = 0; i < attributes.Length; i++)
            attributes[i] = BinaryPrimitives.ReadUInt16LittleEndian(file[(HeaderSize + i * 2)..]);

        var props = new List<LandProp>();
        var propBytes = file.Slice(HeaderSize + attributesSize, propsSize);
        for (int i = 0; i < propsSize / PropSize; i++)
        {
            var p = propBytes[(i * PropSize)..];
            props.Add(new LandProp(
                BinaryPrimitives.ReadInt32LittleEndian(p),
                new Vector3(Fx32(p[4..]), Fx32(p[8..]), Fx32(p[12..])),
                new Vector3(BinaryPrimitives.ReadInt32LittleEndian(p[16..]), BinaryPrimitives.ReadInt32LittleEndian(p[20..]), BinaryPrimitives.ReadInt32LittleEndian(p[24..])),
                new Vector3(Fx32(p[28..]), Fx32(p[32..]), Fx32(p[36..]))));
        }

        var modelBytes = file.Slice(HeaderSize + attributesSize + propsSize, modelSize);
        var heights = file.Slice(HeaderSize + attributesSize + propsSize + modelSize, heightsSize);
        return new LandData
        {
            Attributes = attributes,
            Props = props,
            Heights = heightsSize > 0 ? HeightPlates.Parse(heights) : HeightPlates.Empty,
            ModelName = ModelInfo.Read(modelBytes)?.Name ?? "",
            model = modelBytes.ToArray()
        };
    }

    internal static float Fx32(ReadOnlySpan<byte> bytes) => BinaryPrimitives.ReadInt32LittleEndian(bytes) / 4096f;
}

/// <summary>
/// A building or other object standing on a chunk: which model, and where its origin is, in the original's units
/// from the chunk's centre (x east, y up, z south). The game draws every prop unrotated; the stored rotation is
/// kept only to report it.
/// </summary>
public readonly record struct LandProp(int ModelId, Vector3 Position, Vector3 Rotation, Vector3 Scale)
{
    /// <summary>The prop's origin in tiles from the chunk's north-west corner.</summary>
    public float TileX => Position.X / LandData.TileUnits + LandData.Tiles / 2f;

    public float TileZ => Position.Z / LandData.TileUnits + LandData.Tiles / 2f;
}

/// <summary>
/// The height data of a chunk ("BDHC"): rectangles of ground, each a plane given by a unit normal and a constant.
/// A point's height on a plate is <c>-(nx·x + nz·z + d) / ny</c>; where plates overlap (a bridge over a path) the game
/// takes the one nearest the height the walker is already at. Coordinates are the original's units from the chunk's
/// centre.
/// </summary>
public sealed class HeightPlates
{
    public static readonly HeightPlates Empty = new(Array.Empty<Plate>());

    public IReadOnlyList<Plate> Plates { get; }

    public HeightPlates(IReadOnlyList<Plate> plates) => Plates = plates;

    public readonly record struct Plate(float MinX, float MinZ, float MaxX, float MaxZ, Vector3 Normal, float Constant)
    {
        public bool Contains(float x, float z) => x >= MinX && x <= MaxX && z >= MinZ && z <= MaxZ;

        public float HeightAt(float x, float z) => -(Normal.X * x + Normal.Z * z + Constant) / Normal.Y;

        public bool Flat => Normal.X == 0 && Normal.Z == 0;
    }

    /// <summary>Every height the ground has at a point, lowest first: more than one where plates overlap.</summary>
    public List<float> HeightsAt(float x, float z)
    {
        var heights = new List<float>();
        foreach (var p in Plates)
            if (p.Contains(x, z)) heights.Add(p.HeightAt(x, z));
        heights.Sort();
        return heights;
    }

    /// <summary>The height a walker coming from <paramref name="from"/> ends up at, as the game picks it.</summary>
    public float? HeightAt(float x, float z, float from)
    {
        float? best = null;
        foreach (float h in HeightsAt(x, z))
            if (best == null || MathF.Abs(h - from) < MathF.Abs(best.Value - from)) best = h;
        return best;
    }

    public static HeightPlates Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < 16 || Encoding.ASCII.GetString(data[..4]) != "BDHC")
            throw new InvalidDataException("Height data doesn't start with BDHC");

        int points = BinaryPrimitives.ReadUInt16LittleEndian(data[4..]);
        int normals = BinaryPrimitives.ReadUInt16LittleEndian(data[6..]);
        int constants = BinaryPrimitives.ReadUInt16LittleEndian(data[8..]);
        int plates = BinaryPrimitives.ReadUInt16LittleEndian(data[10..]);
        int strips = BinaryPrimitives.ReadUInt16LittleEndian(data[12..]);
        int accessList = BinaryPrimitives.ReadUInt16LittleEndian(data[14..]);

        int pointsAt = 16;
        int normalsAt = pointsAt + points * 8;
        int constantsAt = normalsAt + normals * 12;
        int platesAt = constantsAt + constants * 4;
        int stripsAt = platesAt + plates * 8;
        int end = stripsAt + strips * 8 + accessList * 2;
        if (end > data.Length) throw new InvalidDataException("Height data is shorter than its counts say");

        // The strips and the access list only speed up the game's search for the plates under a point; the plates
        // alone say everything about the ground
        var result = new Plate[plates];
        for (int i = 0; i < plates; i++)
        {
            var p = data[(platesAt + i * 8)..];
            int first = BinaryPrimitives.ReadUInt16LittleEndian(p);
            int second = BinaryPrimitives.ReadUInt16LittleEndian(p[2..]);
            int normal = BinaryPrimitives.ReadUInt16LittleEndian(p[4..]);
            int constant = BinaryPrimitives.ReadUInt16LittleEndian(p[6..]);
            if (first >= points || second >= points || normal >= normals || constant >= constants)
                throw new InvalidDataException($"Height plate {i} points outside its tables");

            float ax = LandData.Fx32(data[(pointsAt + first * 8)..]), az = LandData.Fx32(data[(pointsAt + first * 8 + 4)..]);
            float bx = LandData.Fx32(data[(pointsAt + second * 8)..]), bz = LandData.Fx32(data[(pointsAt + second * 8 + 4)..]);
            var n = data[(normalsAt + normal * 12)..];
            result[i] = new Plate(MathF.Min(ax, bx), MathF.Min(az, bz), MathF.Max(ax, bx), MathF.Max(az, bz),
                new Vector3(LandData.Fx32(n), LandData.Fx32(n[4..]), LandData.Fx32(n[8..])),
                LandData.Fx32(data[(constantsAt + constant * 4)..]));
        }
        return new HeightPlates(result);
    }
}

/// <summary>
/// The name and the bounding box of a model file (<c>.nsbmd</c>), read from its header. Nothing else of the model
/// is touched: no vertices, materials or textures. The name tells buildings apart, the box gives a building's
/// footprint.
/// </summary>
public sealed record ModelInfo(string Name, Vector3 BoxMin, Vector3 BoxSize)
{
    public static ModelInfo? Read(ReadOnlySpan<byte> file)
    {
        // "BMD0", byte order, version, file size, header size (16), section count, then one offset per section
        if (file.Length < 0x18 || Encoding.ASCII.GetString(file[..4]) != "BMD0") return null;
        int sections = BinaryPrimitives.ReadUInt16LittleEndian(file[14..]);
        for (int s = 0; s < sections; s++)
        {
            int at = BinaryPrimitives.ReadInt32LittleEndian(file[(16 + s * 4)..]);
            if (at + 8 > file.Length || Encoding.ASCII.GetString(file.Slice(at, 4)) != "MDL0") continue;
            var mdl = file[at..];

            // The model set starts with a dictionary: a header, a search tree of (count + 1) nodes, then one data
            // word (the model's offset) and one 16-byte name per entry
            var dict = mdl[8..];
            int count = dict[1];
            if (count == 0) return null;
            int entries = BinaryPrimitives.ReadUInt16LittleEndian(dict[6..]);
            int unit = BinaryPrimitives.ReadUInt16LittleEndian(dict[entries..]);
            int names = BinaryPrimitives.ReadUInt16LittleEndian(dict[(entries + 2)..]);
            int modelAt = BinaryPrimitives.ReadInt32LittleEndian(dict[(entries + 4)..]);
            _ = unit;
            string name = Encoding.ASCII.GetString(dict.Slice(entries + names, 16)).TrimEnd('\0');

            // Model header: five offsets, then the info block, whose bounding box sits 24 bytes in
            var info = mdl[(modelAt + 20)..];
            float scale = LandData.Fx32(info[36..]);
            return new ModelInfo(name, Fx16Vector(info[24..]) * scale, Fx16Vector(info[30..]) * scale);
        }
        return null;
    }

    private static Vector3 Fx16Vector(ReadOnlySpan<byte> bytes) => new Vector3(
        BinaryPrimitives.ReadInt16LittleEndian(bytes),
        BinaryPrimitives.ReadInt16LittleEndian(bytes[2..]),
        BinaryPrimitives.ReadInt16LittleEndian(bytes[4..])) / 4096f;
}
