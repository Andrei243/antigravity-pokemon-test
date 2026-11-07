using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Kalos after its Hall of Fame (plan 23 · Z4b): Lumiose's last two gates (Lumiose Station and its TMV, Azure Bay and Sea
/// Spirit's Den), the neighbour in Kiloude City, the Mega Ring upgraded by Anistar's sundial and the fourteen Mega Stones,
/// the roaming bird by the starter and its rest in the den, Terminus Cave's depths and Zygarde, the Unknown Dungeon and
/// Mewtwo, and Looker's episode, played headless.
/// </summary>
public class KalosPostGameTests
{
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Kalos"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);
    private static readonly Lazy<Dictionary<Map, HashSet<(int X, int Y)>>> Walked =
        new(() => WorldWalk.From(MapNamed, RegionDatabase.Get(RegionDatabase.Kalos)!.Start!));

    private static World Kalos => LoadedWorld.Value;
    private static Map Overworld => BuiltMaps.Value["Kalos"];
    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : Rooms.Value(name);

    private static readonly (string Key, int Low, int High)[] Places =
    {
        ("azure_bay", 58, 61), ("terminus_cave_depths", 60, 63), ("unknown_dungeon", 64, 66)
    };

    private static readonly string[] NewRooms = { "LumioseCity_Station", "LumioseCity_LookerBureau", "LumioseCity_Museum", "LumioseCity_HotelRichissime" };

    /// <summary>X and Y's post-game stones, where they lie (an overworld area or a cave by its map, or a room) and on which tile.</summary>
    private static readonly (string Stone, string Map, int X, int Y)[] Stones =
    {
        ("Heracronite", "Kalos", 242, 564), ("Pinsirite", "Kalos", 277, 564), ("Houndoominite", "Kalos", 383, 239),
        ("Manectite", "Kalos", 432, 239), ("Gyaradosite", "Kalos", 659, 340), ("Medichamite", "Kalos", 328, 180),
        ("Alakazite", "ReflectionCave", 36, 8), ("Kangaskhanite", "GlitteringCave", 39, 7), ("Garchompite", "KalosVictoryRoad", 55, 8),
        ("Scizorite", "FrostCavern", 55, 25), ("Banettite", "LostHotel", 53, 24), ("Aggronite", "CyllageCity_Gym", 1, 16),
        ("Tyranitarite", "CyllageCity_Gym", 11, 16), ("Mawilite", "CamphrierTown_Castle", 13, 10)
    };

    [Fact]
    public void TheNewPlacesAreOpenWithSongsWildPokemonAndTrainersOfTheTable()
    {
        foreach (var (key, low, high) in Places)
        {
            Assert.Contains(key, Kalos.Index.Areas);
            Assert.False(string.IsNullOrEmpty(Kalos.Overlay(key)?.BgmTrack), $"{key} has no song");
            var land = Kalos.Area(key)!.Land!;
            Assert.Equal(12, land.Count);
            foreach (var slot in land)
            {
                var dex = PokemonDatabase.Get(slot.Species)!.DexNumbers;
                Assert.True(dex.Keys.Any(k => k.StartsWith("kalos-", StringComparison.Ordinal)), $"{key}: {slot.Species} is no Kalos species");
                Assert.InRange(slot.Level, low, high);
            }
            foreach (var o in Kalos.Area(key)!.Objects.Where(o => o.Script.StartsWith("TRAINER_", StringComparison.Ordinal)))
            {
                var trainer = TrainerDatabase.Get("kalos_" + o.Script["TRAINER_".Length..].ToLowerInvariant());
                Assert.NotNull(trainer);
                Assert.All(trainer!.Party, p => Assert.NotNull(PokemonDatabase.Get(p.Species)));
            }
        }
        Assert.Contains("sea_spirits_den", Kalos.Index.Areas);
        foreach (string map in new[] { "TerminusCaveDepths", "UnknownDungeon", "SeaSpiritsDen" })
        {
            Assert.True(BuiltMaps.Value[map].IsStreamed);
            Assert.Contains(map, RegionDatabase.Get(RegionDatabase.Kalos)!.Maps);
        }
        foreach (string room in NewRooms) Assert.Contains(room, RegionDatabase.Get(RegionDatabase.Kalos)!.Maps);
        Assert.Equal("Azure Bay", Overworld.DisplayNameAt(420, 450));
        // The station's square is Lumiose's own
        Assert.Equal("Lumiose City", Overworld.DisplayNameAt(433, 360));
        // Every trainer of Looker's episode and the neighbour's three Kiloude teams are of the table
        foreach (string id in new[]
                 {
                     "kalos_rival_kiloude_chespin", "kalos_rival_kiloude_fennekin", "kalos_rival_kiloude_froakie",
                     "kalos_preschooler_nanette", "kalos_preschooler_lou", "kalos_school_kid_cerise", "kalos_school_kid_bastien",
                     "kalos_gang_rouille", "kalos_gang_brutus", "kalos_gang_fauve", "kalos_suspect_vesper", "kalos_suspect_pip",
                     "kalos_suspect_odile", "kalos_butler_anatole", "kalos_malva_looker", "kalos_scientist_clothilde",
                     "kalos_scientist_ernest", "kalos_essentia_1", "kalos_essentia_2", "kalos_essentia_3", "kalos_essentia_rematch"
                 })
            Assert.NotNull(TrainerDatabase.Get(id));
    }

