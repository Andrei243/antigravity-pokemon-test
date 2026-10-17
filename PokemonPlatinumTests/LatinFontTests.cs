using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.UI.Kit;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The interface typeface beyond Latin-1 (plan 17 · N5, the Latin half): a character is drawn if the typeface has a glyph
/// for it, read from the font file's own character map with no window. English pays nothing: its text never leaves the
/// first face, and the second face (Latin Extended-A and the marks) is made only when a text needs it.
/// </summary>
public class LatinFontTests
{
    private const string Romanian = "Țara și ăștia în mâna întâi: ŞţȘșȚț, Ă ă Â â Î î. „Bună ziua”, ‘x’ it’s €5 ™";
    private const string Others = "Łódź żółć Čeština ďňřšťž Magyar őű Ğ ı İ ş Ünal Ķīļā Ēģīptē Ångström Ņ ŉ";

    [Fact]
    public void EveryRomanianLetterIsDrawnInEveryWeight()
    {
        foreach (var weight in Enum.GetValues<UiWeight>())
            Assert.Empty(TextMetrics.Undrawable(Romanian, weight));
    }

    [Fact]
    public void LatinExtendedAIsDrawnInEveryWeightBarWhatTheTypefaceLacks()
    {
        foreach (var weight in Enum.GetValues<UiWeight>())
        {
            // Nunito has no ĳ (Dutch's ligature, written ij all the same) and no long s ſ: they stay question marks
            var lacking = Enumerable.Range(0x100, 0x80).Where(c => !TextMetrics.CanDraw(new Rune(c), weight)).ToList();
            Assert.True(lacking.SequenceEqual(new[] { 0x133, 0x17F }), $"{weight} has no glyph for {string.Join(" ", lacking.Select(c => $"U+{c:X4}"))}");
        }
        Assert.Empty(TextMetrics.Undrawable(Others, UiWeight.ExtraBold));
    }

    [Fact]
    public void TheFirstFaceIsWhatItWasAndEnglishNeverNeedsTheSecond()
    {
        // 231 codepoints asked of the first atlas as before (Latin-1, the dashes, the ellipsis, the arrows)
        Assert.Equal(231, UiFonts.Codepoints.Length);
        Assert.Empty(UiFonts.Codepoints.Intersect(UiFonts.ExtraCodepoints));
        foreach (var weight in Enum.GetValues<UiWeight>())
        {
            var runs = UiFonts.Runs("Hello there! Welcome to the world of Pokémon – and « ¿ñ? » … ok", weight);
            Assert.Single(runs);
            Assert.False(runs[0].Extra);
            // Not a Latin-1 character at all: the first face's question mark, as before
            Assert.Single(UiFonts.Runs("→ ♂", weight));
        }
    }

    [Fact]
    public void ATextIsCutIntoRunsByTheFaceThatHasEachGlyph()
    {
        var runs = UiFonts.Runs("Țara și ăștia", UiWeight.Bold);
        Assert.Equal(new[] { "Ț", "ara ", "ș", "i ", "ăș", "tia" }.Length, runs.Count);
        Assert.Equal("Țara și ăștia", string.Concat(runs.Select(r => r.Text)));
        Assert.True(runs[0].Extra);
        Assert.False(runs[1].Extra);
        for (int i = 1; i < runs.Count; i++) Assert.NotEqual(runs[i - 1].Extra, runs[i].Extra);
        // The runs are all text with a glyph in their face; a character neither face has stays with the first
        Assert.Equal(new[] { false }, UiFonts.Runs("a→b", UiWeight.Bold).Select(r => r.Extra));
    }

    [Fact]
    public void ARomanianTextIsMeasuredGlyphByGlyphAndNeverAsQuestionMarks()
    {
        foreach (var weight in Enum.GetValues<UiWeight>())
        {
            float tracking = UiFonts.Tracking(30);
            float a = TextMetrics.Measure("a", 30, weight), s = TextMetrics.Measure("ș", 30, weight);
            Assert.Equal(a + s + tracking, TextMetrics.Measure("aș", 30, weight), 3);
            // The glyphs are their own: a comma-below ș is not as wide as the question mark that would stand for it
            Assert.NotEqual(TextMetrics.Measure("?", 30, weight), s);
            // and the capital T with a comma below is as wide as the T it is made from, give or take the comma
            Assert.InRange(TextMetrics.Measure("Ț", 100, weight) / TextMetrics.Measure("T", 100, weight), 0.95f, 1.05f);
        }
    }

    [Fact]
    public void EveryCharacterOfALatinLanguagesFilesHasAGlyph()
    {
        // The name of each Latin language as its own pill draws it, and every text its shipped files hold
        var latin = Language.All.Where(l => l.Script == "Latin").ToList();
        Assert.Contains(latin, l => l.Tag == "ro");
        foreach (var language in latin)
            Assert.Empty(TextMetrics.Undrawable(language.Name, UiWeight.Black));
        foreach (var language in latin.Where(l => !l.Pseudo))
        {
            string folder = GameDataFiles.PathOf(Path.Combine("text", language.Tag));
            if (!Directory.Exists(folder)) continue;
            foreach (var file in Directory.EnumerateFiles(folder, "*.json", SearchOption.AllDirectories))
            {
                using var json = JsonDocument.Parse(File.ReadAllText(file));
                var bad = new List<string>();
                foreach (var text in Strings(json.RootElement))
                    foreach (var rune in TextMetrics.Undrawable(text, UiWeight.ExtraBold))
                        if (!KnownGaps.Contains(rune.Value) && !bad.Contains($"U+{rune.Value:X4}")) bad.Add($"U+{rune.Value:X4}");
                Assert.True(bad.Count == 0, $"{file} has characters the typeface can't draw: {string.Join(" ", bad)}");
            }
        }
    }

    /// <summary>
    /// What the files hold that no face draws as itself: the gender marks of Nidoran♀ and Nidoran♂ (an identifier the
    /// game draws as text in a few places and as shapes in others, which is not N5's) and PokeAPI's ⅔ in one French
    /// description. Nothing else may be added without a glyph for it.
    /// </summary>
    private static readonly HashSet<int> KnownGaps = new() { 0x2640, 0x2642, 0x2154 };

    private static IEnumerable<string> Strings(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                yield return element.GetString()!;
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    foreach (var text in Strings(item)) yield return text;
                break;
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                    foreach (var text in Strings(property.Value)) yield return text;
                break;
        }
    }

    [Fact]
    public void ANoBreakSpaceIsAGapThatDoesNotBreak()
    {
        foreach (var weight in Enum.GetValues<UiWeight>())
        {
            Assert.Empty(TextMetrics.Undrawable("Quoi ? Oui ! « Bonjour »", weight));
            Assert.Equal(TextMetrics.Measure("Quoi ?", 30, weight), TextMetrics.Measure("Quoi ?", 30, weight));
        }
        // French sets one before a question mark; wrapped, it stays with the word before it
        var rows = TextMetrics.Wrap("aaa bbb ?", TextMetrics.Measure("aaa bbb", 30), 30);
        Assert.Equal(2, rows.Count);
        Assert.Equal("bbb ?", rows[1]);
    }

    [Fact]
    public void WrappingTreatsRomanianAsAnyWordsAndMeasuresItsWidth()
    {
        var rows = TextMetrics.Wrap("Țara noastră înseamnă pământul și ceea ce ne învață părinții", 300, 30, UiWeight.ExtraBold);
        Assert.True(rows.Count > 1);
        foreach (var row in rows) Assert.True(TextMetrics.Measure(row, 30, UiWeight.ExtraBold) <= 300 || !row.Contains(' '));
    }
}
