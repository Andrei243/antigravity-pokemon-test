using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PokemonPlatinumEngine.UI.Kit;

/// <summary>
/// The interface typeface measured without a window (plan 16 · T8): what <see cref="UiFonts.Measure"/> answers,
/// worked out from the font files' own tables, for whoever must know whether written text fits before anything is
/// drawn (<c>tools/ContentCheck</c>, the tests). No drawing, no raylib: it reads each weight's character map and
/// advance widths (the TrueType tables <c>cmap</c>, <c>hhea</c> and <c>hmtx</c>) once and reckons as raylib does when
/// it loads a font at <see cref="UiFonts.AtlasSize"/> and measures a string: each glyph's advance scaled to the atlas
/// and cut to a whole number, their sum scaled to the size asked for, and the tracking between the characters.
/// </summary>
public static class TextMetrics
{
    private static readonly Dictionary<UiWeight, Face> Faces = new();

    /// <summary>One weight's advances at the atlas's size, by codepoint, for the codepoints <see cref="UiFonts"/> loads and the file has.</summary>
    private sealed class Face
    {
        public readonly Dictionary<int, int> Advance = new();

        /// <summary>The codepoints the file has but draws nothing for and gives no width.</summary>
        public readonly HashSet<int> Blank = new();
        public int Fallback;
    }

    private static Face Get(UiWeight weight)
    {
        lock (Faces)
        {
            if (Faces.TryGetValue(weight, out var face)) return face;
            string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", UiFonts.FileFor(weight));
            if (!File.Exists(path)) throw new FileNotFoundException($"The interface's typeface is not beside the program: {path}", path);
            Faces[weight] = face = Read(File.ReadAllBytes(path));
            return face;
        }
    }