    [Fact]
    public void TheMouthsDoorsAndWaysOutLandOnOpenGround()
    {
        foreach (var (map, x, y, target) in new[]
                 {
                     ("Kalos", 419, 476, "SeaSpiritsDen"), ("Kalos", 698, 107, "UnknownDungeon"), ("TerminusCave", 31, 9, "TerminusCaveDepths"),
                     ("Kalos", 433, 351, "LumioseCity_Station"), ("Kalos", 398, 377, "LumioseCity_LookerBureau"),
                     ("Kalos", 370, 317, "LumioseCity_Museum"), ("Kalos", 387, 357, "LumioseCity_HotelRichissime")
                 })
        {
            var from = MapNamed(map);
            var warp = from.GetWarpAt(x, y);
            Assert.Equal(target, warp?.TargetMap);
            var inside = MapNamed(target);
            Assert.True(inside.IsWalkable(warp!.TargetX, warp.TargetY), $"{target}: the way in lands on a blocked tile");
            var back = inside.Warps.Single(w => w.TargetMap == map);
            Assert.True(from.IsWalkable(back.TargetX, back.TargetY), $"{target}: the way out lands on {back.TargetX},{back.TargetY} of {map}, blocked ({from.GetNpcAt(back.TargetX, back.TargetY)?.Name ?? "no one"})");
        }
    }

    [Fact]
    public void FromTheBedroomTheWayReachesEveryNewPlaceAndEveryChamber()
    {
        var walked = Walked.Value;
        bool Reached(string map, int x, int y) => walked.Any(p => p.Key.Name == map && p.Value.Contains((x, y)));
        foreach (var (x, y, what) in new[]
                 {
                     (433, 360, "the station's square"), (392, 425, "the shore road"), (420, 430, "the meadow by the bay"),
                     (420, 448, "the beach"), (420, 460, "the sandbar"), (419, 475, "the island before the den")
                 })
            Assert.True(Reached("Kalos", x, y), what);
        foreach (var (map, x, y) in new[]
                 {
                     ("TerminusCaveDepths", 31, 52), ("TerminusCaveDepths", 24, 42), ("TerminusCaveDepths", 45, 34),
                     ("TerminusCaveDepths", 30, 25), ("TerminusCaveDepths", 15, 16), ("TerminusCaveDepths", 31, 10),
                     ("UnknownDungeon", 31, 52), ("UnknownDungeon", 20, 44), ("UnknownDungeon", 12, 35), ("UnknownDungeon", 30, 25),
                     ("UnknownDungeon", 50, 16), ("UnknownDungeon", 40, 10), ("UnknownDungeon", 25, 10),
                     ("SeaSpiritsDen", 15, 20), ("SeaSpiritsDen", 12, 12), ("SeaSpiritsDen", 15, 11)
                 })
            Assert.True(Reached(map, x, y), $"{map} {x},{y}");
        foreach (string room in NewRooms) Assert.Contains(walked, p => p.Key.Name == room);
        // Every trainer and item of the new places stands where the walk comes
        foreach (var (key, mapName) in new[] { ("azure_bay", "Kalos"), ("terminus_cave_depths", "TerminusCaveDepths"), ("unknown_dungeon", "UnknownDungeon") })
            foreach (var o in Kalos.Area(key)!.Objects)
                Assert.True(walked.Any(p => p.Key.Name == mapName && Enumerable.Range(-1, 3).Any(dx => Enumerable.Range(-1, 3).Any(dz => p.Value.Contains((o.X + dx, o.Z + dz))))),
                    $"{key}: {o.Id} at {o.X},{o.Z} is out of the way");
        // ...and every Mega Stone
        foreach (var (stone, map, x, y) in Stones)
            Assert.True(walked.Any(p => p.Key.Name == map && Enumerable.Range(-1, 3).Any(dx => Enumerable.Range(-1, 3).Any(dz => p.Value.Contains((x + dx, y + dz))))),
                $"{stone} at {map} {x},{y} is out of the way");
    }

