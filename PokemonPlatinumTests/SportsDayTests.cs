using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Sports days in Nimbasa's stadiums (plan 42 · PS5; <c>docs/mechanics/rulings.md</c>, "Sports days"): every day of the
/// week has its sport at each stadium and the lobby's receptionist takes the player to that field; the athletes battle
/// with the data's teams by the story, once a day; a game in the twilight hours shuts the field and fills the stands;
/// the walk of the whole of Unova reaches every field.
/// </summary>
public class SportsDayTests
{
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Unova"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Map> HandMade = new();
    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : HandMade.GetOrAdd(name, MapDatabase.Get);

    private static readonly Sport[] Sports = Enum.GetValues<Sport>();
    private static readonly DayOfWeek[] Days = Enum.GetValues<DayOfWeek>();

    private static string AreaOf(Sport sport) => sport switch
    {
        Sport.Gridiron => "nimbasa_city_5",
        Sport.Baseball => "nimbasa_city_6",
        Sport.Football => "nimbasa_city_7",
        Sport.Tennis => "nimbasa_city_10",
        _ => "nimbasa_city_11",
    };

    private static string LobbyAreaOf(Stadium stadium) => stadium == Stadium.BigStadium ? "nimbasa_city_3" : "nimbasa_city_8";

    [Fact]
    public void EveryDayHasItsSportAtEachStadiumAsBlackAndWhiteHaveIt()
    {
        foreach (var stadium in Enum.GetValues<Stadium>())
            foreach (var day in Days)
                Assert.Equal(stadium, SportsDays.StadiumOf(SportsDays.On(stadium, day)));
        int Count(Stadium s, Sport sport) => Days.Count(d => SportsDays.On(s, d) == sport);
        Assert.Equal(3, Count(Stadium.BigStadium, Sport.Baseball));
        Assert.Equal(2, Count(Stadium.BigStadium, Sport.Football));
        Assert.Equal(2, Count(Stadium.BigStadium, Sport.Gridiron));
        Assert.Equal(4, Count(Stadium.SmallCourt, Sport.Tennis));
        Assert.Equal(3, Count(Stadium.SmallCourt, Sport.Basketball));
        // Every field is a built map of the region with its sport's three athletes on it
        foreach (var sport in Sports)
        {
            var field = BuiltMaps.Value[SportsDays.FieldOf(sport)];
            Assert.Contains(AreaOf(sport), LoadedWorld.Value.Index.Areas);
            Assert.Equal(3, field.Everyone.Count(n => n.Script?.StartsWith("Athlete", StringComparison.Ordinal) == true));
        }
    }

    [Fact]
    public void TheReceptionistTakesThePlayerToTheDaysFieldAndNowhereWhileAGameIsOn()
    {
        foreach (var stadium in Enum.GetValues<Stadium>())
            foreach (var day in Days)
            {
                var receptionist = BuiltMaps.Value[SportsDays.LobbyOf(stadium)].Everyone.Single(n => n.Script == "Receptionist");
                var host = new HeadlessScriptHost { Weekday = day, TimeOfDay = TimeOfDay.Day, Region = RegionDatabase.Unova };
                Run(host, "Receptionist", LobbyAreaOf(stadium), receptionist);
                string field = SportsDays.FieldOf(SportsDays.On(stadium, day));
                var where = host.Log.Single(l => l.StartsWith("warp ", StringComparison.Ordinal)).Split(' ');
                Assert.Equal(field, where[1]);
                // The field's tile is open ground of the field
                Assert.True(BuiltMaps.Value[field].IsWalkable(int.Parse(where[2]), int.Parse(where[3])), $"{field} {where[2]},{where[3]}");

                var evening = new HeadlessScriptHost { Weekday = day, TimeOfDay = TimeOfDay.Twilight, Region = RegionDatabase.Unova };
                Run(evening, "Receptionist", LobbyAreaOf(stadium), receptionist);
                Assert.DoesNotContain(evening.Log, l => l.StartsWith("warp ", StringComparison.Ordinal));
            }
    }

