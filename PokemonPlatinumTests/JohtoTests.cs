using MapImporter.Hg;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// Johto as HeartGold's maps make it (plan 20 · J2): what <c>Data/world/johto</c> opens, the overworld and its rooms
/// built from the second DS reader's files, where a new game there begins, its wild Pokémon by the time of day, its
/// trainers and its catalogue of models. Floors, not counts, wherever a later session opens more (plan 16 · T16).
/// </summary>
public class JohtoTests
{
    // Maps of this class's own, so nothing another test does to the database's maps shows here. Read only.
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Johto"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Dictionary<Map, HashSet<(int X, int Y)>>> Walked =
        new(() => WorldWalk.From(MapNamed, RegionDatabase.Get(RegionDatabase.Johto)!.Start!));

    private static World Johto => LoadedWorld.Value;
    private static Map Overworld => BuiltMaps.Value["Johto"];
    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : MapDatabase.Get(name);

    private static readonly string[] FirstAreas = { "new_bark", "route_29", "cherrygrove", "route_30", "route_31", "violet" };

    [Fact]
    public void TheIndexOpensNewBarkTownToVioletCityWithTheirRooms()
    {
        Assert.Equal("Johto", Johto.Index.Region);
        Assert.False(Johto.Index.BuildingsAtDoors);
        foreach (string key in FirstAreas) Assert.Contains(key, Johto.Index.Areas);
        Assert.True(Johto.Index.Areas.Count >= 30, $"only {Johto.Index.Areas.Count} areas open in Johto");
        var johto = RegionDatabase.Get(RegionDatabase.Johto)!;
        foreach (var entry in Johto.Index.Maps)
        {
            Assert.Contains(entry.Name, johto.Maps);
            Assert.True(BuiltMaps.Value.ContainsKey(entry.Name), $"{entry.Name} isn't built");
            if (entry.Setting == MapSetting.Indoors) Assert.True(BuiltMaps.Value[entry.Name].IsIndoors, $"{entry.Name} is no room");
            // a map's name is no other region's
            Assert.Equal(1, RegionDatabase.All.Count(r => r.Maps.Contains(entry.Name)));
        }
        // Every open area has its file and its overlay, and the overlay a song of Johto's own
        foreach (string key in Johto.Index.Areas)
        {
            Assert.NotNull(Johto.Area(key));
            Assert.StartsWith("johto/", Johto.Overlay(key)!.BgmTrack);
        }
        // HeartGold's keys meet neither Sinnoh's nor Kanto's
        var others = World.LoadAll().Where(w => w.Index.Region != "Johto").SelectMany(w => w.Index.Areas).ToHashSet();
        Assert.DoesNotContain(Johto.Index.Areas, others.Contains);
    }

    [Fact]
    public void ANewGameInJohtoBeginsBeforeThePlayersHouseInNewBarkTown()
    {
        var start = RegionDatabase.Get(RegionDatabase.Johto)!.Start!;
        Assert.Equal("Johto", start.Map);
        Assert.True(Overworld.IsWalkable(start.X, start.Y));
        Assert.Equal("new_bark", Overworld.AreaAt(start.X, start.Y)?.Key);
        Assert.Equal("New Bark Town", Overworld.DisplayNameAt(start.X, start.Y));
        Assert.Equal("johto/new_bark", Overworld.BgmTrackAt(start.X, start.Y));
        // The door right behind is the player's house
        Assert.Equal("NewBark_PlayersHouse_1F", Overworld.GetWarpAt(start.X, start.Y - 1)?.TargetMap);
        // and the record's two spawns stand on open ground before their rooms' doors
        foreach (var spawn in RegionDatabase.Get(RegionDatabase.Johto)!.File!.Spawns)
        {
            Assert.True(Overworld.IsWalkable(spawn.X, spawn.Y), $"spawn {spawn.Id} stands on a blocked tile");
            Assert.Equal(spawn.Room, Overworld.GetWarpAt(spawn.X, spawn.Y - 1)?.TargetMap);
        }
    }

    [Fact]
    public void TheWalkFromTheStartReachesEveryOpenAreaAndEveryRoom()
    {
        var walked = Walked.Value;
        var outside = walked.Single(kv => kv.Key.Name == "Johto").Value;
        foreach (string key in FirstAreas)
            Assert.True(outside.Any(t => Overworld.AreaAt(t.X, t.Y)?.Key == key), $"{key} can't be walked to from the start");
        foreach (var entry in Johto.Index.Maps)
            Assert.True(walked.Keys.Any(m => m.Name == entry.Name), $"nobody walks into {entry.Name}");
        // Route 31's north end, by the Dark Cave's mouth, is reached; the cave waits for its session
        Assert.Contains(outside, t => Overworld.AreaAt(t.X, t.Y)?.Key == "route_31" && t.X > 555);
        // Violet's Gym is shut until Falkner's chapter (plan 20 · J4), and Sprout Tower and Route 36 lead nowhere yet
        var gymDoor = Johto.Area("violet")!.Warps[2];
        Assert.Equal("violet_gym", gymDoor.To);
        Assert.Empty(Overworld.WarpsAt(gymDoor.X, gymDoor.Z));
        Assert.DoesNotContain(Overworld.Warps, w => w.TargetMap.StartsWith("SproutTower", StringComparison.Ordinal));
    }

