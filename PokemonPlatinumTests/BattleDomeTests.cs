using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// The Battle Dome and its bracket (plan 14 · B8; rulings, "The Battle Dome"): a bracket of sixteen always comes to one
/// winner and is drawn the same for a seed, a round lost ends the player's tournament, the other matches are fought by
/// the battle's core, the record and the Battle Points, the bracket's screen, and the desk and the hall played by
/// <see cref="HeadlessScriptHost"/>.
/// </summary>
[Collection("MapDatabase")]
public class BattleDomeTests
{
    private static Map Room(string name) => GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    private static Func<int> Draws(int seed)
    {
        var random = new Random(seed);
        return () => random.Next(0x10000);
    }

    private static Pokemon Mon(string species, int level) => new(PokemonDatabase.Get(species)!, level, new Random(level));

    private static Party Three()
    {
        var party = new Party();
        party.Add(Mon("Garchomp", 60));
        party.Add(Mon("Lucario", 55));
        party.Add(Mon("Staraptor", 52));
        return party;
    }

    // ------------------------------------------------------------------ the bracket

    [Fact]
    public void ABracketOfSixteenAlwaysComesToOneWinner()
    {
        for (int seed = 1; seed <= 50; seed++)
        {
            var bracket = Bracket.Draw(16, Draws(seed));
            Assert.Equal((16, 4), (bracket.Size, bracket.Rounds));
            Assert.Equal(new[] { 8, 4, 2, 1 }, Enumerable.Range(0, 4).Select(bracket.MatchesIn));
            Assert.Equal(Enumerable.Range(0, 16), bracket.Seats.OrderBy(s => s));
            var coin = Draws(seed * 7);
            bracket.DecideAll((a, b) => (coin() & 1) == 0 ? a : b);
            Assert.True(bracket.IsOver);
            Assert.InRange(bracket.Champion, 0, 15);
            Assert.Single(Enumerable.Range(0, 16), e => bracket.StillIn(e));
            Assert.Equal(bracket.Rounds, bracket.ReachedRound(bracket.Champion));
            // Half the field goes out in each round
            Assert.Equal(new[] { 8, 4, 2, 1 }, Enumerable.Range(0, 4).Select(r => Enumerable.Range(0, 16).Count(e => bracket.ReachedRound(e) == r)));
        }
    }

    [Fact]
    public void ABracketIsDrawnTheSameForASeedAndSeatsOnlyAPowerOfTwo()
    {
        Assert.Equal(Bracket.Draw(16, Draws(42)).Seats, Bracket.Draw(16, Draws(42)).Seats);
        Assert.NotEqual(Bracket.Draw(16, Draws(42)).Seats, Bracket.Draw(16, Draws(43)).Seats);
        Assert.Equal(3, Bracket.Of(8).Rounds);
        Assert.Throws<ArgumentOutOfRangeException>(() => Bracket.Of(12));

        // Who meets whom follows the winners round by round, and a winner must stand in the match
        var bracket = Bracket.Of(8);
        Assert.Equal((0, 1), bracket.Match(0, 0));
        Assert.Equal((-1, -1), bracket.Match(1, 0));
        bracket.Decide(0, 0, 1);
        bracket.Decide(0, 1, 2);
        Assert.Equal((1, 2), bracket.Match(1, 0));
        Assert.Equal(2, bracket.OpponentOf(1, 1));
        Assert.Throws<ArgumentException>(() => bracket.Decide(1, 0, 5));
        Assert.False(bracket.StillIn(0));
        Assert.Equal(0, bracket.Round);
    }

    // ------------------------------------------------------------------ the tournament

