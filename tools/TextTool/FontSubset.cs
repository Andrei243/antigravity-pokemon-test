using System.Buffers.Binary;
using System.Text;

/// <summary>
/// Cuts TrueType faces (outlines in <c>glyf</c>) down to the glyphs of some codepoints (plan 17 · N5): the CJK face
/// the game ships is Noto Sans SC Bold, with Noto Sans KR Bold's Hangul (which SC lacks), cut to the characters the
/// languages' files use, so the build carries under a megabyte where the whole faces are sixteen. Each codepoint's glyph
/// is the first face's that has it; the glyphs kept are renumbered from 1 (.notdef stays 0), a composite's parts come
/// along and are renumbered in it, and the tables are rewritten to match: <c>cmap</c> (one format 12 subtable for
/// Windows' full Unicode), <c>loca</c> (long offsets), <c>hmtx</c>, <c>maxp</c>, <c>post</c> (format 3, no glyph names)
/// and <c>head</c>'s checksum. The layout tables (GSUB, GPOS, GDEF, the vertical metrics, BASE, STAT, the variations)
/// and the hinting programs are left out: raylib's loader reads none of them. <c>name</c>, <c>OS/2</c> and the rest are
/// the first face's. Nothing here draws; the font files are the input, the cut face the output.
/// </summary>
internal static class FontSubset
{
    private static readonly HashSet<string> Dropped = new(StringComparer.Ordinal)
    {
        "GSUB", "GPOS", "GDEF", "BASE", "JSTF", "MATH", "vhea", "vmtx", "VORG", "STAT", "DSIG", "LTSH", "VDMX", "hdmx", "kern",
        "fvar", "gvar", "avar", "cvar", "HVAR", "MVAR", "VVAR", "meta", "fpgm", "prep", "cvt ", "gasp"
    };

    /// <summary>The face's character map, codepoint to glyph (Windows' Unicode subtable, format 4 or 12).</summary>
    public static Dictionary<int, int> Cmap(byte[] font)
    {
        var tables = Tables(font);
        int cmap = tables["cmap"].Offset;
        int map = -1, best = -1;
        for (int i = 0, n = U16(font, cmap + 2); i < n; i++)
        {
            int record = cmap + 4 + i * 8;
            int platform = U16(font, record), encoding = U16(font, record + 2);
            int sub = cmap + (int)U32(font, record + 4);
            int format = U16(font, sub);
            int rank = platform == 3 && encoding == 10 && format == 12 ? 3 : platform == 0 && format == 12 ? 2 : platform == 3 && encoding == 1 && format == 4 ? 1 : -1;
            if (rank > best) { best = rank; map = sub; }
        }
        if (map < 0) throw new InvalidDataException("The face has no Unicode character map of format 4 or 12.");
        var found = new Dictionary<int, int>();
        if (U16(font, map) == 12)
        {
            for (uint g = 0, groups = U32(font, map + 12); g < groups; g++)
            {
                int group = map + 16 + (int)g * 12;
                uint first = U32(font, group), last = U32(font, group + 4), glyph = U32(font, group + 8);
                for (uint c = first; c <= last; c++) found[(int)c] = (int)(glyph + c - first);
            }
            return found;
        }
        int segments = U16(font, map + 6) / 2;
        int ends = map + 14, starts = ends + segments * 2 + 2, deltas = starts + segments * 2, offsets = deltas + segments * 2;
        for (int s = 0; s < segments; s++)
        {
            int end = U16(font, ends + s * 2), start = U16(font, starts + s * 2), delta = S16(font, deltas + s * 2), offset = U16(font, offsets + s * 2);
            for (int c = start; c <= end && c != 0xFFFF; c++)
            {
                int glyph = offset == 0 ? (c + delta) & 0xFFFF : U16(font, offsets + s * 2 + offset + (c - start) * 2);
                if (offset != 0 && glyph != 0) glyph = (glyph + delta) & 0xFFFF;
                if (glyph != 0) found[c] = glyph;
            }
        }
        return found;
    }

