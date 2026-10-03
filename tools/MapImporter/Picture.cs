using System.Buffers.Binary;
using System.IO.Compression;

namespace MapImporter;

public readonly record struct Rgb(byte R, byte G, byte B)
{
    public static Rgb Of(int r, int g, int b) => new((byte)r, (byte)g, (byte)b);

    public Rgb Mix(Rgb other, float t) => new(
        (byte)MathF.Round(R + (other.R - R) * t),
        (byte)MathF.Round(G + (other.G - G) * t),
        (byte)MathF.Round(B + (other.B - B) * t));

    public Rgb Darker(float amount) => Mix(Of(16, 18, 32), amount);
    public Rgb Lighter(float amount) => Mix(Of(255, 255, 255), amount);
}

/// <summary>A plain image the importer draws its maps into and saves as PNG, without any graphics library.</summary>
public sealed class Picture
{
    public int Width { get; }
    public int Height { get; }
    private readonly byte[] pixels;

    public Picture(int width, int height, Rgb? background = null)
    {
        Width = width;
        Height = height;
        pixels = new byte[width * height * 3];
        if (background is { } c) Fill(0, 0, width, height, c);
    }

    public Rgb Get(int x, int y)
    {
        int i = (y * Width + x) * 3;
        return new Rgb(pixels[i], pixels[i + 1], pixels[i + 2]);
    }

