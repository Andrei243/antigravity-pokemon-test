using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Raylib_cs;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The map of the region (plan 01 · M12): Sinnoh's file, the rules of what a game has put on the map (the original's
/// <c>src/applications/town_map</c>), the way come, the one screen the Town Map, Fly and a wall's map are, and the
/// picture painted from the world's own tiles.
/// </summary>
public class RegionMapTests
{
    // Maps of this class's own, so nothing another test does to the database's maps shows here. Read only.
    private static readonly Lazy<Func<string, Map>> Maps = new(MapDatabase.Fresh);

    private static RegionMap Sinnoh => RegionMap.Sinnoh ?? throw new InvalidOperationException("Sinnoh has no map");
    private static Map Overworld => Maps.Value("Sinnoh");

    private static TownMapState State(StoryState story, (int, int)? here = null, MapTrail? trail = null) => new(Sinnoh, story, here, trail);

    // ------------------------------------------------------------------ the file

    [Fact]
    public void SinnohsMapIsAGridOfTheOverworldsChunksWithTheOriginalsBoundsForItsCursor()
    {
        var map = Sinnoh;
        Assert.Equal("Sinnoh", map.Map);
        Assert.Equal((Overworld.ChunkColumns, Overworld.ChunkRows), (map.Columns, map.Rows));
        // HandleInput: the cursor goes from column 1 to 28 and from row 6 to 28; with nowhere to stand it starts on
        // Twinleaf Town (InitDefaultAppResources: 3, 27)
        Assert.Equal((1, 6, 28, 28), map.Bounds);
        Assert.Equal((3, 27), map.Start);
        Assert.Equal("Twinleaf Town", map.BlockAt(3, 27)!.Name);
        // The empty north is left out, and every block lies where the cursor can come
        Assert.All(map.Blocks, b => Assert.True(map.InBounds(b.X, b.Y), $"{b.Name} at {b.X},{b.Y}"));
        Assert.True(map.Blocks.Count >= 180, $"{map.Blocks.Count} blocks");
        Assert.Equal(4, map.Hidden.Count);
    }

    [Fact]
    public void EveryBlockIsNamedAsItsPlaceOnTheMapOfSinnohIs()
    {
        foreach (var block in Sinnoh.Blocks)
        {
            var area = Overworld.AreaAt(block.X * Map.ChunkTiles + 1, block.Y * Map.ChunkTiles + 1);
            if (area is { Open: true }) Assert.Equal(area.DisplayName, block.Name);
            // LoadMapName's table: the cells of no area that the map still names
            else Assert.Contains(block.Name, new[] { "Mt. Coronet", "Battle Frontier" });
        }
    }

    [Fact]
    public void EveryPlaceOfTheMapIsOnItAndHasSomethingToSayThatTheFontCanWrite()
    {
        var map = Sinnoh;
        var used = map.Blocks.Select(b => b.Area).Concat(map.Blocks.Select(b => b.Landmark).OfType<RegionPlace>()).ToHashSet();
        foreach (var place in map.Areas.Concat(map.Landmarks))
        {
            Assert.True(used.Contains(place), $"{place.Key} is on no block");
            Assert.False(string.IsNullOrWhiteSpace(place.About), place.Key);
            // The interface's font is Latin-1; anything else is drawn as a question mark
            Assert.All(place.About + place.Name, c => Assert.True(c < 256, $"{place.Key}: '{c}'"));
            Assert.True(place.About.Length <= 110, $"{place.Key}: {place.About.Length} characters won't fit three lines");
        }
        Assert.All(map.Landmarks, l => Assert.False(string.IsNullOrWhiteSpace(l.Name), l.Key));
        Assert.All(map.Blocks, b => Assert.False(string.IsNullOrWhiteSpace(b.Name)));
        // A hidden place has its blocks, and nothing else is hidden
        foreach (var hidden in map.Hidden) Assert.Contains(map.Blocks, b => b.Hidden == hidden);
    }

