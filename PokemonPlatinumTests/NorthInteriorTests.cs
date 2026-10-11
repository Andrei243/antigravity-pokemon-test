using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// The rooms of the north and of the Battle Zone as plan 01 · M11, part c built them, on the original's plans:
/// Celestic Town's shop and two houses, the Snowbound Lodge and Route 217's two houses, Route 222's fisherman and the
/// Pikachu Fan Club, Sunyshore City's three houses, the Fight Area's two, the houses of Routes 225 to 228, of the
/// Survival Area and of the Resort Area, and the Ribbon Syndicate's lift and spa. Where their people stand is
/// <see cref="RoomPlanTests"/>'s.
/// </summary>
public class NorthInteriorTests
{
    // Maps of this class's own, so nothing another test does to the database's maps shows here
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Sinnoh"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));

    private static World Sinnoh => LoadedWorld.Value;

    private static Map Room(string name) => GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    private static int Have(HeadlessScriptHost host, string item) => host.Bag.GetQuantity(ItemDatabase.Get(item)!);

    private static Pokemon Mon(string species = "Turtwig", int level = 20, Nature nature = Nature.Hardy) =>
        new(PokemonDatabase.Get(species)!, level, Gender.Male, nature, false);

    private static HeadlessScriptHost Run(string script, string place, HeadlessScriptHost host, NPC? who = null)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, place) ?? throw new InvalidOperationException($"no script {place}.{script}"), who);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    private static string Said(HeadlessScriptHost host) => string.Join(" | ", host.Transcript.Select(t => t.Text));

    /// <summary>Each door part c opened: the area, its warp's number, the room, and the mat inside it.</summary>
    public static IEnumerable<object[]> Doors => new[]
    {
        new object[] { "celestic_town", 1, "CelesticTownNorthwestHouse", 4, 8 },
        new object[] { "celestic_town", 2, "CelesticTownNortheastHouse", 4, 8 },
        new object[] { "celestic_town", 3, "CelesticTownSouthwestHouse", 4, 8 },
        new object[] { "route_216", 1, "Route216House", 4, 8 },
        new object[] { "route_217", 0, "Route217WestHouse", 4, 8 },
        new object[] { "route_217", 1, "Route217NortheastHouse", 4, 8 },
        new object[] { "route_222", 2, "Route222EastHouse", 4, 8 },
        new object[] { "route_222", 3, "Route222WestHouse", 4, 8 },
        new object[] { "sunyshore_city", 2, "SunyshoreCityWestHouse", 4, 8 },
        new object[] { "sunyshore_city", 4, "SunyshoreCityNorthwestHouse", 4, 8 },
        new object[] { "sunyshore_city", 9, "SunyshoreCityEastHouse", 4, 8 },
        new object[] { "fight_area", 4, "FightAreaMiddleHouse", 4, 8 },
        new object[] { "fight_area", 5, "FightAreaSouthHouse", 4, 8 },
        new object[] { "route_225", 2, "Route225House", 4, 8 },
        new object[] { "survival_area", 1, "SurvivalAreaSouthHouse", 4, 8 },
        new object[] { "route_226", 0, "Route226House", 4, 8 },
        new object[] { "route_227", 0, "Route227House", 4, 8 },
        new object[] { "route_228", 3, "Route228SouthHouse", 4, 8 },
        new object[] { "resort_area", 1, "ResortAreaHouse", 4, 8 },
    };

    /// <summary>The rooms of part c that no door of the map of Sinnoh leads into: the Ribbon Syndicate's lift and spa.</summary>
    private static readonly string[] Inner = { "ResortAreaRibbonSyndicate2F", "ResortAreaRibbonSyndicateElevator" };

    private static IEnumerable<string> AllRooms => Doors.Select(d => (string)d[2]).Concat(Inner).Distinct();

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
        Assert.True(inside.IsIndoors);
        Assert.Equal(InteriorStyle.House, inside.Interior);
        Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap(room)!.Id);
        Assert.True(inside.IsWalkable(matX, matY), $"{room}: the mat is no floor");
        var exit = inside.GetWarpAt(matX, matY + 1);
        Assert.NotNull(exit);
        Assert.Equal((outside.Name, way.X, way.Z + 1, Direction.Down), (exit!.TargetMap, exit.TargetX, exit.TargetY, exit.TargetFacing));
        Assert.True(outside.IsWalkable(way.X, way.Z + 1), $"{room}: nobody can stand before its door");
        // As its place's rooms have it: the place's own theme
        Assert.Equal(outside.BgmTrackAt(way.X, way.Z + 1), inside.BgmTrack);
    }

    [Fact]
    public void AHouseThatShutsAPocketInIsOneBlockWithItsDoorInItsFront()
    {
        // The original leaves a tile or two open in the middle of four buildings of the Battle Zone, shut in by their
        // own walls. Cut into rectangles round the pocket they fell apart (Route 225's house into a small block, a ring
        // of fence and a door under the fence; the Survival Area's south house and the Battleground into two halves):
        // each is one block now, the box of the tiles it blocks
        var map = BuiltMaps.Value["Sinnoh"];
        var houses = map.PlacedBuildings!;
        foreach (var (x0, y0, x1, y1, door, room) in new[]
        {
            (616, 366, 620, 368, 617, (string?)"Route225House"),
            (654, 346, 658, 348, 655, "SurvivalAreaSouthHouse"),
            (655, 323, 658, 325, 656, "SurvivalAreaNorthHouse"),   // the tutor's house (part d)
            (651, 336, 655, 338, 652, "Battleground"),   // the Battleground (plan 02 · S15, part c)
        })
        {
            var house = Assert.Single(houses, b => b.X0 <= door && b.X1 >= door && b.Y0 <= y1 && b.Y1 >= y0);
            Assert.Equal((x0, y0, x1, y1), (house.X0, house.Y0, house.X1, house.Y1));
            // Every tile of it is the building's and blocked, the pocket too, but for the door of one that is open
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    bool way = room != null && (x, y) == (door, y1);
                    Assert.Equal(!way, map.IsSolid(x, y));
                    Assert.DoesNotContain(map.Props, p => p.Type is PropType.Fence or PropType.LowWall && p.Covers(x, y));
                }
            Assert.Equal(TileType.Door, map.GetGroundTile(door, y1));
            Assert.Contains(house.Doors, d => d.X == door && d.Target == room);
        }
        // A house whose pocket left it no block of two tiles each way was one block already, and is as it was
        // (Route 212's, the shard tutor's)
        var south = Assert.Single(houses, b => b.X0 <= 517 && b.X1 >= 517 && b.Y0 <= 846 && b.Y1 >= 844);
        Assert.Equal((516, 844, 520, 846), (south.X0, south.Y0, south.X1, south.Y1));
        // A building bigger than a house keeps its shape whatever it shuts in: Oreburgh's museum is cut as it was
        Assert.Contains(houses, b => b.Model == "c3_s01" && (b.X0, b.Y0, b.X1, b.Y1) == (310, 747, 313, 753));
        Assert.Contains(map.Props, p => p.Type == PropType.LowWall && p.Covers(312, 754));
        Assert.DoesNotContain(map.Props, p => p.Type == PropType.LowWall && p.Covers(313, 754));
    }

    [Fact]
    public void WhatIsStillShutInTheseAreasIsSomeoneElses()
    {
        // Part d built the three move tutors' houses (Snowpoint City's east house, the Survival Area's north house and
        // Route 228's north house: FinalInteriorTests). The Battleground and the Rock Peak Ruins are the story's; two
        // houses of Sunyshore City are ones the original's own files call unused. Snowpoint Temple's west door is open
        // since plan 02 · S15, part b (the trigger before it keeps it); its east one, which the guard never leaves,
        // stays shut
        Assert.Equal(new[] { 6 }, Sinnoh.Overlay("snowpoint_city")!.Locked);
        Assert.Equal(new[] { 7, 8 }, Sinnoh.Overlay("sunyshore_city")!.Locked);
        Assert.Equal(new[] { 4, 5 }, Sinnoh.Overlay("route_228")!.Locked);
        foreach (string area in new[] { "celestic_town", "route_216", "route_217", "route_222", "fight_area", "route_225", "route_226", "route_227", "resort_area", "survival_area" })
            Assert.True(Sinnoh.Overlay(area)!.Locked is not { Count: > 0 }, $"{area} still has a door shut");
        Assert.Equal("unused_sunyshore_city_house_1", Sinnoh.Area("sunyshore_city")!.Warps[7].To);
        Assert.Equal("unused_sunyshore_city_house_2", Sinnoh.Area("sunyshore_city")!.Warps[8].To);
        // The Battleground, which this part left shut, is built since plan 02 · S15, part c, and opens with the story
        Assert.Equal("FLAG_ARRESTED_CHARON_STARK_MOUNTAIN", Sinnoh.Overlay("survival_area")!.Doors!.Single(d => d.Map == "Battleground").OpenedBy);
    }

    [Fact]
    public void EveryoneStandsOnFloorWithSomethingToSayAndEveryWayLeadsSomewhereOneCanStand()
    {
        var rooms = AllRooms.ToDictionary(n => n, Room);
        rooms["ResortAreaRibbonSyndicate1F"] = Room("ResortAreaRibbonSyndicate1F");
        Assert.True(rooms.Count >= 22, $"only {rooms.Count} rooms");
        foreach (var (name, map) in rooms)
        {
            Assert.True(map.IsIndoors, name);
            Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap(name)!.Id);
            foreach (var who in map.Everyone)
            {
                string where = $"{name}: {who.Key} at {who.GridX},{who.GridY}";
                Assert.False(map.IsSolid(who.GridX, who.GridY), where + " stands in something solid");
                Assert.True(map.GetGroundTile(who.GridX, who.GridY) == TileType.Floor, where + " stands on no floor");
                Assert.True(who.DialogLines.Count > 0 || who.Script != null || who.IsItemBall || who.IsTrainer, where + " has nothing to say");
                if (who.Script != null) Assert.True(ScriptLibrary.Default.Find(who.Script, name) != null, where + $" runs '{who.Script}', which is nowhere");
                Assert.Single(map.Everyone, other => (other.GridX, other.GridY) == (who.GridX, who.GridY));
                Assert.Null(map.GetWarpAt(who.GridX, who.GridY));
            }
            foreach (var (tile, script) in map.SignScripts)
                Assert.True(ScriptLibrary.Default.Find(script, name) != null, $"{name}: what is read at {tile} runs '{script}', which is nowhere");
            foreach (var warp in map.Warps)
            {
                var target = rooms.GetValueOrDefault(warp.TargetMap) ?? BuiltMaps.Value[warp.TargetMap];
                Assert.True(target.IsWalkable(warp.TargetX, warp.TargetY), $"{name}: the way at {warp.SourceX},{warp.SourceY} leads into something solid of {warp.TargetMap}");
                Assert.False(map.IsSolid(warp.SourceX, warp.SourceY), $"{name}: the way at {warp.SourceX},{warp.SourceY} is walled up");
            }
            // No room is bare, and nothing stands on a way out
            Assert.Contains(map.Props, p => p.IsSolid || p.Type is PropType.LiftIndicator);
            foreach (var warp in map.Warps)
                Assert.DoesNotContain(map.Props, p => (p.IsSolid || p.Type == PropType.BookPile) && p.Covers(warp.SourceX, warp.SourceY));
        }
        // Every script of these rooms is free of what the library checks beyond grammar
        Assert.DoesNotContain(ScriptLibrary.Default.Problems().ToList(), p => rooms.Keys.Any(r => p.Contains(r + ".txt", StringComparison.Ordinal)));
    }

    // ------------------------------------------------------------------ Celestic Town

    [Fact]
    public void CelesticTownsShopHasTheTwoCountersAMartHas()
    {
        var shop = Room("CelesticTownNorthwestHouse");
        var his = Assert.Single(shop.Everyone, n => n.Key == "expert_m");
        var hers = Assert.Single(shop.Everyone, n => n.Key == "expert_f");
        // He sells the town's own things, she what every Mart sells; both are the common clerk, served across the counter
        Assert.True(his.IsPokeMartClerk && hers.IsPokeMartClerk);
        Assert.Equal("celestic", his.Mart);
        Assert.Null(hers.Mart);
        Assert.Equal(FieldScripts.Clerk, FieldScripts.For(his));
        Assert.Equal(new[] { "Air Mail", "Dusk Ball", "Quick Ball", "Timer Ball" }, MartDatabase.Specialties["celestic"]);
        foreach (var clerk in new[] { his, hers })
        {
            Assert.Equal(Direction.Down, clerk.Facing);
            Assert.Equal(TileBehavior.Counter, shop.BehaviourAt(clerk.GridX, clerk.GridY + 1));
            Assert.Contains(shop.Props, p => p.Type == PropType.Counter && p.Covers(clerk.GridX, clerk.GridY + 1));
            Assert.True(shop.IsWalkable(clerk.GridX, clerk.GridY + 2));
        }
        var host = Run(FieldScripts.Clerk, "CelesticTownNorthwestHouse", new HeadlessScriptHost { Map = shop }, his);
        Assert.Contains("open Shop celestic", host.Log);
    }

    [Theory]
    [InlineData(TimeOfDay.Morning, "Choice Specs", "FLAG_RECEIVED_CELESTIC_TOWN_NORTHWEST_HOUSE_CHOICE_SPECS")]
    [InlineData(TimeOfDay.Day, "Black Glasses", "FLAG_RECEIVED_CELESTIC_TOWN_NORTHWEST_HOUSE_BLACKGLASSES")]
    [InlineData(TimeOfDay.Twilight, "Black Glasses", "FLAG_RECEIVED_CELESTIC_TOWN_NORTHWEST_HOUSE_BLACKGLASSES")]
    [InlineData(TimeOfDay.Night, "Wise Glasses", "FLAG_RECEIVED_CELESTIC_TOWN_NORTHWEST_HOUSE_WISE_GLASSES")]
    [InlineData(TimeOfDay.LateNight, "Wise Glasses", "FLAG_RECEIVED_CELESTIC_TOWN_NORTHWEST_HOUSE_WISE_GLASSES")]
    public void TheManInGlassesGivesAPairByTheHourEachOnce(TimeOfDay time, string item, string flag)
    {
        var host = new HeadlessScriptHost { TimeOfDay = time };
        Run("Glasses", "CelesticTownNorthwestHouse", host);
        Assert.Equal(1, Have(host, item));
        Assert.True(host.Story.Has(flag));
        Run("Glasses", "CelesticTownNorthwestHouse", host);
        Assert.Equal(1, Have(host, item));
        // Another hour's pair is still to be had
        string other = item == "Choice Specs" ? "Wise Glasses" : "Choice Specs";
        host.TimeOfDay = item == "Choice Specs" ? TimeOfDay.Night : TimeOfDay.Morning;
        Run("Glasses", "CelesticTownNorthwestHouse", host);
        Assert.Equal(1, Have(host, other));
    }

    [Fact]
    public void TheBlackBeltGivesHisWatchAway()
    {
        var host = Run("Watch", "CelesticTownSouthwestHouse", new HeadlessScriptHost());
        Assert.True(host.Poketch.Has(PoketchApp.AnalogWatch));
        Assert.Contains("hourglass", host.Transcript[^1].Text);
        int said = host.Transcript.Count;
        Run("Watch", "CelesticTownSouthwestHouse", host);
        Assert.Single(host.Poketch.Apps, a => a == PoketchApp.AnalogWatch);
        // (the second time he only has his word about watches)
        Assert.Equal(said + 2, host.Transcript.Count);
        Assert.Contains("hourglass", host.Transcript[^1].Text);
        // His Happiny is a Pokémon of the room
        var happiny = Assert.Single(Room("CelesticTownSouthwestHouse").Everyone, n => n.Key == "happiny");
        Assert.True(happiny.IsPokemon);
        Assert.Equal("Happiny", happiny.Species);
    }

    // ------------------------------------------------------------------ the snow

    [Fact]
    public void TheLodgesKeeperAndItsBedGiveARest()
    {
        var lodge = Room("Route216House");
        var hurt = Mon();
        hurt.CurrentHP = 1;
        var host = new HeadlessScriptHost { Map = lodge };
        host.Party.Add(hurt);
        host.Answers.Enqueue(1);
        Run("Rest", "Route216House", host);
        Assert.Equal(1, hurt.CurrentHP);
        Run("Rest", "Route216House", host);
        Assert.Equal(hurt.MaxHP, hurt.CurrentHP);

        // The bed is read from beside it and from its foot
        Assert.Contains(lodge.Props, p => p.Type == PropType.Bed && p.Covers(8, 6) && p.Covers(8, 7));
        Assert.Equal("Bed", lodge.SignScripts[(8, 6)]);
        Assert.Equal("Bed", lodge.SignScripts[(8, 7)]);
        Assert.True(lodge.IsWalkable(7, 6) && lodge.IsWalkable(9, 6) && lodge.IsWalkable(8, 8));
        hurt.CurrentHP = 1;
        host.Answers.Enqueue(1);
        Run("Bed", "Route216House", host);
        Assert.Equal(1, hurt.CurrentHP);
        Run("Bed", "Route216House", host);
        Assert.Equal(hurt.MaxHP, hurt.CurrentHP);
    }

    [Fact]
    public void TheHikerRewardsWhoeverFoundHisHiddenMachine()
    {
        var host = Run("Hiker", "Route217WestHouse", new HeadlessScriptHost());
        Assert.Equal(0, Have(host, "Icicle Plate"));
        Assert.False(host.Story.Has("FLAG_RECEIVED_ROUTE_217_WEST_HOUSE_ICICLE_PLATE"));
        host.Bag.AddItem(ItemDatabase.Get("HM08")!, 1);
        Run("Hiker", "Route217WestHouse", host);
        Assert.Equal((1, 1), (Have(host, "Icicle Plate"), Have(host, "HM08")));
        Run("Hiker", "Route217WestHouse", host);
        Assert.Equal(1, Have(host, "Icicle Plate"));
        // HM08 lies in its ball on the route outside
        Assert.Contains(BuiltMaps.Value["Sinnoh"].Everyone, n => n.IsItemBall && n.Item == "HM08");
    }

    [Fact]
    public void TheWomanOfTheSnowGivesASpellTagAndIsGoneTheNextTime()
    {
        var house = Room("Route217NortheastHouse");
        var woman = Assert.Single(house.Everyone);
        Assert.Equal("FLAG_HIDE_ROUTE_217_NORTHEAST_HOUSE_POKEMON_BREEDER_F", woman.HiddenBy);
        var host = new HeadlessScriptHost { Map = house };
        Run("Woman", "Route217NortheastHouse", host, woman);
        Assert.Equal(1, Have(host, "Spell Tag"));
        Assert.True(host.Story.Has("FLAG_HIDE_ROUTE_217_NORTHEAST_HOUSE_POKEMON_BREEDER_F"));
        // She is there for the rest of the visit, with a word about her gift
        house.ApplyPresence(host.Story.Has);
        Assert.Contains(woman, house.NPCs);
        Run("Woman", "Route217NortheastHouse", host, woman);
        Assert.Equal(1, Have(host, "Spell Tag"));
        Assert.Contains("Ghost-type", host.Transcript[^2].Text);
        // And not when the house is come to again
        house.ForgetForced();
        house.ApplyPresence(host.Story.Has);
        Assert.Empty(house.NPCs);
    }

    // ------------------------------------------------------------------ Route 222: the size of a Remoraid, and the Pikachu

    private static Pokemon Sized(string species, uint personality, int hp, int attack, int defense, int speed, int spAttack, int spDefense)
    {
        var p = Mon(species);
        p.Personality = personality;
        (p.IvHP, p.IvAttack, p.IvDefense, p.IvSpeed, p.IvSpAttack, p.IvSpDefense) = (hp, attack, defense, speed, spAttack, spDefense);
        return p;
    }

    [Fact]
    public void APokemonsSizeIsWorkedOutOfItsPersonalityAndItsIvs()
    {
        // CalcSizeFactor: the low four bits of each IV. HP 31 -> 15, Attack 10, Defense 5, Speed 20 -> 4, Sp. Atk 7,
        // Sp. Def 12, and a personality whose low half is 0x1234:
        //   high = ((10 ^ 5) * 15) ^ 0x34 = 225 ^ 52 = 213;  low = ((7 ^ 12) * 4) ^ 0x12 = 44 ^ 18 = 62
        //   factor = 213 * 256 + 62 = 54,590
        var remoraid = Sized("Remoraid", 0xABCD1234, 31, 10, 5, 20, 7, 12);
        Assert.Equal(54590, SizeContest.Factor(remoraid));
        // CalcMultFromSizeFactor: 54,590 lies in the interval from 47,710 (1,100 and a point for every 100 past it)
        Assert.Equal(1168, SizeContest.Multiplier(54590));
        // CalcMillimeterSize: a Remoraid is 0.6 m, six tenths of a metre: 6 * 1168 / 10 = 700 mm
        Assert.Equal(700, SizeContest.Millimetres(remoraid));
        Assert.Equal("70.0 cm", SizeContest.Told(700));

        // The table's ends and its joins: 290 at nought, 299 and 300 either side of the first join, 1,725 at the top
        Assert.Equal(290, SizeContest.Multiplier(0));
        Assert.Equal(299, SizeContest.Multiplier(9));
        Assert.Equal(300, SizeContest.Multiplier(10));
        Assert.Equal(1000, SizeContest.Multiplier(32710));
        Assert.Equal(1725, SizeContest.Multiplier(65535));
        // The original's search never lands on the last interval but one: from 65,410 it reads the last one's line,
        // wrapped, which climbs one for one all the same
        Assert.Equal(1599, SizeContest.Multiplier(65409));
        Assert.Equal(1600, SizeContest.Multiplier(65410));
        Assert.Equal(1699, SizeContest.Multiplier(65509));
        Assert.Equal(1700, SizeContest.Multiplier(65510));
        // It never falls as the factor rises
        long last = 0;
        for (int factor = 0; factor <= 65535; factor++)
        {
            long now = SizeContest.Multiplier(factor);
            Assert.True(now >= last, $"the multiplier falls at {factor}");
            last = now;
        }

        // The record a game begins with (ScrCmd_InitSizeContestRecord's 33,280): 1,003 thousandths, 601 mm of Remoraid
        var story = new StoryState();
        Assert.Equal(SizeContest.FirstRecord, SizeContest.Record(story));
        Assert.Equal(601, SizeContest.Millimetres(0.6f, SizeContest.FirstRecord));
        Assert.Equal(SizeContest.Outcome.Larger, SizeContest.Compare(remoraid, SizeContest.Record(story)));
        story.SetVar(SizeContest.RecordVariable, 54590);
        Assert.Equal(SizeContest.Outcome.Same, SizeContest.Compare(remoraid, SizeContest.Record(story)));
        // (a factor one point up is the same millimetre: what is told is what is compared)
        story.SetVar(SizeContest.RecordVariable, 54591);
        Assert.Equal(SizeContest.Outcome.Same, SizeContest.Compare(remoraid, SizeContest.Record(story)));
        story.SetVar(SizeContest.RecordVariable, 60000);
        Assert.Equal(SizeContest.Outcome.Smaller, SizeContest.Compare(remoraid, SizeContest.Record(story)));
    }

    [Fact]
    public void TheFishermanMeasuresARemoraidAgainstTheBiggestHeHasSeen()
    {
        // With no Remoraid he only tells his dream
        var host = new HeadlessScriptHost();
        host.Party.Add(Mon());
        Run("Fisherman", "Route222EastHouse", host);
        Assert.DoesNotContain("measure", Said(host));
        Assert.Equal(0, Have(host, "Net Ball"));

        // 70.0 cm against the 60.1 cm every game begins with: a Net Ball, and the record is this one's
        var big = Sized("Remoraid", 0xABCD1234, 31, 10, 5, 20, 7, 12);
        host.Party.Add(big);
        host.PokemonChoice = 1;
        Run("Fisherman", "Route222EastHouse", host);
        Assert.Contains("70.0 cm", Said(host));
        Assert.Equal(1, Have(host, "Net Ball"));
        Assert.Equal(54590, host.Story.Var(SizeContest.RecordVariable));

        // The same one again: he remembers the size, and gives nothing
        Run("Fisherman", "Route222EastHouse", host);
        Assert.Equal(1, Have(host, "Net Ball"));
        Assert.Contains("70.0 cm... Have I been here before?", Said(host));

        // A smaller one: he tells both sizes. factor = ((1 ^ 2) * 3) ^ 0x10 = 25 high, 0 ^ 0 low: 6,400, in the interval
        // from 2,710 (700 and a point for every 50): 700 + 73 = 773, 463 mm
        var small = Sized("Remoraid", 0x0010, 3, 1, 2, 0, 0, 0);
        host.Party.Add(small);
        host.PokemonChoice = 2;
        Run("Fisherman", "Route222EastHouse", host);
        Assert.Contains("comes to 46.3 cm", Said(host));
        Assert.Contains("one of 70.0 cm", Said(host));
        Assert.Equal((1, 54590), (Have(host, "Net Ball"), host.Story.Var(SizeContest.RecordVariable)));

        // Another species, an Egg, and nothing shown at all
        host.PokemonChoice = 0;
        Run("Fisherman", "Route222EastHouse", host);
        Assert.Contains("Did you hear a single word", host.Transcript[^1].Text);
        small.IsEgg = true;
        host.PokemonChoice = 2;
        Run("Fisherman", "Route222EastHouse", host);
        Assert.Contains("Did you hear a single word", host.Transcript[^1].Text);
        host.PokemonChoice = 255;
        Run("Fisherman", "Route222EastHouse", host);
        Assert.Contains("keep me from measuring", host.Transcript[^1].Text);
        Assert.Equal((1, 54590), (Have(host, "Net Ball"), host.Story.Var(SizeContest.RecordVariable)));

        // A bigger one still earns another Net Ball: the gift is for every record
        var huge = Sized("Remoraid", 0xFFFF, 0, 0, 0, 0, 0, 0);
        Assert.Equal(65535, SizeContest.Factor(huge));
        host.Party.Add(huge);
        host.PokemonChoice = 3;
        Run("Fisherman", "Route222EastHouse", host);
        Assert.Contains("103.5 cm", Said(host));
        Assert.Equal((2, 65535), (Have(host, "Net Ball"), host.Story.Var(SizeContest.RecordVariable)));

        // Coming in notes the house, as the original's does
        Run("OnEnter", "Route222EastHouse", host);
        Assert.True(host.Story.Has("FLAG_FIRST_ARRIVAL_POKEMON_SIZE_JUDGE"));
    }

    [Fact]
    public void ThePikachuFanClubHidesATrainerAmongItsPikachu()
    {
        var club = Room("Route222WestHouse");
        var pikachu = club.Everyone.Where(n => n.IsPokemon && n.Species == "Pikachu").ToList();
        Assert.Equal(6, pikachu.Count);
        var janet = Assert.Single(pikachu, n => n.IsTrainer);
        Assert.Equal("poke_kid_janet", janet.Key);
        // She looks like the rest and sees nobody: she battles whoever speaks to her, with Platinum's two Pikachu
        Assert.Equal(0, janet.TrainerData!.SightRange);
        Assert.Equal(FieldScripts.Trainer, ScriptLibrary.Default.Find(FieldScripts.For(janet)!, "Route222WestHouse")!.FullName);
        Assert.Equal(new[] { ("Pikachu", 42), ("Pikachu", 42) }, janet.TrainerData.Party.Members.Select(p => (p.Species.Name, p.Level)));
        Assert.Equal("Poké Kid", janet.TrainerData.TrainerClass);
        var host = new HeadlessScriptHost { Map = club };
        host.Party.Add(Mon());
        Run(FieldScripts.Trainer, "Route222WestHouse", host, janet);
        Assert.True(host.Story.HasDefeated("poke_kid_janet"));
        // The other five only cry
        foreach (var one in pikachu.Where(n => n != janet))
            Assert.Equal(FieldScripts.Pokemon, FieldScripts.For(one));
        Run("OnEnter", "Route222WestHouse", host);
        Assert.True(host.Story.Has("FLAG_FIRST_ARRIVAL_PIKACHU_FAN_CLUB"));
    }

    // ------------------------------------------------------------------ Sunyshore City

    [Fact]
    public void TheInventorGivesAnAppForEachNatureHeAsksToSee()
    {
        var host = new HeadlessScriptHost();
        host.Party.Add(Mon());
        // His first talk only introduces him
        Run("Scientist", "SunyshoreCityEastHouse", host);
        Assert.Equal(1, host.Story.Var("VAR_SUNYSHORE_CITY_EAST_HOUSE_SCIENTIST_STATE"));
        Assert.DoesNotContain("Serious", Said(host));
        Run("Scientist", "SunyshoreCityEastHouse", host);
        Assert.Contains("Serious", host.Transcript[^1].Text);
        Assert.Empty(host.Poketch.Apps);

        // A Serious Egg won't do; a Serious Pokémon will
        var egg = Mon("Starly", nature: Nature.Serious);
        egg.IsEgg = true;
        host.Party.Add(egg);
        Run("Scientist", "SunyshoreCityEastHouse", host);
        Assert.Empty(host.Poketch.Apps);
        var serious = Mon("Bidoof", nature: Nature.Serious);
        host.Party.Add(serious);
        Run("Scientist", "SunyshoreCityEastHouse", host);
        Assert.Equal(new[] { PoketchApp.Calendar }, host.Poketch.Apps);
        Assert.Contains(serious.Nickname, Said(host));

        // Then a Naive one, then a Quirky one; a Quirky one shown early earns nothing yet
        host.Party.Add(Mon("Shinx", nature: Nature.Quirky));
        Run("Scientist", "SunyshoreCityEastHouse", host);
        Assert.Contains("Naive", host.Transcript[^1].Text);
        Assert.Single(host.Poketch.Apps);
        host.Party.Add(Mon("Budew", nature: Nature.Naive));
        Run("Scientist", "SunyshoreCityEastHouse", host);
        Assert.Equal(new[] { PoketchApp.Calendar, PoketchApp.DotArt }, host.Poketch.Apps);
        Run("Scientist", "SunyshoreCityEastHouse", host);
        Assert.Equal(new[] { PoketchApp.Calendar, PoketchApp.DotArt, PoketchApp.Roulette }, host.Poketch.Apps);
        int said = host.Transcript.Count;
        Run("Scientist", "SunyshoreCityEastHouse", host);
        Assert.Equal(said + 1, host.Transcript.Count);
        Assert.Equal(3, host.Poketch.Apps.Count);
    }

    [Fact]
    public void SunyshoresOtherHousesHaveTheirPeople()
    {
        // The diggers' house: seven people round the Underground Man's kind of furniture
        var diggers = Room("SunyshoreCityWestHouse");
        Assert.Equal(7, diggers.Everyone.Count());
        Assert.All(diggers.Everyone, n => Assert.True(n.DialogLines.Count > 0, n.Key));
        Assert.Contains(diggers.Everyone, n => n.DialogLines.Any(l => l.Contains("Explorer Kit")));
        Assert.Contains(diggers.Everyone, n => n.DialogLines.Any(l => l.Contains("Roark")));
        Assert.Equal(2, diggers.Props.Count(p => p.Type == PropType.StoreShelf && p.Width == 2));
        // The old couple remember a boy called Cyrus
        var couple = Room("SunyshoreCityNorthwestHouse");
        Assert.Contains(couple.Everyone, n => n.Key == "old_woman" && n.DialogLines.Any(l => l.Contains("Cyrus")));
        Assert.Contains(couple.Everyone, n => n.Key == "old_man" && n.DialogLines.Any(l => l.Contains("Volkner")));
    }

    // ------------------------------------------------------------------ the Battle Zone

    [Fact]
    public void TheItemExpertTellsOfEightItemsAndEveryAnswerEndsTheTalk()
    {
        // The first menu's four, the way on to the second, and the way out
        string[] first = { "Lum Berry", "Focus Sash", "Expert Belt", "Life Orb" }, second = { "Choice Scarf", "Choice Band", "Choice Specs", "Leftovers" };
        for (int answer = 0; answer < 4; answer++)
        {
            var host = new HeadlessScriptHost();
            host.Answers.Enqueue(answer);
            Run("ItemExpert", "FightAreaSouthHouse", host);
            Assert.Contains(first[answer], Said(host));
            Assert.Single(host.Asked);
        }
        for (int answer = 0; answer < 4; answer++)
        {
            var host = new HeadlessScriptHost();
            host.Answers.Enqueue(4);
            host.Answers.Enqueue(answer);
            Run("ItemExpert", "FightAreaSouthHouse", host);
            Assert.Contains(second[answer], Said(host));
            Assert.Equal(2, host.Asked.Count);
        }
        foreach (int[] way in new[] { new[] { 5 }, new[] { 4, 4 } })
        {
            var host = new HeadlessScriptHost();
            foreach (int answer in way) host.Answers.Enqueue(answer);
            Run("ItemExpert", "FightAreaSouthHouse", host);
            Assert.Contains("never know until you try", host.Transcript[^1].Text);
        }
        // Every item he names is one of the game's
        foreach (string item in first.Concat(second)) Assert.NotNull(ItemDatabase.Get(item));
        // The Torchic of the house is a Pokémon of the room
        Assert.Contains(Room("FightAreaSouthHouse").Everyone, n => n.IsPokemon && n.Species == "Torchic");
    }

    [Theory]
    [InlineData("Worker", "Route225House", "Fresh Water", "FLAG_RECEIVED_ROUTE_225_HOUSE_FRESH_WATER")]
    [InlineData("BlackBelt", "SurvivalAreaSouthHouse", "TM42", "FLAG_RECEIVED_SURVIVAL_AREA_SOUTH_HOUSE_TM42")]
    public void AGiftIsGivenOnce(string script, string room, string item, string flag)
    {
        var host = Run(script, room, new HeadlessScriptHost());
        Assert.Equal(1, Have(host, item));
        Assert.True(host.Story.Has(flag));
        int said = host.Transcript.Count;
        Run(script, room, host);
        Assert.Equal(1, Have(host, item));
        Assert.True(host.Transcript.Count > said);
    }

    [Fact]
    public void TheMeisterTradesHisMagikarpForAFinneon()
    {
        var host = new HeadlessScriptHost();
        host.Party.Add(Mon());
        host.Party.Add(Mon("Finneon"));
        // The first talk sets the Pokédex up, once; refusing leaves everything as it was
        host.Answers.Enqueue(1);
        Run("Meister", "Route226House", host);
        Assert.True(host.Story.Has("FLAG_ENABLED_POKEDEX_LANGUAGE_DETECTION"));
        Assert.Contains("Pokédex", Said(host));
        Assert.Contains("tragedy", host.Transcript[^3].Text);
        int said = host.Transcript.Count;
        // A Pokémon that is no Finneon
        host.PokemonChoice = 0;
        Run("Meister", "Route226House", host);
        Assert.DoesNotContain("Pokédex", string.Join(" | ", host.Transcript.Skip(said).Select(t => t.Text)));
        Assert.Contains("no Finneon", Said(host));
        Assert.False(host.Story.Has("FLAG_TRADED_FOR_FOPPA_MAGIKARP"));
        Assert.Equal("Finneon", host.Party.Members[1].Species.Name);
        // The Finneon: his Magikarp comes, with its name, its trainer and its Lum Berry
        host.PokemonChoice = 1;
        Run("Meister", "Route226House", host);
        Assert.True(host.Story.Has("FLAG_TRADED_FOR_FOPPA_MAGIKARP"));
        var foppa = host.Party.Members[1];
        Assert.Equal(("Magikarp", "Foppa", "Meister", "Lum Berry"), (foppa.Species.Name, foppa.Nickname, foppa.OriginalTrainer?.Name, foppa.HeldItem?.Name));
        // And afterwards he asks no more
        said = host.Transcript.Count;
        Run("Meister", "Route226House", host);
        Assert.Empty(host.Asked.Skip(3));
        Assert.Contains("friend", host.Transcript[said].Text);
        // His notice is read on the wall, and coming in notes the house
        Assert.Contains("150 countries", Room("Route226House").Signboards[(4, 2)]);
        Run("OnEnter", "Route226House", host);
        Assert.True(host.Story.Has("FLAG_FIRST_ARRIVAL_THE_MEISTERS_HOUSE"));
    }

    [Fact]
    public void TheOldWomanOfRoute227GivesARest()
    {
        var hurt = Mon();
        hurt.CurrentHP = 1;
        var host = new HeadlessScriptHost();
        host.Party.Add(hurt);
        host.Answers.Enqueue(1);
        Run("Rest", "Route227House", host);
        Assert.Equal(1, hurt.CurrentHP);
        Run("Rest", "Route227House", host);
        Assert.Equal(hurt.MaxHP, hurt.CurrentHP);
        // One rest is one talk: the question is asked once
        Assert.Equal(2, host.Asked.Count);
    }

    [Fact]
    public void TheOldGentlemanSitsAmongPilesOfBooksThatAreRead()
    {
        var house = Room("Route228SouthHouse");
        // The four piles the original reads, each on a tile nobody walks on
        var piles = house.Props.Where(p => p.Type == PropType.BookPile).Select(p => (p.X, p.Y)).Order().ToList();
        Assert.Equal(new[] { (1, 7), (5, 3), (7, 5), (9, 7) }, piles);
        foreach (var (x, y) in piles)
        {
            Assert.True(house.IsSolid(x, y));
            Assert.Equal(TileType.Floor, house.GetGroundTile(x, y));
            Assert.Contains("books", house.Signboards[(x, y)]);
        }
        Assert.Equal(4, piles.Select(p => house.Signboards[p]).Distinct().Count());
        // The same houses of the inventor and the black belt have the piles too, with nothing to read
        foreach (string other in new[] { "SunyshoreCityEastHouse", "SurvivalAreaSouthHouse" })
            Assert.Equal(4, Room(other).Props.Count(p => p.Type == PropType.BookPile));

        // He tells his tale to yes and stops himself at no
        var host = Run("Gentleman", "Route228SouthHouse", new HeadlessScriptHost());
        Assert.Contains("grandson", Said(host));
        var refused = new HeadlessScriptHost();
        refused.Answers.Enqueue(1);
        Run("Gentleman", "Route228SouthHouse", refused);
        Assert.DoesNotContain("grandson", Said(refused));
        Assert.Contains("Sunyshore", Said(refused));
    }

    [Fact]
    public void APileOfBooksIsPaintedWholeAndNoTwoAlike()
    {
        var seen = new HashSet<string>();
        for (int turn = 0; turn < PropModels.BookPileTurns; turn++)
        {
            var c = new PixelCanvas(PropModels.BookPileWidth, PropModels.BookPileHeight);
            PropModels.PaintBookPile(c, turn);
            var key = new System.Text.StringBuilder();
            int painted = 0;
            for (int y = 0; y < c.Height; y++)
                for (int x = 0; x < c.Width; x++)
                {
                    var texel = c.Get(x, y);
                    key.Append(texel.R).Append(',').Append(texel.G).Append(',').Append(texel.B).Append(',').Append(texel.A).Append(';');
                    if (texel.A == 0) continue;
                    painted++;
                    // Nothing see-through and nothing bloomed to white
                    Assert.Equal(255, texel.A);
                    Assert.False(texel.R > 250 && texel.G > 250 && texel.B > 250, $"turn {turn}: white at {x},{y}");
                }
            Assert.True(painted > c.Width * c.Height / 2, $"turn {turn}: only {painted} texels painted");
            // It stands on the floor: its bottom row is painted
            Assert.Contains(Enumerable.Range(0, c.Width), x => c.Get(x, c.Height - 1).A == 255);
            Assert.True(seen.Add(key.ToString()), $"turn {turn} is painted as another is");
        }
        // The piles of one room are laid different ways
        var house = Room("Route228SouthHouse");
        var turns = house.Props.Where(p => p.Type == PropType.BookPile).Select(p => PropModels.BookPileTurn(p.X, p.Y)).ToList();
        Assert.Equal(turns.Count, turns.Distinct().Count());
    }

    [Fact]
    public void TheResortAreasBoyAsksThePokedexFirst()
    {
        var host = new HeadlessScriptHost { SeenInSinnoh = 209 };
        Run("Boy", "ResortAreaHouse", host);
        Assert.DoesNotContain("You've seen every", Said(host));
        host.SeenInSinnoh = 210;
        Run("Boy", "ResortAreaHouse", host);
        Assert.Contains("You've seen every", Said(host));
        // The collector turns back to his television when he has said his piece
        var house = Room("ResortAreaHouse");
        var collector = Assert.Single(house.Everyone, n => n.Key == "collector");
        Assert.Contains(house.Props, p => p.Type == PropType.Television && p.Covers(collector.GridX, collector.GridY - 1));
        collector.Facing = Direction.Down;
        Run("Collector", "ResortAreaHouse", new HeadlessScriptHost { Map = house }, collector);
        Assert.Equal(Direction.Up, collector.Facing);
    }

    // ------------------------------------------------------------------ the Ribbon Syndicate's lift and spa

    private const string LiftFlag = "FLAG_RIBBON_SYNDICATE_LIFT_UPSTAIRS";

    [Fact]
    public void TheSyndicatesLiftLeadsOutOntoTheFloorItStandsAt()
    {
        var ground = Room("ResortAreaRibbonSyndicate1F");
        var spa = Room("ResortAreaRibbonSyndicate2F");
        var car = Room("ResortAreaRibbonSyndicateElevator");
        Assert.Equal(InteriorStyle.Lab, car.Interior);
        Assert.Contains(car.Props, p => p.Type == PropType.LiftIndicator);

        // The lift's doors in each floor's back wall lead into the car, onto its mat
        foreach (var floor in new[] { ground, spa })
        {
            var doors = floor.GetWarpAt(5, 2)!;
            Assert.Equal(("ResortAreaRibbonSyndicateElevator", 3, 6, Direction.Up), (doors.TargetMap, doors.TargetX, doors.TargetY, doors.TargetFacing));
            Assert.Equal(TileType.Wall, floor.GetGroundTile(5, 2));
            Assert.Equal(TileBehavior.Door, floor.BehaviourAt(5, 2));
            Assert.False(floor.IsSolid(5, 2));
            Assert.True(floor.IsWalkable(5, 3));
            Assert.DoesNotContain((5, 2), floor.Signboards.Keys);
        }
        Assert.True(car.IsWalkable(3, 6));

        // The car's door is two ways on one tile: to the ground floor until the lift has gone up, to the spa from then
        var ways = car.WarpsAt(3, 7).ToList();
        Assert.Equal(2, ways.Count);
        Assert.Contains(ways, w => w.TargetMap == "ResortAreaRibbonSyndicate1F" && w.Until == LiftFlag && w.From == null);
        Assert.Contains(ways, w => w.TargetMap == "ResortAreaRibbonSyndicate2F" && w.From == LiftFlag && w.Until == null);
        Assert.All(ways, w => Assert.Equal((5, 3, Direction.Down), (w.TargetX, w.TargetY, w.TargetFacing)));
        car.ApplyDoors(_ => false);
        Assert.Equal("ResortAreaRibbonSyndicate1F", car.GetWarpAt(3, 7)!.TargetMap);
        Assert.False(car.IsSolid(3, 7));
        car.ApplyDoors(flag => flag == LiftFlag);
        Assert.Equal("ResortAreaRibbonSyndicate2F", car.GetWarpAt(3, 7)!.TargetMap);
        Assert.False(car.IsSolid(3, 7));

        // A map file keeps the two flags when it is written back out
        var written = MapFile.FromMap(car).ToMap();
        Assert.Equal(2, written.WarpsAt(3, 7).Count());
        Assert.Contains(written.Warps, w => w.Until == LiftFlag);
        Assert.Contains(written.Warps, w => w.From == LiftFlag);
    }

    [Fact]
    public void TheLiftsAttendantTakesTheCarUpAndDown()
    {
        var car = Room("ResortAreaRibbonSyndicateElevator");
        var attendant = Assert.Single(car.Everyone);
        Assert.Equal(((1, 6), Direction.Right, "Lift"), ((attendant.GridX, attendant.GridY), attendant.Facing, attendant.Script));
        var host = new HeadlessScriptHost { Map = car };

        // On the ground floor: 1F is where the car is already; 2F rides up; EXIT says nothing more
        host.Answers.Enqueue(1);
        Run("Lift", "ResortAreaRibbonSyndicateElevator", host, attendant);
        Assert.False(host.Story.Has(LiftFlag));
        Assert.Contains("that floor already", host.Transcript[^1].Text);
        host.Answers.Enqueue(2);
        int said = host.Transcript.Count;
        Run("Lift", "ResortAreaRibbonSyndicateElevator", host, attendant);
        Assert.Equal(said + 1, host.Transcript.Count);
        host.Answers.Enqueue(0);
        Run("Lift", "ResortAreaRibbonSyndicateElevator", host, attendant);
        Assert.True(host.Story.Has(LiftFlag));
        Assert.Contains("Going up", Said(host));
        Assert.Contains("Here we are", host.Transcript[^1].Text);
        Assert.Equal(Direction.Right, attendant.Facing);
        // Upstairs: 2F is where it is; 1F rides down
        host.Answers.Enqueue(0);
        Run("Lift", "ResortAreaRibbonSyndicateElevator", host, attendant);
        Assert.True(host.Story.Has(LiftFlag));
        Assert.Contains("that floor already", host.Transcript[^1].Text);
        host.Answers.Enqueue(1);
        Run("Lift", "ResortAreaRibbonSyndicateElevator", host, attendant);
        Assert.False(host.Story.Has(LiftFlag));
        Assert.Contains("Going down", Said(host));

        // Each floor says where the lift stands as it is come to
        var floors = new HeadlessScriptHost();
        Run("OnEnter", "ResortAreaRibbonSyndicate2F", floors);
        Assert.True(floors.Story.Has(LiftFlag));
        floors.Story.Set("FLAG_JOINED_RIBBON_SYNDICATE");
        floors.Map = Room("ResortAreaRibbonSyndicate1F");
        Run("OnEnter", "ResortAreaRibbonSyndicate1F", floors);
        Assert.False(floors.Story.Has(LiftFlag));
    }

    [Theory]
    [InlineData(12, 6)]
    [InlineData(11, 5)]
    [InlineData(13, 5)]
    [InlineData(12, 4)]
    public void TheSpaSeatsItsGuestAndTheAttendantsCloseIn(int fromX, int fromY)
    {
        var spa = Room("ResortAreaRibbonSyndicate2F");
        var attendant = Assert.Single(spa.Everyone, n => n.Key == "receptionist_3");
        Assert.Equal((12, 5), (attendant.GridX, attendant.GridY));
        var lead = Mon();
        lead.Friendship = 70;
        var host = new HeadlessScriptHost { Map = spa, PlayerTile = (fromX, fromY) };
        host.Party.Add(lead);
        host.Rolls.Enqueue(0);
        host.Rolls.Enqueue(99);
        Run("Spa", "ResortAreaRibbonSyndicate2F", host, attendant);

        // The guest is in the chair of the west bay, an attendant on every side but the one who stepped out of the way
        Assert.Equal((16, 7), host.PlayerTile);
        Assert.Contains(spa.Props, p => p.Type == PropType.Cushion && p.Covers(16, 7));
        NPC Who(string key) => Assert.Single(spa.Everyone, n => n.Key == key);
        Assert.Equal((17, 7, Direction.Left), (attendant.GridX, attendant.GridY, attendant.Facing));
        Assert.Equal((16, 6), (Who("receptionist_1").GridX, Who("receptionist_1").GridY));
        Assert.Equal((15, 6, Direction.Down), (Who("receptionist_2").GridX, Who("receptionist_2").GridY, Who("receptionist_2").Facing));
        Assert.Equal((16, 8), (Who("receptionist_4").GridX, Who("receptionist_4").GridY));
        Assert.True(spa.IsWalkable(15, 7));
        // By the die of a hundred: 99 is the least of the three
        Assert.Equal(75, lead.Friendship);
        Assert.True(host.Story.Has("FLAG_DAILY_GOT_SPA_TREATMENT_RESORT_AREA_RIBBON_SYNDICATE_2F"));
        Assert.Contains("diamonds", Said(host));
        Assert.Contains(lead.Nickname, Said(host));
    }

    [Fact]
    public void TheSpaTreatsTheFirstOfTheTeamOnceADay()
    {
        HeadlessScriptHost Visit(HeadlessScriptHost host, int friendshipRoll, int answer = 0)
        {
            var spa = Room("ResortAreaRibbonSyndicate2F");
            host.Map = spa;
            host.PlayerTile = (12, 6);
            host.Answers.Enqueue(answer);
            host.Rolls.Enqueue(3);
            host.Rolls.Enqueue(friendshipRoll);
            return Run("Spa", "ResortAreaRibbonSyndicate2F", host, spa.Everyone.Single(n => n.Key == "receptionist_3"));
        }

        // An Egg at the head of the team is passed by: the treatment is the first hatched Pokémon's
        var egg = Mon("Starly");
        egg.IsEgg = true;
        var lead = Mon();
        lead.Friendship = 100;
        var host = new HeadlessScriptHost();
        host.Party.Add(egg);
        host.Party.Add(lead);
        int cycles = egg.Friendship;

        // Refused: nothing happens, and the offer stands
        Visit(host, 0, answer: 1);
        Assert.Equal(100, lead.Friendship);
        Assert.False(host.Story.Has("FLAG_DAILY_GOT_SPA_TREATMENT_RESORT_AREA_RIBBON_SYNDICATE_2F"));
        host.Answers.Clear();
        host.Rolls.Clear();

        // The die of a hundred: under 5 is 30, under 30 is 10, the rest 5
        Visit(host, 4);
        Assert.Equal(130, lead.Friendship);
        Assert.Equal(cycles, egg.Friendship);
        Assert.Contains("very much fonder", host.Transcript[^1].Text);
        // Once a day
        int said = host.Transcript.Count;
        Visit(host, 4);
        Assert.Equal(130, lead.Friendship);
        Assert.Equal(said + 2, host.Transcript.Count);
        host.Answers.Clear();
        host.Rolls.Clear();
        host.Story.ClearDaily();
        Visit(host, 5);
        Assert.Equal(140, lead.Friendship);
        host.Story.ClearDaily();
        Visit(host, 29);
        Assert.Equal(150, lead.Friendship);
        host.Story.ClearDaily();
        Visit(host, 30);
        Assert.Equal(155, lead.Friendship);
        // A Soothe Bell makes it half as much again, and friendship stops at 255
        host.Story.ClearDaily();
        lead.HeldItem = ItemDatabase.Get("Soothe Bell");
        Visit(host, 0);
        Assert.Equal(200, lead.Friendship);
        host.Story.ClearDaily();
        Visit(host, 0);
        host.Story.ClearDaily();
        Visit(host, 0);
        Assert.Equal(255, lead.Friendship);
    }

    // ------------------------------------------------------------------ what the team is asked

    [Fact]
    public void TheTeamIsAskedForItsFirstThatIsNoEggAndForANature()
    {
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        int Ask(string line)
        {
            runner.Start(ScriptParser.Parse("test", $"script Ask\n  {line}\n  end\n")[0], null);
            runner.RunToEnd();
            return runner.Result;
        }

        Assert.Equal(255, Ask("team first"));
        Assert.Equal(255, Ask("team nature serious"));
        var egg = Mon("Starly", nature: Nature.Quirky);
        egg.IsEgg = true;
        host.Party.Add(egg);
        Assert.Equal(255, Ask("team first"));
        Assert.Equal(255, Ask("team nature Quirky"));
        host.Party.Add(Mon(nature: Nature.Naive));
        host.Party.Add(Mon("Bidoof", nature: Nature.Quirky));
        host.Party.Add(Mon("Shinx", nature: Nature.Quirky));
        Assert.Equal(1, Ask("team first"));
        Assert.Equal(2, Ask("team nature quirky"));
        Assert.Equal(1, Ask("team nature Naive"));
        Assert.Equal(255, Ask("team nature Serious"));

        // What is no nature, and a species that isn't one, are told as the file is read
        Assert.Throws<ScriptException>(() => ScriptParser.Parse("test", "script Ask\n  team nature sleepy\n  end\n"));
        var wrong = ScriptLibrary.FromSources(("test", "script Ask\n  team size \"Remorade\"\n  team size keep\n  end\n"));
        Assert.Contains(wrong.Problems(), p => p.Contains("Remorade"));
    }

    [Fact]
    public void NothingSaysTheSyndicatesLiftIsShutAnyMore()
    {
        Assert.DoesNotContain(Room("ResortAreaRibbonSyndicate1F").Signboards.Values, text => text.Contains("shut", StringComparison.OrdinalIgnoreCase));
    }
}
