using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;
using Raylib_cs;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 12 · Q8, seeing: the options' TEXT SIZE, COLOUR VISION and HIGH CONTRAST, which the whole process holds
/// (<see cref="Seeing"/>), so the class runs with the other shared state and puts all three back. The contrast of the
/// tokens is <c>StyleGuideTests</c>'s and the signs' squares <c>UiKitTests</c>'.
/// </summary>
[Collection("Shared clocks and dice")]
public class SeeingTests : IDisposable
{
    public void Dispose() => Seeing.Reset();

    [Fact]
    public void TheThreeRowsAreOffUnlessChosenAndStandBeforeControls()
    {
        var settings = new GameSettings();
        Assert.Equal((ColourVision.Normal, TextSize.Normal, false), (settings.ColourVision, settings.TextSize, settings.HighContrast));
        var json = new JsonSerializerOptions { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
        var old = JsonSerializer.Deserialize<GameSettings>("{ \"Quality\": \"Medium\" }", json)!;
        Assert.Equal((ColourVision.Normal, TextSize.Normal, false), (old.ColourVision, old.TextSize, old.HighContrast));

        var rows = Enum.GetValues<OptionRow>();
        int size = Array.IndexOf(rows, OptionRow.TextSize);
        Assert.Equal(Array.IndexOf(rows, OptionRow.Requests) + 1, size);
        Assert.Equal(size + 1, Array.IndexOf(rows, OptionRow.ColourVision));
        Assert.Equal(size + 2, Array.IndexOf(rows, OptionRow.HighContrast));
        Assert.Equal(size + 3, Array.IndexOf(rows, OptionRow.Tips));
        Assert.Equal(size + 5, Array.IndexOf(rows, OptionRow.Controls));

        // The first start's page takes them by their names, after the language (plan 16 · T24)
        var page = FirstStartScreen.RowsThatExist(FirstStartScreen.Wanted);
        Assert.Equal(new[] { OptionRow.Language, OptionRow.TextSize, OptionRow.ColourVision, OptionRow.HighContrast }, page.Take(4));
    }

    [Fact]
    public void EachRowChangesAndSaysWhatItDoes()
    {
        var s = new GameSettings();
        OptionsScreen.Change(s, OptionRow.ColourVision, 1);
        Assert.Equal(ColourVision.Protan, s.ColourVision);
        OptionsScreen.Change(s, OptionRow.ColourVision, -2);
        Assert.Equal(ColourVision.Tritan, s.ColourVision);
        OptionsScreen.Change(s, OptionRow.TextSize, 1);
        Assert.Equal(TextSize.Large, s.TextSize);
        OptionsScreen.Change(s, OptionRow.HighContrast, 1);
        Assert.True(s.HighContrast);

        foreach (var row in new[] { OptionRow.TextSize, OptionRow.ColourVision, OptionRow.HighContrast })
        {
            var (label, value, help) = OptionsScreen.Describe(s, row);
            Assert.DoesNotContain("options.", label + value + help);
            var (_, offValue, offHelp) = OptionsScreen.Describe(new GameSettings(), row);
            Assert.NotEqual(value, offValue);
            Assert.NotEqual(help, offHelp);
        }
        foreach (var vision in Enum.GetValues<ColourVision>())
        {
            s.ColourVision = vision;
            Assert.DoesNotContain("options.", string.Concat(OptionsScreen.Describe(s, OptionRow.ColourVision)));
        }

        s.ResetToDefaults();
        Assert.Equal((ColourVision.Normal, TextSize.Normal, false), (s.ColourVision, s.TextSize, s.HighContrast));
    }

    [Fact]
    public void UnderNormalEveryColourIsItselfAndAGreyStaysAGreyUnderEveryRemap()
    {
        Seeing.Reset();
        var random = new Random(8);
        for (int i = 0; i < 500; i++)
        {
            var c = new Color(random.Next(256), random.Next(256), random.Next(256), random.Next(256));
            Assert.Equal(c, Seeing.Seen(c));
            Assert.Equal(c, ModernUi.Seen(c));
        }
        Assert.False(Seeing.Signs);

        foreach (var vision in new[] { ColourVision.Protan, ColourVision.Deutan, ColourVision.Tritan })
        {
            Seeing.Use(vision, TextSize.Normal, false);
            Assert.True(Seeing.Signs);
            for (int v = 0; v < 256; v += 15)
            {
                var shown = Seeing.Seen(new Color(v, v, v, 200));
                Assert.True(Math.Abs(shown.R - v) <= 2 && Math.Abs(shown.G - v) <= 2 && Math.Abs(shown.B - v) <= 2, $"{vision}: grey {v} became {shown}");
                Assert.Equal(200, shown.A);
            }
        }
        Seeing.Use(ColourVision.Normal, TextSize.Normal, true);
        Assert.True(Seeing.Signs);
    }

    /// <summary>The plan's own measure: a protan player tells a Grass move from a Fire one.</summary>
    [Theory]
    [InlineData(ColourVision.Protan)]
    [InlineData(ColourVision.Deutan)]
    public void TheRemapPullsGrassAndFireApartForAnEyeThatMixesThem(ColourVision vision)
    {
        var grass = Palette.GetTypeColor("Grass");
        var fire = Palette.GetTypeColor("Fire");
        float Apart(Color a, Color b) => MathF.Sqrt((a.R - b.R) * (a.R - b.R) + (a.G - b.G) * (a.G - b.G) + (a.B - b.B) * (a.B - b.B));
        float before = Apart(Seeing.Simulate(grass, vision), Seeing.Simulate(fire, vision));
        float after = Apart(Seeing.Simulate(Seeing.Daltonise(grass, vision), vision), Seeing.Simulate(Seeing.Daltonise(fire, vision), vision));
        Assert.True(after > before * 1.2f, $"{vision}: Grass and Fire {before:0} apart as seen, {after:0} once remapped");
        // And each carries a sign of its own
        Assert.NotEqual(string.Join(";", TypeSigns.Parts(PokemonType.Grass)), string.Join(";", TypeSigns.Parts(PokemonType.Fire)));
    }

    [Fact]
    public void ALargeBattleLineWrapsToThreeAndEveryLineThatFitsTwoFitsThree()
    {
        // A line that just fills Normal's two rows takes three under large text
        var words = "The opposing Bronzong's Heavy Metal made it heavier than the Steelix of the Iron Island mine".Split(' ');
        string line = "";
        foreach (var word in words.Concat(words).Concat(words))
        {
            string longer = line.Length == 0 ? word : line + " " + word;
            Seeing.Reset();
            if (ModernUi.MessageRows(longer, ModernUi.MessageBoxWidth).Count > 2) break;
            line = longer;
        }
        Seeing.Reset();
        Assert.Equal(2, ModernUi.MessageRows(line, ModernUi.MessageBoxWidth).Count);
        Seeing.Use(ColourVision.Normal, TextSize.Large, false);
        Assert.Equal(ModernUi.LargeMessageSize, ModernUi.MessageSizeNow);
        var largeRows = ModernUi.MessageRows(line, ModernUi.MessageBoxWidth);
        Assert.True(largeRows.Count == 3, $"{largeRows.Count} rows at {ModernUi.MessageSizeNow}: {string.Join(" | ", largeRows)} ({TextMetrics.Measure(line, 40, UiWeight.ExtraBold):0} wide at 40)");

        // Every battle line of the table that fits two rows at Normal fits the box's three at Large
        var tooLong = new List<string>();
        foreach (var key in Text.EnglishKeys.Where(k => k.StartsWith("battle.", StringComparison.Ordinal)))
        {
            string said = Regex.Replace(Text.Of(key, ("n", 10)), @"\{[A-Za-z0-9]+\}", "Fletchinder");
            Seeing.Reset();
            if (ModernUi.MessageRows(said, ModernUi.MessageBoxWidth).Count > 2) continue;
            Seeing.Use(ColourVision.Normal, TextSize.Large, false);
            if (ModernUi.MessageRows(said, ModernUi.MessageBoxWidth).Count > 3) tooLong.Add($"{key}: {said}");
        }
        Assert.True(tooLong.Count == 0, "Lines past three rows under large text:\n" + string.Join("\n", tooLong));
    }

    [Fact]
    public void TheFieldsBoxHoldsFourLargeRowsSoALineOfThreeNormalRowsStillEndsInIt()
    {
        Assert.Equal(3, DialogueBox.Rows);
        Assert.Equal(4, DialogueBox.LargeRows);
        // The fourth row ends inside the taller panel
        Assert.True(DialogueBox.TextTop + (DialogueBox.LargeRows - 1) * DialogueBox.LargeRowHeight + DialogueBox.LargeTextSize <= DialogueBox.LargeHeight);
        Seeing.Use(ColourVision.Normal, TextSize.Large, false);
        Assert.Equal((DialogueBox.LargeTextSize, DialogueBox.LargeHeight, 4), (DialogueBox.SizeNow, DialogueBox.HeightNow, DialogueBox.RowsNow));
        Seeing.Reset();
        Assert.Equal((DialogueBox.TextSize, DialogueBox.Height, 3), (DialogueBox.SizeNow, DialogueBox.HeightNow, DialogueBox.RowsNow));

        // Lines of three rows at Normal, made of words of every width, never run past four at Large
        var words = "a I Pokémon the Bidoof Professor Rowan Twinleaf Town Wwwww mm illi Sinnoh Galactic. You're {player}'s rival, Barry! Hey!".Split(' ');
        var random = new Random(12);
        for (int trial = 0; trial < 400; trial++)
        {
            string line = "";
            while (true)
            {
                string word = words[random.Next(words.Length)];
                string longer = line.Length == 0 ? word : line + " " + word;
                if (DialogueBox.RowsOf(longer, 1920, large: false).Count > DialogueBox.Rows) break;
                line = longer;
            }
            int large = DialogueBox.RowsOf(line, 1920, large: true).Count;
            Assert.True(large <= DialogueBox.LargeRows, $"{large} rows at Large: {line}");
        }
    }

    [Fact]
    public void HighContrastLightensTheRunsToReadOnBlack()
    {
        var t = ModernUi.TextTokens;
        Seeing.Reset();
        Assert.Equal(t.RunRed, ModernUi.RunInk(RunColour.Red, ModernUi.Ink));
        Seeing.Use(ColourVision.Normal, TextSize.Normal, true);
        Assert.Equal(t.ContrastRed, ModernUi.RunInk(RunColour.Red, Color.White));
        Assert.Equal(t.ContrastBlue, ModernUi.RunInk(RunColour.Blue, Color.White));
        Assert.Equal(Color.White, ModernUi.RunInk(RunColour.Plain, Color.White));
    }
}
