using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The Battle Frontier island's seven halls (plan 21 · H12, part m; rulings, "The Battle Frontier island's halls"): each
/// door stays a locked warp and is read, its attendant crossing a holder of the Frontier Pass to the hall's lobby at
/// Sinnoh's Battle Frontier; the Hoenn guide on Sinnoh's grounds sails back to the door come through; the Ranking Hall's
/// boards open the same records as Sinnoh's lobbies. A crossing names the player's region, so the class runs with the
/// shared clocks and dice.
/// </summary>
[Collection("Shared clocks and dice")]
public class HoennFrontierHallsTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private static readonly Lazy<World> Hoenn = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn));
    private static readonly Lazy<Map> Island = new(() => Hoenn.Value.BuildMaps().Single(m => m.Name == "HoennBattleFrontier"));

    /// <summary>Each hall: its door on the island, its script, the number it keeps, and the lobby and tile it crosses to.</summary>
    public static readonly TheoryData<string, int, int, string, int, string, int, int> Halls = new()
    {
        { "Tower", 72, 14, "TowerDoor", 1, "BattleTower", 15, 19 },
        { "Dome", 19, 17, "DomeDoor", 2, "BattleFrontierAnnex", 7, 9 },
        { "Palace", 101, 56, "PalaceDoor", 3, "BattleFrontierAnnex", 7, 9 },
        { "Arena", 95, 29, "ArenaDoor", 4, "BattleFrontierAnnex", 7, 9 },
        { "Factory", 11, 38, "FactoryDoor", 5, "BattleFactory", 1, 7 },
        { "Pike", 42, 27, "PikeDoor", 6, "BattleFrontierAnnex", 7, 9 },
        { "Pyramid", 114, 14, "PyramidDoor", 7, "BattleFrontierAnnex", 7, 9 },
    };

    private static (StoryState Story, Party Party) Champion()
    {
        var story = new StoryState();
        StoryMigration.BeginNewGame(story, Scripts);
        story.Set("HoennHallOfFame");
        story.SetBadges("Hoenn", 0xFF);
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Swampert")!, 50, new Random(1)));
        return (story, party);
    }

    private static HeadlessScriptHost Run(Map map, string script, string file, StoryState story, Party party, (int X, int Y) tile, params int[] answers) => Run(map, script, file, story, party, tile, null, answers);

    private static HeadlessScriptHost Run(Map map, string script, string file, StoryState story, Party party, (int X, int Y) tile, NPC? self, params int[] answers)
    {
        var host = new HeadlessScriptHost(story, party, new Inventory()) { Map = map, PlayerTile = tile, NeedsPokemon = true };
        foreach (int answer in answers) host.Answers.Enqueue(answer);
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find(script, file)!, self);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    private static NPC Guide(Map grounds) => grounds.Everyone.First(n => n.Key == "battle_frontier.HoennGuide");

    [Theory]
    [MemberData(nameof(Halls))]
    public void EachHallsDoorStaysAWarpThatIsShutAndIsReadBeforeAnOpenTile(string hall, int x, int y, string script, int number, string lobby, int lx, int ly)
    {
        var island = Island.Value;
        island.ApplyDoors(_ => true);
        // (a locked door is no way at all, and still its building's door)
        Assert.Empty(island.WarpsAt(x, y));
        Assert.True(island.BuildingKinds.ContainsKey((x, y)), $"the {hall}'s door is no building's");
        Assert.Equal(script, island.TileScripts.GetValueOrDefault((x, y)));
        Assert.True(island.IsWalkable(x, y + 1), $"nobody stands before the {hall}'s door");
        Assert.NotNull(Scripts.Find(script, island.ScriptFileAt(x, y)));
        _ = (number, lobby, lx, ly);
    }

    [Theory]
    [MemberData(nameof(Halls))]
    public void AHolderOfThePassCrossesToTheHallsLobbyInSinnoh(string hall, int x, int y, string script, int number, string lobby, int lx, int ly)
    {
        var (story, party) = Champion();
        story.Set("FLAG_RECEIVED_FRONTIER_PASS");
        var island = Island.Value;
        var host = Run(island, script, island.ScriptFileAt(x, y), story, party, (x, y + 1), 0);
        Assert.Contains(host.Log, l => l.StartsWith($"travel to {RegionDatabase.Sinnoh} {lobby} {lx} {ly}"));
        Assert.Equal(number, story.Var("VAR_HOENN_FRONTIER_HALL"));
        PlayerIdentity.SetRival(null);

        // Turned down, nothing happens; with no pass, the door is only shut
        var (later, laterParty) = Champion();
        later.Set("FLAG_RECEIVED_FRONTIER_PASS");
        var no = Run(island, script, island.ScriptFileAt(x, y), later, laterParty, (x, y + 1), 1);
        Assert.DoesNotContain(no.Log, l => l.StartsWith("travel"));
        var (shut, shutParty) = Champion();
        var none = Run(island, script, island.ScriptFileAt(x, y), shut, shutParty, (x, y + 1));
        Assert.DoesNotContain(none.Log, l => l.StartsWith("travel"));
        Assert.Empty(none.Asked);
        _ = hall;
    }

    [Theory]
    [MemberData(nameof(Halls))]
    public void EveryLobbyIsSinnohsAndWalkedFromItsArrivalToTheGrounds(string hall, int x, int y, string script, int number, string lobby, int lx, int ly)
    {
        Assert.Contains(lobby, RegionDatabase.Get(RegionDatabase.Sinnoh)!.Maps);
        var room = MapDatabase.Get(lobby);
        Assert.Equal(lobby, room.Name);
        Assert.True(room.IsWalkable(lx, ly), $"the {hall}'s arrival in {lobby} is blocked");
        var walked = WorldWalk.From(MapDatabase.Get, new[] { new MapSpot(lobby, lx, ly, Direction.Up) });
        Assert.Contains(walked.Keys, m => m.Name == "BattleFrontier");
        _ = (x, y, script, number);
    }

    [Theory]
    [InlineData(0, 19, 67)]
    [InlineData(1, 72, 15)]
    [InlineData(5, 11, 39)]
    [InlineData(7, 114, 15)]
    public void TheHoennGuideSailsBackToTheDoorComeThrough(int hall, int x, int y)
    {
        var (story, party) = Champion();
        story.Set("FLAG_RECEIVED_SS_TICKET");
        story.SetVar("VAR_HOENN_FRONTIER_HALL", hall);
        var grounds = MapDatabase.Get("BattleFrontier");
        var host = Run(grounds, "HoennGuide", "battle_frontier", story, party, (48, 52), Guide(grounds), 0);
        Assert.Contains(host.Log, l => l.StartsWith($"travel to {RegionDatabase.Hoenn} HoennBattleFrontier {x} {y}"));
        Assert.True(Island.Value.IsWalkable(x, y));
        PlayerIdentity.SetRival(null);
    }

    [Fact]
    public void TheHoennGuideStillSailsToLilycoveAndOnlyThereWithoutTheTicket()
    {
        var (story, party) = Champion();
        story.Set("FLAG_RECEIVED_SS_TICKET");
        var grounds = MapDatabase.Get("BattleFrontier");
        var harbour = Run(grounds, "HoennGuide", "battle_frontier", story, party, (48, 52), Guide(grounds), 1);
        Assert.Contains(harbour.Log, l => l.StartsWith($"travel to {RegionDatabase.Hoenn} LilycoveCity_Harbor 12 12"));

        var (bare, bareParty) = Champion();
        var asked = Run(grounds, "HoennGuide", "battle_frontier", bare, bareParty, (48, 52), Guide(grounds), 0);
        Assert.DoesNotContain(asked.Asked, a => a.Answer == "FRONTIER ISLAND");
        Assert.Contains(asked.Log, l => l.StartsWith($"travel to {RegionDatabase.Hoenn} LilycoveCity_Harbor 12 12"));
        PlayerIdentity.SetRival(null);
    }

    [Theory]
    [InlineData("TowerRecords", "BattleTower")]
    [InlineData("FactoryRecords", "BattleFactory")]
    public void TheRankingHallsBoardsOpenTheSameRecordsAsSinnohsLobbies(string board, string lobby)
    {
        var (story, party) = Champion();
        var hall = Hoenn.Value.BuildMaps().Single(m => m.Name == "BattleFrontier_RankingHall");
        Assert.Contains(board, hall.TileScripts.Values);
        var here = Run(hall, board, "battle_frontier_ranking_hall", story, party, (5, 5));
        var there = Run(MapDatabase.Get(lobby), "RecordsMachine", lobby, story, party, (5, 5));
        string Opened(HeadlessScriptHost h) => h.Log.Single(l => l.StartsWith("frontier records"));
        Assert.Equal(Opened(there), Opened(here));
    }
}
