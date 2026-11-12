using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 41 · EG9: the gathering at Grayfall Keep, played headless from Looker's call to the show's end with Sinnoh and
/// Kanto finished: the bays of unfinished regions empty, every team of ours legal and lifted, the leaders' battle a day.
/// </summary>
public class GatheringTests
{
    private static readonly string[] Teams =
    {
        "mars_gathering", "jupiter_gathering", "cyrus_gathering", "archer_gathering", "ariana_gathering", "giovanni_gathering",
        "tabitha_gathering", "shelly_gathering", "maxie_gathering", "archie_gathering", "zinzolin_gathering", "ghetsis_gathering",
        "xerosic_gathering", "celosia_gathering", "lysandre_gathering", "ismene_gathering"
    };

    [Fact]
    public void EveryTeamIsOursWithNoLegendaryAndIsLiftedAsAGatheringTeam()
    {
        foreach (string id in Teams)
        {
            var record = TrainerDatabase.Get(id);
            Assert.NotNull(record);
            Assert.True(ChampionsCircuit.IsGatheringTeam(id), id);
            foreach (var m in record!.Party)
            {
                var species = PokemonDatabase.Get(m.Species);
                Assert.NotNull(species);
                Assert.False(species!.Legendary || species.Mythical, $"{id}: {m.Species}");
            }
        }
    }

    [Fact]
    public void ABaysPeopleStandOnlyWhereTheirRegionIsFinished()
    {
        var keep = Room();
        var story = new StoryState();
        story.Set(RegionDatabase.Get(RegionDatabase.Sinnoh)!.StoryCompleteFlag);
        keep.ApplyPresence(story.Has);
        Assert.Contains(keep.NPCs, n => n.Key == "cyrus");
        Assert.DoesNotContain(keep.NPCs, n => n.Key == "giovanni");
        Assert.DoesNotContain(keep.NPCs, n => n.Key == "lysandre");
    }

    [Fact]
    public void TheGatheringIsPlayedFromLookersCallToTheShowsEnd()
    {
        var host = new HeadlessScriptHost();
        host.Party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, 100));
        host.Story.Set(RegionDatabase.Get(RegionDatabase.Sinnoh)!.StoryCompleteFlag);
        host.Story.Set(RegionDatabase.Get(RegionDatabase.Kanto)!.StoryCompleteFlag);
        host.Story.Set("FLAG_AGENCY_MET");
        host.Circuit!.Won = 1;

        Play(host, "Hub_Office", "Looker", Office());
        Assert.True(host.Story.Has("FLAG_GATHERING_CALLED"));
        Assert.Contains(host.Log, l => l.Contains("Hub_Keep"));

        // Ismene waits until every leader of a finished region is beaten
        Play(host, "Hub_Keep", "Ismene", Room());
        Assert.False(host.Story.Has("FLAG_GATHERING_DONE"));
        foreach (string who in new[] { "Mars", "Jupiter", "Cyrus", "Archer", "Ariana", "Giovanni" }) Play(host, "Hub_Keep", who, Room());
        Play(host, "Hub_Keep", "Ismene", Room());
        Assert.True(host.Story.Has("FLAG_GATHERING_DONE"));
        Assert.False(host.Story.Has("FLAG_KEEP_ISMENE_AWAY"));
        Assert.Contains(host.Log, l => l.Contains("ismene_gathering"));

        // Afterwards a leader battles once a day
        Play(host, "Hub_Keep", "Cyrus", Room());
        Assert.True(host.Story.Has("FLAG_DAILY_KEEP_CYRUS"));
    }

    private static PokemonPlatinumEngine.Overworld.Map Room() =>
        GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, "Hub_Keep.json")).ToMap();

    private static PokemonPlatinumEngine.Overworld.Map Office() =>
        GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, "Hub_Office.json")).ToMap();

    private static void Play(HeadlessScriptHost host, string file, string script, PokemonPlatinumEngine.Overworld.Map map)
    {
        host.Map = map;
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        var subject = map.Everyone.FirstOrDefault(n => n.Script == script);
        runner.Start(ScriptLibrary.Default.Find(script, file)!, subject);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
    }
}
