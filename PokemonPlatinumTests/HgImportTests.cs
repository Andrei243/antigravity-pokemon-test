using System.Buffers.Binary;
using System.Text;
using MapImporter;
using MapImporter.Hg;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// The HeartGold reader of <c>tools/MapImporter/Hg</c> (plan 20 · J1) on files built here byte by byte or written
/// out in a few lines, so the tests never need the decompilation: an archive, a binary matrix, a chunk with
/// HeartGold's own section, a header, an area's events, the tables of wild Pokémon, a trainer and a phone contact.
/// </summary>
public class HgImportTests
{
    [Fact]
    public void AnArchiveOfThreeMembersIsReadBack()
    {
        var members = new[] { new byte[] { 1, 2, 3 }, Array.Empty<byte>(), Encoding.ASCII.GetBytes("BMD0 and more") };
        var narc = Narc.Parse(Narc.Build(members));
        Assert.Equal(3, narc.Count);
        for (int i = 0; i < 3; i++) Assert.Equal(members[i], narc.Member(i).ToArray());
        Assert.Throws<ArgumentOutOfRangeException>(() => narc.Member(3).ToArray());
        Assert.Throws<InvalidDataException>(() => Narc.Parse(Encoding.ASCII.GetBytes("NOPE and some bytes")));
    }

    private static byte[] Le16(params int[] values) => values.SelectMany(v => BitConverter.GetBytes((ushort)v)).ToArray();

    [Fact]
    public void ABinaryMatrixIsReadWithItsSections()
    {
        // 2 × 1, headers and altitudes, the name "ab"; the second cell has no chunk
        var both = new byte[] { 2, 1, 1, 1, 2, (byte)'a', (byte)'b' }
            .Concat(Le16(60, 33)).Concat(new byte[] { 4, 0 }).Concat(Le16(7, 0xFFFF)).ToArray();
        var m = Matrix.ParseBinary(5, both, n => n == 60 ? "MAP_NEW_BARK" : "MAP_ROUTE_29");
        Assert.Equal(("ab", 2, 1), (m.Name, m.Width, m.Height));
        Assert.Equal("MAP_NEW_BARK", m.HeaderAt(0, 0));
        Assert.Equal("MAP_ROUTE_29", m.HeaderAt(1, 0));
        Assert.Equal(4, m.AltitudeAt(0, 0));
        Assert.Equal(7, m.LandAt(0, 0));
        Assert.Equal(Matrix.NoLand, m.LandAt(1, 0));

        // And without either: one cell, its chunk alone
        var bare = new byte[] { 1, 1, 0, 0, 0 }.Concat(Le16(3)).ToArray();
        var b = Matrix.ParseBinary(6, bare, _ => "MAP_X");
        Assert.Null(b.Headers);
        Assert.Null(b.Altitudes);
        Assert.Equal(3, b.LandAt(0, 0));

        // A file whose size isn't what its header says is refused
        Assert.Throws<InvalidDataException>(() => Matrix.ParseBinary(7, bare.Append((byte)0).ToArray(), _ => ""));
    }

    private static byte[] HgssLand(ushort[] attributes, int extraRecords)
    {
        var bytes = new List<byte>();
        foreach (int size in new[] { attributes.Length * 2, 0, 0, 0 }) bytes.AddRange(BitConverter.GetBytes(size));
        bytes.AddRange(BitConverter.GetBytes(LandData.HgssMark));
        bytes.AddRange(BitConverter.GetBytes((ushort)(extraRecords * 8)));
        bytes.AddRange(Enumerable.Repeat((byte)0xAB, extraRecords * 8));
        foreach (ushort a in attributes) bytes.AddRange(BitConverter.GetBytes(a));
        return bytes.ToArray();
    }

