using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Core.Autoplay;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 33 · GL6's "done when": a Galar game played by the bot from the bedroom in Postwick to the Hall of Fame, the
/// first itinerary that walks an open region and its grid rooms as one (<see cref="Itineraries.GalarWhole"/>): the open
/// map's triggers and people by their names, steered for along the navigation mesh, and the rooms' people by their keys,
/// through the open map's doors and the rooms' ways back out. Five strong Pokémon are on the team before the first
/// arrives, as Kalos's run has them, so the gyms and the Cup are the story's to be got through.
/// </summary>
[Collection("Shared clocks and dice")]
public class PlaythroughGalarTests
{
    private readonly ITestOutputHelper output;

    public PlaythroughGalarTests(ITestOutputHelper output) => this.output = output;

    private static int Seed => int.TryParse(Environment.GetEnvironmentVariable("PLAYTHROUGH_SEED"), out int s) ? s : 12;

    private static void Strengthen(GameEngine engine)
    {
        var game = engine.Drive;
        foreach (var (species, level) in new[] { ("Corviknight", 80), ("Dragapult", 80), ("Toxtricity", 80), ("Grimmsnarl", 80), ("Excadrill", 80) })
            game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, level));
        game.Bag.AddItem(ItemDatabase.Get("Poké Ball")!, 10);
        game.Flow.EncounterAids.RepelSteps = 1_000_000;
    }

    [Fact]
    public void GalarsFirstChapterPlaysFromTheBedroomToTheTrain()
    {
        Headless.Play(engine =>
        {
            var game = engine.Drive;
            Strengthen(engine);
            var bot = new Autoplayer(engine, seed: 1) { Itinerary = new Itinerary(Itineraries.GalarToTheTrain) };
            Run(bot, 3600);
            var report = bot.Report;
            Assert.True(report.Clean, report.ToMarkdown());
            Assert.Empty(report.LegsGivenUp);
            Assert.True(bot.Itinerary!.Finished, report.ToMarkdown());
            Assert.True(game.Story.Var("VAR_GALAR_STORY") >= 11, $"VAR_GALAR_STORY {game.Story.Var("VAR_GALAR_STORY")}\n{report.ToMarkdown()}");
            Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Dynamax Band")!));
        }, Seed, RegionDatabase.Galar);
    }

    [Fact]
    public void AGalarGamePlaysWholeFromTheBedroomInPostwickToTheHallOfFame()
    {
        Headless.Play(engine =>
        {
            var game = engine.Drive;
            Strengthen(engine);
            var bot = new Autoplayer(engine, seed: 1) { Itinerary = new Itinerary(Itineraries.GalarWhole) };
            Run(bot, 12 * 3600);
            var report = bot.Report;
            output.WriteLine($"ended on {game.Map.Name}, VAR_GALAR_STORY {game.Story.Var("VAR_GALAR_STORY")}, badges {game.Story.BadgeCount(RegionDatabase.Galar)}");
            Assert.True(report.Clean, report.ToMarkdown());
            Assert.Empty(report.LegsGivenUp);
            Assert.True(bot.Itinerary!.Finished, report.ToMarkdown());
            Assert.True(game.Story.Has("GalarHallOfFame"), report.ToMarkdown());
            Assert.Equal(8, game.Story.BadgeCount(RegionDatabase.Galar));
        }, Seed, RegionDatabase.Galar);
    }

    // Plays until the itinerary is done (and the field is the player's again), the run stops or the time is up
    private void Run(Autoplayer bot, double seconds)
    {
        var started = DateTime.UtcNow;
        const float dt = 1f / 60f;
        int lastLeg = -1;
        for (double t = 0; t < seconds && !bot.Stopped; t += dt)
        {
            bot.Frame(dt);
            if (bot.Itinerary!.Index != lastLeg)
            {
                lastLeg = bot.Itinerary.Index;
                output.WriteLine($"{t:0}s leg {lastLeg}: {bot.Itinerary.Current}");
            }
            if (bot.Itinerary.Finished && t > 1) break;
        }
        bot.Release();
        output.WriteLine($"{bot.Report.Seconds:0} s of game time in {(DateTime.UtcNow - started).TotalSeconds:0} s");
        output.WriteLine("team: " + string.Join(", ", bot.Team));
        output.WriteLine(bot.Report.ToMarkdown());
    }
}
