using System.Text.Json;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 17 · N6: the moves' Romanian names and descriptions, made up by us and written by hand in
/// <c>Data/text/ro/names.moves.json</c> (no official ones exist), read by <see cref="LocalNames"/> on top of the
/// importer's files as every language's hand-written <c>names.&lt;kind&gt;.json</c> is. Every move has both (a floor that
/// only rises), every letter is Romanian's own and one the typeface draws, no two moves share a name, a name fits the
/// battle's move card where it is drawn narrowest and every battle line that names a move, and a description fits the
/// battle's information panel. Changing the language is the whole process's, so the class runs alone.
/// </summary>
[Collection("Shared clocks and dice")]
public class RomanianMoveTests
{
    private const string Romanian = "ro";

    /// <summary>How many of the data's moves have a Romanian name and description: raised as they are written, never lowered.</summary>
    private const int NamedAtLeast = 934;

    private static IReadOnlyDictionary<string, string> Names => LocalNames.Section(Romanian, "moves");
    private static IReadOnlyDictionary<string, string> Descriptions => LocalNames.Section(Romanian, "descriptions.moves");

    // ------------------------------------------------------------------ the file and the loader

    [Fact]
    public void TheMovesFileIsAHandWrittenFileBesideTheNamesInTheShapeOfTheImportersOwn()
    {
        string path = Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Data", "text", Romanian, LocalNames.OwnFileName("moves"));
        Assert.True(File.Exists(path), path);
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var sections = document.RootElement.EnumerateObject().Select(p => p.Name).ToList();
        Assert.Equal(new[] { "moves", "descriptions" }, sections);
        Assert.Equal(new[] { "moves" }, document.RootElement.GetProperty("descriptions").EnumerateObject().Select(p => p.Name));

        // The importer writes names.json alone, which is no hand-written file's name
        Assert.Equal("names.json", LocalNames.FileName);
        Assert.DoesNotMatch(new Regex("^names\\..+\\.json$"), LocalNames.FileName);
        Assert.Contains(LocalNames.FilesOf(Romanian), f => Path.GetFileName(f) == "names.moves.json");
        Assert.True(LocalNames.Has(Romanian));
    }

    [Fact]
    public void EveryMoveOfTheDataHasARomanianNameAndDescription()
    {
        var moves = MoveDatabase.GetAll().Select(m => m.Name).ToHashSet();
        var unknown = Names.Keys.Concat(Descriptions.Keys).Where(k => !moves.Contains(k)).Distinct().ToList();
        Assert.True(unknown.Count == 0, "Not moves of the data: " + string.Join(", ", unknown));
        int both = moves.Count(m => Names.ContainsKey(m) && Descriptions.ContainsKey(m));
        Assert.True(both >= NamedAtLeast, $"{both} moves have a Romanian name and description, fewer than {NamedAtLeast}");
        var empty = Names.Concat(Descriptions).Where(e => string.IsNullOrWhiteSpace(e.Value) || e.Value != e.Value.Trim()).Select(e => e.Key).ToList();
        Assert.True(empty.Count == 0, "Empty or untrimmed: " + string.Join(", ", empty));
    }

    [Fact]
    public void EveryRomanianMoveNameAndDescriptionIsWrittenInRomaniansOwnLettersThatTheFaceDraws()
    {
        var wrong = new List<string>();
        foreach (var (move, text) in Names.Select(e => (e.Key, e.Value)).Concat(Descriptions.Select(e => (e.Key, e.Value))))
        {
            if (text.IndexOfAny(new[] { 'Ş', 'ş', 'Ţ', 'ţ' }) >= 0) wrong.Add($"{move}: a cedilla in \"{text}\"");
            if (text.Contains('ã') || text.Contains('Ã')) wrong.Add($"{move}: ã in \"{text}\"");
            if (text.Contains('{') || text.Contains('}')) wrong.Add($"{move}: a placeholder in \"{text}\"");
            foreach (var weight in new[] { UiWeight.Bold, UiWeight.ExtraBold, UiWeight.Black })
                if (TextMetrics.Undrawable(text, weight).Count > 0) wrong.Add($"{move}: \"{text}\" has a character the {weight} face lacks");
        }
        Assert.True(wrong.Count == 0, string.Join("\n", wrong.Distinct()));
    }

    [Fact]
    public void NoTwoMovesShareARomanianName()
    {
        var shared = Names.GroupBy(e => e.Value, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1)
            .Select(g => $"{g.Key}: {string.Join(", ", g.Select(e => e.Key))}").ToList();
        Assert.True(shared.Count == 0, string.Join("\n", shared));
    }