    [Fact]
    public void EveryFlagThePlacesWaitForIsSetBySomethingInTheGame()
    {
        // A script of ours (the original's own map scripts set them), the arrival in a town Fly goes to, or the
        // arrival the map's file names for it
        var written = ScriptLibrary.Default.FlagsWritten;
        var arrivals = SpawnLocations.All.Where(s => s.UnlockedOnArrival).Select(s => s.ArrivalFlag).ToHashSet();
        foreach (var place in Sinnoh.Areas.Concat(Sinnoh.Landmarks))
        {
            if (place.Flag == null) continue;
            Assert.True(written.Contains(place.Flag) || arrivals.Contains(place.Flag) || place.Arrival.Count > 0, $"nothing sets {place.Flag} ({place.Key})");
            // And of the places named as its arrival, one at least is a room of the game or an open area of its world
            // (the Ruin Maniac's longer cave and his tunnel are named too, for when the story has dug them)
            if (place.Arrival.Count > 0)
                Assert.Contains(place.Arrival, where => MapDatabase.MapNames.Contains(where) || MapDatabase.MapNames.Select(Maps.Value).Any(m => m.FindArea(where) is { Open: true }));
        }
        Assert.Equal(new[] { "FLAG_FIRST_ARRIVAL_OREBURGH_GATE" }, Sinnoh.ArrivalFlags("oreburgh_gate_1f"));
        Assert.Equal(new[] { "FLAG_FIRST_ARRIVAL_POKE_PARK_FRONT_GATE" }, Sinnoh.ArrivalFlags("PalParkLobby"));
        Assert.Empty(Sinnoh.ArrivalFlags("twinleaf_town"));
    }

    [Fact]
    public void FlysPlacesAreTheSpawnLocationsOnTheCellsOfTheirTowns()
    {
        var map = Sinnoh;
        // Every place Fly lands (the original's twenty) is on the map once, on its own cell among its cells
        Assert.Equal(SpawnLocations.All.Select(s => s.Id), map.FlySpots.Select(f => f.Spawn));
        foreach (var spot in map.FlySpots)
        {
            var town = SpawnLocations.Get(spot.Spawn)!;
            Assert.Contains(RegionMap.CellOf(town.X, town.Y), spot.Cells);
            // A town's cells are those of its area; the three with a place of their own (sFlyLocations) have one
            if (town.UnlockedOnArrival) Assert.All(spot.Cells, c => Assert.Equal(town.Area, Overworld.AreaAt(c.X * 32, c.Y * 32)?.Key));
            else Assert.Single(spot.Cells);
        }
        // The shapes sFlyLocations gives: Jubilife City's square of four, Oreburgh City's and Eterna City's corners of
        // three, Floaroma Town upright, Solaceon Town lying
        (int, int)[] CellsOf(string area) => map.FlySpots.Single(f => SpawnLocations.Get(f.Spawn)!.Area == area).Cells.ToArray();
        Assert.Equal(new[] { (4, 23), (5, 23), (4, 24), (5, 24) }, CellsOf("jubilife_city"));
        Assert.Equal(new[] { (8, 23), (9, 23), (9, 24) }, CellsOf("oreburgh_city"));
        Assert.Equal(new[] { (9, 16), (10, 16), (9, 17) }, CellsOf("eterna_city"));
        Assert.Equal(new[] { (5, 19), (5, 20) }, CellsOf("floaroma_town"));
        Assert.Equal(new[] { (17, 20), (18, 20) }, CellsOf("solaceon_town"));
        // Pal Park and the foot of Victory Road are not drawn until they are open (TownMap_LoadFlyLocations)
        Assert.Equal(new[] { 15, 19 }, map.FlySpots.Where(f => f.Unseen).Select(f => f.Spawn));
    }