    [Fact]
    public void AGameInTheTwilightHoursFillsTheStandsAndShutsTheWayToTheField()
    {
        foreach (var area in new[] { "nimbasa_city_3", "nimbasa_city_4", "nimbasa_city_8", "nimbasa_city_9" })
            foreach (var time in Enum.GetValues<TimeOfDay>())
            {
                var host = new HeadlessScriptHost { TimeOfDay = time, Region = RegionDatabase.Unova };
                if (time != TimeOfDay.Twilight) host.Story.Set(SportsDays.GameOnFlag);
                Run(host, ScriptLibrary.OnTransition, area);
                Assert.Equal(SportsDays.GameOn(time), host.Story.Has(SportsDays.GameOnFlag));
            }
        // The lobbies' policemen stand across the way only while a game is on; so do the stands' crowds
        foreach (var lobby in new[] { "NimbasaCity_BigStadium", "NimbasaCity_SmallCourt" })
        {
            var police = BuiltMaps.Value[lobby].Everyone.Where(n => n.Name == "Policeman").ToList();
            Assert.Equal(3, police.Count);
            Assert.All(police, p => Assert.Equal(SportsDays.GameOnFlag, p.ShownBy));
        }
        foreach (var stands in new[] { "NimbasaCity_BigStadiumStands", "NimbasaCity_SmallCourtStands" })
            Assert.True(BuiltMaps.Value[stands].Everyone.Count(n => n.ShownBy == SportsDays.GameOnFlag) >= 5);
    }

    [Fact]
    public void EachAthletesTeamsClimbAndTheStoryPicksOne()
    {
        foreach (var sport in Sports)
            foreach (var athlete in SportsDays.AthletesOf(sport))
            {
                var teams = SportsDays.TeamsOf(sport, athlete);
                Assert.Equal(4, teams.Count);
                var levels = teams.Select(id => TrainerDatabase.Get(id, RegionDatabase.Unova)!.Party.Max(p => p.Level)).ToList();
                for (int i = 1; i < 4; i++) Assert.True(levels[i] > levels[i - 1], $"{athlete}: {string.Join(", ", levels)}");
                Assert.All(teams, id => Assert.Equal(SportsDays.ClassOf(sport), TrainerDatabase.Get(id, RegionDatabase.Unova)!.Class));
            }
        Assert.Equal(1, SportsDays.TierOf(4, false));
        Assert.Equal(2, SportsDays.TierOf(5, false));
        Assert.Equal(3, SportsDays.TierOf(8, false));
        Assert.Equal(4, SportsDays.TierOf(8, true));
    }

    [Theory]
    [InlineData(4, false)]
    [InlineData(6, false)]
    [InlineData(8, false)]
    [InlineData(8, true)]
    public void AnAthleteBattlesWithTheTeamTheStoryCallsForOnceADay(int badges, bool hallOfFame)
    {
        var unova = RegionDatabase.Get(RegionDatabase.Unova)!;
        foreach (var sport in Sports)
            for (int n = 0; n < 3; n++)
            {
                var host = new HeadlessScriptHost { Region = RegionDatabase.Unova };
                foreach (var badge in unova.BadgeNames.Take(badges)) host.Story.GiveBadge(RegionDatabase.Unova, badge);
                if (hallOfFame) host.Story.Set("UnovaHallOfFame");
                string expected = SportsDays.TeamOf(sport, SportsDays.AthletesOf(sport)[n], SportsDays.TierOf(badges, hallOfFame));
                var athlete = BuiltMaps.Value[SportsDays.FieldOf(sport)].Everyone.Single(p => p.Script == $"Athlete{n + 1}");

                Run(host, $"Athlete{n + 1}", AreaOf(sport), athlete);
                Assert.Contains(host.Log, l => l.StartsWith($"battle {expected} ", StringComparison.Ordinal));
                Assert.True(host.Story.Has(SportsDays.BeatenTodayFlag(sport, n)));
                // Not again today
                host.Log.Clear();
                Run(host, $"Athlete{n + 1}", AreaOf(sport), athlete);
                Assert.DoesNotContain(host.Log, l => l.StartsWith("battle ", StringComparison.Ordinal));
                // Back tomorrow
                host.Story.ClearDaily();
                Run(host, $"Athlete{n + 1}", AreaOf(sport), athlete);
                Assert.Contains(host.Log, l => l.StartsWith($"battle {expected} ", StringComparison.Ordinal));
            }
    }

    [Fact]
    public void TheWalkOfUnovaReachesEveryField()
    {
        var walked = WorldWalk.From(MapNamed, RegionDatabase.Get(RegionDatabase.Unova)!.Start!);
        foreach (var sport in Sports) Assert.Contains(walked.Keys, m => m.Name == SportsDays.FieldOf(sport));
    }

    private static void Run(HeadlessScriptHost host, string script, string area, NPC? self = null)
    {
        var library = ScriptLibrary.Default;
        var runner = new ScriptRunner(library, host);
        runner.Start(library.Find(script, area)!, self);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
    }
}