    [Fact]
    public void ATournamentDrawsFifteenTrainersOfThreeTheSameForASeed()
    {
        var records = new FrontierRecords();
        var dome = BattleDomeChallenge.Begin(records, new[] { 0, 1, 2 }, Draws(5));
        Assert.Equal(16, dome.Field.Count);
        Assert.True(dome.Field[0].IsPlayer);
        Assert.Equal(15, dome.Field.Skip(1).Select(e => e.Trainer).Distinct().Count());
        Assert.All(dome.Field.Skip(1), e => Assert.Equal(3, e.Opponent!.Pokemon.Count));
        // The first tournament draws from the first room's row
        int[] row = FrontierDatabase.TowerRooms[0];
        Assert.All(dome.Field.Skip(1), e => Assert.InRange(e.Trainer, row[0], row[1]));
        Assert.True(records.Dome.InProgress);

        var again = BattleDomeChallenge.Begin(new FrontierRecords(), new[] { 0, 1, 2 }, Draws(5));
        Assert.Equal(dome.Field.Select(e => e.Trainer), again.Field.Select(e => e.Trainer));
        Assert.Equal(dome.Bracket.Seats, again.Bracket.Seats);
        Assert.NotNull(dome.Opponent);
        Assert.Equal(0, dome.Round);
    }

    [Fact]
    public void ARoundLostEndsThePlayersTournamentAndTheStreak()
    {
        var records = new FrontierRecords();
        records.Dome.Streak = 3;
        var party = Three();
        var dome = BattleDomeChallenge.Begin(records, new[] { 0, 1, 2 }, Draws(9));
        Assert.Equal(3, dome.StreakBefore);
        dome.Lost(party, Draws(10));
        Assert.False(dome.PlayerStillIn);
        Assert.Null(dome.Opponent);
        // The rest of the first round was fought all the same
        Assert.Equal(1, dome.Round);
        Assert.Equal(7, dome.Fought.Count);
        dome.Finish(records, won: false);
        Assert.Equal((0, false), (records.Dome.Streak, records.Dome.InProgress));
        Assert.Equal(0, dome.PointsEarned);
    }

    [Fact]
    public void FourWinsTakeTheTournamentAndItsBattlePoints()
    {
        var records = new FrontierRecords();
        records.Dome.Streak = 1;
        var party = Three();
        var dome = BattleDomeChallenge.Begin(records, new[] { 0, 1, 2 }, Draws(11));
        var draw = Draws(12);
        for (int round = 0; round < 3; round++) Assert.False(dome.Won(records, party, draw));
        Assert.True(dome.Won(records, party, draw));
        Assert.True(dome.PlayerWon);
        Assert.Equal(BattleDomeChallenge.Player, dome.Bracket.Champion);
        // Eleven matches were fought with nobody watching: seven, three and one
        Assert.Equal(11, dome.Fought.Count);
        Assert.All(dome.Fought, f => Assert.True(f.Winner == f.A || f.Winner == f.B));
        dome.Finish(records, won: true);
        Assert.Equal((2, 2, 1, 4, false), (records.Dome.Streak, records.Dome.Best, records.Dome.Won, records.Dome.Matches, records.Dome.InProgress));
        Assert.Equal(4, dome.PointsEarned);
        Assert.Equal((3, 10), (BattleDomeChallenge.PointsFor(0), BattleDomeChallenge.PointsFor(20)));
    }

    [Fact]
    public void ATournamentWalkedOutOnEndsTheStreakWhenTheNextIsBegun()
    {
        var records = new FrontierRecords();
        records.Dome.Streak = 4;
        BattleDomeChallenge.Begin(records, new[] { 0, 1, 2 }, Draws(1));
        var next = BattleDomeChallenge.Begin(records, new[] { 0, 1, 2 }, Draws(2));
        Assert.Equal(0, next.StreakBefore);
    }

    [Fact]
    public void AMatchWithNobodyWatchingIsFoughtByTheCoreAndTheSameForTheSameDraws()
    {
        var party = Three();
        var dome = BattleDomeChallenge.Begin(new FrontierRecords(), new[] { 0, 1, 2 }, Draws(21));
        int first = dome.FightHeadless(1, 2, party, Draws(22));
        int again = dome.FightHeadless(1, 2, party, Draws(22));
        Assert.Contains(first, new[] { 1, 2 });
        Assert.Equal(first, again);
        // Nothing of either trainer's three was spent: the battle had copies
        Assert.All(dome.Field[1].Opponent!.Pokemon.Concat(dome.Field[2].Opponent!.Pokemon), p => Assert.Equal(p.MaxHP, p.CurrentHP));
    }

