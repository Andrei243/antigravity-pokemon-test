using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.UI.Kit;

/// <summary>
/// The CJK half of plan 17 · N5 (style guide, "Typography", "Faces beyond Latin"): the third face, for Japanese,
/// Korean and Chinese, which Nunito has nothing of. It is Noto Sans SC Bold (SIL OFL; docs/art/CREDITS.md) cut down by
/// <c>tools/TextTool -- font</c> to the characters the game's files use (<see cref="FileName"/>), one weight for all
/// three of the interface's. What a language draws from it is loaded in batches of at most <see cref="BatchSize"/>
/// codepoints, one atlas each at <see cref="AtlasSize"/>, gathered from the language's own files (<see cref="Gathered"/>)
/// so the atlases' contents depend on the list alone. This class is the rules: which characters are the face's, where a
/// line of them may break, what a language gathers. GPU-free; <see cref="UiFonts"/> loads the atlases.
/// </summary>
public static class CjkFace
{
    /// <summary>The face's file in Assets/Fonts, beside Nunito's.</summary>
    public const string FileName = "NotoSansCJK-Bold-Subset.ttf";

    /// <summary>Its licence, shipped beside it.</summary>
    public const string LicenceFile = "OFL-NotoSansCJK.txt";

    /// <summary>The pixel size each batch's atlas is rasterised at (raylib's font size: the face's ascent to descent).</summary>
    public const int AtlasSize = 72;

    /// <summary>The most codepoints one atlas holds: six hundred glyphs at 72 px fill 2048 by 2048.</summary>
    public const int BatchSize = 600;

    /// <summary>
    /// Whether a codepoint is drawn from the CJK face (when the face has it): Hangul's letters and syllables, the CJK
    /// radicals, symbols and punctuation, the kana, the ideographs of every block, the compatibility forms, and the
    /// fullwidth and halfwidth forms. Nothing of Latin, the arrows or the gender marks is the face's, so a Latin text
    /// draws exactly as it did before the face came.
    /// </summary>
    public static bool IsCjk(int c) =>
        c is >= 0x1100 and <= 0x11FF          // Hangul Jamo
        or >= 0x2E80 and <= 0x9FFF            // radicals, CJK symbols and punctuation, kana, compatibility Jamo, enclosed, extension A, the unified ideographs
        or >= 0xA960 and <= 0xA97F            // Hangul Jamo Extended-A
        or >= 0xAC00 and <= 0xD7FF            // Hangul syllables, Jamo Extended-B
        or >= 0xF900 and <= 0xFAFF            // compatibility ideographs
        or >= 0xFE30 and <= 0xFE4F            // compatibility forms
        or >= 0xFF00 and <= 0xFFEF            // halfwidth and fullwidth forms
        or >= 0x20000 and <= 0x3FFFF;         // the ideographs past the BMP

    /// <summary>
    /// Whether a line may break on either side of this character (plan 17 · N5's break rule): the ideographs, the kana
    /// and the CJK and fullwidth marks, which Japanese and Chinese write without spaces. Hangul is not: Korean puts
    /// spaces between its words and breaks there, as <c>languages.json</c>'s <c>breaks</c> says.
    /// </summary>
    public static bool BreaksAnywhere(int c) =>
        IsCjk(c) && !(c is >= 0x1100 and <= 0x11FF or >= 0x3130 and <= 0x318F or >= 0xA960 and <= 0xA97F or >= 0xAC00 and <= 0xD7FF);

    /// <summary>
    /// The marks no line may begin with (kinsoku): closing brackets and quotes, the stops and commas, the long vowel
    /// mark, the small kana, the iteration marks, and the Latin closers when they follow a CJK character.
    /// </summary>
    public static bool IsCloser(int c) => c <= 0xFFFF && (
        "、。，．・：；？！ー～ゝゞヽヾ々〻ぁぃぅぇぉっゃゅょゎゕゖァィゥェォッャュョヮヵヶ」』）］｝〕〉》】〙〗〟’”｠｝…‥!?),.:;]}%".Contains((char)c)
        || c is 0xFF01 or 0xFF09 or 0xFF0C or 0xFF0E or 0xFF1A or 0xFF1B or 0xFF1F or 0xFF3D or 0xFF5D);

    /// <summary>The marks no line may end with: opening brackets and quotes.</summary>
    public static bool IsOpener(int c) => c <= 0xFFFF && "「『（［｛〔〈《【〘〖〝‘“｟([{".Contains((char)c);

    /// <summary>
    /// The marks every CJK language draws, gathered whatever its files hold: CJK symbols and punctuation, the kana, the
    /// compatibility Jamo (the Pokédex search's Korean groups) and the fullwidth ASCII.
    /// </summary>
    public static IEnumerable<int> Marks =>
        Enumerable.Range(0x3000, 0x40).Concat(Enumerable.Range(0x3041, 0x56)).Concat(Enumerable.Range(0x309B, 0x65))
            .Concat(Enumerable.Range(0x3131, 0x5E)).Concat(Enumerable.Range(0xFF01, 0x5E));

    private static readonly Dictionary<string, IReadOnlyList<int>> gathered = new();

    /// <summary>
    /// The codepoints a language's batches hold, in order: every CJK character of its own files
    /// (<c>Data/text/&lt;tag&gt;/</c>, every file), of <c>languages.json</c> (each language's own name, the CJK
    /// pseudo-language's brackets) and <see cref="Marks"/>, each one the face has. The same list every time, so the
    /// atlases are the same every run.
    /// </summary>
    public static IReadOnlyList<int> Gathered(string tag)
    {
        lock (gathered)
        {
            if (gathered.TryGetValue(tag, out var list)) return list;
            var set = new SortedSet<int>(Marks);
            foreach (int c in CharactersOf(tag)) set.Add(c);
            foreach (int c in CjkIn(File.ReadAllText(GameDataFiles.PathOf(Path.Combine("text", "languages.json"))))) set.Add(c);
            list = set.Where(TextMetrics.HasCjk).ToList();
            gathered[tag] = list;
            return list;
        }
    }

    /// <summary>Every CJK character of a language's own files, each once, in order.</summary>
    public static SortedSet<int> CharactersOf(string tag)
    {
        var set = new SortedSet<int>();
        foreach (var (_, path) in DataLayers.FilesIn(Path.Combine("text", tag), "*.json", recursive: true))
            foreach (int c in CjkIn(File.ReadAllText(path))) set.Add(c);
        return set;
    }

    /// <summary>The CJK characters of a text.</summary>
    public static IEnumerable<int> CjkIn(string text)
    {
        foreach (var rune in text.EnumerateRunes())
            if (IsCjk(rune.Value)) yield return rune.Value;
    }

    /// <summary>The gathered codepoints cut into the batches the atlases are made of, in order.</summary>
    public static List<int[]> Batches(IReadOnlyList<int> codepoints)
    {
        var batches = new List<int[]>();
        for (int i = 0; i < codepoints.Count; i += BatchSize)
            batches.Add(codepoints.Skip(i).Take(BatchSize).ToArray());
        return batches;
    }
}