    [Fact]
    public void RouteTwentyNineMeetsPidgeyByDayAndHoothootAtNight()
    {
        // HeartGold gives each slot of the grass a species for each time of day (plan 20 · J2)
        var area = Overworld.FindArea("route_29")!;
        Assert.Equal(12, area.WildEncounters.Count);
        var morning = EncounterSlots.Grass(area, new EncounterMoment { Time = TimeOfDay.Morning });
        var day = EncounterSlots.Grass(area, new EncounterMoment { Time = TimeOfDay.Day });
        var night = EncounterSlots.Grass(area, new EncounterMoment { Time = TimeOfDay.Night });
        Assert.Contains(day, s => s.SpeciesName == "Pidgey");
        Assert.DoesNotContain(day, s => s.SpeciesName == "Hoothoot");
        Assert.Contains(night, s => s.SpeciesName == "Hoothoot");
        Assert.DoesNotContain(night, s => s.SpeciesName == "Pidgey");
        Assert.Contains(morning, s => s.SpeciesName == "Pidgey");
        // Levels and weights stay the slot's own whatever the hour
        Assert.Equal(day.Select(s => (s.MinLevel, s.Weight)), night.Select(s => (s.MinLevel, s.Weight)));
        // With no moment the table is its own: the morning's
        Assert.Equal(morning.Select(s => s.SpeciesName), area.WildEncounters.Select(s => s.SpeciesName));
    }

    [Fact]
    public void SinnohsSlotsAreTheSameAllDay()
    {
        // No slot of Platinum's carries a species of its own for the day or the night: its time of day is the area's two
        foreach (var world in World.LoadAll().Where(w => w.Index.Region == "Sinnoh"))
            foreach (string key in world.Index.Areas)
                Assert.All(world.Area(key)?.Land ?? new(), s => Assert.True(s.Day == null && s.Night == null, key));
    }

    [Fact]
    public void TheRoomsAreCutToWhatTheirWaysInReachAndFurnished()
    {
        var home = BuiltMaps.Value["NewBark_PlayersHouse_1F"];
        Assert.Equal((13, 12), (home.Width, home.Height));
        // The room's floor is joined to both its ways out, and the rest of its chunk, which nobody can reach, is wall
        Assert.True(home.IsWalkable(3, 9));
        Assert.Equal(TileType.Wall, home.GetGroundTile(12, 11));
        Assert.Contains(home.Props, p => p.Type == PropType.Television);
        Assert.Contains(home.Props, p => p.Type == PropType.Fridge);
        Assert.Contains(home.Props, p => p.Type == PropType.Table);
        Assert.Contains(home.NPCs, n => n.Name == "Mom" && n.IsHealingNurse);

        var bedroom = BuiltMaps.Value["NewBark_PlayersHouse_2F"];
        Assert.Contains(bedroom.Props, p => p.Type == PropType.Bed);
        Assert.Contains(bedroom.Props, p => p.Type == PropType.Clock);

        var lab = BuiltMaps.Value["NewBark_ElmsLab"];
        Assert.Equal(InteriorStyle.Lab, lab.Interior);
        Assert.Contains(lab.Props, p => p.Type == PropType.HealingMachine);
        Assert.True(lab.Props.Count(p => p.Type == PropType.Bookshelf) >= 8);

        var center = BuiltMaps.Value["Cherrygrove_PokemonCenter_1F"];
        Assert.Equal(InteriorStyle.PokemonCenter, center.Interior);
        Assert.Contains(center.Props, p => p.Type == PropType.HealingMachine);
        Assert.Contains(center.NPCs, n => n.Name == "Nurse");
        Assert.Equal(InteriorStyle.PokeMart, BuiltMaps.Value["Cherrygrove_Mart"].Interior);
        Assert.Contains(BuiltMaps.Value["Cherrygrove_Mart"].Props, p => p.Type == PropType.StoreShelf);
    }

