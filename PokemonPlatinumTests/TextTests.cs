using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.PoketchApps;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 17 · N1: the string table. Every key the engine asks for is in English's table and every entry of it is
/// asked for somewhere; the screens moved onto the table say nothing in quotes of their own; forms follow the
/// plural rule and the player's look; and the pseudo-language brackets every string, which is what shows a string
/// that slipped past. Changing the language is the whole process's, so the class runs alone.
/// </summary>
[Collection("Shared clocks and dice")]
public class TextTests
{
    /// <summary>
    /// The files whose words are all the table's. Each part of N1 adds the files it moves; a word written in quotes
    /// in one of them fails <see cref="TheScreensOnTheTableSayNothingInQuotesOfTheirOwn"/>.
    /// </summary>
    private static readonly string[] OnTheTable =
    {
        "UI/OptionsScreen.cs", "UI/TitleScreen.cs", "UI/StartMenu.cs", "UI/ClosePrompt.cs",
        "UI/BagScreen.cs", "UI/ShopScreen.cs", "UI/PartyScreen.cs", "UI/ModernUi.Items.cs", "UI/ModernUi.Party.cs", "Core/Words.cs",
        "UI/PokedexScreen.cs", "UI/ModernUi.Pokedex.cs", "Models/PokedexSearch.cs", "UI/ModernUi.Records.cs", "UI/TrainerCardScreen.cs",
        "UI/HallOfFameScreen.cs", "UI/JournalScreen.cs", "UI/PCScreen.cs", "UI/ModernUi.Storage.cs", "UI/ModernUi.Lists.cs",
        "UI/StarterSelectScreen.cs", "UI/SaveScreen.cs",
        "UI/ModernUi.TownMap.cs", "UI/ModernUi.Field.cs", "UI/ModernUi.Poketch.cs", "UI/ModernUi.Poketch.Time.cs", "UI/ModernUi.Poketch.Toys.cs",
        "UI/ModernUi.Poketch.Map.cs", "UI/ModernUi.Poketch.Pokemon.cs", "Models/PoketchApps/ColorChangerApp.cs", "UI/HelpScreen.cs",
        "UI/ModernUi.Help.cs", "UI/ModernUi.Wardrobe.cs", "UI/WardrobeScreen.cs", "UI/ModernUi.Hatch.cs", "UI/HatchScreen.cs",
        "UI/ModernUi.Ceremony.cs", "UI/HallOfFameCeremony.cs", "UI/CreditsScreen.cs", "UI/SlotMachineScreen.cs",
        "UI/ModernUi.Frontier.cs", "UI/FrontierRecordsScreen.cs", "Models/Frontier/FrontierBoard.cs", "UI/ModernUi.Factory.cs", "UI/FactoryScreen.cs",
        "UI/ModernUi.Hall.cs", "UI/HallBoardScreen.cs", "UI/ModernUi.Castle.cs", "UI/CastleScreen.cs", "UI/ModernUi.Arcade.cs", "UI/ArcadeBoardScreen.cs",
        "UI/ModernUi.Pastimes.cs"
    };

    /// <summary>What may stay in quotes in a file on the table: names of the data, and the logo, which is a picture (plan 17).</summary>
    private static readonly Dictionary<string, string[]> NotWords = new()
    {
        ["UI/TitleScreen.cs"] = new[] { "Sinnoh", "Giratina", "PLATINUM", "Pokémon", "A  FAN  REMAKE" },
        ["UI/StartMenu.cs"] = new[] { "Trainer" },
        // The items' own names and the uses the item table gives them, which the bag goes by
        ["UI/BagScreen.cs"] = new[]
        {
            "Gracidea", "Bicycle", "OldRod", "GoodRod", "SuperRod", "EscapeRope", "Journal", "VsSeeker", "PokeRadar", "Honey",
            "PoffinCase", "ExplorerKit", "TownMap", "Seal Case", "Fashion Case", "Flute", "Shaymin-Sky"
        },
        // A place's name made from its key says Pokémon with its accent
        ["UI/ModernUi.Pokedex.cs"] = new[] { "Pokémon" },
        // The search's groups of first letters, and the shapes as species.json names them, which it searches by
        ["Models/PokedexSearch.cs"] = new[]
        {
            "ABC", "DEF", "GHI", "JKL", "MNO", "PQR", "STU", "VWX", "YZ",
            "Ball", "Squiggle", "Fish", "Arms", "Blob", "Upright", "Legs", "Quadruped", "Wings", "Tentacles", "Heads", "Humanoid", "Bug Wings", "Armor"
        },
        ["UI/StarterSelectScreen.cs"] = new[] { "Turtwig", "Chimchar", "Piplup" },
        // The words a place's name ends in, which tell a city from a town and a route's colour from a lake's
        ["UI/ModernUi.TownMap.cs"] = new[] { "City" },
        ["UI/ModernUi.Field.cs"] = new[] { "Route", "Lake", "Town", "City" },
        ["UI/ModernUi.Ceremony.cs"] = new[] { "PLATINUM", "Pokémon", "A  FAN  REMAKE" },
        // The bonus game's own Pokémon, whose cry it plays
        ["UI/SlotMachineScreen.cs"] = new[] { "Clefairy" },
        // The same Pokémon, whose sprite the slot machine's display shows in the bonus game
        ["UI/ModernUi.Pastimes.cs"] = new[] { "Clefairy" }
    };

