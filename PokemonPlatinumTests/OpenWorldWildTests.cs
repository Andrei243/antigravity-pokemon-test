using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 32 · OW7, GPU-free: the wild Pokémon of an open map on the open test world's ground with its navigation mesh.
/// A zone brings out what its density and cap allow and never more, out of the player's view, from the rows of its
/// table met at the hour; two runs of the same ticks place the same Pokémon in the same places; one far from the player
/// is gone; each walks by its temperament (a calm one pays no heed, a shy one runs, a curious one comes and watches, an
/// aggressive one runs into the player); one met leaves and its zone waits; the temperaments' file is read and held to
/// its rules; the ground under the feet is the battle's.
/// </summary>
public class OpenWorldWildTests
{
    private const int T = OpenClock.TicksPerSecond;

    private static OpenMap Lab() => OpenWorldPeopleTests.LabWithPeople();

    private static OpenZone SouthMeadow(OpenMap map) => map.Zones.Single(z => z.Name == "south_meadow");

    /// <summary>A place on the lab's ground at x, z (metres), its height the ground's.</summary>
    private static OpenPos On(OpenMap map, double x, double z) =>
        OpenPos.FromMetres(x, map.Ground.Footing(x, z, 20, -20, walkableOnly: true)!.Value.Y, z);

    /// <summary>Ticks of the field with the player's stick as given (at rest unless told), after each the given look.</summary>
    private static void Tick(OpenField field, int ticks, Action? after = null, OpenInput? input = null)
    {
        for (int i = 0; i < ticks; i++)
        {
            field.Player.Step(field.Map, input ?? OpenInput.None, field.Grants);
            field.StepTheRest();
            after?.Invoke();
        }
    }

    /// <summary>The player north of the south meadow on the lab's ground, facing north (the zone behind them, out of view).</summary>
    private static OpenField NorthOfTheMeadow(OpenMap map) => new(map, On(map, 24, 6), OpenYaw.North);

    // ------------------------------------------------------------------ the zone's sums

    [Fact]
    public void AZonesTargetIsItsAreaByItsDensityWithinOneAndItsCap()
    {
        var map = Lab();
        // 18 m by 18 m at 3 to a thousand square metres: 0.97, at least one
        Assert.Equal(1, OpenWildField.TargetOf(map.Zones.Single(z => z.Name == "hill_top")));
        // 32 m by 16 m at 4: 2.05
        Assert.Equal(2, OpenWildField.TargetOf(SouthMeadow(map)));
        // A square kilometre at 4 to a thousand is 4,000, held to its cap
        var wide = new OpenZone("wide", "x", new (int, int)[] { (0, 0), (1_000_000, 0), (1_000_000, 1_000_000), (0, 1_000_000) }, -1000, 1000,
            new[] { new OpenSlot("Bidoof", 2, 2, 1) }, 4, 7);
        Assert.Equal(7, OpenWildField.TargetOf(wide));
    }

    [Fact]
    public void ARowsTimeOfDayIsTheGamesFiveReadAsThree()
    {
        var any = new OpenSlot("Bidoof", 2, 2, 1);
        var night = any with { Time = OpenSlotTime.Night };
        var day = any with { Time = OpenSlotTime.Day };
        var morning = any with { Time = OpenSlotTime.Morning };
        foreach (var now in Enum.GetValues<TimeOfDay>()) Assert.True(OpenWildField.MetAt(any, now));
        Assert.Equal(new[] { TimeOfDay.Night, TimeOfDay.LateNight }, Enum.GetValues<TimeOfDay>().Where(n => OpenWildField.MetAt(night, n)));
        Assert.Equal(new[] { TimeOfDay.Day, TimeOfDay.Twilight }, Enum.GetValues<TimeOfDay>().Where(n => OpenWildField.MetAt(day, n)));
        Assert.Equal(new[] { TimeOfDay.Morning }, Enum.GetValues<TimeOfDay>().Where(n => OpenWildField.MetAt(morning, n)));
    }

    // ------------------------------------------------------------------ coming out

    /// <summary>Every wild Pokémon of a field at every tick, as text, for two runs to be held equal.</summary>
    private static List<string> Watch(OpenField field, int ticks)
    {
        var seen = new List<string>();
        Tick(field, ticks, () => seen.Add(string.Join(";", field.Wild.All.Select(w => $"{w.Id} {w.Species} {w.Level} {w.Pos} {w.Yaw} {w.Mood}"))));
        return seen;
    }