    /// <summary>
    /// The faces (of one design and one em) cut to the glyphs of <paramref name="codepoints"/> they have, as one face,
    /// and how many glyphs that is.
    /// </summary>
    public static (byte[] Font, int Glyphs) Subset(IReadOnlyList<byte[]> fonts, IEnumerable<int> codepoints)
    {
        var faces = fonts.Select(f => new Source(f)).ToList();
        foreach (var face in faces)
            if (face.UnitsPerEm != faces[0].UnitsPerEm) throw new InvalidDataException("The faces have different ems.");

        // Each codepoint's face, and the glyphs kept by face in the order of their old numbers, with every composite's parts
        var mapped = new List<(int Codepoint, int Face, int Glyph)>();
        foreach (int c in codepoints.Distinct().OrderBy(c => c))
            for (int f = 0; f < faces.Count; f++)
                if (faces[f].Cmap.TryGetValue(c, out int g)) { mapped.Add((c, f, g)); break; }
        var keep = new SortedSet<(int Face, int Glyph)> { (0, 0) };
        var pending = new Stack<(int Face, int Glyph)>(mapped.Select(m => (m.Face, m.Glyph)));
        while (pending.Count > 0)
        {
            var (f, g) = pending.Pop();
            if (!keep.Add((f, g))) continue;
            foreach (int part in Components(faces[f], g))
                if (!keep.Contains((f, part))) pending.Push((f, part));
        }
        var order = keep.ToList();
        var renumber = new Dictionary<(int, int), int>();
        for (int i = 0; i < order.Count; i++) renumber[order[i]] = i;

        // glyf and loca, a composite's parts renumbered in its copy
        var newGlyf = new MemoryStream();
        var offsetsOut = new List<int>();
        foreach (var (f, g) in order)
        {
            var face = faces[f];
            offsetsOut.Add((int)newGlyf.Length);
            int length = face.Start(g + 1) - face.Start(g);
            var bytes = face.Font.AsSpan(face.Glyf + face.Start(g), length).ToArray();
            if (length > 0 && S16(bytes, 0) < 0) RenumberComponents(bytes, part => renumber[(f, part)]);
            newGlyf.Write(bytes);
            while (newGlyf.Length % 4 != 0) newGlyf.WriteByte(0);
        }
        offsetsOut.Add((int)newGlyf.Length);
        var newLoca = new byte[offsetsOut.Count * 4];
        for (int i = 0; i < offsetsOut.Count; i++) W32(newLoca, i * 4, (uint)offsetsOut[i]);

        // hmtx: a full metric for every glyph kept
        var newHmtx = new byte[order.Count * 4];
        for (int i = 0; i < order.Count; i++)
        {
            var (f, g) = order[i];
            var (advance, lsb) = faces[f].Metric(g);
            W16(newHmtx, i * 4, (ushort)advance);
            BinaryPrimitives.WriteInt16BigEndian(newHmtx.AsSpan(i * 4 + 2), (short)lsb);
        }

        // cmap: one format 12 subtable, the codepoints in runs of consecutive glyphs
        var groups = new List<(int First, int Last, int Glyph)>();
        foreach (var (c, f, g) in mapped)
        {
            int glyph = renumber[(f, g)];
            if (groups.Count > 0 && groups[^1].Last == c - 1 && groups[^1].Glyph + (c - groups[^1].First) == glyph)
                groups[^1] = (groups[^1].First, c, groups[^1].Glyph);
            else groups.Add((c, c, glyph));
        }
        var newCmap = new byte[4 + 8 + 16 + groups.Count * 12];
        W16(newCmap, 0, 0); W16(newCmap, 2, 1);
        W16(newCmap, 4, 3); W16(newCmap, 6, 10); W32(newCmap, 8, 12);
        W16(newCmap, 12, 12); W16(newCmap, 14, 0); W32(newCmap, 16, (uint)(newCmap.Length - 12)); W32(newCmap, 20, 0); W32(newCmap, 24, (uint)groups.Count);
        for (int i = 0; i < groups.Count; i++)
        {
            W32(newCmap, 28 + i * 12, (uint)groups[i].First);
            W32(newCmap, 32 + i * 12, (uint)groups[i].Last);
            W32(newCmap, 36 + i * 12, (uint)groups[i].Glyph);
        }

        var output = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var (tag, (offset, length)) in faces[0].Tables)
        {
            if (Dropped.Contains(tag)) continue;
            output[tag] = faces[0].Font.AsSpan(offset, length).ToArray();
        }
        output["glyf"] = newGlyf.ToArray();
        output["loca"] = newLoca;
        output["hmtx"] = newHmtx;
        output["cmap"] = newCmap;
        W16(output["head"], 50, 1);              // long loca
        W32(output["head"], 8, 0);               // checkSumAdjustment, set below
        W16(output["maxp"], 4, (ushort)order.Count);
        W16(output["hhea"], 34, (ushort)order.Count);
        if (output.TryGetValue("post", out var post))
        {
            var header = post.AsSpan(0, 32).ToArray();
            W32(header, 0, 0x00030000);
            output["post"] = header;
        }
        return (Write(output), order.Count);
    }

    private static IEnumerable<int> Components(Source face, int g)
    {
        int at = face.Glyf + face.Start(g), length = face.Start(g + 1) - face.Start(g);
        var font = face.Font;
        if (length <= 0 || S16(font, at) >= 0) yield break;
        int p = at + 10;
        while (true)
        {
            int flags = U16(font, p);
            yield return U16(font, p + 2);
            p += 4 + ((flags & 1) != 0 ? 4 : 2) + ((flags & 8) != 0 ? 2 : (flags & 0x40) != 0 ? 4 : (flags & 0x80) != 0 ? 8 : 0);
            if ((flags & 0x20) == 0) yield break;
        }
    }

    private static void RenumberComponents(byte[] glyph, Func<int, int> renumber)
    {
        int p = 10;
        while (true)
        {
            int flags = U16(glyph, p);
            W16(glyph, p + 2, (ushort)renumber(U16(glyph, p + 2)));
            p += 4 + ((flags & 1) != 0 ? 4 : 2) + ((flags & 8) != 0 ? 2 : (flags & 0x40) != 0 ? 4 : (flags & 0x80) != 0 ? 8 : 0);
            if ((flags & 0x20) == 0) return;
        }
    }

    private static byte[] Write(SortedDictionary<string, byte[]> tables)
    {
        int count = tables.Count;
        int searchRange = 1, entrySelector = 0;
        while (searchRange * 2 <= count) { searchRange *= 2; entrySelector++; }
        var header = new byte[12 + 16 * count];
        W32(header, 0, 0x00010000);
        W16(header, 4, (ushort)count);
        W16(header, 6, (ushort)(searchRange * 16));
        W16(header, 8, (ushort)entrySelector);
        W16(header, 10, (ushort)(count * 16 - searchRange * 16));
        var body = new MemoryStream();
        int i = 0, headAt = 0;
        foreach (var (tag, data) in tables)
        {
            int record = 12 + 16 * i++;
            Encoding.ASCII.GetBytes(tag).CopyTo(header, record);
            W32(header, record + 4, Checksum(data));
            W32(header, record + 8, (uint)(header.Length + body.Length));
            W32(header, record + 12, (uint)data.Length);
            if (tag == "head") headAt = header.Length + (int)body.Length;
            body.Write(data);
            while (body.Length % 4 != 0) body.WriteByte(0);
        }
        var file = header.Concat(body.ToArray()).ToArray();
        // head's checkSumAdjustment: 0xB1B0AFBA less the sum of the whole file
        W32(file, headAt + 8, unchecked(0xB1B0AFBA - Checksum(file)));
        return file;
    }

    private static uint Checksum(byte[] data)
    {
        uint sum = 0;
        for (int i = 0; i < data.Length; i += 4)
        {
            uint word = 0;
            for (int k = 0; k < 4; k++) word = (word << 8) | (i + k < data.Length ? data[i + k] : 0u);
            sum = unchecked(sum + word);
        }
        return sum;
    }

    /// <summary>One face being cut: its tables, its character map and where each glyph is.</summary>
    private sealed class Source
    {
        public readonly byte[] Font;
        public readonly Dictionary<string, (int Offset, int Length)> Tables;
        public readonly Dictionary<int, int> Cmap;
        public readonly int Glyf, UnitsPerEm;
        private readonly int loca, hmtx, metrics;
        private readonly bool longLoca;

        public Source(byte[] font)
        {
            Font = font;
            Tables = FontSubset.Tables(font);
            if (!Tables.ContainsKey("glyf")) throw new InvalidDataException("The face has no glyf table: only TrueType outlines are cut.");
            Cmap = FontSubset.Cmap(font);
            int head = Tables["head"].Offset;
            UnitsPerEm = U16(font, head + 18);
            longLoca = S16(font, head + 50) != 0;
            loca = Tables["loca"].Offset;
            Glyf = Tables["glyf"].Offset;
            hmtx = Tables["hmtx"].Offset;
            metrics = U16(font, Tables["hhea"].Offset + 34);
        }

        public int Start(int g) => longLoca ? (int)U32(Font, loca + g * 4) : U16(Font, loca + g * 2) * 2;

        public (int Advance, int Lsb) Metric(int g) =>
            (U16(Font, hmtx + 4 * Math.Min(g, metrics - 1)),
             g < metrics ? S16(Font, hmtx + 4 * g + 2) : S16(Font, hmtx + 4 * metrics + 2 * (g - metrics)));
    }

    private static Dictionary<string, (int Offset, int Length)> Tables(byte[] font)
    {
        if (U32(font, 0) is not (0x00010000 or 0x74727565)) throw new InvalidDataException("Not a TrueType face (an OpenType collection or a CFF face is not cut).");
        var tables = new Dictionary<string, (int, int)>(StringComparer.Ordinal);
        for (int i = 0, n = U16(font, 4); i < n; i++)
        {
            int record = 12 + i * 16;
            tables[Encoding.ASCII.GetString(font, record, 4)] = ((int)U32(font, record + 8), (int)U32(font, record + 12));
        }
        return tables;
    }

    private static int U16(byte[] b, int at) => BinaryPrimitives.ReadUInt16BigEndian(b.AsSpan(at));
    private static int S16(byte[] b, int at) => BinaryPrimitives.ReadInt16BigEndian(b.AsSpan(at));
    private static uint U32(byte[] b, int at) => BinaryPrimitives.ReadUInt32BigEndian(b.AsSpan(at));
    private static void W16(byte[] b, int at, ushort v) => BinaryPrimitives.WriteUInt16BigEndian(b.AsSpan(at), v);
    private static void W32(byte[] b, int at, uint v) => BinaryPrimitives.WriteUInt32BigEndian(b.AsSpan(at), v);
}