    [Fact]
    public void AFileThatContradictsItselfIsRefusedWithItsFault()
    {
        RegionMapFile File() => new()
        {
            Map = "Sinnoh", Columns = 4, Rows = 4, View = new[] { 0, 0, 4, 4 }, Cursor = new[] { 0, 0, 3, 3 }, Start = new[] { 1, 1 },
            Areas = { new RegionPlaceRecord { Key = "a", About = "A." } },
            Blocks = { new RegionBlockRecord { X = 1, Y = 1, Name = "A", Area = "a" } }
        };
        Assert.Single(new RegionMap("test", File()).Blocks);
        var twice = File();
        twice.Blocks.Add(new RegionBlockRecord { X = 1, Y = 1, Name = "B", Area = "a" });
        Assert.Contains("two blocks", Assert.Throws<InvalidDataException>(() => new RegionMap("test", twice)).Message);
        var stray = File();
        stray.Blocks[0].Area = "b";
        Assert.Contains("'b'", Assert.Throws<InvalidDataException>(() => new RegionMap("test", stray)).Message);
        var outside = File();
        outside.Start = new[] { 9, 9 };
        Assert.Contains("starts outside", Assert.Throws<InvalidDataException>(() => new RegionMap("test", outside)).Message);
        var nowhere = File();
        nowhere.Fly.Add(new RegionFlyRecord { Spawn = 1, Cells = "2,2" });
        Assert.Contains("no block", Assert.Throws<InvalidDataException>(() => new RegionMap("test", nowhere)).Message);
    }

    // ------------------------------------------------------------------ what a game has put on the map

    [Fact]
    public void AHiddenPlaceIsOffTheMapUntilItsVariableHoldsItsNumber()
    {
        // SystemVars_CheckHiddenLocation and TownMap_GetMapBlockAtPosition: no block, so no name and nothing told
        var story = new StoryState();
        var island = Sinnoh.Blocks.Single(b => b.Hidden?.Key == "fullmoon_island");
        Assert.Null(State(story).BlockAt(island.X, island.Y));
        Assert.Empty(State(story).RevealedPlaces);
        story.SetVar("VAR_HIDDEN_LOCATION_FULL_MOON_ISLAND", 0x0208);
        Assert.Same(island, State(story).BlockAt(island.X, island.Y));
        Assert.Equal("Fullmoon Island", island.Name);
        // The other three are still hidden; any other number reveals nothing
        Assert.Equal(new[] { "fullmoon_island" }, State(story).RevealedPlaces);
        story.SetVar("VAR_HIDDEN_LOCATION_SEABREAK_PATH", 1);
        Assert.DoesNotContain("seabreak_path", State(story).RevealedPlaces);
        story.SetVar("VAR_HIDDEN_LOCATION_SEABREAK_PATH", 0x1028);
        Assert.Equal(9, Sinnoh.Blocks.Count(b => b.Hidden?.Key == "seabreak_path" && State(story).BlockAt(b.X, b.Y) != null));
    }

    [Fact]
    public void ARouteIsAlwaysToldOfAndATownAndALandmarkOnceTheyHaveBeenComeTo()
    {
        // PrintLocationDescription: no check, or the check's flag is set
        var story = new StoryState();
        var route = Sinnoh.BlockAt(3, 26)!;
        var jubilife = Sinnoh.BlockAt(5, 23)!;
        var gate = Sinnoh.Blocks.Single(b => b.Landmark?.Key == "oreburgh_gate");
        Assert.Equal(RegionBlockKind.Route, route.Kind);
        Assert.True(State(story).Tells(route.Area));
        Assert.False(State(story).Tells(jubilife.Area));
        Assert.False(State(story).TellsLandmark(jubilife));
        Assert.False(State(story).TellsLandmark(gate));
        Assert.False(State(story).TellsLandmark(route));   // it has none

        story.Set("FLAG_FIRST_ARRIVAL_JUBILIFE_CITY");
        Assert.True(State(story).Tells(jubilife.Area));
        // Its landmarks wait for the same arrival; the gate on Route 203 for its own
        Assert.True(State(story).TellsLandmark(jubilife));
        Assert.True(State(story).Tells(gate.Area));
        Assert.False(State(story).TellsLandmark(gate));
        story.Set("FLAG_FIRST_ARRIVAL_OREBURGH_GATE");
        Assert.True(State(story).TellsLandmark(gate));

        // A landmark that waits for nothing of its own waits for what its area waits for (the cabin on Route 205: nothing)
        var cabin = Sinnoh.Blocks.Single(b => b.Landmark?.Key == "route_205_cabin");
        Assert.Null(cabin.Landmark!.Flag);
        Assert.True(State(new StoryState()).TellsLandmark(cabin));
    }

