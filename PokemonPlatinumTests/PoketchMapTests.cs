using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.PoketchApps;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The Pokétch's map apps (plan 06 · R14b): the Dowsing Machine, the Berry Searcher, the Marking Map and the Trainer
/// Counter, each held to the original's rules (<c>src/applications/poketch/dowsing_machine</c>, <c>berry_searcher</c>,
/// <c>marking_map</c>, <c>trainer_counter</c>, <c>poketch_map.c</c>, <c>pokeradar.c</c>), on small maps of the
/// tests' own.
/// </summary>
public class PoketchMapTests
{
    private static (Poketch Poketch, T App) Open<T>(PoketchApp app) where T : PoketchAppState
    {
        var poketch = new Poketch { Enabled = true };
        poketch.Register(app);
        return (poketch, Assert.IsType<T>(poketch.State));
    }

    private static PoketchContext Context(Poketch poketch, Map? map = null, int x = 0, int y = 0, List<string>? sounds = null) => new()
    {
        Poketch = poketch,
        Map = map,
        X = x,
        Y = y,
        Story = new StoryState(),
        Sound = s => sounds?.Add(s)
    };

    // ================================================================== the Dowsing Machine

    private static Map Field(params (int X, int Y, string Flag, int Range)[] hidden)
    {
        var map = new Map(40, 40);
        foreach (var (x, y, flag, range) in hidden) map.HiddenItems[(x, y)] = new HiddenItem("Potion", 1, flag, range);
        return map;
    }

    [Fact]
    public void TheDowsingMachineShowsAnItemTouchedWithinItsRange()
    {
        // FindNearbyHiddenItems: tiles 11 pixels apart; ranges 8, 24 and 48. A range-2 item two tiles right of the
        // player, touched on its own tile (0 pixels) and three tiles off (33 ≤ 48)
        var (poketch, app) = Open<DowsingMachineApp>(PoketchApp.DowsingMachine);
        var sounds = new List<string>();
        var context = Context(poketch, Field((12, 10, "FLAG_HIDDEN_A", 2)), 10, 10, sounds);

        app.Press(DowsingMachineApp.TileId(2, 0), context);
        Assert.Equal(DowsingResult.NearbyItems, app.Result);
        Assert.Equal(new DowsedItem(2, 0, 2), Assert.Single(app.Items));
        Assert.Equal("dowsing_ping", Assert.Single(sounds));
        Assert.True(app.Pinging);

        app.Press(DowsingMachineApp.TileId(-1, 0), context);
        Assert.Equal(DowsingResult.NearbyItems, app.Result);

        // Seven tiles off is 77 pixels: nothing at all
        app.Press(DowsingMachineApp.TileId(-5, 0), context);
        Assert.Equal(DowsingResult.NoItems, app.Result);
        Assert.Empty(app.Items);
    }

    [Fact]
    public void AnItemOutsideItsOwnRangeButWithin48PixelsIsOnlyNear()
    {
        // A range-1 item (24 pixels: within two tiles, not (2, 1), which is 5 × 121 > 576) touched three tiles off:
        // 33 pixels is past its range and within 48, so the ring only says something is near
        var (poketch, app) = Open<DowsingMachineApp>(PoketchApp.DowsingMachine);
        var context = Context(poketch, Field((10, 10, "FLAG_HIDDEN_B", 1)), 10, 10);
        app.Press(DowsingMachineApp.TileId(3, 0), context);
        Assert.Equal(DowsingResult.FarItems, app.Result);
        Assert.Empty(app.Items);
        app.Press(DowsingMachineApp.TileId(2, 0), context);
        Assert.Equal(DowsingResult.NearbyItems, app.Result);
        app.Press(DowsingMachineApp.TileId(2, 1), context);
        Assert.Equal(DowsingResult.FarItems, app.Result);

        // A range-0 item (8 pixels) is shown only from its own tile
        context.Map = Field((10, 10, "FLAG_HIDDEN_C", 0));
        app.Press(DowsingMachineApp.TileId(1, 0), context);
        Assert.Equal(DowsingResult.FarItems, app.Result);
        app.Press(DowsingMachineApp.TileId(0, 0), context);
        Assert.Equal(DowsingResult.NearbyItems, app.Result);
    }

