using PokemonPlatinumEngine.Core.Autoplay;
using PokemonPlatinumEngine.Graphics.OpenWorld;
using PokemonPlatinumEngine.OpenWorld;

partial class Harness
{
    // ---------------------------------------------------------------- plan 32 · OW10, the tools whole

    /// <summary>
    /// The end of <c>open</c> (plan 32 · OW10): the open world's rules as layers over the lab, one layer a shot and all
    /// of them at once (<c>open80_layers_*</c>), printing what each drew; the house's door gone through into its grid
    /// room and back out onto its step (<c>open85</c>, <c>open86</c>); the playthrough bot walking the lab by its
    /// navigation mesh from the house's path through the tunnel into the cavern, whose trigger runs its scene
    /// (<c>open87</c>, <c>open88</c>); the camera posed as a scenario's <c>look</c> poses it (<c>open89</c>); and the Town
    /// Map's picture of the lab painted from its bake (<c>open90_townmap_picture</c>, four times its size).
    /// <c>OPEN_FROM=tools</c> shoots this part alone.
    /// </summary>
    void OpenToolShots(OpenMap map)
    {
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
        game.OpenWeather = null;

        // The layers, one at a time, each where it has the most to show, then all of them
        var shots = new (string Name, DebugLayer Layer, OpenPos At, double Yaw, double Camera, (Vector3 Eye, Vector3 Target)? View)[]
        {
            ("solid", DebugLayer.Solid, OnLabGround(map, -40, 0, 4, -4), 90, 60, null),
            ("nav", DebugLayer.Nav, OnLabGround(map, -44, -34), 225, 200, (new Vector3(-26, 22, -18), new Vector3(-46, 0, -42))),
            ("triggers", DebugLayer.Triggers, map.Marker("cavern_middle")!.Pos, 90, 60, null),
            ("warps", DebugLayer.Warps, map.Marker("house_path")!.Pos, 180, 180, null),
            ("sight", DebugLayer.Sight, map.Marker("north_meadow")!.Pos, 90, 90, (new Vector3(38, 34, -24), new Vector3(38, 0, -58))),
            ("areas", DebugLayer.Areas, map.Marker("hill_top")!.Pos, 180, 200, (new Vector3(-70, 38, 70), new Vector3(0, 2, 0))),
            ("encounters", DebugLayer.Encounters, OnLabGround(map, 24, 22), 0, 0, (new Vector3(24, 20, 48), new Vector3(24, 0, 22))),
            ("chunks", DebugLayer.Chunks, OnLabGround(map, 0, -27), 0, 0, (new Vector3(-60, 90, 60), new Vector3(0, 0, 0))),
            ("markers", DebugLayer.Markers, map.Marker("house_path")!.Pos, 180, 180, null),
            ("all", DebugLayer.All, map.Marker("house_path")!.Pos, 180, 180, (new Vector3(-20, 24, -10), new Vector3(-20, 0, -40)))
        };
        foreach (var (name, layer, at, yaw, camera, view) in shots)
        {
            string shot = $"open80_layers_{name}";
            if (!Wanted(shot)) continue;
            game.World.Layers = layer;
            PutInLab(map, at, yaw, camera);
            if (layer == DebugLayer.Encounters)
                // the zone's Pokémon come out behind the player as the field runs (OW7)
                Frames(60 * 4);
            game.OpenView = view;
            Frames(3);
            Shot(shot);
            TellOpenShot(shot);
            var drawn = game.OpenRenderer!.LastLayers;
            Console.WriteLine($"    layers {DebugLayers.Describe(layer)}: {(drawn == null ? "!! nothing built" : OpenDebugLayers.Describe(drawn))}");
            if (drawn != null) foreach (var label in drawn.Labels.Take(10)) Console.WriteLine($"      {label.Layer}: {label.Text}");
        }
        game.World.Layers = DebugLayer.None;
        game.OpenView = null;

        // The house's door: in by the stick from its path into its grid room, and out again onto its step
        PutInLab(map, map.Marker("house_path")!.Pos, 180, 180);
        for (int f = 0; f < 60 * 10 && engine.InOpenField; f++)
        {
            InputManager.Feed(ActionFrame.Stick(0, -127));
            Frames(1);
        }
        InputManager.Feed(null);
        for (int f = 0; f < 120 && game.State != GameState.Overworld; f++) Frames(1);
        Frames(30);
        if (Wanted("open85_house_room")) { Shot("open85_house_room"); Console.WriteLine($"  open85_house_room: {(engine.InOpenField ? "!! still in the open map" : $"in {game.Map.Name} at {game.Player.GridX},{game.Player.GridY}")}"); }
        game.Steering = (Direction.Down, false);
        for (int f = 0; f < 60 * 10 && !engine.InOpenField; f++) Frames(1);
        game.Steering = null;
        for (int f = 0; f < 120 && game.State != GameState.Overworld; f++) Frames(1);
        Frames(30);
        if (Wanted("open86_house_step_again") && engine.InOpenField) { Shot("open86_house_step_again"); TellOpenShot("open86_house_step_again"); }

        // The playthrough bot from the house's path through the tunnel into the cavern, whose trigger runs its scene
        game.UseScripts(ScriptLibrary.FromSources(("lab_cave", "script Cavern\n  say \"The cavern's air is cold.\"\n  setvar VAR_LAB_CAVE 1\n  end\n"), ("common", "")));
        game.Story.SetVar("VAR_LAB_CAVE", 0);
        PutInLab(map, map.Marker("house_path")!.Pos, 180, 180);
        var bot = new Autoplayer(engine) { OpenWay = new OpenItinerary(new[] { OpenLeg.Marker("tunnel_west"), OpenLeg.Marker("cavern_middle") }) };
        bool tunnelShot = false, sceneShot = false;
        for (int f = 0; f < 60 * 90 && !bot.OpenWay.Finished && !bot.Stopped; f++)
        {
            bot.Frame(1f / 60f);
            if (f % 4 == 0) Frames(1);
            if (!tunnelShot && game.OpenPlayer is { } walker && walker.Pos.X is > -24_000 and < -14_000 && Math.Abs(walker.Pos.Z) < 2_000)
            {
                tunnelShot = true;
                if (Wanted("open87_bot_in_the_tunnel")) { Frames(1); Shot("open87_bot_in_the_tunnel"); TellOpenShot("open87_bot_in_the_tunnel"); }
            }
            if (!sceneShot && game.Dialogue.IsActive)
            {
                sceneShot = true;
                InputManager.Feed(null);
                Frames(90);
                if (Wanted("open88_bot_cavern_scene")) { Shot("open88_bot_cavern_scene"); TellOpenShot("open88_bot_cavern_scene"); }
            }
        }
        // (the scene read to its end, as the bot reads it)
        for (int f = 0; f < 600 && engine.ScriptRunning; f++) bot.Frame(1f / 60f);
        bot.Release();
        Console.WriteLine($"  the bot: legs {string.Join(", ", bot.Report.Legs)}{(bot.Report.LegsGivenUp.Count > 0 ? $"; !! given up {string.Join(", ", bot.Report.LegsGivenUp)}" : "")}, "
            + $"{bot.OpenMetres:0.0} m walked, {bot.Report.Seconds:0.0} s; the cavern's variable {game.Story.Var("VAR_LAB_CAVE")}{(sceneShot ? "" : "; !! its scene never ran")}");
        game.UseScripts(ScriptLibrary.Default);

        // The camera posed as a scenario's `look 200 30 9` poses it
        PutInLab(map, map.Marker("west_road")!.Pos, 90, 90);
        game.OpenCamera.Pose(200 * Math.PI / 180, 30 * Math.PI / 180, 9);
        game.SettleOpenView();
        Frames(2);
        if (Wanted("open89_camera_posed")) { Shot("open89_camera_posed"); TellOpenShot("open89_camera_posed"); }

        // The Town Map's picture of the lab, painted from its bake
        if (Wanted("open90_townmap_picture"))
        {
            var picture = RegionMapArt.PaintOpen(map.Land!, map.Solids);
            var image = picture.ToImage(4);
            Raylib.ExportImage(image, Path.Combine(outDir, "open90_townmap_picture.png"));
            Raylib.UnloadImage(image);
            var cell = RegionMapArt.OpenCellOf(map.Land!, map.Marker("house_step")!.Pos);
            Console.WriteLine($"  open90_townmap_picture: {picture.Width}×{picture.Height} texels (a texel a metre), the house's step in cell {cell.X},{cell.Y}");
        }
    }
}