    [Fact]
    public void FlyLandsWhereThePlacesFlagIsSetAndTheMapDrawsThePlacesItShould()
    {
        var story = new StoryState();
        var none = State(story);
        Assert.DoesNotContain(none.Spots, s => s.Open);
        // Shut, a town is drawn all the same; Pal Park and Victory Road's foot are not (they have no cell to hover either)
        Assert.NotNull(none.SpotAt(5, 23));
        Assert.False(none.CanFlyTo(5, 23));
        Assert.Null(none.SpotAt(9, 28));
        Assert.Null(none.SpotAt(26, 18));
        Assert.NotNull(none.SpotAt(26, 17));

        story.Set(SpawnLocations.ArrivedIn("jubilife_city")!.ArrivalFlag);
        // Every cell of the city is its place (a regular fly location answers for its whole map header)
        Assert.All(new[] { (4, 23), (5, 23), (4, 24), (5, 24) }, c => Assert.True(State(story).CanFlyTo(c.Item1, c.Item2)));
        Assert.False(State(story).CanFlyTo(6, 23));

        // The League's two places share a town and have a cell each (TownMap_GetFlyLocationAtPos)
        story.Set("FLAG_FIRST_ARRIVAL_OUTSIDE_VICTORY_ROAD");
        Assert.Equal(15, State(story).SpotAt(26, 18)!.Town.Id);
        Assert.False(State(story).CanFlyTo(26, 17));
        story.Set("FLAG_FIRST_ARRIVAL_POKEMON_LEAGUE");
        Assert.Equal(20, State(story).SpotAt(26, 17)!.Town.Id);

        // Pal Park opens with its gate's flag, not with Route 221's arrival (sTownMapFlyLocationUnlockFlags)
        story.Set("FLAG_FIRST_ARRIVAL_ROUTE_221");
        Assert.Null(State(story).SpotAt(9, 28));
        story.Set("FLAG_FIRST_ARRIVAL_POKE_PARK_FRONT_GATE");
        Assert.Equal(SpawnLocations.PalParkLobby, State(story).SpotAt(9, 28)!.Town.Id);
        Assert.True(State(story).CanFlyTo(9, 28));

        // A place whose area isn't open is not flown to, whatever its flag
        var shut = new TownMapState(Sinnoh, story, reachable: town => town.Area != "jubilife_city");
        Assert.False(shut.CanFlyTo(5, 23));
        // And nothing lands on a hidden place's cell
        Assert.Null(State(story).SpotAt(1, 8));
    }

    [Fact]
    public void TheCursorStartsOnThePlayersCellOrAtTheMapsStart()
    {
        var story = new StoryState();
        Assert.Equal((3, 27), State(story).FirstCursor());
        Assert.Null(State(story).HereName);
        var here = State(story, RegionMap.CellOf(180, 777));
        Assert.Equal((5, 24), here.Here);
        Assert.Equal((5, 24), here.FirstCursor());
        Assert.Equal("Jubilife City", here.HereName);
        // A cell beyond the cursor's bounds is held to them (the bottom row of the view is row 28, the right column 28)
        Assert.Equal((28, 20), State(story, (29, 20)).FirstCursor());
        // Off the grid altogether there is no telling
        Assert.Null(State(story, (40, 2)).Here);
    }

    [Fact]
    public void ARoomIsShownAtTheCellItsWayInIsOn()
    {
        var places = Sinnoh.PlacesOf(MapDatabase.MapNames.Select(Maps.Value));
        // A Pokémon Center at its door's cell, its upper floor with it, and a cave's deepest floor at its mouth's
        Assert.Equal((5, 24), places["JubilifePokemonCenter"]);
        Assert.Equal((5, 24), places["JubilifePokemonCenter2F"]);
        Assert.Equal(RegionMap.CellOf(SpawnLocations.Get(9)!.X, SpawnLocations.Get(9)!.Y), places["EternaPokemonCenter"]);
        Assert.Equal(places["OreburghMineB1F"], places["OreburghMineB2F"]);
        Assert.Equal("Oreburgh City", Sinnoh.BlockAt(places["OreburghMineB2F"].X, places["OreburghMineB2F"].Y)!.Name);
        Assert.False(places.ContainsKey("Sinnoh"));
        // Every room of Sinnoh a warp leads into has a cell the map shows
        Assert.All(places.Values, c => Assert.True(Sinnoh.InView(c.X, c.Y)));
        Assert.True(places.Count >= 350, $"{places.Count} places");
    }