    [Fact]
    public void TwoOfTheThreeAreSentInAndTheTeamIsCopiesAtFifty()
    {
        var party = Three();
        var dome = BattleDomeChallenge.Begin(new FrontierRecords(), new[] { 0, 1, 2 }, Draws(31));
        dome.Send(new[] { 2, 0 });
        var team = dome.TeamForBattle(party);
        Assert.Equal(new[] { "Staraptor", "Garchomp" }, team.Members.Select(p => p.Species.Name));
        Assert.All(team.Members, p => Assert.Equal(50, p.Level));
        Assert.Equal(60, party.Members[0].Level);
        // Anything but two places of the three sends the first two
        dome.Send(new[] { 1 });
        Assert.Equal(new[] { 0, 1 }, dome.Sent);
        var foe = dome.OpponentAsTrainer(party);
        Assert.Equal(2, foe.Party.Count);
        Assert.All(foe.Party.Members, p => Assert.Contains(dome.Opponent!.Opponent!.Pokemon, o => o.Species == p.Species));
    }

    [Fact]
    public void AnOpponentSendsInTheTwoThatDoBestAgainstThePlayers()
    {
        // A Garchomp (Dragon and Ground) is felled by Ice: the Glaceon and its Ice Beam go in before a Bidoof's Tackle
        var glaceon = Mon("Glaceon", 50);
        glaceon.Moves.Clear();
        glaceon.Moves.Add(new Move(MoveDatabase.Get("Ice Beam")!));
        var bidoof = Mon("Bidoof", 50);
        bidoof.Moves.Clear();
        bidoof.Moves.Add(new Move(MoveDatabase.Get("Tackle")!));
        var magikarp = Mon("Magikarp", 50);
        magikarp.Moves.Clear();
        magikarp.Moves.Add(new Move(MoveDatabase.Get("Splash")!));
        var two = BattleDomeChallenge.ChooseTwo(new[] { magikarp, bidoof, glaceon }, new[] { Mon("Garchomp", 50) });
        Assert.Equal(new[] { bidoof, glaceon }, two);
    }

    // ------------------------------------------------------------------ the screen

    [Fact]
    public void TheBracketsPickSendsInTwoAndItsTreeIsWalkedRound()
    {
        var party = Three();
        var dome = BattleDomeChallenge.Begin(new FrontierRecords(), new[] { 0, 1, 2 }, Draws(41));
        var screen = new BracketScreen();
        screen.Open(dome, dome.OwnThree(party), pick: true);
        Assert.Equal(BracketStep.Pick, screen.Step);
        // BATTLE before two are chosen does nothing
        screen.Move(0, -1);
        Assert.Equal(BracketScreen.GoPlace, screen.PickCursor);
        screen.Confirm();
        Assert.True(screen.IsActive);
        // Back looks at the tree, a card opens on it, and back again comes to the pick
        screen.Cancel();
        Assert.Equal(BracketStep.Tree, screen.Step);
        Assert.Equal(screen.SeatOf(dome.Bracket.OpponentOf(0, BattleDomeChallenge.Player)), screen.Cursor);
        screen.Confirm();
        Assert.Equal(BracketStep.Card, screen.Step);
        screen.Cancel();
        int seat = screen.Cursor;
        screen.Move(1, 0);
        Assert.Equal((seat + 8) % 16, screen.Cursor);
        for (int i = 0; i < 8; i++) screen.Move(0, 1);
        Assert.Equal((seat + 8) % 16, screen.Cursor);
        screen.Cancel();
        Assert.Equal(BracketStep.Pick, screen.Step);
        // The second and the third, and then BATTLE
        screen.Move(0, 1);
        screen.Move(0, 1);
        screen.Confirm();
        screen.Move(0, 1);
        screen.Confirm();
        Assert.True(screen.Ready);
        Assert.Equal(BracketScreen.GoPlace, screen.PickCursor);
        screen.Confirm();
        Assert.False(screen.IsActive);
        Assert.Equal(1, screen.Answer);
        Assert.Equal(new[] { 1, 2 }, screen.Outcome);

        // Opened only to look, Back closes it and sends nobody
        screen.Open(dome, dome.OwnThree(party), pick: false);
        Assert.Equal(BracketStep.Tree, screen.Step);
        screen.Cancel();
        Assert.False(screen.IsActive);
        Assert.Empty(screen.Outcome);
    }

