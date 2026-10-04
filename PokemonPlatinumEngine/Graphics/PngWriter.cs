using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>Encodes a <see cref="PixelCanvas"/> as an RGBA PNG without native code (for model files written by <see cref="GltfWriter"/>).</summary>
internal static class PngWriter
{
    private static readonly uint[] Crc = BuildCrc();

    public static byte[] Encode(PixelCanvas canvas)
    {
        int w = canvas.Width, h = canvas.Height;
        var raw = new byte[h * (w * 4 + 1)];
        for (int y = 0, at = 0; y < h; y++)
        {
            raw[at++] = 0; // no filter
            for (int x = 0; x < w; x++)
            {
                var c = canvas.Get(x, y);
                raw[at++] = c.R;
                raw[at++] = c.G;
                raw[at++] = c.B;
                raw[at++] = c.A;
            }
        }
        using var zipped = new MemoryStream();
        using (var z = new ZLibStream(zipped, CompressionLevel.Optimal, leaveOpen: true)) z.Write(raw);

        using var png = new MemoryStream();
        png.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        var header = new byte[13];
        WriteBig(header, 0, (uint)w);
        WriteBig(header, 4, (uint)h);
        header[8] = 8;  // bits per channel
        header[9] = 6;  // RGBA
        Chunk(png, "IHDR", header);
        Chunk(png, "IDAT", zipped.ToArray());
        Chunk(png, "IEND", Array.Empty<byte>());
        return png.ToArray();
    }

    private static void Chunk(Stream s, string type, byte[] data)
    {
        var head = new byte[8];
        WriteBig(head, 0, (uint)data.Length);
        Encoding.ASCII.GetBytes(type, 0, 4, head, 4);
        s.Write(head);
        s.Write(data);
        uint crc = 0xFFFFFFFFu;
        for (int i = 4; i < 8; i++) crc = Crc[(crc ^ head[i]) & 0xFF] ^ (crc >> 8);
        foreach (byte b in data) crc = Crc[(crc ^ b) & 0xFF] ^ (crc >> 8);
        var tail = new byte[4];
        WriteBig(tail, 0, crc ^ 0xFFFFFFFFu);
        s.Write(tail);
    }

    private static void WriteBig(byte[] b, int at, uint v)
    {
        b[at] = (byte)(v >> 24);
        b[at + 1] = (byte)(v >> 16);
        b[at + 2] = (byte)(v >> 8);
        b[at + 3] = (byte)v;
    }

    private static uint[] BuildCrc()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            table[n] = c;
        }
        return table;
    }
}