    [Fact]
    public void TwoRunsOfTheSameTicksPlaceTheSamePokemon()
    {
        var first = Watch(NorthOfTheMeadow(Lab()), 30 * T);
        var second = Watch(NorthOfTheMeadow(Lab()), 30 * T);
        Assert.Equal(first, second);
        Assert.Contains(first, row => row.Length > 0);
    }

    [Fact]
    public void AZoneNeverHoldsMoreThanItsTargetAndBringsThemOutOfView()
    {
        var map = Lab();
        var zone = SouthMeadow(map);
        var field = NorthOfTheMeadow(map);
        var known = new HashSet<int>();
        int most = 0;
        // Ninety seconds walking back and forth along the meadow's north edge, turning now and then
        for (int leg = 0; leg < 12; leg++)
        {
            var input = new OpenInput(leg % 2 == 0 ? 0.5 : -0.5, 0);
            Tick(field, (int)(7.5 * T), () =>
            {
                var player = field.Player;
                int inZone = field.Wild.All.Count(w => w.Zone == zone);
                most = Math.Max(most, inZone);
                Assert.True(inZone <= OpenWildField.TargetOf(zone), $"{inZone} stood in a zone that wants {OpenWildField.TargetOf(zone)}");
                foreach (var w in field.Wild.All)
                {
                    if (!known.Add(w.Id)) continue;
                    // Out of view as it came out: far enough off and not ahead
                    long d2 = w.Home.GroundDistanceSquared(player.Pos);
                    Assert.InRange(d2, (long)OpenWildField.SpawnNear * OpenWildField.SpawnNear, (long)OpenWildField.SpawnFar * OpenWildField.SpawnFar);
                    int toward = OpenTriangles.YawOf(w.Home.X - player.Pos.X, w.Home.Z - player.Pos.Z);
                    Assert.True(Math.Abs(OpenYaw.Between(player.Yaw, toward)) > OpenWildField.AheadHalfAngle, $"{w} came out ahead of the player");
                    Assert.True(w.Zone.Contains(w.Home), $"{w} came out outside its zone");
                }
            }, input);
        }
        Assert.Equal(OpenWildField.TargetOf(zone), most);
    }

    [Fact]
    public void APokemonOfTheNightsTableOnlyComesOutAtNight()
    {
        foreach (var now in Enum.GetValues<TimeOfDay>())
        {
            var map = Lab();
            var field = NorthOfTheMeadow(map);
            field.Wild.Now = now;
            var species = new HashSet<string>();
            // Long enough for several to come out and go: the player stepped far off and back now and then
            for (int round = 0; round < 6; round++)
            {
                Tick(field, 8 * T, () => { foreach (var w in field.Wild.All) species.Add(w.Species); });
                field.Player.Place(On(map, -60, -60), OpenYaw.East);
                Tick(field, 1);
                Assert.DoesNotContain(field.Wild.All, w => w.Zone.Name == "south_meadow");
                field.Player.Place(On(map, 24, 6), OpenYaw.North);
            }
            bool night = now is TimeOfDay.Night or TimeOfDay.LateNight;
            Assert.Equal(night, species.Contains("Hoothoot"));
            Assert.Equal(now is TimeOfDay.Day or TimeOfDay.Twilight, species.Contains("Starly"));
            Assert.NotEmpty(species);
        }
    }

    [Fact]
    public void OneFarFromThePlayerIsGoneAndNothingComesOutUnderAScript()
    {
        var map = Lab();
        var zone = SouthMeadow(map);
        var field = NorthOfTheMeadow(map);
        int Here() => field.Wild.All.Count(w => w.Zone == zone);
        Tick(field, 10 * T);
        Assert.True(Here() > 0);
        // Put down in the far north-west, more than 72 m off the meadow: gone at the next tick
        field.Player.Place(On(map, -60, -60), OpenYaw.East);
        Tick(field, 1);
        Assert.Equal(0, Here());
        // Back, with the field held (a script): nothing comes out
        field.Player.Place(On(map, 24, 6), OpenYaw.North);
        field.WildMayAct = false;
        Tick(field, 10 * T);
        Assert.Equal(0, Here());
        field.WildMayAct = true;
        Tick(field, 10 * T);
        Assert.True(Here() > 0);
    }

    // ------------------------------------------------------------------ temperaments

