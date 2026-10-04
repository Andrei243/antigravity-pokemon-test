using System;
using System.Collections.Generic;
using System.IO;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Decodes JPEG images (baseline and progressive, Huffman-coded, any chroma subsampling, restart markers) into a
/// <see cref="PixelCanvas"/>, for the textures of imported models: glTF allows PNG and JPEG, and the raylib the game
/// ships with reads only PNG. Arithmetic-coded and lossless JPEGs aren't supported. No GPU or native calls.
/// </summary>
internal static class JpegDecoder
{
    private static readonly int[] ZigZag =
    {
        0, 1, 8, 16, 9, 2, 3, 10, 17, 24, 32, 25, 18, 11, 4, 5, 12, 19, 26, 33, 40, 48, 41, 34, 27, 20, 13, 6, 7, 14, 21,
        28, 35, 42, 49, 56, 57, 50, 43, 36, 29, 22, 15, 23, 30, 37, 44, 51, 58, 59, 52, 45, 38, 31, 39, 46, 53, 60, 61,
        54, 47, 55, 62, 63
    };

    private sealed class Huffman
    {
        // Canonical code tables: for each code length, the first code and the index of its first symbol
        public readonly int[] MaxCode = new int[18];
        public readonly int[] ValOffset = new int[18];
        public byte[] Values = Array.Empty<byte>();

        public Huffman(byte[] counts, byte[] values)
        {
            Values = values;
            int code = 0, k = 0;
            for (int len = 1; len <= 16; len++)
            {
                ValOffset[len] = k - code;
                code += counts[len - 1];
                k += counts[len - 1];
                MaxCode[len] = counts[len - 1] > 0 ? code - 1 : -1;
                code <<= 1;
            }
            MaxCode[17] = int.MaxValue;
        }
    }

    private sealed class Component
    {
        public int Id, H, V, Quant;

        /// <summary>Blocks across and down including the padding of whole MCUs, and those the image itself covers.</summary>
        public int BlocksPerLine, BlocksPerColumn, BlocksAcross, BlocksDown;
        public short[] Coefficients = Array.Empty<short>();
        public Huffman? Dc, Ac;
        public int Pred;

        public int Offset(int row, int col) => (row * (BlocksPerLine + 1) + col) * 64;
    }

    private sealed class Reader
    {
        private readonly byte[] data;
        public int Pos;
        private int bitBuffer, bitCount;
        public bool HitMarker;

        public Reader(byte[] data, int pos)
        {
            this.data = data;
            Pos = pos;
        }

        public int Bit()
        {
            if (bitCount == 0)
            {
                if (Pos >= data.Length) { HitMarker = true; return 0; }
                int b = data[Pos];
                if (b == 0xFF)
                {
                    int next = Pos + 1 < data.Length ? data[Pos + 1] : 0;
                    if (next == 0) Pos += 2;
                    else { HitMarker = true; return 0; }  // a marker: feed zeros until the scan sees it
                }
                else Pos++;
                bitBuffer = b;
                bitCount = 8;
            }
            bitCount--;
            return (bitBuffer >> bitCount) & 1;
        }

        public int Bits(int n)
        {
            int v = 0;
            for (int i = 0; i < n; i++) v = (v << 1) | Bit();
            return v;
        }

        public int Decode(Huffman h)
        {
            int code = 0;
            for (int len = 1; len <= 16; len++)
            {
                code = (code << 1) | Bit();
                if (code <= h.MaxCode[len])
                {
                    int index = code + h.ValOffset[len];
                    return index >= 0 && index < h.Values.Length ? h.Values[index] : 0;
                }
            }
            return 0;
        }

        /// <summary>A coefficient of <paramref name="n"/> bits with the JPEG sign convention.</summary>
        public int Receive(int n)
        {
            if (n == 0) return 0;
            int v = Bits(n);
            return v < 1 << (n - 1) ? v - (1 << n) + 1 : v;
        }

        public void Align()
        {
            bitCount = 0;
            HitMarker = false;
        }
    }

