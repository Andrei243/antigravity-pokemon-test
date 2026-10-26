using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 18 · W4b, L9 (<c>docs/mechanics/rulings.md</c>, "Rematches of a region gone past"): a region come back to keeps
/// the levels it was met at, but its rematch teams take the furthest Hall of Fame's levels, by that region's pairs.
/// The numbers were worked out by a script from the region files' pairs and the trainers' files.
/// </summary>
public class RematchLevelTests
{
    private static StoryState With(params string[] halls)
    {
        var story = new StoryState();
        foreach (var region in halls) story.Set(region + "HallOfFame");
        return story;
    }

    [Theory]
    [InlineData("champion_cynthia_rematch", true)]
    [InlineData("elite_four_aaron_rematch", true)]
    [InlineData("leader_roark_rematch", true)]
    [InlineData("youngster_tristan_rematch_1", true)]
    [InlineData("kanto_elite_four_lorelei_2", true)]
    [InlineData("kanto_champion_rematch_squirtle", true)]
    [InlineData("johto_bug_catcher_wade_2", true)]
    [InlineData("hoenn_sidney_rematch", true)]
    [InlineData("hoenn_wallace_rematch", true)]
    [InlineData("champion_cynthia", false)]
    [InlineData("kanto_elite_four_lorelei", false)]
    [InlineData("kanto_team_rocket_grunt_2", false)]
    [InlineData("johto_bug_catcher_wade", false)]
    [InlineData("elite_four_aaron_beyond", false)]
    [InlineData("champion_cynthia_megamax", false)]
    public void TheRematchTeamsAreKnownByTheirIdsAndRows(string id, bool rematch)
    {
        Assert.NotNull(TrainerDatabase.Get(id));
        Assert.Equal(rematch, TrainerDatabase.IsRematchTeam(id));
    }

    [Theory]
    [InlineData("champion_cynthia_rematch", "Sinnoh")]
    [InlineData("kanto_champion_rematch_squirtle", "Kanto")]
    [InlineData("johto_bug_catcher_wade_2", "Johto")]
    [InlineData("hoenn_wallace_rematch", "Hoenn")]
    public void ATeamBelongsToTheRegionItsIdNames(string id, string region) =>
        Assert.Equal(region, RematchLevels.RegionOfTeam(id).Id);

    [Fact]
    public void NothingMovesWhileNoHallOfFamePastTheRematchsRegionIsEntered()
    {
        Assert.Null(RematchLevels.For("champion_cynthia_rematch", With(), "Sinnoh"));
        Assert.Null(RematchLevels.For("champion_cynthia_rematch", With("Sinnoh"), "Sinnoh"));
        Assert.Null(RematchLevels.For("kanto_champion_rematch_squirtle", With("Sinnoh", "Kanto"), "Sinnoh"));
        // A region beyond the furthest Hall of Fame keeps its own crossed levels
        Assert.Null(RematchLevels.For("hoenn_wallace_rematch", With("Sinnoh", "Kanto", "Johto"), "Sinnoh"));
        // A trainer who is no rematch keeps the levels first met
        Assert.Null(RematchLevels.For("champion_cynthia", With("Sinnoh", "Kanto", "Johto", "Hoenn", "Unova"), "Sinnoh"));
        // As do the teams beyond the League and the Mega Dynamax teams
        Assert.Null(RematchLevels.For("champion_cynthia_beyond", With("Sinnoh", "Kanto", "Johto", "Hoenn", "Unova"), "Sinnoh"));
        Assert.Null(RematchLevels.For("champion_cynthia_megamax", With("Sinnoh", "Kanto", "Johto", "Hoenn", "Unova"), "Sinnoh"));
        // A game begun in Kanto (a development switch, L10) is left alone where Kanto is the furthest
        Assert.Null(RematchLevels.For("champion_cynthia_rematch", With("Kanto"), "Kanto"));
    }

    [Theory]
    // Cynthia's rematch, her first Pokémon of 74 and her ace of 78
    [InlineData("champion_cynthia_rematch", "Kanto", 74, 119)]
    [InlineData("champion_cynthia_rematch", "Kanto", 78, 124)]
    [InlineData("champion_cynthia_rematch", "Johto", 78, 151)]
    [InlineData("champion_cynthia_rematch", "Hoenn", 78, 183)]
    [InlineData("champion_cynthia_rematch", "Unova", 78, 201)]
    // A Vs. Seeker's first row, Route 202's Tristan of 24
    [InlineData("youngster_tristan_rematch_1", "Kanto", 24, 77)]
    [InlineData("youngster_tristan_rematch_1", "Unova", 24, 171)]
    // Kanto's Champion's rematch's ace of 75, at Kanto's post-game (120) while Kanto is the furthest; Johto's 149
    [InlineData("kanto_champion_rematch_squirtle", "Johto", 75, 149)]
    [InlineData("kanto_champion_rematch_squirtle", "Unova", 75, 199)]
    // Johto's phone rematch, Hoenn's Wallace of ours
    [InlineData("johto_bug_catcher_wade_4", "Hoenn", 52, 159)]
    [InlineData("hoenn_wallace_rematch", "Unova", 66, 191)]
    public void ARematchTakesTheFurthestHallOfFamesLevels(string id, string furthest, int own, int met)
    {
        var halls = new List<string>();
        foreach (var region in RegionDatabase.All)
        {
            halls.Add(region.Id);
            if (region.Id == furthest) break;
        }
        var levels = RematchLevels.For(id, With(halls.ToArray()), "Sinnoh");
        Assert.NotNull(levels);
        Assert.Equal(met, levels!.Map(own));
        Assert.Equal(furthest, RematchLevels.Furthest(With(halls.ToArray()))!.Id);
    }

    [Fact]
    public void EveryRematchOfARegionGonePastStaysUnderTheFurthestsPostGame()
    {
        // The whole of Unova's Hall of Fame: every rematch team of the regions before it is met under Unova's post-game
        // pair's level (203) and at or above its first route's (162)
        var story = With(RegionDatabase.All.Select(r => r.Id).Take(5).ToArray());
        var unova = RegionDatabase.Get("Unova")!.Levels!;
        int top = unova.Points[^1].To, bottom = unova.Points[0].To;
        foreach (var region in TrainerDatabase.Regions.Concat(new[] { "" }))
        {
            var records = region.Length == 0 ? TrainerDatabase.All : TrainerDatabase.OfRegion(region);
            foreach (var record in records.Where(r => TrainerDatabase.IsRematchTeam(r.Id) && !r.Id.StartsWith("unova_")))
            {
                var levels = RematchLevels.For(record.Id, story, "Sinnoh");
                Assert.NotNull(levels);
                foreach (var p in record.Party)
                    Assert.InRange(levels!.Map(p.Level), bottom, top);
            }
        }
    }
}
