using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumTests;

/// <summary>
/// Phrases, interviews and reporters (plan 08 · P9): the words a phrase is made of (<see cref="PhraseGroups"/>), the
/// picker's cursor and its groups (<see cref="PhraseEntry"/>, <see cref="PhraseScreen"/>), a phrase kept in the story
/// and the save, Jubilife City's reporters played on every way through, and the interview read back on the television
/// at home. In the collection that runs alone, since the words are English's and another test may change the language.
/// </summary>
[Collection("Shared clocks and dice")]
public class PhraseTests
{
    private static readonly string[] HappyTogether = { "feelings:happy", "small:together" };
    private const string Place = "jubilife_city";
    private const string OnAir = Broadcasts.InterviewOnAirFlag;
    private const string Daily = "FLAG_DAILY_JUBILIFE_INTERVIEW";

    // ------------------------------------------------------------------ the words

    [Fact]
    public void TheGroupsHoldTheGamesNamesAndOurOwnWords()
    {
        // Pokémon seen and the team's moves: names of the data only, each once, in the order given
        var groups = new PhraseGroups(new[] { "Turtwig", "Starly", "Turtwig", "NoSuchPokemon" }, new[] { "Tackle", "Tackle", "Withdraw" });
        Assert.Equal(new[] { "pokemon:Turtwig", "pokemon:Starly" }, groups.WordsOf(PhraseGroup.Pokemon));
        Assert.Equal(new[] { "moves:Tackle", "moves:Withdraw" }, groups.WordsOf(PhraseGroup.Moves));
        Assert.Equal(Enum.GetValues<PokemonType>().Length, groups.WordsOf(PhraseGroup.Types).Count);
        // Our own five groups, every word of each one, each with a word of English's own
        foreach (var group in new[] { PhraseGroup.Feelings, PhraseGroup.People, PhraseGroup.Places, PhraseGroup.Time, PhraseGroup.Small })
        {
            var words = groups.WordsOf(group);
            Assert.True(words.Count >= 15, $"{group} has {words.Count} words");
            Assert.Equal(words.Count, words.Distinct().Count());
            Assert.All(words, w => Assert.True(PhraseGroups.IsWord(w), w));
            Assert.All(words, w => Assert.False(string.IsNullOrWhiteSpace(PhraseGroups.Shown(w)), w));
        }
        // With nothing seen and no team, the names' groups are empty and the rest are there
        var plain = PhraseGroups.Plain();
        Assert.Empty(plain.WordsOf(PhraseGroup.Pokemon));
        Assert.Empty(plain.WordsOf(PhraseGroup.Moves));
        Assert.NotEmpty(plain.WordsOf(PhraseGroup.Feelings));
        // Eight groups, each with a name of its own on the picker
        Assert.Equal(8, PhraseGroups.All.Count);
        Assert.Equal(8, PhraseGroups.All.Select(PhraseGroups.GroupName).Distinct().Count());
    }

    [Fact]
    public void AWordIsKeptByItsKeyAndSaidInTheLanguageChosen()
    {
        Assert.Equal("feelings:happy", PhraseGroups.Key(PhraseGroup.Feelings, "happy"));
        Assert.True(PhraseGroups.TryParse("small:together", out var group, out string id));
        Assert.Equal((PhraseGroup.Small, "together"), (group, id));
        foreach (string bad in new[] { "", "happy", ":happy", "feelings:", "nonsense:happy" })
            Assert.False(PhraseGroups.TryParse(bad, out _, out _), bad);
        // A key that names no word is no word: not one of our lists, a species or a move the data hasn't
        Assert.False(PhraseGroups.IsWord("feelings:grumpy"));
        Assert.False(PhraseGroups.IsWord("pokemon:Nobodymon"));
        Assert.False(PhraseGroups.IsWord("types:Shadow"));

        Assert.Equal("happy", PhraseGroups.Shown("feelings:happy"));
        Assert.Equal("Turtwig", PhraseGroups.Shown("pokemon:Turtwig"));
        Assert.Equal("Tackle", PhraseGroups.Shown("moves:Tackle"));
        Assert.Equal(Words.Type(PokemonType.Fire), PhraseGroups.Shown("types:Fire"));
        Assert.Equal("", PhraseGroups.Shown("feelings:grumpy"));
        Assert.Equal("happy together", PhraseGroups.Shown(HappyTogether));

        Assert.True(PhraseGroups.IsWhole(HappyTogether));
        Assert.False(PhraseGroups.IsWhole(new[] { "feelings:happy" }));
        Assert.False(PhraseGroups.IsWhole(new[] { "feelings:happy", "feelings:grumpy" }));
        Assert.False(PhraseGroups.IsWhole(null));
    }

