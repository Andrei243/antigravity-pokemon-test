using System;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// What the day and the hour bring (plan 06 · R14a): the day's events (<c>FieldSystem_HandleDailyEvents</c>), the
/// Lottery Corner, the people who come and go with the clock and the Valley Windworks' Friday Drifloon.
/// </summary>
public class DailyEventsTests
{
    private static ScriptLibrary Scripts => ScriptLibrary.Default;

    private static HeadlessScriptHost Run(string name, string place, HeadlessScriptHost host, NPC? subject = null)
    {
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find(name, place)!, subject);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    // ---------------------------------------------------------------- the day's events

    [Fact]
    public void ANewDayClearsTheDailyFlagsDrawsALevelAndMovesTheDaysNumberOnOnceADay()
    {
        var story = new StoryState();
        var encounters = SpecialEncounters.NewGame(new Random(1));
        uint before = encounters.DailyNumber;
        story.Set("FLAG_DAILY_CHECKED_LUCKY_NUMBER");
        story.Set("FLAG_NOT_DAILY");

        DailyEvents.DaysPass(3, story, encounters, "route_201", new Random(2));

        Assert.False(story.Has("FLAG_DAILY_CHECKED_LUCKY_NUMBER"));
        Assert.True(story.Has("FLAG_NOT_DAILY"));
        Assert.InRange(story.Var(DailyEvents.RandomLevelVariable), 2, 99);
        // The day's number moves on once for every day (SpecialEncounters.DaysPass), the events happen once
        var oneByOne = SpecialEncounters.NewGame(new Random(1));
        for (int i = 0; i < 3; i++) oneByOne.DaysPass(1);
        Assert.NotEqual(before, encounters.DailyNumber);
        Assert.Equal(oneByOne.DailyNumber, encounters.DailyNumber);

        // No day passing changes nothing
        story.Set("FLAG_DAILY_CHECKED_LUCKY_NUMBER");
        DailyEvents.DaysPass(0, story, encounters, null, new Random(2));
        Assert.True(story.Has("FLAG_DAILY_CHECKED_LUCKY_NUMBER"));
    }

    [Fact]
    public void TheHiddenItemsComeBackTwoByTwoButNotWhereThePlayerStands()
    {
        // Over many days every piece and every Honey comes back somewhere else than where the player stands
        var story = new StoryState();
        var chance = new Random(5);
        void FindAll()
        {
            foreach (var (_, flag) in DailyEvents.IronIslandPieces) story.Set(flag);
            foreach (string flag in DailyEvents.MeadowHoney) story.Set(flag);
        }

        for (int day = 0; day < 200; day++)
        {
            FindAll();
            DailyEvents.ReturnHiddenItems(story, DailyEvents.Meadow, chance);
            // Standing in the meadow keeps its Honey found; the island's pieces come back, at most two
            Assert.All(DailyEvents.MeadowHoney, flag => Assert.True(story.Has(flag)));
            int back = DailyEvents.IronIslandPieces.Count(p => !story.Has(p.Flag));
            Assert.InRange(back, 1, 2);
        }

        bool[] seen = new bool[DailyEvents.MeadowHoney.Length];
        for (int day = 0; day < 200; day++)
        {
            FindAll();
            DailyEvents.ReturnHiddenItems(story, "iron_island_b2f_left_room", chance);
            Assert.True(story.Has("FLAG_OBTAINED_HIDDEN_IRON_ISLAND_B2F_LEFT_ROOM_STAR_PIECE_1"));
            Assert.True(story.Has("FLAG_OBTAINED_HIDDEN_IRON_ISLAND_B2F_LEFT_ROOM_STAR_PIECE_2"));
            for (int i = 0; i < seen.Length; i++) seen[i] |= !story.Has(DailyEvents.MeadowHoney[i]);
        }
        Assert.All(seen, Assert.True);
    }

    // ---------------------------------------------------------------- the lottery