    [Fact]
    public void EveryModelOnJohtosChunksIsInItsCatalogue()
    {
        var unknown = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string file in Directory.GetFiles(GameDataFiles.PathOf(Path.Combine(World.Folder, "johto", "chunks")), "*.json"))
        {
            var chunk = GameDataFiles.Load<WorldChunkFile>(Path.Combine(World.Folder, "johto", "chunks", Path.GetFileName(file)));
            foreach (var prop in chunk.Props)
                if (WorldModels.Of(prop.Name, "johto") == null) unknown.Add(prop.Name);
        }
        Assert.True(unknown.Count == 0, "the catalogue doesn't know " + string.Join(", ", unknown));
        Assert.Same(WorldModels.Johto, WorldModels.Catalogue("johto"));
        // New Bark Town's houses are built as the town builds them, and its lab as a lab
        Assert.Equal((BuildingKind.Lab, (Architecture?)Architecture.Breeze), (WorldModels.Of("wk_labo", "johto")!.Kind, WorldModels.Of("wk_labo", "johto")!.Town));
        Assert.Equal(Architecture.Bayside, WorldModels.Of("yo_h01", "johto")!.Town);
        Assert.Equal(Architecture.Breeze, Overworld.ArchitectureAt(687, 402));
        // A name both games use is Sinnoh's model, and Sinnoh never looks in Johto's table
        Assert.Same(WorldModels.Of("pc"), WorldModels.Of("pc", "johto"));
        Assert.Null(WorldModels.Of("wk_labo"));
    }

    [Fact]
    public void HeartGoldsOwnBehavioursHaveTheirNames()
    {
        Assert.Equal((byte)TileBehavior.HeadbuttTree, HgBehaviours.Headbutt);
        Assert.Equal((byte)TileBehavior.Whirlpool, HgBehaviours.Whirlpool);
        Assert.Equal((byte)TileBehavior.StopSliding, HgBehaviours.StopSliding);
        Assert.Equal((byte)TileBehavior.Magma, HgBehaviours.Magma);
        Assert.Equal((byte)TileBehavior.LadderNorth, HgBehaviours.LadderNorth);
        Assert.Equal((byte)TileBehavior.LadderSouth, HgBehaviours.LadderSouth);
        Assert.Equal((byte)TileBehavior.LadderDown, HgBehaviours.LadderDown);
        // Each is told of, and waits for the session of plan 20 that opens a place using it
        foreach (var b in new[] { TileBehavior.HeadbuttTree, TileBehavior.Whirlpool, TileBehavior.Magma, TileBehavior.LadderDown })
            Assert.Equal(BehaviourSupport.Waiting, TileBehaviors.InGame(b).Support);
    }

    [Fact]
    public void JohtosTrainersBattleWithHeartGoldsPrizeAndPersonalities()
    {
        // Youngster Joey on Route 30: trainer 8 of trainers.h, of class 2 (TRAINERCLASS_YOUNGSTER, a man's), one Rattata
        // (19) at level 4 with a difficulty of 0. TrainerData_BuildParty seeds the generator with 0 + 4 + 19 + 8 = 31 and
        // runs it twice (the class's number): 31 × 1103515245 + 24691 = 4144226214 (mod 2^32), whose high half is 63235;
        // 4144226214 × 1103515245 + 24691 = 2066689569 (mod 2^32), whose high half is 31535; 31535 << 8 + 0x88 = 8073096.
        // CalcPrizeMoney: the last Pokémon's level 4 × 4 × the Youngster's 4 of sPrizeMoneyTbl = 64
        var joey = TrainerDatabase.Get("TRAINER_YOUNGSTER_JOEY", "johto")!;
        Assert.Equal("johto_youngster_joey", joey.Id);
        Assert.Equal(64, joey.PrizeMoney);
        var rattata = Assert.Single(joey.Party);
        Assert.Equal(("Rattata", 4, 8073096u), (rattata.Species, rattata.Level, rattata.Personality));
        // and he stands on Route 30 with that team
        var placed = Overworld.Everyone.Single(n => n.Key == "r30_gsboy2");
        Assert.Equal("johto_youngster_joey", placed.TrainerData?.Id);
        Assert.True(placed.IsTrainer);

        // A woman's Pokémon end in 0x78 (TRAINER_FEMALE): Lass, class 3
        Assert.Equal(new[] { false, true, false, true }, HgssTrainers.ClassWomen("static const u8 sTrainerGenders[] = { TRAINER_MALE, TRAINER_FEMALE, TRAINER_DOUBLE, TRAINER_FEMALE, };"));
        uint gender = 0x78;
        Assert.Equal(unchecked((31535u << 8) + 0x78u), HgssTrainers.Personality(0, 4, 19, 8, 2, ref gender));
        // An ability asked for sets the lowest bit, and keeps it for the Pokémon after
        HgssTrainers.Override("TRPOKE_GENDER_OVERRIDE_OFF", "TRPOKE_ABILITY_OVERRIDE_SECOND", 0, ref gender);
        Assert.Equal(0x79u, gender);
    }

    [Fact]
    public void AHiddenItemHoldsWhatHeartGoldsTableSays()
    {
        // HIDDENITEM_R29_R30_T21_NUGGET (1) is the row of sHiddenItemParam whose flag is 1: a Nugget
        var nugget = Overworld.HiddenItems[(572, 390)];
        Assert.Equal(("Nugget", 1, "FLAG_HIDDEN_ITEM_R29_R30_T21_NUGGET"), (nugget.Item, nugget.Count, nugget.Flag));
        Assert.Equal("Potion", Overworld.HiddenItems[(682, 391)].Item);
    }
}