    [Fact]
    public void LumiosesLastTwoGatesOpenWithTheHallOfFame()
    {
        // Nothing turns the player back at the east or the south-east gate once the roads are at 3
        foreach (var (x, y) in new[] { (418, 358), (391, 418) })
        {
            Assert.Contains(Overworld.TriggersAt(x, y), t => t.Value == 2);
            Assert.DoesNotContain(Overworld.TriggersAt(x, y), t => t.Value == 3);
        }
        var story = new StoryState();
        Play(story, "KalosLeague_HallOfFame", "OnEnter", "KalosLeague_HallOfFame", before: s => s.Set("FLAG_KALOS_DEFEATED_DIANTHA"));
        Assert.Equal(3, story.Var("VAR_KALOS_LUMIOSE_ROADS"));
        Assert.Equal("kalosStation", Objectives.Current(story, RegionDatabase.Kalos)?.Name);
        // A save from before Z4b, past the Hall of Fame with the roads at 2, finds them open as it comes into the city
        var older = new StoryState();
        older.Set("KalosHallOfFame");
        older.Set("FLAG_KALOS_LUMIOSE_BEGUN");
        older.SetVar("VAR_KALOS_LUMIOSE_ROADS", 2);
        Play(older, "lumiose_city", "OnEnter", "Kalos");
        Assert.Equal(3, older.Var("VAR_KALOS_LUMIOSE_ROADS"));
        // The bureau's door waits for the Mega Ring, the museum's and the hotel's for the Hall of Fame
        var city = Kalos.BuildMaps().Single(m => m.Name == "Kalos");
        var fresh = new StoryState();
        city.ApplyDoors(fresh.Has);
        Assert.True(city.GetWarpAt(398, 377) == null || city.IsSolid(398, 377));
        fresh.Set("KalosHallOfFame");
        fresh.Set("FLAG_KALOS_MEGA_RING_UPGRADED");
        city.ApplyDoors(fresh.Has);
        Assert.Equal("LumioseCity_LookerBureau", city.GetWarpAt(398, 377)?.TargetMap);
        Assert.Equal("LumioseCity_Museum", city.GetWarpAt(370, 317)?.TargetMap);
    }

    [Fact]
    public void TheProfessorGivesTheTmvPassAtTheStationAndTheTrainRunsBothWays()
    {
        var story = new StoryState();
        // Before the Hall of Fame the station is quiet and the train is for pass holders
        Play(story, "LumioseCity_Station", "OnEnter", "LumioseCity_Station");
        Assert.False(story.Has("FLAG_KALOS_TMV_PASS"));
        Assert.DoesNotContain(Play(story, "LumioseCity_Station", "Tmv", "LumioseCity_Station", "TMV Attendant").Log, l => l.StartsWith("warp", StringComparison.Ordinal));
        story.Set("KalosHallOfFame");
        Play(story, "LumioseCity_Station", "OnEnter", "LumioseCity_Station");
        Assert.True(story.Has("FLAG_KALOS_TMV_PASS"));
        Assert.Equal(1, story.Var("VAR_KALOS_POSTGAME"));
        Assert.Equal("kalosKiloude", Objectives.Current(story, RegionDatabase.Kalos)?.Name);
        // ...once only
        var again = Play(story, "LumioseCity_Station", "OnEnter", "LumioseCity_Station");
        Assert.Empty(again.Transcript);
        Assert.Contains("warp Kalos 538 355", Play(story, "LumioseCity_Station", "Tmv", "LumioseCity_Station", "TMV Attendant").Log);
        Assert.Contains("warp LumioseCity_Station 12 5", Play(story, "kiloude_city", "TmvBack", "Kalos", "TMV Attendant").Log);
        // The attendant's tile in Kiloude and the arrival beside it are open ground
        Assert.True(Overworld.IsWalkable(538, 355));
        Assert.True(MapNamed("LumioseCity_Station").IsWalkable(12, 5));
    }

