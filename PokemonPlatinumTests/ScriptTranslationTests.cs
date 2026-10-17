using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// The scripts, overlays and rooms in another language (plan 17 · N4): every text-bearing line of a script is
/// numbered, a language's sibling files hold the same lines in order, a line is translated as it is shown, an entry
/// written against other English counts for nothing, and the report says exactly what is missing. The language is the
/// whole process's, so the class runs alone and puts English back.
/// </summary>
[Collection("Shared clocks and dice")]
public sealed class ScriptTranslationTests : IDisposable
{
    private readonly Func<string, string?> reader = TranslationFiles.Reader;
    private readonly Func<string, IEnumerable<string>> lister = TranslationFiles.Lister;

    public void Dispose()
    {
        TranslationFiles.Reader = reader;
        TranslationFiles.Lister = lister;
        TranslationFiles.Forget();
        Text.Use(Text.English);
    }

    /// <summary>Gives the language <c>ro</c> (listed, with no files of its own yet) the files named, by their path under <c>Data</c>.</summary>
    private static void Romanian(Dictionary<string, string> files)
    {
        string Key(string relative) => relative.Replace('\\', '/');
        var byPath = files.ToDictionary(f => Key(f.Key), f => f.Value);
        TranslationFiles.Reader = relative => byPath.GetValueOrDefault(Key(relative));
        TranslationFiles.Lister = relative => byPath.Keys.Where(p => Path.GetDirectoryName(p)!.Replace('\\', '/') == Key(relative)).Select(p => Path.GetFileNameWithoutExtension(p)).ToList();
        TranslationFiles.Forget();
        Text.Use("ro");
    }

    private const string Source = @"
script Greet
  speaker ""Mom""
  say ""Hello, {player}!""
  say ""Bye now.""
  if flag FLAG_TEST_NEVER say ""Not said.""
  ask ""Sure?""
  choose ""Pick one"" ""Red"" ""Blue""
  speaker none
  text ""A line with no one speaking.""
  end
";

    private static (HeadlessScriptHost Host, ScriptRunner Runner) Play(string source, string name, NPC? subject = null, IReadOnlyList<string>? own = null,
        (string Place, string Key)? sign = null, params int[] answers)
    {
        var library = ScriptLibrary.FromSources(("test_town", source));
        var host = new HeadlessScriptHost { Map = new Map(8, 8) };
        foreach (int a in answers) host.Answers.Enqueue(a);
        var runner = new ScriptRunner(library, host);
        runner.Start(library.Find(name, "test_town")!, subject, own, sign: sign);
        runner.RunToEnd();
        return (host, runner);
    }

    // ------------------------------------------------------------------ the numbering

    [Fact]
    public void EveryLineThatSpeaksIsNumberedInTheOrderItIsWritten()
    {
        var script = ScriptParser.Parse("test_town", Source).Single();
        var numbered = script.Everything().Where(i => i.TextIndex >= 0).OrderBy(i => i.TextIndex).ToList();
        Assert.Equal(new[] { Op.Speaker, Op.Say, Op.Say, Op.Say, Op.Ask, Op.Choose, Op.Text }, numbered.Select(i => i.Op));
        Assert.Equal(Enumerable.Range(0, 7), numbered.Select(i => i.TextIndex));
        // A line an if guards is numbered where it is written; a speaker with no name, an end and an if say nothing
        Assert.Equal("Not said.", numbered[3].Lines.Single());
        Assert.All(script.Everything().Where(i => i.Op is Op.End or Op.If), i => Assert.Equal(-1, i.TextIndex));
        Assert.Equal(-1, script.Code.Single(i => i.Op == Op.Speaker && i.Name.Length == 0).TextIndex);
    }

    [Fact]
    public void ALineHasOneAddressForTheTranslationFilesAndTheVoiceCatalogue()
    {
        Assert.Equal("script:twinleaf_town.RivalRunsOut#2.0", Text.ScriptLineKey("twinleaf_town.RivalRunsOut", 2, 0));
        Assert.Equal("world:twinleaf_town.mom.dialog.1", Text.PersonLineKey("world", "twinleaf_town", "mom", PersonField.Dialog, 1));
        Assert.Equal("map:PlayerHouse.12,3.before.0", Text.PersonLineKey("map", "PlayerHouse", "12,3", PersonField.Before, 0));
    }