    [Fact]
    public void AHeartGoldChunkIsPlatinumsWithASectionOfItsOwnBeforeTheAttributes()
    {
        var attributes = new ushort[LandData.Tiles * LandData.Tiles];
        attributes[0] = 0x8002;               // tall grass, blocked
        attributes[LandData.Tiles + 1] = 0x06;   // headbutt
        var file = HgssLand(attributes, extraRecords: 2);
        var land = LandData.Parse(file, hgss: true);
        Assert.Equal(2, land.HgssRecords);
        Assert.Equal(0x02, land.Behaviour(0, 0));
        Assert.True(land.Solid(0, 0));
        Assert.Equal(0x06, land.Behaviour(1, 1));
        // Platinum's reader doesn't take it: the sections don't add up without the section of HeartGold's own
        Assert.Throws<InvalidDataException>(() => LandData.Parse(file));
        Assert.Equal(0, LandData.Parse(HgssLand(attributes, 0), hgss: true).HgssRecords);
    }

    private const string MapsH = """
        #define MAP_EVERYWHERE                                    0   // MAP_EVERYWHERE
        #define MAP_ROUTE_1                                       1   // MAP_R01
        #define MAP_NEW_BARK                                      2   // MAP_T20
        """;

    private const string HeadersH = """
        static const MapHeader sMapHeaders[] = {
            [MAP_ROUTE_1] = {
                                .wildEncounterBank = ENCDATA_R01,
                                .areaDataBank = 2,
                                .moveModelBank = 15,
                                .worldMapX = 13,
                                .worldMapY = 9,
                                .matrixId = NARC_map_matrix_map_matrix_0000_EVERYWHERE_bin,
                                .dayMusicId = SEQ_GS_R_1_29,
                                .nightMusicId = SEQ_GS_R_1_29,
                                .eventsBank = NARC_zone_event_006_R01_bin,
                                .mapsec = MAPSEC_ROUTE_1,
                                .regionNo = MAP_REGION_KANTO,
                                .weather = 0,
                                .mapType = MAP_TYPE_ROUTE,
                                .cameraType = 0,
                                .followMode = MAP_FOLLOWMODE_ALLOW,
                                .battleBg = BATTLE_BG_GENERAL,
                                .bikeAllowed = TRUE,
                                .escapeRopeAllowed = FALSE,
                                .flyAllowed = TRUE,
                                .outgoingCalls = TRUE,
                                .incomingCalls = FALSE,
                                .radioSignal = TRUE,
                                },
            [MAP_NEW_BARK] = {
                                .wildEncounterBank = ENCDATA_NA,
                                .areaDataBank = 1,
                                .worldMapX = 0,
                                .worldMapY = 0,
                                .matrixId = NARC_map_matrix_map_matrix_0000_EVERYWHERE_bin,
                                .dayMusicId = SEQ_GS_T_20,
                                .nightMusicId = SEQ_GS_T_20,
                                .eventsBank = NARC_zone_event_057_T20_bin,
                                .mapsec = MAPSEC_NEW_BARK_TOWN,
                                .regionNo = MAP_REGION_JOHTO,
                                .weather = 0,
                                .mapType = MAP_TYPE_CITY_TOWN,
                                .cameraType = 0,
                                .followMode = MAP_FOLLOWMODE_ALLOW,
                                .battleBg = BATTLE_BG_GENERAL,
                                .bikeAllowed = TRUE,
                                .escapeRopeAllowed = FALSE,
                                .flyAllowed = TRUE,
                                .outgoingCalls = TRUE,
                                .incomingCalls = TRUE,
                                .radioSignal = TRUE,
                                },
        };
        """;

