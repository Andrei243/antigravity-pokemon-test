using System.Buffers.Binary;

namespace MapImporter;

/// <summary>
/// What the Distortion World keeps outside the ordinary map files (plan 01 · M8), read from the two archives the
/// original's overlay 9 opens, <c>res/prebuilt/fielddata/tornworld/tw_arc.narc</c> and <c>tw_arc_attr.narc</c>, as
/// it reads them (<c>DistWorldMapInfoFile_Load</c>, <c>DistWorldMapFile_Load</c>,
/// <c>GetCurrentFloatingPlatformTileAttributes</c> in <c>src/overlay009/ov9_02249960.c</c>). Two things are kept:
/// <list type="bullet">
/// <item>where each floor lies in the Distortion World's own space, which is the one its events are given in (the
/// portal of the first floor stands at 55, 39 of that space: 34, 29 of its own map);</item>
/// <item>the floors that float over a floor's own ground, which the original keeps apart from the map (its "floating
/// platforms" of the floor kind: the stones of B2F's upper level), tile by tile and at their height.</item>
/// </list>
/// Its walls and ceilings, walked with sideways gravity, are not kept: this game doesn't walk them.
/// </summary>
public sealed class DistortionWorld
{
    /// <summary>Where the archives lie in the decompilation.</summary>
    public const string MainArchive = "res/prebuilt/fielddata/tornworld/tw_arc.narc";
    public const string AttributeArchive = "res/prebuilt/fielddata/tornworld/tw_arc_attr.narc";

    /// <summary>
    /// A floor that floats over a map's own ground, in tiles of the map: its north-west corner, its size, how high it
    /// stands (in tiles, as the map's plates measure) and its tile attributes row by row from the north-west (the
    /// behaviour in the low byte, the blocked flag in the top bit, as a chunk's).
    /// </summary>
    public sealed record Floor(int X, int Z, int Width, int Depth, int Height, ushort[] Attributes)
    {
        public ushort At(int x, int z) => Attributes[z * Width + x];
        public bool Solid(int x, int z) => (At(x, z) & 0x8000) != 0;
        public byte Behaviour(int x, int z) => (byte)(At(x, z) & 0xFF);
    }

    /// <summary>Where each floor lies in the Distortion World's space, by its map header's number: subtracted from an event's place.</summary>
    public IReadOnlyDictionary<int, (int X, int Altitude, int Z)> Offsets { get; }

    /// <summary>The floating floors of each map, by its map header's number.</summary>
    public IReadOnlyDictionary<int, List<Floor>> Floors { get; }

    private DistortionWorld(Dictionary<int, (int, int, int)> offsets, Dictionary<int, List<Floor>> floors)
    {
        Offsets = offsets;
        Floors = floors;
    }

    /// <summary>Reads the two archives, or null when the checkout doesn't have them.</summary>
    public static DistortionWorld? Load(string root)
    {
        string main = Path.Combine(root, MainArchive), attributes = Path.Combine(root, AttributeArchive);
        return File.Exists(main) && File.Exists(attributes) ? Parse(File.ReadAllBytes(main), File.ReadAllBytes(attributes)) : null;
    }

    // The floating platforms' kinds (FLOATING_PLATFORM_KIND_*): the floor is the only one walked upright
    private const int FloorKind = 0;
    private const int MapInfoSize = 12, MapFileHeaderSize = 20, PlatformSize = 20;