    [Fact]
    public void TheNeighbourInKiloudeGivesTheAbsoliteOnceAndBattlesOnceADay()
    {
        foreach (var (starter, team) in new[] { ("Chespin", "fennekin"), ("Fennekin", "froakie"), ("Froakie", "chespin") })
        {
            var story = new StoryState();
            story.ChooseStarter(starter);
            story.Set("KalosHallOfFame");
            story.Set("FLAG_KALOS_TMV_PASS");
            story.SetVar("VAR_KALOS_POSTGAME", 1);
            var bag = new Inventory();
            var first = Play(story, "kiloude_city", "RivalKiloude", "Kalos", "{rival}", bag: bag);
            Assert.Contains(first.Log, l => l.Contains($"kalos_rival_kiloude_{team}", StringComparison.Ordinal));
            Assert.True(story.Has("FLAG_KALOS_RIVAL_KILOUDE_BEATEN"));
            Assert.Equal(2, story.Var("VAR_KALOS_POSTGAME"));
            Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Absolite")!));
            // The same day: no second battle
            var sameDay = Play(story, "kiloude_city", "RivalKiloude", "Kalos", "{rival}", bag: bag);
            Assert.DoesNotContain(sameDay.Log, l => l.StartsWith("battle", StringComparison.Ordinal));
            // The next day: a battle, and no second stone
            story.Unset("FLAG_DAILY_KALOS_RIVAL_KILOUDE");
            var nextDay = Play(story, "kiloude_city", "RivalKiloude", "Kalos", "{rival}", bag: bag);
            Assert.Contains(nextDay.Log, l => l.Contains($"kalos_rival_kiloude_{team}", StringComparison.Ordinal));
            Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Absolite")!));
            Assert.True(story.Has("FLAG_DAILY_KALOS_RIVAL_KILOUDE"));
        }
        // A loss the first time gives nothing and lets the player come back
        var lost = new StoryState();
        lost.ChooseStarter("Froakie");
        lost.Set("FLAG_KALOS_TMV_PASS");
        lost.SetVar("VAR_KALOS_POSTGAME", 1);
        var empty = new Inventory();
        Play(lost, "kiloude_city", "RivalKiloude", "Kalos", "{rival}", bag: empty, fight: _ => BattleOutcome.Lost);
        Assert.False(lost.Has("FLAG_KALOS_RIVAL_KILOUDE_BEATEN"));
        Assert.Equal(0, empty.GetQuantity(ItemDatabase.Get("Absolite")!));
    }

    [Fact]
    public void TheSundialUpgradesTheMegaRingAndTheFourteenStonesLieWhereTheyAre()
    {
        var story = new StoryState();
        story.Set("KalosHallOfFame");
        story.SetVar("VAR_KALOS_STORY", 31);
        story.Set("FLAG_KALOS_RIVAL_KILOUDE_BEATEN");
        story.SetVar("VAR_KALOS_POSTGAME", 2);
        // The professor stands by the sundial only once the neighbour is beaten, and goes once the ring is upgraded
        var anistar = Kalos.BuildMaps().Single(m => m.Name == "Kalos");
        anistar.ApplyPresence(new StoryState().Has);
        Assert.DoesNotContain(anistar.NPCs, n => n.Key == "sycamore_sundial");
        anistar.ApplyPresence(story.Has);
        Assert.Contains(anistar.NPCs, n => n.Key == "sycamore_sundial" && (n.GridX, n.GridY) == (649, 219));
        Assert.Equal("kalosSundial", Objectives.Current(story, RegionDatabase.Kalos)?.Name);
        Play(story, "anistar_city", "Sundial", "Kalos", "Prof. Sycamore");
        Assert.True(story.Has("FLAG_KALOS_MEGA_RING_UPGRADED"));
        Assert.Equal(3, story.Var("VAR_KALOS_POSTGAME"));
        Assert.Equal("kalosLooker", Objectives.Current(story, RegionDatabase.Kalos)?.Name);

        var before = new StoryState();
        before.Set("KalosHallOfFame");
        var maps = Kalos.BuildMaps().ToDictionary(m => m.Name);
        Map Of(string name) => maps.TryGetValue(name, out var m) ? m : MapDatabase.Fresh()(name);
        foreach (var (stone, mapName, x, y) in Stones)
        {
            var map = Of(mapName);
            Assert.True(map.IsWalkable(x, y) || map.GetNpcAt(x, y) != null, $"{stone} lies on a blocked tile of {mapName}");
            map.ApplyPresence(before.Has);
            Assert.DoesNotContain(map.NPCs, n => n.IsItemBall && n.Item == stone);
            map.ApplyPresence(story.Has);
            var ball = map.NPCs.Single(n => n.IsItemBall && n.Item == stone);
            Assert.Equal((x, y), (ball.GridX, ball.GridY));
            // ...and once picked up, gone for good
            story.Set(ball.HiddenBy!);
            map.ApplyPresence(story.Has);
            Assert.DoesNotContain(map.NPCs, n => n.IsItemBall && n.Item == stone);
            story.Unset(ball.HiddenBy!);
        }
        Assert.Equal(14, Stones.Select(s => s.Stone).Distinct().Count());
    }