    [Fact]
    public void TheDowsingMachineSkipsItemsTakenAndThoseOutOfTheGrid()
    {
        // FieldSystem_GetNearbyHiddenItems: seven tiles either side, seven up and six down; not one whose flag is set
        var (poketch, app) = Open<DowsingMachineApp>(PoketchApp.DowsingMachine);
        var context = Context(poketch, Field((18, 10, "FLAG_FAR_RIGHT", 2), (10, 17, "FLAG_FAR_DOWN", 2), (3, 10, "FLAG_LEFT_EDGE", 2), (10, 4, "FLAG_TAKEN", 2)), 10, 10);
        context.Story!.Set("FLAG_TAKEN");

        app.Press(DowsingMachineApp.TileId(7, 0), context);     // the item at 8 to the right is off the grid
        Assert.Equal(DowsingResult.NoItems, app.Result);
        app.Press(DowsingMachineApp.TileId(0, 6), context);     // and the one 7 down
        Assert.Equal(DowsingResult.NoItems, app.Result);
        app.Press(DowsingMachineApp.TileId(-7, 0), context);    // seven to the left is on it
        Assert.Equal(new DowsedItem(-7, 0, 2), Assert.Single(app.Items));
        app.Press(DowsingMachineApp.TileId(0, -6), context);    // found already
        Assert.Equal(DowsingResult.NoItems, app.Result);
    }

    [Fact]
    public void TheDowsingMachineShowsEightItemsAtMost()
    {
        var hidden = Enumerable.Range(0, 10).Select(i => (10 + i % 3, 9 + i / 3, $"FLAG_PILE_{i}", 2)).ToArray();
        var (poketch, app) = Open<DowsingMachineApp>(PoketchApp.DowsingMachine);
        var context = Context(poketch, Field(hidden), 10, 10);
        app.Press(DowsingMachineApp.TileId(1, 0), context);
        Assert.Equal(DowsingMachineApp.MaxItems, app.Items.Count);
    }

    [Fact]
    public void TheRingGoesOutOnceForNothingOnAndOnForSomethingNearAndStopsWhenThePlayerWalks()
    {
        var (poketch, app) = Open<DowsingMachineApp>(PoketchApp.DowsingMachine);
        var context = Context(poketch, Field((10, 10, "FLAG_HIDDEN_D", 1)), 10, 10);

        // No items: one ring
        app.Press(DowsingMachineApp.TileId(-6, -6), context);
        app.Update(DowsingMachineApp.PingSeconds + 0.01f, context);
        Assert.False(app.Pinging);

        // Far: the ring again and again
        app.Press(DowsingMachineApp.TileId(3, 0), context);
        for (int i = 0; i < 5; i++) app.Update(DowsingMachineApp.PingSeconds, context);
        Assert.True(app.Pinging);
        Assert.False(app.ShowingItems);

        // Near: the ring, then the items, then the ring
        app.Press(DowsingMachineApp.TileId(1, 0), context);
        app.Update(DowsingMachineApp.PingSeconds + 0.01f, context);
        Assert.True(app.ShowingItems);
        app.Update(DowsingMachineApp.ItemSeconds, context);
        Assert.False(app.ShowingItems);
        Assert.True(app.Pinging);

        // A step stops it all
        context.X = 11;
        app.Update(0.01f, context);
        Assert.False(app.Pinging);
    }

    // ================================================================== the Berry Searcher

    [Fact]
    public void TheBerrySearcherShowsTheCellsOfPatchesGrowingAndInFruit()
    {
        // GetReadyBerryPatches: a patch growing and at BERRY_GROWTH_STAGE_FRUIT, by sBerryPositions; the patches
        // after it in the same cell skipped. A new game's patches are in fruit but grow only once seen
        var berries = BerryPatches.NewGame();
        Assert.Empty(BerrySearcherApp.Look(berries));

        berries.Seen(0);
        berries.Seen(1);
        Assert.Equal(new[] { (5, 20) }, BerrySearcherApp.Look(berries));
        berries.Seen(2);
        Assert.Equal(new[] { (5, 20), (6, 20) }, BerrySearcherApp.Look(berries));

        // Picked bare, it shows no more; one patch of a cell in fruit is enough
        berries.Pick(0);
        Assert.Equal(new[] { (5, 20), (6, 20) }, BerrySearcherApp.Look(berries));
        berries.Pick(1);
        Assert.Equal(new[] { (6, 20) }, BerrySearcherApp.Look(berries));

        // A patch past the table's 118 is never shown
        berries[120].Berry = "Oran Berry";
        berries[120].Stage = BerryStage.Fruit;
        berries[120].Growing = true;
        Assert.Equal(new[] { (6, 20) }, BerrySearcherApp.Look(berries));
    }