    [Fact]
    public void TheTicketIsTheHighHalfOfTheDaysNextNumber()
    {
        // SetJubilifeLotteryTrainerID: 0 × 1103515245 + 12345 = 12345, whose high half is 0
        Assert.Equal(0, Lottery.TicketOf(0));
        // 1 × 1103515245 + 12345 = 1103527590 = 0x41C67EA6: the high half is 0x41C6
        Assert.Equal(0x41C6, Lottery.TicketOf(1));
        Assert.InRange(Lottery.TicketOf(uint.MaxValue), 0, 65535);
    }

    [Theory]
    [InlineData(12345, 12345, 5)]
    [InlineData(12345, 99345, 3)]
    [InlineData(12345, 2345, 4)]
    [InlineData(12345, 12346, 0)]
    [InlineData(5, 5, 5)]
    [InlineData(105, 5, 2)]
    public void DigitsMatchFromTheRight(int ticket, int id, int digits) =>
        Assert.Equal(digits, Lottery.Matching(ticket, id));

    [Fact]
    public void TheBestMatchWinsAndTheTeamWinsATie()
    {
        var player = 11111;
        Pokemon From(int id) => new(PokemonDatabase.Get("Starly")!, 5) { OriginalTrainer = new TrainerMark("Someone", id, PlayerLook.Boy) };
        var mine = new Pokemon(PokemonDatabase.Get("Bidoof")!, 5);
        var twoDigits = From(40045);
        var threeInTheBox = From(60345);
        var threeOnTheTeam = From(20345);

        var draw = Lottery.Check(12345, player, new[] { mine, twoDigits }, new[] { threeInTheBox });
        Assert.Equal((3, threeInTheBox, true), (draw.Digits, draw.Winner, draw.InBox));

        draw = Lottery.Check(12345, player, new[] { mine, threeOnTheTeam }, new[] { threeInTheBox });
        Assert.Equal((3, threeOnTheTeam, false), (draw.Digits, draw.Winner, draw.InBox));

        // The player's own Pokémon carry the player's ID number: 11111 against 12341 is one digit
        draw = Lottery.Check(12341, player, new[] { mine }, Array.Empty<Pokemon>());
        Assert.Equal((1, mine), (draw.Digits, draw.Winner));
        Assert.Equal(0, Lottery.Check(12342, player, new[] { mine }, Array.Empty<Pokemon>()).Digits);
        // Only the card's 16 bits count
        Assert.Equal(5, Lottery.Check(1234, 0x10000 + 1234, new[] { mine }, Array.Empty<Pokemon>()).Digits);
    }

    [Fact]
    public void ThePrizesGrowWithTheDigits()
    {
        Assert.Null(Lottery.Prize(0));
        Assert.Equal(new[] { "Ultra Ball", "PP Up", "Exp. Share", "Max Revive", "Master Ball" }, Enumerable.Range(1, 5).Select(Lottery.Prize));
        Assert.All(Enumerable.Range(1, 5), d => Assert.NotNull(ItemDatabase.Get(Lottery.Prize(d)!)));
    }