    [Theory]
    [InlineData("Chespin", "Articuno")]
    [InlineData("Fennekin", "Zapdos")]
    [InlineData("Froakie", "Moltres")]
    public void TheBirdOfTheFirstPokemonsChoiceIsLooseOverKalosAfterTheHallOfFame(string starter, string bird)
    {
        var story = new StoryState();
        story.ChooseStarter(starter);
        story.SetVar("VAR_KALOS_STORY", 31);
        var host = Play(story, "vaniville_town", "OnEnter", "Kalos", tile: (242, 722));
        int kalosSlot = Roamers.SlotOf(bird, "kalos")!.Value;
        Assert.False(host.Encounters.Roamers[kalosSlot].Active);
        story.Set("KalosHallOfFame");
        host = Play(story, "vaniville_town", "OnEnter", "Kalos", tile: (242, 722));
        var roamer = host.Encounters.Roamers[kalosSlot];
        Assert.True(roamer.Active);
        Assert.Equal(bird, roamer.Species);
        Assert.Equal(70, roamer.Level);
        Assert.Equal("kalos", Roamers.RegionOf(kalosSlot));
        Assert.Contains(Roamers.PlaceOf(roamer), Roamers.KalosRoutes);
        Assert.True(story.Has("FLAG_KALOS_BIRD_LOOSE"));
        // Only that one, and Sinnoh's bird of the same name is untouched
        Assert.Single(host.Encounters.Roamers, r => r.Active);
        Assert.False(host.Encounters.Roamers[Roamers.SlotOf(bird)!.Value].Active);
        // Out of the door again: nothing more
        var again = Play(story, "vaniville_town", "OnEnter", "Kalos", tile: (242, 722));
        Assert.DoesNotContain(again.Log, l => l.StartsWith("cry", StringComparison.Ordinal));
    }

    [Fact]
    public void KalossRoutesForTheBirdHaveGrassAndNeighboursThatAreKalossOwn()
    {
        foreach (string key in Roamers.KalosRoutes)
        {
            Assert.Contains(key, Kalos.Index.Areas);
            Assert.NotEmpty(Kalos.Area(key)!.Land!);
        }
        // Each route's neighbours move the bird about Kalos alone
        var state = SpecialEncounters.NewGame(new Random(3));
        int slot = Roamers.SlotOf("Zapdos", "kalos")!.Value;
        Roamers.SetLoose(state, slot, new Random(5));
        var rng = new Random(7);
        for (int i = 0; i < 200; i++)
        {
            Roamers.PlayerWalkedInto(state, Roamers.KalosRoutes[i % Roamers.KalosRoutes.Length], rng);
            Assert.Contains(Roamers.PlaceOf(state.Roamers[slot]), Roamers.KalosRoutes);
        }
    }

    [Fact]
    public void TheBirdSettlesInSeaSpiritsDenAfterFleeingADozenTimesAndIsMetThere()
    {
        var story = new StoryState();
        var state = SpecialEncounters.NewGame(new Random(1));
        int slot = Roamers.SlotOf("Articuno", "kalos")!.Value;
        Roamers.SetLoose(state, slot, new Random(2));
        var foe = Roamers.ToBattle(state.Roamers[slot]);
        var rng = new Random(4);
        for (int i = 1; i <= 11; i++)
        {
            Roamers.AfterBattle(state, foe, won: false, caught: false, Roamers.PlaceOf(state.Roamers[slot]), story, rng);
            Assert.True(state.Roamers[slot].Active);
            Assert.Equal(i, story.Var(Roamers.FledVariable(slot)));
        }
        Roamers.AfterBattle(state, foe, won: false, caught: false, Roamers.PlaceOf(state.Roamers[slot]), story, rng);
        Assert.False(state.Roamers[slot].Active);
        Assert.Equal(Roamers.Resting, story.Var("VAR_ROAMING_KALOS_ARTICUNO_STATE"));
        // Sinnoh's three never settle
        Assert.Equal(0, Roamers.RestsAfter(Roamers.SlotOf("Articuno")!.Value));

        // The den shows the one at rest, and it is met there once
        var den = Kalos.BuildMaps().Single(m => m.Name == "SeaSpiritsDen");
        den.ApplyPresence(story.Has);
        Assert.DoesNotContain(den.NPCs, n => n.Species == "Articuno");
        var host = Play(story, "sea_spirits_den", "OnEnter", den);
        den.ApplyPresence(story.Has);
        var articuno = den.NPCs.Single(n => n.Species == "Articuno");
        Assert.DoesNotContain(den.NPCs, n => n.Species is "Zapdos" or "Moltres");
        host = Play(story, "sea_spirits_den", "Bird", den, articuno);
        Assert.Contains("wildbattle Articuno 70 Won", host.Log);
        Assert.Equal(Roamers.Defeated, story.Var("VAR_ROAMING_KALOS_ARTICUNO_STATE"));
        Assert.True(story.Has("FLAG_KALOS_DEN_DONE"));
        den.ApplyPresence(story.Has);
        Assert.DoesNotContain(den.NPCs, n => n.Species == "Articuno");
    }