    /// <summary>A field of the lab with nothing coming out by itself (its zones' waits long) and one put down by hand.</summary>
    private static (OpenField Field, OpenWildPokemon Wild) WithOne(string species, double x, double z, int yaw)
    {
        var map = Lab();
        var zone = SouthMeadow(map);
        var field = new OpenField(map, On(map, 24, 22), OpenYaw.North);
        // The zones kept quiet: one met in each already, so they wait
        foreach (var z0 in map.Zones) field.Wild.Ended(z0, OpenWildEnd.CaughtOrFelled);
        Tick(field, OpenWildField.Grace);
        var wild = field.Wild.Put(map, zone, new OpenSlot(species, 5, 5, 1), 5, On(map, x, z), yaw);
        return (field, wild);
    }

    private static double Apart(OpenField field, OpenWildPokemon w) => Math.Sqrt(w.Pos.GroundDistanceSquared(field.Player.Pos)) / 1000.0;

    [Fact]
    public void ACalmOnePaysThePlayerNoHeed()
    {
        // Bidoof: calm. Four metres off, facing the player
        var (field, wild) = WithOne("Bidoof", 24, 18, OpenYaw.South);
        Assert.Equal(OpenTemperament.Calm, wild.Kind.Temperament);
        Tick(field, 5 * T, () => Assert.Equal(OpenWildMood.Roaming, wild.Mood));
        Assert.Null(field.Wild.Touching(field.Player.Pos));
    }

    [Fact]
    public void AShyOneRunsOnceItSmellsThePlayer()
    {
        // Starly: shy, smells within 5 m. Four metres off, facing away
        var (field, wild) = WithOne("Starly", 24, 26, OpenYaw.South);
        Assert.Equal(OpenTemperament.Shy, wild.Kind.Temperament);
        double before = Apart(field, wild);
        Tick(field, T / 2);
        Assert.Equal(OpenWildMood.Fleeing, wild.Mood);
        Tick(field, 3 * T);
        Assert.True(Apart(field, wild) > before + 6, $"it stood {Apart(field, wild):0.0} m off after its flight, from {before:0.0} m");
    }

    [Fact]
    public void ACuriousOneComesOverAndStandsWatching()
    {
        // Wooloo: curious, sees 12 m ahead and comes to 3 m. Ten metres south of the player, facing them
        var (field, wild) = WithOne("Wooloo", 24, 32 - 0.5, OpenYaw.North);
        Assert.Equal(OpenTemperament.Curious, wild.Kind.Temperament);
        Tick(field, 8 * T);
        Assert.Equal(OpenWildMood.Watching, wild.Mood);
        Assert.InRange(Apart(field, wild), 2.4, 3.6);
        Assert.Null(field.Wild.Touching(field.Player.Pos));
        // Facing the player
        int toward = OpenTriangles.YawOf(field.Player.Pos.X - wild.Pos.X, field.Player.Pos.Z - wild.Pos.Z);
        Assert.InRange(Math.Abs(OpenYaw.Between(wild.Yaw, toward)), 0, 600);
    }

    [Fact]
    public void OneThatNoticesThePlayerHasTheExclaimForASecondAndIsToldOnceForItsCry()
    {
        var told = new List<OpenWildPokemon>();
        // A curious Wooloo ten metres south, facing the player: it notices, comes over and cries
        var (field, wild) = WithOne("Wooloo", 24, 32 - 0.5, OpenYaw.North);
        Assert.Equal(-1, wild.NoticedAt);
        int ticks = 0;
        while (wild.Mood == OpenWildMood.Roaming && ticks < 3 * T) { Tick(field, 1); ticks++; }
        Assert.NotEqual(OpenWildMood.Roaming, wild.Mood);
        Assert.Equal(field.Wild.Tick, wild.NoticedAt);
        Assert.True(field.Wild.Exclaiming(wild));
        field.Wild.TakeNoticed(told);
        Assert.Equal(new[] { wild }, told);
        Assert.True(OpenWildField.CriesOut(wild));
        field.Wild.TakeNoticed(told);
        Assert.Empty(told);
        Tick(field, OpenWildField.ExclaimTicks - 1);
        Assert.True(field.Wild.Exclaiming(wild));
        Tick(field, 1);
        Assert.False(field.Wild.Exclaiming(wild));
        // A curious one noticing from within its keep turns to watching at once, and cries all the same
        var (nearField, near) = WithOne("Wooloo", 24, 24.5, OpenYaw.North);
        Tick(nearField, T);
        Assert.Equal(OpenWildMood.Watching, near.Mood);
        nearField.Wild.TakeNoticed(told);
        Assert.Equal(new[] { near }, told);
        Assert.True(OpenWildField.CriesOut(near));
        // A shy one notices and runs off with the "!" but no cry; a calm one never notices
        var (shyField, shy) = WithOne("Starly", 24, 26, OpenYaw.South);
        Tick(shyField, T / 2);
        Assert.True(shyField.Wild.Exclaiming(shy));
        Assert.False(OpenWildField.CriesOut(shy));
        var (calmField, calm) = WithOne("Bidoof", 24, 18, OpenYaw.South);
        Tick(calmField, 3 * T);
        Assert.Equal(-1, calm.NoticedAt);
        calmField.Wild.TakeNoticed(told);
        Assert.Empty(told);
    }

