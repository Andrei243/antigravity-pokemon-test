using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Facilities;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 37 · FC2: the later places' frame (<see cref="StreakChallenge"/>, <see cref="FacilityRecord"/>, <c>facility</c>)
/// and the streak house of Kiloude City's Battle Maison (<see cref="StreakHouse"/>): its four rooms, their hosts and hard
/// rooms, its points, a challenge put down and taken up, its battles' copies and records, the desk and the hall played
/// headless, and triples and rotations fought with the house's own teams.
/// </summary>
public class StreakHouseTests
{
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);

    private static Party Team(int count, int level = 60)
    {
        var party = new Party();
        foreach (string species in new[] { "Greninja", "Talonflame", "Aegislash", "Hawlucha", "Goodra", "Tyrantrum" }.Take(count))
            party.Add(new Pokemon(PokemonDatabase.Get(species)!, level, new Random(party.Count + 1)));
        return party;
    }

    private static Func<int> Draws(int seed)
    {
        var rng = new Random(seed);
        return () => rng.Next(0x10000);
    }

    [Theory]
    [InlineData("single", 3, BattleFormat.Single)]
    [InlineData("double", 4, BattleFormat.Double)]
    [InlineData("triple", 6, BattleFormat.Triple)]
    [InlineData("rotation", 4, BattleFormat.Rotation)]
    public void EachRoomHasTheOriginalsCountAndItsFormatAndTrainersBringThatMany(string way, int count, BattleFormat format)
    {
        foreach (bool hard in new[] { false, true })
        {
            var room = StreakHouse.Room(way, hard);
            Assert.Equal(count, room.Entered);
            Assert.Equal(format, room.Format);
            Assert.Equal(hard ? 50 : 20, room.HostAt);
            var records = new FrontierRecords();
            var challenge = StreakChallenge.Begin(records, room, Enumerable.Range(0, count).ToList(), Team(count), Draws(37));
            Assert.Equal(format, challenge.Conditions.Format);
            var foe = challenge.Opponent(records);
            Assert.Equal(count, foe.Pokemon.Count);
            Assert.All(foe.Pokemon, p => Assert.Equal(FrontierRules.Level, p.Level));
            Assert.Equal(count, foe.Pokemon.Select(p => p.Species.Name).Distinct().Count());
            Assert.Equal(count, challenge.TeamForBattle(Team(count)).Count);
        }
    }

    [Fact]
    public void TheHostsTeamsAreOursAtFiftyWithAMegaStoneAndEveryMoveTheirOwn()
    {
        foreach (string way in StreakHouse.Ways)
        {
            var host = StreakHouse.Hosts[way];
            Assert.True(CharacterStyles.Default.Has(host.Look), $"no look {host.Look}");
            foreach (string id in new[] { host.Normal, host.Hard })
            {
                var record = TrainerDatabase.Get(id) ?? throw new Xunit.Sdk.XunitException($"{id} is no trainer");
                Assert.Contains(record, TrainerDatabase.Ours);
                Assert.True(record.KeyStone, $"{id} holds no Key Stone");
                Assert.Equal(StreakHouse.EnteredIn(way), record.Party.Count);
                var party = TrainerDatabase.PartyOf(record);
                Assert.All(party.Members, p => Assert.Equal(50, p.Level));
                Assert.Contains(party.Members, p => p.HeldItem is { } item && item.Name.EndsWith("ite", StringComparison.Ordinal) && BattleCore.MegaFormOf(p) != null);
                foreach (var p in party.Members) Assert.Empty(Legality.Unlearnable(p));
                Assert.Equal(party.Count, party.Members.Select(p => p.HeldItem?.Name).Distinct().Count());
            }
        }
    }

    [Fact]
    public void TheHostComesAtTheTwentiethAndOpensTheHardRoomWhoseHostLeavesTheTrophyOnce()
    {
        var records = new FrontierRecords();
        var party = Team(3);
        var room = StreakHouse.Room("single", false);
        var challenge = StreakChallenge.Begin(records, room, new[] { 0, 1, 2 }, party, Draws(5));
        for (int battle = 1; battle < 20; battle++)
        {
            Assert.False(challenge.HostComes(records));
            Assert.False(challenge.Won(records, false).OpenedHard);
        }
        Assert.True(challenge.HostComes(records));
        var host = challenge.OpponentAsTrainer(records);
        Assert.Equal("house_host_corvin", host.Id);
        Assert.True(host.KeyStone);
        var won = challenge.Won(records, false);
        Assert.True(won.Host && won.OpenedHard);
        Assert.Equal(20, won.Points);
        Assert.True(records.Facility(StreakHouse.Place, "single", false).HardOpen);
        // Beaten again on another streak, it opens nothing new
        Assert.False(challenge.HostComes(records));

        // The hard room's host at its 50th: a trophy the first time only
        var hard = StreakHouse.Room("single", true);
        records.Facility(StreakHouse.Place, "single", true).Streak = 49;
        var grand = StreakChallenge.Begin(records, hard, new[] { 0, 1, 2 }, party, Draws(6));
        Assert.True(grand.HostComes(records));
        Assert.Equal("house_host_corvin_hard", grand.OpponentAsTrainer(records).Id);
        var first = grand.Won(records, trophyHeld: false);
        Assert.True(first.Trophy);
        Assert.Equal(40, first.Points);
        records.Facility(StreakHouse.Place, "single", true).Streak = 49;
        Assert.False(StreakChallenge.Begin(records, hard, new[] { 0, 1, 2 }, party, Draws(7)).Won(records, trophyHeld: true).Trophy);
    }

    [Fact]
    public void PointsComePerWinRisingWithTheStreak()
    {
        Assert.Equal(1, StreakHouse.PointsFor(1, false));
        Assert.Equal(1, StreakHouse.PointsFor(10, false));
        Assert.Equal(2, StreakHouse.PointsFor(11, false));
        Assert.Equal(20, StreakHouse.PointsFor(20, false));
        Assert.Equal(5, StreakHouse.PointsFor(80, false));
        Assert.Equal(2, StreakHouse.PointsFor(1, true));
        Assert.Equal(40, StreakHouse.PointsFor(50, true));
        for (int streak = 1; streak < 100; streak++)
            if (streak != 20) Assert.True(StreakHouse.PointsFor(streak + 1, false) >= StreakHouse.PointsFor(streak, false) || streak + 1 == 20);
        // The tiers climb with the streak, and the hard rooms start higher
        Assert.Equal((1, 2), StreakHouse.TiersFor(0, false));
        Assert.Equal((3, 4), StreakHouse.TiersFor(60, false));
        Assert.True(StreakHouse.TiersFor(0, true).Lowest > StreakHouse.TiersFor(0, false).Lowest);
        Assert.True(StreakHouse.IvsFor(0, true) > StreakHouse.IvsFor(0, false));
    }

    [Fact]
    public void AChallengePutDownAndTakenUpMeetsTheSameTrainer()
    {
        var party = Team(4);
        var room = StreakHouse.Room("double", false);
        // Twice from the same draws: one goes straight on, the other rests before the second battle and between draws
        var straight = new FrontierRecords();
        var a = StreakChallenge.Begin(straight, room, new[] { 0, 1, 2, 3 }, party, Draws(11));
        a.Opponent(straight);
        a.Won(straight, false);
        var second = a.Opponent(straight);

        var rested = new FrontierRecords();
        var b = StreakChallenge.Begin(rested, room, new[] { 0, 1, 2, 3 }, party, Draws(11));
        b.Opponent(rested);
        b.Won(rested, false);
        b.Pause(rested);
        Assert.False(rested.Facility(StreakHouse.Place, "double", false).InProgress);
        Assert.True(StreakChallenge.IsPaused(rested, room));
        var again = StreakChallenge.Resume(rested, room, party)!;
        Assert.Equal(1, again.StreakBefore);
        var met = again.Opponent(rested);
        Assert.Equal(second.Dress!.Id, met.Dress!.Id);
        Assert.Equal(second.Pokemon.Select(p => p.Species.Name), met.Pokemon.Select(p => p.Species.Name));

        // Put down with the trainer already drawn, the same trainer is met again
        again.Pause(rested);
        Assert.Equal(met.Dress.Id, StreakChallenge.Resume(rested, room, party)!.Opponent(rested).Dress!.Id);

        // A team changed meanwhile can't take it up: the streak ends
        var changed = new FrontierRecords();
        var c = StreakChallenge.Begin(changed, room, new[] { 0, 1, 2, 3 }, party, Draws(11));
        c.Won(changed, false);
        c.Pause(changed);
        var other = Team(4);
        other.Members.Reverse();
        Assert.Null(StreakChallenge.Resume(changed, room, other));
        Assert.Equal(0, changed.Facility(StreakHouse.Place, "double", false).Streak);
        Assert.Equal(1, changed.Facility(StreakHouse.Place, "double", false).Latest);
    }

    [Fact]
    public void AChallengeWalkedOutOnEndsTheStreakAtTheNextBegin()
    {
        var records = new FrontierRecords();
        var room = StreakHouse.Room("single", false);
        var first = StreakChallenge.Begin(records, room, new[] { 0, 1, 2 }, Team(3), Draws(2));
        first.Won(records, false);
        first.Won(records, false);
        var next = StreakChallenge.Begin(records, room, new[] { 0, 1, 2 }, Team(3), Draws(3));
        Assert.Equal(0, next.StreakBefore);
        Assert.Equal(2, records.Facility(StreakHouse.Place, "single", false).Latest);
        Assert.Equal(2, records.Facility(StreakHouse.Place, "single", false).Best);
    }

    [Fact]
    public void TheCopiesLeaveTheTeamAsItWas()
    {
        var party = Team(3, level: 70);
        var records = new FrontierRecords();
        var challenge = StreakChallenge.Begin(records, StreakHouse.Room("single", false), new[] { 2, 0, 1 }, party, Draws(9));
        var team = challenge.TeamForBattle(party);
        Assert.Equal(new[] { "Aegislash", "Greninja", "Talonflame" }, team.Members.Select(p => p.Species.Name));
        Assert.All(team.Members, p => Assert.Equal(50, p.Level));
        foreach (var p in team.Members) p.CurrentHP = 0;
        Assert.All(party.Members, p => Assert.Equal(70, p.Level));
        Assert.All(party.Members, p => Assert.Equal(p.MaxHP, p.CurrentHP));
        Assert.All(challenge.TeamForBattle(party).Members, p => Assert.Equal(p.MaxHP, p.CurrentHP));
    }

    [Fact]
    public void TheRecordsRoundTripASaveAndAreNullUntilTheFirstIsEntered()
    {
        var records = new FrontierRecords();
        Assert.Null(records.Facilities);
        var room = StreakHouse.Room("rotation", false);
        var challenge = StreakChallenge.Begin(records, room, new[] { 0, 1, 2, 3 }, Team(4), Draws(4));
        challenge.Won(records, false);
        challenge.Pause(records);
        records.Facility(StreakHouse.Place, "rotation", false).HardOpen = true;
        var read = SaveManager.Read(SaveManager.Write(new SaveData { Frontier = records }))!.Frontier!;
        var kept = read.PeekFacility(StreakHouse.Place, "rotation", false)!;
        Assert.Equal(1, kept.Streak);
        Assert.True(kept.HardOpen);
        Assert.NotNull(kept.Paused);
        Assert.Equal(new[] { 0, 1, 2, 3 }, kept.Paused!.Slots);
        Assert.Equal(records.Facility(StreakHouse.Place, "rotation", false).Paused!.State, kept.Paused.State);
        Assert.Equal(challenge.Opponent(records).Dress!.Id, StreakChallenge.Resume(read, room, Team(4))!.Opponent(read).Dress!.Id);
    }

    [Fact]
    public void TheBoardShowsEachRoomAndTheHardRoomsOnceOpen()
    {
        var records = new FrontierRecords();
        var story = new StoryState();
        var board = FrontierBoard.ForFacility(StreakHouse.Place, records, story);
        Assert.Equal(4, board.Rows.Count);
        Assert.Equal(FrontierPrint.None, board.Print);
        records.Facility(StreakHouse.Place, "triple", false).HardOpen = true;
        records.Facility(StreakHouse.Place, "triple", false).Best = 24;
        var rotation = StreakChallenge.Begin(records, StreakHouse.Room("rotation", false), new[] { 0, 1, 2, 3 }, Team(4), Draws(1));
        rotation.Won(records, false);
        story.SetVar(StreakHouse.TrophyVariable("triple"), 1);
        board = FrontierBoard.ForFacility(StreakHouse.Place, records, story);
        Assert.Equal(5, board.Rows.Count);
        Assert.Contains(board.Rows, r => r.Record == 24);
        Assert.Contains(board.Rows, r => r.Running && r.Latest == 1);
        Assert.Equal(FrontierPrint.Silver, board.Print);
    }

    [Fact]
    public void ABattleOfTheHouseReplaysAndATripleAndARotationAlwaysEnd()
    {
        // The formats' fuzz with the house's teams (plan 37's risk note): copies of a team of six against the house's
        // trainers of the later sets, both sides chosen by the trainer AI under the modern rules, and each recorded battle replayed
        foreach (string way in new[] { "triple", "rotation", "double", "single" })
            for (int seed = 0; seed < 6; seed++)
            {
                var room = StreakHouse.Room(way, seed % 2 == 1);
                var records = new FrontierRecords();
                if (seed % 3 == 2) records.Facility(StreakHouse.Place, way, room.Hard).Streak = room.HostAt - 1;
                var party = Team(room.Entered);
                var challenge = StreakChallenge.Begin(records, room, Enumerable.Range(0, room.Entered).ToList(), party, Draws(100 + seed));
                BattleCore Fight() => new(new CoreSetup
                {
                    PlayerParty = challenge.TeamForBattle(party), Trainers = new List<Trainer> { challenge.OpponentAsTrainer(records) },
                    Random = new BattleRandom((uint)(seed * 31 + 7)), Rules = Ruleset.Modern, Format = room.Format, Frontier = true,
                    PlayerController = TrainerAi.Instance, KeyStone = true
                });
                var core = Fight();
                Assert.Equal(room.Format, core.Format);
                core.Start();
                Assert.Null(core.Request);
                Assert.True(core.Result is BattleResult.PlayerVictory or BattleResult.PlayerDefeat, $"{way} {seed} ended as {core.Result}");
                Assert.True(core.Turn < 1000);
                var said = core.Log.OfType<Said>().Select(s => s.Text).ToList();
                var again = Fight();
                again.Replay(core.Record!);
                Assert.Equal(said, again.Log.OfType<Said>().Select(s => s.Text).ToList());
                Assert.All(party.Members, p => Assert.Equal(p.MaxHP, p.CurrentHP));
            }
    }

    [Fact]
    public void ABattleOfTheHouseHasTheBagsKeyStone()
    {
        var bag = new Inventory();
        bag.AddItem(ItemDatabase.Get(BattleCore.KeyStoneItem)!, 1);
        var records = new FrontierRecords();
        var challenge = StreakChallenge.Begin(records, StreakHouse.Room("single", false), new[] { 0, 1, 2 }, Team(3), Draws(8));
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = challenge.TeamForBattle(Team(3)), Inventory = bag, Pokedex = new Pokedex(), Trainers = new List<Trainer> { challenge.OpponentAsTrainer(records) },
            Frontier = true, Format = challenge.Conditions.Format ?? BattleFormat.Single, Random = new BattleRandom(3)
        });
        Assert.True(battle.Core.KeyStone);
    }

    // ------------------------------------------------------------------ the desk and the hall, headless

    private static HeadlessScriptHost Host(StoryState story, Party party, Map at, int[] answers, Func<NPC?, BattleOutcome>? fight = null)
    {
        at.ForgetForced();
        at.ApplyPresence(story.Has);
        var host = new HeadlessScriptHost(story, party, new Inventory())
        {
            Map = at, Region = RegionDatabase.Kalos, MapNamed = name => Rooms.Value(name), Fight = fight
        };
        foreach (int a in answers) host.Answers.Enqueue(a);
        return host;
    }

    private static void Run(HeadlessScriptHost host, string file, string script, NPC? who = null)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, file) ?? throw new Xunit.Sdk.XunitException($"{file}.{script} is missing"), who);
        runner.RunToEnd(20000);
        Assert.Empty(host.Problems);
    }

    [Fact]
    public void TheDeskBeginsATripleAndTheHallPlaysItsBattlesUntilTheyAreRetiredFrom()
    {
        var story = new StoryState();
        var lobby = Rooms.Value("KiloudeCity_BattleMaison");
        // Enter, the triple room
        var host = Host(story, Team(6), lobby, new[] { 0, 2 });
        Run(host, "KiloudeCity_BattleMaison", "Desk", lobby.Everyone.Single(n => n.Key == "desk"));
        Assert.NotNull(host.Facility);
        Assert.Equal("triple", host.Facility!.Room.Way);
        Assert.Contains(host.Log, l => l.StartsWith("warp KiloudeCity_BattleMaisonHall", StringComparison.Ordinal));

        // Three wins, then retire: three battles of the Frontier in the triple format, a point each, the streak ended
        var hall = Rooms.Value("KiloudeCity_BattleMaisonHall");
        host.Map = hall;
        foreach (int a in new[] { 0, 0, 2 }) host.Answers.Enqueue(a);
        Run(host, "KiloudeCity_BattleMaisonHall", "OnEnter");
        Assert.Equal(3, host.FrontierBattles.Count);
        Assert.All(host.FrontierFields, f => Assert.Equal(BattleFormat.Triple, f!.Format));
        Assert.All(host.FrontierBattles, b => Assert.Equal(6, b.Team.Count));
        Assert.Equal(3, host.Frontier.BattlePoints);
        var record = host.Frontier.Facility(StreakHouse.Place, "triple", false);
        Assert.Equal(0, record.Streak);
        Assert.Equal(3, record.Latest);
        Assert.Equal(3, record.Best);
        Assert.Null(host.Facility);
        Assert.Contains(host.Log, l => l.StartsWith("warp KiloudeCity_BattleMaison ", StringComparison.Ordinal));
    }

    [Fact]
    public void ALossEndsTheStreakAndARestKeepsItForTheDesk()
    {
        var story = new StoryState();
        var lobby = Rooms.Value("KiloudeCity_BattleMaison");
        var hall = Rooms.Value("KiloudeCity_BattleMaisonHall");
        var desk = lobby.Everyone.Single(n => n.Key == "desk");

        // A win and a rest: the challenge is put down and saved
        var host = Host(story, Team(4), lobby, new[] { 0, 3 });
        Run(host, "KiloudeCity_BattleMaison", "Desk", desk);
        Assert.Equal("rotation", host.Facility!.Room.Way);
        host.Map = hall;
        host.Answers.Enqueue(1);
        Run(host, "KiloudeCity_BattleMaisonHall", "OnEnter");
        Assert.Contains(host.Log, l => l.StartsWith("frontier save", StringComparison.Ordinal));
        Assert.True(StreakChallenge.IsPaused(host.Frontier, StreakHouse.Room("rotation", false)));
        Assert.Null(host.Facility);

        // At the desk again it is taken up at once, with its streak of one
        host.Map = lobby;
        Run(host, "KiloudeCity_BattleMaison", "Desk", desk);
        Assert.NotNull(host.Facility);
        Assert.Contains(host.Transcript, l => l.Text.Contains("streak of 1 still stands", StringComparison.Ordinal));

        // Then a loss: the streak ends
        host.Map = hall;
        host.Fight = _ => BattleOutcome.Lost;
        Run(host, "KiloudeCity_BattleMaisonHall", "OnEnter");
        var record = host.Frontier.Facility(StreakHouse.Place, "rotation", false);
        Assert.Equal(0, record.Streak);
        Assert.Equal(1, record.Latest);
        Assert.Null(record.Paused);
        Assert.Contains(host.Transcript, l => l.Text.Contains("The streak ends here", StringComparison.Ordinal));
    }

    [Fact]
    public void TheHallsHostOpensTheGrandRoomAndTheDeskOffersIt()
    {
        var story = new StoryState();
        var lobby = Rooms.Value("KiloudeCity_BattleMaison");
        var hall = Rooms.Value("KiloudeCity_BattleMaisonHall");
        var host = Host(story, Team(3), hall, new[] { 2 });
        host.Frontier.Facility(StreakHouse.Place, "single", false).Streak = 19;
        host.Facility = StreakChallenge.Begin(host.Frontier, StreakHouse.Room("single", false), new[] { 0, 1, 2 }, host.Party, Draws(12));
        Run(host, "KiloudeCity_BattleMaisonHall", "OnEnter");
        Assert.Contains(host.Transcript, l => l.Text.Contains("host of this room is coming down", StringComparison.Ordinal));
        Assert.Equal("house_host_corvin", host.FrontierBattles.Single().Foes.Single().Id);
        Assert.Contains(host.Transcript, l => l.Text.Contains("grand room of this hall is open", StringComparison.Ordinal));
        Assert.True(host.Frontier.Facility(StreakHouse.Place, "single", false).HardOpen);
        Assert.Equal(20, host.Frontier.BattlePoints);

        // The desk asks which room, and the grand one is begun at its own streak
        host.Map = lobby;
        foreach (int a in new[] { 0, 0, 1 }) host.Answers.Enqueue(a);
        Run(host, "KiloudeCity_BattleMaison", "Desk", lobby.Everyone.Single(n => n.Key == "desk"));
        Assert.True(host.Facility!.Room.Hard);

        // The grand room's host at its 50th leaves the trophy, kept in the story
        host.Frontier.Facility(StreakHouse.Place, "single", true).Streak = 49;
        host.Map = hall;
        host.Answers.Enqueue(2);
        Run(host, "KiloudeCity_BattleMaisonHall", "OnEnter");
        Assert.Equal(1, story.Var(StreakHouse.TrophyVariable("single")));
        Assert.Contains(host.Transcript, l => l.Text.Contains("this room's trophy", StringComparison.Ordinal));
    }
}
