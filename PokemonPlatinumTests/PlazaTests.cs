using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 41 · EG3: Pennant Hall, the hub's plaza (<see cref="Plaza"/>, <c>Data/plaza.json</c>, <c>Hub_Plaza.txt</c>;
/// rulings, "Pennant Hall"). Its rank by the pennants earned and its pitches by rank, the stalls offered by the regions
/// finished, the day's guests drawn once a day, their word and their battle a day at the lifted level, the pennants never
/// under nought, the lottery's one draw shared with Jubilife's, every stall's service run from its booth, the state saved
/// and an older save reading none, and a game ranking the hall from its first rank to its top.
/// </summary>
[Collection("Shared clocks and dice")]
public class PlazaTests
{
    private static readonly DateTime Monday = new(2026, 11, 9);

    private static StoryState Finished(params string[] regions)
    {
        var story = new StoryState();
        foreach (string id in regions)
        {
            story.EnterRegion(id);
            story.CompleteRegion(id);
        }
        return story;
    }

    private static Party Team()
    {
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, 100));
        return party;
    }

    private static NPC Person(string id) => new() { Id = id, Key = id, Name = id };

    /// <summary>A script of the hall played to its end in a game with no screen, with a person of the hall as its subject.</summary>
    private static HeadlessScriptHost Play(HeadlessScriptHost host, string script, string? subject = null, params int[] answers)
    {
        foreach (int a in answers) host.Answers.Enqueue(a);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, Plaza.Map)!, subject == null ? null : Person(subject));
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    private static HeadlessScriptHost Host(StoryState story) => new(story, Team(), new Inventory()) { Money = 50000 };

    [Fact]
    public void TheHallsRankComesWithThePennantsItEarnsAndEachRankHasItsPitches()
    {
        Assert.Equal(6, Plaza.TopRank);
        Assert.Equal(8, Plaza.MostPitches);
        Assert.Equal(new[] { 1, 1, 2, 2, 3, 4, 5, 6, 6 }, new[] { 0, 59, 60, 149, 150, 300, 500, 800, 5000 }.Select(Plaza.RankOf));
        Assert.Equal(new[] { 3, 4, 5, 6, 7, 8 }, Enumerable.Range(1, 6).Select(Plaza.PitchesAt));
        Assert.Equal(60, Plaza.ToNextRank(0));
        Assert.Equal(0, Plaza.ToNextRank(800));
        // Spending never lowers the rank: it goes by what was earned
        var plaza = new PlazaState();
        plaza.Give(160);
        Assert.True(plaza.Spend(150));
        Assert.Equal(10, plaza.Coins);
        Assert.Equal(3, plaza.Rank);
    }

    [Fact]
    public void ThePennantsNeverGoUnderNought()
    {
        var plaza = new PlazaState();
        plaza.Give(12);
        Assert.False(plaza.Spend(13));
        Assert.Equal(12, plaza.Coins);
        Assert.True(plaza.Spend(12));
        Assert.Equal(0, plaza.Coins);
        Assert.False(plaza.Spend(1));
        plaza.Give(-5);
        Assert.Equal(0, plaza.Coins);
        // ...and a script that spends what isn't there hears 0
        var host = Host(Finished(RegionDatabase.Sinnoh, RegionDatabase.Kanto));
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptParser.Parse("test", "script Spend\n  plaza spend 5\n  end\n")[0]);
        runner.RunToEnd();
        Assert.Equal(0, runner.Result);
        Assert.Equal(0, host.Plaza!.Coins);
    }

    [Fact]
    public void ACaseClosedPaysPennantsAndSaysSoOnlyOnceTheHallIsOpen()
    {
        // Looker's agency's share (plan 41's leftovers): 40 pennants for a case closed, paid whether or not the hall has
        // met the player, told only once it has
        foreach (bool opened in new[] { false, true })
        {
            var host = Host(Finished(RegionDatabase.Sinnoh, RegionDatabase.Kanto));
            host.Plaza!.Opened = opened;
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find("CasePennants", "Hub_Office")!);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            Assert.Equal(40, host.Plaza.Coins);
            Assert.Equal(opened, host.Transcript.Any(l => l.Text.Contains("40 pennants", StringComparison.Ordinal)));
        }
    }

    [Fact]
    public void AStallIsOfferedOnlyOnceItsRegionIsFinishedAndItsRankReached()
    {
        var story = Finished(RegionDatabase.Sinnoh, RegionDatabase.Kanto);
        var lottery = Plaza.Stall("lottery")!;
        var relearner = Plaza.Stall("relearner")!;
        var goods = Plaza.Stall("goods")!;
        Assert.True(Plaza.Offered(goods, story, 1));
        Assert.True(Plaza.Offered(lottery, story, 1));
        Assert.False(Plaza.Offered(relearner, story, 3), "Johto's stall waits for Johto's Hall of Fame");
        story.CompleteRegion(RegionDatabase.Johto);
        Assert.False(Plaza.Offered(relearner, story, 2), "and for its rank");
        Assert.True(Plaza.Offered(relearner, story, 3));
        // A stall not offered can't be set out, nor at a pitch the rank hasn't
        var plaza = new PlazaState();
        Assert.False(plaza.Set(1, "relearner", story));
        Assert.True(plaza.Set(1, "lottery", story));
        Assert.False(plaza.Set(4, "goods", story), "a hall of rank 1 has three pitches");
        // A stall stands at one pitch: set elsewhere, it moves
        Assert.True(plaza.Set(2, "lottery", story));
        Assert.Null(plaza.At(1));
        Assert.Equal("lottery", plaza.At(2));
        // The goods widen with the rank
        Assert.True(Plaza.StockAt(goods, 1).Count < Plaza.StockAt(goods, Plaza.TopRank).Count);
        Assert.All(goods.Goods, g => Assert.NotNull(ItemDatabase.Get(g.Item)));
        Assert.All(Plaza.Stalls, s => Assert.True(s.Region == null || RegionDatabase.Get(s.Region) != null, s.Id));
    }

    [Fact]
    public void TheDaysGuestsAreTheSameHoweverOftenTheHallIsComeToAndNewTheNextDay()
    {
        var story = Finished(RegionDatabase.Sinnoh, RegionDatabase.Kanto);
        var plaza = new PlazaState();
        Assert.True(plaza.GuestsFor(Monday, story, new Random(7)));
        var first = plaza.Guests.ToList();
        Assert.Equal(Plaza.GuestsAt(1), first.Count);
        Assert.Equal(3, first.Count);
        Assert.Equal(first.Count, first.Distinct().Count());
        Assert.All(first, id => Assert.NotNull(TrainerDatabase.Get(id)));
        // Come to again the same day, whatever chance is handed: the same guests, the day's words remembered
        Assert.Equal(5, plaza.Greet(1));
        Assert.False(plaza.GuestsFor(Monday, story, new Random(99)));
        Assert.Equal(first, plaza.Guests);
        Assert.Equal(0, plaza.Greet(1));
        // The next day draws anew, and the words and battles are forgotten
        Assert.True(plaza.GuestsFor(Monday.AddDays(1), story, new Random(99)));
        Assert.Equal(5, plaza.Greet(1));
        // Guests come only from the finished regions' tables, of the classes the file names
        var pool = Plaza.GuestPool(story);
        Assert.NotEmpty(pool);
        Assert.All(pool, t => Assert.Contains(t.Class, Plaza.File.GuestClasses));
        Assert.All(pool, t => Assert.True(TrainerDatabase.All.Contains(t) || t.Id.StartsWith("kanto_", StringComparison.Ordinal), t.Id));
        Assert.Empty(Plaza.GuestPool(new StoryState()));
    }

    [Fact]
    public void AGuestGivesTheirWordAndBattlesOnceADayAtTheLiftedLevel()
    {
        GameClock.FixedDate = Monday;
        try
        {
            var story = Finished(RegionDatabase.Sinnoh, RegionDatabase.Kanto);
            var host = Host(story);
            host.Plaza!.GuestsFor(Monday, story, new Random(3));
            var guest = host.Plaza.GuestAt(1)!;
            int top = 0, levelsOfTheMapsTrainer = TrainerDatabase.Get(guest.Id)!.Party.Max(p => p.Level);
            string? battled = null;
            host.Fight = npc =>
            {
                top = npc!.TrainerData!.Party.Members.Max(p => p.Level);
                battled = npc.TrainerData.Id;
                Assert.True(npc.TrainerData.LevelsKept);
                return BattleOutcome.Won;
            };
            Play(host, "Guest", "guest_1", 0);
            Assert.Equal(Plaza.GuestLevel(story), top);
            Assert.Equal(120, Plaza.GuestLevel(story));
            Assert.Equal(ScriptRunner.PlazaGuestTrainer + guest.Id, battled);
            // The trainer of the data keeps their team, and their region's trainer is never beaten here
            Assert.Equal(levelsOfTheMapsTrainer, TrainerDatabase.Get(guest.Id)!.Party.Max(p => p.Level));
            Assert.False(story.HasDefeated(guest.Id));
            Assert.Equal(Plaza.File.GuestWord + Plaza.File.GuestWin, host.Plaza.Coins);
            // Spoken to again the same day: no word's pennants, and no battle
            battled = null;
            var again = Play(host, "Guest", "guest_1", 0);
            Assert.Null(battled);
            Assert.Contains(again.Transcript, l => l.Text.Contains("fair and square"));
            Assert.Equal(Plaza.File.GuestWord + Plaza.File.GuestWin, host.Plaza.Coins);
            // A battle lost pays nothing and may be tried again that day
            host.Plaza.GuestsFor(Monday.AddDays(1), story, new Random(3));
            host.Fight = _ => BattleOutcome.Lost;
            Play(host, "Guest", "guest_2", 0);
            Assert.DoesNotContain(2, host.Plaza.Battled);
        }
        finally
        {
            GameClock.FixedDate = null;
        }
    }

    [Fact]
    public void ALiftedTeamKeepsItsSpreadOnACopy()
    {
        var record = TrainerDatabase.Get("leader_roark")!;
        var trainer = new Trainer { Id = record.Id };
        TrainerDatabase.Fill(trainer, record);
        var before = trainer.Party.Members.Select(p => p.Level).ToList();
        var lifted = LevelLift.To(trainer, 120);
        Assert.True(lifted.LevelsKept);
        Assert.False(trainer.LevelsKept);
        Assert.Equal(120, lifted.Party.Members.Max(p => p.Level));
        int by = 120 - before.Max();
        Assert.Equal(before.Select(l => l + by), lifted.Party.Members.Select(p => p.Level));
        Assert.Equal(before, trainer.Party.Members.Select(p => p.Level));
        Assert.NotSame(trainer.Party, lifted.Party);
        // The post-game level is the furthest finished region's last pair, Sinnoh's own with only Sinnoh
        Assert.Equal(0, LevelLift.PostGameLevel(new StoryState()));
        Assert.Equal(LevelLift.SinnohPostGame, LevelLift.PostGameLevel(Finished(RegionDatabase.Sinnoh)));
        Assert.Equal(156, LevelLift.PostGameLevel(Finished(RegionDatabase.Sinnoh, RegionDatabase.Kanto, RegionDatabase.Johto)));
    }

    [Fact]
    public void TheManagerSetsAStallAtAPitchWithTheQuestionBox()
    {
        GameClock.FixedDate = Monday;
        try
        {
            var story = Finished(RegionDatabase.Sinnoh, RegionDatabase.Kanto);
            var host = Host(story);
            // SET A STALL, pitch 1, the second stall offered (the lottery), then NOTHING
            int lottery = Plaza.OfferedNow(story, 1).ToList().FindIndex(s => s.Id == "lottery");
            Play(host, "Manager", "manager", 0, 0, lottery);
            Assert.Equal("lottery", host.Plaza!.At(1));
            Assert.True(story.Has(Plaza.KeeperFlag(1)));
            Assert.False(story.Has(Plaza.KeeperFlag(2)));
            Assert.True(story.Has("FLAG_HUB_PLAZA_MET"));
            Assert.True(host.Plaza.Opened);
            // The guests were drawn as the manager was spoken to, and their flags stand
            Assert.Equal(3, host.Plaza.Guests.Count);
            Assert.True(story.Has(Plaza.GuestFlag(3)));
            Assert.False(story.Has(Plaza.GuestFlag(4)));
            // A pitch the rank hasn't: SET A STALL, the south wall, pitch 5, NOTHING
            var later = Play(host, "Manager", "manager", 0, 4, 0);
            Assert.Contains(later.Transcript, l => l.Text.Contains("higher rank"));
            // EMPTY clears the pitch
            Play(host, "Manager", "manager", 0, 0, Plaza.OfferedNow(story, 1).Count);
            Assert.Null(host.Plaza.At(1));
            Assert.False(story.Has(Plaza.KeeperFlag(1)));
        }
        finally
        {
            GameClock.FixedDate = null;
        }
    }

    [Fact]
    public void EveryStallsServiceRunsFromItsBooth()
    {
        GameClock.FixedDate = Monday;
        try
        {
            var story = Finished(RegionDatabase.Sinnoh, RegionDatabase.Kanto, RegionDatabase.Johto, RegionDatabase.Hoenn);
            foreach (var stall in Plaza.Stalls)
            {
                var host = Host(story);
                host.Plaza!.Give(Plaza.File.Ranks[^1]);
                Assert.True(host.Plaza.Set(1, stall.Id, story), stall.Id);
                foreach (string item in new[] { "Seal Case", "Poffin Case", "Pokéblock Case", "Heart Scale", "Oran Berry" })
                    host.Bag.AddItem(ItemDatabase.Get(item)!, 1);
                Play(host, "Keeper", Plaza.KeeperPrefix + "1", 0, 0, 0, 0);
                Assert.True(host.Log.Count + host.Transcript.Count > 0, stall.Id);
                if (stall.Goods.Count > 0) Assert.Contains($"open Shop {ScriptRunner.PlazaShopCounter}{stall.Id}", host.Log);
            }
        }
        finally
        {
            GameClock.FixedDate = null;
        }
    }

    [Fact]
    public void TheLotterysOneDrawIsSharedWithJubilifes()
    {
        var story = Finished(RegionDatabase.Sinnoh, RegionDatabase.Kanto);
        var host = Host(story);
        host.Plaza!.Set(1, "lottery", story);
        story.Set("FLAG_DAILY_CHECKED_LUCKY_NUMBER");
        var drawn = Play(host, "Keeper", Plaza.KeeperPrefix + "1");
        Assert.Contains(drawn.Transcript, l => l.Text.Contains("here or in Jubilife"));
        // ...and a draw here closes Jubilife's counter for the day
        story.Unset("FLAG_DAILY_CHECKED_LUCKY_NUMBER");
        Play(host, "Keeper", Plaza.KeeperPrefix + "1", 0);
        Assert.True(story.Has("FLAG_DAILY_CHECKED_LUCKY_NUMBER"));
        var jubilife = new HeadlessScriptHost(story, Team(), new Inventory());
        var runner = new ScriptRunner(ScriptLibrary.Default, jubilife);
        runner.Start(ScriptLibrary.Default.Find("LotteryClerk", "JubilifeTV1F")!);
        runner.RunToEnd();
        Assert.Contains(jubilife.Transcript, l => l.Text.Contains("Today's draw is done"));
    }

    [Fact]
    public void ThePlazaRoundTripsASaveAndAnOlderSaveReadsNone()
    {
        var story = Finished(RegionDatabase.Sinnoh, RegionDatabase.Kanto);
        var plaza = new PlazaState();
        Assert.Null(plaza.ToSave());
        plaza.Give(200);
        plaza.Spend(30);
        plaza.Set(2, "goods", story);
        plaza.GuestsFor(Monday, story, new Random(5));
        plaza.Greet(1);
        plaza.Battled.Add(2);
        var text = GameDataFiles.Serialize(plaza.ToSave());
        var back = new PlazaState();
        back.Load(GameDataFiles.Deserialize<PlazaSave>(text));
        Assert.Equal(text, GameDataFiles.Serialize(back.ToSave()));
        Assert.Equal(175, back.Coins);
        Assert.Equal(205, back.Earned);
        Assert.Equal("goods", back.At(2));
        Assert.Contains(1, back.Greeted);
        Assert.False(back.GuestsFor(Monday, story, new Random(1)));
        // A save from before the hall: nothing opened, nothing written back
        var older = new SaveData();
        Assert.Null(older.Plaza);
        var none = new PlazaState();
        none.Load(older.Plaza);
        Assert.False(none.Opened);
        Assert.Null(none.ToSave());
    }

    [Fact]
    public void AGameRanksTheHallFromItsFirstRankToItsTopOnPennants()
    {
        var story = Finished(RegionDatabase.Sinnoh, RegionDatabase.Kanto);
        var host = Host(story);
        host.Fight = _ => BattleOutcome.Won;
        try
        {
            int days = 0;
            var ranks = new List<int> { host.Plaza!.Rank };
            while (host.Plaza.Rank < Plaza.TopRank && days < 60)
            {
                GameClock.FixedDate = Monday.AddDays(days++);
                Play(host, "Manager", "manager", 3);
                for (int g = 1; g <= host.Plaza.Guests.Count; g++) Play(host, "Guest", Plaza.GuestPrefix + g, 0);
                ranks.Add(host.Plaza.Rank);
            }
            Assert.Equal(Plaza.TopRank, host.Plaza.Rank);
            Assert.Equal(Enumerable.Range(1, Plaza.TopRank), ranks.Distinct());
            Assert.True(days < 30, $"the top took {days} days");
            // At the top, eight pitches and every stall of the finished regions offered
            Assert.Equal(8, Plaza.PitchesAt(host.Plaza.Rank));
        }
        finally
        {
            GameClock.FixedDate = null;
        }
    }
}
