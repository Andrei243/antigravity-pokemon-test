using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>Lockwork's medals, requests and march (plan 44 · SD5, the extras SD3 and SD4 owed).</summary>
public class LockworkExtrasTests
{
    [Fact]
    public void TheFirstReclaimAndTheThirtiethOpenedHeartAreMedals()
    {
        var medals = new Medals();
        Assert.Empty(medals.Check(new MedalFacts()));
        Assert.Equal(new[] { "heartsReclaimed.1" }, medals.Check(new MedalFacts { HeartsReclaimedCount = 1 }).Select(m => m.Id));
        Assert.Empty(medals.Check(new MedalFacts { HeartsReclaimedCount = 3, HeartsOpenedCount = 29 }));
        Assert.Equal(new[] { "heartsOpened.30" }, medals.Check(new MedalFacts { HeartsOpenedCount = 30 }).Select(m => m.Id));
    }

    [Fact]
    public void EachCellHasARequestThatOpensWithItsHallOfFameAndEndsWithItsAdmin()
    {
        foreach (string region in new[] { "Sinnoh", "Kanto", "Johto", "Hoenn", "Unova" })
        {
            var r = Requests.Find($"lockwork_{region.ToLowerInvariant()}")!;
            var story = new StoryState();
            Assert.Equal(RequestState.Unknown, Requests.State(r, story));
            story.Set($"{region}HallOfFame");
            Assert.Equal(RequestState.Open, Requests.State(r, story));
            story.Set($"FLAG_LOCKWORK_{region.ToUpperInvariant()}_ADMIN_BEATEN");
            if (region == "Unova")
            {
                Assert.Equal("head", Requests.Next(r, story)!.Name);
                story.Set("FLAG_LOCKWORK_MACHINE_STOPPED");
            }
            Assert.Equal(RequestState.Done, Requests.State(r, story));
        }
    }

    [Fact]
    public void ALockworkTrainerSpottingThePlayerBringsTheMarchInEveryRegion()
    {
        Assert.True(MusicLibrary.Exists("common/eye_lockwork"));
        foreach (string region in new[] { "Sinnoh", "Kanto", "Johto", "Hoenn", "Unova" })
            foreach (string cls in new[] { "Lockwork Agent", "Lockwork Admin", "Lockwork Head" })
                Assert.Equal("common/eye_lockwork", MusicDirector.EyeSong(cls, region, MusicLibrary.Exists));
    }
}
