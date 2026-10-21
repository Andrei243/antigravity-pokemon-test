using System;
using System.Buffers.Binary;

namespace PokemonPlatinumEngine.Foreign;

/// <summary>
/// The little reading helpers every reader of a foreign file shares (plan 08 · P20). All of the readers take
/// <see cref="ReadOnlySpan{T}"/>s: nothing here can write to a file's bytes.
/// </summary>
internal static class ForeignBytes
{
    public static ushort U16(ReadOnlySpan<byte> data, int at) => BinaryPrimitives.ReadUInt16LittleEndian(data[at..]);
    public static uint U32(ReadOnlySpan<byte> data, int at) => BinaryPrimitives.ReadUInt32LittleEndian(data[at..]);

    /// <summary>Bits <paramref name="from"/> to <paramref name="from"/> + <paramref name="count"/> - 1 of a word (the C bitfields of both consoles fill from the low end).</summary>
    public static int Bits(uint word, int from, int count) => (int)((word >> from) & ((1u << count) - 1));

    /// <summary>The checksum of a sector of the GBA's saves: the 32-bit words added, then their two halves (<c>ov97_02235E7C</c>).</summary>
    public static ushort GbaSectorChecksum(ReadOnlySpan<byte> data, int size)
    {
        uint sum = 0;
        for (int i = 0; i < size / 4; i++) sum += U32(data, i * 4);
        return (ushort)(((sum >> 16) & 0xFFFF) + (sum & 0xFFFF));
    }

    /// <summary>The sum of 16-bit words a Pokémon's checksum is, on either console (<c>CalculateGBABoxMonChecksum</c>, <c>Pokemon_GetDataChecksum</c>).</summary>
    public static ushort WordSum(ReadOnlySpan<byte> data)
    {
        ushort sum = 0;
        for (int i = 0; i + 1 < data.Length; i += 2) sum += U16(data, i);
        return sum;
    }

    private static readonly ushort[] Crc16Table = BuildCrcTable();

    private static ushort[] BuildCrcTable()
    {
        var table = new ushort[256];
        for (int i = 0; i < 256; i++)
        {
            int value = i << 8;
            for (int bit = 0; bit < 8; bit++) value = (value & 0x8000) != 0 ? (value << 1) ^ 0x1021 : value << 1;
            table[i] = (ushort)value;
        }
        return table;
    }

    /// <summary>
    /// The checksum of a block of a Platinum save (<c>CalcCRC16Checksum</c>, the SDK's CRC-16/CCITT, whose code the
    /// decompilation leaves out): the polynomial 0x1021 taken from the high bit, started at 0xFFFF.
    /// </summary>
    public static ushort Crc16(ReadOnlySpan<byte> data)
    {
        ushort crc = 0xFFFF;
        foreach (byte b in data) crc = (ushort)((crc << 8) ^ Crc16Table[(crc >> 8) ^ b]);
        return crc;
    }

    /// <summary>The same polynomial taken from the low bit (0x8408), the other way the SDK's name can be read.</summary>
    public static ushort Crc16Reflected(ReadOnlySpan<byte> data)
    {
        ushort crc = 0xFFFF;
        foreach (byte b in data)
        {
            crc ^= b;
            for (int bit = 0; bit < 8; bit++) crc = (ushort)((crc & 1) != 0 ? (crc >> 1) ^ 0x8408 : crc >> 1);
        }
        return crc;
    }

    /// <summary>Whether a Platinum block's stored checksum is the block's, by either reading of the SDK's CRC-16/CCITT (a save is the check no file here is).</summary>
    public static bool Crc16Matches(ReadOnlySpan<byte> data, ushort stored) => Crc16(data) == stored || Crc16Reflected(data) == stored;
}
