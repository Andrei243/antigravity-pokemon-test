using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 13 · V24: the live feed (a file for an OBS browser source), the overlay that reads it, the LiveSplit splits file
/// and stream-safe. The feed is held against a JSON written into the test; nothing is written while nothing changes and
/// nothing at all with the row off; the file is replaced whole; the overlay names only fields the feed has; the splits
/// file is read back as XML; stream-safe hides the ID. All of it is files: no test here opens a socket, and the engine
/// code for it has none.
/// </summary>
[Collection("Shared clocks and dice")]
public class StreamKitTests
{
    private static FeedState Fixed() => new()
    {
        Updated = "2026-11-03T12:00:00Z",
        Place = "Oreburgh City",
        Region = "Sinnoh",
        Badges = new[] { "Coal" },
        Rules = new FeedRules("nuzlocke", new[] { "nuzlocke", "dupes" }),
        Party = new[] { new FeedMon("Turtwig", null, "Leafy", 14, 30, 41, "None", false), new FeedMon("Starly", null, null, 9, 0, 28, "Faint", true) },
        Graveyard = new[] { new FeedFallen("Bidoof", null, 5, "Route 201") },
        AreasMet = 4,
        Timer = new FeedTimer(1234.5, 1200, false, new FeedSplit("badge.sinnoh.Coal", "Coal Badge", 1000, 980)),
        Battle = new FeedBattle("trainer", "Youngster Tristan", new[] { new FeedMon("Turtwig", null, "Leafy", 14, 30, 41, "None", false) },
            new[] { new FeedMon("Zubat", null, null, 12, 20, 20, "None", false) })
    };

    // The JSON a reader gets for Fixed(), written here and not made by the code under test
    private const string ExpectedJson = """
        {
          "format": 1,
          "game": "Pokémon Platinum (a fan remake)",
          "updated": "2026-11-03T12:00:00Z",
          "place": "Oreburgh City",
          "region": "Sinnoh",
          "badges": [
            "Coal"
          ],
          "rules": {
            "preset": "nuzlocke",
            "on": [
              "nuzlocke",
              "dupes"
            ]
          },
          "party": [
            {
              "species": "Turtwig",
              "nickname": "Leafy",
              "level": 14,
              "hp": 30,
              "maxHp": 41,
              "status": "None",
              "shiny": false
            },
            {
              "species": "Starly",
              "level": 9,
              "hp": 0,
              "maxHp": 28,
              "status": "Faint",
              "shiny": true
            }
          ],
          "graveyard": [
            {
              "species": "Bidoof",
              "level": 5,
              "place": "Route 201"
            }
          ],
          "areasMet": 4,
          "timer": {
            "realSeconds": 1234.5,
            "gameSeconds": 1200,
            "finished": false,
            "last": {
              "id": "badge.sinnoh.Coal",
              "name": "Coal Badge",
              "realSeconds": 1000,
              "gameSeconds": 980
            }
          },
          "battle": {
            "kind": "trainer",
            "foe": "Youngster Tristan",
            "player": [
              {
                "species": "Turtwig",
                "nickname": "Leafy",
                "level": 14,
                "hp": 30,
                "maxHp": 41,
                "status": "None",
                "shiny": false
              }
            ],
            "enemy": [
              {
                "species": "Zubat",
                "level": 12,
                "hp": 20,
                "maxHp": 20,
                "status": "None",
                "shiny": false
              }
            ]
          }
        }
        """;

