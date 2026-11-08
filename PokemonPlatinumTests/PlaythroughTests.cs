using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Core.Autoplay;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 16 · T13: the end-to-end test. The playthrough bot (plan 16 · T7) plays a new game with no window from the
/// bedroom, on an itinerary of the story's own triggers and people (<see cref="Itineraries"/>), finding its own way
/// between them by the field's rules and playing every scene, question and battle as it comes, to the Coal Badge.
/// Both run with the suite (the whole way to Roark is about twelve minutes of game time and twenty seconds), and fail
/// on an exception, a softlock, a script gone wrong, a value out of range or a leg given up. <c>PLAYTHROUGH_SEED=n</c>
/// plays them on other dice (the nightly's).
/// </summary>
[Collection("Shared clocks and dice")]
public class PlaythroughTests
{
    private readonly ITestOutputHelper output;

    public PlaythroughTests(ITestOutputHelper output) => this.output = output;

    private static int Seed => int.TryParse(Environment.GetEnvironmentVariable("PLAYTHROUGH_SEED"), out int s) ? s : 12;

    [Fact]
    public void TheFirstChapterPlaysThroughFromTheBedroomWithNoWindow()
    {
        Headless.Play(engine =>
        {
            var game = engine.Drive;
            var bot = new Autoplayer(engine, seed: 1) { Itinerary = new Itinerary(Itineraries.FirstChapter) };
            Run(bot, 1800);
            var report = bot.Report;
            Assert.True(bot.Itinerary!.Finished, report.ToMarkdown());
            Assert.Empty(report.LegsGivenUp);
            Assert.True(report.Clean, report.ToMarkdown());
            Assert.True(game.Story.Has(StoryState.PokedexFlag), report.ToMarkdown());
            Assert.True(game.Story.Has(StoryState.RunningShoesFlag), report.ToMarkdown());
            Assert.Equal(5, game.Bag.GetQuantity(ItemDatabase.Get("Poké Ball")!));
        }, Seed);
    }

    [Fact]
    public void TwinleafToCoalBadge()
    {

        Headless.Play(engine =>
        {
            var game = engine.Drive;
            var bot = new Autoplayer(engine, seed: 1) { Itinerary = new Itinerary(Itineraries.TwinleafToCoalBadge) };
            Run(bot, 4 * 3600);
            var report = bot.Report;
            Assert.True(report.Clean, report.ToMarkdown());
            Assert.Empty(report.LegsGivenUp);
            Assert.True(game.Story.HasBadge("Sinnoh", "Coal"), report.ToMarkdown());
        }, Seed);
    }