    // ------------------------------------------------------------------ the picker

    [Fact]
    public void ThePickersCursorGoesAlongThePhraseAndThroughTheGroupsAndTheirWords()
    {
        var entry = new PhraseEntry(new PhraseGroups(new[] { "Turtwig" }, new[] { "Tackle" }));
        Assert.Equal((PhraseStage.Slots, 0, false), (entry.Stage, entry.Slot, entry.Complete));

        // Along the phrase's row: the two words and OK, wrapping round; OK with no words is refused
        entry.Move(1, 0);
        entry.Move(1, 0);
        Assert.Equal(PhraseEntry.OkSlot, entry.Slot);
        Assert.Equal(PhrasePress.Refused, entry.Press());
        entry.Move(1, 0);
        Assert.Equal(0, entry.Slot);
        entry.Move(0, -1);
        Assert.Equal(PhraseEntry.OkSlot, entry.Slot);
        entry.Move(0, 1);
        Assert.Equal(0, entry.Slot);

        // The groups, two to a row: across, down and round from the top to the bottom row
        Assert.Equal(PhrasePress.Opened, entry.Press());
        Assert.Equal((PhraseStage.Groups, PhraseGroup.Pokemon), (entry.Stage, entry.Group));
        entry.Move(1, 0);
        Assert.Equal(PhraseGroup.Moves, entry.Group);
        entry.Move(0, 1);
        Assert.Equal(PhraseGroup.Feelings, entry.Group);
        entry.Move(0, -1);
        entry.Move(0, -1);
        Assert.Equal(PhraseGroup.Small, entry.Group);
        entry.Move(0, -1);
        entry.Move(0, -1);
        Assert.Equal(PhraseGroup.Feelings, entry.Group);

        // A group's words, three to a row with five rows in view: down past the fourth row moves the window
        Assert.Equal(PhrasePress.Opened, entry.Press());
        Assert.Equal((PhraseStage.Words, 0, 0), (entry.Stage, entry.Word, entry.FirstRow));
        Assert.Equal("feelings:happy", entry.GroupWords[0]);
        for (int i = 0; i < 4; i++) entry.Move(0, 1);
        Assert.Equal((12, 1), (entry.Word, entry.FirstRow));
        for (int i = 0; i < 4; i++) entry.Move(0, -1);
        Assert.Equal((0, 0), (entry.Word, entry.FirstRow));
        entry.Move(-1, 0);
        Assert.Equal(2, entry.Word);
        entry.Move(1, 0);
        Assert.Equal(0, entry.Word);

        // A word chosen fills the slot and goes on to the next one
        Assert.Equal(PhrasePress.Chose, entry.Press());
        Assert.Equal((PhraseStage.Slots, 1), (entry.Stage, entry.Slot));
        Assert.Equal("feelings:happy", entry.Words[0]);

        // B goes back a step at a time, and leaves the phrase as it was
        entry.Press();
        entry.Press();
        Assert.Equal(PhraseStage.Words, entry.Stage);
        Assert.False(entry.Back());
        Assert.Equal(PhraseStage.Groups, entry.Stage);
        Assert.False(entry.Back());
        Assert.Equal((PhraseStage.Slots, 1), (entry.Stage, entry.Slot));
        Assert.Null(entry.Words[1]);

        // The second word, and then OK: the phrase is done
        Assert.True(entry.Choose(1, "small:together"));
        Assert.Equal((PhraseEntry.OkSlot, true), (entry.Slot, entry.Complete));
        Assert.Null(entry.Result);
        Assert.Equal(PhrasePress.Done, entry.Press());
        Assert.Equal(HappyTogether, entry.Result);
        // Nothing moves once it is done
        entry.Move(1, 0);
        Assert.Equal(PhrasePress.Nothing, entry.Press());
    }