    [Fact]
    public void ABatchOfLinesAndAChoiceListTheirWordsInOrder()
    {
        var choose = ScriptParser.Parse("test_town", Source).Single().Code.Single(i => i.Op == Op.Choose);
        Assert.Equal(new[] { "Pick one", "Red", "Blue" }, choose.Words);
        var speaker = ScriptParser.Parse("test_town", Source).Single().Code.First(i => i.Op == Op.Speaker);
        Assert.Equal(new[] { "Mom" }, speaker.Words);
    }

    // ------------------------------------------------------------------ a line is translated as it is shown

    private static string Entry(string[] en, string[] text) =>
        "{ \"en\": [" + string.Join(", ", en.Select(l => "\"" + l + "\"")) + "], \"text\": [" + string.Join(", ", text.Select(l => "\"" + l + "\"")) + "] }";

    private static string GreetFile(string saysEnglish = "Hello, {player}!") => "{ \"Greet\": [" + string.Join(", ",
        Entry(new[] { "Mom" }, new[] { "Mama" }),
        Entry(new[] { saysEnglish }, new[] { "Salut, {player}!" }),
        Entry(new[] { "Bye now." }, new[] { "" }),
        Entry(new[] { "Not said." }, new[] { "Nu se spune." }),
        Entry(new[] { "Sure?" }, new[] { "Sigur?" }),
        Entry(new[] { "Pick one", "Red", "Blue" }, new[] { "Alege", "Rosu", "Albastru" }),
        Entry(new[] { "A line with no one speaking." }, new[] { "O replica fara vorbitor." })) + "] }";

    [Fact]
    public void ALanguageWithNoFilesSaysTheScriptInEnglish()
    {
        var (english, _) = Play(Source, "Greet");
        Romanian(new Dictionary<string, string>());
        var (romanian, _) = Play(Source, "Greet");
        Assert.Equal(english.Transcript, romanian.Transcript);
        Assert.Equal(english.Asked, romanian.Asked);
    }

    [Fact]
    public void ATranslatedScriptSaysItsLinesQuestionsAndAnswersInTheLanguageWithItsPlaceholdersFilled()
    {
        Romanian(new Dictionary<string, string> { ["text/ro/scripts/test_town.json"] = GreetFile() });
        var (host, _) = Play(Source, "Greet", answers: new[] { 0, 1 });
        string player = host.Transcript[0].Text.Split(' ')[1].TrimEnd('!');
        Assert.Equal(("Mama", $"Salut, {player}!"), host.Transcript[0]);
        // An entry with no text for a line says that line in English, and the guarded line is not said
        Assert.Equal(("Mama", "Bye now."), host.Transcript[1]);
        Assert.DoesNotContain(host.Transcript, line => line.Text.Contains("Nu se spune"));
        Assert.Equal(("Mama", "Sigur?"), host.Transcript[2]);
        Assert.Equal(("Mama", "Alege"), host.Transcript[3]);
        // The answers are the language's; the script goes by the one chosen (the second: Blue)
        Assert.Equal(new[] { ("Sigur?", "Yes"), ("Alege", "Albastru") }, host.Asked.Select(a => (a.Question, a.Answer)).ToArray());
        Assert.Equal((null, "O replica fara vorbitor."), host.Transcript[^1]);
    }

    [Fact]
    public void AnEntryWrittenAgainstOtherEnglishIsStaleAndTheEnglishIsShown()
    {
        Romanian(new Dictionary<string, string> { ["text/ro/scripts/test_town.json"] = GreetFile("Hello there, {player}!") });
        var (host, _) = Play(Source, "Greet");
        Assert.StartsWith("Hello, ", host.Transcript[0].Text);
        // The speaker's entry was not stale and still counts
        Assert.Equal("Mama", host.Transcript[0].Speaker);
        var report = TextSources.Count(TextSources.Flatten(TextSources.Scripts(ScriptLibrary.FromSources(("test_town", Source)))["test_town"]),
            TextSources.Flatten(TranslationFiles.Scripts("ro", "test_town")!));
        Assert.Equal(1, report.Stale);
        Assert.Equal(0, report.Unknown);
    }

