using System;
using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 41 · EG4: the Laurel Circuit. Its one level by its rule, a League's team lifted on a copy, its stamps only from a
/// finished region's Hall of Fame while one runs, the final's order by the regions finished, and a headless circuit of
/// Sinnoh and Kanto from the desk to the final's title.
/// </summary>
public class ChampionsCircuitTests
{
    private static StoryState Finished(params string[] regions)
    {
        var story = new StoryState();
        foreach (string r in regions) story.Set(RegionDatabase.Get(r)!.StoryCompleteFlag);
        return story;
    }

    [Fact]
    public void TheLevelIsTheFurthestRegionsPostGameAndFiveMoreForEachCircuitWon()
    {
        var story = Finished(RegionDatabase.Sinnoh, RegionDatabase.Kanto);
        int kanto = RegionDatabase.Get(RegionDatabase.Kanto)!.Levels!.Points[^1].To;
        var circuit = new CircuitState();
        Assert.Equal(kanto, ChampionsCircuit.Level(circuit, story, Ruleset.Modern));
        circuit.Won = 2;
        Assert.Equal(kanto + 2 * ChampionsCircuit.StepPerCircuit, ChampionsCircuit.Level(circuit, story, Ruleset.Modern));
        // never past the cap
        circuit.Won = 1000;
        Assert.Equal(Ruleset.Modern.LevelCap, ChampionsCircuit.Level(circuit, story, Ruleset.Modern));
    }

    [Fact]
    public void ACircuitIsBegunOnlyWithTwoRegionsFinished()
    {
        var circuit = new CircuitState();
        Assert.False(ChampionsCircuit.Begin(circuit, Finished(RegionDatabase.Sinnoh)));
        Assert.True(ChampionsCircuit.Begin(circuit, Finished(RegionDatabase.Sinnoh, RegionDatabase.Kanto)));
        Assert.True(circuit.Running);
        // and not twice
        Assert.False(ChampionsCircuit.Begin(circuit, Finished(RegionDatabase.Sinnoh, RegionDatabase.Kanto)));
    }

    [Fact]
    public void AStampComesOnlyFromAFinishedRegionsHallOfFameWhileOneRuns()
    {
        var story = Finished(RegionDatabase.Sinnoh, RegionDatabase.Kanto);
        var circuit = new CircuitState();
        Assert.False(ChampionsCircuit.Stamp(circuit, RegionDatabase.Sinnoh, story));
        ChampionsCircuit.Begin(circuit, story);
        Assert.False(ChampionsCircuit.Stamp(circuit, RegionDatabase.Johto, story));
        Assert.False(ChampionsCircuit.Stamp(circuit, RegionDatabase.Hub, story));
        Assert.True(ChampionsCircuit.Stamp(circuit, RegionDatabase.Sinnoh, story));
        Assert.False(ChampionsCircuit.Stamp(circuit, RegionDatabase.Sinnoh, story));
        Assert.False(ChampionsCircuit.Ready(circuit, story));
        Assert.True(ChampionsCircuit.Stamp(circuit, RegionDatabase.Kanto, story));
        Assert.True(ChampionsCircuit.Ready(circuit, story));
    }

    [Fact]
    public void TheFinalsOrderIsTheChainsByTheRegionsFinished()
    {
        var story = Finished(RegionDatabase.Hoenn, RegionDatabase.Sinnoh, RegionDatabase.Kanto);
        Assert.Equal(new[] { RegionDatabase.Sinnoh, RegionDatabase.Kanto, RegionDatabase.Hoenn }, ChampionsCircuit.FinalOrder(story).Select(r => r.Region));
        // every row names a region of the chain, a team of the data that is a League's, and a person of the court
        var court = GameDataFiles.Load<MapFile>(System.IO.Path.Combine(MapDatabase.Folder, "Hub_Arena.json"));
        foreach (var row in ChampionsCircuit.Rows)
        {
            Assert.NotNull(RegionDatabase.Get(row.Region));
            Assert.True(ChampionsCircuit.IsLeagueTeam(row.Team), row.Team);
            Assert.Contains(court.Npcs, n => n.Id == row.Person);
        }
    }