    [Fact]
    public void ZygardeSleepsInTheDepthsAndMewtwoWaitsInTheUnknownDungeonEachMetOnce()
    {
        var story = new StoryState();
        story.Set("KalosHallOfFame");
        var bag = new Inventory();
        // Run from, Zygarde stays
        var depths = Kalos.BuildMaps().Single(m => m.Name == "TerminusCaveDepths");
        depths.ApplyPresence(story.Has);
        var zygarde = depths.NPCs.Single(n => n.Species == "Zygarde");
        Assert.Equal((31, 8), (zygarde.GridX, zygarde.GridY));
        var fled = Play(story, "terminus_cave_depths", "Zygarde", depths, zygarde, fight: _ => BattleOutcome.Fled);
        Assert.Contains("wildbattle Zygarde 70 Fled", fled.Log);
        Assert.False(story.Has("FLAG_KALOS_ZYGARDE_GONE"));
        Play(story, "terminus_cave_depths", "Zygarde", depths, zygarde, fight: _ => BattleOutcome.Caught);
        Assert.True(story.Has("FLAG_KALOS_ZYGARDE_GONE"));
        // Mewtwo, and the two stones after it
        var dungeon = Kalos.BuildMaps().Single(m => m.Name == "UnknownDungeon");
        dungeon.ApplyPresence(story.Has);
        var mewtwo = dungeon.NPCs.Single(n => n.Species == "Mewtwo");
        var met = Play(story, "unknown_dungeon", "Mewtwo", dungeon, mewtwo, bag: bag);
        Assert.Contains("wildbattle Mewtwo 70 Won", met.Log);
        Assert.True(story.Has("FLAG_KALOS_MEWTWO_GONE"));
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Mewtwonite X")!));
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Mewtwonite Y")!));
        dungeon.ApplyPresence(story.Has);
        Assert.DoesNotContain(dungeon.NPCs, n => n.Species == "Mewtwo");
        // The ways to them are kept until the Hall of Fame
        var before = new StoryState();
        var cave = Kalos.BuildMaps().Single(m => m.Name == "TerminusCave");
        cave.ApplyPresence(before.Has);
        Assert.Contains(cave.NPCs, n => n.Key == "worker" && (n.GridX, n.GridY) == (31, 17));
        cave.ApplyPresence(story.Has);
        Assert.DoesNotContain(cave.NPCs, n => n.Key == "worker");
        var village = Kalos.BuildMaps().Single(m => m.Name == "Kalos");
        village.ApplyPresence(before.Has);
        Assert.Contains(village.NPCs, n => n.Key == "guardian" && (n.GridX, n.GridY) == (698, 107));
        village.ApplyPresence(story.Has);
        Assert.DoesNotContain(village.NPCs, n => n.Key == "guardian");
    }