    private static readonly Regex Literal = new(@"(\$?)""((?:[^""\\\n]|\\.)*)""", RegexOptions.Compiled);

    private static string Engine => Path.Combine(Repository.Root, "PokemonPlatinumEngine");

    // A source file's lines with the comments left out
    private static IEnumerable<string> CodeOf(string path) =>
        File.ReadLines(path).Where(line => !line.TrimStart().StartsWith("//")).Select(line =>
        {
            int comment = line.IndexOf(" // ", StringComparison.Ordinal);
            return comment >= 0 && line[..comment].Count(c => c == '"') % 2 == 0 ? line[..comment] : line;
        });

    private static IEnumerable<(string Text, bool Interpolated)> LiteralsOf(string path) =>
        CodeOf(path).SelectMany(line => Literal.Matches(line)).Select(m => (m.Groups[2].Value, m.Groups[1].Value == "$"));

    private static HashSet<string> Sections => Text.EnglishKeys.Select(k => k[..k.IndexOf('.')]).ToHashSet();

    private static bool LooksLikeAKey(string text, HashSet<string> sections) =>
        Regex.IsMatch(text, @"^[a-z][A-Za-z]*(\.[A-Za-z0-9{}.?\s]+)+$") && sections.Contains(text[..text.IndexOf('.')]);

    // A key as a pattern: whatever an interpolation puts in is one segment of letters and digits
    private static Regex PatternOf(string key) =>
        new("^" + string.Concat(Regex.Split(key, @"(\{[^}]*\})").Select(part => part.StartsWith('{') ? "[A-Za-z0-9]+" : Regex.Escape(part))) + "$");