    // ------------------------------------------------------------------ the desk and the hall

    [Fact]
    public void TheDesksTournamentIsPlayedThroughTheHallToTheTitle()
    {
        var host = new HeadlessScriptHost();
        foreach (var p in Three().Members) host.Party.Add(p);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        host.Map = Room("BattleFrontierAnnex");
        host.PlayerTile = (7, 5);
        runner.Start(ScriptLibrary.Default.Find("Receptionist", "BattleFrontierAnnex")!, host.Map.Everyone.First(n => n.Key == "dome_receptionist"));
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        Assert.NotNull(host.Dome);
        Assert.Contains(host.Log, l => l.StartsWith("warp BattleDome"));

        host.Map = Room("BattleDome");
        runner.Start(ScriptLibrary.Default.Find("OnEnter", "BattleDome")!, null);
        runner.RunToEnd(20000);
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        // Four matches fought (each won by the headless host), the title, three Battle Points, and the tournament let go
        Assert.Equal(4, host.FrontierBattles.Count);
        Assert.All(host.FrontierBattles, b => Assert.Equal(2, b.Team.Count));
        Assert.Equal((1, 1, 3), (host.Frontier.Dome.Streak, host.Frontier.Dome.Won, host.Frontier.BattlePoints));
        Assert.Null(host.Dome);
        Assert.Contains(host.Log, l => l.StartsWith("warp BattleFrontierAnnex"));
        Assert.Contains(host.Transcript, l => l.Text.Contains("champion of the Battle Dome"));
    }

    [Fact]
    public void AMatchLostInTheHallEndsTheTournament()
    {
        var host = new HeadlessScriptHost { Fight = _ => BattleOutcome.Lost };
        foreach (var p in Three().Members) host.Party.Add(p);
        host.Dome = BattleDomeChallenge.Begin(host.Frontier, new[] { 0, 1, 2 }, Draws(51));
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        host.Map = Room("BattleDome");
        runner.Start(ScriptLibrary.Default.Find("OnEnter", "BattleDome")!, null);
        runner.RunToEnd(20000);
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        Assert.Single(host.FrontierBattles);
        Assert.Equal((0, 0, 0), (host.Frontier.Dome.Streak, host.Frontier.Dome.Won, host.Frontier.BattlePoints));
        Assert.Null(host.Dome);
    }

    [Fact]
    public void TheAnnexIsComeToByTheGuideAndLeftByItsDoor()
    {
        var lobby = Room("BattleFrontierAnnex");
        // (Its other way is the Masters' Hall's door in the west wall, plan 14 · B15)
        var door = Assert.Single(lobby.Warps, w => w.TargetMap != "MastersHall");
        Assert.Equal(("BattleFrontier", 44, 63), (door.TargetMap, door.TargetX, door.TargetY));
        Assert.True(lobby.IsWalkable(7, 9));
        var hall = Room("BattleDome");
        Assert.Equal("BattleFrontierAnnex", Assert.Single(hall.Warps).TargetMap);
        foreach (var (x, y) in new[] { (8, 3), (8, 4), (8, 5), (8, 10) }) Assert.False(hall.IsSolid(x, y));
        var guide = ScriptLibrary.Default.Find("AnnexGuide", "battle_frontier");
        Assert.NotNull(guide);
        Assert.Contains(guide!.Everything(), i => i.Op == Op.Warp && i.Name == "BattleFrontierAnnex");
    }
}
