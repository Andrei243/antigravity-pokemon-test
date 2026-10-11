using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.SuperContests;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// The rooms of the east and the sea as plan 01 · M11, part b built them, on the original's plans: Café Cabin, the
/// houses of Veilstone City, Pastoria City and Canalave City, the Seven Stars Restaurant, the Hotel Grand Lake's lobby
/// and cottages, the Great Marsh's observatory with the Safari Game's counter, the Pokémon Mansion, the house on
/// Route 221 and Iron Island's cabin. Where their people stand is <see cref="RoomPlanTests"/>'s.
/// </summary>
public class EastInteriorTests
{
    // Maps of this class's own, so nothing another test does to the database's maps shows here
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Sinnoh"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));

    private static World Sinnoh => LoadedWorld.Value;

    private static Map Room(string name) => GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    private static int Have(HeadlessScriptHost host, string item) => host.Bag.GetQuantity(ItemDatabase.Get(item)!);

    private static Pokemon Mon(string species = "Turtwig", int level = 20, Gender gender = Gender.Male) =>
        new(PokemonDatabase.Get(species)!, level, gender, Nature.Hardy, false);

    private static HeadlessScriptHost Run(string script, string place, HeadlessScriptHost host, NPC? who = null)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, place) ?? throw new InvalidOperationException($"no script {place}.{script}"), who);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    /// <summary>
    /// Open ground, whoever a flag will take away may stand on it meanwhile: the beauty who has lost her Suite Key stands
    /// before her cottage's door at Valor Lakefront until it is brought back to her.
    /// </summary>
    private static bool Open(Map map, int x, int y) =>
        map.IsWalkable(x, y) || (!map.IsSolid(x, y) && map.NPCs.Where(n => n.GridX == x && n.GridY == y).All(n => n.HiddenBy != null));

    private static string Said(HeadlessScriptHost host) => string.Join(" | ", host.Transcript.Select(t => t.Text));

    /// <summary>Each door part b opened: the area, its warp's number, the room, and the mat inside it.</summary>
    public static IEnumerable<object[]> Doors => new[]
    {
        new object[] { "route_210_south", 0, "CafeCabin", 5, 8 },
        new object[] { "veilstone_city", 6, "VeilstoneCitySouthwestHouse", 4, 8 },
        new object[] { "veilstone_city", 8, "VeilstoneCityNorthwestHouse", 4, 8 },
        new object[] { "veilstone_city", 9, "VeilstoneCityNortheastHouse", 4, 8 },
        new object[] { "valor_lakefront", 0, "SevenStarsRestaurant", 9, 12 },
        new object[] { "valor_lakefront", 1, "GrandLakeValorLakefrontEastHouse", 6, 8 },
        new object[] { "valor_lakefront", 2, "GrandLakeValorLakefrontWestHouse", 6, 8 },
        new object[] { "route_213", 3, "GrandLakeRoute213Lobby", 8, 11 },
        new object[] { "route_213", 5, "GrandLakeRoute213EastHouse", 6, 8 },
        new object[] { "route_213", 6, "GrandLakeRoute213NorthwestHouse", 6, 8 },
        new object[] { "route_213", 7, "GrandLakeRoute213NortheastHouse", 6, 8 },
        new object[] { "pastoria_city", 1, "PastoriaCityMiddleHouse", 4, 8 },
        new object[] { "pastoria_city", 3, "PastoriaCityNorthHouse", 4, 8 },
        new object[] { "pastoria_city", 5, "PastoriaCityNortheastHouse", 4, 8 },
        new object[] { "pastoria_city", 6, "PastoriaCitySouthwestHouse", 4, 8 },
        new object[] { "pastoria_city", 7, "PastoriaCityObservatoryGate1F", 5, 11 },
        new object[] { "route_212_north", 2, "PokemonMansion", 33, 16 },
        new object[] { "route_221", 0, "Route221House", 4, 8 },
        new object[] { "canalave_city", 4, "CanalaveCitySoutheastHouse", 4, 8 },
        new object[] { "canalave_city", 6, "CanalaveCitySailorEldritchHouse", 4, 8 },
        new object[] { "canalave_city", 10, "CanalaveCityWestHouse", 4, 8 },
        new object[] { "iron_island", 0, "IronIslandHouse", 4, 8 },
    };

    /// <summary>The rooms of part b that no door of the map of Sinnoh leads into by its front.</summary>
    private static readonly string[] Inner =
    {
        "PastoriaCityObservatoryGate2F", "PokemonMansionMaidsRoomWest", "PokemonMansionMaidsRoomMiddle", "PokemonMansionMaidsRoomEast", "PokemonMansionOffice"
    };

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
        Assert.True(Open(outside, way.X, way.Z + 1), $"{room}: nobody can stand before its door");
        // As its place's rooms have it: the place's own theme
        Assert.Equal(outside.BgmTrackAt(way.X, way.Z + 1), inside.BgmTrack);
    }

    [Fact]
    public void WhatIsStillShutInTheseAreasIsSomeoneElses()
    {
        // Veilstone's Department Store, the Move Relearner's house in Pastoria, the Move Deleter's in Canalave and the
        // two tutors' on Routes 210 and 212 are part d's; Canalave's Harbor Inn waits for the night Darkrai calls
        // (plan 02 · S15b); the Iron Ruins are the story's; Lake Valor's two are warps the original leaves on its
        // shore that no way leads to
        Assert.Equal(new[] { 3 }, Sinnoh.Overlay("veilstone_city")!.Locked);
        Assert.Equal(new[] { 4 }, Sinnoh.Overlay("pastoria_city")!.Locked);
        Assert.Equal(new[] { 5, 9 }, Sinnoh.Overlay("canalave_city")!.Locked);
        Assert.Equal(new[] { 0 }, Sinnoh.Overlay("route_210_north")!.Locked);
        Assert.Equal(new[] { 0 }, Sinnoh.Overlay("route_212_south")!.Locked);
        Assert.Equal(new[] { 2, 3 }, Sinnoh.Overlay("iron_island_b3f")!.Locked);
        Assert.Equal(new[] { 0, 2 }, Sinnoh.Overlay("lake_valor")!.Locked);
        foreach (string area in new[] { "route_210_south", "valor_lakefront", "route_213", "route_212_north", "route_221", "iron_island", "great_marsh_6", "trophy_garden" })
            Assert.True(Sinnoh.Overlay(area)!.Locked is not { Count: > 0 }, $"{area} still has a door shut");
        // The rooms left shut are not built by this part either
        foreach (string left in new[] { "PastoriaCityEastHouse", "CanalaveCityEastHouse", "CanalaveCityHarborInn", "Route210GrandmaWilmaHouse", "Route212House" })
            Assert.False(File.Exists(GameDataFiles.PathOf(Path.Combine(MapDatabase.Folder, left + ".json"))), left);
        // The gate houses are walked through until part d, as before
        Assert.Equal(new[] { 0, 1, 11, 12 }, Sinnoh.Overlay("veilstone_city")!.Through!.Select(t => t.Warp).Order());
        Assert.Equal(new[] { 10, 11 }, Sinnoh.Overlay("pastoria_city")!.Through!.Select(t => t.Warp).Order());
        Assert.Equal(new[] { 0, 1 }, Sinnoh.Overlay("route_213")!.Through!.Select(t => t.Warp).Order());
        Assert.Equal(new[] { 0, 1 }, Sinnoh.Overlay("route_212_north")!.Through!.Select(t => t.Warp).Order());
    }

    [Fact]
    public void EveryoneStandsOnFloorWithSomethingToSayAndEveryWayLeadsSomewhereOneCanStand()
    {
        var rooms = AllRooms.ToDictionary(n => n, Room);
        Assert.True(rooms.Count >= 27, $"only {rooms.Count} rooms");
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
                // Nobody stands where another does (but the Pokémon Mansion's maids and prodigies, who come out one at a
                // time onto the mat the first maid has stepped off), nor on a way out
                if (who.HiddenBy is not ("FLAG_HIDE_POKEMON_MANSION_FIVE_MAID_KNOCKOUT_MAID" or "FLAG_HIDE_POKEMON_MANSION_FIVE_MAID_KNOCKOUT_BOSS"))
                    Assert.Single(map.Everyone, other => (other.GridX, other.GridY) == (who.GridX, who.GridY) && other.HiddenBy == who.HiddenBy);
                Assert.Null(map.GetWarpAt(who.GridX, who.GridY));
            }
            foreach (var (tile, script) in map.SignScripts)
                Assert.True(ScriptLibrary.Default.Find(script, name) != null, $"{name}: what is read at {tile} runs '{script}', which is nowhere");
            foreach (var trigger in map.Triggers)
                Assert.True(ScriptLibrary.Default.Find(trigger.Script, name) != null, $"{name}: the trigger at {trigger.X},{trigger.Y} runs '{trigger.Script}', which is nowhere");
            foreach (var warp in map.Warps)
            {
                var target = rooms.GetValueOrDefault(warp.TargetMap) ?? BuiltMaps.Value[warp.TargetMap];
                Assert.True(Open(target, warp.TargetX, warp.TargetY), $"{name}: the way at {warp.SourceX},{warp.SourceY} leads into something solid of {warp.TargetMap}");
                Assert.False(map.IsSolid(warp.SourceX, warp.SourceY), $"{name}: the way at {warp.SourceX},{warp.SourceY} is walled up");
            }
        }
        // Every script of these rooms is free of what the library checks beyond grammar
        Assert.DoesNotContain(ScriptLibrary.Default.Problems().ToList(), p => AllRooms.Any(r => p.Contains(r + ".txt", StringComparison.Ordinal)));
    }

    // ------------------------------------------------------------------ ways through back walls, stairs and inner doors

    [Fact]
    public void TheHotelsLobbyTheObservatoryAndTheMansionHaveAWayOutAtTheBack()
    {
        // The lobby: the door in its back wall leads up to the terrace where the cottages stand, and back
        var overworld = BuiltMaps.Value["Sinnoh"];
        var lobby = Room("GrandLakeRoute213Lobby");
        var terrace = Sinnoh.Area("route_213")!.Warps[4];
        var up = lobby.GetWarpAt(8, 2)!;
        Assert.Equal(("Sinnoh", terrace.X, terrace.Z - 1, Direction.Up), (up.TargetMap, up.TargetX, up.TargetY, up.TargetFacing));
        Assert.True(overworld.IsWalkable(terrace.X, terrace.Z - 1));
        var down = overworld.GetWarpAt(terrace.X, terrace.Z)!;
        Assert.Equal(("GrandLakeRoute213Lobby", 8, 3, Direction.Down), (down.TargetMap, down.TargetX, down.TargetY, down.TargetFacing));
        Assert.Equal(TileType.Wall, lobby.GetGroundTile(8, 2));
        Assert.False(lobby.IsSolid(8, 2));

        // The observatory: its back door is the Great Marsh's (EastAndSeaTests has the counter)
        var gate = Room("PastoriaCityObservatoryGate1F");
        Assert.Equal("GreatMarsh", gate.GetWarpAt(5, 2)!.TargetMap);

        // The mansion: the Trophy Garden lies behind its hall
        var garden = BuiltMaps.Value["TrophyGarden"];
        var hall = Room("PokemonMansion");
        var back = hall.GetWarpAt(33, 2)!;
        Assert.Equal(("TrophyGarden", Direction.Up), (back.TargetMap, back.TargetFacing));
        Assert.True(garden.IsWalkable(back.TargetX, back.TargetY));
        var gardenWays = garden.Warps.Where(w => w.TargetMap == "PokemonMansion").ToList();
        Assert.Equal(2, gardenWays.Count);
        Assert.All(gardenWays, w =>
        {
            Assert.Equal((33, 3, Direction.Down), (w.TargetX, w.TargetY, w.TargetFacing));
            Assert.Equal(w.SourceY - 1, back.TargetY);
        });
        Assert.DoesNotContain(garden.Warps, w => w.TargetMap == "Sinnoh");
    }

    [Fact]
    public void TheObservatorysFloorsAreJoinedByTheirStairs()
    {
        Map below = Room("PastoriaCityObservatoryGate1F"), above = Room("PastoriaCityObservatoryGate2F");
        var up = below.GetWarpAt(10, 8)!;
        Assert.Equal(("PastoriaCityObservatoryGate2F", 6, 8, Direction.Left), (up.TargetMap, up.TargetX, up.TargetY, up.TargetFacing));
        var down = above.GetWarpAt(7, 8)!;
        Assert.Equal(("PastoriaCityObservatoryGate1F", 11, 8, Direction.Right), (down.TargetMap, down.TargetX, down.TargetY, down.TargetFacing));
        Assert.True(above.IsWalkable(6, 8));
        Assert.True(below.IsWalkable(11, 8));
        Assert.Contains(below.Props, p => p.Type == PropType.SideStairsUp && p.Covers(9, 8));
        Assert.Contains(above.Props, p => p.Type == PropType.SideStairsDown && p.Covers(8, 8));
        // Three pairs of binoculars under the windows, each read from the tile before it
        Assert.Equal(3, above.Props.Count(p => p.Type == PropType.Binoculars));
        foreach (int x in new[] { 2, 6, 10 })
        {
            Assert.Contains(above.Props, p => p.Type == PropType.Binoculars && p.Covers(x, 3));
            Assert.Equal("Binoculars", above.SignScripts[(x, 3)]);
            Assert.True(above.IsWalkable(x, 4));
        }
    }

    [Fact]
    public void TheMansionsCorridorsHaveDoorwaysInTheirOwnBackWall()
    {
        var hall = Room("PokemonMansion");
        var office = Room("PokemonMansionOffice");
        // The hall is a cross: the corridor runs the width of the house, three tiles deep, and the hall proper reaches
        // from the Trophy Garden's door to the front door
        Assert.Equal((1, 3), hall.RoomCorner());
        Assert.True(hall.IsWalkable(2, 7) && hall.IsWalkable(55, 7) && hall.IsWalkable(33, 4) && hall.IsWalkable(33, 15));
        Assert.Equal(TileType.Wall, hall.GetGroundTile(10, 4));
        Assert.True(hall.IsSolid(10, 12) && !hall.IsWalkable(10, 12));

        // Three doors to the maids' rooms and one to the office, each a wall tile one walks into, in a wall that has come
        // forward from the hall's back wall and stands its full height, the door's tile with it
        foreach (var (x, room, inX) in new[] { (5, "PokemonMansionMaidsRoomWest", 4), (13, "PokemonMansionMaidsRoomMiddle", 4), (21, "PokemonMansionMaidsRoomEast", 4), (48, "PokemonMansionOffice", 7) })
        {
            var door = hall.GetWarpAt(x, 5);
            Assert.NotNull(door);
            Assert.Equal(room, door!.TargetMap);
            Assert.Equal((inX, Direction.Up), (door.TargetX, door.TargetFacing));
            Assert.Equal(TileType.Wall, hall.GetGroundTile(x, 5));
            Assert.False(hall.IsSolid(x, 5));
            Assert.True(hall.IsWalkable(x, 6));
            Assert.True(MapScene.BackWallJog(hall, x, 5), $"the door at {x},5 is in no wall of full height");
            Assert.True(MapScene.BackWallJog(hall, x - 1, 5) && MapScene.BackWallJog(hall, x + 1, 5));
            // and the room behind it leads back out to the tile before the door
            var inside = room == "PokemonMansionOffice" ? office : Room(room);
            var exit = inside.GetWarpAt(door.TargetX, door.TargetY + 1)!;
            Assert.Equal(("PokemonMansion", x, 6, Direction.Down), (exit.TargetMap, exit.TargetX, exit.TargetY, exit.TargetFacing));
        }
        // The wall south of the corridor has floor behind it: it is seen over, as every inner wall is
        Assert.False(MapScene.BackWallJog(hall, 10, 9));
        // Beyond that wall the house has nothing (style guide, "Rooms": a house whose plan is no rectangle): void that
        // nobody walks, never within a tile of the floor, never behind it, and none of it left of the floor's corner
        var nothing = (from y in Enumerable.Range(0, hall.Height) from x in Enumerable.Range(0, hall.Width)
                       where hall.GetGroundTile(x, y) == TileType.Void select (x, y)).ToList();
        Assert.True(nothing.Count >= 200, $"only {nothing.Count} tiles of nothing round the mansion's hall");
        Assert.Equal((1, 3), hall.RoomCorner());
        Assert.All(nothing, at =>
        {
            Assert.True(hall.IsSolid(at.x, at.y));
            Assert.InRange(at.x, 1, hall.Width - 2);
            Assert.InRange(at.y, 10, hall.Height - 2);
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    Assert.True(hall.IsSolid(at.x + dx, at.y + dy), $"the nothing at {at.x},{at.y} is beside floor");
        });
        Assert.Equal(TileType.Wall, hall.GetGroundTile(10, 9));
        Assert.Equal(TileType.Void, hall.GetGroundTile(10, 10));
        // A doorway of part a's is as it was: a way on the row behind the floor needs no wall come forward
        Assert.False(MapScene.BackWallJog(Room("OldChateau"), 9, 2));

        // The maids' rooms are three, which the original keeps side by side in one map: a map to each here, each with
        // its one door and its bed to rest in
        foreach (string name in new[] { "PokemonMansionMaidsRoomWest", "PokemonMansionMaidsRoomMiddle", "PokemonMansionMaidsRoomEast" })
        {
            var maids = Room(name);
            Assert.Equal((9, 11), (maids.Width, maids.Height));
            Assert.Single(maids.Warps);
            Assert.Single(maids.Props, p => p.Type == PropType.Bed);
            Assert.All(new[] { (6, 4), (6, 5) }, at => Assert.Equal("Bed", maids.SignScripts[at]));
            Assert.NotNull(ScriptLibrary.Default.Find("Bed", name));
        }
        Assert.Contains(Room("PokemonMansionMaidsRoomWest").Everyone, n => n.IsItemBall && n.Item == "Luxury Ball");
        Assert.Contains(Room("PokemonMansionMaidsRoomEast").HiddenItems, h => h.Value.Item == "Protein");
        Assert.Contains(office.Everyone, n => n.IsItemBall && n.Item == "TM87");
    }

    // ------------------------------------------------------------------ what their people give

    /// <summary>A room, its giver's script, the flag that says it was given, and the gift.</summary>
    public static IEnumerable<object[]> Gifts => new[]
    {
        new object[] { "GrandLakeValorLakefrontEastHouse", "WhiteFlute", "FLAG_RECEIVED_GRAND_LAKE_VALOR_LAKEFRONT_EAST_HOUSE_WHITE_FLUTE", "White Flute" },
        new object[] { "GrandLakeRoute213NorthwestHouse", "TrickRoom", "FLAG_RECEIVED_GRAND_LAKE_ROUTE_213_NORTHWEST_HOUSE_TM92", "TM92" },
        new object[] { "PokemonMansionMaidsRoomMiddle", "SootheBell", "FLAG_RECEIVED_POKEMON_MANSION_MAIDS_ROOM_SOOTHE_BELL", "Soothe Bell" },
        new object[] { "CanalaveCitySoutheastHouse", "SkillSwap", "FLAG_RECEIVED_CANALAVE_CITY_SOUTHEAST_HOUSE_TM48", "TM48" },
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
        Assert.Contains(Room(room).Everyone, n => n.Script == script);
    }

    [Fact]
    public void ThePorygonIsGivenOnceAndOnlyToATeamWithRoom()
    {
        var host = new HeadlessScriptHost();
        for (int i = 0; i < 6; i++) host.Party.Add(Mon());
        Run("Porygon", "VeilstoneCityNortheastHouse", host);
        Assert.True(host.Story.Has("FLAG_TALKED_TO_VEILSTONE_CITY_NORTHEAST_HOUSE_POKEFAN_M"));
        Assert.False(host.Story.Has("FLAG_RECEIVED_VEILSTONE_CITY_NORTHEAST_HOUSE_PORYGON"));

        host.Party.Members.RemoveAt(5);
        host.Answers.Enqueue(1);
        Run("Porygon", "VeilstoneCityNortheastHouse", host);
        Assert.Equal(5, host.Party.Count);
        Run("Porygon", "VeilstoneCityNortheastHouse", host);
        var porygon = Assert.Single(host.Party.Members, p => p.Species.Name == "Porygon");
        Assert.Equal(25, porygon.Level);
        Assert.True(host.Story.Has("FLAG_RECEIVED_VEILSTONE_CITY_NORTHEAST_HOUSE_PORYGON"));
        Run("Porygon", "VeilstoneCityNortheastHouse", host);
        Assert.Single(host.Party.Members, p => p.Species.Name == "Porygon");
    }

    [Fact]
    public void TheCafeSellsMoomooMilkByTheBottleAndByTheDozen()
    {
        var host = new HeadlessScriptHost { Money = 7000 };
        host.Answers.Enqueue(0);
        Run("Milk", "CafeCabin", host);
        Assert.Equal((1, 6500), (Have(host, "Moomoo Milk"), host.Money));
        host.Answers.Enqueue(1);
        Run("Milk", "CafeCabin", host);
        Assert.Equal((13, 500), (Have(host, "Moomoo Milk"), host.Money));
        // Short of a dozen's price nothing is sold; and "no, thanks" costs nothing
        host.Answers.Enqueue(1);
        Run("Milk", "CafeCabin", host);
        Assert.Equal((13, 500), (Have(host, "Moomoo Milk"), host.Money));
        host.Answers.Enqueue(2);
        Run("Milk", "CafeCabin", host);
        Assert.Equal((13, 500), (Have(host, "Moomoo Milk"), host.Money));

        // Its three who battle are trainers of Platinum's data, who battle only when spoken to; the old man knows of the Psyduck
        var cafe = Room("CafeCabin");
        foreach (string id in new[] { "collector_fernando", "collector_edwin", "waitress_kati" })
        {
            var trainer = Assert.Single(cafe.Everyone, n => n.Key == id);
            Assert.True(trainer.IsTrainer);
            Assert.Equal(id, trainer.TrainerData!.Id);
            Assert.Equal(0, trainer.TrainerData.SightRange);
            Assert.NotEmpty(trainer.TrainerData.Party.Members);
            Assert.NotEmpty(trainer.TrainerData.DialogueBefore);
        }
        var before = Run("Psyduck", "CafeCabin", new HeadlessScriptHost());
        var cured = new HeadlessScriptHost();
        cured.Story.Set("FLAG_USED_SECRETPOTION");
        Assert.NotEqual(Said(before), Said(Run("Psyduck", "CafeCabin", cured)));
    }

    // ------------------------------------------------------------------ the script command `team`

    [Fact]
    public void TheMassageGirlMakesOnePokemonFriendlierOnceADayAndFindsAnAccessory()
    {
        var host = new HeadlessScriptHost();
        var first = Mon();
        var second = Mon("Starly");
        (first.Friendship, second.Friendship) = (70, 100);
        host.Party.Add(first);
        host.Party.Add(second);
        host.PokemonChoice = 1;
        host.Rolls.Enqueue(15);
        Run("Massage", "VeilstoneCitySouthwestHouse", host);
        Assert.Equal((70, 103), (first.Friendship, second.Friendship));
        Assert.True(host.Story.Has("FLAG_DAILY_GOT_MASSAGE_VEILSTONE_CITY_SOUTHWEST_HOUSE"));
        // The sixteenth of her accessories, by the die: Glitter Powder
        Assert.Equal(1, host.FashionCase.Count(SuperContestDatabase.Accessory("Glitter Powder")));
        Assert.Contains(host.Transcript, t => t.Text.Contains(second.Nickname) && t.Text.Contains("content"));

        // Nothing more today
        Run("Massage", "VeilstoneCitySouthwestHouse", host);
        Assert.Equal(103, second.Friendship);
        Assert.Equal(1, ScriptRunner.MassageAccessories.Sum(a => host.FashionCase.Count(SuperContestDatabase.Accessory(a))));

        // A new day: a Soothe Bell makes it half as much again, and friendship stops at 255
        host.Story.ClearDaily();
        second.HeldItem = ItemDatabase.Get("Soothe Bell");
        Run("Massage", "VeilstoneCitySouthwestHouse", host);
        Assert.Equal(107, second.Friendship);
        host.Story.ClearDaily();
        second.Friendship = 254;
        Run("Massage", "VeilstoneCitySouthwestHouse", host);
        Assert.Equal(255, second.Friendship);

        // Turned down, or with nobody chosen, or shown an Egg: nothing
        foreach (var refuse in new Action<HeadlessScriptHost>[] { h => h.Answers.Enqueue(1), h => h.PokemonChoice = 255 })
        {
            var shy = new HeadlessScriptHost();
            shy.Party.Add(Mon());
            refuse(shy);
            Run("Massage", "VeilstoneCitySouthwestHouse", shy);
            Assert.False(shy.Story.Has("FLAG_DAILY_GOT_MASSAGE_VEILSTONE_CITY_SOUTHWEST_HOUSE"));
            Assert.Equal(70, shy.Party.Members[0].Friendship);
        }
    }

    [Fact]
    public void TheMassageGirlsAccessoryIsDrawnAmongThoseTheCaseHasRoomFor()
    {
        Assert.Equal(16, ScriptRunner.MassageAccessories.Count);
        Assert.All(ScriptRunner.MassageAccessories, a => Assert.True(SuperContestDatabase.Accessory(a) >= 0, a));
        Assert.Equal(("Pretty Dewdrop", "Glitter Powder"), (ScriptRunner.MassageAccessories[0], ScriptRunner.MassageAccessories[^1]));

        // Fill the case with all but the Seashell: whatever the die says, the Seashell is what she finds
        var host = new HeadlessScriptHost();
        foreach (string name in ScriptRunner.MassageAccessories.Where(a => a != "Seashell"))
        {
            int accessory = SuperContestDatabase.Accessory(name);
            while (host.FashionCase.CanFit(accessory)) host.FashionCase.Add(accessory);
        }
        var script = ScriptParser.Parse("test", "script Find\n  team massage room\n  if result == 0 end\n  team massage\n  end\n")[0];
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        host.Rolls.Enqueue(7);
        runner.Start(script, null);
        runner.RunToEnd();
        Assert.Equal(1, host.FashionCase.Count(SuperContestDatabase.Accessory("Seashell")));
        // With no room left for any of the sixteen she finds nothing, and says nothing
        int seashell = SuperContestDatabase.Accessory("Seashell");
        while (host.FashionCase.CanFit(seashell)) host.FashionCase.Add(seashell);
        int said = host.Transcript.Count;
        runner.Start(script, null);
        runner.RunToEnd();
        Assert.Equal(said, host.Transcript.Count);
        Assert.Equal(0, runner.Result);
    }

    [Fact]
    public void AScriptBefriendsTheChosenPokemonAsTheOriginalCountsIt()
    {
        // ScrCmd_IncreasePartyMonFriendship: the amount, half as much again with a Soothe Bell, then one more out of a
        // Luxury Ball; RESULT stays the Pokémon's place
        var plain = Mon();
        var belled = Mon();
        var luxury = Mon();
        var both = Mon();
        var egg = Mon();
        foreach (var p in new[] { plain, belled, luxury, both, egg }) p.Friendship = 50;
        belled.HeldItem = both.HeldItem = ItemDatabase.Get("Soothe Bell");
        luxury.Ball = both.Ball = "Luxury Ball";
        egg.IsEgg = true;
        var host = new HeadlessScriptHost();
        foreach (var p in new[] { plain, belled, luxury, both, egg }) host.Party.Add(p);
        var script = ScriptParser.Parse("test", "script Rub\n  choosepokemon\n  team befriend 3\n  end\n")[0];
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        int[] results = new int[6];
        for (int slot = 0; slot < 5; slot++)
        {
            host.PokemonChoice = slot;
            runner.Start(script, null);
            runner.RunToEnd();
            results[slot] = runner.Result;
        }
        host.PokemonChoice = 255;
        runner.Start(script, null);
        runner.RunToEnd();
        results[5] = runner.Result;
        Assert.Equal(new[] { 53, 54, 54, 55, 50 }, new[] { plain.Friendship, belled.Friendship, luxury.Friendship, both.Friendship, egg.Friendship });
        Assert.Equal(new[] { 0, 1, 2, 3, 254, 255 }, results);
    }

    [Fact]
    public void TheBoyGivesHisMachoBraceForTheSightOfBothCombee()
    {
        var host = new HeadlessScriptHost();
        host.Party.Add(Mon());
        Run("Combee", "PastoriaCityMiddleHouse", host);   // he asks
        Run("Combee", "PastoriaCityMiddleHouse", host);   // no Combee
        host.Party.Add(Mon("Combee", 10, Gender.Male));
        Run("Combee", "PastoriaCityMiddleHouse", host);   // one sex
        Assert.Equal(0, Have(host, "Macho Brace"));
        // An Egg is no Combee to look at
        var egg = Mon("Combee", 1, Gender.Female);
        egg.IsEgg = true;
        host.Party.Add(egg);
        Run("Combee", "PastoriaCityMiddleHouse", host);
        Assert.Equal(0, Have(host, "Macho Brace"));
        host.Party.Add(Mon("Combee", 12, Gender.Female));
        Run("Combee", "PastoriaCityMiddleHouse", host);
        Assert.Equal(1, Have(host, "Macho Brace"));
        Assert.True(host.Story.Has("FLAG_RECEIVED_PASTORIA_CITY_MIDDLE_HOUSE_MACHO_BRACE"));
        Run("Combee", "PastoriaCityMiddleHouse", host);
        Assert.Equal(1, Have(host, "Macho Brace"));
    }

    [Fact]
    public void TheScarfGuyGivesAScarfForEachConditionAtTwoHundredOneToAVisit()
    {
        var host = new HeadlessScriptHost();
        var lead = Mon();
        host.Party.Add(lead);
        string[] scarves = { "Red Scarf", "Blue Scarf", "Pink Scarf", "Green Scarf", "Yellow Scarf" };
        Run("Scarves", "PastoriaCityNortheastHouse", host);
        Assert.Equal(0, scarves.Sum(s => Have(host, s)));

        // 199 is not enough; with Smart and Tough both at 200 the Green Scarf comes first, the Yellow on the next visit
        (lead.Cool, lead.Smart, lead.Tough) = (199, 200, 255);
        Run("Scarves", "PastoriaCityNortheastHouse", host);
        Assert.Equal((1, 0), (Have(host, "Green Scarf"), Have(host, "Yellow Scarf")));
        Run("Scarves", "PastoriaCityNortheastHouse", host);
        Assert.Equal((1, 1), (Have(host, "Green Scarf"), Have(host, "Yellow Scarf")));
        Run("Scarves", "PastoriaCityNortheastHouse", host);
        Assert.Equal(2, scarves.Sum(s => Have(host, s)));

        // The first of the team that is no Egg is the one he looks at
        var egg = Mon();
        egg.IsEgg = true;
        (egg.Cool, lead.Cool, lead.Beauty, lead.Cute) = (255, 200, 200, 200);
        host.Party.Members.Insert(0, egg);
        for (int visit = 0; visit < 4; visit++) Run("Scarves", "PastoriaCityNortheastHouse", host);
        Assert.All(scarves, s => Assert.Equal(1, Have(host, s)));
        foreach (string colour in new[] { "RED", "BLUE", "PINK", "GREEN", "YELLOW" })
            Assert.True(host.Story.Has($"FLAG_RECEIVED_PASTORIA_CITY_NORTHEAST_HOUSE_{colour}_SCARF"));
    }

    [Fact]
    public void TheOldManOfRoute221RewardsAPokemonOfTheDaysLevel()
    {
        var host = new HeadlessScriptHost();
        host.Story.SetVar(DailyEvents.RandomLevelVariable, 37);
        host.Party.Add(Mon(level: 36));
        Run("Level", "Route221House", host);
        Assert.False(host.Story.Has("FLAG_DAILY_RECEIVED_ROUTE_221_HOUSE_REWARD"));
        host.Party.Add(Mon("Starly", 37));
        string[] rewards = { "Black Belt", "Expert Belt", "Focus Sash", "Black Belt" };
        for (int day = 0; day < rewards.Length; day++)
        {
            Run("Level", "Route221House", host);
            Assert.True(host.Story.Has("FLAG_DAILY_RECEIVED_ROUTE_221_HOUSE_REWARD"));
            int had = Have(host, rewards[day]);
            Run("Level", "Route221House", host);   // once a day
            Assert.Equal(had, Have(host, rewards[day]));
            host.Story.ClearDaily();
        }
        Assert.Equal((2, 1, 1), (Have(host, "Black Belt"), Have(host, "Expert Belt"), Have(host, "Focus Sash")));
        Assert.Contains(host.Transcript, t => t.Text.Contains("Starly") && t.Text.Contains("37"));

        // A game in its first day has had no level drawn: he makes do with one of his own
        var first = new HeadlessScriptHost();
        first.Party.Add(Mon(level: 25));
        Run("Level", "Route221House", first);
        Assert.Equal(25, first.Story.Var(DailyEvents.RandomLevelVariable));
        Assert.Equal(1, Have(first, "Black Belt"));
    }

    // ------------------------------------------------------------------ the hotel

    [Fact]
    public void TheSuiteKeyLetsTheBeautyIntoHerCottageWhereSheGivesTheWhiteFlute()
    {
        var overworld = Sinnoh.BuildMaps().Single(m => m.Name == "Sinnoh");
        var beauty = overworld.FindPerson("beauty", "valor_lakefront")!;
        var door = Sinnoh.Area("valor_lakefront")!.Warps[1];
        Assert.Equal((door.X, door.Z + 1), (beauty.GridX, beauty.GridY));
        Assert.Equal("Beauty", beauty.Script);
        // The key lies hidden beside the hotel's reception on Route 213, as the import has it
        Assert.Contains(overworld.HiddenItems, h => h.Value.Item == "Suite Key" && overworld.AreaAt(h.Key.X, h.Key.Y)?.Key == "route_213");

        var host = new HeadlessScriptHost { Map = overworld, PlayerTile = (beauty.GridX, beauty.GridY + 1), PlayerFacing = Direction.Up };
        host.Story.Set("FLAG_HIDE_GRAND_LAKE_VALOR_LAKEFRONT_EAST_HOUSE_BEAUTY");   // as common.ChapterFive leaves it
        Run("Beauty", "valor_lakefront", host, beauty);
        Assert.False(host.Story.Has("FLAG_HIDE_VALOR_LAKEFRONT_BEAUTY"));
        Assert.NotEmpty(host.Transcript);

        host.Bag.AddItem(ItemDatabase.Get("Suite Key")!, 1);
        Run("Beauty", "valor_lakefront", host, beauty);
        Assert.Equal(0, Have(host, "Suite Key"));
        Assert.True(host.Story.Has("FLAG_HIDE_VALOR_LAKEFRONT_BEAUTY"));
        Assert.False(host.Story.Has("FLAG_HIDE_GRAND_LAKE_VALOR_LAKEFRONT_EAST_HOUSE_BEAUTY"));
        // She is off the lakefront and in her cottage from then on
        overworld.ApplyPresence(host.Story.Has);
        Assert.DoesNotContain(overworld.NPCs, n => n == beauty);
        var cottage = Room("GrandLakeValorLakefrontEastHouse");
        cottage.ApplyPresence(host.Story.Has);
        Assert.Contains(cottage.NPCs, n => n.Script == "WhiteFlute");
        cottage.ApplyPresence(flag => flag == "FLAG_HIDE_GRAND_LAKE_VALOR_LAKEFRONT_EAST_HOUSE_BEAUTY");
        Assert.Empty(cottage.NPCs);
    }

    [Fact]
    public void TheHotelsLobbyRestsTheTeamAndLookerIsThereWhileTheStoryHasHim()
    {
        var host = new HeadlessScriptHost();
        var lead = Mon();
        lead.CurrentHP = 1;
        host.Party.Add(lead);
        Run("Rest", "GrandLakeRoute213Lobby", host);
        Assert.Equal(lead.MaxHP, lead.CurrentHP);

        var lobby = Room("GrandLakeRoute213Lobby");
        var looker = Assert.Single(lobby.Everyone, n => n.Key == "looker");
        Assert.Equal(("FLAG_HIDE_GRAND_LAKE_ROUTE_213_LOBBY_LOOKER", "Looker"), (looker.HiddenBy, looker.Script));
        var story = new HeadlessScriptHost { Map = lobby };
        Run("Looker", "GrandLakeRoute213Lobby", story, looker);
        Assert.True(story.Story.Has("FLAG_TALKED_TO_GRAND_LAKE_ROUTE_213_LOBBY_LOOKER"));
        int first = story.Transcript.Count;
        Run("Looker", "GrandLakeRoute213Lobby", story, looker);
        Assert.True(story.Transcript.Count > first);
        // He turns back to the receptionist he was questioning
        Assert.Equal(Direction.Right, looker.Facing);
    }

    [Fact]
    public void TheCottagesGuestsHaveTheirDiceAndTheirApp()
    {
        // The traveller names one of four places; the psychic reads one of four fortunes, once
        var places = Enumerable.Range(0, 4).Select(roll =>
        {
            var host = new HeadlessScriptHost();
            host.Rolls.Enqueue(roll);
            return Said(Run("Traveller", "GrandLakeRoute213EastHouse", host));
        }).ToList();
        Assert.Equal(4, places.Distinct().Count());

        var fortunes = Enumerable.Range(0, 10).Select(roll =>
        {
            var host = new HeadlessScriptHost();
            host.Rolls.Enqueue(roll);
            return Said(Run("Fortune", "GrandLakeRoute213NorthwestHouse", host));
        }).ToList();
        Assert.Equal(new[] { 4, 3, 2, 1 }, fortunes.GroupBy(f => f).Select(g => g.Count()).OrderDescending());
        var read = new HeadlessScriptHost();
        Run("Fortune", "GrandLakeRoute213NorthwestHouse", read);
        int before = read.Transcript.Count;
        Run("Fortune", "GrandLakeRoute213NorthwestHouse", read);
        Assert.DoesNotContain(fortunes, f => f == string.Join(" | ", read.Transcript.Skip(before).Select(t => t.Text)));

        // The Coin Toss app, once; and the lady's line changes with it
        var couple = new HeadlessScriptHost();
        string sour = Said(Run("Lady", "GrandLakeRoute213NortheastHouse", new HeadlessScriptHost()));
        Run("CoinToss", "GrandLakeRoute213NortheastHouse", couple);
        Assert.True(couple.Poketch.Has(PoketchApp.CoinToss));
        var lady = new HeadlessScriptHost();
        lady.Poketch.Register(PoketchApp.CoinToss);
        Assert.NotEqual(sour, Said(Run("Lady", "GrandLakeRoute213NortheastHouse", lady)));
        Assert.Contains(Room("GrandLakeRoute213NortheastHouse").HiddenItems, h => h.Value.Item == "Max Revive");
    }

    [Fact]
    public void TheGameDirectorKnowsACompletePokedexOfSinnoh()
    {
        Assert.Equal(210, Pokedex.Entries(PokedexMode.Sinnoh).Count);
        var host = new HeadlessScriptHost { SeenInSinnoh = 209 };
        string filling = Said(Run("Director", "GrandLakeValorLakefrontWestHouse", host));
        Assert.False(host.Story.Has("FLAG_MAP_LOCAL_GAME_DIRECTOR_SPOKE"));
        host.SeenInSinnoh = 210;
        string complete = Said(Run("Director", "GrandLakeValorLakefrontWestHouse", host));
        Assert.NotEqual(filling, complete);
        Assert.Contains("diploma", complete);
        // Having said so he talks of other things, until the player has gone out and come back
        int said = host.Transcript.Count;
        Run("Director", "GrandLakeValorLakefrontWestHouse", host);
        Assert.DoesNotContain("diploma", string.Join(" ", host.Transcript.Skip(said).Select(t => t.Text)));
        host.Story.ClearLocal();
        said = host.Transcript.Count;
        Run("Director", "GrandLakeValorLakefrontWestHouse", host);
        Assert.Contains("diploma", string.Join(" ", host.Transcript.Skip(said).Select(t => t.Text)));
    }

    // ------------------------------------------------------------------ Pastoria City

    [Fact]
    public void PastoriasBerryOfTheDayIsOneOfSeventeen()
    {
        // Occa to Chilan, in the order of the original's item numbers (ITEM_OCCA_BERRY + the roll)
        string[] berries =
        {
            "Occa", "Passho", "Wacan", "Rindo", "Yache", "Chople", "Kebia", "Shuca", "Coba", "Payapa", "Tanga", "Charti", "Kasib", "Haban", "Colbur",
            "Babiri", "Chilan"
        };
        for (int roll = 0; roll < berries.Length; roll++)
        {
            var host = new HeadlessScriptHost();
            host.Rolls.Enqueue(roll);
            Run("Berry", "PastoriaCitySouthwestHouse", host);
            Assert.Equal(1, Have(host, berries[roll] + " Berry"));
            Assert.True(host.Story.Has("FLAG_DAILY_RECEIVED_PASTORIA_CITY_SOUTHWEST_HOUSE_RANDOM_BERRY"));
            Run("Berry", "PastoriaCitySouthwestHouse", host);
            Assert.Equal(1, berries.Sum(b => Have(host, b + " Berry")));
        }
        // As the item table numbers them
        for (int i = 0; i < berries.Length; i++)
            Assert.Equal(ItemDatabase.Get("Occa Berry")!.Id + i, ItemDatabase.Get(berries[i] + " Berry")!.Id);
    }

    [Fact]
    public void TheCowgirlGivesTheMatchupCheckerForFiveCaughtInOneSafariGame()
    {
        const string gate = "PastoriaCityObservatoryGate1F";
        HeadlessScriptHost Back(int caught)
        {
            var host = new HeadlessScriptHost { Map = Room(gate), PlayerTile = (5, 3), PlayerFacing = Direction.Down };
            host.Safari.Start();
            host.Safari.Caught = caught;
            host.Safari.End();
            host.Story.SetVar("VAR_SAFARI_GAME_STATE", 2);
            return Run("OnEnter", gate, host);
        }
        var four = Back(4);
        Assert.False(four.Poketch.Has(PoketchApp.MatchupChecker));
        Assert.Equal((5, 7), four.PlayerTile);
        var five = Back(5);
        Assert.True(five.Poketch.Has(PoketchApp.MatchupChecker));
        Assert.True(five.Story.Has("FLAG_RECEIVED_PASTORIA_CITY_OBSERVATORY_GATE_1F_MATCHUPCHECKER"));
        // She came up to the player and went back to her place by the wall
        var cowgirl = five.Map!.Everyone.Single(n => n.Key == "cowgirl");
        Assert.Equal((1, 9, Direction.Right), (cowgirl.GridX, cowgirl.GridY, cowgirl.Facing));
        // The count is the game's own: it starts again with the next game, and a save keeps it
        var game = new SafariGame();
        game.Start();
        game.Caught = 3;
        game.End();
        Assert.Equal(3, game.Caught);
        game.Start();
        Assert.Equal(0, game.Caught);
        game.Resume(12, 345, 4);
        Assert.Equal((12, 345, 4), (game.Balls, game.Steps, game.Caught));
        Assert.Equal(new SafariSave(12, 345, 0), new SafariSave(12, 345));
        // Spoken to, she says what her game is, and afterwards what the app is for
        string asks = Said(Run("Cowgirl", gate, new HeadlessScriptHost()));
        int said = five.Transcript.Count;
        Run("Cowgirl", gate, five);
        Assert.NotEqual(asks, string.Join(" | ", five.Transcript.Skip(said).Select(t => t.Text)));
    }

    [Fact]
    public void TheBinocularsCostAHundredAndNameTheDaysPokemonOfAnArea()
    {
        var tables = SpecialEncounterTables.Sinnoh.GreatMarsh;
        Assert.Equal(32, tables.Local.Count);
        for (int area = 0; area < 6; area++)
        {
            var host = new HeadlessScriptHost { Money = 150 };
            host.Encounters.MarshDaily = 0x12345678;
            host.Rolls.Enqueue(area);
            Run("Binoculars", "PastoriaCityObservatoryGate2F", host);
            Assert.Equal(50, host.Money);
            string species = GreatMarsh.DailySpecies(tables, host.Encounters.MarshDaily, GreatMarsh.Areas[area], nationalDex: false)!;
            Assert.Contains(host.Transcript, t => t.Text.Contains(species) && !t.Text.Contains('{'));
            Assert.Contains(host.Transcript, t => t.Text.Contains($"Area {area + 1}"));
            // A second look can't be paid for
            host.Rolls.Enqueue(area);
            int said = host.Transcript.Count;
            Run("Binoculars", "PastoriaCityObservatoryGate2F", host);
            Assert.Equal(50, host.Money);
            Assert.DoesNotContain(host.Transcript.Skip(said), t => t.Text.Contains(species));
        }
        var no = new HeadlessScriptHost { Money = 500 };
        no.Answers.Enqueue(1);
        Run("Binoculars", "PastoriaCityObservatoryGate2F", no);
        Assert.Equal(500, no.Money);
    }

    // ------------------------------------------------------------------ the Seven Stars Restaurant

    private static readonly string[] Couples =
    {
        "ISMAEL_HARLEY", "ROMAN_KYLIE", "LEONARDO_REBECCA", "EUGENE_ALISON", "ESTEBAN_MEREDITH", "EMANUEL_BLYTHE", "DARRYL_VALERIE",
        "KENDRICK_GABRIELLA", "EMILIO_KAYLEE"
    };

    private static int Seated(HeadlessScriptHost host) => Couples.Count(c => !host.Story.Has("FLAG_HIDE_RESTAURANT_" + c));

    [Fact]
    public void FiveOfTheRestaurantsNineCouplesHaveATableEachDay()
    {
        var room = Room("SevenStarsRestaurant");
        // Eighteen diners at nine tables for two, each facing the other across it, and the waiter by the door
        var diners = room.Everyone.Where(n => n.IsTrainer).ToList();
        Assert.Equal(18, diners.Count);
        foreach (string couple in Couples)
        {
            var pair = diners.Where(n => n.HiddenBy == "FLAG_HIDE_RESTAURANT_" + couple).OrderBy(n => n.GridX).ToList();
            Assert.Equal(2, pair.Count);
            Assert.Equal((pair[0].GridY, pair[0].GridX + 2), (pair[1].GridY, pair[1].GridX));
            Assert.Equal((Direction.Right, Direction.Left), (pair[0].Facing, pair[1].Facing));
            Assert.Equal(pair[0].Script, pair[1].Script);
            Assert.Contains(room.Props, p => p.Type == PropType.Table && p.Covers(pair[0].GridX + 1, pair[0].GridY));
            Assert.All(pair, n => Assert.Equal(0, n.TrainerData!.SightRange));
        }

        // The die of nine seats five, whatever it shows: here it keeps coming up with the same three tables at first
        var host = new HeadlessScriptHost { TimeOfDay = TimeOfDay.Day };
        foreach (int roll in new[] { 2, 2, 7, 2, 7, 0, 0, 2, 8, 4, 6 }) host.Rolls.Enqueue(roll);
        Run("OnEnter", "SevenStarsRestaurant", host);
        Assert.Equal(5, Seated(host));
        foreach (int table in new[] { 2, 7, 0, 8, 4 }) Assert.False(host.Story.Has("FLAG_HIDE_RESTAURANT_" + Couples[table]));
        Assert.True(host.Story.Has("FLAG_DAILY_SET_RESTAURANT_TRAINERS"));
        Assert.True(host.Story.Has("FLAG_FIRST_ARRIVAL_VALOR_LAKEFRONT_RESTAURANT"));
        // They keep their tables for the day, whatever the die would say now
        host.Rolls.Enqueue(1);
        Run("OnEnter", "SevenStarsRestaurant", host);
        Assert.True(host.Story.Has("FLAG_HIDE_RESTAURANT_" + Couples[1]));
        // A die that never shows a fifth table still fills five
        var stuck = new HeadlessScriptHost { TimeOfDay = TimeOfDay.Twilight };
        Run("OnEnter", "SevenStarsRestaurant", stuck);
        Assert.Equal(5, Seated(stuck));
        // And a new day seats them afresh
        host.Story.ClearDaily();
        foreach (int roll in new[] { 1, 3, 5, 6, 8 }) host.Rolls.Enqueue(roll);
        Run("OnEnter", "SevenStarsRestaurant", host);
        Assert.Equal(5, Seated(host));
        foreach (int table in new[] { 1, 3, 5, 6, 8 }) Assert.False(host.Story.Has("FLAG_HIDE_RESTAURANT_" + Couples[table]));
    }

    [Theory]
    [InlineData(TimeOfDay.LateNight)]
    [InlineData(TimeOfDay.Morning)]
    public void TheRestaurantIsShutLateAtNightAndInTheMorning(TimeOfDay time)
    {
        var host = new HeadlessScriptHost { TimeOfDay = time };
        Run("OnEnter", "SevenStarsRestaurant", host);
        Assert.Equal(0, Seated(host));
        Assert.False(host.Story.Has("FLAG_DAILY_SET_RESTAURANT_TRAINERS"));
        // The waiter shows the player out, before the restaurant's door
        var door = Sinnoh.Area("valor_lakefront")!.Warps[0];
        Assert.Contains($"warp Sinnoh {door.X} {door.Z + 1}", host.Log);
        Assert.NotEmpty(host.Transcript);
    }

    [Fact]
    public void ACoupleBattlesTwoAgainstTwoOnceADayWithTheirTeamsAfresh()
    {
        var room = Room("SevenStarsRestaurant");
        var ismael = room.Everyone.Single(n => n.Key == "artist_ismael");
        var harley = room.Everyone.Single(n => n.Key == "beauty_harley");
        var host = new HeadlessScriptHost { Map = room };
        host.Party.Add(Mon());
        // With one Pokémon nobody will battle
        Run("IsmaelHarley", "SevenStarsRestaurant", host, ismael);
        Assert.DoesNotContain(host.Log, l => l.StartsWith("battle"));
        host.Party.Add(Mon("Starly"));
        // Turned down, they sit back; taken up, both battle at once and are done for the day, whichever was spoken to
        host.Answers.Enqueue(1);
        Run("IsmaelHarley", "SevenStarsRestaurant", host, harley);
        Assert.DoesNotContain(host.Log, l => l.StartsWith("battle"));
        int money = host.Money;
        Run("IsmaelHarley", "SevenStarsRestaurant", host, harley);
        Assert.Contains("battle artist_ismael and beauty_harley Won", host.Log);
        Assert.True(host.Story.Has("FLAG_DAILY_BEAT_RESTAURANT_ISMAEL_HARLEY"));
        Assert.True(host.Money > money);
        int battles = host.Log.Count(l => l.StartsWith("battle"));
        Run("IsmaelHarley", "SevenStarsRestaurant", host, ismael);
        Assert.Equal(battles, host.Log.Count(l => l.StartsWith("battle")));

        // The next day they battle again, and whoever was left with a fainted team has it afresh from Platinum's data
        foreach (var p in ismael.TrainerData!.Party.Members.Concat(harley.TrainerData!.Party.Members)) p.CurrentHP = 0;
        host.Story.ClearDaily();
        Run("IsmaelHarley", "SevenStarsRestaurant", host, ismael);
        Assert.Equal(battles + 1, host.Log.Count(l => l.StartsWith("battle")));
        Assert.All(ismael.TrainerData.Party.Members.Concat(harley.TrainerData.Party.Members), p => Assert.Equal(p.MaxHP, p.CurrentHP));
        Assert.Equal(("Kricketune", 28), (ismael.TrainerData.Party.Members[0].Species.Name, ismael.TrainerData.Party.Members[0].Level));
        // Every couple has a script that battles the two of them, and a daily flag of its own
        foreach (string script in room.Everyone.Where(n => n.IsTrainer).Select(n => n.Script!).Distinct())
        {
            var battle = ScriptLibrary.Default.Find(script, "SevenStarsRestaurant")!.Everything().Single(i => i.Op == Op.Battle);
            var pair = room.Everyone.Where(n => n.Script == script).Select(n => n.Key).ToHashSet();
            Assert.Equal(pair, new HashSet<string> { battle.Name, battle.Other });
        }
    }

    // ------------------------------------------------------------------ the Pokémon Mansion

    /// <summary>Plays the maids' challenge with each battle taking the turns given, as the original counts them.</summary>
    private static HeadlessScriptHost Challenge(int target, int[] turns, int boss = 0, Func<int, BattleOutcome>? outcome = null)
    {
        var room = Room("PokemonMansion");
        var maid = room.Everyone.Single(n => n.Key == "maid_far_west");
        var host = new HeadlessScriptHost { Map = room, PlayerTile = (2, 7), PlayerFacing = Direction.Left };
        host.Party.Add(Mon(level: 50));
        Run("OnEnter", "PokemonMansion", host);
        host.Story.SetVar("VAR_FIVE_MAID_KNOCKOUT_TURN_TARGET", target);
        room.ApplyPresence(host.Story.Has);
        int battle = 0;
        host.Fight = _ =>
        {
            int n = battle++;
            host.BattleTurns = n < turns.Length ? turns[n] : 0;
            return outcome?.Invoke(n) ?? BattleOutcome.Won;
        };
        host.Rolls.Enqueue(boss);
        return Run("Challenge", "PokemonMansion", host, maid);
    }

    [Fact]
    public void TheMaidsChallengeIsFiveBattlesInTheDaysNumberOfTurns()
    {
        // The day's number is five to nine, by a die of five, drawn once a day as the hall is come into
        for (int roll = 0; roll < 5; roll++)
        {
            var day = new HeadlessScriptHost();
            day.Rolls.Enqueue(roll);
            Run("OnEnter", "PokemonMansion", day);
            Assert.Equal(5 + roll, day.Story.Var("VAR_FIVE_MAID_KNOCKOUT_TURN_TARGET"));
            day.Rolls.Enqueue(4);
            Run("OnEnter", "PokemonMansion", day);
            Assert.Equal(5 + roll, day.Story.Var("VAR_FIVE_MAID_KNOCKOUT_TURN_TARGET"));
            Assert.True(day.Story.Has("FLAG_HIDE_POKEMON_MANSION_FIVE_MAID_KNOCKOUT_MAID") && day.Story.Has("FLAG_HIDE_POKEMON_MANSION_FIVE_MAID_KNOCKOUT_BOSS"));
        }

        // The five maids in their order, and with the number hit, the young master
        var hit = Challenge(target: 7, turns: new[] { 1, 2, 0, 3, 1 });
        string[] maids = { "maid_belinda", "maid_sophie", "maid_emily", "maid_elena", "maid_clare" };
        Assert.Equal(maids.Append("rich_boy_liam").Select(m => $"battle {m} Won"), hit.Log.Where(l => l.StartsWith("battle")));
        Assert.True(hit.Story.Has("FLAG_DAILY_TOOK_FIVE_MAID_KNOCKOUT_CHALLENGE"));
        Assert.Contains(hit.Transcript, t => t.Text.Contains("taken 6 turns") && t.Text.Contains("target is 7"));
        // Everyone is back where they stood, and nobody is left out on the mat
        var first = hit.Map!.Everyone.Single(n => n.Key == "maid_far_west");
        Assert.Equal((1, 7, Direction.Right), (first.GridX, first.GridY, first.Facing));
        Assert.All(hit.Map.Everyone.Where(n => n.Key is "five_maid_knockout_maid" or "liam" or "celeste"), n => Assert.NotEqual(true, n.Forced));

        // The other face of the die of two is the young lady
        Assert.Contains("battle lady_celeste Won", Challenge(7, new[] { 1, 2, 0, 3, 1 }, boss: 1).Log);
        // A turn too many or too few: the sweep is praised and nobody else comes out. Won in one turn each, five battles
        // come to no turns at all, as the original counts them
        foreach (var turns in new[] { new[] { 1, 2, 0, 3, 2 }, new[] { 1, 2, 0, 3, 0 }, new[] { 0, 0, 0, 0, 0 } })
        {
            var missed = Challenge(7, turns);
            Assert.Equal(5, missed.Log.Count(l => l.StartsWith("battle")));
            Assert.True(missed.Story.Has("FLAG_DAILY_TOOK_FIVE_MAID_KNOCKOUT_CHALLENGE"));
        }
        // Once taken it can't be taken again that day; and whoever says no may come back
        int fought = hit.Log.Count(l => l.StartsWith("battle"));
        Run("Challenge", "PokemonMansion", hit, first);
        Assert.Equal(fought, hit.Log.Count(l => l.StartsWith("battle")));
        var no = new HeadlessScriptHost { Map = Room("PokemonMansion"), PlayerTile = (2, 7) };
        no.Party.Add(Mon());
        no.Answers.Enqueue(1);
        Run("Challenge", "PokemonMansion", no, no.Map!.Everyone.Single(n => n.Key == "maid_far_west"));
        Assert.False(no.Story.Has("FLAG_DAILY_TOOK_FIVE_MAID_KNOCKOUT_CHALLENGE"));
        Assert.True(no.Story.Has("FLAG_STARTED_FIVE_MAID_KNOCKOUT_CHALLENGE"));
        // A battle lost ends it there
        var lost = Challenge(7, new[] { 1, 2 }, outcome: n => n == 1 ? BattleOutcome.Lost : BattleOutcome.Won);
        Assert.Equal(2, lost.Log.Count(l => l.StartsWith("battle")));
    }

    [Fact]
    public void WhoeverSpeaksToTheMaidFromHerSideIsStoodBeforeHer()
    {
        foreach (var from in new[] { (1, 8), (1, 6), (2, 7) })
        {
            var host = new HeadlessScriptHost { Map = Room("PokemonMansion"), PlayerTile = from };
            host.Party.Add(Mon());
            host.Answers.Enqueue(1);
            Run("Challenge", "PokemonMansion", host, host.Map!.Everyone.Single(n => n.Key == "maid_far_west"));
            Assert.Equal((2, 7), host.PlayerTile);
        }
    }

    [Fact]
    public void AScriptCountsItsBattlesTurnsAsTheOriginalDoes()
    {
        // TURNS is every battle this script has fought, added up; it starts again with the next script
        var script = ScriptParser.Parse("test", "script Two\n  battle self\n  battle self\n  if var TURNS == 5 setflag FLAG_FIVE_TURNS\n  end\n")[0];
        var room = Room("CafeCabin");
        var host = new HeadlessScriptHost { Map = room };
        host.Party.Add(Mon());
        int n = 0;
        host.Fight = _ => { host.BattleTurns = n++ == 0 ? 2 : 3; return BattleOutcome.Won; };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(script, room.Everyone.First(p => p.IsTrainer));
        runner.RunToEnd();
        Assert.True(host.Story.Has("FLAG_FIVE_TURNS"));
        host.Story.Unset("FLAG_FIVE_TURNS");
        runner.Start(script, room.Everyone.First(p => p.IsTrainer));
        runner.RunToEnd();
        Assert.False(host.Story.Has("FLAG_FIVE_TURNS"));   // three and three this time
    }

    [Fact]
    public void MrBacklotBringsAPokemonToTheTrophyGardenOnceADayAfterTheHallOfFame()
    {
        var office = Room("PokemonMansionOffice");
        var backlot = office.Everyone.Single(n => n.Key == "mr_backlot");
        var butler = office.Everyone.Single(n => n.Key == "old_man");
        var host = new HeadlessScriptHost { Map = office, PlayerTile = (backlot.GridX, backlot.GridY + 1), PlayerFacing = Direction.Up };
        Run("Backlot", "PokemonMansionOffice", host, backlot);
        Assert.Null(host.Encounters.TrophyFirst);

        // The original asks for the National Pokédex, not the Hall of Fame (plan 02 · S15, part a)
        host.Story.Set("FLAG_GAME_COMPLETED");
        Run("Backlot", "PokemonMansionOffice", host, backlot);
        Assert.Null(host.Encounters.TrophyFirst);
        host.NationalDex = true;
        // Disagreeing with him gets nowhere
        host.Answers.Enqueue(1);
        Run("Backlot", "PokemonMansionOffice", host, backlot);
        Assert.Null(host.Encounters.TrophyFirst);
        Run("Backlot", "PokemonMansionOffice", host, backlot);
        Assert.NotNull(host.Encounters.TrophyFirst);
        Assert.True(host.Story.Has("FLAG_DAILY_ADDED_TROPHY_GARDEN_MON"));
        string species = TrophyGardenRules.SpeciesIn(host.Encounters.TrophyFirst, SpecialEncounterTables.Sinnoh.TrophyGarden)!;
        Assert.Contains(host.Transcript, t => t.Text.Contains(species));
        // The butler has gone to see to it, and is back the next time the office is come into
        Assert.Equal(false, butler.Forced);
        // Once a day
        int first = host.Encounters.TrophyFirst!.Value;
        Run("Backlot", "PokemonMansionOffice", host, backlot);
        Assert.Equal(first, host.Encounters.TrophyFirst);
        Assert.Null(host.Encounters.TrophySecond);
        host.Story.ClearDaily();
        (butler.GridX, butler.GridY, butler.Facing, butler.Forced) = (6, 8, Direction.Down, null);
        Run("Backlot", "PokemonMansionOffice", host, backlot);
        Assert.Equal(first, host.Encounters.TrophySecond);
        // The butler's own word follows the day's news
        Assert.Contains(TrophyGardenRules.SpeciesIn(host.Encounters.TrophyFirst, SpecialEncounterTables.Sinnoh.TrophyGarden)!, Said(Run("Butler", "PokemonMansionOffice", new HeadlessScriptHost { Encounters = host.Encounters, Map = office }.With(h => h.Story.Set("FLAG_DAILY_ADDED_TROPHY_GARDEN_MON")), butler)));
    }

    [Fact]
    public void TheOfficesStatueIsGuardedButLateAtNightAndItsBookTellsOfManaphy()
    {
        var office = Room("PokemonMansionOffice");
        var trigger = Assert.Single(office.Triggers);
        Assert.Equal((7, 4, "BlockStatue", "VAR_POKEMON_MANSION_OFFICE_BLOCK_STATUE_STATE", 0), (trigger.X, trigger.Y, trigger.Script, trigger.Variable, trigger.Value));
        Assert.Contains(office.Props, p => p.Type == PropType.Statue && p.Covers(7, 3));
        Assert.Equal("Statue", office.SignScripts[(7, 3)]);
        Assert.Equal("Book", office.SignScripts[(7, 6)]);
        var guard = office.Everyone.Single(n => n.Key == "policeman");
        Assert.Equal((8, 4, "FLAG_HIDE_POKEMON_MANSION_OFFICE_POLICEMAN"), (guard.GridX, guard.GridY, guard.HiddenBy));

        foreach (var (time, away) in new[] { (TimeOfDay.Morning, false), (TimeOfDay.Day, false), (TimeOfDay.Twilight, false), (TimeOfDay.Night, false), (TimeOfDay.LateNight, true) })
        {
            var host = new HeadlessScriptHost { TimeOfDay = time };
            if (time == TimeOfDay.LateNight) host.Story.Unset("FLAG_HIDE_POKEMON_MANSION_OFFICE_POLICEMAN");
            else host.Story.Set("FLAG_HIDE_POKEMON_MANSION_OFFICE_POLICEMAN");
            Run("OnEnter", "PokemonMansionOffice", host);
            Assert.Equal(away, host.Story.Has("FLAG_HIDE_POKEMON_MANSION_OFFICE_POLICEMAN"));
            Assert.Equal(away ? 1 : 0, host.Story.Var("VAR_POKEMON_MANSION_OFFICE_BLOCK_STATUE_STATE"));
        }
        // Stepping up to the statue while he is there, the player is sent a step back
        var blocked = new HeadlessScriptHost { Map = office, PlayerTile = (7, 4), PlayerFacing = Direction.Up };
        Run("BlockStatue", "PokemonMansionOffice", blocked);
        Assert.Equal((7, 5), blocked.PlayerTile);

        // The book: read once, Manaphy is in the Pokédex as seen
        var reader = new HeadlessScriptHost();
        reader.Answers.Enqueue(1);
        Run("Book", "PokemonMansionOffice", reader);
        Assert.Empty(reader.SeenSpecies);
        Run("Book", "PokemonMansionOffice", reader);
        Assert.Equal(new[] { "Manaphy" }, reader.SeenSpecies);
        Assert.True(reader.Story.Has("FLAG_SKIP_POKEMON_MANSION_OFFICE_BOOK_SEEN_MANAPHY_CHECK"));
        int lines = reader.Transcript.Count;
        Run("Book", "PokemonMansionOffice", reader);
        Assert.DoesNotContain(reader.Transcript.Skip(lines), t => t.Text.Contains("added to the Pokédex"));
    }

    [Fact]
    public void AMaidsBedRestsTheTeam()
    {
        var host = new HeadlessScriptHost();
        var lead = Mon();
        lead.CurrentHP = 1;
        host.Party.Add(lead);
        host.Answers.Enqueue(1);
        Run("Bed", "PokemonMansionMaidsRoomWest", host);
        Assert.Equal(1, lead.CurrentHP);
        Run("Bed", "PokemonMansionMaidsRoomEast", host);
        Assert.Equal(lead.MaxHP, lead.CurrentHP);
    }

    // ------------------------------------------------------------------ who is left for the story

    [Fact]
    public void TheStorysPeopleOfTheseRoomsAreLeftForItsChapters()
    {
        // Sailor Eldritch at home (the Lunar Wing's errand, plan 02 · S15b) and the reporter in Canalave's west house
        // (plan 08's programmes) are in the original's rooms and not in ours yet. Byron is in Iron Island's cabin since
        // plan 02 · S15, part a, behind his flag (TheNationalPokedexAndTheBattleZoneTests), and nobody else is
        Assert.DoesNotContain(Room("CanalaveCitySailorEldritchHouse").Everyone, n => n.Key == "sailor_eldritch");
        Assert.Equal("FLAG_HIDE_IRON_ISLAND_HOUSE_BYRON", Assert.Single(Room("IronIslandHouse").Everyone).HiddenBy);
        Assert.Empty(Room("CanalaveCityWestHouse").Everyone);
        // The sailor's boy and his mother are as they are before the nightmare
        var home = Room("CanalaveCitySailorEldritchHouse");
        Assert.Equal(new[] { "little_boy", "pokefan_f" }, home.Everyone.Select(n => n.Key).Order());
        Assert.All(home.Everyone, n => Assert.NotEmpty(n.DialogLines));
    }
}

internal static class HeadlessHostSetup
{
    /// <summary>Sets a host up in passing, for a test that needs one made and changed in one expression.</summary>
    public static HeadlessScriptHost With(this HeadlessScriptHost host, Action<HeadlessScriptHost> change)
    {
        change(host);
        return host;
    }
}
