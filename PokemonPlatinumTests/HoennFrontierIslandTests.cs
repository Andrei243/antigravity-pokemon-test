using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The Battle Frontier island of Hoenn's post-game (plan 21 · H12, part b): the ferry from Lilycove City with the S.S.
/// Ticket and back, the island's two halves and the rooms of its grounds, its seven halls shut, Scott's pass in the
/// reception gate, his Battle Points and his crossing to Sinnoh's Frontier, and the strange tree watered with the Wailmer
/// Pail. Scott's crossing names the player's region, so the class runs with the shared clocks and dice.
/// </summary>
[Collection("Shared clocks and dice")]
public class HoennFrontierIslandTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private static readonly Lazy<World> Hoenn = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn));

    private static readonly string[] Rooms =
    {
        "BattleFrontier_ReceptionGate", "BattleFrontier_PokemonCenter_1F", "BattleFrontier_PokemonCenter_2F", "BattleFrontier_Mart",
        "BattleFrontier_ExchangeServiceCorner", "BattleFrontier_ScottsHouse", "BattleFrontier_RankingHall", "BattleFrontier_Lounge1",
        "BattleFrontier_Lounge2", "BattleFrontier_Lounge3", "BattleFrontier_Lounge4", "BattleFrontier_Lounge5", "BattleFrontier_Lounge6",
        "BattleFrontier_Lounge7", "BattleFrontier_Lounge8",
    };

    private sealed class Game
    {
        private readonly Dictionary<string, Map> maps = new(StringComparer.OrdinalIgnoreCase);
        public StoryState Story { get; } = new();
        public Party Party { get; } = new();
        public Inventory Bag { get; } = new();
        public Map Map { get; private set; } = null!;
        public (int X, int Y) Tile { get; set; }
        public BattleOutcome Fight { get; set; } = BattleOutcome.Won;

        public Game(string map, int x, int y)
        {
            foreach (var m in Hoenn.Value.BuildMaps()) maps[m.Name] = m;
            StoryMigration.BeginNewGame(Story, Scripts);
            Story.Set("HoennHallOfFame");
            Story.SetBadges("Hoenn", 0xFF);
            Party.Add(new Pokemon(PokemonDatabase.Get("Swampert")!, 50, new Random(1)));
            Arrive(map, x, y);
        }

        public void Arrive(string name, int x, int y)
        {
            Map = maps[name];
            Map.ForgetForced();
            Map.ApplyPresence(Story.Has);
            Map.ApplyDoors(Story.Has);
            Tile = (x, y);
        }

        public NPC? Present(string key) => Map.NPCs.FirstOrDefault(n => n.Key == key);

        public HeadlessScriptHost Talk(string key, params int[] answers)
        {
            var npc = Map.NPCs.First(n => n.Key == key);
            Tile = (npc.GridX, npc.GridY + 1);
            var host = new HeadlessScriptHost(Story, Party, Bag)
            {
                Map = Map,
                PlayerTile = Tile,
                MapNamed = name => maps.GetValueOrDefault(name),
                Fight = _ => Fight,
                NeedsPokemon = true
            };
            foreach (int answer in answers) host.Answers.Enqueue(answer);
            var runner = new ScriptRunner(Scripts, host);
            runner.Start(Scripts.Find(FieldScripts.For(npc)!, npc.ScriptFile ?? Map.ScriptFileAt(npc.GridX, npc.GridY))!, npc);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            foreach (var map in maps.Values.Distinct())
            {
                map.ApplyPresence(Story.Has);
                map.ApplyDoors(Story.Has);
            }
            if (host.Map is { } there && there != Map) Arrive(there.Name, host.PlayerTile.X, host.PlayerTile.Y);
            return host;
        }
    }

    [Fact]
    public void TheIslandAndItsRoomsAreHoennsAndOpen()
    {
        var hoenn = RegionDatabase.Get(RegionDatabase.Hoenn)!;
        Assert.Contains("HoennBattleFrontier", hoenn.Maps);
        foreach (string room in Rooms) Assert.Contains(room, hoenn.Maps);
        foreach (string key in new[] { "battle_frontier_outside_west", "battle_frontier_outside_east", "battle_frontier_reception_gate", "battle_frontier_scotts_house" })
            Assert.Contains(key, Hoenn.Value.Index.Areas);
        var built = Hoenn.Value.BuildMaps().Select(m => m.Name).ToHashSet();
        foreach (string room in Rooms.Append("HoennBattleFrontier")) Assert.Contains(room, built);
    }

    [Fact]
    public void TheSevenHallsAndArtisanCaveStayShut()
    {
        var island = Hoenn.Value.BuildMaps().Single(m => m.Name == "HoennBattleFrontier");
        island.ApplyDoors(_ => true);
        foreach (var warp in island.Warps.Where(w => !w.Shut))
        {
            Assert.DoesNotContain("Lobby", warp.TargetMap);
            Assert.DoesNotContain("ArtisanCave", warp.TargetMap);
        }
        // The lounges, the Mart, the Pokémon Center and the rest are ways in
        foreach (string room in Rooms.Where(r => !r.EndsWith("2F")))
            Assert.Contains(island.Warps, w => !w.Shut && w.TargetMap == room);
    }

    [Fact]
    public void LilycovesFerrySailsAChampionWithTheTicketToTheIslandAndTheIslandsFerrySailsBack()
    {
        var game = new Game("LilycoveCity_Harbor", 8, 11);
        game.Story.Set("FLAG_ENABLE_SHIP_SOUTHERN_ISLAND");
        game.Story.Set("FLAG_RECEIVED_SS_TICKET");
        var sailed = game.Talk("lilycove_harbor_attendant", 1);
        Assert.Contains(sailed.Log, l => l.StartsWith("warp HoennBattleFrontier 19 67"));
        Assert.Equal("HoennBattleFrontier", game.Map.Name);
        Assert.Equal("battle_frontier_outside_west", game.Map.AreaAt(19, 67)?.Key);
        Assert.True(game.Map.IsWalkable(19, 67));

        // Southern Island is still the first choice
        var other = new Game("LilycoveCity_Harbor", 8, 11);
        other.Story.Set("FLAG_ENABLE_SHIP_SOUTHERN_ISLAND");
        other.Story.Set("FLAG_RECEIVED_SS_TICKET");
        other.Talk("lilycove_harbor_attendant", 0);
        Assert.Equal("SouthernIsland_Exterior", other.Map.Name);

        // On the island's quay the attendant takes the player home
        var back = game.Talk("frontier_ferry_attendant");
        Assert.Contains(back.Log, l => l.StartsWith("warp LilycoveCity_Harbor 8 11"));
        Assert.Equal("LilycoveCity_Harbor", game.Map.Name);
    }

    [Fact]
    public void WithoutTheTicketTheFerryOffersOnlySouthernIsland()
    {
        var game = new Game("LilycoveCity_Harbor", 8, 11);
        game.Story.Set("FLAG_ENABLE_SHIP_SOUTHERN_ISLAND");
        var talk = game.Talk("lilycove_harbor_attendant");
        Assert.DoesNotContain(talk.Asked, a => a.Answer == "BATTLE FRONTIER");
        Assert.Equal("SouthernIsland_Exterior", game.Map.Name);
    }

    [Fact]
    public void ScottGivesThePassInTheReceptionGateAndIsGoneOnceItIsHad()
    {
        var game = new Game("BattleFrontier_ReceptionGate", 4, 12);
        Assert.NotNull(game.Present("frontier_reception_scott"));
        game.Talk("frontier_reception_scott");
        Assert.True(game.Story.Has("FLAG_RECEIVED_FRONTIER_PASS"));
        game.Arrive("BattleFrontier_ReceptionGate", 4, 12);
        Assert.Null(game.Present("frontier_reception_scott"));
    }

    [Fact]
    public void ScottGivesBattlePointsOnceAndOffersTheCrossingToSinnohsFrontier()
    {
        var game = new Game("BattleFrontier_ScottsHouse", 2, 6);
        var first = game.Talk("scotts_house_scott", 1);
        Assert.True(game.Story.Has("FLAG_RECEIVED_SCOTTS_BATTLE_POINTS"));
        Assert.Contains(first.Transcript, l => l.Text.Contains("3 Battle Points"));
        Assert.DoesNotContain(first.Log, l => l.StartsWith("travel"));

        var again = game.Talk("scotts_house_scott", 0);
        Assert.DoesNotContain(again.Transcript, l => l.Text.Contains("3 Battle Points"));
        Assert.Contains(again.Log, l => l.StartsWith("travel to Sinnoh BattleFrontier 47 51"));
        PlayerIdentity.SetRival(null);
    }

    [Theory]
    [InlineData(BattleOutcome.Won, true)]
    [InlineData(BattleOutcome.Caught, true)]
    [InlineData(BattleOutcome.Fled, false)]
    public void TheStrangeTreeIsASudowoodoWateredWithTheWailmerPail(BattleOutcome outcome, bool gone)
    {
        var game = new Game("HoennBattleFrontier", 110, 63);
        Assert.NotNull(game.Present("frontier_sudowoodo"));
        var dry = game.Talk("frontier_sudowoodo");
        Assert.DoesNotContain(dry.Log, l => l.StartsWith("wildbattle"));

        game.Bag.AddItem(ItemDatabase.Get("Wailmer Pail")!, 1);
        game.Fight = outcome;
        var watered = game.Talk("frontier_sudowoodo");
        Assert.Contains(watered.Log, l => l.StartsWith("wildbattle Sudowoodo 40"));
        Assert.Equal(gone, game.Story.Has("FLAG_HIDE_BATTLE_FRONTIER_SUDOWOODO"));
        game.Arrive("HoennBattleFrontier", 110, 63);
        Assert.Equal(gone, game.Present("frontier_sudowoodo") == null);
    }

    [Fact]
    public void TheWalkOfTheWholeGameReachesTheIslandAndEveryRoomOfItsGrounds()
    {
        var built = Hoenn.Value.BuildMaps().ToDictionary(m => m.Name);
        Map MapNamed(string name) => built.TryGetValue(name, out var map) ? map : MapDatabase.Get(name);
        var walked = WorldWalk.From(MapNamed, new[] { RegionDatabase.Get(RegionDatabase.Hoenn)!.Start! });
        foreach (string name in Rooms.Append("HoennBattleFrontier"))
            Assert.True(walked.Keys.Any(m => m.Name == name), $"nobody walks into {name}");
    }
}