    [Fact]
    public void TheBerrySearcherLooksAgainOnlyWhenTouched()
    {
        var berries = BerryPatches.NewGame();
        var (poketch, app) = Open<BerrySearcherApp>(PoketchApp.BerrySearcher);
        var sounds = new List<string>();
        var context = Context(poketch, sounds: sounds);
        context.Berries = berries;
        app.Update(0.01f, context);
        Assert.Empty(app.Ready(context));

        // Seen since: nothing changes until the screen is touched
        berries.Seen(4);
        app.Update(0.5f, context);
        Assert.Empty(app.Ready(context));
        app.Press(0, context);
        Assert.Equal(new[] { (6, 19) }, app.Ready(context));
        Assert.Equal(1f, app.Blur);

        // A touch while the picture comes back into focus does nothing
        berries.Seen(6);
        app.Press(0, context);
        Assert.Single(app.Ready(context));
        Assert.Single(sounds);
        app.Update(BerrySearcherApp.RefreshSeconds, context);
        Assert.Equal(0f, app.Blur);
        app.Press(0, context);
        Assert.Equal(new[] { (6, 19), (7, 17) }, app.Ready(context));
    }

    // ================================================================== the map of Sinnoh

    [Fact]
    public void ThePlayerIsInTheChunkTheyStandInOnTheOverworldOnly()
    {
        // PoketchMap_GetPlayerLocation: the tile over 32 on the main matrix
        var poketch = new Poketch();
        var sinnoh = new Map(4, 4) { Name = PoketchMap.Overworld };
        Assert.Equal((5, 24), PoketchMap.PlayerCell(Context(poketch, sinnoh, 180, 777)));   // Jubilife City
        Assert.Null(PoketchMap.PlayerCell(Context(poketch, new Map(4, 4) { Name = "JubilifePokemonCenter" }, 5, 5)));
        Assert.Null(PoketchMap.PlayerCell(Context(poketch, sinnoh, 10, 10)));   // the empty north is off the map
    }

    [Fact]
    public void TheOriginalsPixelsBecomeCells()
    {
        // mapPositionsX: 26 + 6x; mapPositionsY: 24 + 6(y - 5)
        Assert.Equal((0, 5), PoketchMap.CellOfPixel(26, 24));
        Assert.Equal((29, 29), PoketchMap.CellOfPixel(200, 168));
        // PoketchMap_GetPositionFromMapID: Route 201 at (47, 150), the Valley Windworks at (68, 114)
        Assert.Equal((4, 26), PoketchMap.RouteCell(0));
        Assert.Equal((7, 20), PoketchMap.RouteCell(27));
        Assert.Null(PoketchMap.RouteCell(Roamers.Routes.Length));
    }

    // ================================================================== the Marking Map

    [Fact]
    public void TheMarkersStartInARowAndArePickedUpAndPutDown()
    {
        // sDefaultMapMarkers: (104 … 184, 152) from the screen's corner at (16, 16)
        var (poketch, app) = Open<MarkingMapApp>(PoketchApp.MarkingMap);
        Assert.Equal(new[] { (16, 29), (18, 29), (21, 29), (24, 29), (26, 29), (29, 29) }, app.Positions);

        // A touch within eight pixels (a cell either way) of a marker picks it up; another cell puts it down
        var context = Context(poketch);
        app.Press(PoketchMap.Id(17, 28), context);
        Assert.Equal(0, app.Carried);
        app.Press(PoketchMap.Id(10, 20), context);
        Assert.Null(app.Carried);
        Assert.Equal((10, 20), app.Positions[0]);

        // A touch far from any marker picks nothing up
        app.Press(PoketchMap.Id(0, 5), context);
        Assert.Null(app.Carried);
    }