    [Fact]
    public void AnAggressiveOneChargesAndRunsIntoThePlayer()
    {
        // Staravia: aggressive, sees 16 m. Twelve metres east of the player, facing them
        var (field, wild) = WithOne("Staravia", 36, 22, OpenYaw.West);
        Assert.Equal(OpenTemperament.Aggressive, wild.Kind.Temperament);
        int ticks = 0;
        while (field.Wild.Touching(field.Player.Pos) == null && ticks < 10 * T)
        {
            Tick(field, 1);
            ticks++;
        }
        Assert.Same(wild, field.Wild.Touching(field.Player.Pos));
        Assert.Equal(OpenWildMood.Charging, wild.Mood);
        // At a run: twelve metres less the reach in well under five seconds
        Assert.InRange(ticks, T, 5 * T);
    }

    [Fact]
    public void OneMetLeavesTheFieldAndItsZoneWaits()
    {
        var (field, wild) = WithOne("Bidoof", 24, 22.6, OpenYaw.South);
        var zone = wild.Zone;
        Tick(field, 1);
        Assert.Same(wild, field.Wild.Touching(field.Player.Pos));
        // One walked into from behind: the player facing within an eighth of the way it faces
        Assert.True(OpenWildField.FromBehind(OpenYaw.South, wild.Yaw + 2000));
        Assert.False(OpenWildField.FromBehind(OpenYaw.North, wild.Yaw));
        field.Wild.Met(wild);
        Assert.DoesNotContain(wild, field.Wild.All);
        Assert.True(field.Wild.WaitOf(zone) >= OpenWildField.RespawnAfterBattle - 2);
        // Run from: the shorter wait, and a grace in which nothing is met
        field.Wild.Ended(zone, OpenWildEnd.Otherwise);
        Assert.Equal(OpenWildField.RespawnAfterFlight, field.Wild.WaitOf(zone));
        var another = field.Wild.Put(field.Map, zone, new OpenSlot("Bidoof", 3, 3, 1), 3, field.Player.Pos, OpenYaw.South);
        Assert.Null(field.Wild.Touching(field.Player.Pos));
        Tick(field, OpenWildField.Grace);
        Assert.Same(another, field.Wild.Touching(field.Player.Pos));
    }

    // ------------------------------------------------------------------ the temperaments' file

    [Fact]
    public void TheGamesTemperamentsAreReadAndGoBySpeciesThenTypeThenTheDefault()
    {
        var kinds = OpenWildKinds.Game;
        Assert.Equal(OpenTemperament.Curious, kinds.For("Wooloo").Temperament);
        Assert.Equal(OpenTemperament.Shy, kinds.For("Starly").Temperament);
        // Dratini is named nowhere: a Dragon type
        Assert.Equal(OpenTemperament.Aggressive, kinds.For("Dratini").Temperament);
        // Zubat: Poison, then Flying
        Assert.Equal(OpenTemperament.Shy, kinds.For("Zubat").Temperament);
        // Geodude: Rock and Ground, neither with a row
        Assert.Equal(OpenTemperament.Calm, kinds.For("Geodude").Temperament);
        // A form takes its species' where it has none of its own
        Assert.Equal(OpenTemperament.Curious, kinds.For("Pikachu-Belle").Temperament);
        Assert.False(kinds.For("Bidoof").Notices);
        Assert.True(kinds.For("Starly").Sight > 0);
        // Every species the file names is the data's
        Assert.All(kinds.NamedSpecies, s => Assert.True(PokemonDatabase.Get(s) != null || PokemonDatabase.SpeciesOfForm(s) != null, s));
    }

