using System.Diagnostics;
using OpenWorldBake;
using PokemonPlatinumEngine.OpenWorld;

partial class Harness
{
    // ---------------------------------------------------------------- plan 32 · OW8, the open world's renderer I

    OpenMap? openLab;

    /// <summary>
    /// The open test world (tools/OpenWorldBake/worlds/lab) as the game knows it, baked by the harness itself into its
    /// out dir (<c>BuildOpenLab</c>, plan 32's "The tools"): its places from its files and its land from the bake, which
    /// is baked again only when its descriptions changed (the index's hash of them). The game lays it in by the
    /// Driver, as OW5's tests do.
    /// </summary>
    OpenMap BuildOpenLab()
    {
        string folder = RegionFolders.Find("lab") ?? throw new DirectoryNotFoundException("no tools/OpenWorldBake/worlds/lab");
        var description = RegionDescription.Read(folder);
        string baked = Path.Combine(outDir, "openlab", "baked");
        string index = Path.Combine(baked, "index.json");
        bool fresh = File.Exists(index) && BakedRegion.Open(baked).Index.Source == description.Source;
        var watch = Stopwatch.StartNew();
        if (!fresh)
        {
            var files = RegionBake.Files(description);
            RegionBake.Write(baked, files);
            Console.WriteLine($"  the lab baked into {baked}: {files.Count} files in {watch.Elapsed.TotalSeconds:0.0} s");
        }
        else Console.WriteLine($"  the lab's bake in {baked} is the descriptions' own (hash {description.Source}): not baked again");
        var map = OpenPlaceFiles.LoadRegion(folder, withLand: false).Single();
        map.Land = BakedRegion.Open(baked);
        game.AddOpenMap(map);
        return map;
    }

    /// <summary>A place on the lab's ground: the highest ground facing up between two heights at x, z.</summary>
    static OpenPos OnLabGround(OpenMap map, double x, double z, double top = 60, double bottom = -60) =>
        OpenPos.FromMetres(x, map.Ground.Footing(x, z, top, bottom)?.Y ?? 0, z);

    /// <summary>
    /// The player put down at a place facing a way (degrees: nought south, 90 east), settled, the camera turned to a yaw
    /// of its own if given, and (plan 32 · OW9) every far figure's impostor baked unless <paramref name="bake"/> is false
    /// (a shot that must be taken within a few frames of the placing: a trainer's "!").
    /// </summary>
    void PutInLab(OpenMap map, OpenPos at, double yawDegrees, double? cameraDegrees = null, bool bake = true)
    {
        game.OpenView = null;
        game.PlaceOpen(map, at, OpenPlaceFiles.YawOfDegrees(yawDegrees));
        Frames(2);
        if (cameraDegrees is { } c) game.OpenCamera.Face(c * Math.PI / 180);
        game.SettleOpenView();
        Frames(2);
        if (bake) BakeTheFiguresImpostors();
    }

    /// <summary>What a shot of the open world shows, printed: the place, the area, the camera, the light and what was drawn.</summary>
    void TellOpenShot(string name)
    {
        var field = game.OpenField!;
        var player = field.Player;
        var camera = game.OpenCamera;
        var renderer = game.OpenRenderer!;
        var area = field.Map.AreaAt(player.Pos);
        var zone = field.Map.ZoneAt(player.Pos);
        var cam = renderer.Camera;
        Console.WriteLine($"  {name}: tick {field.Clock.Tick}, feet {player.Pos}, yaw {player.Yaw} ({player.Yaw * 360.0 / 65536:0}°), "
            + $"{(area != null ? $"area {area.Key}" : "no area")}{(zone != null ? $", zone {zone.Name}" : "")}, {player.Body.Stance}, hour {GameClock.Hour:0.0}, weather {game.OpenWeatherHere}");
        Console.WriteLine($"    camera at ({cam.Position.X:0.00}, {cam.Position.Y:0.00}, {cam.Position.Z:0.00}) looking at ({cam.Target.X:0.00}, {cam.Target.Y:0.00}, {cam.Target.Z:0.00}), "
            + (game.OpenView != null ? "placed by hand" : $"yaw {camera.Yaw * 180 / Math.PI:0}°, arm {camera.Arm:0.00} m{(camera.Held ? " (held in)" : "")}{(camera.Looking != null ? $", looking at a marker {camera.LookShare:0.00} of the way" : "")}")
            + $"; under rock {(game.OpenUnderRock.Under ? "yes" : "no")}, the cave's share {renderer.CaveShare:0.00}");
        Console.WriteLine($"    drawn: chunks on the card {string.Join("/", renderer.ChunksAt)} at the three levels, {renderer.ChunksDrawn} in view ({renderer.TerrainTriangles} triangles), "
            + $"water {renderer.WaterTriangles} triangles{(renderer.Mirrored ? $", mirrored about {renderer.MirrorPlane:0.000} m" : ", no mirror")}, "
            + $"shadow casters {string.Join("/", renderer.ShadowTriangles)} triangles, {renderer.WeatherPieces} pieces of weather, the player {(renderer.PlayerDrawn ? "drawn" : "!! not drawn")}; "
            + $"the scene at {renderer.Step:0.000} of the target ({game.RenderContext.PartWidth}×{game.RenderContext.PartHeight})");
    }