    [Fact]
    public void TheMarkerMovedLastIsFoundFirst()
    {
        // UpdateMarkerPriorities: markers 1 and 2 at 18 and 21; a touch at 19 or 20 is within a cell of one of them.
        // Marker 2 moved to 19 lies beside marker 1: a touch there now finds marker 2 first
        var (poketch, app) = Open<MarkingMapApp>(PoketchApp.MarkingMap);
        var context = Context(poketch);
        app.Press(PoketchMap.Id(21, 29), context);
        app.Press(PoketchMap.Id(19, 29), context);
        Assert.Equal(2, app.Order[0]);
        app.Press(PoketchMap.Id(18, 29), context);
        Assert.Equal(2, app.Carried);
    }

    [Fact]
    public void TheMarkersOutlastASaveAndTheAppComingUpAgain()
    {
        // Poketch_SetMapMarker / Poketch_MapMarkerPos: the Pokétch keeps them
        var (poketch, app) = Open<MarkingMapApp>(PoketchApp.MarkingMap);
        var context = Context(poketch);
        app.Press(PoketchMap.Id(16, 29), context);
        app.Press(PoketchMap.Id(12, 15), context);

        poketch.Close();
        Assert.Equal((12, 15), Assert.IsType<MarkingMapApp>(poketch.State).Positions[0]);

        var loaded = new Poketch();
        loaded.Load(poketch.Save());
        var again = Assert.IsType<MarkingMapApp>(loaded.State);
        Assert.Equal((12, 15), again.Positions[0]);
        Assert.Equal((18, 29), again.Positions[1]);
    }

    [Fact]
    public void TheMarkingMapShowsTheRoamersOnTheLoose()
    {
        // Roamer_GetData(ROAMER_DATA_ACTIVE) and its map: Mesprit on Route 201, Cresselia not loose
        var encounters = new SpecialEncounters();
        encounters.Roamers[0].Active = true;
        encounters.Roamers[0].Route = 0;
        encounters.Roamers[1].Route = 5;
        Assert.Equal(new[] { (0, 4, 26) }, PoketchMap.RoamerCells(encounters));
        encounters.Roamers[1].Active = true;
        Assert.Equal(2, PoketchMap.RoamerCells(encounters).Count());
    }

    [Fact]
    public void AHiddenPlaceShowsOnceItsVariableHoldsItsMagicNumber()
    {
        // SystemVars_CheckHiddenLocation: Fullmoon Island's variable at 0x0208
        var story = new StoryState();
        Assert.False(PoketchMap.Shows(story, 0));
        story.SetVar("VAR_HIDDEN_LOCATION_FULL_MOON_ISLAND", 1);
        Assert.False(PoketchMap.Shows(story, 0));
        story.SetVar("VAR_HIDDEN_LOCATION_FULL_MOON_ISLAND", 0x0208);
        Assert.True(PoketchMap.Shows(story, 0));
        Assert.Equal((1, 8), PoketchMap.HiddenCell(0));
    }

    // ================================================================== the Trainer Counter

    private static Map Grass()
    {
        var map = new Map(32, 32);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
                map.SetGroundTile(x, y, TileType.TallGrass);
        return map;
    }

    // A chain of one species grown by so many Pokémon met in its patches, the records followed after each
    private static void Grow(RadarChain radar, Map map, Poketch poketch, string species, int times, Random rng)
    {
        var table = new List<WildEncounterEntry> { new() { SpeciesName = species, MinLevel = 5, MaxLevel = 5, Weight = 100 } };
        for (int i = 0; i < times; i++)
        {
            if (!radar.Active) Assert.True(radar.Spawn(map, 10, 10, 0, rng));
            var patch = radar.Patches.First(p => p.Active);
            var step = radar.StepOnto(patch.X, patch.Y)!.Value;
            radar.Meet(table, null, step, null, map, patch.X, patch.Y, 0, rng);
            TrainerCounterApp.Follow(poketch, radar);
        }
    }

