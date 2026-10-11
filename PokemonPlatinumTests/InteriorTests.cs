using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// The rooms of the south-west and the centre as plan 01 · M11, part a built them, on the original's plans: the
/// houses of Twinleaf Town, Sandgem Town, Jubilife City, Oreburgh City, Floaroma Town, Eterna City, Hearthome City and
/// Solaceon Town and of the routes between, the flower shop, the herb shop, the Pokémon News Press, the Berry
/// Master's house, the Foreign Building and the Old Chateau. Where their people stand is <see cref="RoomPlanTests"/>'s.
/// </summary>
public class InteriorTests
{
    // Maps of this class's own, so nothing another test does to the database's maps shows here
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Sinnoh"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));

    private static World Sinnoh => LoadedWorld.Value;

    private static Map Room(string name) => GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    private static int Have(HeadlessScriptHost host, string item) => host.Bag.GetQuantity(ItemDatabase.Get(item)!);

    private static Pokemon Mon(string species = "Turtwig", int level = 20) => new(PokemonDatabase.Get(species)!, level, Gender.Male, Nature.Hardy, false);

    private static HeadlessScriptHost Run(string script, string place, HeadlessScriptHost host, NPC? who = null)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, place) ?? throw new InvalidOperationException($"no script {place}.{script}"), who);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    /// <summary>Each door part a opened: the area, its warp's number, the room, and the mat inside it.</summary>
    public static IEnumerable<object[]> Doors => new[]
    {
        new object[] { "twinleaf_town", 2, "TwinleafTownNortheastHouse", 4, 8 },
        new object[] { "twinleaf_town", 3, "TwinleafTownSouthwestHouse", 4, 8 },
        new object[] { "sandgem_town", 3, "SandgemTownCounterpartHouse1F", 6, 10 },
        new object[] { "sandgem_town", 4, "SandgemTownHouse", 4, 8 },
        new object[] { "jubilife_city", 0, "JubilifeCityCondominiums1F", 11, 12 },
        new object[] { "jubilife_city", 4, "JubilifeCitySouthHouse1F", 11, 12 },
        new object[] { "oreburgh_city", 3, "OreburghCityNorthwestHouse1F", 11, 12 },
        new object[] { "oreburgh_city", 4, "OreburghCityWestHouse", 4, 8 },
        new object[] { "oreburgh_city", 5, "OreburghCityMiddleHouse", 4, 8 },
        new object[] { "oreburgh_city", 7, "OreburghCityEastHouse1F", 11, 12 },
        new object[] { "oreburgh_city", 9, "OreburghCitySouthHouse", 4, 8 },
        new object[] { "floaroma_town", 2, "FlowerShop", 8, 11 },
        new object[] { "floaroma_town", 3, "FloaromaTownSoutheastHouse", 4, 8 },
        new object[] { "floaroma_town", 4, "FloaromaTownMiddleHouse", 4, 8 },
        new object[] { "floaroma_meadow", 2, "FloaromaMeadowHouse", 4, 8 },
        new object[] { "route_205_south", 2, "Route205House", 4, 8 },
        new object[] { "eterna_city", 5, "EternaCitySouthHouse", 4, 8 },
        new object[] { "eterna_city", 6, "EternaCityEastHouse", 4, 8 },
        new object[] { "eterna_city", 7, "EternaCityHerbShop", 4, 8 },
        new object[] { "eterna_forest", 4, "OldChateau", 9, 15 },
        new object[] { "route_208", 2, "Route208House", 4, 8 },
        new object[] { "hearthome_city", 6, "HearthomeCityNorthwestHouse", 5, 8 },
        new object[] { "hearthome_city", 7, "HearthomeCitySoutheastHouse1F", 10, 12 },
        new object[] { "hearthome_city", 9, "ForeignBuilding", 8, 20 },
        new object[] { "hearthome_city", 13, "HearthomeCityNortheastHouse1F", 10, 12 },
        new object[] { "solaceon_town", 1, "SolaceonTownNortheastHouse", 4, 8 },
        new object[] { "solaceon_town", 2, "SolaceonTownPokemonNewsPress", 4, 8 },
        new object[] { "solaceon_town", 3, "SolaceonTownNorthHouse", 4, 8 },
    };

    /// <summary>Every room of part a, and the two ground floors it gave their stairs.</summary>
    private static IEnumerable<string> AllRooms =>
        Doors.Select(d => (string)d[2]).Concat(RoomPlanTests.InnerRooms).Concat(new[] { "OreburghNorthHouse1F", "EternaCondominiums1F" }).Distinct();

    [Theory]
    [MemberData(nameof(Doors))]
    public void ADoorLeadsOntoItsRoomsMatAndTheRoomBackOutBeforeTheDoor(string area, int warp, string room, int matX, int matY)
    {
        var way = Sinnoh.Area(area)!.Warps[warp];
        var outside = BuiltMaps.Value.Values.First(m => m.FindArea(area) != null);
        var door = outside.GetWarpAt(way.X, way.Z);
        Assert.NotNull(door);
        Assert.Equal((room, matX, matY, Direction.Up), (door!.TargetMap, door.TargetX, door.TargetY, door.TargetFacing));
        Assert.DoesNotContain(warp, Sinnoh.Overlay(area)!.Locked ?? new());

        var inside = Room(room);
        Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap(room)!.Id);
        Assert.True(inside.IsWalkable(matX, matY), $"{room}: the mat is no floor");
        var exit = inside.GetWarpAt(matX, matY + 1);
        Assert.NotNull(exit);
        Assert.Equal((outside.Name, way.X, way.Z + 1, Direction.Down), (exit!.TargetMap, exit.TargetX, exit.TargetY, exit.TargetFacing));
        Assert.True(outside.IsWalkable(way.X, way.Z + 1), $"{room}: nobody can stand before its door");
        // As its town's rooms have it: the town's own theme, and the town's name on the room
        Assert.Equal(outside.BgmTrackAt(way.X, way.Z + 1) == "sinnoh/forest" ? "sinnoh/turnback_cave" : outside.BgmTrackAt(way.X, way.Z + 1), inside.BgmTrack);
    }

    [Fact]
    public void WhatIsStillShutInTheseAreasIsSomeoneElses()
    {
        // Jubilife City's Global Terminal (part d) and the two doors of houses the original never used; the room of the
        // Solaceon Ruins the Ruin Maniac's tunnel leads to (the story's). Oreburgh's Mining Museum is open since plan 06 · R16b
        Assert.Equal(new[] { 1, 10, 11 }, Sinnoh.Overlay("jubilife_city")!.Locked);
        Assert.Equal(new[] { 7 }, Sinnoh.Overlay("solaceon_town")!.Locked);
        foreach (string area in new[] { "twinleaf_town", "sandgem_town", "oreburgh_city", "floaroma_town", "floaroma_meadow", "route_205_south", "eterna_city", "eterna_forest", "route_208", "hearthome_city" })
            Assert.True(Sinnoh.Overlay(area)!.Locked is not { Count: > 0 }, $"{area} still has a door shut");
    }

    [Fact]
    public void EveryoneStandsOnFloorWithSomethingToSayAndEveryWayLeadsSomewhereOneCanStand()
    {
        var rooms = AllRooms.ToDictionary(n => n, Room);
        Assert.True(rooms.Count >= 49, $"only {rooms.Count} rooms");
        foreach (var (name, map) in rooms)
        {
            Assert.True(map.IsIndoors, name);
            Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap(name)!.Id);
            foreach (var who in map.Everyone)
            {
                string where = $"{name}: {who.Key} at {who.GridX},{who.GridY}";
                Assert.False(map.IsSolid(who.GridX, who.GridY), where + " stands in something solid");
                Assert.True(map.GetGroundTile(who.GridX, who.GridY) == TileType.Floor, where + " stands on no floor");
                Assert.True(who.DialogLines.Count > 0 || who.Script != null || who.IsItemBall, where + " has nothing to say");
                if (who.Script != null) Assert.True(ScriptLibrary.Default.Find(who.Script, name) != null, where + $" runs '{who.Script}', which is nowhere");
                // Nobody stands where another does, nor on a way out
                Assert.Single(map.Everyone, other => (other.GridX, other.GridY) == (who.GridX, who.GridY));
                Assert.Null(map.GetWarpAt(who.GridX, who.GridY));
            }
            foreach (var (tile, script) in map.SignScripts)
                Assert.True(ScriptLibrary.Default.Find(script, name) != null, $"{name}: what is read at {tile} runs '{script}', which is nowhere");
            foreach (var warp in map.Warps)
            {
                // (a room of a later part's that one of these opens on is read from its own file)
                var target = rooms.GetValueOrDefault(warp.TargetMap) ?? (BuiltMaps.Value.TryGetValue(warp.TargetMap, out var built) ? built : Room(warp.TargetMap));
                Assert.True(target.IsWalkable(warp.TargetX, warp.TargetY), $"{name}: the way at {warp.SourceX},{warp.SourceY} leads into something solid of {warp.TargetMap}");
                Assert.False(map.IsSolid(warp.SourceX, warp.SourceY), $"{name}: the way at {warp.SourceX},{warp.SourceY} is walled up");
            }
        }
    }

    [Fact]
    public void FloorsAndRoomsAreJoinedBothWays()
    {
        // Whoever takes a flight of stairs, a lift or a doorway comes out one step from the way back, which leads to one
        // step from where they set out
        var rooms = AllRooms.ToDictionary(n => n, Room);
        int joined = 0;
        foreach (var (name, map) in rooms)
            foreach (var warp in map.Warps.Where(w => rooms.ContainsKey(w.TargetMap)))
            {
                var far = rooms[warp.TargetMap];
                var back = far.Warps.Where(w => w.TargetMap == name && Math.Abs(w.SourceX - warp.TargetX) + Math.Abs(w.SourceY - warp.TargetY) == 1).ToList();
                Assert.True(back.Count == 1, $"{name}: the way at {warp.SourceX},{warp.SourceY} comes out at {warp.TargetX},{warp.TargetY} of {warp.TargetMap}, beside {back.Count} ways back");
                Assert.Equal(1, Math.Abs(back[0].TargetX - warp.SourceX) + Math.Abs(back[0].TargetY - warp.SourceY));
                // They walk away from the way they came by
                var (dx, dy) = warp.TargetFacing switch { Direction.Up => (0, -1), Direction.Down => (0, 1), Direction.Left => (-1, 0), _ => (1, 0) };
                Assert.NotEqual((back[0].SourceX, back[0].SourceY), (warp.TargetX + dx, warp.TargetY + dy));
                joined++;
            }
        // Seven houses with a floor upstairs (eight flights: the Eterna Condominiums have two), two lifts, and the Old
        // Chateau's nine ways between its ten rooms, each both ways
        Assert.True(joined >= 2 * (8 + 2 + 9), $"only {joined} ways between rooms");

        // A flight stands beside its warp: up to the west of it, down to the east, as the original's tiles have them
        foreach (var (below, ux, uy, above, dx, dy) in new[]
                 {
                     ("JubilifeCityCondominiums1F", 15, 3, "JubilifeCityCondominiums2F", 8, 3), ("OreburghNorthHouse1F", 15, 3, "OreburghNorthHouse2F", 8, 3),
                     ("EternaCondominiums1F", 15, 3, "EternaCondominiums2F", 8, 3), ("EternaCondominiums2F", 15, 3, "EternaCondominiums3F", 8, 3),
                     ("SandgemTownCounterpartHouse1F", 9, 3, "SandgemTownCounterpartHouse2F", 6, 3)
                 })
        {
            var up = rooms[below].GetWarpAt(ux, uy)!;
            Assert.Equal((above, dx - 1, dy, Direction.Left), (up.TargetMap, up.TargetX, up.TargetY, up.TargetFacing));
            var down = rooms[above].GetWarpAt(dx, dy)!;
            Assert.Equal((below, ux + 1, uy, Direction.Right), (down.TargetMap, down.TargetX, down.TargetY, down.TargetFacing));
            Assert.Contains(rooms[below].Props, p => p.Type == PropType.SideStairsUp && p.Covers(ux - 1, uy));
            Assert.Contains(rooms[above].Props, p => p.Type == PropType.SideStairsDown && p.Covers(dx + 1, dy));
        }

        // A lift's doors are the original's door tile in the wall behind the floor, and lead to the tile before the
        // doors of the other floor
        foreach (string house in new[] { "HearthomeCitySoutheastHouse", "HearthomeCityNortheastHouse" })
            foreach (var (from, to) in new[] { ("1F", "2F"), ("2F", "1F") })
            {
                var floor = rooms[house + from];
                Assert.Equal(TileBehavior.Door, floor.BehaviourAt(18, 2));
                Assert.Equal(TileType.Wall, floor.GetGroundTile(18, 2));
                var lift = floor.GetWarpAt(18, 2)!;
                Assert.Equal((house + to, 18, 3, Direction.Down), (lift.TargetMap, lift.TargetX, lift.TargetY, lift.TargetFacing));
            }
    }

    // ------------------------------------------------------------------ what their people give

    /// <summary>A room, its giver's script, the flag that says it was given, and the gift.</summary>
    public static IEnumerable<object[]> Gifts => new[]
    {
        new object[] { "JubilifeCityCondominiums1F", "QuickClaw", "FLAG_RECEIVED_JUBILIFE_CITY_CONDOMINIUMS_1F_QUICK_CLAW", "Quick Claw" },
        new object[] { "OreburghCityNorthwestHouse2F", "DuskBall", "FLAG_RECEIVED_OREBURGH_CITY_NORTHWEST_HOUSE_2F_DUSK_BALL", "Dusk Ball" },
        new object[] { "OreburghCityEastHouse2F", "GreatBall", "FLAG_RECEIVED_OREBURGH_CITY_EAST_HOUSE_2F_GREAT_BALL", "Great Ball" },
        new object[] { "FlowerShop", "Sprayduck", "FLAG_RECEIVED_FLOWER_SHOP_SPRAYDUCK", "Sprayduck" },
        new object[] { "FloaromaTownMiddleHouse", "Pluck", "FLAG_RECEIVED_FLOAROMA_TOWN_MIDDLE_HOUSE_TM88", "TM88" },
        new object[] { "EternaCondominiums2F", "Recycle", "FLAG_RECEIVED_ETERNA_CITY_CONDOMINIUMS_2F_TM67", "TM67" },
        new object[] { "HearthomeCitySoutheastHouse2F", "ShellBell", "FLAG_RECEIVED_HEARTHOME_CITY_SOUTHEAST_HOUSE_2F_SHELL_BELL", "Shell Bell" },
    };

    [Theory]
    [MemberData(nameof(Gifts))]
    public void AGiftIsGivenOnceByTheOriginalsFlag(string room, string script, string flag, string item)
    {
        var host = new HeadlessScriptHost();
        host.Party.Add(Mon());
        Run(script, room, host);
        Assert.Equal(1, Have(host, item));
        Assert.True(host.Story.Has(flag));
        Run(script, room, host);
        Assert.Equal(1, Have(host, item));
        // And whoever gives it is in the room, on the original's own script
        Assert.Contains(Room(room).Everyone, n => n.Script == script);
    }

    [Fact]
    public void TheLittleGirlKeepsHerTmFromWhoeverDisagrees()
    {
        var host = new HeadlessScriptHost();
        host.Answers.Enqueue(1);
        Run("Pluck", "FloaromaTownMiddleHouse", host);
        Assert.Equal(0, Have(host, "TM88"));
        Assert.False(host.Story.Has("FLAG_RECEIVED_FLOAROMA_TOWN_MIDDLE_HOUSE_TM88"));
    }

    [Fact]
    public void TheHealBallIsForTheSightOfAGeodude()
    {
        var host = new HeadlessScriptHost();
        host.Party.Add(Mon());
        Run("Geodude", "OreburghNorthHouse2F", host);
        Assert.Equal(0, Have(host, "Heal Ball"));
        host.Party.Add(Mon("Geodude", 8));
        Run("Geodude", "OreburghNorthHouse2F", host);
        Run("Geodude", "OreburghNorthHouse2F", host);
        Assert.Equal(1, Have(host, "Heal Ball"));
        Assert.True(host.Story.Has("FLAG_RECEIVED_OREBURGH_CITY_NORTH_HOUSE_2F_HEAL_BALL"));
    }

    [Fact]
    public void BebeGivesHerEeveeOnceAndOnlyToATeamWithRoom()
    {
        var host = new HeadlessScriptHost();
        for (int i = 0; i < 6; i++) host.Party.Add(Mon());
        Run("Bebe", "HearthomeCityNorthwestHouse", host);
        Assert.True(host.Story.Has("FLAG_MET_BEBE"));
        Assert.False(host.Story.Has("FLAG_RECEIVED_HEARTHOME_CITY_NORTHWEST_HOUSE_EEVEE"));
        Assert.DoesNotContain(host.Party.Members, p => p.Species.Name == "Eevee");

        host.Party.Members.RemoveAt(5);
        // Turned down, she keeps it
        host.Answers.Enqueue(1);
        Run("Bebe", "HearthomeCityNorthwestHouse", host);
        Assert.Equal(5, host.Party.Count);
        Run("Bebe", "HearthomeCityNorthwestHouse", host);
        var eevee = Assert.Single(host.Party.Members, p => p.Species.Name == "Eevee");
        Assert.Equal(20, eevee.Level);
        Assert.True(host.Story.Has("FLAG_RECEIVED_HEARTHOME_CITY_NORTHWEST_HOUSE_EEVEE"));
        Run("Bebe", "HearthomeCityNorthwestHouse", host);
        Assert.Single(host.Party.Members, p => p.Species.Name == "Eevee");
    }

    [Fact]
    public void ARestOnRoute205HealsTheTeam()
    {
        var host = new HeadlessScriptHost();
        var lead = Mon();
        lead.CurrentHP = 1;
        host.Party.Add(lead);
        host.Answers.Enqueue(1);
        Run("Rest", "Route205House", host);
        Assert.Equal(1, lead.CurrentHP);
        Run("Rest", "Route205House", host);
        Assert.Equal(lead.MaxHP, lead.CurrentHP);
    }

    [Fact]
    public void TheAssistantsSisterTellsOfTheSwarmsOnceTheHallOfFameIsBehindThePlayer()
    {
        var host = new HeadlessScriptHost();
        Run("LittleSister", "SandgemTownCounterpartHouse1F", host);
        Assert.False(host.Story.Has("FLAG_TALKED_TO_COUNTERPART_SISTER_WITH_NATIONAL_DEX"));
        Assert.False(host.Encounters.SwarmsOn);
        host.Story.Set("FLAG_GAME_COMPLETED");
        Run("LittleSister", "SandgemTownCounterpartHouse1F", host);
        Assert.True(host.Story.Has("FLAG_TALKED_TO_COUNTERPART_SISTER_WITH_NATIONAL_DEX"));
        Assert.True(host.Encounters.SwarmsOn);
        // The common script is hers now, and nobody else's
        Assert.Contains(Room("SandgemTownCounterpartHouse1F").Everyone, n => n.Key == "twin" && n.Script == "LittleSister");
    }

    // ------------------------------------------------------------------ a script's die

    [Fact]
    public void AScriptsDieGivesItsResult()
    {
        var script = ScriptParser.Parse("test", "script Roll\n  random 6\n  if result == 4 setflag FLAG_ROLLED_FIVE\n  end\n")[0];
        var host = new HeadlessScriptHost();
        host.Rolls.Enqueue(4);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(script, null);
        runner.RunToEnd();
        Assert.True(host.Story.Has("FLAG_ROLLED_FIVE"));
        // A roll is taken within its die's sides, and a die needs a side
        host.Rolls.Enqueue(13);
        Assert.Equal(1, host.Random(6));
        Assert.Equal(0, host.Random(6));
        Assert.ThrowsAny<Exception>(() => ScriptParser.Parse("test", "script Roll\n  random 0\n  end\n"));
    }

    [Fact]
    public void TheFlowerShopsBerryOfTheDayIsOneOfFiveAndTheBerryMastersOneOfTwentySix()
    {
        string[] five = { "Cheri Berry", "Chesto Berry", "Pecha Berry", "Rawst Berry", "Aspear Berry" };
        for (int roll = 0; roll < five.Length; roll++)
        {
            var host = new HeadlessScriptHost();
            host.Rolls.Enqueue(roll);
            Run("Berry", "FlowerShop", host);
            Assert.Equal(1, Have(host, five[roll]));
            Assert.True(host.Story.Has("FLAG_DAILY_RECEIVED_FLOWER_SHOP_BERRY"));
            // Nothing more until the day's flags are cleared
            Run("Berry", "FlowerShop", host);
            Assert.Equal(1, five.Sum(b => Have(host, b)));
        }

        // Cheri to Tamato, in the order of the original's item numbers (ITEM_CHERI_BERRY + the roll)
        string[] berries =
        {
            "Cheri", "Chesto", "Pecha", "Rawst", "Aspear", "Leppa", "Oran", "Persim", "Lum", "Sitrus", "Figy", "Wiki", "Mago", "Aguav", "Iapapa",
            "Razz", "Bluk", "Nanab", "Wepear", "Pinap", "Pomeg", "Kelpsy", "Qualot", "Hondew", "Grepa", "Tamato"
        };
        for (int roll = 0; roll < berries.Length; roll++)
        {
            var host = new HeadlessScriptHost();
            host.Rolls.Enqueue(roll);
            Run("BerryMaster", "Route208House", host);
            Assert.Equal(1, Have(host, berries[roll] + " Berry"));
            Assert.True(host.Story.Has("FLAG_DAILY_RECEIVED_ROUTE_208_HOUSE_RANDOM_BERRY"));
            Run("BerryMaster", "Route208House", host);
            Assert.Equal(1, berries.Sum(b => Have(host, b + " Berry")));
        }
        // A day's flag is one the day clears
        Assert.StartsWith("FLAG_DAILY_", "FLAG_DAILY_RECEIVED_FLOWER_SHOP_BERRY");
    }

    /// <summary>
    /// The flower shop's accessories with the berry and the number of them each costs (the original's
    /// <c>sAccessoryShop_ItemLists</c>, src/overlay007/accessory_shop.c), and the answers that lead to each.
    /// </summary>
    public static IEnumerable<object[]> Accessories => new[]
    {
        new object[] { "Red Flower", "Cheri Berry", 1, new[] { 0, 0, 0, 0 } },
        new object[] { "Pink Flower", "Chesto Berry", 1, new[] { 0, 0, 0, 1 } },
        new object[] { "White Flower", "Pecha Berry", 1, new[] { 0, 0, 0, 2 } },
        new object[] { "Blue Flower", "Oran Berry", 1, new[] { 0, 0, 0, 3 } },
        new object[] { "Orange Flower", "Rawst Berry", 1, new[] { 0, 0, 0, 4, 0 } },
        new object[] { "Yellow Flower", "Aspear Berry", 1, new[] { 0, 0, 0, 4, 1 } },
        new object[] { "Googly Specs", "Leppa Berry", 1, new[] { 0, 0, 1, 0 } },
        new object[] { "Black Specs", "Persim Berry", 1, new[] { 0, 0, 1, 1 } },
        new object[] { "Gorgeous Specs", "Razz Berry", 10, new[] { 0, 1, 0 } },
        new object[] { "Sweet Candy", "Bluk Berry", 10, new[] { 0, 1, 1 } },
        new object[] { "Confetti", "Nanab Berry", 10, new[] { 0, 1, 2 } },
        new object[] { "Colored Parasol", "Wepear Berry", 10, new[] { 0, 1, 3 } },
        new object[] { "Old Umbrella", "Pinap Berry", 10, new[] { 0, 1, 4 } },
        new object[] { "Spotlight", "Cornn Berry", 50, new[] { 0, 2, 0 } },
        new object[] { "Cape", "Pamtre Berry", 100, new[] { 0, 2, 4, 0 } },
        new object[] { "Standing Mike", "Magost Berry", 50, new[] { 0, 2, 1 } },
        new object[] { "Surfboard", "Watmel Berry", 100, new[] { 0, 2, 4, 1 } },
        new object[] { "Carpet", "Rabuta Berry", 50, new[] { 0, 2, 2 } },
        new object[] { "Retro Pipe", "Nomel Berry", 50, new[] { 0, 2, 3 } },
        new object[] { "Fluffy Bed", "Durin Berry", 100, new[] { 0, 2, 4, 2 } },
        new object[] { "Mirror Ball", "Spelon Berry", 100, new[] { 0, 2, 4, 3 } },
        new object[] { "Photo Board", "Belue Berry", 100, new[] { 0, 2, 4, 4 } },
    };

    [Theory]
    [MemberData(nameof(Accessories))]
    public void TheFloristSwapsBerriesForAnAccessoryAtTheOriginalsPrice(string accessory, string berry, int price, int[] answers)
    {
        int id = SuperContestDatabase.Accessory(accessory);
        Assert.True(id >= 0, accessory);
        HeadlessScriptHost With(int berries)
        {
            var host = new HeadlessScriptHost();
            if (berries > 0) host.Bag.AddItem(ItemDatabase.Get(berry)!, berries);
            foreach (int answer in answers) host.Answers.Enqueue(answer);
            return Run("Accessories", "FlowerShop", host);
        }
        // One berry short: nothing changes hands
        var poor = With(price - 1);
        Assert.Equal(0, poor.FashionCase.Count(id));
        Assert.Equal(price - 1, Have(poor, berry));
        // The price exactly: the accessory for the berries
        var rich = With(price);
        Assert.Equal(1, rich.FashionCase.Count(id));
        Assert.Equal(0, Have(rich, berry));
    }

    [Fact]
    public void TheBerryMastersHouseSellsMulchAndGivesTheBerrySearcher()
    {
        var host = new HeadlessScriptHost { Money = 500 };
        host.Answers.Enqueue(1);
        Run("Mulch", "Route208House", host);
        Assert.Equal((1, 300), (Have(host, "Damp Mulch"), host.Money));
        host.Money = 150;
        host.Answers.Enqueue(3);
        Run("Mulch", "Route208House", host);
        Assert.Equal((0, 150), (Have(host, "Gooey Mulch"), host.Money));
        // "No, thanks" is the last answer, and costs nothing
        host.Answers.Enqueue(4);
        Run("Mulch", "Route208House", host);
        Assert.Equal(150, host.Money);

        // The app goes onto a Pokétch, to whoever owns up to forgetting
        var forgetful = new HeadlessScriptHost();
        forgetful.Poketch.Enabled = true;
        Run("BerrySearcher", "Route208House", forgetful);
        Assert.True(forgetful.Poketch.Has(PoketchApp.BerrySearcher));
        var tidy = new HeadlessScriptHost();
        tidy.Poketch.Enabled = true;
        tidy.Answers.Enqueue(1);
        Run("BerrySearcher", "Route208House", tidy);
        Assert.False(tidy.Poketch.Has(PoketchApp.BerrySearcher));

        // The book lies on the table, and is read where it lies
        var house = Room("Route208House");
        Assert.Equal("Book", house.SignScripts[(4, 6)]);
        Assert.Contains(house.Props, p => p.Type == PropType.Table && p.Covers(4, 6));
    }

    [Fact]
    public void TheHerbShopSellsEternasHerbs()
    {
        var shop = Room("EternaCityHerbShop");
        var clerk = Assert.Single(shop.Everyone, n => n.IsPokeMartClerk);
        Assert.Equal(("cashier_f", "eterna_house", 3, 4), (clerk.Key, clerk.Mart, clerk.GridX, clerk.GridY));
        // She serves across the counter in front of her
        Assert.True(shop.IsCounter(3, 5));
        Assert.True(shop.IsWalkable(3, 6));
        Assert.Contains("Heal Powder", MartDatabase.Specialties["eterna_house"]);
    }

    // ------------------------------------------------------------------ the Old Chateau

    private static readonly string[] Chateau =
    {
        "OldChateau", "OldChateauDiningArea", "OldChateauWestRoom", "OldChateauEastRoom", "OldChateauCorridor", "OldChateauBackWestRoom",
        "OldChateauBackMiddleWestRoom", "OldChateauBackMiddleRoom", "OldChateauBackMiddleEastRoom", "OldChateauBackEastRoom"
    };

    [Fact]
    public void TheOldChateausWildPokemonLiveOnItsFloor()
    {
        foreach (string name in Chateau)
        {
            var room = Room(name);
            // Platinum's table and rate (res/field/encounters/encounters_old_chateau*.json: Gastly on every floor)
            Assert.Equal(12, room.WildEncounters.Count);
            Assert.Contains(room.WildEncounters, e => e.SpeciesName == "Gastly");
            Assert.True(room.EncounterRate > 0);
            // They are met on the tiles of the original's own floor behaviour, as the Lost Tower's are
            int haunted = 0;
            for (int y = 0; y < room.Height; y++)
                for (int x = 0; x < room.Width; x++)
                    if (room.BehaviourAt(x, y) == TileBehavior.OldChateauFloor) { haunted++; Assert.Equal(TileType.Floor, room.GetGroundTile(x, y)); }
            Assert.True(haunted >= 20, $"{name}: only {haunted} tiles of the chateau's floor");
            Assert.Equal("Old Chateau", room.DisplayName);
        }
    }

    [Fact]
    public void TheOldChateausHallHasItsGalleryAndTheItemsLieInItsRooms()
    {
        var hall = Room("OldChateau");
        // The gallery runs round three sides, three tiles up; the two flights climb to it from the hall's floor
        Assert.True(hall.HasRelief);
        Assert.Equal(3f, hall.HeightAt(2, 6));
        Assert.Equal(3f, hall.HeightAt(9, 3));
        Assert.Equal(0f, hall.HeightAt(9, 7));
        Assert.Equal(0f, hall.HeightAt(9, 14));
        Assert.InRange(hall.HeightAt(5, 10), 0.5f, 2.5f);
        Assert.InRange(hall.HeightAt(13, 10), 0.5f, 2.5f);
        // The flights are a tile wide to the feet, whatever is drawn of them
        Assert.All(new[] { (4, 10), (5, 10), (6, 10), (12, 10), (13, 10), (14, 10) }, t => Assert.True(hall.IsWalkable(t.Item1, t.Item2)));
        Assert.All(new[] { (5, 9), (5, 11), (13, 9), (13, 11) }, t => Assert.False(hall.IsWalkable(t.Item1, t.Item2)));
        // The statue under the gallery is read where it stands
        Assert.Contains("glaring", hall.Signboards[(7, 6)]);
        Assert.Contains(hall.Props, p => p.Type == PropType.Statue && p.Covers(7, 6));

        // What lies in the rooms, each ball once: a room that shows the room next door doesn't show its ball again
        var balls = Chateau.SelectMany(name => Room(name).Everyone.Where(n => n.IsItemBall).Select(n => (name, n.Item, n.HiddenBy))).ToList();
        Assert.Equal(new[]
        {
            ("OldChateauBackEastRoom", "TM90"), ("OldChateauBackWestRoom", "Dread Plate"), ("OldChateauDiningArea", "Big Pearl"),
            ("OldChateauDiningArea", "Rare Candy"), ("OldChateauWestRoom", "Old Gateau")
        }, balls.Select(b => (b.name, b.Item!)).OrderBy(b => b.name, StringComparer.Ordinal).ThenBy(b => b.Item2, StringComparer.Ordinal));
        Assert.Equal(balls.Count, balls.Select(b => b.HiddenBy).Distinct().Count());
        var hidden = Assert.Single(Room("OldChateauDiningArea").HiddenItems);
        Assert.Equal(("Honey", 3), (hidden.Value.Item, hidden.Value.Count));
        // The notebook in the east room off the gallery
        Assert.Contains("notebook", Room("OldChateauEastRoom").Signboards.Values.Single());
    }

    [Fact]
    public void TheTelevisionRotomLivesInSaysNothingYet()
    {
        // Plan 02 · S15c gives it its scene: the set stands at (11, 3) and (12, 3) of the second room off the corridor
        var room = Room("OldChateauBackMiddleWestRoom");
        Assert.Contains(room.Props, p => p.Type == PropType.Television && p.Covers(11, 3) && p.Covers(12, 3));
        Assert.True(room.IsWalkable(11, 4));
        Assert.True(room.IsWalkable(12, 4));
        Assert.DoesNotContain(room.Signboards.Keys, k => k is (11, 3) or (12, 3));
        Assert.DoesNotContain(room.SignScripts.Keys, k => k is (11, 3) or (12, 3));
        Assert.Null(ScriptLibrary.Default.Find("TV", "OldChateauBackMiddleWestRoom"));
    }

    [Theory]
    [InlineData("OldChateauDiningArea", "old_man", "FLAG_HIDE_OLD_CHATEAU_DINING_AREA_OLD_MAN", 10, 19, 11)]
    [InlineData("OldChateauBackMiddleEastRoom", "twin", "FLAG_HIDE_OLD_CHATEAU_BACK_MIDDLE_EAST_ROOM_TWIN", 25, 13, 7)]
    public void AGhostOfTheOldChateauShowsOnTheOriginalsRollAndIsGone(string name, string who, string flag, int chance, int doorX, int doorY)
    {
        // CheckShouldShowGhost: a die of a hundred and one, shown on the chance or less
        foreach (var (roll, shown) in new[] { (0, true), (chance, true), (chance + 1, false), (100, false) })
        {
            var room = Room(name);
            var ghost = room.Everyone.Single(n => n.Key == who);
            var home = (ghost.GridX, ghost.GridY);
            var host = new HeadlessScriptHost { Map = room, PlayerTile = (doorX, doorY), PlayerFacing = Direction.Up };
            host.Rolls.Enqueue(roll);
            Run(ScriptLibrary.OnEnter, name, host);
            // Either way nobody is left standing there once the room has been come into
            Assert.True(host.Story.Has(flag));
            // Shown, the ghost goes its way and is gone; not shown, nobody was ever there
            Assert.Equal(shown, (ghost.GridX, ghost.GridY) != home);
            Assert.Equal(flag, ghost.HiddenBy);
        }
    }

    // ------------------------------------------------------------------ how they are built and drawn

    [Fact]
    public void ASeatIsWalkedOntoAndAStandOfFlowersIsNot()
    {
        Assert.False(new Prop { Type = PropType.Cushion }.IsSolid);
        // Whoever sits at a table stands on the seat's own tile: Twinleaf's grandmother, Oreburgh's boy
        var house = Room("TwinleafTownSouthwestHouse");
        Assert.Contains(house.Props, p => p.Type == PropType.Cushion && p.Covers(5, 5));
        Assert.Equal((5, 5), (house.FindPerson("expert_f")!.GridX, house.FindPerson("expert_f")!.GridY));
        // An empty one is floor
        var condo = Room("JubilifeCityCondominiums1F");
        var empty = condo.Props.Where(p => p.Type == PropType.Cushion && condo.Everyone.All(n => !p.Covers(n.GridX, n.GridY))).ToList();
        Assert.NotEmpty(empty);
        Assert.All(empty, p => Assert.True(condo.IsWalkable(p.X, p.Y)));

        // The flower shop's stands block by the room's own tiles
        var shop = Room("FlowerShop");
        var stands = shop.Props.Where(p => p.Type == PropType.FlowerStand).ToList();
        Assert.True(stands.Count >= 5);
        foreach (var stand in stands)
            for (int y = stand.Y; y < stand.Y + stand.Depth; y++)
                for (int x = stand.X; x < stand.X + stand.Width; x++)
                    Assert.True(shop.IsSolid(x, y), $"the flower stand at {x},{y} can be walked through");
        // Six blooms' colours, dealt round by the tile
        var first = new PixelCanvas(30, 44);
        var second = new PixelCanvas(30, 44);
        PropModels.PaintFlowerStand(first, 0);
        PropModels.PaintFlowerStand(second, 1);
        Assert.NotEqual(first.Get(4, 27), second.Get(4, 27));
    }

    [Fact]
    public void ABlockOfWallAgainstTheBackWallStandsFullHeightAndAPartitionIsSeenOver()
    {
        // The flank of a staircase: wall from the back wall forward with nothing behind it
        var condo = Room("JubilifeCityCondominiums1F");
        Assert.All(new[] { (11, 3), (12, 3), (11, 4), (12, 4) }, t => Assert.True(MapScene.BackWallJog(condo, t.Item1, t.Item2)));
        // The partitions between the Old Chateau's rooms have floor on both sides: cut to the wainscot
        var rooms = Room("OldChateauBackWestRoom");
        Assert.Equal(TileType.Wall, rooms.GetGroundTile(7, 3));
        Assert.All(Enumerable.Range(3, 5), y => Assert.False(MapScene.BackWallJog(rooms, 7, y)));
        // A block with floor behind it is an inner wall like any other
        var hearthome = Room("HearthomeCitySoutheastHouse1F");
        Assert.Equal(TileType.Wall, hearthome.GetGroundTile(5, 6));
        Assert.False(MapScene.BackWallJog(hearthome, 5, 6));
        // And no room that isn't a house has one: Team Galactic's walls stay as they were
        var lab = Room("GalacticHQ1F");
        for (int y = 0; y < lab.Height; y++)
            for (int x = 0; x < lab.Width; x++)
                Assert.False(MapScene.BackWallJog(lab, x, y));
    }

    [Fact]
    public void TheWaysOnThroughAHousesBackWallAreItsOwn()
    {
        // The corridor of the Old Chateau: five doorways in the wall behind its floor, each a warp walked into
        var corridor = Room("OldChateauCorridor");
        var (_, back) = corridor.RoomCorner();
        var doorways = corridor.Warps.Where(w => w.SourceY == back - 1).ToList();
        Assert.Equal(5, doorways.Count);
        Assert.All(doorways, w =>
        {
            Assert.Equal(TileType.Wall, corridor.GetGroundTile(w.SourceX, w.SourceY));
            Assert.False(corridor.IsSolid(w.SourceX, w.SourceY));
            Assert.NotEqual(TileBehavior.Door, corridor.BehaviourAt(w.SourceX, w.SourceY));
        });

        // A doorway framed in wood, a lift's doors of steel, neither Team Galactic's panel
        PixelCanvas Painted(Action<PixelCanvas> paint) { var c = new PixelCanvas(PropModels.DoorwayWidth, PropModels.DoorwayHeight); paint(c); return c; }
        var house = Painted(PropModels.PaintDoorway);
        var lift = Painted(PropModels.PaintLiftDoors);
        var galactic = Painted(GalacticArt.PaintDoorway);
        Assert.Equal((GalacticArt.DoorwayWidth, GalacticArt.DoorwayHeight), (PropModels.DoorwayWidth, PropModels.DoorwayHeight));
        Assert.NotEqual(house.Get(1, 20), galactic.Get(1, 20));
        Assert.NotEqual(house.Get(14, 30), lift.Get(14, 30));
        // The doorway is dark all the way down; the lift's leaves are not
        Assert.True(house.Get(14, 30).R < 60);
        Assert.True(lift.Get(10, 30).R > 120);

        // A house's upper level stands on its own wainscot, a panel repeating whole down the face
        var face = GroundBaker.RoomFace();
        Assert.Equal((32, NatureArt.FaceCap + NatureArt.FaceBody), (face.Width, face.Height));
        for (int x = 0; x < 32; x++)
            Assert.Equal(face.Get(x, NatureArt.FaceCap + 5), face.Get(x, NatureArt.FaceCap + 29));
    }

    [Fact]
    public void TheRoomsAreFurnishedAsTheOriginalsAre()
    {
        // The small house every town has: a television and a shelf against the back wall, a table with a seat
        foreach (string name in new[] { "TwinleafTownNortheastHouse", "SandgemTownHouse", "OreburghCityWestHouse", "FloaromaTownMiddleHouse", "SolaceonTownNorthHouse" })
        {
            var house = Room(name);
            Assert.Equal((11, 10), (house.Width, house.Height));
            Assert.Contains(house.Props, p => p.Type == PropType.Television && p.Covers(6, 3));
            Assert.Contains(house.Props, p => p.Type == PropType.Bookshelf && p.Covers(9, 3));
            Assert.Contains(house.Props, p => p.Type == PropType.Table && p.Covers(3, 5) && p.Covers(4, 6));
            Assert.Contains(house.Props, p => p.Type == PropType.Window);
            Assert.Contains(house.Props, p => p.Type == PropType.Rug && p.Covers(4, 8));
        }
        // No room is empty of furniture, and none has a piece standing on its mat or its ways out
        foreach (string name in AllRooms)
        {
            var room = Room(name);
            Assert.True(room.Props.Count(p => p.IsSolid || p.Type == PropType.FlowerStand) >= 1, $"{name} is bare");
            foreach (var warp in room.Warps)
                Assert.DoesNotContain(room.Props, p => (p.IsSolid || p.Type == PropType.FlowerStand) && p.Covers(warp.SourceX, warp.SourceY));
        }
        // The Foreign Building's pews and the dining room's long table are the rooms' own
        Assert.Equal(6, Room("ForeignBuilding").Props.Count(p => p.Type == PropType.Bench));
        Assert.Contains(Room("OldChateauDiningArea").Props, p => p.Type == PropType.Table && p.Width == 18 && p.Depth == 4);
    }
}