    [Fact]
    public void AHeadersFieldsAreReadWithItsRegionAndItsPlaceOnTheMap()
    {
        var ids = HgMaps.ParseMapIds(MapsH);
        Assert.Equal(("MAP_NEW_BARK", 2, "T20"), ids[2]);
        var headers = HgMaps.ParseHeaders(HeadersH, ids);
        var route1 = headers["MAP_ROUTE_1"];
        Assert.Equal(("route_1", "R01", "006_R01", 0), (route1.Key, route1.Encounters, route1.Events, route1.Matrix));
        Assert.True(route1.Kanto);
        Assert.Equal((13, 9), (route1.WorldMapX, route1.WorldMapY));
        Assert.Equal(("Route 1", "ROUTE", "GENERAL"), (route1.Name, route1.MapType, route1.BattleBackground));
        Assert.True(route1.OutgoingCalls);
        Assert.False(route1.IncomingCalls);
        var newBark = headers["MAP_NEW_BARK"];
        Assert.False(newBark.Kanto);
        Assert.Null(newBark.Encounters);
        Assert.Equal(("new_bark", "New Bark Town"), (newBark.Key, newBark.Name));
    }

    [Fact]
    public void AnAreasEventsAreReadIntoPlatinumsShape()
    {
        var events = HgEvents.Parse("""
            {
              "header": "fielddata/script/scr_seq/event_T20.h",
              "bgs": [ { "scriptId": "_EV_scr_seq_T20_013 + 1", "type": 1, "x": 682, "z": 393, "y": 0, "dir": 4 } ],
              "objects": [
                { "id": "obj_R30_gsboy2", "spriteId": "SPRITE_GSBOY2", "movement": 16, "type": 1, "eventFlag": "FLAG_HIDE_X",
                  "scriptId": "std_trainer(TRAINER_YOUNGSTER_JOEY)", "facingDirection": 2, "param0": 4, "param1": 0, "param2": 0,
                  "xRange": 1, "yRange": 2, "x": 690, "z": 380, "y": 0 },
                { "id": "obj_R29_monstarball", "spriteId": "SPRITE_MONSTARBALL", "movement": 0, "type": 0, "eventFlag": "FLAG_NOTHING",
                  "scriptId": "std_itemball_r29_potion", "facingDirection": 0, "param0": 0, "param1": 0, "param2": 0,
                  "xRange": 0, "yRange": 0, "x": 600, "z": 395, "y": 0 }
              ],
              "warps": [ { "x": 684, "z": 393, "header": "MAP_NEW_BARK_ELMS_LAB_1F", "anchor": 0, "y": 0 } ],
              "coords": [ { "scriptId": "_EV_scr_seq_T20_002 + 1", "x": 676, "z": 396, "w": 1, "h": 7, "y": 0, "val": 0, "var": "VAR_SCENE_NEW_BARK_WEST_EXIT" } ]
            }
            """);
        Assert.Equal(("T20_013+1", 1, 682, 393), (events.Signs[0].Script, events.Signs[0].Type, events.Signs[0].X, events.Signs[0].Z));
        var joey = events.Objects[0];
        Assert.Equal(("TRAINER_YOUNGSTER_JOEY", "SPRITE_GSBOY2", "16", "1", "FLAG_HIDE_X"), (joey.Script, joey.GraphicsId, joey.MovementType, joey.TrainerType, joey.HiddenFlag));
        Assert.Equal(4, joey.Data[0]);   // how far the trainer sees
        Assert.Equal((1, 2, 690, 380), (joey.MovementRangeX, joey.MovementRangeZ, joey.X, joey.Z));
        Assert.Equal("", events.Objects[1].HiddenFlag);   // FLAG_NOTHING
        Assert.Equal("ITEM_POTION", HgEvents.BallItem(events.Objects[1].Script, "R29"));
        Assert.Equal("ITEM_TM28", HgEvents.BallItem("std_itemball_d22r0101_tm28", "D22R0102"));
        Assert.Equal("ITEM_ULTRA_BALL", HgEvents.BallItem("std_itemball_ultra_ball_2", "EVERYWHERE"));
        Assert.Null(HgEvents.BallItem("std_field_cut", "R30"));
        Assert.Equal(("MAP_NEW_BARK_ELMS_LAB_1F", 0), (events.Warps[0].DestHeaderId, events.Warps[0].DestWarpId));
        var trigger = events.Triggers[0];
        Assert.Equal(("T20_002+1", 1, 7, "VAR_SCENE_NEW_BARK_WEST_EXIT", "0"), (trigger.Script, trigger.Width, trigger.Length, trigger.Var, trigger.Value));
    }

