using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Kalos's loose ends (plan 23 · Z4 afterwards): Route 16 and the Lost Hotel off Lumiose's north-east gate, Frost Cavern off
/// Route 17, Terminus Cave off Route 13, Kiloude City west of Couriway Town and AZ on its landing after the Hall of Fame.
/// </summary>
public class KalosLooseEndsTests
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
        ("kalos_route_16", 33, 35), ("lost_hotel", 36, 38), ("frost_cavern", 38, 41), ("terminus_cave", 30, 32)
    };

    [Fact]
    public void TheFivePlacesAreOpenWithSongsWildPokemonAndTrainersOfTheTable()
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
        Assert.Contains("kiloude_city", Kalos.Index.Areas);
        foreach (string map in new[] { "LostHotel", "FrostCavern", "TerminusCave" })
        {
            Assert.True(BuiltMaps.Value[map].IsStreamed);
            Assert.Contains(map, RegionDatabase.Get(RegionDatabase.Kalos)!.Maps);
        }
        foreach (string room in new[] { "KiloudeCity_PokemonCenter_1F", "KiloudeCity_NorthHouse", "KiloudeCity_SeaHouse" })
            Assert.Contains(room, RegionDatabase.Get(RegionDatabase.Kalos)!.Maps);
        Assert.Equal("Route 16", Overworld.DisplayNameAt(400, 270));
        Assert.Equal("Kiloude City", Overworld.DisplayNameAt(560, 380));
    }

    [Fact]
    public void TheCavesMouthsAndTheirWaysOutLandOnOpenGround()
    {
        foreach (var (x, y, cave) in new[] { (422, 240, "LostHotel"), (590, 203, "FrostCavern"), (246, 410, "TerminusCave") })
        {
            var warp = Overworld.GetWarpAt(x, y);
            Assert.Equal(cave, warp?.TargetMap);
            var inside = BuiltMaps.Value[cave];
            Assert.True(inside.IsWalkable(warp!.TargetX, warp.TargetY), $"{cave}: the way in lands on rock");
            var back = inside.GetWarpAt(31, 58);
            Assert.Equal("Kalos", back?.TargetMap);
            Assert.True(Overworld.IsWalkable(back!.TargetX, back.TargetY), $"{cave}: the way out lands on a blocked tile");
        }
        // The Pokémon Center and the houses of Kiloude City are found at their doors
        foreach (var (x, y, room) in new[] { (543, 348, "KiloudeCity_PokemonCenter_1F"), (581, 348, "KiloudeCity_NorthHouse"), (591, 364, "KiloudeCity_SeaHouse") })
            Assert.Equal(room, Overworld.GetWarpAt(x, y)?.TargetMap);
        Assert.Contains(MapNamed("KiloudeCity_PokemonCenter_1F").NPCs, n => n.IsHealingNurse);
        Assert.Contains(MapNamed("KiloudeCity_PokemonCenter_1F").NPCs, n => n.IsPokeMartClerk);
    }

    [Fact]
    public void FromTheBedroomTheWayReachesEveryNewPlaceAndEveryChamberOfTheCaves()
    {
        var walked = Walked.Value;
        bool Reached(string map, int x, int y) => walked.Any(p => p.Key.Name == map && p.Value.Contains((x, y)));
        foreach (var (x, y, what) in new[]
                 {
                     (393, 290, "Route 16's gate road"), (405, 270, "Route 16's crossroad"), (422, 250, "Route 16's head"), (422, 241, "before the hotel"),
                     (546, 357, "Kiloude's street"), (563, 370, "the pier"), (563, 380, "the landing"), (543, 349, "Kiloude's Pokémon Center door")
                 })
            Assert.True(Reached("Kalos", x, y), what);
        foreach (var (map, x, y) in new[]
                 {
                     ("LostHotel", 31, 55), ("LostHotel", 27, 30), ("LostHotel", 12, 24), ("LostHotel", 50, 26), ("LostHotel", 28, 16),
                     ("FrostCavern", 31, 50), ("FrostCavern", 31, 36), ("FrostCavern", 12, 14), ("FrostCavern", 28, 14), ("FrostCavern", 44, 14),
                     ("TerminusCave", 31, 50), ("TerminusCave", 30, 36), ("TerminusCave", 12, 16), ("TerminusCave", 45, 16), ("TerminusCave", 32, 20)
                 })
            Assert.True(Reached(map, x, y), $"{map} {x},{y}");
        foreach (string room in new[] { "KiloudeCity_PokemonCenter_1F", "KiloudeCity_NorthHouse", "KiloudeCity_SeaHouse" })
            Assert.Contains(walked, p => p.Key.Name == room);
        // Every item ball and trainer of the new places stands on ground that was walked
        foreach (var (key, mapName) in new[] { ("kalos_route_16", "Kalos"), ("lost_hotel", "LostHotel"), ("frost_cavern", "FrostCavern"), ("terminus_cave", "TerminusCave"), ("kiloude_city", "Kalos") })
            foreach (var o in Kalos.Area(key)!.Objects)
                Assert.True(Reached(mapName, o.X, o.Z) || walked.Any(p => p.Key.Name == mapName && Enumerable.Range(-1, 3).Any(dx => Enumerable.Range(-1, 3).Any(dz => p.Value.Contains((o.X + dx, o.Z + dz))))),
                    $"{key}: {o.Id} at {o.X},{o.Z} is out of the way");
    }

    [Fact]
    public void LumiosesNorthEastGateOpensWithThePowerAndTheOtherTwoStayShut()
    {
        Assert.DoesNotContain(Overworld.TriggersAt(392, 301), t => t.Script == "NotYetNorth" && t.Value == 2);
        Assert.Contains(Overworld.TriggersAt(392, 301), t => t.Script == "NotYetNorth" && t.Value == 1);
        Assert.Contains(Overworld.TriggersAt(418, 358), t => t.Script == "NotYetEast" && t.Value == 2);
        Assert.Contains(Overworld.TriggersAt(392, 418), t => t.Script == "NotYetSouth" && t.Value == 2);
    }

    [Fact]
    public void AZWaitsOnTheLandingOnlyAfterTheHallOfFameAndBattlesOnce()
    {
        var story = new StoryState();
        var map = Kalos.BuildMaps().Single(m => m.Name == "Kalos");
        map.ApplyPresence(story.Has);
        Assert.DoesNotContain(map.NPCs, n => n.Name == "AZ");
        story.Set("KalosHallOfFame");
        map.ApplyPresence(story.Has);
        var az = map.NPCs.Single(n => n.Name == "AZ");
        Assert.Equal((563, 381), (az.GridX, az.GridY));
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Greninja")!, 80));
        var bag = new Inventory();
        HeadlessScriptHost Play()
        {
            var host = new HeadlessScriptHost(story, party, bag) { Map = map, Region = RegionDatabase.Kalos };
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find("Az", "kiloude_city")!, az);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            return host;
        }
        var first = Play();
        Assert.True(story.Has("FLAG_KALOS_AZ_DONE"));
        Assert.Equal(3, bag.GetQuantity(ItemDatabase.Get("Rare Candy")!));
        Assert.Contains(first.Transcript, l => l.Text.Contains("I was a king", StringComparison.Ordinal));
        Play();
        Assert.Equal(3, bag.GetQuantity(ItemDatabase.Get("Rare Candy")!));
        var record = TrainerDatabase.Get("kalos_az_1")!;
        Assert.Equal(new[] { "Sigilyph", "Golurk", "Florges" }, record.Party.Select(p => p.Species));
    }

    [Fact]
    public void AllThreeKalosPokedexesAreCoveredByTheWildPokemonOfTheRegionOrItsSpeciesOnlyElsewhere()
    {
        // A floor, not a count: the Central, Coastal and Mountain lists all have species met somewhere in Kalos's tables
        var met = Kalos.Index.Areas.Select(k => Kalos.Area(k)?.Land).Where(l => l != null).SelectMany(l => l!).Select(s => s.Species).ToHashSet();
        foreach (string dex in new[] { "kalos-central", "kalos-coastal", "kalos-mountain" })
            Assert.True(Pokedex.Entries(dex).Count(e => met.Contains(e.Species.Name)) >= 25, dex);
    }
}
