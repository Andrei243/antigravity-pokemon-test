using System.Text.Json;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>Plan 13 · V9: the medals, their place in the save, the option that tells of them and the Trainer Card's page.</summary>
public class MedalTests
{
    [Fact]
    public void ThereAreAHundredAndTwentyInSixCategoriesEachOnTheCardsPage()
    {
        Assert.Equal(120, Medals.All.Count);
        Assert.Equal(Medals.All.Count, Medals.All.Select(m => m.Id).Distinct().Count());
        foreach (var category in Enum.GetValues<MedalCategory>())
        {
            int n = Medals.In(category).Count();
            Assert.InRange(n, 1, Medals.MostInACategory);
        }
        Assert.Equal(6, Enum.GetValues<MedalCategory>().Length);
    }

    [Fact]
    public void EveryMedalStandsOnACounterOrAFactThatIsNamed()
    {
        var keys = Statistics.All.Select(k => k.Key).Concat(MedalFacts.Others).ToHashSet();
        foreach (var medal in Medals.All)
        {
            Assert.Contains(medal.Source, keys);
            Assert.True(medal.Goal > 0, medal.Id);
        }
        // and each source's goals rise
        foreach (var group in Medals.All.GroupBy(m => m.Source))
            Assert.Equal(group.Select(m => m.Goal).OrderBy(g => g), group.Select(m => m.Goal));
    }

    /// <summary>Facts with one source at a value and everything else at nothing.</summary>
    private static MedalFacts At(string source, long value)
    {
        var counts = new Statistics();
        int seen = 0, caught = 0, badges = 0, champions = 0;
        switch (source)
        {
            case MedalFacts.DexSeen: seen = (int)value; break;
            case MedalFacts.DexCaught: caught = (int)value; break;
            case MedalFacts.Badges: badges = (int)value; break;
            case MedalFacts.Champions: champions = (int)value; break;
            default: counts.Count(source, value); break;
        }
        return new MedalFacts { Counts = counts, Seen = seen, Caught = caught, BadgeCount = badges, ChampionCount = champions };
    }

    [Fact]
    public void EveryMedalIsReachedByItsCountersAndNotOneShort()
    {
        foreach (var medal in Medals.All)
        {
            var under = new Medals();
            Assert.DoesNotContain(medal, under.Check(At(medal.Source, medal.Goal - 1)));
            Assert.Equal(medal.Goal - 1, medal.Progress(At(medal.Source, medal.Goal - 1)));

            var at = new Medals();
            var won = at.Check(At(medal.Source, medal.Goal));
            Assert.Contains(medal, won);
            Assert.True(at.Has(medal));
            // Everything of its own source up to its goal comes with it, nothing of any other
            Assert.All(won, m => Assert.Equal(medal.Source, m.Source));
            Assert.Equal(Medals.All.Count(m => m.Source == medal.Source && m.Goal <= medal.Goal), won.Count);
        }
    }

    [Fact]
    public void NoMedalIsWonTwice()
    {
        var medals = new Medals();
        var counts = new Statistics();
        counts.Count(Statistics.Catches);
        var first = medals.Check(new MedalFacts { Counts = counts });
        Assert.Single(first);
        Assert.Equal("caught.1", first[0].Id);
        Assert.Empty(medals.Check(new MedalFacts { Counts = counts }));
        counts.Count(Statistics.Catches, 4);
        Assert.Equal(new[] { "caught.5" }, medals.Check(new MedalFacts { Counts = counts }).Select(m => m.Id));
        Assert.Empty(medals.Check(new MedalFacts { Counts = counts }));
        Assert.Equal(2, medals.Count);
    }

    [Fact]
    public void TheMedalsSurviveARoundTripOfTheSaveAndANewGameWritesNone()
    {
        Assert.Null(new Medals().ToSaved());
        Assert.Null(JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(new SaveData()))!.Medals);
        Assert.DoesNotContain("\"Medals\"", JsonSerializer.Serialize(new SaveData()));