    // ------------------------------------------------------------------ the way come

    [Fact]
    public void TheWayComeIsTheFiveCellsBeforeThisOne()
    {
        var trail = new MapTrail();
        Assert.True(trail.IsEmpty);
        Assert.Empty(trail.Recent(null));
        // OverworldMapHistory_Push: a cell is written when it is another than the last
        trail.Push(3, 27, Direction.Up);
        trail.Push(3, 27, Direction.Left);
        trail.Push(3, 26, Direction.Up);
        trail.Push(4, 26, Direction.Right);
        // TownMapContext_Init skips the newest (where the player is) and reads back from there
        Assert.Equal(new[] { new TrailStep(3, 26, Direction.Up), new TrailStep(3, 27, Direction.Up) }, trail.Recent((4, 26)));

        // Six are kept and five shown: the oldest falls off
        foreach (var (x, y) in new[] { (5, 26), (5, 25), (5, 24), (5, 23), (6, 23) }) trail.Push(x, y, Direction.Up);
        var recent = trail.Recent((6, 23));
        Assert.Equal(MapTrail.Shown, recent.Count);
        Assert.Equal((5, 23), (recent[0].X, recent[0].Y));
        Assert.Equal((4, 26), (recent[^1].X, recent[^1].Y));

        // LoadLocationHistory leaves out the cell the player is in and shows a cell once, facing as its oldest writing says
        trail.Clear();
        trail.Push(1, 10, Direction.Down);
        trail.Push(2, 10, Direction.Right);
        trail.Push(1, 10, Direction.Left);
        trail.Push(2, 10, Direction.Up);
        trail.Push(3, 10, Direction.Right);
        Assert.Equal(new[] { new TrailStep(2, 10, Direction.Right), new TrailStep(1, 10, Direction.Down) }, trail.Recent((3, 10)));
        Assert.Equal(new[] { new TrailStep(1, 10, Direction.Down) }, trail.Recent((2, 10)));
    }

    [Fact]
    public void ComingOntoTheMapByADoorIsWrittenWithNoWayFacedAndTheFirstStepWritesTheCellAgain()
    {
        // OverworldMapHistory_PushViaWarp leaves the cell stood in as it was
        var trail = new MapTrail();
        trail.Push(3, 27, Direction.Up);
        trail.PushViaWarp(9, 16);
        trail.Push(9, 16, Direction.Down);
        trail.Push(9, 17, Direction.Down);
        Assert.Equal(new[] { new TrailStep(9, 16, null), new TrailStep(3, 27, Direction.Up) }, trail.Recent((9, 17)));
        // Coming out where one went in writes nothing
        trail.PushViaWarp(9, 17);
        Assert.Equal(2, trail.Recent((9, 17)).Count);
    }

    [Fact]
    public void TheWayComeIsKeptByTheSave()
    {
        var trail = new MapTrail();
        foreach (var (x, y, way) in new[] { (3, 27, Direction.Up), (3, 26, Direction.Up), (4, 26, Direction.Right) }) trail.Push(x, y, way);
        trail.PushViaWarp(9, 16);
        var save = new PokemonPlatinumEngine.Core.SaveData { MapTrail = trail.ToSave() };
        var back = GameDataFiles.Deserialize<PokemonPlatinumEngine.Core.SaveData>(GameDataFiles.Serialize(save));
        var loaded = new MapTrail();
        loaded.Restore(back.MapTrail);
        Assert.Equal(trail.Recent(null), loaded.Recent(null));
        // The ring's hand is kept too: the next cell lands where it would have
        trail.Push(9, 17, Direction.Down);
        loaded.Push(9, 17, Direction.Down);
        Assert.Equal(trail.Recent((9, 17)), loaded.Recent((9, 17)));
        // A save from before it, or of another shape, has no way come
        loaded.Restore(null);
        Assert.True(loaded.IsEmpty);
        loaded.Restore(new[] { 1, 2, 3 });
        Assert.True(loaded.IsEmpty);
    }

    // ------------------------------------------------------------------ the screen