    [Fact]
    public void AnEncounterEntryHasThreeSpeciesASlotItsVersionsItsSoundsAndItsSwarms()
    {
        var tables = HgssEncounters.Parse("""
            {"encounters":[{"map":"R29",
              "land":{"rate":25,"mons":[
                {"level":2,"species":{"morn":"SPECIES_PIDGEY","day":"SPECIES_PIDGEY","nite":"SPECIES_HOOTHOOT"}},
                {"level":{"HEARTGOLD":2,"SOULSILVER":4},"species":{"morn":{"HEARTGOLD":"SPECIES_SENTRET","SOULSILVER":"SPECIES_RATTATA"},"day":"SPECIES_SENTRET","nite":"SPECIES_RATTATA"}}]},
              "surf":{"rate":15,"mons":[{"level":{"min":15,"max":25},"species":"SPECIES_TENTACOOL"}]},
              "rock_smash":{"rate":0,"mons":[]},
              "fishing":{"old_rod":{"rate":0,"mons":[]},"good_rod":{"rate":0,"mons":[]},"super_rod":{"rate":75,"mons":[{"level":{"min":40,"max":40},"species":"SPECIES_CHINCHOU"}]}},
              "hoenn":["SPECIES_PLUSLE","SPECIES_MINUN"],"sinnoh":["SPECIES_SHINX","SPECIES_SHINX"],
              "landSwarm":"SPECIES_PIDGEY","surfSwarm":{"HEARTGOLD":"SPECIES_TENTACOOL","SOULSILVER":"SPECIES_KRABBY"},"nightFish":"SPECIES_SHELLDER"}]}
            """);
        var r29 = Assert.Single(tables);
        Assert.Equal(25, r29.Land.Rate);
        Assert.Equal("SPECIES_HOOTHOOT", r29.Land.Slots[0].Species(heartGold: true, "nite"));
        Assert.Equal("SPECIES_PIDGEY", r29.Land.Slots[0].Species(heartGold: false, "morn"));
        var split = r29.Land.Slots[1];
        Assert.True(split.Morning.Split);
        Assert.Equal(("SPECIES_SENTRET", "SPECIES_RATTATA"), (split.Morning.HeartGold, split.Morning.SoulSilver));
        Assert.Equal((2, 4), (split.MinLevel.HeartGold, split.MinLevel.SoulSilver));
        Assert.False(split.Day.Split);
        Assert.Equal((15, 25), (r29.Surf.Slots[0].MinLevel.HeartGold, r29.Surf.Slots[0].MaxLevel.HeartGold));
        Assert.Equal(75, r29.SuperRod.Rate);
        Assert.Equal(0, r29.OldRod.Rate);
        Assert.Equal(new[] { "SPECIES_PLUSLE", "SPECIES_MINUN" }, r29.Hoenn.Select(s => s.HeartGold));
        Assert.Equal("SPECIES_PIDGEY", r29.LandSwarm!.Value.HeartGold);
        Assert.Equal("SPECIES_KRABBY", r29.SurfSwarm!.Value.SoulSilver);
        Assert.Null(r29.FishSwarm);
        Assert.Equal("SPECIES_SHELLDER", r29.NightFish!.Value.HeartGold);
    }

