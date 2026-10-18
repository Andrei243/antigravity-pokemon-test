using System.Buffers.Binary;
using System.Text;

namespace MapImporter;

/// <summary>
/// A Nitro archive (<c>.narc</c>, or a file of <c>files/a/</c>), the DS's bundle of files (plan 20 · J1): a header,
/// the allocation table (<c>BTAF</c>: where each member starts and ends), the name table (<c>BTNF</c>, which the
/// archives HeartGold's importer reads leave empty) and the image (<c>GMIF</c>) the members lie in. Only the table
/// and the members' bytes are read; what a member holds is its reader's business.
/// </summary>
public sealed class Narc
{
    private readonly byte[] file;
    private readonly (int Start, int End)[] members;
    private readonly int image;

    public int Count => members.Length;

    private Narc(byte[] file, (int, int)[] members, int image) => (this.file, this.members, this.image) = (file, members, image);

    /// <summary>The bytes of member <paramref name="index"/>.</summary>
    public ReadOnlySpan<byte> Member(int index)
    {
        if (index < 0 || index >= members.Length) throw new ArgumentOutOfRangeException(nameof(index), $"The archive has {members.Length} members, not {index + 1}");
        var (start, end) = members[index];
        return file.AsSpan(image + start, end - start);
    }

    public static Narc Read(string path) => Parse(File.ReadAllBytes(path));

    public static Narc Parse(byte[] file)
    {
        if (file.Length < 16 || Encoding.ASCII.GetString(file, 0, 4) != "NARC") throw new InvalidDataException("Not a NARC: it doesn't start with NARC");
        int at = BinaryPrimitives.ReadUInt16LittleEndian(file.AsSpan(12));
        Section(file, at, "BTAF");
        int btafSize = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(at + 4));
        int count = BinaryPrimitives.ReadUInt16LittleEndian(file.AsSpan(at + 8));
        if (12 + count * 8 > btafSize) throw new InvalidDataException($"The NARC's allocation table is too short for its {count} members");
        var members = new (int, int)[count];
        for (int i = 0; i < count; i++)
            members[i] = (BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(at + 12 + i * 8)), BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(at + 16 + i * 8)));
        at += btafSize;
        Section(file, at, "BTNF");
        at += BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(at + 4));
        Section(file, at, "GMIF");
        int imageSize = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(at + 4)) - 8;
        int image = at + 8;
        foreach (var (start, end) in members)
            if (start < 0 || end < start || end > imageSize || image + end > file.Length)
                throw new InvalidDataException($"A member of the NARC ({start}..{end}) lies outside its image of {imageSize} bytes");
        return new Narc(file, members, image);
    }

    private static void Section(byte[] file, int at, string magic)
    {
        if (at + 8 > file.Length || Encoding.ASCII.GetString(file, at, 4) != magic)
            throw new InvalidDataException($"The NARC has no {magic} section where its header says");
    }

    /// <summary>An archive of the given members, for tests: the inverse of <see cref="Parse"/>.</summary>
    public static byte[] Build(IReadOnlyList<byte[]> members)
    {
        var image = new MemoryStream();
        var table = new List<(int, int)>();
        foreach (var m in members)
        {
            int start = (int)image.Length;
            image.Write(m);
            table.Add((start, (int)image.Length));
            while (image.Length % 4 != 0) image.WriteByte(0xFF);
        }
        int btafSize = 12 + members.Count * 8;
        var output = new MemoryStream();
        var w = new BinaryWriter(output);
        w.Write(Encoding.ASCII.GetBytes("NARC"));
        w.Write((ushort)0xFFFE); w.Write((ushort)0x0100);
        w.Write(16 + btafSize + 16 + 8 + (int)image.Length);
        w.Write((ushort)16); w.Write((ushort)3);
        w.Write(Encoding.ASCII.GetBytes("BTAF")); w.Write(btafSize); w.Write((ushort)members.Count); w.Write((ushort)0);
        foreach (var (s, e) in table) { w.Write(s); w.Write(e); }
        w.Write(Encoding.ASCII.GetBytes("BTNF")); w.Write(16); w.Write(4); w.Write((ushort)0); w.Write((ushort)1);
        w.Write(Encoding.ASCII.GetBytes("GMIF")); w.Write(8 + (int)image.Length);
        w.Write(image.ToArray());
        return output.ToArray();
    }
}