    [Fact]
    public void TheCursorMovesACellAtATimeBothWaysAtOnceAndStopsAtTheMapsBounds()
    {
        var screen = new TownMapScreen();
        screen.Open(State(new StoryState(), (3, 27)), TownMapMode.Item);
        Assert.True(screen.IsActive);
        Assert.Equal((3, 27), (screen.CursorX, screen.CursorY));
        Assert.Equal("Twinleaf Town", screen.Hovered!.Name);
        Assert.True(screen.Move(1, -1));
        Assert.Equal((4, 26), (screen.CursorX, screen.CursorY));
        // HandleInput: up while the row is 7 or more, down while it is 27 or less, right while the column is 27 or
        // less, left while it is 2 or more
        for (int i = 0; i < 40; i++) screen.Move(-1, 1);
        Assert.Equal((1, 28), (screen.CursorX, screen.CursorY));
        Assert.False(screen.Move(-1, 0));
        Assert.False(screen.Move(0, 1));
        for (int i = 0; i < 40; i++) screen.Move(1, -1);
        Assert.Equal((28, 6), (screen.CursorX, screen.CursorY));
        Assert.Null(screen.Hovered);   // open sea
        screen.Cancel();
        Assert.False(screen.IsActive);
        Assert.Null(screen.TakeChoice());
    }

    [Fact]
    public void LookingAtTheMapTheButtonBringsItNearerAndTheNearMapKeepsTheCursorInSight()
    {
        var screen = new TownMapScreen();
        screen.Open(State(new StoryState(), (15, 21)), TownMapMode.Item);
        Assert.False(screen.Zoomed);
        screen.Confirm();
        Assert.True(screen.Zoomed);
        Assert.True(screen.IsActive);
        // Ten cells by eight are shown, the cursor's cell in their middle: (15.5 - 5, 21 - 5 + 0.5 - 4)
        Assert.Equal((10.5f, 12.5f), screen.ZoomTarget());
        // At the map's edge the window stops and the cursor goes on to the corner
        for (int i = 0; i < 40; i++) screen.Move(-1, -1);
        Assert.Equal((0f, 0f), screen.ZoomTarget());
        for (int i = 0; i < 40; i++) screen.Move(1, 1);
        Assert.Equal((20f, 16f), screen.ZoomTarget());
        // What is drawn follows it within a moment
        for (int i = 0; i < 60; i++) screen.Advance(1f / 60f);
        Assert.Equal(20f, screen.ZoomShown.X, 2);
        Assert.Equal(16f, screen.ZoomShown.Y, 2);
        screen.ToggleZoom();
        Assert.False(screen.Zoomed);
        // On a wall's map too
        screen.Open(State(new StoryState()), TownMapMode.Wall);
        screen.Confirm();
        Assert.True(screen.Zoomed);
    }

    [Fact]
    public void TheWayComeIsShownACellAtATimeFromTheOldestAndNotOnAWall()
    {
        var trail = new MapTrail();
        foreach (var (x, y) in new[] { (11, 22), (12, 22), (13, 22), (14, 22) }) trail.Push(x, y, Direction.Right);
        var state = State(new StoryState(), (14, 22), trail);
        Assert.Equal(3, state.Trail.Count);
        var screen = new TownMapScreen();
        screen.Open(state, TownMapMode.Item);
        // Task_UpdateShownLocationHistoryIdx: the oldest first, the next every 26 frames of thirty a second, and round
        Assert.Equal(2, screen.TrailShown);
        screen.Advance(TownMapScreen.TrailStepTime + 0.01f);
        Assert.Equal(1, screen.TrailShown);
        screen.Advance(TownMapScreen.TrailStepTime);
        Assert.Equal(0, screen.TrailShown);
        screen.Advance(TownMapScreen.TrailStepTime);
        Assert.Equal(2, screen.TrailShown);
        screen.Open(state, TownMapMode.Wall);
        Assert.Equal(-1, screen.TrailShown);
        screen.Open(State(new StoryState()), TownMapMode.Fly);
        Assert.Equal(-1, screen.TrailShown);
    }

    // ------------------------------------------------------------------ the picture

    private static PixelCanvas Painted(params string[] revealed) =>
        RegionMapArt.Paint(Overworld, Sinnoh, (x, y) => Habitats.Sinnoh!.LookAt(x, y) != ' ', revealed.ToHashSet());