    [Fact]
    public void KalossLastChaptersPlayThroughFromCoumarineToTheHallOfFame()
    {
        // Plan 23 · Z4: a Kalos game brought to where the Tower of Mastery leaves it (three Badges, in Coumarine City),
        // with a team strong enough for the League, played by the bot through every scene, Gym and battle to the Hall of Fame
        // (the team a few levels higher since Kalos's last places joined Kalos's Gyms and water: the dice the maps draw had
        // moved, and the bot lost to Diantha)
        Headless.Play(engine =>
        {
            var game = engine.Drive;
            var story = game.Story;
            story.ChooseStarter("Froakie");
            story.Set(StoryState.PokedexFlag);
            story.Set(StoryState.RunningShoesFlag);
            foreach (string badge in new[] { "Bug", "Cliff", "Rumble" }) story.GiveBadge(RegionDatabase.Kalos, badge);
            story.SetVar("VAR_KALOS_STORY", 22);
            story.SetVar("VAR_KALOS_LUMIOSE_ROADS", 1);
            foreach (var (species, level) in new[] { ("Greninja", 82), ("Talonflame", 80), ("Venusaur", 80), ("Lucario", 80), ("Garchomp", 80), ("Gardevoir", 80) })
                game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, level));
            game.Bag.AddItem(ItemDatabase.Get("Poké Ball")!, 10);
            // (a Repel that lasts the whole way: the wild Pokémon are all weaker than the lead, and only the story is tried here)
            game.Flow.EncounterAids.RepelSteps = 1_000_000;
            game.Place(MapDatabase.Get("Kalos"), 243, 474, Direction.Down);
            game.State = GameState.Overworld;
            var bot = new Autoplayer(engine, seed: 1) { Itinerary = new Itinerary(Itineraries.KalosLastChapters) };
            Run(bot, 6 * 3600);
            var report = bot.Report;
            Assert.True(report.Clean, report.ToMarkdown());
            Assert.Empty(report.LegsGivenUp);
            Assert.True(story.Has("KalosHallOfFame"), report.ToMarkdown());
            Assert.Equal(8, story.BadgeCount(RegionDatabase.Kalos));
        }, Seed, RegionDatabase.Kalos);
    }

    [Fact]
    public void AKalosGamePlaysWholeFromTheBedroomInVanivilleToTheHallOfFame()
    {
        // Plan 23 · Z4's "done when": the bot plays the opening, the middle chapters and the last ones in one itinerary.
        // Five strong Pokémon are on the team before the first Pokémon arrives (the starter is the sixth), so the Gyms
        // and the League are the story's to be got through and the bot has no training to do
        Headless.Play(engine =>
        {
            var game = engine.Drive;
            var story = game.Story;
            foreach (var (species, level) in new[] { ("Talonflame", 80), ("Venusaur", 80), ("Lucario", 80), ("Garchomp", 80), ("Gardevoir", 80) })
                game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, level));
            game.Bag.AddItem(ItemDatabase.Get("Poké Ball")!, 10);
            game.Flow.EncounterAids.RepelSteps = 1_000_000;
            var bot = new Autoplayer(engine, seed: 1) { Itinerary = new Itinerary(Itineraries.KalosWhole) };
            Run(bot, 10 * 3600);
            var report = bot.Report;
            Assert.True(report.Clean, report.ToMarkdown());
            Assert.Empty(report.LegsGivenUp);
            Assert.True(bot.Itinerary!.Finished, report.ToMarkdown());
            Assert.True(story.Has("KalosHallOfFame"), report.ToMarkdown());
            Assert.Equal(8, story.BadgeCount(RegionDatabase.Kalos));
        }, Seed, RegionDatabase.Kalos);
    }

    [Fact]
    public void TheWayIsFoundAcrossMapsAndAlongAMapWithNoBound()
    {
        // Across maps, by their warps: down the stairs and out of the house; into the mine and down a floor
        Assert.Equal("PlayerHouse", Itinerary.NextMap("PlayerHouse2F", "Sinnoh", MapDatabase.Get));
        Assert.Equal("OreburghMineB1F", Itinerary.NextMap("Sinnoh", "OreburghMineB2F", MapDatabase.Get));
        Assert.Null(Itinerary.NextMap("Sinnoh", "Sinnoh", MapDatabase.Get));

        // Along a map, further than the bot's own goals look: Twinleaf Town to Sandgem Town's Pokémon Center door,
        // every step one the field takes, ending on the door
        var sinnoh = MapDatabase.Get("Sinnoh");
        var door = sinnoh.Warps.First(w => w.TargetMap == "PokemonCenter");
        var steps = Policy.Route(sinnoh, 116, 886, new Walker(), (x, y) => (x, y) == (door.SourceX, door.SourceY), door.SourceX, door.SourceY);
        Assert.NotNull(steps);
        Assert.True(steps!.Count > Policy.Reach, $"{steps.Count} steps");
        int px = 116, py = 886;
        float h = sinnoh.HeightAt(px, py);
        foreach (var way in steps)
        {
            var step = FieldMovement.Step(sinnoh, px, py, way, new Walker() with { Height = h });
            Assert.True(step.Moves, $"blocked at {px},{py} going {way}: {step.Obstacle}");
            (px, py, h) = (step.X, step.Y, step.Height);
        }
        Assert.Equal((door.SourceX, door.SourceY), (px, py));

        // An itinerary goes leg by leg, keeping what it did and what it gave up
        var it = new Itinerary(new[] { Leg.Enter("PlayerHouse"), Leg.Reach("Sinnoh", 1, 1) });
        Assert.Equal(LegKind.Enter, it.Current!.Kind);
        it.Advance();
        it.Advance(givenUp: true);
        Assert.True(it.Finished);
        Assert.Equal(new[] { "Enter PlayerHouse" }, it.Done);
        Assert.Equal(new[] { "Reach Sinnoh 1,1" }, it.GivenUp);
    }

    // Plays until the itinerary is done (and the field is the player's again), the run stops or the time is up
    private void Run(Autoplayer bot, double seconds)
    {
        var started = DateTime.UtcNow;
        const float dt = 1f / 60f;
        for (double t = 0; t < seconds && !bot.Stopped; t += dt)
        {
            bot.Frame(dt);
            if (bot.Itinerary!.Finished && t > 1) break;
        }
        bot.Release();
        output.WriteLine($"{bot.Report.Seconds:0} s of game time in {(DateTime.UtcNow - started).TotalSeconds:0} s");
        output.WriteLine("team: " + string.Join(", ", bot.Team));
        output.WriteLine(bot.Report.ToMarkdown());
    }
}

/// <summary>A game with no window for the playthrough tests (plan 16 · T7, T13), begun in Sinnoh's bedroom in a save folder of its own.</summary>
internal static class Headless
{
    public static void Play(Action<GameEngine> play, int dice = 12, string region = "Sinnoh")
    {
        string folder = Path.Combine(Path.GetTempPath(), "playthrough-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(dice);
            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            engine.PeopleStayPut = true;
            engine.NewGameRegion = region;
            engine.StartNewGame();
            engine.Drive.State = GameState.Overworld;
            play(engine);
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            PlayerIdentity.InRegion(RegionDatabase.First);
            PlayerIdentity.SetRival(null);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    /// <summary>Past the first chapter as the harness takes it, with a lead strong enough for any grass of the open world's south.</summary>
    public static void PastTheOpening(GameEngine engine, int level = 30)
    {
        var game = engine.Drive;
        StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, level));
        game.Bag.AddItem(ItemDatabase.Get("Poké Ball")!, 10);
        game.Place(MapDatabase.Get("Sinnoh"), 116, 886, Direction.Down);
        game.TrainersLookOnArrival = false;
        game.State = GameState.Overworld;
    }
}
