using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Hoenn's three Battle Tents (plan 21 · H3's last part; rulings, "Hoenn's Battle Tents"): a tent's round of three on the
/// Palace's, the Arena's or the Factory's rules, its levels, Slateport's rentals and swaps, and each tent played from its
/// attendant through the corridor and the battle room by <see cref="HeadlessScriptHost"/>, won and lost, with the lobbies'
/// two TM givers.
/// </summary>
[Collection("Shared clocks and dice")]
public class HoennBattleTentTests
{
    private static readonly Lazy<List<Map>> Maps = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn).BuildMaps().ToList());

    private static Map Room(string name) => Maps.Value.Single(m => m.Name == name);

    private static int Count(HeadlessScriptHost host, string item) => host.Bag.GetQuantity(ItemDatabase.Get(item)!);

    private static Func<int> Draws(int seed)
    {
        var random = new Random(seed);
        return () => random.Next(0x10000);
    }

    private static Party Three(int top = 22)
    {
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Combusken")!, top, new Random(1)));
        party.Add(new Pokemon(PokemonDatabase.Get("Lombre")!, 18, new Random(2)));
        party.Add(new Pokemon(PokemonDatabase.Get("Taillow")!, 16, new Random(3)));
        return party;
    }

    [Theory]
    [InlineData(22, 30)]
    [InlineData(41, 41)]
    public void ATentsTrainersAreAtTheHighestOfTheThreeAndNeverUnderThirty(int top, int expected)
    {
        var party = Three(top);
        var round = BattleVenueChallenge.BeginTent(BattleTent.Verdanturf, new[] { 0, 1, 2 }, party, BattleVenueChallenge.TentMinimumLevel, Draws(top));
        Assert.Equal((BattleTent.Verdanturf, 3, expected), (round.Tent!.Value, round.Length, round.FoeLevel));
        Assert.Equal(3, round.Foes.Count);
        Assert.All(round.Foes, f => Assert.All(f.Pokemon, p => Assert.Equal(expected, p.Level)));
        // The player's own go in at their own levels, whole, as copies
        var team = round.TeamForBattle(party);
        Assert.Equal(new[] { top, 18, 16 }, team.Members.Select(p => p.Level));
        Assert.All(team.Members, p => Assert.NotSame(party.Members[0], p));
        Assert.Equal(new FrontierConditions(Palace: true), round.Conditions);
        Assert.Equal(new FrontierConditions(TurnLimit: ArenaJudge.Turns),
            BattleVenueChallenge.BeginTent(BattleTent.Fallarbor, new[] { 0, 1, 2 }, party, 30, Draws(2)).Conditions);
    }

    [Fact]
    public void ATentKeepsNoStreakAndEarnsNoBattlePoints()
    {
        var records = new FrontierRecords();
        var round = BattleVenueChallenge.BeginTent(BattleTent.Fallarbor, new[] { 0, 1, 2 }, Three(), 30, Draws(5));
        Assert.False(round.Won(records));
        Assert.False(round.Won(records));
        Assert.True(round.Won(records));
        round.Finish(records, won: true);
        Assert.Equal((0, 0, 0, 0), (records.Arena.Streak, records.Arena.Rounds, records.Palace.Streak, round.PointsEarned));
    }

    [Fact]
    public void SlateportOffersSixRentalsAtThirtyAndASwapAfterAWin()
    {
        var round = BattleVenueChallenge.BeginTent(BattleTent.Slateport, Array.Empty<int>(), Three(50), 30, Draws(9));
        Assert.Equal(6, round.Offer.Count);
        Assert.All(round.Offer, p => Assert.Equal(30, p.Level));
        Assert.Equal(6, round.Offer.Select(p => p.Species.Name).Distinct().Count());
        // Slateport's trainers are met at thirty whatever the player's own team is
        Assert.Equal(30, round.FoeLevel);
        Assert.Equal(new FrontierConditions(), round.Conditions);
        round.Rent(new[] { 4, 0, 2 });
        var team = round.TeamForBattle(new Party());
        Assert.Equal(new[] { round.Offer[4].Species.Name, round.Offer[0].Species.Name, round.Offer[2].Species.Name }, team.Members.Select(p => p.Species.Name));
        // No swap before a win; after one, a rental for one of the trainer just beaten
        Assert.False(round.Trade(0, 0));
        round.Won(new FrontierRecords());
        string theirs = round.Foes[0].Pokemon[1].Species.Name;
        Assert.True(round.Trade(2, 1));
        Assert.Equal(theirs, round.Rented[2].Species.Name);
        Assert.False(round.Trade(3, 0));
    }

    [Theory]
    [InlineData("VerdanturfTown", "verdanturf_town", "verdanturf_tent_attendant", "Nest Ball", BattleTent.Verdanturf)]
    [InlineData("FallarborTown", "fallarbor_town", "fallarbor_tent_attendant", "Hyper Potion", BattleTent.Fallarbor)]
    [InlineData("SlateportCity", "slateport_city", "slateport_tent_attendant", "Full Heal", BattleTent.Slateport)]
    public void ATentIsPlayedFromItsAttendantThroughTheBattleRoomToItsPrize(string town, string key, string attendant, string prize, BattleTent tent)
    {
        bool ownThree = tent != BattleTent.Slateport;
        var host = new HeadlessScriptHost { Region = RegionDatabase.Hoenn };
        foreach (var p in Three().Members) host.Party.Add(p);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        host.Map = Room(town + "_BattleTentLobby");
        host.PlayerTile = (6, 6);
        runner.Start(ScriptLibrary.Default.Find("Attendant", key + "_battle_tent_lobby")!, host.Map.Everyone.First(n => n.Key == attendant));
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        Assert.Equal(tent, host.Venue!.Tent);
        Assert.Contains(host.Log, l => l.StartsWith("warp " + town + "_BattleTentCorridor"));

        host.Map = Room(town + "_BattleTentCorridor");
        runner.Start(ScriptLibrary.Default.Find("OnEnter", key + "_battle_tent_corridor")!, null);
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        Assert.Contains(host.Log, l => l.StartsWith("warp " + town + "_BattleTentBattleRoom"));
        if (!ownThree) Assert.Equal(3, host.Venue.Rented.Count);

        host.Map = Room(town + "_BattleTentBattleRoom");
        runner.Start(ScriptLibrary.Default.Find("OnEnter", key + "_battle_tent_battle_room")!, null);
        runner.RunToEnd(20000);
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        Assert.Equal(3, host.FrontierBattles.Count);
        Assert.All(host.FrontierBattles, b => Assert.Equal((3, 3), (b.Team.Count, b.Foes[0].Party.Count)));
        // The player's own three go in at their own levels; Slateport's rentals at thirty
        if (ownThree) Assert.Equal(new[] { 22, 18, 16 }, host.FrontierBattles[0].Team.Members.Select(p => p.Level));
        else Assert.All(host.FrontierBattles[0].Team.Members, p => Assert.Equal(30, p.Level));
        Assert.Equal(1, Count(host, prize));
        Assert.Null(host.Venue);
        Assert.Contains(host.Log, l => l.StartsWith("warp " + town + "_BattleTentLobby"));
        // Nothing on the Frontier's boards
        Assert.Equal((0, 0, 0), (host.Frontier.Palace.Streak, host.Frontier.Arena.Streak, host.Frontier.BattlePoints));
    }

    [Fact]
    public void ABattleLostInATentEndsTheRoundWithNoPrize()
    {
        var host = new HeadlessScriptHost { Fight = _ => BattleOutcome.Lost, Region = RegionDatabase.Hoenn };
        foreach (var p in Three().Members) host.Party.Add(p);
        host.Venue = BattleVenueChallenge.BeginTent(BattleTent.Fallarbor, new[] { 0, 1, 2 }, host.Party, 30, Draws(51));
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        host.Map = Room("FallarborTown_BattleTentBattleRoom");
        host.PlayerTile = (4, 8);
        runner.Start(ScriptLibrary.Default.Find("OnEnter", "fallarbor_town_battle_tent_battle_room")!, null);
        runner.RunToEnd(20000);
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        Assert.Single(host.FrontierBattles);
        Assert.Equal(0, Count(host, "Hyper Potion"));
        Assert.Null(host.Venue);
    }

    [Fact]
    public void ARoomComeToWithNoRoundUnderWayDoesNothing()
    {
        foreach (var key in new[] { "verdanturf_town", "fallarbor_town", "slateport_city" })
            foreach (var room in new[] { "_battle_tent_corridor", "_battle_tent_battle_room" })
            {
                var host = new HeadlessScriptHost { Region = RegionDatabase.Hoenn };
                var runner = new ScriptRunner(ScriptLibrary.Default, host);
                runner.Start(ScriptLibrary.Default.Find("OnEnter", key + room)!, null);
                runner.RunToEnd();
                Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
                Assert.DoesNotContain(host.Log, l => l.StartsWith("warp "));
            }
    }

    [Theory]
    [InlineData("VerdanturfTown", "verdanturf_town_battle_tent_lobby", "girl_1", "AttractGiver", "TM45", "FLAG_RECEIVED_TM_ATTRACT")]
    [InlineData("SlateportCity", "slateport_city_battle_tent_lobby", "sailor", "TormentGiver", "TM41", "FLAG_RECEIVED_TM_TORMENT")]
    public void ALobbysGiverHandsOverTheirTmOnce(string town, string area, string person, string script, string tm, string flag)
    {
        var host = new HeadlessScriptHost { Region = RegionDatabase.Hoenn, Map = Room(town + "_BattleTentLobby") };
        var giver = host.Map.Everyone.First(n => n.Key == person);
        Assert.Equal(script, giver.Script);
        host.PlayerTile = (giver.GridX, giver.GridY + 1);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, area)!, giver);
        runner.RunToEnd();
        Assert.Equal(1, Count(host, tm));
        Assert.True(host.Story.Has(flag));
        
        runner.Start(ScriptLibrary.Default.Find(script, area)!, giver);
        runner.RunToEnd();
        Assert.Equal(1, Count(host, tm));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BirchRegistersHisNumberInTheLabOnceThePokeNavHasMatchCall(bool pokenav)
    {
        // H3's last part: Emerald's EventScript_RegisterProfBirch, whose call (phone.txt, ProfBirch) is the book's prof_birch
        var host = new HeadlessScriptHost { Region = RegionDatabase.Hoenn, Map = Room("LittlerootTown_ProfessorBirchsLab") };
        host.Story.SetVar("VAR_BIRCH_LAB_STATE", 5);
        if (pokenav) host.Story.Set("FLAG_SYS_POKENAV_GET");
        var birch = host.Map.Everyone.First(n => n.Key == "birchs_lab_birch");
        host.PlayerTile = (birch.GridX, birch.GridY + 1);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("Birch", "littleroot_town_professor_birchs_lab")!, birch);
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        Assert.Equal(pokenav, host.Story.Has(Pokegear.ContactFlag("prof_birch")));
        Assert.Contains(PhoneBook.Hoenn, c => c.Id == "prof_birch");
        Assert.NotNull(ScriptLibrary.Default.Find("ProfBirch", "phone"));
    }

    [Fact]
    public void TheTentsRoomsAreBuiltWithTheirPeople()
    {
        foreach (var town in new[] { "VerdanturfTown", "FallarborTown", "SlateportCity" })
        {
            var room = Room(town + "_BattleTentBattleRoom");
            // The opponent of ours waits off the floor; Emerald's stand-in for the player is left out
            Assert.Contains(room.Everyone, n => n.Key == "opponent");
            Assert.DoesNotContain(room.Everyone, n => n.Key != null && n.Key.EndsWith("_battle_player"));
            Assert.NotNull(Room(town + "_BattleTentCorridor"));
        }
    }
}