    [Theory]
    [InlineData("""{ "kinds": { "Calm": {}, "Shy": {}, "Curious": {} } }""", "has no row for Aggressive")]
    [InlineData("""{ "kinds": { "Calm": {}, "Shy": {}, "Curious": {}, "Aggressive": {}, "Sleepy": {} } }""", "'Sleepy' is no temperament")]
    [InlineData("""{ "kinds": { "Calm": {}, "Shy": { "sight": 40 }, "Curious": {}, "Aggressive": {} } }""", "sight and scent are from nought")]
    [InlineData("""{ "kinds": { "Calm": {}, "Shy": {}, "Curious": {}, "Aggressive": {} }, "types": { "Cosmic": "Shy" } }""", "no such type")]
    [InlineData("""{ "kinds": { "Calm": {}, "Shy": {}, "Curious": {}, "Aggressive": {} }, "species": { "Bidoof": "Grumpy" } }""", "'Grumpy' is no temperament")]
    [InlineData("""{ "kinds": { "Calm": {}, "Shy": {}, "Curious": {}, "Aggressive": {} }, "sight": 3 }""", "no such field")]
    public void ATemperamentsFileThatBreaksItsRulesIsRefused(string text, string says)
    {
        var e = Assert.Throws<InvalidDataException>(() => OpenWildKinds.From(GameDataFiles.ReadStrict<OpenWildFile>(text, "open-wild.json"), "open-wild.json"));
        Assert.Contains(says, e.Message);
        Assert.StartsWith("open-wild.json: ", e.Message);
    }

    [Theory]
    [InlineData("""{ "zones": [ { "name": "z", "polygon": [[0,0],[1,0],[1,1]], "slots": [ { "species": "Bidoof", "minLevel": 2, "maxLevel": 301 } ] } ] }""", "maxLevel")] // one past Ruleset.HighestLevel (300 since plan 46 · CP15)
    [InlineData("""{ "zones": [ { "name": "z", "polygon": [[0,0],[1,0],[1,1]], "slots": [ { "species": "Bidoof", "minLevel": 2, "maxLevel": 3, "time": "noon" } ] } ] }""", "time")]
    public void AZonesRowThatBreaksItsRulesIsRefused(string text, string says)
    {
        var e = Assert.ThrowsAny<Exception>(() => OpenPlaceFiles.Read(text, "places/x.json"));
        Assert.True(e is InvalidDataException or System.Text.Json.JsonException, e.GetType().Name);
        Assert.Contains(says, e.Message);
    }

    [Fact]
    public void TheLabsMeadowZoneReadsItsRowsTimes()
    {
        var zone = SouthMeadow(Lab());
        Assert.Equal(new[] { OpenSlotTime.Day, OpenSlotTime.Night, OpenSlotTime.Any }, zone.Slots.Select(s => s.Time));
    }

    // ------------------------------------------------------------------ the content check