    private static string Folder()
    {
        string folder = Path.Combine(Path.GetTempPath(), "stream-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    [Fact]
    public void TheFeedForAFixedStateIsTheJsonTheTestWrites()
    {
        Assert.Equal(ExpectedJson.ReplaceLineEndings("\n"), LiveFeed.ToJson(Fixed()).ReplaceLineEndings("\n"));
    }

    [Fact]
    public void TheFeedCarriesNoIdAndNoPath()
    {
        string json = LiveFeed.ToJson(Fixed());
        foreach (string word in new[] { "trainerId", "TrainerId", "friend", "code", "personality", "C:\\", "/home/" })
            Assert.DoesNotContain(word, json, StringComparison.OrdinalIgnoreCase);
        // By its type, too: nothing of a FeedState is named for an identity
        var names = new[] { typeof(FeedState), typeof(FeedMon), typeof(FeedFallen), typeof(FeedTimer), typeof(FeedBattle) }
            .SelectMany(t => t.GetProperties().Select(p => p.Name));
        Assert.DoesNotContain(names, n => n.Contains("TrainerId") || n.Contains("Folder") || n.Contains("Path") || n.Contains("Code") || n.Contains("Friend"));
    }

    [Fact]
    public void NothingIsWrittenWhileNothingChangesAndAtMostFourTimesASecond()
    {
        string folder = Folder();
        try
        {
            string path = Path.Combine(folder, "state.json");
            var feed = new LiveFeed();
            var state = Fixed();
            Assert.True(feed.Offer(state, 0.0, path, timerRuns: false));
            Assert.True(File.Exists(path));
            // The same state, later and later: no write (the moment it was written and the timer's seconds are no change)
            for (double t = 0.3; t < 30; t += 0.1)
                Assert.False(feed.Offer(state with { Updated = "later", Timer = state.Timer! with { RealSeconds = 5000 + t } }, t, path, timerRuns: false));
            Assert.Equal(1, feed.Writes);

            // A change inside the gap waits for it; after it, one write
            var hurt = state with { Party = new[] { state.Party[0] with { Hp = 12 }, state.Party[1] } };
            Assert.True(feed.Offer(hurt, 30.0, path, false));
            Assert.Contains("\"hp\": 12", File.ReadAllText(path));
            var moved = hurt with { Place = "Route 202" };
            Assert.False(feed.Offer(moved, 30.1, path, false));
            Assert.True(feed.Offer(moved, 30.3, path, false));
            // Changes every frame for two seconds: at most four a second
            int before = feed.Writes;
            for (int f = 0; f < 120; f++)
                feed.Offer(hurt with { Place = "Route " + f }, 31 + f / 60.0, path, false);
            Assert.InRange(feed.Writes - before, 1, 8);

            // With the timer shown and running the file is refreshed once a second
            before = feed.Writes;
            for (int f = 0; f < 300; f++) feed.Offer(hurt with { Place = "Same" }, 40 + f / 60.0, path, timerRuns: true);
            Assert.InRange(feed.Writes - before, 4, 7);
        }
        finally { Directory.Delete(folder, true); }
    }

    [Fact]
    public void TheFileIsReplacedWholeBesideItsTemporaryOne()
    {
        string folder = Folder();
        try
        {
            string path = Path.Combine(folder, "stream", "state.json");
            Assert.True(LiveFeed.WriteWhole(path, "{\"a\":1}"));
            Assert.True(LiveFeed.WriteWhole(path, "{\"a\":2}"));
            Assert.Equal("{\"a\":2}", File.ReadAllText(path));
            // Nothing else is left beside it: the temporary file was moved over the old one
            Assert.Equal(new[] { path }, Directory.GetFiles(Path.GetDirectoryName(path)!));
        }
        finally { Directory.Delete(folder, true); }
    }

    [Fact]
    public void TheOverlayNamesOnlyFieldsTheFeedHas()
    {
        string html = File.ReadAllText(Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Assets", "stream", "overlay.html"));
        string Camel(string n) => char.ToLowerInvariant(n[0]) + n[1..];
        HashSet<string> Fields(params Type[] types) => types.SelectMany(t => t.GetProperties().Select(p => Camel(p.Name))).ToHashSet();
        var known = new Dictionary<string, HashSet<string>>
        {
            ["state"] = Fields(typeof(FeedState)),
            ["mon"] = Fields(typeof(FeedMon), typeof(FeedFallen)),
            ["fallen"] = Fields(typeof(FeedFallen)),
            ["timer"] = Fields(typeof(FeedTimer)),
            ["fight"] = Fields(typeof(FeedBattle)),
        };
        int seen = 0;
        foreach (Match m in Regex.Matches(html, @"\b(state|mon|fallen|timer|fight)\.(?!json\b)([A-Za-z_]\w*)"))
        {
            string owner = m.Groups[1].Value, field = m.Groups[2].Value;
            Assert.True(known[owner].Contains(field), $"The overlay reads {owner}.{field}, which the feed has no field for.");
            seen++;
        }
        Assert.True(seen > 20);
        // It reads the one file and opens nothing else
        Assert.Contains("\"state.json?\"", html);
        Assert.DoesNotContain("WebSocket", html);
        Assert.DoesNotContain("http://", html);
        Assert.DoesNotContain("https://", html);
    }

    [Fact]
    public void TheSplitsFileIsALiveSplitRunWithTheTakenSplitsInIt()
    {
        var timer = new RunTimer();
        timer.Splits.Add(new TimerSplit { Id = "badge.sinnoh.Coal", RealSeconds = 3725.5, GameSeconds = 3700 });
        string xml = LiveSplitFile.Of(timer, TimerCategory.Badges, "Test Game");
        var run = XDocument.Parse(xml).Root!;
        Assert.Equal("Run", run.Name.LocalName);
        Assert.Equal("1.7.0", run.Attribute("version")!.Value);
        Assert.Equal("Test Game", run.Element("GameName")!.Value);
        Assert.Equal("Badges", run.Element("CategoryName")!.Value);
        var segments = run.Element("Segments")!.Elements("Segment").ToList();
        // A segment for every point the category counts: the badges of every region and the Halls of Fame, no story turns
        Assert.Equal(RunTimer.Points.Count(p => p.CountsIn(TimerCategory.Badges)), segments.Count);
        Assert.DoesNotContain(segments, s => s.Element("Name")!.Value.Contains("Windworks"));
        var taken = segments.Where(s => s.Element("SplitTimes")!.Element("SplitTime")!.HasElements).ToList();
        var coal = Assert.Single(taken);
        Assert.Equal(RunTimer.PointOf("badge.sinnoh.Coal")!.Shown, coal.Element("Name")!.Value);
        Assert.Equal("01:02:05.5000000", coal.Element("SplitTimes")!.Element("SplitTime")!.Element("RealTime")!.Value);
        Assert.Equal("01:01:40.0000000", coal.Element("SplitTimes")!.Element("SplitTime")!.Element("GameTime")!.Value);
        // The Story category counts the story's turns as well
        Assert.True(XDocument.Parse(LiveSplitFile.Of(timer, TimerCategory.Story, "x")).Root!.Element("Segments")!.Elements("Segment").Count() > segments.Count);
        Assert.Equal("100:00:00.0000000", LiveSplitFile.Time(360000));
    }

    [Fact]
    public void StreamSafeHidesTheIdAndTheUsersFolderAndNothingChangesWithItOff()
    {
        Assert.False(StreamSafe.On);
        Assert.Equal("00042", TrainerCardScreen.FormatId(42));
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var book = new LogBook(() => DateTime.UnixEpoch);
        try
        {
            StreamSafe.Use(true);
            Assert.Equal("-----", TrainerCardScreen.FormatId(42));
            Assert.Equal("-----", StreamSafe.Id("00042"));
            book.Write("INFO", @"Saved to C:\Users\someone\AppData\Roaming\PokemonPlatinum\save1.json");
            Assert.DoesNotContain("someone", string.Join("\n", book.Tail()));
        }
        finally { StreamSafe.Use(false); }
        Assert.Equal("00042", TrainerCardScreen.FormatId(42));
        book.Write("INFO", @"Saved to C:\Users\someone\save2.json");
        Assert.Contains("someone", string.Join("\n", book.Tail()));
        _ = home;
    }

    [Fact]
    public void TheTwoRowsStandBeforeHelpWithTheirWordsInBothLanguagesAndOffByDefault()
    {
        var rows = Enum.GetValues<OptionRow>();
        // (FRIEND AS PARTNER, plan 13 · V20, stands between them and HELP)
        Assert.Equal(OptionRow.StreamFeed, rows[Array.IndexOf(rows, OptionRow.Help) - 7]);
        Assert.Equal(OptionRow.StreamSafe, rows[Array.IndexOf(rows, OptionRow.Help) - 6]);
        var fresh = new GameSettings();
        Assert.Equal(StreamFeedMode.Off, fresh.StreamFeed);
        Assert.False(fresh.StreamSafe);
        var s = new GameSettings();
        var seen = new List<StreamFeedMode>();
        for (int i = 0; i < 4; i++) { seen.Add(s.StreamFeed); OptionsScreen.Change(s, OptionRow.StreamFeed, 1); }
        Assert.Equal(new[] { StreamFeedMode.Off, StreamFeedMode.File, StreamFeedMode.FileAndSplits, StreamFeedMode.Off }, seen);
        OptionsScreen.Change(s, OptionRow.StreamSafe, 1);
        Assert.True(s.StreamSafe);
        foreach (var row in new[] { OptionRow.StreamFeed, OptionRow.StreamSafe })
            foreach (var mode in Enum.GetValues<StreamFeedMode>())
            {
                s.StreamFeed = mode;
                var (label, value, help) = OptionsScreen.Describe(s, row);
                Assert.False(string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(help));
            }
        // A safe-mode start turns the kit off for the run
        var launch = CommandLine.Parse(new[] { "--safe-mode" });
        var chosen = new GameSettings { StreamFeed = StreamFeedMode.FileAndSplits };
        launch.ApplyTo(chosen);
        Assert.Equal(StreamFeedMode.Off, chosen.StreamFeed);
    }

    // ------------------------------------------------------------------ in the game

    private const float Frame = 1f / 60f;

    private static void Run(GameEngine engine)
    {
        InputManager.Feed(ActionFrame.None);
        engine.Update(Frame);
    }

    [Theory]
    [InlineData(StreamFeedMode.Off)]
    [InlineData(StreamFeedMode.File)]
    [InlineData(StreamFeedMode.FileAndSplits)]
    public void TheGameWritesTheKitsFilesOnlyWithTheRowOn(StreamFeedMode mode)
    {
        string folder = Folder();
        var paths = SavePaths.Current;
        var fixedClock = WallClock.Fixed;
        SavePaths.Current = SavePaths.In(folder);
        try
        {
            WallClock.Fixed = new DateTime(2026, 11, 3, 12, 0, 0);
            var engine = new GameEngine(new GameSettings { Muted = true, StreamFeed = mode, RunTimer = TimerCategory.Badges });
            engine.InitializeHeadless();
            engine.PeopleStayPut = true;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            StoryMigration.PastTheOpening(engine.Drive.Story, ScriptLibrary.Default);
            var game = engine.Drive;
            game.State = GameState.Overworld;
            for (int f = 0; f < 60; f++) Run(engine);

            string stream = Path.Combine(folder, SavePaths.StreamName);
            string state = Path.Combine(stream, SavePaths.FeedFileName), splits = Path.Combine(stream, SavePaths.LiveSplitFileName);
            if (mode == StreamFeedMode.Off)
            {
                Assert.False(Directory.Exists(stream));
                return;
            }
            Assert.True(File.Exists(state));
            using (var doc = JsonDocument.Parse(File.ReadAllText(state)))
            {
                var root = doc.RootElement;
                Assert.Equal(1, root.GetProperty("format").GetInt32());
                Assert.Equal(JsonValueKind.Array, root.GetProperty("party").ValueKind);
                Assert.Equal(0, root.GetProperty("badges").GetArrayLength());
                Assert.False(root.TryGetProperty("battle", out _));
            }

            // A Badge changes what the feed holds: it is written again, and the splits file has the split
            game.Story.GiveBadge(RegionDatabase.Sinnoh, "Coal");
            for (int f = 0; f < 30; f++) Run(engine);
            Assert.Contains("\"Coal\"", File.ReadAllText(state));
            Assert.Equal(mode == StreamFeedMode.FileAndSplits, File.Exists(splits));
            if (mode == StreamFeedMode.FileAndSplits)
            {
                var run = XDocument.Parse(File.ReadAllText(splits)).Root!;
                Assert.Contains(run.Element("Segments")!.Elements("Segment"), s => s.Element("SplitTimes")!.Element("SplitTime")!.HasElements);
            }
            // No socket: the engine's source for the kit names none
            string kit = File.ReadAllText(Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Core", "GameEngine.Stream.cs"))
                + File.ReadAllText(Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Core", "LiveFeed.cs"))
                + File.ReadAllText(Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Models", "LiveSplitFile.cs"));
            Assert.DoesNotContain("System.Net", kit);
            Assert.DoesNotContain("TcpClient", kit);
        }
        finally
        {
            InputManager.Feed(null);
            Ruleset.Use(RulesPreset.Platinum);
            WallClock.Fixed = fixedClock;
            SavePaths.Current = paths;
            try { Directory.Delete(folder, true); } catch (IOException) { }
        }
    }
}
