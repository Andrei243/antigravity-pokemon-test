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
/// The CJK half of plan 17 · N5: a third face (Noto Sans SC and KR Bold, cut to what the game's files use) for
/// Japanese, Korean and Chinese, read from the face file's own character map with no window. A CJK character is drawn
/// if and only if the face has it; a language's atlases are batches of at most six hundred codepoints gathered from its
/// own files; a line of CJK breaks between its characters but never before a closing mark; Latin is untouched.
/// </summary>
[Collection("Shared clocks and dice")]
public class CjkFontTests
{
    private static readonly string[] CjkLanguages = { "ja", "ko", "zh-Hans", "zh-Hant" };

    [Fact]
    public void TheFaceShipsWithItsLicenceAndIsSmall()
    {
        string folder = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts");
        var face = new FileInfo(Path.Combine(folder, CjkFace.FileName));
        Assert.True(face.Exists, $"{CjkFace.FileName} is not beside the executable");
        Assert.True(File.Exists(Path.Combine(folder, CjkFace.LicenceFile)), $"{CjkFace.LicenceFile} is not beside the executable");
        Assert.Contains("SIL Open Font License", File.ReadAllText(Path.Combine(folder, CjkFace.LicenceFile)));
        // Cut to what the files use: under two megabytes, where the two whole faces are sixteen
        Assert.InRange(face.Length, 100_000, 2_000_000);
        Assert.InRange(TextMetrics.CjkCount, 1000, 20000);
    }

    [Fact]
    public void ACjkCharacterIsDrawableIfAndOnlyIfTheFaceHasIt()
    {
        foreach (var weight in Enum.GetValues<UiWeight>())
        {
            Assert.Empty(TextMetrics.Undrawable("日本語 한국어 简体中文 繁體中文 ポケモン ひらがな 「」。、！？", weight));
            for (int c = 0x3000; c <= 0x9FFF; c += 7)
                Assert.Equal(TextMetrics.HasCjk(c), TextMetrics.CanDraw(new Rune(c), weight));
            for (int c = 0xAC00; c <= 0xD7A3; c += 11)
                Assert.Equal(TextMetrics.HasCjk(c), TextMetrics.CanDraw(new Rune(c), weight));
        }
        // An ideograph nobody's file uses is not in the cut face, so it is a question mark like any other
        Assert.False(TextMetrics.HasCjk(0x9F98));
        Assert.False(TextMetrics.CanDraw(new Rune(0x9F98)));
        // and nothing of Latin, the arrows or the gender marks became the CJK face's
        foreach (int c in new[] { 'A', 0xE9, 0x2192, 0x2640, 0x2642, 0x2026, 0x2014 })
            Assert.False(CjkFace.IsCjk(c));
    }

    [Fact]
    public void EveryCharacterOfACjkLanguagesFilesHasAGlyph()
    {
        foreach (var language in Language.All.Where(l => l.Script != "Latin"))
            Assert.Empty(TextMetrics.Undrawable(language.Name, UiWeight.Black));
        foreach (string tag in CjkLanguages)
        {
            string folder = GameDataFiles.PathOf(Path.Combine("text", tag));
            Assert.True(Directory.Exists(folder), $"{tag} has no folder");
            foreach (var file in Directory.EnumerateFiles(folder, "*.json", SearchOption.AllDirectories))
            {
                var bad = new SortedSet<string>();
                foreach (int c in CjkFace.CjkIn(File.ReadAllText(file)))
                    if (!TextMetrics.HasCjk(c)) bad.Add($"{char.ConvertFromUtf32(c)} U+{c:X4}");
                Assert.True(bad.Count == 0, $"{file} has CJK characters the face lacks (cut it again with tools/TextTool -- font): {string.Join(" ", bad.Take(30))}");
            }
            // and what the language gathers for its atlases is every one of them, in batches no bigger than six hundred
            var gathered = CjkFace.Gathered(tag);
            Assert.Subset(gathered.ToHashSet(), CjkFace.CharactersOf(tag).ToHashSet());
            Assert.Equal(gathered.OrderBy(c => c), gathered);
            var batches = CjkFace.Batches(gathered);
            Assert.All(batches, b => Assert.InRange(b.Length, 1, CjkFace.BatchSize));
            Assert.Equal(gathered, batches.SelectMany(b => b));
        }
    }

    [Fact]
    public void ACjkCharacterIsMeasuredAtItsOwnAtlasAndBesideLatinAsOneRun()
    {
        foreach (var weight in Enum.GetValues<UiWeight>())
        {
            float tracking = UiFonts.Tracking(30);
            float a = TextMetrics.Measure("a", 30, weight), wide = TextMetrics.Measure("語", 30, weight);
            // An ideograph is a little over two thirds of the size across (its em is 1000 of the face's 1448 units)
            Assert.InRange(wide / 30f, 0.6f, 0.75f);
            Assert.Equal(a + wide + tracking, TextMetrics.Measure("a語", 30, weight), 3);
            Assert.Equal(wide * 3 + 2 * tracking, TextMetrics.Measure("日本語", 30, weight), 3);
            // Its baseline is lowered or raised onto Nunito's by a few hundredths of the size, never more
            Assert.InRange(TextMetrics.CjkDrop(weight), -0.12f, 0.12f);
        }
    }