    private static readonly Lazy<PixelCanvas> Picture = new(() => Painted());

    private static IEnumerable<Color> CellOf(PixelCanvas picture, int cx, int cy)
    {
        var view = Sinnoh.View;
        for (int y = 0; y < RegionMapArt.CellTexels; y++)
            for (int x = 0; x < RegionMapArt.CellTexels; x++)
                yield return picture.Get((cx - view.X) * RegionMapArt.CellTexels + x, (cy - view.Y) * RegionMapArt.CellTexels + y);
    }

    private static bool IsSea(Color c) =>
        c.Equals(RegionMapArt.SeaDeep) || c.Equals(RegionMapArt.SeaMid) || c.Equals(RegionMapArt.SeaShallow) || c.Equals(RegionMapArt.Shore);

    [Fact]
    public void ThePictureIsATexelToATileOfWhatTheViewShowsAndTheSameEveryTime()
    {
        var picture = Picture.Value;
        Assert.Equal((Sinnoh.View.Width * Map.ChunkTiles, Sinnoh.View.Height * Map.ChunkTiles), (picture.Width, picture.Height));
        var again = Painted();
        for (int y = 0; y < picture.Height; y += 3)
            for (int x = 0; x < picture.Width; x += 3)
            {
                var c = picture.Get(x, y);
                Assert.Equal(255, c.A);
                Assert.Equal(c, again.Get(x, y));
            }
        // The half-size copy is half as large each way
        var half = RegionMapArt.Halved(picture);
        Assert.Equal((picture.Width / 2, picture.Height / 2), (half.Width, half.Height));
    }

    [Fact]
    public void ThePictureShowsTheWorldItIsPaintedFrom()
    {
        var picture = Picture.Value;
        // The far corners are open sea, though the world has no chunk there at all
        Assert.All(CellOf(picture, 15, 6), c => Assert.True(IsSea(c)));
        Assert.All(CellOf(picture, 1, 12), c => Assert.True(IsSea(c)));
        // A town's tiles are its own: paving and roofs where Jubilife City has them, and its streets' pale grey
        var jubilife = CellOf(picture, 4, 23).ToList();
        Assert.DoesNotContain(jubilife, IsSea);
        Assert.Contains(jubilife, c => Math.Abs(c.R - c.G) < 12 && c.R > 180 && c.B > c.R);
        // Route 219 south of Sandgem Town is a beach: water and sand
        var beach = CellOf(picture, 5, 27).ToList();
        Assert.Contains(beach, IsSea);
        Assert.Contains(beach, c => c.R > 220 && c.G > 200 && c.B < 200);
        // The heights of Mt. Coronet are under snow; no forest grows on its spine
        Assert.Contains(CellOf(picture, 12, 17), c => c.R > 200 && c.G > 210 && c.B > 225);
        // Land with no chunk under it that the sea can't come to is land (the hollow inside the mountains)
        Assert.DoesNotContain(CellOf(picture, 13, 19), IsSea);
    }

    [Fact]
    public void AHiddenPlaceIsOpenSeaInThePictureUntilItIsRevealed()
    {
        var island = Sinnoh.Blocks.Single(b => b.Hidden?.Key == "fullmoon_island");
        Assert.All(CellOf(Picture.Value, island.X, island.Y), c => Assert.True(IsSea(c)));
        var revealed = Painted("fullmoon_island");
        Assert.Contains(CellOf(revealed, island.X, island.Y), c => !IsSea(c));
        // The other island stays out of it
        var other = Sinnoh.Blocks.Single(b => b.Hidden?.Key == "newmoon_island");
        Assert.All(CellOf(revealed, other.X, other.Y), c => Assert.True(IsSea(c)));
        // The Seabreak Path and Flower Paradise at its end
        var paradise = Sinnoh.Blocks.Single(b => b.Area.Key == "flower_paradise");
        Assert.All(CellOf(Picture.Value, paradise.X, paradise.Y), c => Assert.True(IsSea(c)));
        Assert.Contains(CellOf(Painted("seabreak_path"), paradise.X, paradise.Y), c => !IsSea(c));
    }
}