    [Fact]
    public void ThePseudoLanguageBracketsEveryLineOfAScript()
    {
        Text.Use(Text.Pseudo);
        var (host, _) = Play(Source, "Greet", answers: new[] { 0, 0 });
        Assert.All(host.Transcript, line =>
        {
            Assert.StartsWith("[", line.Text);
            Assert.EndsWith("]", line.Text);
        });
        Assert.Equal("[Mom]", host.Transcript[0].Speaker);
        Assert.Equal("[Red]", host.Asked[1].Answer);
    }

    [Fact]
    public void EveryScriptOfTheGameUnderThePseudoLanguageSaysBracketedLinesOnly()
    {
        Text.Use(Text.Pseudo);
        var walk = new ScriptWalk();
        var unbracketed = new List<string>();
        foreach (var (what, script, ways) in walk.PlayEveryScript())
            foreach (var way in ways)
            {
                foreach (var (speaker, text) in way.Host.Transcript)
                {
                    if (!text.StartsWith('[') || !text.EndsWith(']')) unbracketed.Add($"{script.FullName} ({what}): {text}");
                    if (speaker != null && !speaker.StartsWith('[')) unbracketed.Add($"{script.FullName} ({what}), spoken by {speaker}");
                }
                foreach (var (question, answer) in way.Host.Asked)
                    if (!answer.StartsWith('[')) unbracketed.Add($"{script.FullName} ({what}): answer {answer} to {question}");
            }
        Assert.True(unbracketed.Count == 0, $"{unbracketed.Count} lines slipped past the translation:\n  " + string.Join("\n  ", unbracketed.Distinct().Take(40)));
    }

    // ------------------------------------------------------------------ people and signs

    private const string Talk = @"
script Talk
  sayown
  end
";

    private static NPC Mom() => new() { Name = "Mom", DialogLines = new() { "Take care.", "Come back soon." }, LinesPlace = "test_town", LinesKey = "mom" };

    [Fact]
    public void APersonsLinesAndNameAreTranslatedByTheirPlaceAndKey()
    {
        Romanian(new Dictionary<string, string>
        {
            ["text/ro/world/test_town.json"] = "{ \"people\": { \"mom\": { \"name\": " + Entry(new[] { "Mom" }, new[] { "Mama" })
                + ", \"dialog\": " + Entry(new[] { "Take care.", "Come back soon." }, new[] { "Ai grija.", "" }) + " } } }"
        });
        var (host, _) = Play(Talk, "Talk", Mom());
        Assert.Equal(new (string?, string)[] { ("Mama", "Ai grija."), ("Mama", "Come back soon.") }, host.Transcript.ToArray());
    }

    [Fact]
    public void ASignIsTranslatedByItsPlaceAndId()
    {
        Romanian(new Dictionary<string, string>
        {
            ["text/ro/maps/TestRoom.json"] = "{ \"signs\": { \"3,4\": " + Entry(new[] { "KEEP OUT" }, new[] { "INTRARE INTERZISA" }) + " } }"
        });
        var (host, _) = Play(Talk, "Talk", own: new[] { "KEEP OUT" }, sign: ("TestRoom", "3,4"));
        Assert.Equal("INTRARE INTERZISA", host.Transcript.Single().Text);
        // The same sign somewhere the file doesn't list says its English
        var (elsewhere, _) = Play(Talk, "Talk", own: new[] { "KEEP OUT" }, sign: ("TestRoom", "9,9"));
        Assert.Equal("KEEP OUT", elsewhere.Transcript.Single().Text);
    }