    [Fact]
    public void TheLotteryClerkChecksOnceADayAndGivesThePrize()
    {
        var host = new HeadlessScriptHost { TrainerNumber = 0 };
        host.Encounters.DailyNumber = 1;
        int ticket = Lottery.TicketOf(1);
        // A traded Pokémon whose ID number shares the ticket's last three digits, in the box
        int id = 50000 + ticket % 1000;
        var traded = new Pokemon(PokemonDatabase.Get("Shinx")!, 9) { OriginalTrainer = new TrainerMark("Pen Pal", id, PlayerLook.Girl) };
        host.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 9));
        host.Boxes.Add(traded);
        Assert.Equal(3, Lottery.Matching(ticket, id));
        Assert.True(Lottery.Matching(ticket, 0) < 3);

        Run("LotteryClerk", "JubilifeTV1F", host);

        Assert.Contains(host.Transcript, l => l.Text.Contains(ticket.ToString("D5")));
        Assert.Contains(host.Transcript, l => l.Text.Contains("Shinx") && l.Text.Contains("PC Boxes"));
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Exp. Share")!));
        Assert.True(host.Story.Has("FLAG_DAILY_CHECKED_LUCKY_NUMBER"));

        // Once a day: no second prize until the day's flags are cleared
        Run("LotteryClerk", "JubilifeTV1F", host);
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Exp. Share")!));
        host.Story.ClearDaily();
        Run("LotteryClerk", "JubilifeTV1F", host);
        Assert.Equal(2, host.Bag.GetQuantity(ItemDatabase.Get("Exp. Share")!));

        // Saying no checks nothing
        var declined = new HeadlessScriptHost();
        declined.Answers.Enqueue(1);
        Run("LotteryClerk", "JubilifeTV1F", declined);
        Assert.False(declined.Story.Has("FLAG_DAILY_CHECKED_LUCKY_NUMBER"));
    }

    [Fact]
    public void JubilifeTVsDoorLeadsToTheLotteryCorner()
    {
        var game = new OpeningTests.Game();
        game.Arrive("Sinnoh", 164, 752);
        var door = game.Map.Warps.Single(w => (w.SourceX, w.SourceY) == (164, 751));
        Assert.Equal("JubilifeTV1F", door.TargetMap);
        game.Through("JubilifeTV1F");
        Assert.Contains(game.Map.NPCs, n => n.Script == "LotteryClerk");
        Assert.Contains(game.Map.Warps, w => w.TargetMap == "Sinnoh" && (w.TargetX, w.TargetY) == (164, 752));
    }

    // ---------------------------------------------------------------- the people of the hour

    [Theory]
    [InlineData(TimeOfDay.Morning, true)]
    [InlineData(TimeOfDay.Day, false)]
    [InlineData(TimeOfDay.Twilight, false)]
    [InlineData(TimeOfDay.Night, false)]
    [InlineData(TimeOfDay.LateNight, false)]
    public void TheJoggersBattleOnlyInTheMorning(TimeOfDay time, bool battle)
    {
        foreach (var (place, jogger) in new[]
        {
            ("route_209", "ROUTE_209_JOGGER_RICHARD"), ("route_209", "ROUTE_209_JOGGER_RAUL"),
            ("route_210_south", "ROUTE_210_SOUTH_JOGGER_WYATT"),
            ("route_215", "ROUTE_215_JOGGER_SCOTT"), ("route_215", "ROUTE_215_JOGGER_CRAIG")
        })
        {
            var host = Run(ScriptLibrary.OnEnter, place, new HeadlessScriptHost { TimeOfDay = time });
            Assert.Equal(battle, !host.Story.Has($"FLAG_HIDE_{jogger}"));
            Assert.Equal(battle, host.Story.Has($"FLAG_HIDE_{jogger}_NO_BATTLE"));
        }
    }

    [Theory]
    [InlineData(TimeOfDay.Morning, false)]
    [InlineData(TimeOfDay.Day, false)]
    [InlineData(TimeOfDay.Twilight, false)]
    [InlineData(TimeOfDay.Night, true)]
    [InlineData(TimeOfDay.LateNight, true)]
    public void ThePolicemenBattleOnlyAtNight(TimeOfDay time, bool battle)
    {
        foreach (var (place, policeman) in new[]
        {
            ("route_212_south", "ROUTE_212_SOUTH_POLICEMAN_DANNY"),
            ("route_212_north", "ROUTE_212_NORTH_POLICEMAN_BOBBY"), ("route_212_north", "ROUTE_212_NORTH_POLICEMAN_CALEB"),
            ("route_222", "ROUTE_222_POLICEMAN_THOMAS")
        })
        {
            var host = Run(ScriptLibrary.OnEnter, place, new HeadlessScriptHost { TimeOfDay = time });
            Assert.Equal(battle, !host.Story.Has($"FLAG_HIDE_{policeman}"));
            Assert.Equal(battle, host.Story.Has($"FLAG_HIDE_{policeman}_NO_BATTLE"));
        }
    }

    [Fact]
    public void OnlyOneOfEachPairStandsOnTheirTile()
    {
        // A new game has the doubles out until the place's own script has run; a morning on Route 209 swaps them
        var game = new OpeningTests.Game();
        game.Arrive("Sinnoh", 164, 752);
        Assert.Null(game.Present("jogger_richard", "route_209"));
        Assert.NotNull(game.Present("jogger_richard_no_battle", "route_209"));
        Assert.Null(game.Present("policeman_thomas", "route_222"));
        Assert.NotNull(game.Present("policeman_thomas_no_battle", "route_222"));

        game.Arrive("Sinnoh", 540, 720);
        Assert.Equal("route_209", game.Map.ScriptFileAt(540, 720));
        Assert.NotNull(game.Present("jogger_richard", "route_209"));
        Assert.Null(game.Present("jogger_richard_no_battle", "route_209"));
        Assert.True(game.Present("jogger_richard", "route_209")!.IsTrainer);
    }

    [Fact]
    public void ASaveFromBeforeHasTheDoublesOut()
    {
        var story = new StoryState();
        StoryMigration.Upgrade(story, 6, Array.Empty<Pokemon>(), Scripts, new Inventory());
        Assert.True(story.Has("FLAG_HIDE_ROUTE_215_JOGGER_CRAIG"));
        Assert.True(story.Has("FLAG_HIDE_ROUTE_212_NORTH_POLICEMAN_ALEX"));
        Assert.True(story.Has("FLAG_HIDE_VALLEY_WINDWORKS_OUTSIDE_DRIFLOON"));
        Assert.False(story.Has("FLAG_HIDE_ROUTE_215_JOGGER_CRAIG_NO_BATTLE"));
        Assert.True(StoryState.CurrentVersion >= 7);
    }

    // ---------------------------------------------------------------- Friday's Drifloon

    [Theory]
    [InlineData(DayOfWeek.Friday, 2, false, true)]
    [InlineData(DayOfWeek.Thursday, 2, false, false)]
    [InlineData(DayOfWeek.Friday, 1, false, false)]
    [InlineData(DayOfWeek.Friday, 2, true, false)]
    public void TheDrifloonComesOnFridaysOnceTheWorksAreFree(DayOfWeek day, int works, bool battledToday, bool there)
    {
        var host = new HeadlessScriptHost { Weekday = day };
        host.Story.SetVar("VAR_VALLEY_WINDWORKS_STATE", works);
        if (battledToday) host.Story.Set("FLAG_DAILY_WON_AGAINST_VALLEY_WINDWORKS_OUTSIDE_DRIFLOON");
        Run("DrifloonToday", "valley_windworks_outside", host);
        Assert.Equal(there, !host.Story.Has("FLAG_HIDE_VALLEY_WINDWORKS_OUTSIDE_DRIFLOON"));
    }

    [Fact]
    public void TheDrifloonIsGoneForTheDayOnceBattled()
    {
        var host = new HeadlessScriptHost { Weekday = DayOfWeek.Friday };
        host.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 20));
        Run("Drifloon", "valley_windworks_outside", host, new NPC { Name = "Drifloon", Key = "drifloon", Species = "Drifloon" });
        Assert.Contains("wildbattle Drifloon 15 Won", host.Log);
        Assert.True(host.Story.Has("FLAG_DAILY_WON_AGAINST_VALLEY_WINDWORKS_OUTSIDE_DRIFLOON"));
        host.Story.ClearDaily();
        Assert.False(host.Story.Has("FLAG_DAILY_WON_AGAINST_VALLEY_WINDWORKS_OUTSIDE_DRIFLOON"));
    }
}