    [Fact]
    public void ATextIsCutIntoRunsOfLatinAndOfTheCjkBatches()
    {
        var runs = UiFonts.Runs("Lv.5 ピカチュウ is here", UiWeight.Bold);
        Assert.Equal(3, runs.Count);
        Assert.Equal("Lv.5 ピカチュウ is here", string.Concat(runs.Select(r => r.Text)));
        Assert.Equal(UiFonts.FirstFace, runs[0].Face);
        Assert.True(runs[1].Face >= UiFonts.CjkFaceOf(0));
        Assert.Equal(UiFonts.FirstFace, runs[2].Face);
        // English never leaves the first face
        Assert.Single(UiFonts.Runs("Hello there! Welcome to the world of Pokémon.", UiWeight.Bold));
    }

    [Fact]
    public void ALineOfJapaneseOrChineseBreaksBetweenItsCharactersButNotBeforeAClosingMark()
    {
        const float size = 30;
        string line = "ポケモンの世界へようこそ。わたしはナナカマド、ポケモン博士だ。";
        float width = TextMetrics.Measure("ポケモンの世界へよう", size);
        var rows = TextMetrics.Wrap(line, width, size);
        Assert.True(rows.Count >= 3);
        Assert.Equal(line, string.Concat(rows));
        foreach (var row in rows)
        {
            Assert.True(TextMetrics.Measure(row, size) <= width + 0.01f, $"'{row}' is wider than the line");
            Assert.False(CjkFace.IsCloser(row[0]), $"'{row}' begins with a closing mark");
            Assert.False(CjkFace.IsOpener(row[^1]), $"'{row}' ends with an opening mark");
        }
        // A closing mark stays with the character before it even when it would just fit alone on the next line
        var tight = TextMetrics.Wrap("あいう。", TextMetrics.Measure("あいう", size), size);
        Assert.Equal(new[] { "あい", "う。" }, tight);
        // An opening mark goes down with what it opens
        var opened = TextMetrics.Wrap("あい「う」", TextMetrics.Measure("あい「", size), size);
        Assert.Equal(new[] { "あい", "「う」" }, opened);
    }

    [Fact]
    public void KoreanAndLatinBreakAtTheirSpacesAsTheyAlwaysHave()
    {
        const float size = 30;
        var korean = TextMetrics.Wrap("포켓몬 세계에 온 것을 환영한다", TextMetrics.Measure("포켓몬 세계에 온", size), size);
        Assert.Equal(new[] { "포켓몬 세계에 온", "것을 환영한다" }, korean);
        Assert.Equal(new[] { "포켓몬" }, TextMetrics.Pieces("포켓몬"));
        var english = TextMetrics.Wrap("Welcome to the world of Pokémon", TextMetrics.Measure("Welcome to the", size), size);
        Assert.Equal("Welcome to the", english[0]);
        Assert.Equal(new[] { "Welcome" }, TextMetrics.Pieces("Welcome"));
        // A line of both: the Latin word is whole, the kana around it break
        var both = TextMetrics.Wrap("ピカチュウとPikachuはともだち", TextMetrics.Measure("ピカチュウとPikachu", size), size);
        Assert.Equal("ピカチュウとPikachu", both[0]);
        Assert.Equal(new[] { "ピ", "カ", "チュ", "ウ", "と", "Pikachu", "は" }, TextMetrics.Pieces("ピカチュウとPikachuは"));
    }

    [Fact]
    public void TheCjkScriptsAreDrawnSoACjkLanguageIsOfferedOnceItHasATable()
    {
        foreach (var script in new[] { "Japanese", "Korean", "Chinese" })
            Assert.Contains(script, Language.DrawnScripts);
        foreach (string tag in CjkLanguages)
        {
            var language = Language.Of(tag)!;
            // No translation ships yet: each is listed and drawn, and offered the day it has a ui.json of its own
            Assert.Equal(language.HasTable, Language.Offered.Contains(language));
        }
        Assert.DoesNotContain(Language.Offered, l => l.Pseudo);
    }

    [Fact]
    public void TheCjkPseudoLanguageMarksEveryStringWithFullwidthBracketsAndThreeScripts()
    {
        try
        {
            Text.Use("xx-cjk");
            string yes = Text.Of("shared.yes");
            Assert.StartsWith("［あ字한", yes);
            Assert.EndsWith("］", yes);
            Assert.Empty(TextMetrics.Undrawable(yes));
            Text.Use(Text.Pseudo);
            Assert.StartsWith("[", Text.Of("shared.yes"));
        }
        finally
        {
            Text.Use(Text.English);
        }
    }
}