    public static PixelCanvas Decode(byte[] data)
    {
        if (data.Length < 4 || data[0] != 0xFF || data[1] != 0xD8) throw new InvalidDataException("not a JPEG");
        var quant = new int[4][];
        var dcTables = new Huffman[4];
        var acTables = new Huffman[4];
        var components = new List<Component>();
        int width = 0, height = 0, restart = 0, mcusPerLine = 0, mcusPerColumn = 0, maxH = 1, maxV = 1;
        bool progressive = false, adobe = false;
        int adobeTransform = -1;
        int pos = 2;

        while (pos + 4 <= data.Length)
        {
            if (data[pos] != 0xFF) { pos++; continue; }
            int marker = data[pos + 1];
            pos += 2;
            if (marker == 0xD8 || (marker >= 0xD0 && marker <= 0xD7) || marker == 0x01 || marker == 0x00 || marker == 0xFF)
            {
                if (marker == 0xFF) pos--; // fill bytes before a marker
                continue;
            }
            if (marker == 0xD9) break;
            int length = (data[pos] << 8) | data[pos + 1];
            int segment = pos + 2, end = pos + length;
            if (end > data.Length) throw new InvalidDataException("a JPEG segment runs past the end");
            switch (marker)
            {
                case 0xDB:
                    for (int p = segment; p < end;)
                    {
                        int pq = data[p] >> 4, tq = data[p] & 15;
                        p++;
                        var table = new int[64];
                        for (int i = 0; i < 64; i++)
                        {
                            table[ZigZag[i]] = pq == 0 ? data[p] : (data[p] << 8) | data[p + 1];
                            p += pq == 0 ? 1 : 2;
                        }
                        quant[tq & 3] = table;
                    }
                    break;
                case 0xC0:
                case 0xC1:
                case 0xC2:
                {
                    progressive = marker == 0xC2;
                    height = (data[segment + 1] << 8) | data[segment + 2];
                    width = (data[segment + 3] << 8) | data[segment + 4];
                    int count = data[segment + 5];
                    for (int i = 0; i < count; i++)
                    {
                        int at = segment + 6 + i * 3;
                        components.Add(new Component { Id = data[at], H = Math.Max(1, data[at + 1] >> 4), V = Math.Max(1, data[at + 1] & 15), Quant = data[at + 2] & 3 });
                    }
                    foreach (var c in components)
                    {
                        maxH = Math.Max(maxH, c.H);
                        maxV = Math.Max(maxV, c.V);
                    }
                    mcusPerLine = (width + 8 * maxH - 1) / (8 * maxH);
                    mcusPerColumn = (height + 8 * maxV - 1) / (8 * maxV);
                    foreach (var c in components)
                    {
                        c.BlocksPerLine = mcusPerLine * c.H;
                        c.BlocksPerColumn = mcusPerColumn * c.V;
                        c.BlocksAcross = ((width * c.H + maxH - 1) / maxH + 7) / 8;
                        c.BlocksDown = ((height * c.V + maxV - 1) / maxV + 7) / 8;
                        c.Coefficients = new short[(c.BlocksPerColumn + 1) * (c.BlocksPerLine + 1) * 64];
                    }
                    if (width <= 0 || height <= 0 || components.Count == 0) throw new InvalidDataException("a JPEG without size");
                    break;
                }
                case 0xC3: case 0xC5: case 0xC6: case 0xC7: case 0xC9: case 0xCA: case 0xCB: case 0xCD: case 0xCE: case 0xCF:
                    throw new NotSupportedException("lossless and arithmetic-coded JPEGs can't be read; save the texture as PNG");
                case 0xC4:
                    for (int p = segment; p < end;)
                    {
                        int tc = data[p] >> 4, th = data[p] & 3;
                        var counts = new byte[16];
                        Array.Copy(data, p + 1, counts, 0, 16);
                        int total = 0;
                        foreach (byte n in counts) total += n;
                        var values = new byte[total];
                        Array.Copy(data, p + 17, values, 0, total);
                        p += 17 + total;
                        if (tc == 0) dcTables[th] = new Huffman(counts, values);
                        else acTables[th] = new Huffman(counts, values);
                    }
                    break;
                case 0xDD:
                    restart = (data[segment] << 8) | data[segment + 1];
                    break;
                case 0xEE:
                    if (length >= 12 && data[segment] == 'A' && data[segment + 1] == 'd' && data[segment + 2] == 'o' && data[segment + 3] == 'b' && data[segment + 4] == 'e')
                    {
                        adobe = true;
                        adobeTransform = data[segment + 11];
                    }
                    break;
                case 0xDA:
                {
                    int count = data[segment];
                    var scan = new List<Component>();
                    for (int i = 0; i < count; i++)
                    {
                        int id = data[segment + 1 + i * 2], tables = data[segment + 2 + i * 2];
                        var c = components.Find(x => x.Id == id) ?? throw new InvalidDataException("a scan names a component that doesn't exist");
                        c.Dc = dcTables[tables >> 4];
                        c.Ac = acTables[tables & 3];
                        scan.Add(c);
                    }
                    int at = segment + 1 + count * 2;
                    int ss = data[at], se = data[at + 1], ah = data[at + 2] >> 4, al = data[at + 2] & 15;
                    pos = Scan(data, end, scan, mcusPerLine, mcusPerColumn, restart, progressive, ss, se, ah, al);
                    continue;
                }
            }
            pos = end;
        }
        if (components.Count == 0) throw new InvalidDataException("a JPEG without image data");
        return Assemble(components, quant, width, height, maxH, maxV, adobe, adobeTransform);
    }