    public static DistortionWorld Parse(byte[] mainArchive, byte[] attributeArchive)
    {
        var main = Narc(mainArchive);
        var attributes = Narc(attributeArchive);
        if (main.Count == 0) throw new InvalidDataException("The Distortion World's archive has no map list");

        // Member 0: a count, then { u32 map header, u16 map file, s16 x, s16 altitude, s16 z } for each map
        var info = main[0].Span;
        int count = BinaryPrimitives.ReadInt32LittleEndian(info);
        var offsets = new Dictionary<int, (int, int, int)>();
        var floors = new Dictionary<int, List<Floor>>();
        for (int i = 0; i < count; i++)
        {
            var entry = info[(4 + i * MapInfoSize)..];
            int header = (int)BinaryPrimitives.ReadUInt32LittleEndian(entry);
            int file = BinaryPrimitives.ReadUInt16LittleEndian(entry[4..]) + 1;
            int ox = BinaryPrimitives.ReadInt16LittleEndian(entry[6..]);
            int oa = BinaryPrimitives.ReadInt16LittleEndian(entry[8..]);
            int oz = BinaryPrimitives.ReadInt16LittleEndian(entry[10..]);
            offsets[header] = (ox, oa, oz);
            if (file >= main.Count) throw new InvalidDataException($"Map {header} of the Distortion World names file {file}, and the archive has {main.Count}");

            // A map file: five sizes (the first unused), then the floating platforms' section: a count and 20 bytes each
            var map = main[file].Span;
            int platformsSize = BinaryPrimitives.ReadInt32LittleEndian(map[4..]);
            if (platformsSize < 4) continue;
            var platforms = map[MapFileHeaderSize..];
            int n = BinaryPrimitives.ReadInt32LittleEndian(platforms);
            for (int p = 0; p < n; p++)
            {
                var t = platforms[(4 + p * PlatformSize)..];
                int kind = BinaryPrimitives.ReadInt16LittleEndian(t);
                int attr = BinaryPrimitives.ReadUInt16LittleEndian(t[2..]);
                int sx = BinaryPrimitives.ReadInt16LittleEndian(t[4..]), sy = BinaryPrimitives.ReadInt16LittleEndian(t[6..]), sz = BinaryPrimitives.ReadInt16LittleEndian(t[8..]);
                int wx = BinaryPrimitives.ReadInt16LittleEndian(t[10..]), wz = BinaryPrimitives.ReadInt16LittleEndian(t[14..]);
                int vertical = BinaryPrimitives.ReadUInt16LittleEndian(t[16..]);
                if (kind != FloorKind) continue;
                if (attr >= attributes.Count) throw new InvalidDataException($"A floor of map {header} names attributes {attr}, and the archive has {attributes.Count}");

                // A floor's bounds count both ends; its attributes run east first, a row of tileCountVertical to each step south
                int width = wx + 1, depth = wz + 1;
                var tiles = new ushort[width * depth];
                var source = attributes[attr].Span;
                for (int z = 0; z < depth; z++)
                    for (int x = 0; x < width; x++)
                    {
                        int at = (x + z * vertical) * 2;
                        tiles[z * width + x] = at + 1 < source.Length ? BinaryPrimitives.ReadUInt16LittleEndian(source[at..]) : (ushort)0x8000;
                    }
                if (!floors.TryGetValue(header, out var list)) floors[header] = list = new();
                list.Add(new Floor(sx - ox, sz - oz, width, depth, sy - oa, tiles));
            }
        }
        return new DistortionWorld(offsets, floors);
    }

    /// <summary>
    /// The members of a Nitro archive: its header, then the allocation table (<c>BTAF</c>: a count and a start and an end
    /// for each member), the names (<c>BTNF</c>) and the members themselves (<c>GMIF</c>).
    /// </summary>
    public static List<ReadOnlyMemory<byte>> Narc(byte[] file)
    {
        var span = file.AsSpan();
        if (span.Length < 16 || span[0] != 'N' || span[1] != 'A' || span[2] != 'R' || span[3] != 'C')
            throw new InvalidDataException("Not a Nitro archive");
        int at = BinaryPrimitives.ReadUInt16LittleEndian(span[12..]);
        (int Start, int End)[]? table = null;
        int data = -1;
        while (at + 8 <= span.Length)
        {
            string magic = System.Text.Encoding.ASCII.GetString(span.Slice(at, 4));
            int size = BinaryPrimitives.ReadInt32LittleEndian(span[(at + 4)..]);
            if (size < 8) throw new InvalidDataException($"The archive's {magic} section is {size} bytes long");
            if (magic == "BTAF")
            {
                int count = BinaryPrimitives.ReadInt32LittleEndian(span[(at + 8)..]);
                table = new (int, int)[count];
                for (int i = 0; i < count; i++)
                    table[i] = (BinaryPrimitives.ReadInt32LittleEndian(span[(at + 12 + i * 8)..]), BinaryPrimitives.ReadInt32LittleEndian(span[(at + 16 + i * 8)..]));
            }
            else if (magic == "GMIF") data = at + 8;
            at += size;
        }
        if (table == null || data < 0) throw new InvalidDataException("The archive has no allocation table or no members");
        return table.Select(t => new ReadOnlyMemory<byte>(file, data + t.Start, t.End - t.Start)).ToList();
    }
}