    [Fact]
    public void TheCounterReadsTheRadarsChain()
    {
        var (poketch, app) = Open<TrainerCounterApp>(PoketchApp.TrainerCounter);
        var radar = new RadarChain();
        Assert.Null(TrainerCounterApp.Active(radar));
        Grow(radar, Grass(), poketch, "Bidoof", 3, new Random(5));
        Assert.Equal(("Bidoof", 3), TrainerCounterApp.Active(radar));
        Assert.Equal(new ChainRecord(3, "Bidoof"), TrainerCounterApp.Best(poketch)[0]);

        // The app on the screen follows the chain by itself
        var context = Context(poketch);
        context.Radar = radar;
        Grow(radar, Grass(), new Poketch(), "Bidoof", 1, new Random(6));
        app.Update(0.1f, context);
        Assert.Equal(new ChainRecord(4, "Bidoof"), TrainerCounterApp.Best(poketch)[0]);

        radar.Clear();
        Assert.Null(TrainerCounterApp.Active(radar));
    }

    [Fact]
    public void AChainTakesTheLowestRecordAndTheRecordsStayBestFirst()
    {
        // GetLowestChainRecordSlot and TryReplaceLowestChainRecord, worked through: Bidoof 3, Starly 5, Shinx 2 fill
        // the three; Kricketot then takes the lowest (Shinx's), passes it at 3 and Bidoof's at 4
        var poketch = new Poketch();
        var radar = new RadarChain();
        var map = Grass();
        var rng = new Random(9);
        Grow(radar, map, poketch, "Bidoof", 3, rng);
        radar.Clear();
        TrainerCounterApp.Follow(poketch, radar);
        Grow(radar, map, poketch, "Starly", 5, rng);
        radar.Clear();
        TrainerCounterApp.Follow(poketch, radar);
        Grow(radar, map, poketch, "Shinx", 2, rng);
        Assert.Equal(new[] { new ChainRecord(5, "Starly"), new ChainRecord(3, "Bidoof"), new ChainRecord(2, "Shinx") }, TrainerCounterApp.Best(poketch));

        radar.Clear();
        TrainerCounterApp.Follow(poketch, radar);
        Grow(radar, map, poketch, "Kricketot", 2, rng);
        Assert.Equal(new ChainRecord(2, "Shinx"), TrainerCounterApp.Best(poketch)[2]);
        Grow(radar, map, poketch, "Kricketot", 1, rng);
        Assert.Equal(new[] { new ChainRecord(5, "Starly"), new ChainRecord(3, "Bidoof"), new ChainRecord(3, "Kricketot") }, TrainerCounterApp.Best(poketch));
        Grow(radar, map, poketch, "Kricketot", 1, rng);
        Assert.Equal(new[] { new ChainRecord(5, "Starly"), new ChainRecord(4, "Kricketot"), new ChainRecord(3, "Bidoof") }, TrainerCounterApp.Best(poketch));

        // The records outlast a save
        var loaded = new Poketch();
        loaded.Load(poketch.Save());
        Assert.Equal(TrainerCounterApp.Best(poketch), TrainerCounterApp.Best(loaded));
    }

    [Fact]
    public void ARecordTouchedCries()
    {
        var (poketch, app) = Open<TrainerCounterApp>(PoketchApp.TrainerCounter);
        var cries = new List<string>();
        var context = Context(poketch);
        context.Cry = p => cries.Add(p.Species.Name);

        // No records yet: nothing to touch (RadarChainRecords_GetNumFilledSlots is nought)
        Assert.Equal(0, TrainerCounterApp.Filled(poketch));
        app.Press(0, context);
        Assert.Empty(cries);

        Grow(new RadarChain(), Grass(), poketch, "Starly", 2, new Random(1));
        Assert.Equal(TrainerCounterApp.Records, TrainerCounterApp.Filled(poketch));
        app.Press(0, context);
        Assert.Equal(new[] { "Starly" }, cries);
        Assert.True(app.Hop(0) >= 0f);

        // While it hops, and on a record with no chain, nothing
        app.Press(0, context);
        app.Update(TrainerCounterApp.HopSeconds, context);
        app.Press(1, context);
        Assert.Single(cries);
    }
}
