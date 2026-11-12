using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Facilities;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 37 · FC4: the house of titles on Kalos's Route 7 (<see cref="TitlesHouse"/>, <c>KalosRoute7_BattleChateau</c>):
/// its members' teams of the later sets at the title's level over the player's highest, the same all day and never
/// relevelled; the day's visitor of another finished region by the title; points, thresholds and the title on the card.
/// </summary>
public class TitlesHouseTests
{
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);

    [Fact]
    public void TheTitlesComeAtTheirThresholdsAndTheScriptGivesThemTheSame()
    {
        Assert.Equal(0, TitlesHouse.TitleFor(0));
        Assert.Equal(1, TitlesHouse.TitleFor(3));
        Assert.Equal(2, TitlesHouse.TitleFor(14));
        Assert.Equal(7, TitlesHouse.TitleFor(500));
        var hall = Rooms.Value("KalosRoute7_BattleChateau");
        for (int title = 1; title <= TitlesHouse.HighestTitle; title++)
        {
            var story = new StoryState();
            story.SetVar(TitlesHouse.PointsVariable, TitlesHouse.Thresholds[title]);
            Play(story, "Title", hall);
            Assert.Equal(title, story.Var(TitlesHouse.TitleVariable));
            var below = new StoryState();
            below.SetVar(TitlesHouse.PointsVariable, TitlesHouse.Thresholds[title] - 1);
            Play(below, "Title", hall);
            Assert.Equal(title - 1, below.Var(TitlesHouse.TitleVariable));
        }
    }

    [Fact]
    public void AMembersTeamIsThreeLaterSetsAtTheTitlesLevelOverThePlayersHighestAndIsKept()
    {
        for (int title = 0; title <= TitlesHouse.HighestTitle; title++)
        {
            var team = TitlesHouse.MemberTeam("mirabelle", title, 190, 1234);
            Assert.Equal(3, team.Party.Count);
            Assert.All(team.Party.Members, p => Assert.Equal(TitlesHouse.LevelFor(title, 190), p.Level));
            Assert.True(team.LevelsKept);
            Assert.Equal("kalos_chateau_mirabelle", team.Id);
            Assert.Equal("Mirabelle", team.Name);
            Assert.Equal(3, team.Party.Members.Select(p => p.Species.Name).Distinct().Count());
        }
        Assert.Equal(184, TitlesHouse.LevelFor(0, 190));
        Assert.Equal(195, TitlesHouse.LevelFor(7, 190));
        Assert.Equal(1, TitlesHouse.LevelFor(0, 3));
        // The same all day; another day's seed, and another member, another team
        string Names(Trainer t) => string.Join(",", t.Party.Members.Select(p => p.Species.Name));
        Assert.Equal(Names(TitlesHouse.MemberTeam("octave", 4, 100, 77)), Names(TitlesHouse.MemberTeam("octave", 4, 100, 77)));
        Assert.NotEqual(Names(TitlesHouse.MemberTeam("octave", 4, 100, 77)), Names(TitlesHouse.MemberTeam("octave", 4, 100, 78)));
        Assert.NotEqual(Names(TitlesHouse.MemberTeam("octave", 4, 100, 77)), Names(TitlesHouse.MemberTeam("isaure", 4, 100, 77)));
        // The east place's sets are a step stronger
        Assert.True(TitlesHouse.TiersFor(3, east: true).Lowest > TitlesHouse.TiersFor(3, east: false).Lowest);
    }

    [Fact]
    public void TheDaysVisitorIsOfAnotherFinishedRegionAndOfTheRoleTheTitleAllows()
    {
        Func<string, bool> kalosOnly = f => f == "KalosHallOfFame";
        Assert.Equal(0, TitlesHouse.VisitorOfTheDay(0, kalosOnly, 9));
        Func<string, bool> two = f => f is "KalosHallOfFame" or "HoennHallOfFame";
        for (int seed = 0; seed < 40; seed++)
        {
            foreach (var (title, role) in new[] { (0, "leader"), (6, "elite"), (7, "champion") })
            {
                int n = TitlesHouse.VisitorOfTheDay(title, two, seed);
                Assert.True(n > 0);
                var visitor = PartnersTree.Cast[n - 1];
                Assert.Equal("Hoenn", visitor.Region);
                Assert.Equal(role, visitor.Role);
            }
        }
        Assert.Equal(TitlesHouse.VisitorOfTheDay(0, two, 5), TitlesHouse.VisitorOfTheDay(0, two, 5));
    }

    [Fact]
    public void AMemberBattledThroughTheScriptPaysPointsOnceADayAndIsNeverRelevelled()
    {
        var hall = Rooms.Value("KalosRoute7_BattleChateau");
        var story = new StoryState();
        story.SetVar(TitlesHouse.SeedVariable, 321);
        story.SetVar(TitlesHouse.TitleVariable, 3);
        Trainer? met = null;
        var octave = hall.Everyone.Single(n => n.Key == "octave");
        var host = Play(story, "Guest", hall, octave, fight: npc => { met = npc?.TrainerData; return BattleOutcome.Won; });
        Assert.NotNull(met);
        Assert.True(met!.LevelsKept);
        Assert.All(met.Party.Members, p => Assert.Equal(TitlesHouse.LevelFor(3, 80), p.Level));
        Assert.Equal(2, story.Var(TitlesHouse.PointsVariable));
        Assert.True(story.Has("FLAG_DAILY_KALOS_CHATEAU_BEAT_OCTAVE"));
        Assert.Contains(host.Log, l => l.StartsWith("battle kalos_chateau_octave", StringComparison.Ordinal));
        // Battled again the same day: nothing
        Play(story, "Guest", hall, octave);
        Assert.Equal(2, story.Var(TitlesHouse.PointsVariable));
        // The member's own trainer of the map is left as it was
        Assert.Null(octave.TrainerData);
    }

    [Fact]
    public void AVisitorStandsInTheHallOnceAnotherRegionIsFinishedAndPaysByTheirRole()
    {
        var hall = Rooms.Value("KalosRoute7_BattleChateau");
        var story = new StoryState();
        story.Set("KalosHallOfFame");
        Play(story, "OnEnter", hall);
        Assert.False(story.Has("FLAG_KALOS_CHATEAU_HAS_VISITOR"));
        Assert.Equal(0, story.Var(TitlesHouse.VisitorVariable));

        var finished = new StoryState();
        finished.Set("KalosHallOfFame");
        finished.Set("SinnohHallOfFame");
        finished.SetVar(TitlesHouse.TitleVariable, 7);
        Play(finished, "OnEnter", hall);
        Assert.True(finished.Has("FLAG_KALOS_CHATEAU_HAS_VISITOR"));
        int n = finished.Var(TitlesHouse.VisitorVariable);
        Assert.Equal("champion_cynthia_rematch", PartnersTree.Cast[n - 1].Trainer);
        hall.ApplyPresence(finished.Has);
        var visitor = hall.Everyone.Single(v => v.Key == "opponent");
        Trainer? met = null;
        var host = Play(finished, "Guest", hall, visitor, answers: new[] { 0 }, fight: npc => { met = npc?.TrainerData; return BattleOutcome.Won; });
        Assert.NotNull(met);
        Assert.Equal(TitlesHouse.VisitorTrainer + "champion_cynthia_rematch", met!.Id);
        Assert.True(met.LevelsKept);
        Assert.Equal(TitlesHouse.PointsFor(TitlesGuestKind.Visitor, "champion"), finished.Var(TitlesHouse.PointsVariable));
        Assert.True(finished.Has("FLAG_DAILY_KALOS_CHATEAU_BEAT_VISITOR"));
        // Cynthia herself, the trainer of Sinnoh's table, is not beaten by it
        Assert.False(finished.HasDefeated("champion_cynthia_rematch"));
    }

    [Fact]
    public void TheTitleHasItsWordsForTheCard()
    {
        for (int title = 0; title <= TitlesHouse.HighestTitle; title++)
            Assert.False(string.IsNullOrWhiteSpace(Text.Of($"titlesHouse.title.{title}")));
        Assert.Contains("Patron", Text.Of("titlesHouse.card", ("title", Text.Of("titlesHouse.title.5"))), StringComparison.Ordinal);
    }

    private static HeadlessScriptHost Play(StoryState story, string script, Map at, NPC? who = null, int[]? answers = null, Func<NPC?, BattleOutcome>? fight = null)
    {
        at.ForgetForced();
        at.ApplyPresence(story.Has);
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Greninja")!, 80));
        var host = new HeadlessScriptHost(story, party, new Inventory()) { Map = at, Region = RegionDatabase.Kalos, Weekday = DayOfWeek.Monday, Fight = fight };
        foreach (int a in answers ?? Array.Empty<int>()) host.Answers.Enqueue(a);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, "KalosRoute7_BattleChateau") ?? throw new Xunit.Sdk.XunitException($"{script} is missing"), who);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }
}