    /// <summary>
    /// `profile`'s open_lab scenes (plan 32 · OW8): the game's own open renderer on the open test world, the player on
    /// its west road looking east over the meadow, the hill, the river and its bridge (the most of the lab in view), by
    /// day and in rain, and in the cavern under the hill, each at 1920×1080 in the preset's target. Laid in the first time
    /// a preset times them; the player is put back on the grid afterwards.
    /// </summary>
    void OpenLabProfile()
    {
        const string label = "open_lab";
        var only = (Environment.GetEnvironmentVariable("SHOTS_ONLY") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        bool look = Environment.GetEnvironmentVariable("SHOTS_LOOK") == "1";
        if (only.Length > 0 && !only.Any(word => label.Contains(word, StringComparison.OrdinalIgnoreCase)) && !look) return;
        var map = openLab ??= BuildOpenLab();
        var (stood, x, y, facing) = (game.Map, game.Player.GridX, game.Player.GridY, game.Player.Facing);
        game.OpenWeather = null;
        PutInLab(map, map.Marker("west_road")!.Pos, 90, 90);
        Profile("open_lab: the meadow from the west road, by day");
        game.OpenWeather = FieldWeather.Rain;
        PutInLab(map, map.Marker("west_road")!.Pos, 90, 90);
        Profile("open_lab: the meadow from the west road, in rain");
        game.OpenWeather = null;
        PutInLab(map, map.Marker("cavern_middle")!.Pos, 90, 60);
        Profile("open_lab: the cavern under the hill");
        // Plan 32 · OW9's done-when: thirty people, twelve field Pokémon and the village in view, by day
        LayTheVillage(map);
        PutInLab(map, OnLabGround(map, -14, -24), 180, 200);
        Profile("open_lab: the village with thirty people and twelve Pokémon");
        game.OpenFiguresByHand = null;
        game.Place(stood, x, y, facing);
    }

    double TimeOpenLab(int frames = 30)
    {
        Frames(1);
        var watch = Stopwatch.StartNew();
        Frames(frames);
        return watch.Elapsed.TotalMilliseconds / frames;
    }

    void OpenLabShot(string name)
    {
        if (!Wanted(name)) return;
        double ms = TimeOpenLab();
        Shot(name);
        TellOpenShot(name);
        TellOpenScenery(name);
        Console.WriteLine($"    frame {ms:0.00} ms (a mean of 30 on this machine, a card other runs share: the user's `profile` is what is judged)");
    }

    /// <summary>
    /// Plan 32 · OW8's mode (not part of <c>all</c>): the open test world drawn by the game's own open renderer through
    /// the engine's own Draw, the player put down on the lab's markers and ground with the orbit camera behind them: the
    /// meadow from four yaws by day, the hill, the tunnel and the cavern under the hill (the cave's light), the river by
    /// its rock bridge and the pond (the water and its mirror), after dark, in rain, a script's <c>camera look</c> at a
    /// marker, the cave's light easing in at the tunnel's mouth, and the land from over it with its chunks' levels
    /// (<c>open00</c> to <c>open42</c>). It prints at each the tick, the feet, the yaw, the area and zone, the camera, the
    /// cave's share, what was drawn and a frame's mean on this machine, which only the user's own <c>profile</c> judges.
    /// </summary>
    public void OpenMode()
    {
        var map = openLab ??= BuildOpenLab();
        Console.WriteLine($"  the open test world: {map.Land!.Index.Chunks.Count} chunks at {map.Land.Levels} levels, {map.Markers.Count} markers, {map.Areas.Count} areas; the player as {game.OpenPlayerLook}");
        game.OpenWeather = null;
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // By day, from the west road: four yaws
        var west = map.Marker("west_road")!;
        string[] ways = { "south", "east", "north", "west" };
        for (int k = 0; k < 4; k++)
        {
            PutInLab(map, west.Pos, 90, k * 90);
            OpenLabShot($"open00_meadow_day_{ways[k]}");
        }
        // The hill from the meadow's south-west, and from the north by the house
        PutInLab(map, OnLabGround(map, -34, 30), 135);
        OpenLabShot("open01_hill_day");
        PutInLab(map, map.Marker("house_path")!.Pos, 180, 180);
        OpenLabShot("open02_house_path_day");

        // The tunnel's west mouth, in the tunnel and the cavern under the hill: the cave's light
        PutInLab(map, OnLabGround(map, -40, 0, 4, -4), 90);
        OpenLabShot("open03_tunnel_mouth_day");
        PutInLab(map, OnLabGround(map, -22, 0, 4, -4), 90);
        OpenLabShot("open04_tunnel_day");
        PutInLab(map, OnLabGround(map, -12, 0, 4, -4), 90);
        OpenLabShot("open04b_in_the_tunnel_day");
        PutInLab(map, map.Marker("cavern_middle")!.Pos, 90, 60);
        OpenLabShot("open05_cavern_day");
        PutInLab(map, OnLabGround(map, 8, -2, 4, -4), 270, 240);
        OpenLabShot("open06_cavern_back_day");
        // The cavern from four yaws, the player in its middle
        for (int k = 0; k < 4; k++)
        {
            PutInLab(map, map.Marker("cavern_middle")!.Pos, k * 90, k * 90);
            OpenLabShot($"open06{(char)('a' + k)}_cavern_{ways[k]}");
        }

        // The river by its rock bridge, and the pond with its stack: the water and its mirror
        PutInLab(map, OnLabGround(map, 14, 37), 90, 110);
        OpenLabShot("open07_river_day");
        PutInLab(map, OnLabGround(map, 24, 44, 4, -4), 0, 0);
        OpenLabShot("open08_rock_bridge_day");
        PutInLab(map, OnLabGround(map, 30, -28), 130, 130);
        OpenLabShot("open09_pond_day");
        // The ledge of the plateau in the north-west, from its foot
        PutInLab(map, OnLabGround(map, -44, -34), 225, 200);
        OpenLabShot("open10_ledge_day");

        // After dark
        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        for (int k = 0; k < 4; k++)
        {
            PutInLab(map, west.Pos, 90, k * 90);
            OpenLabShot($"open20_meadow_night_{ways[k]}");
        }
        PutInLab(map, OnLabGround(map, -22, 0, 4, -4), 90);
        OpenLabShot("open21_tunnel_night");
        PutInLab(map, OnLabGround(map, 14, 37), 90, 110);
        OpenLabShot("open22_river_night");

        // In rain, by day
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
        game.OpenWeather = FieldWeather.Rain;
        for (int k = 0; k < 4; k++)
        {
            PutInLab(map, west.Pos, 90, k * 90);
            OpenLabShot($"open30_meadow_rain_{ways[k]}");
        }
        PutInLab(map, OnLabGround(map, 14, 37), 90, 110);
        OpenLabShot("open31_river_rain");
        PutInLab(map, OnLabGround(map, -22, 0, 4, -4), 90);
        OpenLabShot("open32_tunnel_rain");
        game.OpenWeather = FieldWeather.HeavySnow;
        PutInLab(map, west.Pos, 90, 90);
        OpenLabShot("open33_meadow_snow");
        game.OpenWeather = null;

        // A script's `camera look` from the west road at the tunnel's west mouth: half way, there, and come home
        PutInLab(map, west.Pos, 90, 180);
        var library = ScriptLibrary.FromSources(("lab_meadow", "script Look\n  camera look tunnel_west 1.0\n  wait 2.0\n  end\n"), ("common", ""));
        game.UseScripts(library);
        engine.StartScript("Look", file: "lab_meadow");
        // (Each of the look's shots is taken at once, with no frames of timing, which would let the camera ease on)
        Frames(30);
        if (Wanted("open34_look_half")) { Shot("open34_look_half"); TellOpenShot("open34_look_half"); }
        Frames(60);
        if (Wanted("open35_look_there")) { Shot("open35_look_there"); TellOpenShot("open35_look_there"); }
        for (int f = 0; f < 60 * 5 && engine.ScriptRunning; f++) Frames(1);
        Frames(60);
        if (Wanted("open36_look_home")) { Shot("open36_look_home"); TellOpenShot("open36_look_home"); }
        game.UseScripts(ScriptLibrary.Default);

        // Running into the tunnel's west mouth by the stick: the cave's light easing in as the camera goes under rock
        PutInLab(map, OnLabGround(map, -46, 0, 4, -4), 90, 90);
        var run = new ActionFrame(ActionFrame.Stick(127, 0).Down | ActionFrame.Press(GameAction.Run).Down, 0) { MoveX = 127, MoveY = 0 };
        for (int f = 0; f < 60 * 10 && !(game.OpenUnderRock.Under && game.OpenUnderRock.Share > 0.45); f++)
        {
            InputManager.Feed(run);
            Frames(1);
        }
        InputManager.Feed(null);
        if (Wanted("open37_into_the_tunnel"))
        {
            // Shot at once, the cave's light part of the way in (no frames of timing, which would let it ease on)
            Shot("open37_into_the_tunnel");
            TellOpenShot("open37_into_the_tunnel");
        }

        // The land from over it, the chunks' levels round the player on the hill's top
        PutInLab(map, map.Marker("hill_top")!.Pos, 180, 200);
        game.OpenView = (new Vector3(-70, 38, 70), new Vector3(0, 2, 0));
        OpenLabShot("open40_overview_day");
        game.OpenView = (new Vector3(60, 46, -60), new Vector3(0, 0, 0));
        OpenLabShot("open41_overview_north_east");
        PutInLab(map, OnLabGround(map, 52, 52), 315);
        game.OpenView = (new Vector3(-120, 140, 120), new Vector3(0, 0, 0));
        OpenLabShot("open42_levels_from_the_corner");
        game.OpenView = null;
        game.OpenWeather = null;

        // Plan 32 · OW9: the buildings, trees, grass, people, follower, the rings to the horizon and the village's crowd
        OpenSceneryShots(map);
    }
}