    [Fact]
    public void LookersEpisodePlaysHeadlessFromHisFirstCaseToHisFarewellAndEssentiaTrainsOnHerDays()
    {
        var story = new StoryState();
        story.Set("KalosHallOfFame");
        story.SetVar("VAR_KALOS_STORY", 31);
        story.Set("FLAG_KALOS_TMV_PASS");
        story.Set("FLAG_KALOS_RIVAL_KILOUDE_BEATEN");
        story.Set("FLAG_KALOS_MEGA_RING_UPGRADED");
        story.SetVar("VAR_KALOS_POSTGAME", 3);
        // Fresh copies of the maps, since the scripts hide and show their people
        var built = Kalos.BuildMaps().ToDictionary(m => m.Name);
        var rooms = MapDatabase.Fresh();
        var cache = new Dictionary<string, Map>();
        Map Fresh(string name) => cache.TryGetValue(name, out var m) ? m : cache[name] = built.TryGetValue(name, out var b) ? b : rooms(name);
        HeadlessScriptHost Do(string file, string script, string map, string? person = null, DayOfWeek day = DayOfWeek.Monday)
        {
            var at = Fresh(map);
            // (coming to the map again: whoever a script hid or showed is as the flags say)
            at.ForgetForced();
            at.ApplyPresence(story.Has);
            var who = person == null ? null : at.NPCs.Single(n => n.Key == person);
            return Play(story, file, script, at, who, mapNamed: Fresh, day: day);
        }
        int Case() => story.Var("VAR_KALOS_LOOKER");

        // The first case: the three tickets
        Do("LumioseCity_LookerBureau", "OnEnter", "LumioseCity_LookerBureau");
        Assert.Equal(1, Case());
        Do("LumioseCity_LookerBureau", "Looker", "LumioseCity_LookerBureau", "looker");
        Assert.Equal(1, Case());
        foreach (string room in new[] { "LumioseCity_Station", "LumioseCity_Museum", "LumioseCity_HotelRichissime" })
            Do(room, "Ticket", room);
        Do("LumioseCity_LookerBureau", "Looker", "LumioseCity_LookerBureau", "looker");
        Assert.Equal(2, Case());
        // The second: the park's children, then Emma and her Espurr (the first two answers are wrong)
        var early = Do("lumiose_city", "AlleyEmma", "Kalos", "alley_emma");
        Assert.False(story.Has("FLAG_KALOS_EMMA_BUREAU"));
        foreach (string child in new[] { "Nanette", "Lou", "Cerise", "Bastien" })
            Do("lumiose_city", "Alley" + child, "Kalos", "alley_" + child.ToLowerInvariant());
        Do("lumiose_city", "AlleyEmma", "Kalos", "alley_emma");
        Assert.True(story.Has("FLAG_KALOS_EMMA_BUREAU"));
        Assert.True(story.Has("FLAG_KALOS_LOOKER_ALLEY_DONE"));
        Do("LumioseCity_LookerBureau", "Looker", "LumioseCity_LookerBureau", "looker");
        Assert.Equal(3, Case());
        // The third: the gang at the station
        foreach (string gang in new[] { "Rouille", "Brutus", "Fauve" })
            Do("LumioseCity_Station", "Gang" + gang, "LumioseCity_Station", "gang_" + gang.ToLowerInvariant());
        Do("LumioseCity_Station", "Tourist", "LumioseCity_Station", "tourist");
        Assert.True(story.Has("FLAG_KALOS_LOOKER_GANG_DONE"));
        Assert.False(story.Has("FLAG_KALOS_EMMA_AWAY"));
        Do("LumioseCity_LookerBureau", "Looker", "LumioseCity_LookerBureau", "looker");
        Assert.Equal(4, Case());
        // The fourth: the museum's curator, the three suspects, and the butler at the bureau
        Do("LumioseCity_Museum", "Curator", "LumioseCity_Museum", "curator");
        Assert.True(story.Has("FLAG_KALOS_LOOKER_STING"));
        foreach (var (script, person) in new[] { ("SuspectWoman", "suspect_woman"), ("SuspectChild", "suspect_child"), ("SuspectLady", "suspect_lady") })
            Do("lumiose_city", script, "Kalos", person);
        Do("LumioseCity_LookerBureau", "OnEnter", "LumioseCity_LookerBureau");
        Assert.True(story.Has("FLAG_KALOS_LOOKER_BUTLER"));
        Assert.True(story.Has("FLAG_KALOS_EMMA_AWAY"));
        Do("LumioseCity_LookerBureau", "Butler", "LumioseCity_LookerBureau", "butler");
        Assert.Equal(5, Case());
        // The fifth: Malva at the hotel, then the laboratory under the café
        Do("LumioseCity_HotelRichissime", "Malva", "LumioseCity_HotelRichissime", "malva");
        Assert.True(story.Has("FLAG_KALOS_LOOKER_LABS"));
        Assert.Contains("warp LumioseCity_LysandreLabs 8 14", Do("LumioseCity_LysandreCafe", "Waiter", "LumioseCity_LysandreCafe", "waiter").Log);
        var waiting = Do("LumioseCity_LysandreLabs", "Essentia", "LumioseCity_LysandreLabs", "essentia");
        Assert.DoesNotContain(waiting.Log, l => l.StartsWith("battle", StringComparison.Ordinal));
        Do("LumioseCity_LysandreLabs", "ScientistClothilde", "LumioseCity_LysandreLabs", "scientist_clothilde");
        Do("LumioseCity_LysandreLabs", "ScientistErnest", "LumioseCity_LysandreLabs", "scientist_ernest");
        var essentia = Do("LumioseCity_LysandreLabs", "Essentia", "LumioseCity_LysandreLabs", "essentia");
        Assert.Equal(3, essentia.Log.Count(l => l.StartsWith("battle", StringComparison.Ordinal)));
        Do("LumioseCity_LysandreLabs", "Xerosic", "LumioseCity_LysandreLabs", "xerosic");
        Assert.Equal(6, Case());
        Assert.True(story.Has("FLAG_KALOS_LOOKER_FAREWELL"));
        // The farewell: Emma's letter, and Looker at the museum
        Do("LumioseCity_LookerBureau", "Emma", "LumioseCity_LookerBureau", "emma");
        Do("LumioseCity_Museum", "LookerFarewell", "LumioseCity_Museum", "looker_farewell");
        Assert.Equal(7, Case());
        Assert.True(story.Has("FLAG_KALOS_LOOKER_LEFT"));
        Assert.Equal("kalosFree", Objectives.Current(story, RegionDatabase.Kalos)?.Name);
        // Looker has gone from the bureau and the museum, and Emma stays
        var bureau = Fresh("LumioseCity_LookerBureau");
        bureau.ApplyPresence(story.Has);
        Assert.DoesNotContain(bureau.NPCs, n => n.Key == "looker");
        Assert.Contains(bureau.NPCs, n => n.Key == "emma");
        // Essentia battles on her days only, once a day
        Assert.DoesNotContain(Do("LumioseCity_LookerBureau", "Emma", "LumioseCity_LookerBureau", "emma", DayOfWeek.Monday).Log, l => l.StartsWith("battle", StringComparison.Ordinal));
        Assert.Contains(Do("LumioseCity_LookerBureau", "Emma", "LumioseCity_LookerBureau", "emma", DayOfWeek.Tuesday).Log, l => l.Contains("kalos_essentia_rematch", StringComparison.Ordinal));
        Assert.DoesNotContain(Do("LumioseCity_LookerBureau", "Emma", "LumioseCity_LookerBureau", "emma", DayOfWeek.Tuesday).Log, l => l.StartsWith("battle", StringComparison.Ordinal));
        story.Unset("FLAG_DAILY_KALOS_ESSENTIA");
        Assert.Contains(Do("LumioseCity_LookerBureau", "Emma", "LumioseCity_LookerBureau", "emma", DayOfWeek.Saturday).Log, l => l.Contains("kalos_essentia_rematch", StringComparison.Ordinal));
        Assert.NotNull(early);
    }

