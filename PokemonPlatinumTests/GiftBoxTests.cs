using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 40 · GE7: the gift box (Models/GiftBox.cs, Data/gifts.json, scripts/gifts.txt, common.Courier; rulings, "The gift
/// box"). A gift is due by date, by milestone and by words, and never twice; what holds but has no room waits; a pack's gift
/// is laid over the game's by id; the box's screen is driven without input; the Gift Runner and the questionnaire are played
/// headless.
/// </summary>
public class GiftBoxTests
{
    private static Gift G(string id) => GiftBox.All.Single(g => g.Id == id);

    private static GiftFacts On(int year, int month, int day, int caught = 0, double hours = 0, DateTime? started = null,
        string? champion = null, bool room = true, RunRules? run = null) => new()
    {
        Today = new DateTime(year, month, day),
        Caught = caught,
        HoursPlayed = hours,
        Started = started,
        HallOfFame = r => r == champion,
        TeamHasRoom = room,
        Run = run ?? RunRules.Normal
    };

    [Fact]
    public void TheGameHasTheTwelveGiftsOfTheSheetAndTheFestivalsEachWithAScriptAndWords()
    {
        string[] sheet =
        {
            "new_year", "laurel_sinnoh", "laurel_kanto", "laurel_johto", "laurel_hoenn", "laurel_unova", "registry_100", "registry_500",
            "registry_1000", "hour_keepers", "anniversary", "gift_day"
        };
        Assert.All(sheet, id => Assert.NotNull(GiftBox.Get(id)));
        Assert.Equal(GiftBox.All.Count, GiftBox.All.Select(g => g.Id).Distinct().Count());
        foreach (var gift in GiftBox.All)
        {
            Assert.True(ScriptLibrary.Default.Find(ScriptRunner.GiftScripts + "." + gift.Script) != null, $"{gift.Id}: no script {gift.Script} in gifts.txt");
            foreach (string part in new[] { "sender", "title", "card" })
                Assert.DoesNotContain("[", PokemonPlatinumEngine.Core.Text.Of($"giftBox.gift.{GiftBox.KeyOf(gift.Id)}.{part}"));
            // What a card shows is of the data
            switch (gift.Picture.Kind)
            {
                case "pokemon" or "egg": Assert.NotNull(PokemonDatabase.Get(gift.Picture.Name)); break;
                case "item": Assert.NotNull(ItemDatabase.Get(gift.Picture.Name)); break;
                case "backdrop": Assert.True(SuperContestDatabase.Backdrop(gift.Picture.Name) >= 0, gift.Picture.Name); break;
                case "seal": Assert.True(SealDatabase.Seal(gift.Picture.Name) > 0, gift.Picture.Name); break;
                default: Assert.Fail($"{gift.Id}: no picture of kind {gift.Picture.Kind}"); break;
            }
        }
        // Every festival has its gift
        Assert.All(Festivals.All, f => Assert.Contains(GiftBox.All, g => g.When.Festival == f.Id || f.Id == "new_year" && g.Id == "new_year"));
    }

    [Fact]
    public void AGiftByDateWaitsOnItsDayOnceAYear()
    {
        var box = new GiftBox();
        var newYear = G("new_year");
        Assert.Contains(newYear, box.Due(On(2027, 1, 1)));
        Assert.DoesNotContain(newYear, box.Due(On(2027, 1, 2)));
        Assert.DoesNotContain(newYear, box.Due(On(2026, 12, 31)));
        box.Take(newYear, new DateTime(2027, 1, 1));
        Assert.DoesNotContain(newYear, box.Due(On(2027, 1, 1)));
        // The next year it waits again
        Assert.Contains(newYear, box.Due(On(2028, 1, 1)));
        // A festival's: Christmas from the 24th to the 26th
        var christmas = G("christmas");
        Assert.Contains(christmas, box.Due(On(2026, 12, 25)));
        Assert.DoesNotContain(christmas, box.Due(On(2026, 12, 27)));
    }

    [Fact]
    public void AGiftByMilestoneWaitsOnceAndWaitsForRoomOrTheRun()
    {
        var box = new GiftBox();
        var gible = G("laurel_sinnoh");
        Assert.DoesNotContain(gible, box.Due(On(2026, 6, 1)));
        Assert.Contains(gible, box.Due(On(2026, 6, 1, champion: "Sinnoh")));
        // No room on the team: it waits, and the box says so
        Assert.DoesNotContain(gible, box.Due(On(2026, 6, 1, champion: "Sinnoh", room: false)));
        Assert.Contains(gible, box.Waiting(On(2026, 6, 1, champion: "Sinnoh", room: false)));
        // A run that keeps the team to one type it isn't: it waits too
        var fire = RunRules.Normal with { Monotype = PokemonType.Fire };
        Assert.Contains(gible, box.Waiting(On(2026, 6, 1, champion: "Sinnoh", run: fire)));
        box.Take(gible, new DateTime(2026, 6, 1));
        Assert.DoesNotContain(gible, box.Due(On(2027, 6, 1, champion: "Sinnoh")));

        // Counts: caught and hours
        Assert.DoesNotContain(G("registry_100"), box.Due(On(2026, 6, 1, caught: 99)));
        Assert.Contains(G("registry_100"), box.Due(On(2026, 6, 1, caught: 100)));
        Assert.Contains(G("hour_keepers"), box.Due(On(2026, 6, 1, hours: 50)));
        Assert.DoesNotContain(G("hour_keepers"), box.Due(On(2026, 6, 1, hours: 49.9)));

        // The save's anniversary, once for each year since the game began
        var anniversary = G("anniversary");
        var started = new DateTime(2025, 3, 10);
        Assert.DoesNotContain(anniversary, box.Due(On(2026, 3, 9, started: started)));
        Assert.Contains(anniversary, box.Due(On(2026, 3, 10, started: started)));
        box.Take(anniversary, new DateTime(2026, 3, 10));
        Assert.DoesNotContain(anniversary, box.Due(On(2026, 12, 1, started: started)));
        Assert.Contains(anniversary, box.Due(On(2027, 3, 10, started: started)));
    }

    [Fact]
    public void AGiftByWordsWaitsOnceItsWordsAreToldInEitherOrderAndNeverTwice()
    {
        var box = new GiftBox();
        var facts = On(2026, 6, 1);
        var pichu = G("gift_day");
        Assert.DoesNotContain(pichu, box.Due(facts));
        Assert.Empty(box.Tell(new[] { "feelings:happy", "small:together" }, facts));
        Assert.Equal(new[] { pichu }, box.Tell(new[] { "small:clockwork", "small:sparrow" }, facts));
        Assert.Contains(pichu, box.Due(facts));
        box.Take(pichu, facts.Today);
        Assert.DoesNotContain(pichu, box.Due(facts));
        Assert.Empty(box.Tell(new[] { "small:sparrow", "small:clockwork" }, facts));

        // The box saved and read again is the same box
        var again = new GiftBox();
        again.Load(box.ToSave());
        Assert.Equal(box.TakenOn(pichu.Id), again.TakenOn(pichu.Id));
        Assert.Null(new GiftBox().ToSave());
    }

    [Fact]
    public void EveryPairOfWordsOfTheGamesGiftsIsSaidByALineInTheWorld()
    {
        // Gather every line a script says, then hold each pair of words to one that says both
        var lines = ScriptLibrary.Default.All.SelectMany(s => s.Everything()).SelectMany(i => i.Lines).ToList();
        foreach (var gift in GiftBox.All.Where(g => g.When.Words != null))
        {
            var words = gift.When.Words!.Select(PhraseGroups.Shown).ToList();
            Assert.All(words, w => Assert.NotEmpty(w));
            Assert.True(lines.Any(l => words.All(w => l.Contains(w, StringComparison.OrdinalIgnoreCase))),
                $"{gift.Id}: no line in the world says both {string.Join(" and ", words)}");
        }
    }

    [Fact]
    public void APacksGiftIsLaidOverTheGamesById()
    {
        string root = Path.Combine(Path.GetTempPath(), "gifts-pack-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, GiftBox.FileName),
                "{ \"gifts\": [ { \"id\": \"hour_keepers\", \"when\": { \"hours\": 10 }, \"picture\": { \"kind\": \"item\", \"name\": \"PP Up\" }, \"script\": \"HourKeepers\" }," +
                " { \"id\": \"packs_own\", \"when\": { \"dates\": [ \"05-04\" ] }, \"picture\": { \"kind\": \"item\", \"name\": \"Potion\" }, \"script\": \"Valentine\" } ] }");
            var gifts = GiftBox.Read(new[] { new DataLayer(GameDataFiles.Directory, null), new DataLayer(root, null) });
            Assert.Equal(GiftBox.All.Count + 1, gifts.Count);
            Assert.Equal(10, gifts.Single(g => g.Id == "hour_keepers").When.Hours);
            Assert.Equal("packs_own", gifts[^1].Id);
            // A pack's file is read strictly: a field nobody has stops the load
            File.WriteAllText(Path.Combine(root, GiftBox.FileName), "{ \"gifts\": [ { \"id\": \"x\", \"when\": {}, \"picture\": { \"kind\": \"item\", \"name\": \"Potion\" }, \"script\": \"X\", \"colour\": 1 } ] }");
            Assert.Throws<InvalidDataException>(() => GiftBox.Read(new[] { new DataLayer(root, null) }));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void TheBoxsScreenChoosesAGiftOrTheWordsWithoutInput()
    {
        var box = new GiftBox();
        var facts = On(2026, 12, 25, caught: 120, champion: "Sinnoh");
        var screen = new GiftBoxScreen();
        screen.Open(box, facts);
        Assert.Equal(box.Due(facts).Count, screen.Due.Count);
        Assert.True(screen.Due.Count >= 3);
        screen.Move(1, 0);
        screen.Confirm();
        Assert.False(screen.IsActive);
        Assert.Equal(2, screen.Outcome);

        screen.Open(box, facts);
        screen.ChangeTab(-1);
        Assert.Equal(GiftBoxScreen.GiftTab.Words, screen.Tab);
        screen.Confirm();
        Assert.Equal(GiftBoxAnswers.Words, screen.Outcome);

        // The album has nothing to choose
        screen.Open(box, facts);
        screen.ChangeTab(1);
        screen.Confirm();
        Assert.True(screen.IsActive);
        screen.Cancel();
        Assert.Equal(0, screen.Outcome);
    }

    // ------------------------------------------------------------------ played headless

    private static (HeadlessScriptHost Host, ScriptRunner Runner) Play(string script, StoryState story, Party party, GiftFacts facts, Action<HeadlessScriptHost>? ready = null, string? file = null)
    {
        var host = new HeadlessScriptHost(story, party) { GiftFacts = facts };
        ready?.Invoke(host);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, file)!, new NPC { Name = "Gift Runner" });
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return (host, runner);
    }

    private static Party Team(params string[] species)
    {
        var party = new Party();
        foreach (string s in species) party.Add(new Pokemon(PokemonDatabase.Get(s)!, 30, new Random(party.Count + 1)));
        return party;
    }

    [Fact]
    public void TheQuestionnaireOpensTheBoxOnSparrowAndClockwork()
    {
        var story = new StoryState();
        var (wrong, _) = Play("Questionnaire", story, Team("Turtwig"), On(2026, 6, 1), file: "JubilifeTV1F");
        Assert.False(story.Has(GiftBox.OpenFlag));
        Assert.Contains(wrong.Transcript, l => l.Text.Contains("Not what I had in mind", StringComparison.Ordinal));
        // The test's phrase for both questions: the right pair
        Play("Questionnaire", story, Team("Turtwig"), On(2026, 6, 1), h => h.PhraseChoice = new[] { "small:sparrow", "small:clockwork" }, "JubilifeTV1F");
        Assert.True(story.Has(GiftBox.OpenFlag));
    }

    [Fact]
    public void AGiftRunnerHandsOverAGiftByMilestoneByDateAndByWordsEachOnce()
    {
        var story = new StoryState();
        var party = Team("Turtwig");
        // Before the box is open: nothing, and the two words said
        var (closed, _) = Play("Courier", story, party, On(2026, 6, 1, champion: "Sinnoh"));
        Assert.Single(party.Members);
        Assert.Contains(closed.Transcript, l => l.Text.Contains("sparrow", StringComparison.Ordinal) && l.Text.Contains("clockwork", StringComparison.Ordinal));

        story.Set(GiftBox.OpenFlag);
        var box = new GiftBox();
        // By milestone: Sinnoh's Hall of Fame brings the Gible Egg in a Cherish Ball with its ribbon
        Play("Courier", story, party, On(2026, 6, 1, champion: "Sinnoh"), h => h.Gifts = box);
        var egg = party.Members.Last();
        Assert.True(egg.IsEgg);
        Assert.Equal("Gible", egg.Species.Name);
        Assert.Equal("Cherish Ball", egg.Ball);
        Assert.Contains("RIBBON_CHAMPION_HEARTH", egg.Ribbons);
        // ...once
        Play("Courier", story, party, On(2026, 6, 1, champion: "Sinnoh"), h => h.Gifts = box);
        Assert.Equal(2, party.Count);

        // By date: on Valentine's Day, the Fairy Gem with a card from nobody
        var bag = new Inventory();
        // (the clock keeps the festivals' flags in the game, KeepTheClock)
        Festivals.Keep(story, new DateTime(2027, 2, 14));
        var host = new HeadlessScriptHost(story, party, bag) { GiftFacts = On(2027, 2, 14, champion: "Sinnoh"), Gifts = box };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("Courier")!, new NPC { Name = "Gift Runner" });
        runner.RunToEnd();
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Fairy Gem")!));
        Assert.Contains(host.Transcript, l => l.Text.Contains("wouldn't leave a name", StringComparison.Ordinal));

        // By words: told at the runner, and taken
        Play("Courier", story, party, On(2027, 3, 1), h =>
        {
            h.Gifts = box;
            h.PhraseChoice = new[] { "small:sparrow", "small:clockwork" };
        });
        var pichu = party.Members.Last();
        Assert.Equal("Pichu", pichu.Species.Name);
        Assert.Equal("Pichu-Spiky-Eared", pichu.Form);
        Assert.Equal("Surf", pichu.Moves[0].Name);
        Assert.Contains("RIBBON_GIFT_DAY", pichu.Ribbons);
        Assert.Equal("the Gift Desk", pichu.OriginalTrainer?.Name);
        Assert.Equal(new DateTime(2027, 3, 1), box.TakenOn("gift_day").Single());
    }

    [Fact]
    public void TheEventPokemonKnowOnlyWhatTheirSpeciesMayAndTheRibbonsArePageWorthy()
    {
        var story = new StoryState { };
        story.Set(GiftBox.OpenFlag);
        foreach (string script in new[] { "GiftDay", "Registry1000" })
        {
            var party = Team("Turtwig");
            var host = new HeadlessScriptHost(story, party);
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find(ScriptRunner.GiftScripts + "." + script)!);
            runner.RunToEnd();
            var given = party.Members.Last();
            Assert.Empty(Legality.Unlearnable(given));
            Assert.Equal("Cherish Ball", given.Ball);
            Assert.All(given.Ribbons, r => Assert.Null(RibbonRules.WaitsFor(r)));
            Assert.NotEmpty(RibbonRules.Of(given));
        }
    }
}
