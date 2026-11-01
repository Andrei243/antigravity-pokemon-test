using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The post-game of Hoenn, part i (plan 21 · H12; docs/mechanics/rulings.md, "The Abandoned Ship, Artisan Cave and the
/// last shut rooms"): the Abandoned Ship off Route 108 with its locked doors, its flooded passages and its hidden floor,
/// the Scanner taken to Captain Stern, Thalia and Kira and Dan in Match Call, Artisan Cave under the Battle Frontier, and
/// the rooms of Fortree, Mossdeep and Sootopolis that were shut. Each scene starts as the game starts it, on maps of this
/// class's own.
/// </summary>
public class HoennAbandonedShipTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private static readonly Lazy<World> Hoenn = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn));

    private static readonly string[] Places =
    {
        "AbandonedShip_Deck", "AbandonedShip_Corridors_1F", "AbandonedShip_Rooms_1F", "AbandonedShip_Corridors_B1F",
        "AbandonedShip_Rooms_B1F", "AbandonedShip_Rooms2_B1F", "AbandonedShip_Underwater1", "AbandonedShip_Room_B1F",
        "AbandonedShip_Rooms2_1F", "AbandonedShip_CaptainsOffice", "AbandonedShip_Underwater2",
        "AbandonedShip_HiddenFloorCorridors", "AbandonedShip_HiddenFloorRooms", "ArtisanCave_1F", "ArtisanCave_B1F",
        "FortreeCity_DecorationShop", "MossdeepCity_GameCorner_1F", "SootopolisCity_MysteryEventsHouse_1F"
    };

    private sealed class Game
    {
        private readonly Dictionary<string, Map> maps = new(StringComparer.OrdinalIgnoreCase);
        public StoryState Story { get; } = new();
        public Party Party { get; } = new();
        public Inventory Bag { get; } = new();
        public Map Map { get; private set; } = null!;
        public (int X, int Y) Tile { get; set; }
        public int Money { get; set; } = 3000;
        public List<StoredDecoration> Decorations { get; } = new();
        public HeadlessScriptHost Last { get; private set; } = null!;

        public Game(string map, int x, int y, int team = 2)
        {
            foreach (var m in Hoenn.Value.BuildMaps()) maps[m.Name] = m;
            StoryMigration.BeginNewGame(Story, Scripts);
            Story.Set("HoennHallOfFame");
            Story.Set("FLAG_SYS_POKENAV_GET");
            Story.SetBadges("Hoenn", 0xFF);
            Party.Add(new Pokemon(PokemonDatabase.Get("Swampert")!, 60, new Random(1)));
            if (team > 1) Party.Add(new Pokemon(PokemonDatabase.Get("Manectric")!, 60, new Random(2)));
            Arrive(map, x, y);
        }

        public Map MapOf(string name) => maps[name];

        public void Arrive(string name, int x, int y)
        {
            Map = maps[name];
            Map.ForgetForced();
            Map.ApplyPresence(Story.Has);
            Map.ApplyDoors(Story.Has);
            Wandering.SendHome(Map);
            Tile = (x, y);
            if (Scripts.In(Map.ScriptFileAt(x, y), ScriptLibrary.OnEnter) is { } script) Play(script);
        }

        public HeadlessScriptHost Talk(string key, params int[] answers)
        {
            var npc = Map.NPCs.First(n => n.Key == key);
            Tile = (npc.GridX, npc.GridY + 1);
            return Play(Scripts.Find(FieldScripts.For(npc)!, npc.ScriptFile ?? Map.ScriptFileAt(npc.GridX, npc.GridY))!, npc, answers);
        }

        /// <summary>What is read on a tile: a sign's script, or one of the overlay's tiles read.</summary>
        public HeadlessScriptHost Read(int x, int y, params int[] answers)
        {
            string name = Map.SignScripts.TryGetValue((x, y), out var sign) ? sign
                : Map.TileScripts.TryGetValue((x, y), out var tile) ? tile
                : throw new Xunit.Sdk.XunitException($"Nothing is read at {x},{y} of {Map.Name}");
            return Play(Scripts.Find(name, Map.ScriptFileAt(x, y))!, null, answers);
        }

        public HeadlessScriptHost Play(Script script, NPC? subject = null, params int[] answers)
        {
            var host = new HeadlessScriptHost(Story, Party, Bag)
            {
                Map = Map,
                PlayerTile = Tile,
                MapNamed = name => maps.GetValueOrDefault(name),
                Fight = _ => BattleOutcome.Won,
                NeedsPokemon = true,
                Money = Money
            };
            host.Decorations.AddRange(Decorations);
            foreach (int answer in answers) host.Answers.Enqueue(answer);
            var runner = new ScriptRunner(Scripts, host);
            runner.Start(script, subject);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            Money = host.Money;
            Decorations.Clear();
            Decorations.AddRange(host.Decorations);
            foreach (var map in maps.Values.Distinct())
            {
                map.ApplyPresence(Story.Has);
                map.ApplyDoors(Story.Has);
            }
            Tile = host.PlayerTile;
            if (host.Map is { } there && there != Map) Arrive(there.Name, host.PlayerTile.X, host.PlayerTile.Y);
            return Last = host;
        }
    }

    [Fact]
    public void TheShipArtisanCaveAndTheRoomsAreHoennsAndJoined()
    {
        var hoenn = RegionDatabase.Get(RegionDatabase.Hoenn)!;
        foreach (string name in Places) Assert.Contains(name, hoenn.Maps);
        var built = Hoenn.Value.BuildMaps().ToDictionary(m => m.Name);
        foreach (string name in Places) Assert.True(built.ContainsKey(name), name);
        // Route 108's way aboard, and the deck's ways down and into the captain's office
        Assert.Equal("AbandonedShip_Deck", built["Hoenn"].WarpsAt(169, 366).Single().TargetMap);
        Assert.Equal("AbandonedShip_Corridors_1F", built["AbandonedShip_Deck"].GetWarpAt(13, 9)!.TargetMap);
        Assert.Equal("AbandonedShip_CaptainsOffice", built["AbandonedShip_Deck"].GetWarpAt(12, 5)!.TargetMap);
        Assert.Equal("AbandonedShip_Corridors_B1F", built["AbandonedShip_Corridors_1F"].GetWarpAt(16, 2)!.TargetMap);
        // The two cave mouths of Artisan Cave, from the Frontier island's two halves
        var frontier = built["HoennBattleFrontier"];
        Assert.Contains(frontier.Warps, w => w.TargetMap == "ArtisanCave_1F");
        Assert.Contains(frontier.Warps, w => w.TargetMap == "ArtisanCave_B1F");
        // Smeargle paints its walls
        Assert.All(built["ArtisanCave_B1F"].Areas.Single().WildEncounters, e => Assert.Equal("Smeargle", e.SpeciesName));
        // Fortree's decoration shop, Mossdeep's game corner and Sootopolis's house have their doors open, and their
        // basements none
        foreach (var (room, x, y) in new[] { ("FortreeCity_DecorationShop", 3, 5), ("MossdeepCity_GameCorner_1F", 5, 9), ("SootopolisCity_MysteryEventsHouse_1F", 3, 7) })
            Assert.Contains(built["Hoenn"].Warps.Concat(built.Values.SelectMany(m => m.Warps)), w => w.TargetMap == room);
        Assert.Null(built["MossdeepCity_GameCorner_1F"].GetWarpAt(2, 0));
        Assert.Null(built["SootopolisCity_MysteryEventsHouse_1F"].GetWarpAt(3, 1));
    }

    [Fact]
    public void TheFloodedPassagesJoinTheLowerDeckToTheHiddenFloor()
    {
        var built = Hoenn.Value.BuildMaps().ToDictionary(m => m.Name);
        var cabins = built["AbandonedShip_Rooms_B1F"];
        Assert.Equal(TileBehavior.DiveWater, cabins.BehaviourAt(13, 7));
        var down = cabins.DiveTargetAt(13, 7)!;
        Assert.Equal(("AbandonedShip_Underwater2", 17, 4), (down.Map, down.X, down.Y));
        var second = built["AbandonedShip_Underwater2"];
        Assert.True(second.IsUnderwater);
        var back = second.EmergeTargetAt(17, 4)!;
        Assert.Equal(("AbandonedShip_Rooms_B1F", 13, 7), (back.Map, back.X, back.Y));
        Assert.Equal("AbandonedShip_Underwater1", second.GetWarpAt(3, 1)!.TargetMap);
        var first = built["AbandonedShip_Underwater1"];
        var up = first.EmergeTargetAt(5, 4)!;
        Assert.Equal(("AbandonedShip_HiddenFloorCorridors", 0, 10), (up.Map, up.X, up.Y));
        var hidden = built["AbandonedShip_HiddenFloorCorridors"];
        Assert.Equal(TileBehavior.DiveWater, hidden.BehaviourAt(0, 10));
        var again = hidden.DiveTargetAt(0, 10)!;
        Assert.Equal(("AbandonedShip_Underwater1", 5, 4), (again.Map, again.X, again.Y));
    }

    [Fact]
    public void TheStorageRoomOpensOnlyWithTheStorageKey()
    {
        var game = new Game("AbandonedShip_Corridors_B1F", 11, 5);
        // Shut: nobody walks into it, and reading it says so
        Assert.Null(game.Map.GetWarpAt(11, 4));
        var host = game.Read(11, 4);
        Assert.Contains(host.Transcript, l => l.Text.Contains("STORAGE"));
        Assert.Null(game.Map.GetWarpAt(11, 4));

        // The key from the captain's office turns in the lock, stays in it, and the door is a way from then on
        game.Bag.AddItem(ItemDatabase.Get("Storage Key")!, 1);
        game.Read(11, 4);
        Assert.True(game.Story.Has("FLAG_USED_STORAGE_KEY"));
        Assert.False((game.Bag.GetQuantity(ItemDatabase.Get("Storage Key")!) > 0));
        Assert.Equal("AbandonedShip_Room_B1F", game.Map.GetWarpAt(11, 4)!.TargetMap);
        host = game.Read(11, 4);
        Assert.Contains(host.Transcript, l => l.Text.Contains("stands open"));
    }

    [Theory]
    [InlineData("Room 1 Key", "FLAG_USED_ROOM_1_KEY", 3, 8, 3, 8)]
    [InlineData("Room 2 Key", "FLAG_USED_ROOM_2_KEY", 6, 8, 6, 8)]
    [InlineData("Room 4 Key", "FLAG_USED_ROOM_4_KEY", 3, 3, 3, 3)]
    [InlineData("Room 6 Key", "FLAG_USED_ROOM_6_KEY", 9, 3, 9, 3)]
    public void EachLockedCabinOfTheHiddenFloorOpensWithItsOwnKey(string key, string flag, int readX, int readY, int doorX, int doorY)
    {
        var game = new Game("AbandonedShip_HiddenFloorCorridors", 1, 6);
        Assert.Null(game.Map.GetWarpAt(doorX, doorY));
        // Another room's key doesn't fit
        string other = key == "Room 1 Key" ? "Room 2 Key" : "Room 1 Key";
        game.Bag.AddItem(ItemDatabase.Get(other)!, 1);
        game.Read(readX, readY);
        Assert.False(game.Story.Has(flag));
        Assert.Null(game.Map.GetWarpAt(doorX, doorY));
        // Its own does, and stays in the lock
        game.Bag.AddItem(ItemDatabase.Get(key)!, 1);
        game.Read(readX, readY);
        Assert.True(game.Story.Has(flag));
        Assert.False((game.Bag.GetQuantity(ItemDatabase.Get(key)!) > 0));
        Assert.True((game.Bag.GetQuantity(ItemDatabase.Get(other)!) > 0));
        Assert.Equal("AbandonedShip_HiddenFloorRooms", game.Map.GetWarpAt(doorX, doorY)!.TargetMap);
    }

    [Fact]
    public void RoomsThreeAndFiveAreNeverLockedAndTheKeysLieInTheOtherCabins()
    {
        var built = Hoenn.Value.BuildMaps().ToDictionary(m => m.Name);
        var corridor = built["AbandonedShip_HiddenFloorCorridors"];
        corridor.ApplyDoors(_ => false);
        Assert.Equal("AbandonedShip_HiddenFloorRooms", corridor.GetWarpAt(9, 8)!.TargetMap);
        Assert.Equal("AbandonedShip_HiddenFloorRooms", corridor.GetWarpAt(6, 3)!.TargetMap);
        var rooms = built["AbandonedShip_HiddenFloorRooms"];
        var hidden = rooms.HiddenItems.Values.Select(h => h.Item).ToList();
        foreach (string key in new[] { "Room 1 Key", "Room 2 Key", "Room 4 Key", "Room 6 Key" }) Assert.Contains(key, hidden);
        Assert.Contains(rooms.Everyone, n => n.IsItemBall && n.Item == "Scanner");
        // Room 3's key opens Room 1 (it lies in Room 3, which is never locked)
        Assert.Equal("Room 1 Key", rooms.HiddenItems[(42, 10)].Item);
    }

    [Fact]
    public void SternsAideAsksForTheScannerAndSternTradesForIt()
    {
        var game = new Game("AbandonedShip_CaptainsOffice", 3, 5);
        var host = game.Talk("scientist_1");
        Assert.Contains(host.Transcript, l => l.Text.Contains("haven't had any luck"));
        game.Bag.AddItem(ItemDatabase.Get("Scanner")!, 1);
        host = game.Talk("scientist_1");
        Assert.Contains(host.Transcript, l => l.Text.Contains("Captain Stern in Slateport"));

        // Stern, on Slateport's quay, trades it for the Deep Sea Scale (no to the tooth first)
        game.Story.Set("FLAG_TEAM_AQUA_ESCAPED_IN_SUBMARINE");
        game.Arrive("SlateportCity_Harbor", 6, 14);
        game.Story.Unset("FLAG_HIDE_SLATEPORT_CITY_HARBOR_CAPTAIN_STERN");
        game.Map.ApplyPresence(game.Story.Has);
        game.Talk("slateport_harbor_capt_stern", 0, 1, 1, 0);
        Assert.True(game.Story.Has("FLAG_EXCHANGED_SCANNER"));
        Assert.False((game.Bag.GetQuantity(ItemDatabase.Get("Scanner")!) > 0));
        Assert.True((game.Bag.GetQuantity(ItemDatabase.Get("Deep Sea Scale")!) > 0));
        Assert.False((game.Bag.GetQuantity(ItemDatabase.Get("Deep Sea Tooth")!) > 0));
        // and the aide talks of the ship from then on
        game.Arrive("AbandonedShip_CaptainsOffice", 3, 5);
        host = game.Talk("scientist_1");
        Assert.Contains(host.Transcript, l => l.Text.Contains("S.S. Cactus"));
    }

    [Fact]
    public void SternKeepsNothingFromAPlayerWhoWontTrade()
    {
        var game = new Game("SlateportCity_Harbor", 6, 14);
        game.Story.Set("FLAG_TEAM_AQUA_ESCAPED_IN_SUBMARINE");
        game.Story.Unset("FLAG_HIDE_SLATEPORT_CITY_HARBOR_CAPTAIN_STERN");
        game.Map.ApplyPresence(game.Story.Has);
        game.Bag.AddItem(ItemDatabase.Get("Scanner")!, 1);
        game.Talk("slateport_harbor_capt_stern", 2);
        Assert.True((game.Bag.GetQuantity(ItemDatabase.Get("Scanner")!) > 0));
        Assert.False(game.Story.Has("FLAG_EXCHANGED_SCANNER"));
    }

    [Fact]
    public void ThaliaAndKiraAndDanGiveTheirNumbersOnceBeaten()
    {
        var game = new Game("AbandonedShip_Rooms_1F", 10, 12);
        game.Talk("beauty", 0);
        Assert.True(game.Story.HasDefeated("hoenn_thalia_1"));
        Assert.True(game.Story.Has(Pokegear.ContactFlag("hoenn_thalia")));
        Assert.NotNull(PhoneBook.Hoenn.SingleOrDefault(c => c.Id == "hoenn_thalia"));

        var couple = new Game("AbandonedShip_Rooms2_1F", 6, 14);
        couple.Talk("woman_4", 1);
        Assert.True(couple.Story.HasDefeated("hoenn_kira_and_dan_1"));
        Assert.False(couple.Story.Has(Pokegear.ContactFlag("hoenn_kira_and_dan")));
        couple.Talk("man_4", 0);
        Assert.True(couple.Story.Has(Pokegear.ContactFlag("hoenn_kira_and_dan")));

        // With a single Pokémon they won't battle at all
        var alone = new Game("AbandonedShip_Rooms2_1F", 6, 14, team: 1);
        alone.Talk("woman_4");
        Assert.False(alone.Story.HasDefeated("hoenn_kira_and_dan_1"));
    }

    private static readonly DateTime Monday = new(2026, 11, 2);

    private static (DateTime Day, TimeOfDay Time) HourOf(PhoneContact contact) =>
        (Enumerable.Range(0, 7).Select(i => Monday.AddDays(i)).First(d => d.DayOfWeek.ToString() == contact.RematchDay),
         contact.RematchTime == "Morning" ? TimeOfDay.Morning : contact.RematchTime == "Day" ? TimeOfDay.Day : TimeOfDay.Night);

    [Fact]
    public void EveryRouteTrainerOfMatchCallHasADayAndARowOfFourTeams()
    {
        var routes = PhoneBook.Hoenn.Where(PhoneRematch.IsHoennRouteTrainer).ToList();
        Assert.Equal(26, routes.Count);
        foreach (var contact in routes)
        {
            Assert.NotNull(contact.RematchDay);
            Assert.NotNull(contact.RematchTime);
            var row = TrainerDatabase.Get(contact.Trainer!)!.Rematches!;
            Assert.Equal(4, row.Count);
            // Each team stronger than the one before
            int last = TrainerDatabase.Get(contact.Trainer!)!.Party.Max(p => p.Level);
            foreach (var id in row)
            {
                int top = TrainerDatabase.Get(id!)!.Party.Max(p => p.Level);
                Assert.True(top > last, $"{id}: {top} after {last}");
                last = top;
            }
        }
        // The Gym Leaders, the Four and Wally are not route trainers
        Assert.DoesNotContain(routes, c => c.Id is "roxanne" or "sidney" or "hoenn_wally" or "wallace");
    }

    [Fact]
    public void ARouteTrainerAsksForARematchFromFiveBadgesAndBringsTheTeamTheBadgesOpen()
    {
        var contact = PhoneBook.Get("hoenn_thalia")!;
        var (day, time) = HourOf(contact);
        var story = new StoryState();
        story.Defeat("hoenn_thalia_1");
        story.Set(Pokegear.ContactFlag(contact.Id));
        story.SetBadges("Hoenn", 0x0F);
        Assert.False(PhoneRematch.Wants(contact, story, day, time));
        story.SetBadges("Hoenn", 0x1F);
        // Not off their hour, but on it: no Hall of Fame asked of a route trainer
        Assert.False(PhoneRematch.Wants(contact, story, day.AddDays(1), time));
        Assert.True(PhoneRematch.Wants(contact, story, day, time));
        Assert.Equal(Pokegear.CallRematch, PhoneRematch.Call(contact, story, day, time, incoming: false));
        Assert.Equal(Pokegear.CallWaiting, PhoneRematch.Call(contact, story, day, time, incoming: false));
        var record = TrainerDatabase.Get("hoenn_thalia_1")!;
        Assert.Equal("hoenn_thalia_2", VsSeeker.RematchTeam(record, story));
        story.Defeat("hoenn_thalia_2");
        // Five Badges open only the second team, which is fought again until the sixth
        Assert.Equal("hoenn_thalia_2", VsSeeker.RematchTeam(record, story));
        story.SetBadges("Hoenn", 0xFF);
        Assert.Equal("hoenn_thalia_3", VsSeeker.RematchTeam(record, story));
        story.Defeat("hoenn_thalia_3");
        story.Defeat("hoenn_thalia_4");
        // The last team with all eight Badges, fought again from then on
        Assert.Equal("hoenn_thalia_5", VsSeeker.RematchTeam(record, story));
        story.Defeat("hoenn_thalia_5");
        Assert.Equal("hoenn_thalia_5", VsSeeker.RematchTeam(record, story));
    }

    [Fact]
    public void AWaitingRouteTrainerIsFoughtOnTheirRouteWithTheirNextTeam()
    {
        var game = new Game("AbandonedShip_Rooms_1F", 10, 12);
        game.Talk("beauty", 0);
        game.Story.Set(PhoneRematch.WaitingFlag("hoenn_thalia"));
        PhoneRematch.KeepWaiting(game.Map.Everyone, game.Story);
        var host = game.Talk("beauty");
        Assert.True(game.Story.HasDefeated("hoenn_thalia_2"));
        Assert.False(game.Story.Has(PhoneRematch.WaitingFlag("hoenn_thalia")));
        // Ricky on Route 109 the same
        var beach = new Game("Hoenn", 60, 60);
        var ricky = beach.MapOf("Hoenn").Everyone.First(n => n.TrainerData?.Id == "hoenn_ricky_1");
        beach.Story.Defeat("hoenn_ricky_1");
        beach.Story.Set(Pokegear.ContactFlag("hoenn_ricky"));
        beach.Story.Set(PhoneRematch.WaitingFlag("hoenn_ricky"));
        PhoneRematch.KeepWaiting(beach.Map.Everyone, beach.Story);
        Assert.True(ricky.ReadyForRematch);
        host = beach.Play(ScriptLibrary.Default.Find(FieldScripts.For(ricky)!, ricky.ScriptFile ?? beach.Map.ScriptFileAt(ricky.GridX, ricky.GridY))!, ricky);
        Assert.True(beach.Story.HasDefeated("hoenn_ricky_2"));
    }

    [Fact]
    public void FortreesDecorationShopSellsDesksAndChairsIntoThePC()
    {
        var game = new Game("FortreeCity_DecorationShop", 1, 3) { Money = 10000 };
        game.Talk("mart_employee", 2);
        Assert.Equal(4000, game.Money);
        Assert.Contains(game.Decorations, d => DecorationDatabase.Get(d.Kind)?.Name == "Heavy Desk");
        game.Talk("mart_employee_2", 4, 3);
        Assert.Equal(2000, game.Money);
        Assert.Contains(game.Decorations, d => DecorationDatabase.Get(d.Kind)?.Name == "Hard Chair");
        // Short of money, nothing changes hands
        game.Talk("mart_employee", 4, 1);
        Assert.Equal(2000, game.Money);
        Assert.DoesNotContain(game.Decorations, d => DecorationDatabase.Get(d.Kind)?.Name == "Brick Desk");
    }
}