    [Fact]
    public void KalossObjectivesFollowThePostGameAndEndFree()
    {
        var story = new StoryState();
        story.SetVar("VAR_KALOS_STORY", 31);
        string? Next() => Objectives.Current(story, RegionDatabase.Kalos)?.Name;
        Assert.Equal("kalosStation", Next());
        foreach (var (value, name) in new[] { (1, "kalosKiloude"), (2, "kalosSundial"), (3, "kalosLooker") })
        {
            story.SetVar("VAR_KALOS_POSTGAME", value);
            Assert.Equal(name, Next());
        }
        story.SetVar("VAR_KALOS_LOOKER", 7);
        Assert.Equal("kalosFree", Next());
    }

    [Fact]
    public void ARoamersSlotForKalosIsNamedByTheScriptsWordAndKeepsAVariableOfItsOwn()
    {
        var parsed = ScriptLibrary.FromSources(("test_kalos_bird", "script Loose\n  roamer start \"Moltres\" kalos\n  end\n"));
        Assert.Empty(parsed.Problems());
        int slot = Roamers.SlotOf("Moltres", "kalos")!.Value;
        Assert.Equal("VAR_ROAMING_KALOS_MOLTRES_STATE", Roamers.StateVariable(slot));
        Assert.Equal("VAR_ROAMING_MOLTRES_STATE", Roamers.StateVariable(Roamers.SlotOf("Moltres")!.Value));
        Assert.Equal(12, Roamers.RestsAfter(slot));
    }

    // ------------------------------------------------------------------ helpers

    private static HeadlessScriptHost Play(StoryState story, string file, string script, string map, string? person = null,
        Inventory? bag = null, Func<NPC?, BattleOutcome>? fight = null, (int X, int Y)? tile = null)
    {
        // The maps are shared by the tests: each play comes to its map afresh, as the player would, by the story's flags
        var at = MapNamed(map);
        at.ForgetForced();
        at.ApplyPresence(story.Has);
        var who = person == null ? null : at.Everyone.First(n => n.Name == person);
        return Play(story, file, script, at, who, bag: bag, fight: fight, tile: tile);
    }

    private static HeadlessScriptHost Play(StoryState story, string file, string script, Map at, NPC? who = null,
        Inventory? bag = null, Func<NPC?, BattleOutcome>? fight = null, (int X, int Y)? tile = null,
        Func<string, Map?>? mapNamed = null, DayOfWeek day = DayOfWeek.Monday, Action<StoryState>? before = null)
    {
        before?.Invoke(story);
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Greninja")!, 80));
        var host = new HeadlessScriptHost(story, party, bag ?? new Inventory())
        {
            Map = at, Region = RegionDatabase.Kalos, Fight = fight, MapNamed = mapNamed ?? (n => MapNamed(n)), Weekday = day
        };
        if (tile is { } t) host.PlayerTile = t;
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, file) ?? throw new Xunit.Sdk.XunitException($"{file}.{script} is missing"), who);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    private static HeadlessScriptHost Play(StoryState story, string file, string script, string map, Action<StoryState> before) =>
        Play(story, file, script, MapNamed(map), before: before);
}
