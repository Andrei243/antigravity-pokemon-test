using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapImporter.Gba;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The GBA reader's events, wild Pokémon, trainers and rooms (plan 18 · W7), on a small decompilation built here file
/// by file: a town with a house in it, the house's room, the four lists of events, an item ball's script, a trainer's,
/// the tables of wild Pokémon, a trainer and its party, a heal location. No test needs the checkout.
/// </summary>
public sealed class GbaEventsTests : IDisposable
{
    private readonly string dir = Path.Combine(Path.GetTempPath(), "gba-events-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
    }

    private static readonly GbaGame TestGame = new("test", "kanto", "Kanto", 4, "MAP_TEST_TOWN");

    // The tileset's metatiles: 0 plain ground (labelled), 5 an arrow warp south, 7 ground nobody labelled, 8 a door
    private static readonly int[] Behaviours = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x65, 0x00, 0x00, 0x69 };
    private const int Lawn = 0, Mat = 5, Bare = 7, DoorTile = 8;

    private static ushort Block(int metatile, bool blocked = false) => (ushort)(metatile | (blocked ? 1 << 10 : 0) | 3 << 12);

    private void Put(string path, string text)
    {
        string full = Path.Combine(dir, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, text);
    }

    private void Put(string path, byte[] bytes)
    {
        string full = Path.Combine(dir, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, bytes);
    }

    private static byte[] Blocks(IEnumerable<ushort> blocks)
    {
        var list = blocks.ToList();
        var bytes = new byte[list.Count * 2];
        for (int i = 0; i < list.Count; i++) BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(i * 2), list[i]);
        return bytes;
    }

    /// <summary>
    /// A town of 8 by 8, lawn but for a house of 4 by 3 (tiles 2 to 5 across, rows 1 to 3) whose door is on (3, 3),
    /// and the house's room of 6 by 5: walls round a floor, its mat on (2, 4).
    /// </summary>
    private GbaMaps Build()
    {
        Put("include/constants/metatile_behaviors.h", "#define MB_NORMAL 0x00\n#define MB_SOUTH_ARROW_WARP 0x65\n#define MB_ANIMATED_DOOR 0x69\n");
        Put("include/constants/metatile_labels.h", "#define METATILE_General_Plain_Grass 0x000\n");
        Put("include/fieldmap.h", "#define NUM_METATILES_IN_PRIMARY 640\n");
        Put("src/data/tilesets/headers.h", "const struct Tileset gTileset_General =\n{\n    .metatileAttributes = gMetatileAttributes_General,\n};\n");
        Put("src/data/tilesets/metatiles.h", "const u32 gMetatileAttributes_General[] = INCBIN_U32(\"data/tilesets/primary/general/metatile_attributes.bin\");\n");
        var attributes = new byte[Behaviours.Length * 4];
        for (int i = 0; i < Behaviours.Length; i++) BinaryPrimitives.WriteUInt32LittleEndian(attributes.AsSpan(i * 4), (uint)Behaviours[i]);
        Put("data/tilesets/primary/general/metatile_attributes.bin", attributes);

        Put("data/layouts/layouts.json", """
            { "layouts": [
              { "id": "LAYOUT_TEST_TOWN", "name": "TestTown_Layout", "width": 8, "height": 8, "primary_tileset": "gTileset_General",
                "secondary_tileset": "gTileset_None", "blockdata_filepath": "data/layouts/TestTown/map.bin" },
              { "id": "LAYOUT_TEST_HOUSE", "name": "TestHouse_Layout", "width": 6, "height": 5, "primary_tileset": "gTileset_General",
                "secondary_tileset": "gTileset_None", "blockdata_filepath": "data/layouts/TestHouse/map.bin" } ] }
            """);
        var town = new List<ushort>();
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
                town.Add((x, y) == (3, 3) ? Block(DoorTile) : x is >= 2 and <= 5 && y is >= 1 and <= 3 ? Block(Bare, blocked: true) : Block(Lawn));
        Put("data/layouts/TestTown/map.bin", Blocks(town));
        var house = new List<ushort>();
        for (int y = 0; y < 5; y++)
            for (int x = 0; x < 6; x++)
                house.Add((x, y) == (2, 4) ? Block(Mat) : x == 0 || x == 5 || y == 0 || y == 4 ? Block(Bare, blocked: true) : Block(Bare));
        Put("data/layouts/TestHouse/map.bin", Blocks(house));

        Put("data/maps/map_groups.json", """{ "group_order": ["gMapGroup_Test"], "gMapGroup_Test": ["TestTown", "TestTown_House"] }""");
        Put("data/maps/TestTown/map.json", """
            {
              "id": "MAP_TEST_TOWN", "name": "TestTown", "layout": "LAYOUT_TEST_TOWN", "music": "MUS_TEST",
              "region_map_section": "MAPSEC_TEST_TOWN", "weather": "WEATHER_SUNNY", "map_type": "MAP_TYPE_TOWN",
              "allow_cycling": true, "allow_escaping": false, "allow_running": true,
              "connections": [ { "map": "MAP_TEST_TOWN_UNDERWATER", "offset": 0, "direction": "dive" } ],
              "object_events": [
                { "local_id": "LOCALID_TEST_LADY", "type": "object", "graphics_id": "OBJ_EVENT_GFX_WOMAN_1", "x": 1, "y": 5, "elevation": 3,
                  "movement_type": "MOVEMENT_TYPE_FACE_DOWN_AND_UP", "movement_range_x": 1, "movement_range_y": 2, "trainer_type": "TRAINER_TYPE_NONE",
                  "trainer_sight_or_berry_tree_id": "0", "script": "TestTown_EventScript_Lady", "flag": "0" },
                { "type": "object", "graphics_id": "OBJ_EVENT_GFX_YOUNGSTER", "x": 6, "y": 5, "elevation": 3,
                  "movement_type": "MOVEMENT_TYPE_FACE_LEFT", "movement_range_x": 1, "movement_range_y": 1, "trainer_type": "TRAINER_TYPE_NORMAL",
                  "trainer_sight_or_berry_tree_id": "3", "script": "TestTown_EventScript_Ben", "flag": "0" },
                { "type": "object", "graphics_id": "OBJ_EVENT_GFX_ITEM_BALL", "x": 7, "y": 7, "elevation": 3,
                  "movement_type": "MOVEMENT_TYPE_FACE_DOWN", "movement_range_x": 1, "movement_range_y": 1, "trainer_type": "TRAINER_TYPE_NONE",
                  "trainer_sight_or_berry_tree_id": "0", "script": "TestTown_EventScript_ItemPotion", "flag": "FLAG_HIDE_TEST_TOWN_POTION" },
                { "type": "clone", "graphics_id": "OBJ_EVENT_GFX_CUT_TREE", "x": -1, "y": 2, "target_local_id": "LOCALID_TREE", "target_map": "MAP_TEST_ROUTE" }
              ],
              "warp_events": [ { "x": 3, "y": 3, "elevation": 0, "dest_map": "MAP_TEST_TOWN_HOUSE", "dest_warp_id": "0" } ],
              "coord_events": [
                { "type": "trigger", "x": 0, "y": 7, "elevation": 3, "var": "VAR_TEST_SCENE", "var_value": "1", "script": "TestTown_EventScript_Stop" },
                { "type": "weather", "x": 1, "y": 7, "elevation": 3, "weather": "COORD_EVENT_WEATHER_SUNNY" }
              ],
              "bg_events": [
                { "type": "sign", "x": 6, "y": 2, "elevation": 0, "player_facing_dir": "BG_EVENT_PLAYER_FACING_ANY", "script": "TestTown_EventScript_TownSign" },
                { "type": "hidden_item", "x": 0, "y": 0, "elevation": 3, "item": "ITEM_RARE_CANDY", "flag": "FLAG_HIDDEN_ITEM_TEST_TOWN_RARE_CANDY", "quantity": 2, "underfoot": false }
              ]
            }
            """);
        Put("data/maps/TestTown_House/map.json", """
            {
              "id": "MAP_TEST_TOWN_HOUSE", "name": "TestTown_House", "layout": "LAYOUT_TEST_HOUSE", "music": "MUS_TEST",
              "region_map_section": "MAPSEC_TEST_TOWN", "weather": "WEATHER_NONE", "map_type": "MAP_TYPE_INDOOR",
              "allow_cycling": false, "allow_escaping": false, "allow_running": false, "connections": null,
              "object_events": [], "warp_events": [ { "x": 2, "y": 4, "elevation": 0, "dest_map": "MAP_TEST_TOWN", "dest_warp_id": "0" } ],
              "coord_events": [], "bg_events": []
            }
            """);

        Put("data/scripts/item_ball_scripts.inc", "TestTown_EventScript_ItemPotion::\n\tfinditem ITEM_POTION\n\tend\n\nTestTown_EventScript_ItemTwo::\n\tfinditem ITEM_POKE_BALL, 3\n\tend\n");
        Put("data/maps/TestTown/scripts.inc", "TestTown_MapScripts::\n\t.byte 0\n\nTestTown_EventScript_Ben::\n\ttrainerbattle_single TRAINER_YOUNGSTER_BEN, TestTown_Text_BenIntro, TestTown_Text_BenDefeat\n\tmsgbox TestTown_Text_BenPostBattle, MSGBOX_AUTOCLOSE\n\tend\n");
        Put("src/data/trainers.h", """
            const struct Trainer gTrainers[] = {
                [TRAINER_NONE] = {
                    .trainerName = _(""),
                },
                [TRAINER_YOUNGSTER_BEN] = {
                    .trainerClass = TRAINER_CLASS_YOUNGSTER,
                    .trainerName = _("BEN"),
                    .items = {ITEM_POTION, ITEM_NONE},
                    .doubleBattle = FALSE,
                    .aiFlags = AI_SCRIPT_CHECK_BAD_MOVE | AI_SCRIPT_CHECK_VIABILITY,
                    .party = ITEM_CUSTOM_MOVES(sParty_YoungsterBen),
                },
            };
            """);
        Put("src/data/trainer_parties.h", """
            static const struct TrainerMonNoItemDefaultMoves sParty_Nobody[] = {DUMMY_TRAINER_MON};
            static const struct TrainerMonItemCustomMoves sParty_YoungsterBen[] = {
                {
                    .iv = 100,
                    .lvl = 11,
                    .species = SPECIES_RATTATA,
                    .heldItem = ITEM_ORAN_BERRY,
                    .moves = {MOVE_TACKLE, MOVE_TAIL_WHIP, MOVE_NONE, MOVE_NONE}
                },
                {
                    .iv = 0,
                    .lvl = 11,
                    .species = SPECIES_NIDORAN_F,
                    .heldItem = ITEM_NONE,
                    .moves = {MOVE_GROWL, MOVE_NONE, MOVE_NONE, MOVE_NONE}
                },
            };
            """);
        Put("src/data/heal_locations.json", """
            { "heal_locations": [ { "id": "HEAL_LOCATION_TEST_TOWN", "map": "MAP_TEST_TOWN", "x": 3, "y": 4, "respawn_map": "MAP_TEST_TOWN_HOUSE" } ] }
            """);
        Put("src/data/wild_encounters.json", WildJson());
        return new GbaMaps(dir, TestGame);
    }

    /// <summary>Twelve land slots of twelve species in a known order, and a fishing table of ten split among the rods as FireRed's header says.</summary>
    private static readonly string[] LandSpecies =
        { "PIDGEY", "RATTATA", "SPEAROW", "EKANS", "SANDSHREW", "NIDORAN_F", "NIDORAN_M", "VULPIX", "ZUBAT", "ODDISH", "PARAS", "MEOWTH" };

    private static string WildJson()
    {
        string Mons(IEnumerable<string> species, int level) =>
            string.Join(", ", species.Select((s, i) => $$"""{ "min_level": {{level + i}}, "max_level": {{level + i + (i % 2)}}, "species": "SPECIES_{{s}}" }"""));
        return $$"""
            { "wild_encounter_groups": [ {
              "label": "gWildMonHeaders", "for_maps": true,
              "fields": [
                { "type": "land_mons", "encounter_rates": [20, 20, 10, 10, 10, 10, 5, 5, 4, 4, 1, 1] },
                { "type": "water_mons", "encounter_rates": [60, 30, 5, 4, 1] },
                { "type": "rock_smash_mons", "encounter_rates": [60, 30, 5, 4, 1] },
                { "type": "fishing_mons", "encounter_rates": [70, 30, 60, 20, 20, 40, 40, 15, 4, 1],
                  "groups": { "old_rod": [0, 1], "good_rod": [2, 3, 4], "super_rod": [5, 6, 7, 8, 9] } } ],
              "encounters": [
                { "map": "MAP_TEST_TOWN", "base_label": "sTestTown_FireRed",
                  "land_mons": { "encounter_rate": 21, "mons": [ {{Mons(LandSpecies, 2)}} ] },
                  "rock_smash_mons": { "encounter_rate": 50, "mons": [ {{Mons(new[] { "GEODUDE", "GEODUDE", "ONIX", "ONIX", "ONIX" }, 5)}} ] },
                  "fishing_mons": { "encounter_rate": 10, "mons": [ {{Mons(new[] { "MAGIKARP", "MAGIKARP", "POLIWAG", "HORSEA", "KRABBY", "GOLDEEN", "SEAKING", "STARYU", "SHELLDER", "GYARADOS" }, 5)}} ] } },
                { "map": "MAP_TEST_TOWN", "base_label": "sTestTown_LeafGreen",
                  "land_mons": { "encounter_rate": 21, "mons": [ {{Mons(LandSpecies.Reverse(), 2)}} ] } }
              ] } ] }
            """;
    }

    private (GbaWorld World, GbaTables Tables) Read()
    {
        var maps = Build();
        return (new GbaWorld(maps), new GbaTables(dir, maps.Maps));
    }

    [Fact]
    public void TheFourListsOfEventsAreRead()
    {
        var town = Build().ById["MAP_TEST_TOWN"];
        Assert.Equal(3, town.Objects.Count);   // the clone is the neighbour's
        Assert.Equal(new GbaObject("LOCALID_TEST_LADY", "OBJ_EVENT_GFX_WOMAN_1", 1, 5, 3, "MOVEMENT_TYPE_FACE_DOWN_AND_UP", 1, 2,
            "TRAINER_TYPE_NONE", "0", "TestTown_EventScript_Lady", "0"), town.Objects[0]);
        Assert.Equal(new GbaWarp(3, 3, 0, "MAP_TEST_TOWN_HOUSE", 0), town.Warps.Single());
        Assert.Equal(2, town.Coords.Count);
        Assert.Equal("VAR_TEST_SCENE", town.Coords[0].Var);
        Assert.Equal(new GbaBg("hidden_item", 0, 0, 3, "", "ITEM_RARE_CANDY", 2, "FLAG_HIDDEN_ITEM_TEST_TOWN_RARE_CANDY"), town.Bgs[1]);
    }

    [Fact]
    public void TheGamesTablesAreRead()
    {
        var (_, tables) = Read();
        Assert.Equal(("ITEM_POTION", 1), tables.ItemBalls["TestTown_EventScript_ItemPotion"]);
        Assert.Equal(("ITEM_POKE_BALL", 3), tables.ItemBalls["TestTown_EventScript_ItemTwo"]);
        Assert.Equal("TRAINER_YOUNGSTER_BEN", tables.TrainerScripts["TestTown_EventScript_Ben"]);
        Assert.Equal(new GbaHealLocation("HEAL_LOCATION_TEST_TOWN", "MAP_TEST_TOWN", 3, 4, "MAP_TEST_TOWN_HOUSE"), tables.HealLocations.Single());

        // A trainer in the shape of trainers.json (decision 8), the placeholder with nobody left out
        var ben = tables.Trainers.Single();
        Assert.Equal(("youngster_ben", "Ben", "Youngster"), (ben.Id, ben.Name, ben.Class));
        Assert.Equal(new[] { "Basic", "Expert" }, ben.Ai);
        Assert.Equal(new[] { "Potion" }, ben.Items);
        Assert.Equal(new[] { "Rattata", "Nidoran♀" }, ben.Party.Select(p => p.Species));
        Assert.Equal((11, 100, "Oran Berry"), (ben.Party[0].Level, ben.Party[0].IvScale, ben.Party[0].Item));
        Assert.Equal(new[] { "Tackle", "Tail Whip" }, ben.Party[0].Moves);
    }

    [Fact]
    public void AnEncounterTablesTwelveSlotsKeepTheirOrder()
    {
        var (_, tables) = Read();
        var wild = tables.Wild["MAP_TEST_TOWN"];
        // FireRed's own table, not LeafGreen's (which is the same species the other way round)
        Assert.Equal(LandSpecies.Select(s => "SPECIES_" + s), wild.Land!.Slots.Select(s => s.Species));
        Assert.Equal(WorldAreaFile.LandSlotWeights, wild.Land.Slots.Select(s => s.Weight));
        Assert.Null(wild.Water);

        var area = new WorldAreaFile();
        GbaEvents.Fill(area, new GbaPlaced(Build().ById["MAP_TEST_TOWN"], null!, 0, 0), tables, _ => null, new List<string>());
        Assert.Equal(new[] { "Pidgey", "Rattata", "Spearow", "Ekans", "Sandshrew", "Nidoran♀", "Nidoran♂", "Vulpix", "Zubat", "Oddish", "Paras", "Meowth" },
            area.Land!.Select(s => s.Species));
        Assert.Equal(21, area.LandRate);
        Assert.Equal(Enumerable.Range(2, 12), area.Land.Select(s => s.Level));
        Assert.Equal((null, 4), (area.Land[0].MaxLevel, area.Land[1].MaxLevel));
        // Only a weight Platinum's table of the kind wouldn't give is written: the Old Rod's 70, the Good Rod's 60, 20 and 20
        Assert.All(area.Land, s => Assert.Null(s.Weight));
        Assert.Equal(new int?[] { 70, null }, area.OldRod!.Select(s => s.Weight));
        Assert.Equal(new int?[] { 60, 20, 20 }, area.GoodRod!.Select(s => s.Weight));
        Assert.Equal(new[] { "Goldeen", "Seaking", "Staryu", "Shellder", "Gyarados" }, area.SuperRod!.Select(s => s.Species));
        Assert.All(area.SuperRod!, s => Assert.Null(s.Weight));
        Assert.Equal(new[] { "Geodude", "Geodude", "Onix", "Onix", "Onix" }, area.RockSmash!.Select(s => s.Species));
        Assert.Equal(50, area.RockSmashRate);
    }

    [Fact]
    public void TheEventsRoundTripThroughTheAreaFile()
    {
        var (world, tables) = Read();
        string folder = Path.Combine(dir, "out", "world");
        GbaImport.WriteWorld(world, tables, folder);
        var index = GameDataFiles.Load<WorldIndexFile>(Path.Combine(folder, World.IndexFile));
        var area = World.Open(folder, index).Area("test_town")!;
        var town = world.Matrices.SelectMany(m => m.Maps).Single(p => p.Map.Name == "TestTown");

        var warp = area.Warps.Single();
        Assert.Equal((town.X + 3, town.Y + 3, "test_town_house", 0), (warp.X, warp.Z, warp.To, warp.ToWarp));

        var lady = area.Objects.Single(o => o.Id == "test_lady");
        Assert.Equal(("woman_1", "look_south_and_north", 1, 1, 2), (lady.Looks, lady.Movement, lady.Facing, lady.RangeX, lady.RangeZ));
        Assert.Equal((town.X + 1, town.Y + 5), (lady.X, lady.Z));
        Assert.Null(lady.HiddenBy);
        Assert.Null(lady.Trainer);

        var ben = area.Objects.Single(o => o.Looks == "youngster");
        Assert.Equal(("normal", 3, "TRAINER_YOUNGSTER_BEN", 2), (ben.Trainer, ben.Sight, ben.Script, ben.Facing));
        Assert.Equal(2, GbaEvents.Movement("MOVEMENT_TYPE_FACE_LEFT").Facing);

        var ball = area.Objects.Single(o => o.Looks == GbaEvents.ItemBallLooks);
        Assert.Equal(("Potion", (int?)null, "FLAG_HIDE_TEST_TOWN_POTION"), (ball.Item, ball.Count, ball.HiddenBy));

        var sign = area.Signs.Single(s => s.Type == 0);
        Assert.Equal((town.X + 6, town.Y + 2, "TestTown_EventScript_TownSign"), (sign.X, sign.Z, sign.Script));
        var hidden = area.Signs.Single(s => s.Type == AreaSign.HiddenItem);
        Assert.Equal(("Rare Candy", (int?)2, "FLAG_HIDDEN_ITEM_TEST_TOWN_RARE_CANDY"), (hidden.Item, hidden.Count, hidden.Flag));

        var trigger = area.Triggers.Single();   // the weather's tile is no trigger
        Assert.Equal((town.X, town.Y + 7, "VAR_TEST_SCENE", "1", "TestTown_EventScript_Stop"), (trigger.X, trigger.Z, trigger.Variable, trigger.Value, trigger.Script));
        Assert.Null(area.Dive);   // the sea floor it joins has no map here

        // The region's own files: its trainers in trainers.json's shape, its spawns and where its wild Pokémon live
        var trainers = GameDataFiles.Deserialize<List<TrainerRecord>>(File.ReadAllText(Path.Combine(folder, "trainers.json")));
        Assert.Equal("youngster_ben", trainers.Single().Id);
        var spawn = GameDataFiles.Deserialize<List<RegionSpawn>>(File.ReadAllText(Path.Combine(folder, "spawns.json"))).Single();
        Assert.Equal((1, "test_town", town.X + 3, town.Y + 4, "TestTown_House"), (spawn.Id, spawn.Area, spawn.X, spawn.Y, spawn.Room));
        var habitats = GameDataFiles.Deserialize<WorldHabitatsFile>(File.ReadAllText(Path.Combine(folder, WorldHabitatsFile.FileName)));
        Assert.Contains("Pidgey", habitats.Areas.Single(a => a.Key == "test_town").Day!);
    }

    [Fact]
    public void AHousesRoomIsBuiltFromItsLayoutAndWalkedInto()
    {
        var (world, tables) = Read();
        string folder = Path.Combine(dir, "out", "world");
        GbaImport.WriteWorld(world, tables, folder);
        var index = GameDataFiles.Load<WorldIndexFile>(Path.Combine(folder, World.IndexFile));
        var opened = World.Open(folder, index);
        Assert.Equal(MapSetting.Indoors, index.Maps.Single(m => m.Name == "TestTown_House").Setting);
        var maps = index.Maps.ToDictionary(m => m.Name, m => WorldMapBuilder.Build(opened, m));
        var town = maps["Kanto"];
        var room = maps["TestTown_House"];

        // The house is found at its door, a building of four by three whose front row the door is in
        var house = town.PlacedBuildings!.Single();
        Assert.Equal((2, 1, 5, 3), (house.X0, house.Y0, house.X1, house.Y1));
        Assert.Equal((3, "TestTown_House"), house.Doors.Single());
        Assert.Equal(BuildingKind.House, house.Kind);
        Assert.Equal(TileType.Door, town.GetGroundTile(3, 3));
        Assert.Equal(TileType.RoofRed, town.GetGroundTile(2, 1));
        Assert.Equal(TileType.Wall, town.GetGroundTile(5, 3));

        // Its door leads onto the room's mat, the room's mat back out before the door
        var into = town.GetWarpAt(3, 3)!;
        Assert.Equal(("TestTown_House", 2, 3), (into.TargetMap, into.TargetX, into.TargetY));
        var outOf = room.GetWarpAt(2, 4)!;
        Assert.Equal(("Kanto", 3, 4), (outOf.TargetMap, outOf.TargetX, outOf.TargetY));

        // The room: floor where it is walked, wall round it and wherever the layout reaches no further
        Assert.False(room.IsSolid(2, 2));
        Assert.Equal(TileType.Floor, room.GetGroundTile(2, 2));
        Assert.Equal(TileType.Wall, room.GetGroundTile(0, 0));
        // The room is as large as its layout and the wall round it, not the chunk it is laid in (plan 19 · K1)
        Assert.True(room.Width < 32 && room.Height < 32, $"the room is {room.Width} by {room.Height}");
        Assert.Equal(TileType.Wall, room.GetGroundTile(room.Width - 1, room.Height - 1));
        Assert.True(room.IsSolid(room.Width - 1, room.Height - 1));
        Assert.Equal(InteriorStyle.House, room.Interior);

        // And walked: from the town's street into every open tile of the room
        var reached = WorldWalk.From(name => maps[name], new MapSpot("Kanto", 3, 6));
        Assert.True(reached.TryGetValue(room, out var inside));
        Assert.Superset(new HashSet<(int, int)> { (1, 1), (4, 1), (1, 3), (4, 3), (2, 3) }, inside!);
    }

    [Theory]
    [InlineData("ViridianCity_PokemonCenter_1F", BuildingKind.PokemonCenter)]
    [InlineData("ViridianCity_Mart", BuildingKind.PokeMart)]
    [InlineData("PewterCity_Gym", BuildingKind.Gym)]
    [InlineData("PalletTown_ProfessorOaksLab", BuildingKind.Lab)]
    [InlineData("JubilifePokemonCenter", BuildingKind.PokemonCenter)]
    [InlineData("RowanLab", BuildingKind.Lab)]
    [InlineData("PalletTown_PlayersHouse_1F", null)]
    public void ABuildingsKindIsWhatItsDoorLeadsInto(string target, BuildingKind? kind) => Assert.Equal(kind, MapStructures.KindOfDoor(target));

    [Theory]
    [InlineData("ITEM_TM01_FOCUS_PUNCH", "TM01")]
    [InlineData("ITEM_TM_SHADOW_BALL", "TM30")]
    [InlineData("ITEM_POKE_BALL", "Poké Ball")]
    [InlineData("ITEM_X_DEFEND", "X Defense")]
    public void AGbaConstantIsTheDatasName(string constant, string name) => Assert.Equal(name, GbaNames.Item(constant));

    [Theory]
    // A warp on a wall that is no way anywhere is never taken (FireRed's player's house, its third mat warp at 3, 9)
    [InlineData(TileBehavior.None, true, true)]
    // A door, the mat at a room's edge and a pad are walked into or onto, blocked or not, and so is open floor
    [InlineData(TileBehavior.Door, true, false)]
    [InlineData(TileBehavior.ExitSouth, false, false)]
    [InlineData(TileBehavior.WarpPanel, true, false)]
    [InlineData(TileBehavior.None, false, false)]
    public void AWarpOnAWallThatIsNoWayIsNeverTaken(TileBehavior behaviour, bool blocked, bool never) =>
        Assert.Equal(never, GbaEvents.NeverTaken(behaviour, blocked));
}