    [Fact]
    public void ATrainersWordsBeforeAndAfterAreTranslated()
    {
        Romanian(new Dictionary<string, string>
        {
            ["text/ro/world/test_town.json"] = "{ \"people\": { \"hiker\": { \"before\": " + Entry(new[] { "Let's battle!" }, new[] { "Hai la lupta!" }) + " } } }"
        });
        var trainer = new NPC { Name = "Hiker", IsTrainer = true, LinesPlace = "test_town", LinesKey = "hiker", TrainerData = new PokemonPlatinumEngine.Models.Trainer { DialogueBefore = "Let's battle!" } };
        var (host, _) = Play("script Talk\n  trainerline before\n  end\n", "Talk", trainer);
        Assert.Equal("Hai la lupta!", host.Transcript.Single().Text);
    }

    [Fact]
    public void APlacesNameIsTheRoomsOwnThenPokeApisThenTheLanguagesTableThenEnglish()
    {
        Romanian(new Dictionary<string, string>
        {
            ["text/ro/maps/TestRoom.json"] = "{ \"displayName\": " + Entry(new[] { "Test Room" }, new[] { "Camera de proba" }) + " }"
        });
        Assert.Equal("Camera de proba", LocalNames.PlaceShown("TestRoom", "Test Room"));
        Assert.Equal("Somewhere Else", LocalNames.PlaceShown("OtherRoom", "Somewhere Else"));
        Text.Use(Text.Pseudo);
        Assert.Equal("[Test Room]", LocalNames.PlaceShown("TestRoom", "Test Room"));
    }

    [Fact]
    public void AGameSavedInOneLanguageNamesItsPlaceInEnglishTheMapKeepsAsItsIdentifier()
    {
        var map = MapDatabase.Get("PlayerHouse");
        Text.Use(Text.Pseudo);
        // What a Pokémon is met at and a save keeps stays the English name; what a screen shows is the language's
        Assert.DoesNotContain("[", map.DisplayNameAt(4, 6));
        Assert.StartsWith("[", map.ShownNameAt(4, 6));
    }

    // ------------------------------------------------------------------ the files and the report

    [Fact]
    public void TheSkeletonListsEveryLineOfTheGamesScriptsOverlaysAndRooms()
    {
        var scripts = TextSources.Scripts();
        Assert.Contains("twinleaf_town", scripts.Keys);
        foreach (var (file, byScript) in scripts)
            foreach (var (name, entries) in byScript)
            {
                var script = ScriptLibrary.Default.In(file, name)!;
                Assert.Equal(script.Everything().Count(i => i.SpeaksText), entries.Count);
                Assert.All(entries, e => Assert.Equal(e.En.Count, e.Text.Count));
            }
        Assert.Contains("twinleaf_town", TextSources.Overlays().Keys);
        Assert.Contains("PlayerHouse", TextSources.Maps().Keys);
        Assert.NotEmpty(TextSources.Overlays()["twinleaf_town"].People);
    }

    [Fact]
    public void MergingKeepsWhatIsTranslatedMarksWhatWentStaleAndAddsWhatIsNew()
    {
        var english = new Dictionary<string, List<LineEntry>>
        {
            ["A"] = new()
            {
                new LineEntry { En = new() { "One" }, Text = new() { "" } },
                new LineEntry { En = new() { "Two, rewritten" }, Text = new() { "" } },
                new LineEntry { En = new() { "Three" }, Text = new() { "" } }
            }
        };
        var existing = new Dictionary<string, List<LineEntry>>
        {
            ["A"] = new()
            {
                new LineEntry { En = new() { "One" }, Text = new() { "Unu" } },
                new LineEntry { En = new() { "Two" }, Text = new() { "Doi" } }
            },
            ["Gone"] = new() { new LineEntry { En = new() { "X" }, Text = new() { "Y" } } }
        };
        var merged = TextSources.Merge(english, existing);
        Assert.Equal("Unu", merged["A"][0].Text[0]);
        Assert.Equal(new[] { "Two, rewritten" }, merged["A"][1].En);
        Assert.Equal("", merged["A"][1].Text[0]);
        Assert.Equal(new[] { "Doi" }, merged["A"][1].Was);
        Assert.Equal("", merged["A"][2].Text[0]);
        // A script the game no longer has is dropped
        Assert.DoesNotContain("Gone", merged.Keys);
    }

