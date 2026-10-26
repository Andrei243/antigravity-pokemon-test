using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// The Battle Palace and the Battle Arena (plan 14 · B9; rulings, "The Battle Palace" and "The Battle Arena"): a Pokémon
/// of each nature picks from the group its table says at the rate it says, a Palace battle plays with nobody asked and
/// replays, a bout judged on each of the three counts, a judged battle ends in three turns and replays, the venues' rounds
/// of seven, and the desks and the halls played by <see cref="HeadlessScriptHost"/>.
/// </summary>
[Collection("MapDatabase")]
public class BattleVenueTests
{
    private static Map Room(string name) => GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    private static Func<int> Draws(int seed)
    {
        var random = new Random(seed);
        return () => random.Next(0x10000);
    }

    private static Pokemon Natured(string species, Nature nature, params string[] moves)
    {
        var p = Of(Gender.Male, species, 50, moves);
        p.Nature = nature;
        return p;
    }

    private static BattleCore Unstarted(Pokemon mine, Pokemon foe, Random rolls, bool palace = false, int turnLimit = 0, params Pokemon[] bench)
    {
        var party = new Party();
        party.Add(mine);
        foreach (var p in bench) party.Add(p);
        var trainer = new Trainer { Id = "ace", Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 0 };
        trainer.Party.Add(foe);
        return new BattleCore(new CoreSetup
        {
            PlayerParty = party, Trainers = new List<Trainer> { trainer }, Random = rolls, Rules = Ruleset.Platinum,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas", Palace = palace, TurnLimit = turnLimit, Frontier = true
        });
    }