    /// <summary>Decodes one scan into the components' coefficients; returns where the data after it starts.</summary>
    private static int Scan(byte[] data, int pos, List<Component> scan, int mcusPerLine, int mcusPerColumn, int restart, bool progressive,
        int ss, int se, int ah, int al)
    {
        var r = new Reader(data, pos);
        bool single = scan.Count == 1;
        var only = scan[0];
        // A scan of one component walks the blocks the image covers; an interleaved one walks whole MCUs
        int expected = single ? only.BlocksAcross * only.BlocksDown : mcusPerLine * mcusPerColumn;
        int interval = restart > 0 ? restart : expected;
        int n = 0;
        while (n < expected)
        {
            int eobRun = 0;
            foreach (var c in scan) c.Pred = 0;
            for (int i = 0; i < interval && n < expected; i++, n++)
            {
                if (single)
                {
                    Block(r, only, only.Offset(n / only.BlocksAcross, n % only.BlocksAcross), progressive, ss, se, ah, al, ref eobRun);
                    continue;
                }
                int mcuRow = n / mcusPerLine, mcuCol = n % mcusPerLine;
                foreach (var c in scan)
                    for (int v = 0; v < c.V; v++)
                        for (int h = 0; h < c.H; h++)
                            Block(r, c, c.Offset(mcuRow * c.V + v, mcuCol * c.H + h), progressive, ss, se, ah, al, ref eobRun);
            }
            // The bits left in the last byte are padding; a restart marker starts the next interval
            r.Align();
            if (n < expected && r.Pos + 1 < data.Length && data[r.Pos] == 0xFF && data[r.Pos + 1] >= 0xD0 && data[r.Pos + 1] <= 0xD7) r.Pos += 2;
            else if (n < expected) break;
        }
        // On to the next marker that isn't a restart, a stuffed byte or fill
        int p = r.Pos;
        while (p + 1 < data.Length && !(data[p] == 0xFF && data[p + 1] != 0x00 && data[p + 1] != 0xFF && (data[p + 1] < 0xD0 || data[p + 1] > 0xD7))) p++;
        return p;
    }

    private static void Block(Reader r, Component c, int at, bool progressive, int ss, int se, int ah, int al, ref int eobRun)
    {
        var z = c.Coefficients;
        if (!progressive)
        {
            int t = r.Decode(c.Dc!);
            c.Pred += r.Receive(t);
            z[at] = (short)c.Pred;
            for (int k = 1; k < 64;)
            {
                int rs = r.Decode(c.Ac!);
                int s = rs & 15, run = rs >> 4;
                if (s == 0)
                {
                    if (run < 15) break;
                    k += 16;
                    continue;
                }
                k += run;
                if (k > 63) break;
                z[at + ZigZag[k]] = (short)r.Receive(s);
                k++;
            }
            return;
        }

        if (ss == 0)
        {
            // DC: the first pass, or one more bit of it
            if (ah == 0)
            {
                int t = r.Decode(c.Dc!);
                c.Pred += r.Receive(t);
                z[at] = (short)(c.Pred << al);
            }
            else if (r.Bit() == 1) z[at] |= (short)(1 << al);
            return;
        }

        if (ah == 0)
        {
            // AC, first pass
            if (eobRun > 0)
            {
                eobRun--;
                return;
            }
            for (int k = ss; k <= se;)
            {
                int rs = r.Decode(c.Ac!);
                int s = rs & 15, run = rs >> 4;
                if (s == 0)
                {
                    if (run < 15)
                    {
                        eobRun = (1 << run) - 1 + (run > 0 ? r.Bits(run) : 0);
                        break;
                    }
                    k += 16;
                    continue;
                }
                k += run;
                if (k > 63) break;
                z[at + ZigZag[k]] = (short)(r.Receive(s) * (1 << al));
                k++;
            }
            return;
        }

        // AC, refining: one more bit for every coefficient already set, new ones placed among the zeros
        int p1 = 1 << al, m1 = -1 << al;
        int kk = ss;
        if (eobRun <= 0)
        {
            for (; kk <= se;)
            {
                int rs = r.Decode(c.Ac!);
                int s = rs & 15, run = rs >> 4;
                int value = 0;
                if (s == 0)
                {
                    if (run < 15)
                    {
                        eobRun = (1 << run) + (run > 0 ? r.Bits(run) : 0);
                        break;
                    }
                }
                else value = r.Bit() == 1 ? p1 : m1;
                while (kk <= se)
                {
                    int idx = at + ZigZag[kk];
                    if (z[idx] != 0)
                    {
                        if (r.Bit() == 1 && (z[idx] & p1) == 0) z[idx] = (short)(z[idx] >= 0 ? z[idx] + p1 : z[idx] + m1);
                    }
                    else
                    {
                        if (run == 0)
                        {
                            if (value != 0) z[idx] = (short)value;
                            kk++;
                            break;
                        }
                        run--;
                    }
                    kk++;
                }
            }
        }
        if (eobRun > 0)
        {
            for (; kk <= se; kk++)
            {
                int idx = at + ZigZag[kk];
                if (z[idx] != 0 && r.Bit() == 1 && (z[idx] & p1) == 0) z[idx] = (short)(z[idx] >= 0 ? z[idx] + p1 : z[idx] + m1);
            }
            eobRun--;
        }
    }