    public void Set(int x, int y, Rgb c, float alpha = 1f)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return;
        if (alpha < 1f) c = Get(x, y).Mix(c, alpha);
        int i = (y * Width + x) * 3;
        pixels[i] = c.R;
        pixels[i + 1] = c.G;
        pixels[i + 2] = c.B;
    }

    public void Fill(int x, int y, int w, int h, Rgb c, float alpha = 1f)
    {
        for (int py = Math.Max(0, y); py < Math.Min(Height, y + h); py++)
            for (int px = Math.Max(0, x); px < Math.Min(Width, x + w); px++)
                Set(px, py, c, alpha);
    }

    public void Frame(int x, int y, int w, int h, Rgb c, int thickness = 1)
    {
        Fill(x, y, w, thickness, c);
        Fill(x, y + h - thickness, w, thickness, c);
        Fill(x, y, thickness, h, c);
        Fill(x + w - thickness, y, thickness, h, c);
    }

    public void Blit(Picture source, int x, int y)
    {
        for (int py = 0; py < source.Height; py++)
            for (int px = 0; px < source.Width; px++)
                Set(x + px, y + py, source.Get(px, py));
    }

    public Picture Crop(int x, int y, int w, int h)
    {
        var result = new Picture(w, h);
        for (int py = 0; py < h; py++)
            for (int px = 0; px < w; px++)
                if (x + px >= 0 && y + py >= 0 && x + px < Width && y + py < Height) result.Set(px, py, Get(x + px, y + py));
        return result;
    }

    // ---------------------------------------------------------------- lettering

    // A 3×5 face: five rows of three cells per glyph
    private static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
    {
        ['A'] = ".#./#.#/###/#.#/#.#", ['B'] = "##./#.#/##./#.#/##.", ['C'] = ".##/#../#../#../.##", ['D'] = "##./#.#/#.#/#.#/##.",
        ['E'] = "###/#../##./#../###", ['F'] = "###/#../##./#../#..", ['G'] = ".##/#../#.#/#.#/.##", ['H'] = "#.#/#.#/###/#.#/#.#",
        ['I'] = "###/.#./.#./.#./###", ['J'] = "..#/..#/..#/#.#/.#.", ['K'] = "#.#/#.#/##./#.#/#.#", ['L'] = "#../#../#../#../###",
        ['M'] = "#.#/###/###/#.#/#.#", ['N'] = "##./#.#/#.#/#.#/#.#", ['O'] = ".#./#.#/#.#/#.#/.#.", ['P'] = "##./#.#/##./#../#..",
        ['Q'] = ".#./#.#/#.#/###/.##", ['R'] = "##./#.#/##./#.#/#.#", ['S'] = ".##/#../.#./..#/##.", ['T'] = "###/.#./.#./.#./.#.",
        ['U'] = "#.#/#.#/#.#/#.#/###", ['V'] = "#.#/#.#/#.#/#.#/.#.", ['W'] = "#.#/#.#/###/###/#.#", ['X'] = "#.#/#.#/.#./#.#/#.#",
        ['Y'] = "#.#/#.#/.#./.#./.#.", ['Z'] = "###/..#/.#./#../###",
        ['0'] = "###/#.#/#.#/#.#/###", ['1'] = ".#./##./.#./.#./###", ['2'] = "##./..#/.#./#../###", ['3'] = "##./..#/.#./..#/##.",
        ['4'] = "#.#/#.#/###/..#/..#", ['5'] = "###/#../##./..#/##.", ['6'] = ".##/#../###/#.#/###", ['7'] = "###/..#/.#./.#./.#.",
        ['8'] = "###/#.#/###/#.#/###", ['9'] = "###/#.#/###/..#/##.",
        ['-'] = ".../.../###/.../...", ['.'] = ".../.../.../.../.#.", ['_'] = ".../.../.../.../###", ['/'] = "..#/..#/.#./#../#..",
        [':'] = ".../.#./.../.#./...", ['+'] = ".../.#./###/.#./...", ['\''] = ".#./.#./.../.../...", [','] = ".../.../.../.#./#.."
    }.ToDictionary(kv => kv.Key, kv => kv.Value.Replace("/", ""));

    public static int TextWidth(string text, int scale = 1) => text.Length == 0 ? 0 : (text.Length * 4 - 1) * scale;

    public const int TextHeight = 5;

    public void Text(int x, int y, string text, Rgb c, int scale = 1, Rgb? shadow = null)
    {
        if (shadow is { } s) Draw(x + scale, y + scale, text, s, scale);
        Draw(x, y, text, c, scale);
    }

    private void Draw(int x, int y, string text, Rgb c, int scale)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (!Glyphs.TryGetValue(char.ToUpperInvariant(text[i]), out var cells)) continue;
            for (int cell = 0; cell < Math.Min(15, cells.Length); cell++)
                if (cells[cell] == '#')
                    Fill(x + (i * 4 + cell % 3) * scale, y + cell / 3 * scale, scale, scale, c);
        }
    }

    // ---------------------------------------------------------------- PNG

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var file = File.Create(path);
        file.Write(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A });

        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, Width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), Height);
        header[8] = 8;   // bits per channel
        header[9] = 2;   // truecolour
        Chunk(file, "IHDR", header);

        using var packed = new MemoryStream();
        using (var zlib = new ZLibStream(packed, CompressionLevel.Optimal, leaveOpen: true))
        {
            for (int y = 0; y < Height; y++)
            {
                zlib.WriteByte(0);   // no filter
                zlib.Write(pixels, y * Width * 3, Width * 3);
            }
        }
        Chunk(file, "IDAT", packed.ToArray());
        Chunk(file, "IEND", Array.Empty<byte>());
    }

    private static void Chunk(Stream file, string type, byte[] data)
    {
        Span<byte> word = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(word, data.Length);
        file.Write(word);

        var tag = System.Text.Encoding.ASCII.GetBytes(type);
        file.Write(tag);
        file.Write(data);
        BinaryPrimitives.WriteUInt32BigEndian(word, Crc(data, Crc(tag, 0xFFFFFFFF)) ^ 0xFFFFFFFF);
        file.Write(word);
    }

    private static readonly uint[] CrcTable = Enumerable.Range(0, 256).Select(n =>
    {
        uint c = (uint)n;
        for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
        return c;
    }).ToArray();

    private static uint Crc(byte[] data, uint crc)
    {
        foreach (byte b in data) crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc;
    }
}
