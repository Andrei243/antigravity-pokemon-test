using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// A Windows icon file (<c>.ico</c>) put together from PNG pictures of a square at several sizes (plan 16 · T2: the
/// harness's <c>icon</c> mode writes <c>Assets/icon.ico</c> with it, which the project file makes the executable's icon).
/// Every Windows since Vista reads a picture of an icon stored as PNG, so nothing is turned into bitmaps. GPU-free.
/// </summary>
internal static class IconFile
{
    private const int HeaderSize = 6, EntrySize = 16;

    /// <summary>The bytes of an icon holding these pictures, largest first, each a PNG of a square from 1 to 256 wide.</summary>
    public static byte[] Write(IReadOnlyList<(int Size, byte[] Png)> pictures)
    {
        if (pictures.Count == 0) throw new ArgumentException("An icon needs at least one picture.", nameof(pictures));
        foreach (var (size, png) in pictures)
        {
            if (size is < 1 or > 256) throw new ArgumentOutOfRangeException(nameof(pictures), $"An icon's picture is 1 to 256 wide, not {size}.");
            if (!IsPng(png)) throw new ArgumentException($"The {size}-wide picture is no PNG.", nameof(pictures));
        }
        var ordered = pictures.OrderByDescending(p => p.Size).ToList();
        using var stream = new MemoryStream();
        Span<byte> buffer = stackalloc byte[EntrySize];

        // ICONDIR: reserved, type 1 (an icon), the count
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, 0);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[2..], 1);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[4..], (ushort)ordered.Count);
        stream.Write(buffer[..HeaderSize]);

        // ICONDIRENTRY each: width and height (0 is 256), no palette, reserved, one plane, 32 bits, the size and where it is
        int offset = HeaderSize + EntrySize * ordered.Count;
        foreach (var (size, png) in ordered)
        {
            buffer.Clear();
            buffer[0] = (byte)(size == 256 ? 0 : size);
            buffer[1] = (byte)(size == 256 ? 0 : size);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[4..], 1);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer[6..], 32);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer[8..], (uint)png.Length);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer[12..], (uint)offset);
            stream.Write(buffer);
            offset += png.Length;
        }
        foreach (var (_, png) in ordered) stream.Write(png);
        return stream.ToArray();
    }

    /// <summary>The sizes an icon holds, read back from its directory, in the order it lists them.</summary>
    public static IReadOnlyList<int> SizesIn(byte[] icon)
    {
        if (icon.Length < HeaderSize || BinaryPrimitives.ReadUInt16LittleEndian(icon.AsSpan(2)) != 1)
            throw new InvalidDataException("This is no icon file.");
        int count = BinaryPrimitives.ReadUInt16LittleEndian(icon.AsSpan(4));
        var sizes = new List<int>(count);
        for (int i = 0; i < count; i++)
        {
            int at = HeaderSize + i * EntrySize;
            if (at + EntrySize > icon.Length) throw new InvalidDataException("The icon's directory runs past its end.");
            int width = icon[at] == 0 ? 256 : icon[at];
            int length = (int)BinaryPrimitives.ReadUInt32LittleEndian(icon.AsSpan(at + 8));
            int offset = (int)BinaryPrimitives.ReadUInt32LittleEndian(icon.AsSpan(at + 12));
            if (offset + length > icon.Length || !IsPng(icon.AsSpan(offset, length).ToArray()))
                throw new InvalidDataException($"The icon's {width}-wide picture is not where its directory says.");
            sizes.Add(width);
        }
        return sizes;
    }

    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    private static bool IsPng(byte[] bytes) => bytes.Length > PngSignature.Length && bytes.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature);
}