    [Fact]
    public void AHeadbuttTableHasItsThreeKindsOfSlotAndItsTrees()
    {
        var tables = HgssEncounters.ParseHeadbutt("""
            {"tables":[{"Map":"R01",
              "CommonMons":[{"species":"SPECIES_HOOTHOOT","minLevel":2,"maxLevel":3}],
              "RareMons":[{"species":{"gold":"SPECIES_SPINARAK","silver":"SPECIES_LEDYBA"},"minLevel":5,"maxLevel":6}],
              "SecretMons":[],
              "Trees":[[{"x":1033,"y":300},{"x":1033,"y":301}],[{"x":1034,"y":307}]],
              "SecretTrees":[]}]}
            """);
        var r01 = Assert.Single(tables);
        Assert.Equal("SPECIES_HOOTHOOT", r01.Common[0].Day.HeartGold);
        Assert.Equal(("SPECIES_SPINARAK", "SPECIES_LEDYBA"), (r01.Rare[0].Day.HeartGold, r01.Rare[0].Day.SoulSilver));
        Assert.Equal((5, 6), (r01.Rare[0].MinLevel.HeartGold, r01.Rare[0].MaxLevel.HeartGold));
        Assert.Empty(r01.Secret);
        Assert.Equal(2, r01.Trees.Count);
        Assert.Equal(new[] { (1033, 300), (1033, 301) }, r01.Trees[0]);
    }

    [Fact]
    public void ATrainerIsReadWithoutItsMessages()
    {
        var problems = new List<string>();
        var trainers = HgssTrainers.Parse("""
            {"trainers":[
              {"type":"TRTYPE_MON","class":"TRAINERCLASS_PKMN_TRAINER","name":"","items":[],"ai_flags":0,"double":0,"party":[],"messages":[]},
              {"type":"TRTYPE_MON_ITEM_MOVES","class":"TRAINERCLASS_LEADER_FALKNER","name":"{TRNAME}Falkner","items":["ITEM_POTION"],"ai_flags":7,"double":0,
               "party":[{"difficulty":50,"level":9,"species":"SPECIES_PIDGEY","item":"ITEM_NONE","moves":["MOVE_TACKLE","MOVE_SAND_ATTACK"],"capsule":0},
                        {"difficulty":50,"level":13,"species":"SPECIES_PIDGEOTTO","item":"ITEM_ORAN_BERRY","moves":["MOVE_TACKLE","MOVE_FAINT_ATTACK","MOVE_NONE"],"capsule":0}],
               "messages":[{"type":"TRMSG_LOSE","message":"A line of the original's that must never be read"}]}]}
            """, "#define TRAINER_NONE 0\n#define TRAINER_LEADER_FALKNER 1\n", problems);
        var falkner = Assert.Single(trainers);
        Assert.Empty(problems);
        Assert.Equal(("leader_falkner", "Falkner", "Leader"), (falkner.Id, falkner.Name, falkner.Class));
        Assert.Equal(new[] { "Basic", "EvalAttack", "Expert" }, falkner.Ai);
        Assert.Equal(new[] { "Potion" }, falkner.Items);
        Assert.Equal(("Pidgeotto", 13, 50, "Oran Berry"), (falkner.Party[1].Species, falkner.Party[1].Level, falkner.Party[1].IvScale, falkner.Party[1].Item));
        Assert.Equal(new[] { "Tackle", "Feint Attack" }, falkner.Party[1].Moves);
        Assert.Null(falkner.Party[0].Item);
        Assert.DoesNotContain("must never", PokemonPlatinumEngine.Data.GameDataFiles.Serialize(trainers));
    }