    /// <summary>Every key the engine's code asks for, as written: a literal, or an interpolation of one.</summary>
    private static List<(string Key, string File)> KeysAskedFor()
    {
        var sections = Sections;
        var found = new List<(string, string)>();
        foreach (var file in Directory.EnumerateFiles(Engine, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")) continue;
            foreach (var (text, _) in LiteralsOf(file))
                if (LooksLikeAKey(text, sections)) found.Add((text, Path.GetRelativePath(Engine, file)));
        }
        return found;
    }

    [Fact]
    public void EveryKeyTheEngineAsksForIsInEnglishsTable()
    {
        var keys = Text.EnglishKeys;
        var asked = KeysAskedFor();
        Assert.NotEmpty(asked);
        var missing = asked.Where(a => a.Key.Contains('{') ? !keys.Any(PatternOf(a.Key).IsMatch) : !keys.Contains(a.Key))
            .Select(a => $"{a.File}: {a.Key}").ToList();
        Assert.True(missing.Count == 0, "Keys with no English:\n" + string.Join("\n", missing));
    }

    [Fact]
    public void EveryEntryOfEnglishsTableIsAskedForSomewhere()
    {
        var asked = KeysAskedFor().Select(a => a.Key).Distinct().ToList();
        var plain = asked.Where(k => !k.Contains('{')).ToHashSet();
        var patterns = asked.Where(k => k.Contains('{')).Select(PatternOf).ToList();
        var unused = Text.EnglishKeys.Where(k => !plain.Contains(k) && !patterns.Any(p => p.IsMatch(k))).OrderBy(k => k).ToList();
        Assert.True(unused.Count == 0, "Entries nothing asks for:\n" + string.Join("\n", unused));
    }

    [Fact]
    public void TheScreensOnTheTableSayNothingInQuotesOfTheirOwn()
    {
        var sections = Sections;
        var said = new List<string>();
        foreach (var name in OnTheTable)
        {
            var allowed = NotWords.GetValueOrDefault(name, Array.Empty<string>());
            foreach (var (text, _) in LiteralsOf(Path.Combine(Engine, name)))
            {
                string bare = Regex.Replace(text, @"\{[^}]*\}", "");
                if (!Regex.IsMatch(bare, @"\p{L}{2,}")) continue;            // no word in it: a format, a number
                if (LooksLikeAKey(text, sections)) continue;                 // the table's key
                if (Regex.IsMatch(text, @"^[a-z_]+$")) continue;             // a sound's or a song's name
                if (Regex.IsMatch(text, @"^[X.]+$")) continue;               // a picture in blocks (the Pokétch's)
                if (allowed.Contains(text)) continue;
                said.Add($"{name}: \"{text}\"");
            }
        }
        Assert.True(said.Count == 0, "Words in quotes on screens that are on the table:\n" + string.Join("\n", said));
    }

    [Fact]
    public void TheEnglishIsWhatTheScreensAlwaysSaid()
    {
        Assert.Equal("OPTIONS", Text.Of("options.title"));
        Assert.Equal("Slot 2 can't be read", TitleNotice.ForSlot(2, SaveLoad.Damaged)!.Heading);
        Assert.Equal("It will be saved in slot 3. The save you have is kept as it is.", TitleScreen.NewGameBlurb(3, 1));
        Assert.Equal("It will be saved in slot 3. The saves you have are kept as they are.", TitleScreen.NewGameBlurb(3, 2));
        Assert.Equal("It will be saved in slot 3. The saves you have are kept as they are.", TitleScreen.NewGameBlurb(3, 0));
        Assert.Equal(new[] { "KEEP PLAYING", "SAVE AND QUIT", "QUIT" }, StartMenu.QuitAnswers);
        Assert.Equal("1280 × 720", Text.Of("options.windowSize.value", ("width", 1280), ("height", 720)));
        // N1b: the bag, the shop, the party and the names of the game's enums
        Assert.Equal(new[] { "BUY", "SELL", "SEE YA!" }, ShopScreen.Choices);
        Assert.Equal("Sold a Potion for 150.", Text.Of("shop.sold", ("n", 1), ("item", "Potion"), ("money", 150)));
        Assert.Equal("Sold 3 × Potion for 450.", Text.Of("shop.sold", ("n", 3), ("item", "Potion"), ("money", 450)));
        Assert.Equal("Choose 3 Pokémon to enter. (2 to go)", Text.Of("party.prompt.enter", ("count", 3), ("left", 2)));
        Assert.Equal("PSN", Words.Status(StatusCondition.Toxic));
        Assert.Equal("", Words.Status(StatusCondition.None));
        Assert.Equal("Sp. Atk", Words.Stat(StatType.SpAttack));
        Assert.Equal("Quirky", Words.Nature(Nature.Quirky));
        Assert.Equal("9 March 2026", Words.Date(new DateTime(2026, 3, 9)));
        Assert.Equal(new DateTime(2026, 3, 9).ToString("d MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture), Words.Date(new DateTime(2026, 3, 9)));
        Assert.All(Enum.GetValues<PokemonType>(), t => Assert.Equal(t.ToString(), Words.Type(t)));
        Assert.All(Enum.GetValues<Nature>(), n => Assert.Equal(n.ToString(), Words.Nature(n)));
        // N1c: the Pokédex, the records, the PC, the starters and saving
        Assert.Equal(new[] { "Number", "A to Z", "Heaviest" }, new[] { PokedexOrder.Number, PokedexOrder.Alphabet, PokedexOrder.Heaviest }.Select(PokedexSearch.NameOf));
        Assert.All(PokedexSearch.Shapes, shape => Assert.Equal(shape, PokedexSearch.NameOfShape(shape)));
        Assert.Equal("Any", PokedexSearch.NameOfGroup(-1));
        Assert.Equal("Grass: morning, night · Surfing · Old, Good Rods",
            ModernUi.WaysOf(HabitatWays.Morning | HabitatWays.Night | HabitatWays.Surfing | HabitatWays.OldRod | HabitatWays.GoodRod));
        Assert.Equal("Grass, all day · Super Rod", ModernUi.WaysOf(HabitatWays.Morning | HabitatWays.Day | HabitatWays.Night | HabitatWays.SuperRod));
        Assert.Equal("No. 007", ModernUi.DexNumber(7));
        Assert.Equal("10.5 kg", ModernUi.Kilograms(10.5f));
        Assert.Equal("Bidoof lives in one place.", Text.Of("pokedex.livesIn", ("name", "Bidoof"), ("n", 1)));
        Assert.Equal(Enumerable.Range(0, PcBoxes.WallpaperNames.Count).Select(i => PcBoxes.WallpaperNames[i]), Enumerable.Range(0, PcBoxes.WallpaperNames.Count).Select(i => Text.Of($"boxes.wallpaper.{i}")));
        Assert.Equal("Monday", Words.Weekday(new DateTime(2026, 10, 12)));
        Assert.Equal("9 Mar 2026", Words.ShortDate(new DateTime(2026, 3, 9)));
        Assert.Equal(new[] { "SAVE", "CANCEL" }, SaveScreen.Answers);
        Assert.Equal("SLOT 2", ModernUi.SlotLabel(2));
        // N1d1: the Town Map, the Pokétch, help, the wardrobe and the Hall of Fame's scene
        Assert.Equal("October", Words.Month(new DateTime(2026, 10, 12)));
        Assert.Equal("GREEN", ColorChangerApp.Name(0));
        Assert.Equal("FAI", HelpScreen.ShortName(PokemonType.Fairy));
        var help = new HelpScreen();
        help.Open();
        Assert.Equal(("Super effective", "A Fire move against a Grass Pokémon does twice the damage."), help.Describe());
        Assert.Equal("Your Own Bottoms", ModernUi.GarmentName(ClothingSlot.Bottoms, null));
        Assert.Equal(new[] { "BUY", "NO" }, WardrobeScreen.BuyChoices);
        Assert.Equal("Met at Wayward Cave, at Lv. 62.", Text.Of("ceremony.metAt", ("place", "Wayward Cave"), ("level", 62)));
        // A placeholder the call names nothing for is left for whoever fills it later
        Assert.Equal("Hello {player}", Text.Fill("Hello {player}"));
    }

    [Fact]
    public void AKeyWithNoEntryShowsItselfInBrackets()
    {
        Assert.Equal("[no.such.key]", Text.Of("no.such.key"));
        Assert.False(Text.Has("no.such.key"));
    }

    [Fact]
    public void TheLanguagesFollowTheirOwnPluralRules()
    {
        var english = Language.Of("en")!;
        Assert.Equal(new[] { "other", "one", "other", "other" }, new long[] { 0, 1, 2, 21 }.Select(english.FormFor));
        var french = new Language { Tag = "fr", Plural = PluralRule.French };
        Assert.Equal(new[] { "one", "one", "other" }, new long[] { 0, 1, 2 }.Select(french.FormFor));
        var none = new Language { Tag = "ja", Plural = PluralRule.None };
        Assert.Equal("other", none.FormFor(1));
        Assert.Equal("1.5", Text.Decimal(1.5, 1));
    }

    [Fact]
    public void OnlyLanguagesThePlayerCanReadAreOffered()
    {
        Assert.Contains(Language.All, l => l.Tag == Text.English);
        Assert.Contains(Language.All, l => l.Tag == Text.Pseudo && l.Pseudo);
        Assert.DoesNotContain(Language.Offered, l => l.Pseudo);
        Assert.All(Language.Offered, l => Assert.Contains(l.Script, Language.DrawnScripts));

        // The options' row goes round what is offered and never lands on the pseudo-language
        var settings = new GameSettings();
        Assert.Equal(Text.English, settings.Language);
        for (int i = 0; i < Language.All.Count + 1; i++)
        {
            OptionsScreen.Change(settings, OptionRow.Language, 1);
            Assert.Contains(Language.Offered, l => l.Tag == settings.Language);
        }
        settings.Language = Text.Pseudo;
        OptionsScreen.Change(settings, OptionRow.Language, 1);
        Assert.NotEqual(Text.Pseudo, settings.Language);
    }

    [Fact]
    public void ThePseudoLanguageBracketsEveryString()
    {
        try
        {
            Text.Use(Text.Pseudo);
            Assert.Equal(Text.Pseudo, Text.Current);
            Assert.Equal("[OPTIONS]", Text.Of("options.title"));
            Assert.Equal("[Delete slot 2?]", TitleScreen.DeleteHeading(2));
            Assert.All(StartMenu.QuitAnswers, a => Assert.Matches(@"^\[.+\]$", a));

            // A tag nobody knows is English
            Text.Use("qq");
            Assert.Equal(Text.English, Text.Current);
            Assert.Equal("OPTIONS", Text.Of("options.title"));
        }
        finally
        {
            Text.Use(Text.English);
        }
    }

    [Fact]
    public void AnEntryOfTheBoyAndTheGirlFollowsThePlayersLook()
    {
        // The forms' reader is the same for every entry: one of the table's own is held, and a look's form is read from the look
        Assert.Equal("Your save is intact.", Text.Of("title.notice.crash.intact", ("n", 1)));
        Assert.Equal("Your saves are intact.", Text.Of("title.notice.crash.intact", ("n", 3)));
        Assert.Equal("girl", Text.FormOfLook(PlayerLook.Girl));
        Assert.Equal("boy", Text.FormOfLook(PlayerLook.Boy));
    }
}