    /// <summary>
    /// The width of a text in layout units, as <see cref="UiFonts.Measure"/> gives it. A character the typeface can't
    /// draw (<see cref="CanDraw"/>) is as wide as the question mark drawn in its place.
    /// </summary>
    public static float Measure(string text, float size, UiWeight weight = UiWeight.Bold)
    {
        if (text.Length == 0) return 0f;
        var face = Get(weight);
        float width = 0f;
        int count = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            count++;
            width += face.Advance.TryGetValue(rune.Value, out int advance) ? advance : face.Fallback;
        }
        return width * (size / UiFonts.AtlasSize) + (count - 1) * UiFonts.Tracking(size);
    }

    /// <summary>
    /// Whether the interface draws a character as itself. Anything else comes out as a question mark (a character the
    /// typeface wasn't loaded with or has no glyph for), or as nothing at all (the non-breaking space).
    /// </summary>
    public static bool CanDraw(Rune character, UiWeight weight = UiWeight.Bold) =>
        Get(weight) is var face && face.Advance.ContainsKey(character.Value) && !face.Blank.Contains(character.Value);

    /// <summary>The characters of a text the interface can't draw, each once, in the order they are met.</summary>
    public static List<Rune> Undrawable(string text, UiWeight weight = UiWeight.Bold)
    {
        var found = new List<Rune>();
        foreach (var rune in text.EnumerateRunes())
            if (rune.Value != '\n' && !CanDraw(rune, weight) && !found.Contains(rune)) found.Add(rune);
        return found;
    }

    /// <summary>A text broken into lines no wider than <paramref name="maxWidth"/>, as the interface breaks it (<see cref="Wrap(string, float, Func{string, float})"/>).</summary>
    public static List<string> Wrap(string text, float maxWidth, float size, UiWeight weight = UiWeight.Bold) =>
        Wrap(text, maxWidth, line => Measure(line, size, weight));

    /// <summary>
    /// Breaks text into lines no wider than <paramref name="maxWidth"/> by whatever measures it; "\n" forces a break. A
    /// word wider than a line by itself keeps a line to itself. The interface's one way of wrapping: <c>ModernUi.Wrap</c>
    /// hands it the font's own measure, and a check with no window <see cref="Measure"/>.
    /// </summary>
    public static List<string> Wrap(string text, float maxWidth, Func<string, float> measure)
    {
        var lines = new List<string>();
        foreach (var paragraph in text.Split('\n'))
        {
            string line = "";
            foreach (var word in paragraph.Split(' '))
            {
                string test = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && measure(test) > maxWidth)
                {
                    lines.Add(line);
                    line = word;
                }
                else line = test;
            }
            lines.Add(line);
        }
        return lines;
    }

    // ------------------------------------------------------------------ the font file

    /// <summary>
    /// Reads a TrueType file's advances as raylib's loader does (stb_truetype): the scale is the atlas's size over the
    /// face's ascent less its descent, and an advance is the glyph's width in the font's units times that, cut to a
    /// whole number.
    /// </summary>
    private static Face Read(byte[] file)
    {
        var tables = new Dictionary<string, int>();
        int count = U16(file, 4);
        for (int i = 0; i < count; i++)
        {
            int record = 12 + i * 16;
            tables[Encoding.ASCII.GetString(file, record, 4)] = (int)U32(file, record + 8);
        }
        int hhea = Table("hhea"), hmtx = Table("hmtx"), cmap = Table("cmap");
        int Table(string tag) => tables.TryGetValue(tag, out int at) ? at : throw new InvalidDataException($"The typeface has no '{tag}' table.");

        float scale = (float)UiFonts.AtlasSize / (S16(file, hhea + 4) - S16(file, hhea + 6));
        int metrics = U16(file, hhea + 34);
        int AdvanceOf(int glyph) => U16(file, hmtx + 4 * Math.Min(glyph, metrics - 1));

        // The character map for Unicode: Windows' (platform 3, encoding 1 or 10) or Unicode's own (platform 0)
        int map = -1;
        for (int i = 0, n = U16(file, cmap + 2); i < n; i++)
        {
            int record = cmap + 4 + i * 8;
            int platform = U16(file, record), encoding = U16(file, record + 2);
            if (platform == 0 || platform == 3 && encoding is 1 or 10) map = cmap + (int)U32(file, record + 4);
        }
        if (map < 0) throw new InvalidDataException("The typeface has no Unicode character map.");
        int format = U16(file, map);
        if (format != 4 && format != 12) throw new InvalidDataException($"The typeface's character map is of format {format}; formats 4 and 12 are read.");

        int GlyphOf(int codepoint)
        {
            if (format == 12)
            {
                for (uint g = 0, groups = U32(file, map + 12); g < groups; g++)
                {
                    int group = map + 16 + (int)g * 12;
                    uint first = U32(file, group), last = U32(file, group + 4);
                    if (codepoint >= first && codepoint <= last) return (int)(U32(file, group + 8) + (uint)codepoint - first);
                }
                return 0;
            }
            if (codepoint > 0xFFFF) return 0;
            int segments = U16(file, map + 6) / 2;
            int ends = map + 14, starts = ends + segments * 2 + 2, deltas = starts + segments * 2, offsets = deltas + segments * 2;
            for (int s = 0; s < segments; s++)
            {
                if (codepoint > U16(file, ends + s * 2)) continue;
                int start = U16(file, starts + s * 2);
                if (codepoint < start) return 0;
                int offset = U16(file, offsets + s * 2);
                if (offset == 0) return (codepoint + S16(file, deltas + s * 2)) & 0xFFFF;
                int glyph = U16(file, offsets + s * 2 + offset + (codepoint - start) * 2);
                return glyph == 0 ? 0 : (glyph + S16(file, deltas + s * 2)) & 0xFFFF;
            }
            return 0;
        }

        // A glyph with no outline (the non-breaking space) is loaded with no picture and no advance: only the space
        // itself keeps its width. It is drawn as nothing, where a reader would look for a gap
        int head = Table("head"), loca = Table("loca");
        bool longOffsets = S16(file, head + 50) != 0;
        uint OutlineAt(int glyph) => longOffsets ? U32(file, loca + glyph * 4) : (uint)U16(file, loca + glyph * 2) * 2;
        bool Blank(int glyph) => OutlineAt(glyph + 1) == OutlineAt(glyph);

        var face = new Face();
        foreach (int codepoint in UiFonts.Codepoints)
        {
            if (GlyphOf(codepoint) is not (> 0 and var glyph)) continue;
            bool blank = codepoint != ' ' && Blank(glyph);
            face.Advance[codepoint] = blank ? 0 : (int)(AdvanceOf(glyph) * scale);
            if (blank) face.Blank.Add(codepoint);
        }
        face.Fallback = face.Advance.GetValueOrDefault('?');
        return face;
    }

    private static int U16(byte[] b, int at) => BinaryPrimitives.ReadUInt16BigEndian(b.AsSpan(at));
    private static int S16(byte[] b, int at) => BinaryPrimitives.ReadInt16BigEndian(b.AsSpan(at));
    private static uint U32(byte[] b, int at) => BinaryPrimitives.ReadUInt32BigEndian(b.AsSpan(at));
}