    // ------------------------------------------------------------------ pixels

    private static PixelCanvas Assemble(List<Component> components, int[][] quant, int width, int height, int maxH, int maxV, bool adobe, int transform)
    {
        // Each component's samples at its own resolution, through the inverse DCT
        var planes = new byte[components.Count][];
        var planeWidth = new int[components.Count];
        Span<float> block = stackalloc float[64];
        Span<float> tmp = stackalloc float[64];
        for (int ci = 0; ci < components.Count; ci++)
        {
            var c = components[ci];
            var q = quant[c.Quant] ?? throw new InvalidDataException("a JPEG component without its quantization table");
            int w = c.BlocksPerLine * 8, h = c.BlocksPerColumn * 8;
            var plane = new byte[w * h];
            for (int row = 0; row < c.BlocksPerColumn; row++)
                for (int col = 0; col < c.BlocksPerLine; col++)
                {
                    int at = c.Offset(row, col);
                    for (int i = 0; i < 64; i++) block[i] = c.Coefficients[at + i] * q[i];
                    InverseDct(block, tmp);
                    for (int y = 0; y < 8; y++)
                        for (int x = 0; x < 8; x++)
                            plane[(row * 8 + y) * w + col * 8 + x] = (byte)Math.Clamp((int)MathF.Round(block[y * 8 + x] + 128f), 0, 255);
                }
            planes[ci] = plane;
            planeWidth[ci] = w;
        }

        if (components.Count == 4) throw new NotSupportedException("CMYK JPEGs can't be read; save the texture as PNG");
        var canvas = new PixelCanvas(width, height);
        bool ycc = components.Count == 3 && (!adobe || transform != 0);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float Sample(int ci)
                {
                    var c = components[ci];
                    int sx = x * c.H / maxH, sy = y * c.V / maxV;
                    return planes[ci][sy * planeWidth[ci] + sx];
                }
                if (components.Count == 1)
                {
                    int g = (int)Sample(0);
                    canvas.SetRaw(x, y, new Raylib_cs.Color(g, g, g, 255));
                    continue;
                }
                float a = Sample(0), b = Sample(1), d = Sample(2);
                int red, green, blue;
                if (ycc)
                {
                    red = (int)MathF.Round(a + 1.402f * (d - 128f));
                    green = (int)MathF.Round(a - 0.344136f * (b - 128f) - 0.714136f * (d - 128f));
                    blue = (int)MathF.Round(a + 1.772f * (b - 128f));
                }
                else
                {
                    red = (int)a;
                    green = (int)b;
                    blue = (int)d;
                }
                canvas.SetRaw(x, y, new Raylib_cs.Color(Math.Clamp(red, 0, 255), Math.Clamp(green, 0, 255), Math.Clamp(blue, 0, 255), 255));
            }
        return canvas;
    }

    private static readonly float[] Cos = BuildCos();

    private static float[] BuildCos()
    {
        var t = new float[64];
        for (int x = 0; x < 8; x++)
            for (int u = 0; u < 8; u++)
                t[x * 8 + u] = (u == 0 ? MathF.Sqrt(0.5f) : 1f) * MathF.Cos((2 * x + 1) * u * MathF.PI / 16f);
        return t;
    }

    /// <summary>The separable inverse DCT of an 8×8 block, in place.</summary>
    private static void InverseDct(Span<float> block, Span<float> tmp)
    {
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
            {
                float sum = 0f;
                for (int u = 0; u < 8; u++) sum += Cos[x * 8 + u] * block[y * 8 + u];
                tmp[y * 8 + x] = sum * 0.5f;
            }
        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
            {
                float sum = 0f;
                for (int v = 0; v < 8; v++) sum += Cos[y * 8 + v] * tmp[v * 8 + x];
                block[y * 8 + x] = sum * 0.5f;
            }
    }
}