    [Fact]
    public void EveryDescriptionEndsAsASentenceDoes()
    {
        var wrong = Descriptions.Where(e => !".!".Contains(e.Value[^1])).Select(e => $"{e.Key}: {e.Value}").ToList();
        Assert.True(wrong.Count == 0, string.Join("\n", wrong));
    }

    // ------------------------------------------------------------------ what fits

    /// <summary>
    /// The room a move's name has on the battle's move card (<c>ModernUi.MoveMenu</c>), its narrowest use: from the name's
    /// left edge to the "PP" before the widest count a move can show, less a gap.
    /// </summary>
    private static float CardRoom()
    {
        const float card = 536, nameLeft = 156, gap = 12;
        float pp = TextMetrics.Measure("64/64", 26, UiWeight.Black);
        Text.Use(Romanian);
        try
        {
            float label = TextMetrics.Measure(Text.Of("summary.pp"), 18, UiWeight.ExtraBold);
            return card - 40 - pp - label - nameLeft - gap;
        }
        finally { Text.Use(Text.English); }
    }

    [Fact]
    public void EveryRomanianNameOfAMoveThatCanBeKnownFitsTheBattlesMoveCard()
    {
        // Z-Moves and Max Moves are never on a card (MoveKind): they are said in a line, measured below
        float room = CardRoom();
        var known = MoveDatabase.GetAll().Where(m => m.Kind == MoveKind.Standard).Select(m => m.Name).ToHashSet();
        var tooWide = Names.Where(e => known.Contains(e.Key))
            .Select(e => (e.Key, e.Value, Width: TextMetrics.Measure(e.Value, 32, UiWeight.Black)))
            .Where(e => e.Width > room).OrderByDescending(e => e.Width)
            .Select(e => $"{e.Key}: \"{e.Value}\" is {e.Width:0} wide, the card has {room:0}").ToList();
        Assert.True(tooWide.Count == 0, string.Join("\n", tooWide));
    }

    /// <summary>The battle's lines that name a move, by the placeholders they name it with.</summary>
    private static readonly string[] MovePlaceholders = { "move", "newMove", "bindingMove", "forgotten" };

    [Fact]
    public void EveryRomanianBattleLineFitsTheMessageBoxWithTheWidestRomanianMoveName()
    {
        string widest = Names.Values.OrderByDescending(n => TextMetrics.Measure(n, ModernUi.MessageSize, UiWeight.ExtraBold)).First();
        try
        {
            Text.Use(Romanian);
            var tooLong = new List<string>();
            foreach (var key in Text.EnglishKeys.Where(k => k.StartsWith("battle.", StringComparison.Ordinal)).OrderBy(k => k))
                foreach (long n in new long[] { 1, 20 })
                {
                    string line = Text.Of(key, ("n", n));
                    if (!MovePlaceholders.Any(p => line.Contains("{" + p + "}"))) continue;
                    foreach (Match hole in Regex.Matches(line, @"\{([A-Za-z0-9]+)\}"))
                        line = line.Replace(hole.Value, MovePlaceholders.Contains(hole.Groups[1].Value) ? widest : "Kangaskhan inamic");
                    var rows = TextMetrics.Wrap(line, ModernUi.MessageBoxWidth - ModernUi.MessageMargin, ModernUi.MessageSize, UiWeight.ExtraBold);
                    if (rows.Count > 2) tooLong.Add($"{key}: {line}");
                }
            Assert.True(tooLong.Count == 0, "Lines that run past the box's two lines:\n" + string.Join("\n", tooLong.Distinct()));
        }
        finally
        {
            Text.Use(Text.English);
        }
    }

    [Fact]
    public void EveryRomanianDescriptionFitsTheBattlesInformationPanelsTwoLines()
    {
        // ModernUi.MoveInfo: 700 wide less 44 each side, 26 in the Bold face, two rows of 36 under the panel's top line
        const float width = 700 - 88;
        var tooLong = Descriptions.Where(e => TextMetrics.Wrap(e.Value, width, 26, UiWeight.Bold).Count > 2)
            .Select(e => $"{e.Key}: {e.Value}").ToList();
        Assert.True(tooLong.Count == 0, "Descriptions past two lines:\n" + string.Join("\n", tooLong));
    }

    [Fact]
    public void AMoveIsShownByItsRomanianNameAndDescriptionInRomanianAndByItsEnglishOnesInEnglish()
    {
        var tackle = MoveDatabase.Get("Tackle")!;
        Assert.Equal(("Tackle", tackle.Description), (tackle.DisplayName, tackle.DisplayDescription));
        Text.Use(Romanian);
        try
        {
            Assert.Equal((Names["Tackle"], Descriptions["Tackle"]), (tackle.DisplayName, tackle.DisplayDescription));
            Assert.NotEqual("Tackle", tackle.DisplayName);
        }
        finally { Text.Use(Text.English); }
    }
}
