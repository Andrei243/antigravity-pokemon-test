using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Starfall Street's scripts played headless on Paldea's open map (plan 34 · PD5): Cassiopeia's first call only after
/// the academy's welcome, a gate's grunt who turns the player away before it and starts the raid after it, a raid lost
/// putting the player back on the gate, a raid won bringing the boss out and the battle with the Starmobile, the five
/// bosses beaten counting Starfall's five and Cassiopeia's thanks after the fifth; and every scene of the street played
/// and skipped alike. The raid itself is the field's (<c>LetsGoTests</c>, <c>LetsGoGameTests</c>).
/// </summary>
public class StarfallStreetTests
{
    private static readonly Lazy<OpenMap> Open = new(() => OpenRegions.Load(RegionDatabase.Get(RegionDatabase.Paldea)!).Single());

    private sealed class Game
    {
        public readonly StoryState Story = new();
        public readonly Party Party = new();
        public readonly Inventory Bag = new();

        public Game(int storyAt = 7)
        {
            StoryMigration.BeginNewGame(Story, ScriptLibrary.Default);
            Story.SetVar("VAR_PALDEA_STORY", storyAt);
            Party.Add(new Pokemon(PokemonDatabase.Get("Fuecoco")!, 40, new Random(0)));
        }

        public HeadlessScriptHost Play(string script, bool hurry = false, params int[] answers)
        {
            var host = new HeadlessScriptHost(Story, Party, Bag)
            {
                OpenMap = Open.Value,
                OpenMapNamed = name => name == Open.Value.Name ? Open.Value : null,
                Region = RegionDatabase.Paldea
            };
            foreach (int a in answers) host.Answers.Enqueue(a);
            var runner = new ScriptRunner(ScriptLibrary.Default, host) { Impatient = hurry };
            runner.Start(ScriptLibrary.Default.Find(script, StarBases.File)!, null);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            return host;
        }
    }

    [Fact]
    public void TheStreetIsAnAreaOfPaldeaWithFiveGatesTheirYardsGruntsAndBosses()
    {
        var map = Open.Value;
        Assert.Contains(map.Areas, a => a.Key == StarBases.File);
        Assert.Equal("Paldea", OpenRegions.MapOfArea(StarBases.File));
        foreach (var b in StarBases.All)
        {
            var yard = map.Marker(b.Yard);
            Assert.NotNull(yard);
            Assert.NotNull(map.Marker(b.Gate));
            Assert.Equal(StarBases.File, map.AreaAt(yard!.Pos)?.Key);
            Assert.Contains(map.People, p => p.Key == b.Key + "_grunt" && p.Look == "star_grunt");
            var boss = map.People.Single(p => p.Key == b.Key + "_boss");
            Assert.Equal(b.Key, boss.Look);
            Assert.Equal($"FLAG_PALDEA_STAR_{b.Key.ToUpperInvariant()}_RAIDED", boss.ShownBy);
            Assert.NotNull(ScriptLibrary.Default.Find(b.WonScript, StarBases.File));
            Assert.NotNull(ScriptLibrary.Default.Find(b.LostScript, StarBases.File));
        }
        Assert.Contains(map.Triggers, t => t.Name == "starfall_first_call");
    }

    [Fact]
    public void CassiopeiaCallsOnlyAfterTheAcademysWelcome()
    {
        var early = new Game(storyAt: 6);
        early.Play("CassiopeiaCalls");
        Assert.Equal(0, early.Story.Var("VAR_PALDEA_STARFALL"));

        var game = new Game();
        var host = game.Play("CassiopeiaCalls");
        Assert.Equal(1, game.Story.Var("VAR_PALDEA_STARFALL"));
        Assert.Contains(host.Transcript, t => t.Speaker == "Cassiopeia");
        Assert.Contains("sound phone_ring", host.Log);
    }

    [Fact]
    public void AGatesGruntStartsTheRaidOnlyOnceTheStreetIsBegunAndAsked()
    {
        var game = new Game();
        var before = game.Play("GiacomoGate");
        Assert.Null(before.StarRaidBegun);
        game.Story.SetVar("VAR_PALDEA_STARFALL", 1);
        // Asked, and told no: nothing begins
        var no = game.Play("GiacomoGate", answers: 1);
        Assert.Null(no.StarRaidBegun);
        var yes = game.Play("GiacomoGate", answers: 0);
        Assert.Equal("giacomo", yes.StarRaidBegun);
        Assert.Equal("giacomo_gate", yes.OnMarker["player"]);
    }

    [Fact]
    public void ARaidLostPutsThePlayerBackOnTheGateAndARaidWonBringsTheBossAndTheStarmobile()
    {
        var game = new Game();
        game.Story.SetVar("VAR_PALDEA_STARFALL", 1);
        var lost = game.Play("GiacomoRaidLost");
        Assert.Equal("giacomo_gate", lost.OnMarker["player"]);
        Assert.False(game.Story.Has("FLAG_PALDEA_STAR_GIACOMO_RAIDED"));

        var won = game.Play("GiacomoRaidWon");
        Assert.True(game.Story.Has("FLAG_PALDEA_STAR_GIACOMO_RAIDED"));
        Assert.Contains(won.Log, l => l.Contains("paldea_star_giacomo"));
        Assert.True(game.Story.Has("FLAG_PALDEA_STAR_GIACOMO_BEATEN"));
        Assert.Equal(2, game.Story.Var("VAR_PALDEA_STARFALL"));
        Assert.Contains(won.Transcript, t => t.Speaker == "Cassiopeia");
    }

    [Fact]
    public void TheFiveBossesBeatenCountTheStreetsFiveAndCassiopeiaThanksThePlayer()
    {
        var game = new Game();
        game.Play("CassiopeiaCalls");
        HeadlessScriptHost? last = null;
        foreach (var b in StarBases.All) last = game.Play(b.WonScript);
        Assert.Equal(6, game.Story.Var("VAR_PALDEA_STARFALL"));
        Assert.All(StarBases.All, b => Assert.True(game.Story.Has($"FLAG_PALDEA_STAR_{b.Key.ToUpperInvariant()}_BEATEN")));
        Assert.Contains(last!.Transcript, t => t.Speaker == "Cassiopeia" && t.Text.Contains("fifth"));
    }

    [Fact]
    public void TheStreetsScenesPlayedAndSkippedLeaveTheSame()
    {
        foreach (string script in StarBases.All.Select(b => b.WonScript).Prepend("CassiopeiaCalls"))
        {
            var ends = new List<string>();
            foreach (bool hurry in new[] { false, true })
            {
                var game = new Game();
                if (script != "CassiopeiaCalls") game.Story.SetVar("VAR_PALDEA_STARFALL", 1);
                var host = game.Play(script, hurry);
                ends.Add(System.Text.Json.JsonSerializer.Serialize(game.Story.Snapshot()) + "|"
                    + string.Join(",", host.OnMarker.OrderBy(m => m.Key).Select(m => m.Key + "=" + m.Value)));
            }
            Assert.True(ends[0] == ends[1], $"{script} skipped leaves otherwise than played:\n{ends[0]}\n{ends[1]}");
        }
    }
}