    [Fact]
    public void TheContentCheckFindsAZoneWithNoMeshInsideItAndAnUnknownTemperamentsSpecies()
    {
        var unreadable = new List<ContentCheck.Finding>();
        var region = ContentCheck.GameContent.ReadOpenRegion(OpenWorldBakeTests.LabFolder, "lab", unreadable);
        Assert.Empty(unreadable);
        // The lab's own maps, with the land baked and its navigation mesh: its zones are walked inside
        var map = Lab();
        var house = OpenWorldPlacesTests.LabHouse();
        var found = new List<ContentCheck.Finding>();
        ContentCheck.OpenWorlds.CheckRegion(region with { Maps = new[] { map } }, n => n == house.Name ? house : null,
            ScriptLibrary.FromSources(("lab_cave", "script Cavern\n end\n"), ("common", "")), null, found);
        Assert.DoesNotContain(found, f => f.Name is "south_meadow" or "hill_top");
        // A zone over the pond's water and its stack: ground under it, but no walked polygon of the mesh
        map.Add(new OpenZone("pond_zone", "lab_meadow", new (int, int)[] { (37_000, -40_000), (43_000, -40_000), (43_000, -36_000), (37_000, -36_000) }, -3_000, 1_000,
            new[] { new OpenSlot("Bidoof", 3, 4, 10) }, 3, 3));
        found.Clear();
        ContentCheck.OpenWorlds.CheckRegion(region with { Maps = new[] { map } }, n => n == house.Name ? house : null,
            ScriptLibrary.FromSources(("lab_cave", "script Cavern\n end\n"), ("common", "")), null, found);
        Assert.Contains(found, f => f.Name == "pond_zone" && f.What.Contains("navigation mesh", StringComparison.Ordinal));

        // The game's own temperaments pass; a file naming nobody is told of
        var told = new List<ContentCheck.Finding>();
        ContentCheck.OpenWorlds.CheckTemperaments(GameDataFiles.Directory, told);
        Assert.Empty(told);
        string dir = Path.Combine(Path.GetTempPath(), "openwild-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, OpenWildKinds.FileName), """
                { "kinds": { "Calm": {}, "Shy": {}, "Curious": {}, "Aggressive": {} }, "species": { "Nobodymon": "Shy" } }
                """);
            ContentCheck.OpenWorlds.CheckTemperaments(dir, told);
            var finding = Assert.Single(told);
            Assert.Equal(("open-wild.json", "Nobodymon"), (finding.File, finding.Name));
        }
        finally { Directory.Delete(dir, true); }
    }

    // ------------------------------------------------------------------ the ground a battle is fought on

    [Fact]
    public void TheGroundUnderTheFeetIsTheBattles()
    {
        Assert.Equal(BattleTerrain.Sand, OpenBattleGround.TerrainOf(OpenCover.Sand, inWater: false));
        Assert.Equal(BattleTerrain.Water, OpenBattleGround.TerrainOf(OpenCover.Sand, inWater: true));
        Assert.Equal(BattleTerrain.Grass, OpenBattleGround.TerrainOf(OpenCover.GrassDeep, false));
        Assert.Equal(BattleTerrain.Plain, OpenBattleGround.TerrainOf(OpenCover.Grass, false));
        Assert.Equal(BattleTerrain.Cave, OpenBattleGround.TerrainOf(OpenCover.CaveFloor, false));
        Assert.Equal(BattleTerrain.Mountain, OpenBattleGround.TerrainOf(OpenCover.Rock, false));
        Assert.Equal(BattleTerrain.Building, OpenBattleGround.TerrainOf(null, false));
        var map = Lab();
        // The meadow's grass, its muddy square a marsh, the cavern's floor, and a box's top (the house) a building's floor
        Assert.Equal(BattleTerrain.Plain, OpenBattleGround.Under(map, On(map, 14, 22), swimming: false));
        Assert.Equal(BattleTerrain.GreatMarsh, OpenBattleGround.Under(map, On(map, 25, 22), swimming: false));
        Assert.Equal(BattleTerrain.Cave, OpenBattleGround.Under(map, map.Marker("cavern_middle")!.Pos, swimming: false));
        var roof = OpenPos.FromMetres(-20, 4, -46);
        Assert.Equal(BattleTerrain.Building, OpenBattleGround.Under(map, roof, swimming: false));
        // The river's shore is sand somewhere along its north bank
        Assert.NotNull(SandOf(map));
    }

    /// <summary>A place on the lab's river shore whose cover is sand and over no water, or null.</summary>
    internal static OpenPos? SandOf(OpenMap map)
    {
        for (int z = 30_000; z <= 44_000; z += 250)
            for (int x = -50_000; x <= 50_000; x += 1000)
            {
                if (map.Ground.Footing(x / 1000.0, z / 1000.0, 10, -10, walkableOnly: true) is not { } footing) continue;
                if (map.Ground.WaterAt(x / 1000.0, z / 1000.0) is { } water && water > footing.Y - 0.05) continue;
                if (map.Ground.CoverUnder(x / 1000.0, z / 1000.0, footing.Y + 0.6, footing.Y - 1) != OpenCover.Sand) continue;
                // Sand at every corner round it, so a body's own place is surely sand
                bool all = true;
                foreach (var (dx, dz) in new[] { (300, 0), (-300, 0), (0, 300), (0, -300) })
                    if (map.Ground.CoverUnder((x + dx) / 1000.0, (z + dz) / 1000.0, footing.Y + 0.6, footing.Y - 1) != OpenCover.Sand) all = false;
                if (all) return new OpenPos(x, OpenPos.Millimetres(footing.Y), z);
            }
        return null;
    }
}