    private static Party Three()
    {
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, 60, new Random(60)));
        party.Add(new Pokemon(PokemonDatabase.Get("Lucario")!, 55, new Random(55)));
        party.Add(new Pokemon(PokemonDatabase.Get("Staraptor")!, 52, new Random(52)));
        return party;
    }

    // ------------------------------------------------------------------ the Palace's table

    [Fact]
    public void EveryNatureHasItsRowAndItsSharesFallAsTheTableSays()
    {
        foreach (var nature in Enum.GetValues<Nature>())
        {
            var style = PalaceNatures.Of(nature);
            foreach (bool low in new[] { false, true })
            {
                var groups = Enumerable.Range(0, 100).Select(roll => style.GroupFor(roll, low)).ToList();
                Assert.Equal(low ? style.AttackLow : style.Attack, groups.Count(g => g == PalaceGroup.Attack));
                Assert.Equal(low ? style.DefenceLow : style.Defence, groups.Count(g => g == PalaceGroup.Defence));
                Assert.Equal(style.Support(low), groups.Count(g => g == PalaceGroup.Support));
                Assert.InRange(style.Support(low), 0, 100);
            }
        }
        // Two rows read off Emerald's table: Hardy 61/7/32 both ways, Gentle 18/70/12 and 90/5/5 low
        Assert.Equal((61, 7, 61, 7), (PalaceNatures.Of(Nature.Hardy).Attack, PalaceNatures.Of(Nature.Hardy).Defence, PalaceNatures.Of(Nature.Hardy).AttackLow, PalaceNatures.Of(Nature.Hardy).DefenceLow));
        Assert.Equal((18, 70, 90, 5), (PalaceNatures.Of(Nature.Gentle).Attack, PalaceNatures.Of(Nature.Gentle).Defence, PalaceNatures.Of(Nature.Gentle).AttackLow, PalaceNatures.Of(Nature.Gentle).DefenceLow));
        Assert.Equal(PalaceFlavour.Growl, PalaceNatures.Of(Nature.Sassy).Flavour);
    }

    [Fact]
    public void ThePalaceSortsMovesByWhatTheyAreAimedAt()
    {
        PalaceGroup Group(string move) => PalaceNatures.GroupOf(MoveDatabase.Get(move)!);
        Assert.Equal(PalaceGroup.Attack, Group("Tackle"));
        Assert.Equal(PalaceGroup.Attack, Group("Earthquake"));
        Assert.Equal(PalaceGroup.Defence, Group("Swords Dance"));
        Assert.Equal(PalaceGroup.Defence, Group("Reflect"));
        Assert.Equal(PalaceGroup.Support, Group("Growl"));
        Assert.Equal(PalaceGroup.Support, Group("Thunder Wave"));
        Assert.Equal(PalaceGroup.Support, Group("Spikes"));
        // Emerald aims a weather move at every battler, and its last case makes that an attack
        Assert.Equal(PalaceGroup.Attack, Group("Rain Dance"));
        // A move of variable power is no status move, whatever power the data gives it
        Assert.Equal(PalaceGroup.Attack, Group("Seismic Toss"));
    }

    [Theory]
    [InlineData(Nature.Hardy)]
    [InlineData(Nature.Lonely)]
    [InlineData(Nature.Brave)]
    [InlineData(Nature.Bold)]
    [InlineData(Nature.Timid)]
    [InlineData(Nature.Jolly)]
    [InlineData(Nature.Mild)]
    [InlineData(Nature.Gentle)]
    [InlineData(Nature.Sassy)]
    [InlineData(Nature.Careful)]
    public void APokemonOfEachNaturePicksFromItsGroupAtTheRateItsTableSays(Nature nature)
    {
        // One move of each group: Tackle strikes, Swords Dance defends, Growl supports
        var mine = Natured("Bidoof", nature, "Tackle", "Swords Dance", "Growl");
        var core = Unstarted(mine, Of(Gender.Male, "Bidoof", 50, "Tackle"), new BattleRandom((uint)nature + 1));
        const int Tries = 4000;
        var counts = new Dictionary<PalaceGroup, int> { [PalaceGroup.Attack] = 0, [PalaceGroup.Defence] = 0, [PalaceGroup.Support] = 0 };
        for (int i = 0; i < Tries; i++)
        {
            var choice = PalaceController.Instance.ChooseAction(core, core.PlayerSlots[0]);
            Assert.Equal(ChoiceKind.Fight, choice.Kind);
            Assert.False(choice.Falters);
            counts[PalaceNatures.GroupOf(mine.Moves[choice.Move].Data)]++;
        }
        var style = PalaceNatures.Of(nature);
        // Within three points of the table's share (nearly four deviations at the widest)
        Assert.InRange(counts[PalaceGroup.Attack], (style.Attack - 3) * Tries / 100, (style.Attack + 3) * Tries / 100);
        Assert.InRange(counts[PalaceGroup.Defence], (style.Defence - 3) * Tries / 100, (style.Defence + 3) * Tries / 100);
        Assert.InRange(counts[PalaceGroup.Support], (style.Support(false) - 3) * Tries / 100, (style.Support(false) + 3) * Tries / 100);
    }

    [Fact]
    public void WithNoMoveOfItsGroupAPokemonUsesAnotherOneTimeInTwo()
    {
        // A Bold Bidoof with Tackle alone, the roll calling for support: Tackle all the same, used on a roll under 50
        BattleChoice Choose(int coin)
        {
            var core = Unstarted(Natured("Bidoof", Nature.Bold, "Tackle"), Of(Gender.Male, "Bidoof", 50, "Tackle"), Steady().Force(RollKind.Palace, 99, 0, coin));
            return PalaceController.Instance.ChooseAction(core, core.PlayerSlots[0]);
        }
        Assert.Equal((0, false), (Choose(10).Move, Choose(10).Falters));
        Assert.Equal((0, true), (Choose(70).Move, Choose(70).Falters));
    }

    [Fact]
    public void APalaceBattlePlaysWithNobodyAskedAndReplays()
    {
        BattleCore Fight()
        {
            var core = Unstarted(Natured("Lucario", Nature.Adamant, "Aura Sphere", "Swords Dance", "Growl", "Quick Attack"),
                Natured("Machamp", Nature.Calm, "Cross Chop", "Bulk Up", "Leer", "Rock Slide"), new BattleRandom(77), palace: true);
            core.Start();
            return core;
        }
        var core = Fight();
        // Nothing was asked: the battle is over from the start
        Assert.Null(core.Request);
        Assert.NotEqual(BattleResult.None, core.Result);
        var said = Lines(core.Log);
        Assert.Contains(said, l => l.Contains("is deciding for itself"));
        // A choice that hesitates is the Palace's alone
        var plain = Against(new[] { Of(Gender.Male, "Bidoof", 50, "Tackle") }, new[] { Of(Gender.Male, "Bidoof", 50, "Tackle") });
        Assert.NotNull(plain.WhyNot(BattleChoice.Fight(Mine, 0) with { Falters = true }));

        // Its record is its seed alone, and played again it says the same
        var record = core.Record!;
        Assert.Empty(record.Answers);
        var again = Unstarted(Natured("Lucario", Nature.Adamant, "Aura Sphere", "Swords Dance", "Growl", "Quick Attack"),
            Natured("Machamp", Nature.Calm, "Cross Chop", "Bulk Up", "Leer", "Rock Slide"), new BattleRandom(77), palace: true);
        again.Replay(record);
        Assert.Equal(said, Lines(again.Log));
    }

    [Fact]
    public void APalacePokemonAtHalfItsHpSaysSoOnce()
    {
        // Super Fang halves the Snorlax, which can do nothing back and says so the first time it is at half and never again
        var snorlax = Natured("Snorlax", Nature.Hardy, "Growl");
        var rattata = Natured("Raticate", Nature.Hardy, "Super Fang");
        var core = Unstarted(snorlax, rattata, Calm(), palace: true);
        core.Start();
        Assert.Single(Lines(core.Log), l => l == "Snorlax looks hungry for the next exchange!");
    }

    // ------------------------------------------------------------------ the Arena's judge

    [Fact]
    public void ABoutIsJudgedOnEachOfTheThreeCounts()
    {
        var a = Of(Gender.Male, "Bidoof", 50, "Tackle");
        var b = Of(Gender.Male, "Bidoof", 50, "Tackle");
        ArenaVerdict Judge(Action<ArenaJudge> bout, int hpMine, int hpTheirs)
        {
            var judge = new ArenaJudge();
            judge.Begin(a, b);
            bout(judge);
            return judge.Verdict(hpMine, hpTheirs);
        }
        var tackle = MoveDatabase.Get("Tackle")!;
        var protect = MoveDatabase.Get("Protect")!;

        // Mind: three attacks against three Protects, the rest level
        var mind = Judge(j => { for (int i = 0; i < 3; i++) { j.Moved(BattleSide.Player, tackle); j.Moved(BattleSide.Enemy, protect); } }, a.MaxHP, b.MaxHP);
        Assert.Equal((3, -3, 4, 2, BattleSide.Player), (mind.MindMine, mind.MindTheirs, mind.ScoreMine, mind.ScoreTheirs, mind.Winner));
        // Skill: a super effective hit against one that missed
        var skill = Judge(j => { j.Skilled(BattleSide.Player, ArenaJudge.SkillOf(false, false, false, false)); j.Skilled(BattleSide.Enemy, ArenaJudge.SkillOf(false, false, true, false)); }, a.MaxHP, b.MaxHP);
        Assert.Equal((1, 2, BattleSide.Enemy), (skill.SkillMine, skill.SkillTheirs, skill.Winner));
        // Body: the share of the bout's starting HP kept
        var body = Judge(_ => { }, a.MaxHP / 2, b.MaxHP);
        Assert.Equal((a.MaxHP / 2 * 100 / a.MaxHP, 100, BattleSide.Enemy), (body.BodyMine, body.BodyTheirs, body.Winner));
        Assert.InRange(body.BodyMine, 49, 50);
        // Level on all three: a draw
        Assert.Null(Judge(_ => { }, a.MaxHP, b.MaxHP).Winner);

        // Emerald's ratings: Fake Out and the protections one less, Counter nothing, a status move nothing
        Assert.Equal((1, -1, 0, -1, 0), (ArenaJudge.MindOf(tackle), ArenaJudge.MindOf(MoveDatabase.Get("Fake Out")!), ArenaJudge.MindOf(MoveDatabase.Get("Counter")!),
            ArenaJudge.MindOf(protect), ArenaJudge.MindOf(MoveDatabase.Get("Growl")!)));
        Assert.Equal((-2, 0, 1, 2, -1, 1), (ArenaJudge.SkillOf(true, false, false, false), ArenaJudge.SkillOf(true, true, false, false), ArenaJudge.SkillOf(false, false, false, false),
            ArenaJudge.SkillOf(false, false, true, false), ArenaJudge.SkillOf(false, false, false, true), ArenaJudge.SkillOf(false, false, true, true)));
    }

    [Fact]
    public void AJudgedBattleEndsInThreeTurnsAndReplays()
    {
        BattleCore Bout() => Unstarted(Of(Gender.Male, "Magikarp", 50, "Splash"), Of(Gender.Male, "Magikarp", 50, "Splash"), Calm(), turnLimit: ArenaJudge.Turns);
        var core = Bout();
        core.Start();
        for (int turn = 0; turn < ArenaJudge.Turns; turn++)
        {
            Assert.Equal(BattleResult.None, core.Result);
            Turn(core);
        }
        // Neither did better than the other: both are out, the player's side first, and the battle is lost on the third turn
        var verdict = Assert.Single(core.Verdicts);
        Assert.Null(verdict.Winner);
        Assert.Equal(BattleResult.PlayerDefeat, core.Result);
        Assert.Equal(ArenaJudge.Turns - 1, core.Turn);
        var judged = core.Log.OfType<Said>().Single(s => s.Shows.OfType<Judged>().Any());
        Assert.Equal(verdict, judged.Shows.OfType<Judged>().Single().Verdict);

        var again = Bout();
        again.Replay(core.Record!);
        Assert.Equal(Lines(core.Log), Lines(again.Log));
    }

    [Fact]
    public void TheBolderSideTakesTheBoutAndTheLoserFalls()
    {
        // A Tackle every turn against a Protect every turn: the judge gives the bout to the attacker
        var core = Unstarted(Of(Gender.Male, "Snorlax", 50, "Tackle"), Of(Gender.Male, "Snorlax", 50, "Protect"), Calm(), turnLimit: ArenaJudge.Turns);
        core.Start();
        for (int turn = 0; turn < ArenaJudge.Turns && core.Result == BattleResult.None; turn++) Turn(core);
        var verdict = Assert.Single(core.Verdicts);
        Assert.Equal(BattleSide.Player, verdict.Winner);
        Assert.True(verdict.MindMine > verdict.MindTheirs);
        Assert.Equal(BattleResult.PlayerVictory, core.Result);
    }

    [Fact]
    public void NobodySwitchesInTheArenaAndTheNextComesInByItself()
    {
        var core = Unstarted(Of(Gender.Male, "Magikarp", 50, "Splash"), Of(Gender.Male, "Magikarp", 50, "Splash"), Calm(), false, ArenaJudge.Turns,
            Of(Gender.Male, "Feebas", 50, "Splash"));
        core.Trainers[0].Party.Add(Of(Gender.Male, "Goldeen", 50, "Splash"));
        core.Start();
        Assert.Equal("A bout of the Battle Arena is fought to its end!", core.WhyNot(BattleChoice.Switch(Mine, 1)));
        for (int turn = 0; turn < ArenaJudge.Turns; turn++) Turn(core);
        // The draw took both Magikarp out, and the next of each team came in with nobody asked: a new bout
        Assert.Equal(BattleResult.None, core.Result);
        Assert.IsType<ActionRequest>(core.Request);
        Assert.Equal(("Feebas", "Goldeen"), (core.PlayerSlots[0].Pokemon!.Species.Name, core.EnemySlots[0].Pokemon!.Species.Name));
    }

    // ------------------------------------------------------------------ the venues' rounds

    [Theory]
    [InlineData(AnnexVenue.Palace)]
    [InlineData(AnnexVenue.Arena)]
    public void ARoundDrawsSevenTrainersOfThreeTheSameForASeed(AnnexVenue venue)
    {
        var records = new FrontierRecords();
        var round = BattleVenueChallenge.Begin(records, venue, new[] { 0, 1, 2 }, Draws(5));
        Assert.Equal(7, round.Foes.Count);
        Assert.Equal(7, round.Foes.Select(f => f.Number).Distinct().Count());
        Assert.All(round.Foes, f => Assert.Equal(3, f.Pokemon.Count));
        int[] row = FrontierDatabase.TowerRooms[0];
        Assert.All(round.Foes, f => Assert.InRange(f.Number, row[0], row[1]));
        Assert.True(BattleVenueChallenge.RecordOf(records, venue).InProgress);
        var again = BattleVenueChallenge.Begin(new FrontierRecords(), venue, new[] { 0, 1, 2 }, Draws(5));
        Assert.Equal(round.Foes.Select(f => f.Number), again.Foes.Select(f => f.Number));
        // What each hands the battle
        Assert.Equal(venue == AnnexVenue.Palace ? new FrontierConditions(Palace: true) : new FrontierConditions(TurnLimit: 3), round.Conditions);
    }

    [Fact]
    public void SevenWinsTakeTheRoundAndALossEndsTheStreak()
    {
        var records = new FrontierRecords();
        records.Arena.Streak = 7;
        var round = BattleVenueChallenge.Begin(records, AnnexVenue.Arena, new[] { 0, 1, 2 }, Draws(9));
        Assert.Equal(7, round.StreakBefore);
        // The second round draws from the second room
        int[] row = FrontierDatabase.TowerRooms[1];
        Assert.All(round.Foes, f => Assert.InRange(f.Number, row[0], row[1]));
        for (int i = 0; i < 6; i++) Assert.False(round.Won(records));
        Assert.True(round.Won(records));
        round.Finish(records, won: true);
        Assert.Equal((14, 14, 1, false), (records.Arena.Streak, records.Arena.Best, records.Arena.Rounds, records.Arena.InProgress));
        Assert.Equal(BattleDomeChallenge.PointsFor(1), round.PointsEarned);
        // The Palace's record is its own
        Assert.Equal(0, records.Palace.Streak);

        var next = BattleVenueChallenge.Begin(records, AnnexVenue.Arena, new[] { 0, 1, 2 }, Draws(10));
        next.Won(records);
        next.Finish(records, won: false);
        Assert.Equal((0, 15), (records.Arena.Streak, records.Arena.Best));
        Assert.Equal(0, next.PointsEarned);
    }

    [Fact]
    public void ARoundWalkedOutOnEndsTheStreakWhenTheNextIsBegun()
    {
        var records = new FrontierRecords();
        records.Palace.Streak = 9;
        BattleVenueChallenge.Begin(records, AnnexVenue.Palace, new[] { 0, 1, 2 }, Draws(1));
        Assert.Equal(0, BattleVenueChallenge.Begin(records, AnnexVenue.Palace, new[] { 0, 1, 2 }, Draws(2)).StreakBefore);
    }

    [Fact]
    public void TheTeamIsThreeCopiesAtFiftyWholeForEveryBattle()
    {
        var party = Three();
        var round = BattleVenueChallenge.Begin(new FrontierRecords(), AnnexVenue.Palace, new[] { 2, 0, 1 }, Draws(3));
        var team = round.TeamForBattle(party);
        Assert.Equal(new[] { "Staraptor", "Garchomp", "Lucario" }, team.Members.Select(p => p.Species.Name));
        Assert.All(team.Members, p => Assert.Equal((50, p.MaxHP), (p.Level, p.CurrentHP)));
        team.Members[0].CurrentHP = 1;
        Assert.Equal(round.TeamForBattle(party).Members[0].MaxHP, round.TeamForBattle(party).Members[0].CurrentHP);
        Assert.Equal(3, round.OpponentAsTrainer().Party.Count);
    }

    // ------------------------------------------------------------------ the desks and the halls

    [Theory]
    [InlineData("PalaceDesk", "palace_receptionist", "BattlePalace")]
    [InlineData("ArenaDesk", "arena_receptionist", "BattleArena")]
    public void ADesksRoundIsPlayedThroughTheHallToTheSeventhWin(string desk, string receptionist, string hall)
    {
        var host = new HeadlessScriptHost();
        foreach (var p in Three().Members) host.Party.Add(p);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        host.Map = Room("BattleFrontierAnnex");
        host.PlayerTile = (7, 5);
        runner.Start(ScriptLibrary.Default.Find(desk, "BattleFrontierAnnex")!, host.Map.Everyone.First(n => n.Key == receptionist));
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        Assert.NotNull(host.Venue);
        Assert.Contains(host.Log, l => l.StartsWith("warp " + hall));

        host.Map = Room(hall);
        runner.Start(ScriptLibrary.Default.Find("OnEnter", hall)!, null);
        runner.RunToEnd(20000);
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        // Seven battles, three against three, each under the venue's rules, then the record and the Battle Points
        Assert.Equal(7, host.FrontierBattles.Count);
        Assert.All(host.FrontierBattles, b => Assert.Equal((3, 3), (b.Team.Count, b.Foes[0].Party.Count)));
        bool palace = hall == "BattlePalace";
        Assert.All(host.FrontierFields, f => Assert.Equal(palace ? new FrontierConditions(Palace: true) : new FrontierConditions(TurnLimit: 3), f));
        var record = palace ? host.Frontier.Palace : host.Frontier.Arena;
        Assert.Equal((7, 1, false), (record.Streak, record.Rounds, record.InProgress));
        Assert.Equal(3, host.Frontier.BattlePoints);
        Assert.Null(host.Venue);
        Assert.Contains(host.Log, l => l.StartsWith("warp BattleFrontierAnnex"));
    }

    [Fact]
    public void ABattleLostInTheHallEndsTheRound()
    {
        var host = new HeadlessScriptHost { Fight = _ => BattleOutcome.Lost };
        foreach (var p in Three().Members) host.Party.Add(p);
        host.Venue = BattleVenueChallenge.Begin(host.Frontier, AnnexVenue.Arena, new[] { 0, 1, 2 }, Draws(51));
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        host.Map = Room("BattleArena");
        runner.Start(ScriptLibrary.Default.Find("OnEnter", "BattleArena")!, null);
        runner.RunToEnd(20000);
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        Assert.Single(host.FrontierBattles);
        Assert.Equal((0, 0, 0), (host.Frontier.Arena.Streak, host.Frontier.Arena.Rounds, host.Frontier.BattlePoints));
        Assert.Null(host.Venue);
    }

    [Fact]
    public void TheHallsLeadBackToTheirDesksInTheLobby()
    {
        var lobby = Room("BattleFrontierAnnex");
        Assert.Single(lobby.Warps);
        foreach (var (hall, x) in new[] { ("BattlePalace", 2), ("BattleArena", 12) })
        {
            var room = Room(hall);
            var door = Assert.Single(room.Warps);
            Assert.Equal(("BattleFrontierAnnex", x, 5), (door.TargetMap, door.TargetX, door.TargetY));
            Assert.True(lobby.IsWalkable(x, 5));
            foreach (var (tx, ty) in new[] { (8, 3), (8, 4), (8, 5), (8, 10) }) Assert.False(room.IsSolid(tx, ty));
        }
    }

    [Fact]
    public void AVsRecorderFileKeepsThePalaceAndTheArena()
    {
        var core = Unstarted(Of(Gender.Male, "Magikarp", 50, "Splash"), Of(Gender.Male, "Magikarp", 50, "Splash"), new BattleRandom(5), turnLimit: 3);
        var setup = new CoreSetup { PlayerParty = core.PlayerParty, Palace = true, TurnLimit = 3 };
        var file = BattleRecordFile.Opening(setup);
        Assert.Equal((true, 3), (file.Palace, file.TurnLimit));
        var back = file.ToSetup();
        Assert.Equal((true, 3), (back.Palace, back.TurnLimit));
    }
}