    [Fact]
    public void APhoneContactIsReadWithItsAreaAndItsRematch()
    {
        var problems = new List<string>();
        var book = Phonebook.Parse("""
            {"pmtel_book":[{"id":"PHONE_CONTACT_YOUNGSTER_JOEY","type":4,"unk2":0,"trainerClass":"TRAINERCLASS_PHONE_YOUNGSTER",
              "trainerId":"TRAINER_YOUNGSTER_JOEY","mapId":"MAP_ROUTE_30","gift":"ITEM_HP_UP","phoneScriptIfLocal":"PHONE_SCRIPT_000",
              "unkC":255,"rematchWeekday":"RTC_WEEK_MONDAY","rematchTimeOfDay":"TIMEOFDAY_WILD_NITE","unkF":0,"sortParam":[1,2,3,0]},
              {"id":"PHONE_CONTACT_PROF__ELM","type":2,"trainerClass":"TRAINERCLASS_PHONE_POKEMON_PROFESSOR","trainerId":"TRAINER_NONE",
              "mapId":"MAP_NEW_BARK_ELMS_LAB_1F","gift":"ITEM_NONE","rematchWeekday":"RTC_WEEK_MAX","rematchTimeOfDay":"TIMEOFDAY_WILD_MAX"}]}
            """, problems);
        Assert.Empty(problems);
        var joey = book[0];
        Assert.Equal(("youngster_joey", "Youngster", "youngster_joey", "route_30", "HP Up", "Monday", "Night"),
            (joey.Id, joey.Class, joey.Trainer, joey.Area, joey.Gift, joey.RematchDay, joey.RematchTime));
        var elm = book[1];
        Assert.Equal(("prof_elm", "Pokémon Professor"), (elm.Id, elm.Class));
        Assert.Null(elm.Trainer);
        Assert.Null(elm.RematchDay);
        Assert.Null(elm.RematchTime);
    }

    [Fact]
    public void ABehaviourIsMappedByNameWhereHeartGoldsNumberDiffersFromPlatinums()
    {
        var names = HgMaps.ParseBehaviourNames("""
            enum TILE_BEHAVIOR {
                TILE_BEHAVIOR_0,
                TILE_BEHAVIOR_1_UNUSED,
                TILE_BEHAVIOR_TALL_GRASS,
                TILE_BEHAVIOR_VERY_TALL_GRASS,
                TILE_BEHAVIOR_4,
                TILE_BEHAVIOR_5,
                TILE_BEHAVIOR_HEADBUTT,
                TILE_BEHAVIOR_MAGMA = 44,
                TILE_BEHAVIOR_REFLECTIVE,
            };
            """);
        Assert.Equal("HEADBUTT", names[6]);
        Assert.False(names.ContainsKey(4));
        Assert.Equal("REFLECTIVE", names[0x2D]);
        // Platinum's reflective floor is at 0x2C, where HeartGold has its magma
        Assert.Equal((byte)TileBehavior.ShinyFloor, HgBehaviours.Of(0x2D, names, out bool known));
        Assert.True(known);
        Assert.Equal(HgBehaviours.Magma, HgBehaviours.Of(0x2C, names, out _));
        Assert.NotEqual((byte)TileBehavior.ShinyFloor, HgBehaviours.Magma);
        // HeartGold's own at a number Platinum leaves unused keeps it; a bare number is Platinum's own
        Assert.Equal(0x06, HgBehaviours.Of(0x06, names, out _));
        Assert.Equal((byte)TileBehavior.TallGrass, HgBehaviours.Of(0x02, names, out _));
        Assert.Equal(0x04, HgBehaviours.Of(0x04, names, out _));
        // The ladders stand where Platinum has a ledge's corner, and move
        Assert.NotEqual((byte)TileBehavior.LedgeCornerSouthEast, HgBehaviours.ByName["LADDER_DOWN"]);
    }

    [Fact]
    public void HeartGoldsTexturesAreSortedBeforePlatinumsAndTheRestFallsThrough()
    {
        Assert.Equal(PokemonPlatinumEngine.Data.TerrainCover.Water, HgCover.OfTexture("sea_on", out bool known));
        Assert.True(known);
        Assert.Equal(PokemonPlatinumEngine.Data.TerrainCover.Cliff, HgCover.OfTexture("wall01_d", out _));
        Assert.Equal(PokemonPlatinumEngine.Data.TerrainCover.Grass, HgCover.OfTexture("ngrass", out _));   // Platinum's own name
        Assert.Null(HgCover.OfTexture("zzz_nobody", out known));
        Assert.False(known);
        // Platinum's list hasn't learnt HeartGold's names
        Assert.Null(Cover.OfTexture("sea_on", out known));
        Assert.False(known);
    }
}
