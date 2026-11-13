using System;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Core.Autoplay;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 44 · SD3, SD4's owed walk: a game with no window, its region's Hall of Fame entered, walked by the playthrough
/// bot from the port to each of Lockwork's cells (the doorman spoken to, his warp down taken) and up to the cell's admin,
/// whose battle the bot fights and wins; Unova's admin through the tower's door on Castelia's avenue. Nothing is put
/// into the rooms by hand: the bot finds its way by the game's own steps.
/// </summary>
[Collection("Shared clocks and dice")]
public class HeartsWalkTests
{
    public static TheoryData<string> Cells => new() { "Sinnoh", "Kanto", "Johto", "Hoenn", "Unova" };

    [Theory]
    [MemberData(nameof(Cells))]
    public void TheBotWalksFromThePortToTheCellsAdminAndBeatsThem(string region)
    {
        string folder = Path.Combine(Path.GetTempPath(), "heartswalk-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(45);
            var engine = new GameEngine(new GameSettings { Muted = true, BattleStyle = BattleStyle.Set });
            engine.InitializeHeadless();
            engine.PeopleStayPut = true;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            var game = engine.Drive;
            StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
            game.Story.Set("SinnohHallOfFame");
            game.Story.Set($"{region}HallOfFame");
            game.Party.Clear();
            // A team the cells' admins can't stand against, never tiring
            foreach (string s in new[] { "Garchomp", "Metagross", "Gyarados", "Tyranitar", "Salamence", "Dragonite" }) game.Party.Add(new Pokemon(PokemonDatabase.Get(s)!, 250, new Random(1)));
            game.TrainersLookOnArrival = false;

            Leg[] legs;
            string admin;
            if (region == "Unova")
            {
                game.Place(MapDatabase.Get("CasteliaCity"), 54, 8, Direction.Up);
                legs = new[] { Leg.Enter("LockworkTower1F"), Leg.Talk("LockworkTower1F", "lockwork_admin") };
                admin = "lockwork_unova_admin";
            }
            else
            {
                var map = MapDatabase.Get(RegionDatabase.Get(region)!.OverworldMap!);

                game.State = GameState.Overworld;
                var doorman = map.Everyone.First(n => n.Key == "lockwork_doorman");
                var (x, y) = NearestOpen(map, doorman.GridX, doorman.GridY);
                game.Place(map, x, y, Direction.Up);
                string cell = "LockworkCell" + region;
                legs = new[] { Leg.Talk(map.Name, "lockwork_doorman", doorman.ScriptFile ?? map.ScriptFileAt(doorman.GridX, doorman.GridY)), Leg.Talk(cell, "lockwork_admin") };
                admin = $"lockwork_{region.ToLowerInvariant()}_admin";
            }
            game.State = GameState.Overworld;

            // A few frames first, so the story's Hall of Fame has opened the ways it opens (the doors are laid as the game updates)
            for (int f = 0; f < 5; f++) { InputManager.Feed(ActionFrame.None); engine.Update(1f / 60f); }
            var bot = new Autoplayer(engine, seed: 1) { Itinerary = new Itinerary(legs) };
            bot.Play(240);
            var report = bot.Report;
            bot.Release();
            Assert.True(bot.Itinerary!.Finished, report.ToMarkdown());
            Assert.True(report.LegsGivenUp.Count == 0, report.ToMarkdown());
            Assert.Empty(report.Exceptions);
            Assert.True(game.Story.HasDefeated(admin), $"{admin} wasn't beaten\n{report.ToMarkdown()}");
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            PlayerIdentity.InRegion(RegionDatabase.First);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    // A tile someone may stand on a few steps from the doorman, so the bot has a short walk to him
    private static (int X, int Y) NearestOpen(Map map, int x, int y)
    {
        for (int r = 2; r <= 8; r++)
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) continue;
                    if (map.IsWalkable(x + dx, y + dy) && map.GetNpcAt(x + dx, y + dy) == null) return (x + dx, y + dy);
                }
        throw new InvalidOperationException($"nowhere to stand near {x},{y} on {map.Name}");
    }
}
