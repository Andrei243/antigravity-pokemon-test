using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// The World Tournament: Sinnoh's leaders (plan 14 · B11; rulings, "The World Tournament"): a bracket of eight plays
/// through headless with the trainer AI as the player's controller, a party is untouched by a tournament, every team
/// of the file is legal, a scaled copy has the stats of level 50, the events and their lock, the screen, and the desk
/// and the hall played by <see cref="HeadlessScriptHost"/>.
/// </summary>
[Collection("MapDatabase")]
public class WorldTournamentTests
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
        party.Add(Mon("Garchomp", 70));
        party.Add(Mon("Lucario", 35));
        party.Add(Mon("Staraptor", 52));
        return party;
    }

    private static TournamentEventRecord Leaders => TournamentDatabase.Get("sinnohLeaders")!;
    private static TournamentEventRecord Champion => TournamentDatabase.Get("sinnohChampion")!;

    // What a party is, as far as a battle could change it
    private static string StateOf(Party party) => string.Join("; ", party.Members.Select(p =>
        $"{p.Species.Name} {p.Level} {p.CurrentExp} {p.CurrentHP}/{p.MaxHP} {p.Status} {p.Friendship} {p.HeldItem?.Name} {string.Join(",", p.Moves.Select(m => $"{m.Data.Name}:{m.CurrentPP}"))} {p.Id}"));

    // ------------------------------------------------------------------ the rules

    [Fact]
    public void ABracketOfEightPlaysThroughHeadlessWithThePlayersController()
    {
        for (int seed = 1; seed <= 4; seed++)
        {
            var records = new FrontierRecords();
            var party = Three();
            var t = Tournament.Begin(records, Leaders, party, new[] { 0, 1, 2 }, Draws(seed));
            Assert.Equal(3, t.Bracket.Rounds);
            Assert.True(records.Tournament.InProgress);
            bool won = t.PlayHeadless(records, Draws(seed + 100));
            Assert.True(t.Bracket.IsOver);
            Assert.Equal(won, t.PlayerWon);
            Assert.InRange(t.Bracket.Champion, 0, Tournament.Entrants - 1);
            // Seven matches in all: the player's are fought headless too, the rest of each round beside them
            Assert.False(records.Tournament.InProgress);
            Assert.Equal(won ? 1 : 0, records.Tournament.TitlesOf("sinnohLeaders"));
            Assert.Equal(won ? Leaders.Points : 0, t.PointsEarned);
        }
    }

    [Fact]
    public void APartyIsUntouchedByATournament()
    {
        var records = new FrontierRecords();
        var party = Three();
        party.Members[0].CurrentHP = 10;
        string before = StateOf(party);
        var t = Tournament.Begin(records, Leaders, party, new[] { 0, 1, 2 }, Draws(5));
        Assert.All(t.Field[Tournament.Player].Three, p => Assert.Equal((Tournament.Level, p.MaxHP), (p.Level, p.CurrentHP)));
        var team = t.TeamForBattle();
        team.Members[0].CurrentHP = 0;
        t.PlayHeadless(records, Draws(6));
        Assert.Equal(before, StateOf(party));
        // A copy fights, never the Pokémon itself
        Assert.All(t.Field[Tournament.Player].Three, p => Assert.Equal(Guid.Empty, p.Id));
    }

    [Fact]
    public void EveryTeamOfTheFileIsLegal()
    {
        var events = TournamentDatabase.Events;
        Assert.Contains(events, e => e.Id == "sinnohLeaders");
        Assert.Contains(events, e => e.Id == "sinnohChampion");
        foreach (var ev in events)
        {
            Assert.True(ev.Always.Count + ev.Pool.Count >= Tournament.Entrants - 1, $"{ev.Id} can't seat seven");
            foreach (var entrant in ev.Always.Concat(ev.Pool))
            {
                var record = TrainerDatabase.Get(entrant.Trainer);
                Assert.True(record != null, $"{ev.Id}: no trainer {entrant.Trainer}");
                Assert.True(CharacterStyles.Default.Has(entrant.Look), $"{entrant.Trainer}: no look {entrant.Look}");
                var three = Tournament.ThreeOf(record!);
                Assert.Equal(Tournament.TeamSize, three.Count);
                Assert.All(three, p => Assert.Equal(Tournament.Level, p.Level));
                // The clauses the player is held to hold for them too, and every move is one the species can learn
                Assert.Equal(FrontierRefusal.None, FrontierRules.WhyNot(three));
                foreach (var p in three)
                    Assert.True(Legality.Unlearnable(p).Count == 0, $"{entrant.Trainer}'s {p.Species.Name} can't learn {string.Join(", ", Legality.Unlearnable(p))}");
            }
        }
    }

    [Fact]
    public void AScaledCopyHasTheStatsOfLevelFifty()
    {
        foreach (int level in new[] { 12, 50, 88 })
        {
            var species = PokemonDatabase.Get("Luxray")!;
            var own = new Pokemon(species, level, new Random(9));
            var atFifty = new Pokemon(species, 50, new Random(9));
            string before = $"{own.Level} {own.MaxHP} {own.CurrentExp}";
            var copy = LevelScaling.At(own, 50);
            Assert.Equal(50, copy.Level);
            Assert.Equal((atFifty.MaxHP, atFifty.Attack, atFifty.Defense, atFifty.SpAttack, atFifty.SpDefense, atFifty.Speed),
                (copy.MaxHP, copy.Attack, copy.Defense, copy.SpAttack, copy.SpDefense, copy.Speed));
            Assert.Equal(Pokemon.GetExpForLevel(50, species.GrowthRate), copy.CurrentExp);
            Assert.Equal(copy.MaxHP, copy.CurrentHP);
            Assert.Equal(before, $"{own.Level} {own.MaxHP} {own.CurrentExp}");
        }
    }

    [Fact]
    public void TheEventsSeatTheirOwnAndTheChampionsWaitsForTheLeadersTitle()
    {
        var records = new FrontierRecords();
        Assert.True(Tournament.IsOpen(Leaders, records.Tournament));
        Assert.False(Tournament.IsOpen(Champion, records.Tournament));
        records.Tournament.Titles["sinnohLeaders"] = 1;
        Assert.True(Tournament.IsOpen(Champion, records.Tournament));

        var t = Tournament.Begin(records, Champion, Three(), new[] { 0, 1, 2 }, Draws(3));
        var ids = t.Field.Skip(1).Select(e => e.Record!.Id).ToList();
        Assert.Equal(7, ids.Distinct().Count());
        foreach (var always in Champion.Always) Assert.Contains(always.Trainer, ids);
        Assert.Equal(2, ids.Count(id => id.StartsWith("leader_")));
        // A seed draws the same field and the same bracket
        var again = Tournament.Begin(new FrontierRecords(), Champion, Three(), new[] { 0, 1, 2 }, Draws(3));
        Assert.Equal(ids, again.Field.Skip(1).Select(e => e.Record!.Id));
        Assert.Equal(t.Bracket.Seats, again.Bracket.Seats);

        var leaders = Tournament.Begin(records, Leaders, Three(), new[] { 0, 1, 2 }, Draws(4));
        Assert.All(leaders.Field.Skip(1), e => Assert.Equal("Leader", e.Record!.Class));
    }

    [Fact]
    public void TheOpponentIsATrainerWithTheirThreeAtFiftyAndNoBag()
    {
        var t = Tournament.Begin(new FrontierRecords(), Leaders, Three(), new[] { 0, 1, 2 }, Draws(8));
        var foe = t.OpponentAsTrainer();
        Assert.Equal(3, foe.Party.Count);
        Assert.All(foe.Party.Members, p => Assert.Equal(50, p.Level));
        Assert.Empty(foe.Items);
        Assert.Equal(0, foe.PrizeMoney);
        Assert.False(string.IsNullOrEmpty(foe.Look));
        Assert.Equal(3, t.TeamForBattle().Count);
    }

    // ------------------------------------------------------------------ the screen

    [Fact]
    public void TheEventsScreenChoosesAnOpenEventAndRefusesALockedOne()
    {
        var screen = new TournamentScreen();
        var record = new TournamentRecord();
        screen.OpenEvents(TournamentDatabase.Events, record);
        Assert.Equal(TournamentStep.Events, screen.Step);
        Assert.Equal(0, screen.Cursor);
        Assert.Equal(new[] { true, false }, screen.Open);
        screen.Move(0, 1);
        screen.Confirm();
        Assert.True(screen.IsActive);
        screen.Move(0, 1);
        screen.Confirm();
        Assert.False(screen.IsActive);
        Assert.Equal(1, screen.Answer);

        record.Titles["sinnohLeaders"] = 2;
        screen.OpenEvents(TournamentDatabase.Events, record);
        screen.Move(0, 1);
        screen.Confirm();
        Assert.Equal(2, screen.Answer);
        screen.OpenEvents(TournamentDatabase.Events, record);
        screen.Cancel();
        Assert.Equal(0, screen.Answer);
    }

    [Fact]
    public void TheBracketScreenLooksAtEveryEntrantsCard()
    {
        var t = Tournament.Begin(new FrontierRecords(), Leaders, Three(), new[] { 0, 1, 2 }, Draws(12));
        var screen = new TournamentScreen();
        screen.OpenBracket(t);
        Assert.Equal(TournamentStep.Tree, screen.Step);
        Assert.Equal(screen.SeatOf(t.Bracket.OpponentOf(0, Tournament.Player)), screen.Cursor);
        int start = screen.Cursor;
        screen.Move(1, 0);
        Assert.Equal((start + 4) % 8, screen.Cursor);
        screen.Confirm();
        Assert.Equal(TournamentStep.Card, screen.Step);
        screen.Cancel();
        Assert.Equal(TournamentStep.Tree, screen.Step);
        screen.Cancel();
        Assert.False(screen.IsActive);
        Assert.Equal(0, screen.Answer);
    }

    // ------------------------------------------------------------------ the desk and the hall

    [Fact]
    public void TheDesksTournamentIsPlayedThroughTheHallToTheTitle()
    {
        var host = new HeadlessScriptHost();
        foreach (var p in Three().Members) host.Party.Add(p);
        string before = StateOf(host.Party);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        host.Map = Room("BattleFrontierAnnex");
        host.PlayerTile = (12, 8);
        runner.Start(ScriptLibrary.Default.Find("TournamentDesk", "BattleFrontierAnnex")!, host.Map.Everyone.First(n => n.Key == "tournament_receptionist"));
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        Assert.NotNull(host.Tournament);
        Assert.Equal("sinnohLeaders", host.Tournament!.Event.Id);
        Assert.Contains(host.Log, l => l.StartsWith("warp WorldTournamentHall"));

        host.Map = Room("WorldTournamentHall");
        runner.Start(ScriptLibrary.Default.Find("OnEnter", "WorldTournamentHall")!, null);
        runner.RunToEnd(20000);
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        // Three matches of three against three, each won by the headless host, the title and its Battle Points
        Assert.Equal(3, host.FrontierBattles.Count);
        Assert.All(host.FrontierBattles, b => Assert.Equal((3, 3), (b.Team.Count, b.Foes[0].Party.Count)));
        Assert.All(host.FrontierFields, f => Assert.True(f?.Tournament));
        Assert.Equal((1, Leaders.Points), (host.Frontier.Tournament.TitlesOf("sinnohLeaders"), host.Frontier.BattlePoints));
        Assert.Null(host.Tournament);
        Assert.Contains(host.Log, l => l.StartsWith("warp BattleFrontierAnnex"));
        Assert.Contains(host.Transcript, l => l.Text.Contains("title of the World Tournament"));
        // The save's team is what it was: only the record and the Battle Points moved
        Assert.Equal(before, StateOf(host.Party));
    }

    [Fact]
    public void TheChampionsEventIsOnlyEnteredOnceTheLeadersTitleIsWon()
    {
        var host = new HeadlessScriptHost { TournamentChoice = 2 };
        foreach (var p in Three().Members) host.Party.Add(p);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        host.Map = Room("BattleFrontierAnnex");
        var desk = ScriptLibrary.Default.Find("TournamentDesk", "BattleFrontierAnnex")!;
        runner.Start(desk, host.Map.Everyone.First(n => n.Key == "tournament_receptionist"));
        runner.RunToEnd();
        Assert.Null(host.Tournament);

        host.Frontier.Tournament.Titles["sinnohLeaders"] = 1;
        runner.Start(desk, host.Map.Everyone.First(n => n.Key == "tournament_receptionist"));
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        Assert.Equal("sinnohChampion", host.Tournament?.Event.Id);
        Assert.Contains(host.Tournament!.Field, e => e.Record?.Id == "champion_cynthia_rematch");
    }

    [Fact]
    public void AMatchLostInTheHallEndsTheTournament()
    {
        var host = new HeadlessScriptHost { Fight = _ => BattleOutcome.Lost };
        foreach (var p in Three().Members) host.Party.Add(p);
        host.Tournament = Tournament.Begin(host.Frontier, Leaders, host.Party, new[] { 0, 1, 2 }, Draws(51));
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        host.Map = Room("WorldTournamentHall");
        runner.Start(ScriptLibrary.Default.Find("OnEnter", "WorldTournamentHall")!, null);
        runner.RunToEnd(20000);
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        Assert.Single(host.FrontierBattles);
        Assert.Equal((0, 0, false), (host.Frontier.Tournament.TitlesOf("sinnohLeaders"), host.Frontier.BattlePoints, host.Frontier.Tournament.InProgress));
        Assert.Null(host.Tournament);
    }

    [Fact]
    public void TheHallIsComeToFromTheDeskAndLeftForIt()
    {
        var hall = Room("WorldTournamentHall");
        var door = Assert.Single(hall.Warps);
        Assert.Equal(("BattleFrontierAnnex", 12, 8), (door.TargetMap, door.TargetX, door.TargetY));
        Assert.Equal("common/tournament/hall", hall.BgmTrack);
        Assert.Equal(hall.BgmTrack, PokemonPlatinumEngine.Audio.MusicDirector.Resolve(PokemonPlatinumEngine.Audio.MusicRole.Tournament, RegionDatabase.Sinnoh, PokemonPlatinumEngine.Audio.MusicLibrary.Exists));
        var lobby = Room("BattleFrontierAnnex");
        Assert.True(lobby.IsWalkable(12, 8));
        foreach (var (x, y) in new[] { (8, 3), (8, 4), (8, 5), (8, 10) }) Assert.False(hall.IsSolid(x, y));
    }
}