    [Fact]
    public void ALeaguesTeamIsLiftedOnACopyWithItsSpreadKept()
    {
        var record = TrainerDatabase.Get("elite_four_aaron")!;
        var aaron = new Trainer { Id = record.Id };
        TrainerDatabase.Fill(aaron, record);
        var levels = aaron.Party.Members.Select(p => p.Level).ToList();
        var lifted = ChampionsCircuit.Lift(aaron, 120);
        Assert.Equal(120, lifted.Party.Members.Max(p => p.Level));
        int by = 120 - levels.Max();
        Assert.Equal(levels.Select(l => l + by), lifted.Party.Members.Select(p => p.Level));
        // the trainer of the map keeps theirs
        Assert.Equal(levels, aaron.Party.Members.Select(p => p.Level));
    }

    [Fact]
    public void OnlyAFinishedRegionsLeagueIsLiftedAndOnlyWhileACircuitRuns()
    {
        var story = Finished(RegionDatabase.Sinnoh, RegionDatabase.Kanto);
        var circuit = new CircuitState();
        Assert.False(ChampionsCircuit.Lifts(circuit, "elite_four_aaron", story));
        ChampionsCircuit.Begin(circuit, story);
        Assert.True(ChampionsCircuit.Lifts(circuit, "elite_four_aaron", story));
        Assert.True(ChampionsCircuit.Lifts(circuit, "kanto_champion_beyond", story));
        Assert.False(ChampionsCircuit.Lifts(circuit, "johto_champion_lance", story));
        Assert.False(ChampionsCircuit.Lifts(circuit, "youngster_tristan", story));
    }

    [Fact]
    public void AHeadlessCircuitOfSinnohAndKantoRunsFromTheDeskToTheFinalsTitle()
    {
        var host = new HeadlessScriptHost();
        host.Party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, 100));
        host.Story.Set(RegionDatabase.Get(RegionDatabase.Sinnoh)!.StoryCompleteFlag);
        host.Story.Set(RegionDatabase.Get(RegionDatabase.Kanto)!.StoryCompleteFlag);

        Play(host, "Hub_HallOfChampions", "CircuitDesk");
        Assert.True(host.Circuit!.Running);

        // Each League won again enters its Hall of Fame, which stamps it
        host.Region = RegionDatabase.Sinnoh;
        host.EnterHallOfFame();
        host.Region = RegionDatabase.Kanto;
        host.EnterHallOfFame();
        Assert.True(ChampionsCircuit.Ready(host.Circuit, host.Story));

        host.Region = RegionDatabase.Hub;
        Play(host, "Hub_Arena", "Herald");
        Assert.False(host.Circuit.Running);
        Assert.Equal(1, host.Circuit.Won);
        Assert.Single(host.Circuit.Finals);
        Assert.Contains(host.Log, l => l.Contains("champion_cynthia_beyond"));
        Assert.Contains(host.Log, l => l.Contains("kanto_champion_beyond"));
        Assert.DoesNotContain(host.Log, l => l.Contains("johto_champion_lance_beyond"));

        // The state round-trips a save, and the next circuit meets them five levels higher
        var back = JsonSerializer.Deserialize<CircuitState>(JsonSerializer.Serialize(host.Circuit))!;
        Assert.Equal(1, back.Won);
        Assert.Single(back.Finals);
        int kanto = RegionDatabase.Get(RegionDatabase.Kanto)!.Levels!.Points[^1].To;
        Assert.Equal(kanto + ChampionsCircuit.StepPerCircuit, ChampionsCircuit.Level(back, host.Story, Ruleset.Modern));
    }

    [Fact]
    public void AnEmptyCircuitIsNotSaved()
    {
        Assert.True(new CircuitState().IsEmpty);
        Assert.Null(new SaveData().Circuit);
    }

    private static void Play(HeadlessScriptHost host, string file, string script)
    {
        var map = GameDataFiles.Load<MapFile>(System.IO.Path.Combine(MapDatabase.Folder, file + ".json")).ToMap();
        host.Map = map;
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        var subject = map.Everyone.FirstOrDefault(n => n.Script == script);
        runner.Start(ScriptLibrary.Default.Find(script, file)!, subject);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
    }
}