    [Fact]
    public void TheFilesOfALanguageAreHeldToTheShapeOfTheEnglishAndItsPlaceholders()
    {
        const string path = "text/ro/scripts/twinleaf_town.json";
        Romanian(new() { [path] = "{}" });
        Assert.Empty(TextSources.Problems("ro"));

        // The skeleton itself, one line translated: a line with {rival} in it
        string Skeleton(Action<Dictionary<string, List<LineEntry>>> change)
        {
            var copy = GameDataFiles.Deserialize<Dictionary<string, List<LineEntry>>>(GameDataFiles.Serialize(TextSources.Scripts()["twinleaf_town"]));
            change(copy);
            return GameDataFiles.Serialize(copy);
        }
        LineEntry Rival(Dictionary<string, List<LineEntry>> file) => file.Values.SelectMany(v => v).First(e => e.En.Any(l => l.Contains("{rival}")));

        Romanian(new() { [path] = Skeleton(_ => { }) });
        Assert.Empty(TextSources.Problems("ro"));
        Romanian(new() { [path] = Skeleton(f => Rival(f).Text[0] = "Fara cod") });
        Assert.Empty(TextSources.Problems("ro"));
        Romanian(new() { [path] = Skeleton(f => Rival(f).Text[0] = "Cu {rival} si {player}") });
        Assert.Empty(TextSources.Problems("ro").Where(p => p.Contains("{rival}")));
        // A placeholder its English line does not have, the wrong number of lines
        Romanian(new() { [path] = Skeleton(f => Rival(f).Text[0] = "Salut {money}") });
        Assert.Contains(TextSources.Problems("ro"), p => p.Contains("{money}"));
        Romanian(new() { [path] = Skeleton(f => Rival(f).Text.Clear()) });
        Assert.Contains(TextSources.Problems("ro"), p => p.Contains("0 lines of text"));
        // An entry written against other English is stale, and nothing in it is looked at
        Romanian(new() { [path] = Skeleton(f => { var e = Rival(f); e.En[0] += " (old)"; e.Text[0] = "Salut {money}"; }) });
        Assert.Empty(TextSources.Problems("ro"));
        // A script that isn't there, a file that is nobody's
        Romanian(new() { [path] = "{ \"NoSuchScript\": [ " + Entry(new[] { "x" }, new[] { "y" }) + " ] }" });
        Assert.Contains(TextSources.Problems("ro"), p => p.Contains("NoSuchScript#0 matches no line"));
        Romanian(new() { ["text/ro/scripts/no_such_file.json"] = "{}", ["text/ro/world/no_such_area.json"] = "{}" });
        var problems = TextSources.Problems("ro");
        Assert.Contains(problems, p => p.Contains("no_such_file") && p.Contains("script file"));
        Assert.Contains(problems, p => p.Contains("no_such_area") && p.Contains("overlay"));
    }

    [Fact]
    public void CodesAreHeldWholeForPlaceholdersAndByNameForMarkup()
    {
        Assert.Equal(new[] { "{player}", "{var:VAR_X}" }, TextSources.CodesOf("Hi {player}, {var:VAR_X}"));
        Assert.Equal(new[] { "{red...}", "{pause...}" }, TextSources.CodesOf("{red:Alert} {pause 0.4}"));
    }

    [Fact]
    public void TheShippedLanguagesFilesHaveNothingWrongWithThem()
    {
        foreach (var language in Language.All.Where(l => l.Tag != Text.English && !l.Pseudo))
            Assert.True(TextSources.Problems(language.Tag).Count == 0, $"{language.Tag}: {string.Join("; ", TextSources.Problems(language.Tag).Take(10))}");
    }

    [Fact]
    public void TheLocalisationReportIsTheOneTheFilesGive()
    {
        string path = Path.Combine(Repository.Root, "docs", "localisation.md");
        string expected = TextSources.Report();
        if (Environment.GetEnvironmentVariable("GOLDEN") == "accept") File.WriteAllText(path, expected);
        Assert.True(File.Exists(path), "docs/localisation.md is missing: run `dotnet run --project tools/TextTool -- report`");
        Assert.Equal(expected, File.ReadAllText(path).Replace("\r\n", "\n"));
    }
}