    [Fact]
    public void ThePickerOpensOnTheLastAnswerAndAnEmptyGroupCantBeOpened()
    {
        var groups = new PhraseGroups(new[] { "Turtwig" }, Array.Empty<string>());
        // Opened on the answer given last time: both words there, the cursor on OK
        var entry = new PhraseEntry(groups, new[] { "feelings:sad", "types:Fire" });
        Assert.Equal((PhraseEntry.OkSlot, true), (entry.Slot, entry.Complete));
        // Its first word opens its own group, on itself, the window moved to show it
        entry.Move(1, 0);
        entry.Press();
        Assert.Equal(PhraseGroup.Feelings, entry.Group);
        entry.Press();
        Assert.Equal("feelings:sad", entry.GroupWords[entry.Word]);
        Assert.True(entry.FirstRow <= entry.Word / PhraseEntry.WordColumns
            && entry.Word / PhraseEntry.WordColumns < entry.FirstRow + PhraseEntry.WordRows);
        // Backed out of all the way, the phrase is given up and nothing is kept
        Assert.False(entry.Back());
        Assert.False(entry.Back());
        Assert.True(entry.Back());
        Assert.True(entry.Cancelled);
        Assert.Null(entry.Result);

        // A word that is no word is left out of the phrase it opens on
        var odd = new PhraseEntry(groups, new[] { "nonsense", "feelings:happy" });
        Assert.Equal((0, false), (odd.Slot, odd.Complete));
        Assert.Equal(new string?[] { null, "feelings:happy" }, odd.Words);

        // The team knows no moves: their group is refused, the others open
        odd.Press();
        odd.Move(1, 0);
        Assert.Equal(PhraseGroup.Moves, odd.Group);
        Assert.Equal(PhrasePress.Refused, odd.Press());
        Assert.Equal(PhraseStage.Groups, odd.Stage);
        // Start takes the cursor straight to OK
        odd.ToDone();
        Assert.Equal((PhraseStage.Slots, PhraseEntry.OkSlot), (odd.Stage, odd.Slot));
    }

    [Fact]
    public void TheScreenClosesOnAPhraseOrWhenItIsBackedOutOf()
    {
        var screen = new PhraseScreen();
        screen.Open(StoryState.InterviewPhrase, PhraseGroups.Plain(), null);
        Assert.True(screen.IsActive);
        Assert.Equal(StoryState.InterviewPhrase, screen.Name);
        Assert.True(screen.Entry!.Choose(0, "feelings:happy"));
        Assert.True(screen.Entry.Choose(1, "small:together"));
        Assert.Null(screen.Outcome);
        screen.Confirm();
        Assert.False(screen.IsActive);
        Assert.Equal(HappyTogether, screen.Outcome);

        screen.Open(StoryState.InterviewPhrase, PhraseGroups.Plain(), HappyTogether);
        screen.Cancel();
        Assert.False(screen.IsActive);
        Assert.Null(screen.Outcome);
    }

    // ------------------------------------------------------------------ kept in the story and the save

    [Fact]
    public void APhraseIsKeptInTheStoryAndTheSave()
    {
        var story = new StoryState();
        Assert.Null(story.PhraseOf(StoryState.InterviewPhrase));
        int revision = story.Revision;
        story.SetPhrase(StoryState.InterviewPhrase, HappyTogether);
        Assert.True(story.Revision > revision);
        Assert.Equal(HappyTogether, story.PhraseOf(StoryState.InterviewPhrase));
        // The same phrase again changes nothing
        revision = story.Revision;
        story.SetPhrase(StoryState.InterviewPhrase, HappyTogether.ToList());
        Assert.Equal(revision, story.Revision);

        // Written into a save and read back
        var json = SaveManager.Write(new SaveData { StoryPhrases = story.PhrasesToSave() });
        var read = SaveManager.Read(json)!;
        var again = new StoryState();
        again.RestorePhrases(read.StoryPhrases);
        Assert.Equal(HappyTogether, again.PhraseOf(StoryState.InterviewPhrase));

        // A save from before phrases were kept has none
        var old = new StoryState();
        old.RestorePhrases(null);
        Assert.Empty(old.Phrases);
        // A new story forgets them
        story.Clear();
        Assert.Empty(story.Phrases);
    }

    // ------------------------------------------------------------------ the reporters, played

    private static OpeningTests.Game InJubilife()
    {
        var game = new OpeningTests.Game();
        game.Arrive("Sinnoh", 168, 754);
        game.Facing = Direction.Up;
        return game;
    }

    private static HeadlessScriptHost Interview(OpeningTests.Game game, params int[] answers)
    {
        foreach (int answer in answers) game.Answers.Enqueue(answer);
        return game.Talk("tv_reporter", Place);
    }