        var medals = new Medals();
        medals.Check(At(Statistics.StepsFoot, 1000));
        string json = JsonSerializer.Serialize(new SaveData { Medals = medals.ToSaved() });
        var back = Medals.From(JsonSerializer.Deserialize<SaveData>(json)!.Medals);
        Assert.Equal(3, back.Count);
        Assert.True(back.Has("stepsFoot.1000"));
        // A medal a later game names is passed over
        Assert.Equal(1, Medals.From(new[] { "stepsFoot.100", "teleports.1" }).Count);
    }

    [Fact]
    public void TheOptionIsOffUnlessChosenAndItsRowStandsWithTheSettings()
    {
        Assert.False(new GameSettings().MedalNotices);
        var rows = OptionsScreen.AllRows.ToList();
        Assert.Equal(rows.IndexOf(OptionRow.ReduceMotion) + 1, rows.IndexOf(OptionRow.MedalNotices));
        var s = new GameSettings();
        OptionsScreen.Change(s, OptionRow.MedalNotices, 1);
        Assert.True(s.MedalNotices);
        Assert.True(JsonSerializer.Deserialize<GameSettings>(JsonSerializer.Serialize(s))!.MedalNotices);
    }

    [Fact]
    public void TheMedalsFanfareIsOurOwnSong()
    {
        Assert.Equal("common/fanfare_medal", MusicDirector.Resolve(MusicRole.FanfareMedal, "Sinnoh", MusicLibrary.Exists));
    }

    [Fact]
    public void TheCardHasAMedalsPageOnceAnythingIsCounted()
    {
        var card = new TrainerCardScreen();
        card.Open(hasStats: true, hasMedals: true);
        Assert.True(card.GoTo(CardPage.Medals));
        Assert.Equal(CardPage.Medals, card.Page);
        Assert.True(card.ChangeMedalTab(-1));
        Assert.Equal(Enum.GetValues<MedalCategory>().Length - 1, card.MedalTab);
        card.Open();
        Assert.False(card.GoTo(CardPage.Medals));
    }
}

/// <summary>Plan 13 · V9 in the game itself: the first catch wins a medal, kept in the save, told only with the option on.</summary>
[Collection("Shared clocks and dice")]
public class MedalGameTests
{
    private const float Frame = 1f / 60f;

    private static (GameEngine Engine, List<string> Heard) Game(bool notices)
    {
        var engine = new GameEngine(new GameSettings { Muted = true, MedalNotices = notices });
        engine.InitializeHeadless();
        engine.PeopleStayPut = true;
        engine.NewGameRegion = "Sinnoh";
        engine.StartNewGame();
        StoryMigration.PastTheOpening(engine.Drive.Story, ScriptLibrary.Default);
        engine.Drive.State = GameState.Overworld;
        var heard = AudioManager.Listen();
        for (int f = 0; f < 3; f++) Run(engine);
        return (engine, heard);
    }

    private static void Run(GameEngine engine)
    {
        InputManager.Feed(ActionFrame.None);
        engine.Update(Frame);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheFirstCatchWinsAMedalAndTheOptionDecidesWhetherItIsTold(bool notices)
    {
        string folder = Path.Combine(Path.GetTempPath(), "medals-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        SavePaths.Current = SavePaths.In(folder);
        try
        {
            var (engine, heard) = Game(notices);
            var game = engine.Drive;
            heard.Clear();
            Assert.False(game.Medals.Has("caught.1"));
            game.Statistics.CountBattle(PokemonPlatinumEngine.Battle.BattleResult.EnemyCaught, trainer: false);
            Run(engine);

            Assert.True(game.Medals.Has("caught.1"));
            Assert.Contains("caught.1", game.BuildSave().Medals!);
            Assert.Equal(notices, game.MedalNotice.Visible);
            Assert.Equal(notices, heard.Contains($"fanfare {MusicRole.FanfareMedal}"));
            if (notices) Assert.Equal("caught.1", game.MedalNotice.Medal!.Id);

            // Once, whatever comes after; the notice goes away by itself
            heard.Clear();
            for (int f = 0; f < 60 * 5; f++) Run(engine);
            Assert.False(game.MedalNotice.Visible);
            Assert.DoesNotContain($"fanfare {MusicRole.FanfareMedal}", heard);
        }
        finally
        {
            InputManager.Feed(null);
            AudioManager.StopListening();
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, true); } catch (IOException) { }
        }
    }
}