    [Fact]
    public void TheReporterAndHerCameramanStandBesideTheStationsDoor()
    {
        var game = InJubilife();
        var map = game.Maps["Sinnoh"];
        var reporter = game.Present("tv_reporter", Place)!;
        var cameraman = game.Present("tv_cameraman", Place)!;
        Assert.Equal((168, 753, "Interview"), (reporter.GridX, reporter.GridY, reporter.Script));
        Assert.Equal((169, 753, "Cameraman"), (cameraman.GridX, cameraman.GridY, cameraman.Script));
        // Nobody hides them, and they are spoken to from open ground south of them
        Assert.Null(reporter.HiddenBy);
        Assert.Null(cameraman.HiddenBy);
        foreach (var (x, y) in new[] { (168, 754), (169, 754) }) Assert.False(map.IsSolid(x, y), $"({x}, {y}) is shut");
        Assert.Contains(game.Talk("tv_cameraman", Place).Transcript, l => l.Speaker == "Cameraman");
    }

    [Fact]
    public void AnInterviewGivenIsKeptPutOnTheAirAndAskedOnceADay()
    {
        var game = InJubilife();
        var talk = Interview(game, 0);
        Assert.Single(talk.Asked);
        Assert.Contains($"open Phrase {StoryState.InterviewPhrase} {string.Join(" ", HappyTogether)}", talk.Log);
        Assert.Equal(HappyTogether, game.Story.PhraseOf(StoryState.InterviewPhrase));
        // She says the answer back, as it is said
        Assert.Contains(talk.Transcript, l => l.Speaker == "Reporter" && l.Text.StartsWith("\"happy together\"", StringComparison.Ordinal));
        Assert.True(game.Story.Has(OnAir));
        Assert.True(game.Story.Has(Daily));

        // The same day: no question and no picker, only her thanks
        talk = Interview(game);
        Assert.Empty(talk.Asked);
        Assert.DoesNotContain(talk.Log, l => l.StartsWith("open Phrase", StringComparison.Ordinal));
        Assert.Contains("Thanks again", talk.Transcript.Single().Text);

        // The next day she asks again, and a new answer takes the old one's place
        game.Story.ClearDaily();
        Assert.False(game.Story.Has(Daily));
        game.Phrase = new[] { "people:friends", "time:forever" };
        talk = Interview(game, 0);
        Assert.Equal(new[] { "people:friends", "time:forever" }, game.Story.PhraseOf(StoryState.InterviewPhrase));
        Assert.Contains(talk.Transcript, l => l.Text.StartsWith("\"friends forever\"", StringComparison.Ordinal));
    }

    [Fact]
    public void ARefusalOrAPickerBackedOutOfLeavesNothingOnTheAir()
    {
        // No to her question: no picker, nothing kept
        var game = InJubilife();
        var talk = Interview(game, 1);
        Assert.DoesNotContain(talk.Log, l => l.StartsWith("open Phrase", StringComparison.Ordinal));
        Assert.Contains("change your mind", talk.Transcript[^1].Text);
        Assert.Null(game.Story.PhraseOf(StoryState.InterviewPhrase));
        Assert.False(game.Story.Has(OnAir));
        Assert.False(game.Story.Has(Daily));

        // Yes, and then the picker backed out of: nothing kept, and she may be asked again the same day
        game.Phrase = null;
        talk = Interview(game, 0);
        Assert.Contains($"open Phrase {StoryState.InterviewPhrase} none", talk.Log);
        Assert.Contains("Lost for words", string.Join(" ", talk.Transcript.Select(l => l.Text)));
        Assert.Null(game.Story.PhraseOf(StoryState.InterviewPhrase));
        Assert.False(game.Story.Has(OnAir));
        Assert.False(game.Story.Has(Daily));
        Assert.Single(Interview(game, 1).Asked);
    }

    [Fact]
    public void TheInterviewIsOnTheFirstTelevisionLookedAtAfterItAndAmongTheProgrammesFromThen()
    {
        var game = InJubilife();
        Interview(game, 0);

        // The television at home: the interview first, the flag cleared as it is shown
        var tv = game.Play(ScriptLibrary.Default.Find("TV", "PlayerHouse")!);
        var lines = tv.Transcript.Select(l => l.Text).ToList();
        Assert.All(tv.Transcript, l => Assert.Equal("TV", l.Speaker));
        Assert.Equal(4, lines.Count);
        Assert.Contains("Jubilife Street Talk", lines[0]);
        Assert.Contains("in just two words: happy together!", lines[2]);
        Assert.False(game.Story.Has(OnAir));
        // Looked at again, the room's own programme
        Assert.DoesNotContain(game.Play(ScriptLibrary.Default.Find("TV", "PlayerHouse")!).Transcript, l => l.Text.Contains("Street Talk"));

        // Any other television says it first the same way (common.Television), with no die drawn
        game.Story.Set(OnAir);
        var anywhere = game.Play(ScriptLibrary.Default.In("common", "Television")!);
        Assert.Contains("in just two words: happy together!", anywhere.Transcript[2].Text);
        Assert.False(game.Story.Has(OnAir));

        // ... and after that it is one of the programmes a die may draw
        var facts = anywhere.Broadcast();
        Assert.Equal(HappyTogether, facts.Interview);
        Assert.Contains(Programme.Interview, Broadcasts.On(facts));
        Assert.Contains("happy together", string.Join(" ", Broadcasts.Lines(Programme.Interview, facts)));
        // Before any interview it is not on, and asking for it by name gives what the die picks
        var none = new BroadcastFacts();
        Assert.DoesNotContain(Programme.Interview, Broadcasts.On(none));
        Assert.Equal(Broadcasts.Compose(none, _ => 0), Broadcasts.Compose(none, _ => 0, "interview"));
    }

    // ------------------------------------------------------------------ the game itself

    [Fact]
    public void AnInterviewGivenInJubilifeCityIsHeardOnTheTelevisionAtHome()
    {
        string folder = Path.Combine(Path.GetTempPath(), "phrases-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(9);
            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            engine.PeopleStayPut = true;
            var game = engine.Drive;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
            game.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 12));
            game.State = GameState.Overworld;

            // Jubilife City: the reporter, spoken to from the south; yes to her question, and two words in the picker
            game.Place(MapDatabase.Get("Sinnoh"), 168, 754, Direction.Up);
            game.Interact();
            Assert.Equal("jubilife_city.Interview", game.ScriptNow);
            bool picked = false;
            var heard = Read(engine, () =>
            {
                Assert.Equal(StoryState.InterviewPhrase, game.Phrases.Name);
                // The picker's groups are this game's: the moves of the Turtwig on the team are among them
                Assert.NotEmpty(game.Phrases.Entry!.Groups.WordsOf(PhraseGroup.Moves));
                Assert.True(game.Phrases.Entry.Choose(0, "feelings:happy"));
                Assert.True(game.Phrases.Entry.Choose(1, "small:together"));
                game.Phrases.Confirm();
                picked = true;
            });
            Assert.True(picked);
            Assert.Equal(HappyTogether, game.Story.PhraseOf(StoryState.InterviewPhrase));
            Assert.True(game.Story.Has(OnAir));
            Assert.Contains(heard, l => l.Contains("happy together"));
            // The save keeps it
            Assert.Equal(HappyTogether, game.BuildSave().StoryPhrases[StoryState.InterviewPhrase]);

            // Home: the living room's television says it
            game.Place(MapDatabase.Get("PlayerHouse"), 6, 6, Direction.Up);
            game.Interact();
            Assert.Equal("PlayerHouse.TV", game.ScriptNow);
            heard = Read(engine, null);
            Assert.Contains(heard, l => l.Contains("Jubilife Street Talk"));
            Assert.Contains(heard, l => l.Contains("in just two words: happy together!"));
            Assert.False(game.Story.Has(OnAir));
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    // The lines read on with the confirm button until the script has ended and the field is back; the picker, where it
    // opens, is handed to the test. Every line the text box showed is given back
    private static List<string> Read(GameEngine engine, Action? atThePicker)
    {
        var heard = new List<string>();
        var game = engine.Drive;
        for (int f = 0; f < 60 * 60; f++)
        {
            string line = game.Dialogue.CurrentLine;
            if (line.Length > 0 && (heard.Count == 0 || heard[^1] != line)) heard.Add(line);
            if (game.State == GameState.Phrases)
            {
                Assert.NotNull(atThePicker);
                atThePicker!();
                InputManager.Feed(ActionFrame.None);
                engine.Update(1f / 60f);
                continue;
            }
            if (game.State == GameState.Overworld && !engine.ScriptRunning) return heard;
            InputManager.Feed(f % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
            engine.Update(1f / 60f);
        }
        Assert.Fail($"The script never ended; the game is in {engine.Drive.State}.");
        return heard;
    }
}
