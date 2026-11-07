using System.Diagnostics;
using OpenWorldBake;

partial class Harness
{
    // ---------------------------------------------------------------- plan 32 · OW0, the open world's prototype gate

    OpenProtoScene openScene;
    WalkRecord openWalk;

    /// <summary>
    /// The gate's scene, baked and uploaded: the hill's chunks at three levels, the trees (meshes near, impostors far),
    /// the capsule's walk of the route, and the person and the Pokémon on the ground. It prints what the bake made.
    /// </summary>
    OpenProtoScene BuildOpenScene()
    {
        var world = Prototype.World();
        var bake = Prototype.BakeAll(world);
        for (int lod = 0; lod < Bake.Cells.Length; lod++)
        {
            var chunks = bake.Chunks.Where(c => c.Key.Lod == lod).Select(c => c.Value).ToList();
            Console.WriteLine($"  bake, level {lod} ({Bake.Cells[lod]} m cells): {chunks.Count} chunks, {chunks.Sum(c => c.TriangleCount)} triangles drawn "
                + $"({chunks.Sum(c => c.GroundTriangles)} the chunks' own, {chunks.Sum(c => c.ApronTriangles)} in aprons, {chunks.Sum(c => c.TriangleCount - c.GroundTriangles - c.ApronTriangles)} in skirts)");
        }
        var collision = CollisionMesh.Of(bake);
        openWalk = Prototype.Walk(collision);
        Console.WriteLine($"  walk: {(openWalk.Finished ? "finished" : "!! not finished")} in {openWalk.Samples[^1].Tick} ticks, {openWalk.Samples[^1].Walked:0.0} m; highest {openWalk.HighestY:0.000} m, "
            + $"longest fall {openWalk.LongestFall:0.000} m, {openWalk.PushTicks} ticks pushed off walls, {openWalk.SlideTicks} sliding, the trigger ran {openWalk.TriggerEntries} time(s)");
        foreach (string line in openWalk.Events) Console.WriteLine("    " + line);
        var trees = Prototype.Trees(world);
        var scene = new OpenProtoScene(game.RenderContext, bake, collision, trees)
        {
            PersonFeet = V(Prototype.OnGround(collision, Prototype.Person)),
            PersonYaw = (float)Prototype.PersonYaw,
            PokemonFeet = V(Prototype.OnGround(collision, Prototype.PokemonSpot)),
            PokemonYaw = (float)Prototype.PokemonYaw
        };
        Console.WriteLine($"  trees: {scene.TreesTotal} in all; the person ({scene.PersonLook}) at {Prototype.OnGround(collision, Prototype.Person)}, a wild {scene.Species} at {Prototype.OnGround(collision, Prototype.PokemonSpot)}");
        return scene;
    }

    static Vector3 V(D3 p) => new((float)p.X, (float)p.Y, (float)p.Z);

    /// <summary>
    /// A frame of the prototype as the game frames itself: the scene rendered and composited into the virtual screen,
    /// then shown in the window and swapped, each part lapped for the profiler.
    /// </summary>
    void OpenFrame(OpenProtoScene scene)
    {
        FrameProfiler.LapCpu(FrameSection.Update);
        var context = game.RenderContext;
        float before = context.ResolutionStep;
        scene.Render();
        scene.Composite(game.VirtualScreen);
        context.ResolutionStep = before;
        FrameProfiler.Lap(FrameSection.Interface);
        int sw = Raylib.GetScreenWidth(), sh = Raylib.GetScreenHeight();
        float fit = MathF.Min((float)sw / GameEngine.VirtualWidth, (float)sh / GameEngine.VirtualHeight);
        int dw = (int)(GameEngine.VirtualWidth * fit), dh = (int)(GameEngine.VirtualHeight * fit);
        var screen = game.VirtualScreen;
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);
        Raylib.DrawTexturePro(screen.Texture, new Rectangle(0, 0, screen.Texture.Width, -screen.Texture.Height),
            new Rectangle((sw - dw) / 2, (sh - dh) / 2, dw, dh), Vector2.Zero, 0f, Color.White);
        FrameProfiler.Lap(FrameSection.Present);
        Raylib.EndDrawing();
        FrameProfiler.Lap(FrameSection.Swap);
        FrameProfiler.EndFrame();
    }

    void OpenShot(OpenProtoScene scene, string name)
    {
        if (!Wanted(name)) return;
        OpenFrame(scene);
        var img = Raylib.LoadImageFromTexture(game.VirtualScreen.Texture);
        Raylib.ImageFlipVertical(ref img);
        if (!native4K) Raylib.ImageResize(ref img, GameEngine.VirtualWidth, GameEngine.VirtualHeight);
        Save(img, name);
    }

    /// <summary>The player where the walk stood at a sample, walking as it walked, and the orbit camera about them.</summary>
    (double Arm, bool Held) PlaceOnWalk(OpenProtoScene scene, WalkSample s, double yawOffset, double pitchDegrees, double distance)
    {
        var feet = s.Pos.Metres;
        scene.PlayerFeet = V(feet);
        scene.PlayerYaw = (float)(s.Yaw * Math.PI * 2 / 65536);
        scene.PlayerPose = new CharacterPose
        {
            Time = s.Tick / (float)OpenBody.TicksPerSecond,
            Walk = (float)(s.Walked / 1.6 % 1.0),
            WalkBlend = s.Speed > 0.2 ? 1f : 0f,
            Running = s.Speed > 3
        };
        scene.Time = s.Tick / (float)OpenBody.TicksPerSecond;
        var orbit = new OrbitCamera { Yaw = scene.PlayerYaw + yawOffset, Pitch = pitchDegrees * Math.PI / 180, Distance = distance };
        var placed = orbit.Place(scene.Collision, feet);
        scene.Camera = new Camera3D(V(placed.Position), V(placed.Target), Vector3.UnitY, (float)orbit.FovY, CameraProjection.Perspective);
        return (placed.Arm, placed.Held);
    }

    void PlaceOverview(OpenProtoScene scene, WalkSample s)
    {
        PlaceOnWalk(scene, s, 0, 16, 5.5);
        scene.Camera = new Camera3D(new Vector3(-78, 26, 64), new Vector3(-2, 2, 0), Vector3.UnitY, 50f, CameraProjection.Perspective);
    }

    /// <summary>What a shot shows, printed: the tick, the position, the yaw, where it is, the camera and what was drawn.</summary>
    void TellOpen(OpenProtoScene scene, string name, WalkSample s, (double Arm, bool Held)? arm, double frameMs)
    {
        var p = s.Pos.Metres;
        var world = scene.Collision;
        bool under = scene.Collision.Raycast(p + new D3(0, 1.7, 0), new D3(0, 1, 0), 40) is not null;
        string where = Prototype.Trigger.Holds(p + new D3(0, 0.8, 0)) ? "in the cavern (in the trigger's box)" : under ? "under rock" : Prototype.Zone.Holds(p) ? $"on the hill's top (zone '{Prototype.Zone.Name}')" : "in the open";
        Console.WriteLine($"  {name}: tick {s.Tick} ({s.Tick / 120.0:0.00} s), feet {s.Pos}, yaw {s.Yaw} ({s.Yaw * 360.0 / 65536:0.0}°), {where}, {(s.Grounded ? "on the ground" : "falling")}, {s.Speed:0.00} m/s, hour {scene.Hour:0}");
        Console.WriteLine($"    camera at {scene.Camera.Position.X:0.00}, {scene.Camera.Position.Y:0.00}, {scene.Camera.Position.Z:0.00}, lens {scene.Camera.FovY:0}°"
            + (arm is { } a ? $", arm {a.Arm:0.00} m{(a.Held ? " (held in by a wall)" : "")}" : " (placed by hand)"));
        Console.WriteLine($"    drawn: chunks {scene.ChunksAt[0]}/{scene.ChunksAt[1]}/{scene.ChunksAt[2]} at the three levels ({scene.TerrainTriangles} triangles), "
            + $"{scene.TreeMeshes} trees as meshes in {scene.TreeChunksNear} chunks ({scene.TreeTriangles} triangles), {scene.Impostors} impostors, "
            + $"rigs {scene.RigTriangles} triangles, the Pokémon {scene.PokemonTriangles}; the shadow pass {scene.ShadowTriangles}");
        Console.WriteLine($"    frame {frameMs:0.00} ms (a mean of 30 on this machine, a card other runs share: the user's `profile` is what is judged)");
    }

    double TimeOpenFrames(OpenProtoScene scene, int frames = 30)
    {
        OpenFrame(scene);
        var watch = Stopwatch.StartNew();
        for (int i = 0; i < frames; i++) OpenFrame(scene);
        return watch.Elapsed.TotalMilliseconds / frames;
    }

    /// <summary>
    /// Plan 32 · OW0's prototype (not part of <c>all</c>; nothing of it reaches the game): the gate's hill baked by
    /// the first cut of the baker (tools/OpenWorldBake), the capsule's walk through its tunnel, its cavern, up its top
    /// and off its west portal, the orbit camera behind it held off walls by its arm, a trigger in the cavern, a wild
    /// Pokémon standing in the top's zone, a person by the west cutting, trees near and impostors far, by day and after
    /// dark, the scene at 1920×1080 (<c>ow00</c> to <c>ow09</c>, with <c>ow02n</c>, <c>ow07n</c> after dark and
    /// <c>ow07v</c> with the volumes and the navigation's path drawn through the rock). It also writes the vertical
    /// sections, the plan from above and the impostors' atlas. It prints positions, ticks, the camera and what was
    /// drawn at each shot, and the frame's mean on this machine, which only the user's own <c>profile</c> judges.
    /// </summary>
    public void OpenPrototypeMode()
    {
        var scene = openScene ??= BuildOpenScene();
        var walk = openWalk;
        var samples = walk.Samples;
        WalkSample Find(Func<WalkSample, bool> where) => samples.FirstOrDefault(where) is { Tick: > 0 } s ? s : samples[0];

        // The pictures without a window, from the same bake and walk
        var world = Prototype.World();
        Pictures.Save(Pictures.Section(world, (-60, 0), (60, 0), -6, 24, walk, Prototype.Trigger, Prototype.Zone), Path.Combine(outDir, "ow_section_tunnel.png"));
        Pictures.Save(Pictures.Section(world, (0, -40), (0, 40), -6, 24, walk, Prototype.Trigger, Prototype.Zone), Path.Combine(outDir, "ow_section_across.png"));
        var nav = new NavProto(scene.Collision);
        var west = Prototype.OnGround(scene.Collision, new D3(-50, 0, 0));
        var east = Prototype.OnGround(scene.Collision, new D3(50, 0, 0));
        var through = nav.Path(west, east);
        var over = nav.Path(west, east, p => p.Y < world.Height(p.X, p.Z) - 0.8);
        var paths = new List<(List<D3>, Color)>();
        if (through is { } t1) paths.Add((t1.Path, new Color(40, 220, 230, 255)));
        if (over is { } t2) paths.Add((t2.Path, new Color(230, 60, 200, 255)));
        Pictures.Save(Pictures.Plan(world, -70, -50, 70, 50, scene.Collision, walk, Prototype.Trees(world), paths), Path.Combine(outDir, "ow_plan.png"));
        if (scene.ImpostorAtlas is { } atlas)
            for (int k = 0; k < atlas.Length; k++) Pictures.Save(atlas[k], Path.Combine(outDir, $"ow_impostors_{(k == 0 ? "pine" : "round")}.png"));
        Console.WriteLine("wrote ow_section_tunnel, ow_section_across, ow_plan, ow_impostors_pine, ow_impostors_round");
        Console.WriteLine($"  navigation: {nav.Walkable.Count} walkable triangles, {nav.Components} pieces (the largest {nav.Largest}), "
            + $"{nav.LayeredColumns(-30, -30, 30, 30)} square metres of the hill with a floor over a floor; west to east {through?.Length:0.0} m through the tunnel, {over?.Length:0.0} m with it barred");

        void Shoot(string name, WalkSample s, (double Arm, bool Held)? arm)
        {
            double ms = TimeOpenFrames(scene);
            OpenShot(scene, name);
            TellOpen(scene, name, s, arm, ms);
        }

        // By day
        scene.Hour = 12;
        var start = samples[0];
        PlaceOverview(scene, start);
        Shoot("ow00_overview_day", start, null);
        // OPENPROTO_DIAG=1: the same with the skirts left out, and with nothing cast into the shadow map
        if (Environment.GetEnvironmentVariable("OPENPROTO_DIAG") == "1")
        {
            scene.DrawSkirts = false;
            Shoot("ow00_diag_no_skirts", start, null);
            scene.DrawSkirts = true;
            scene.CastShadows = false;
            Shoot("ow00_diag_no_shadow_casters", start, null);
            scene.CastShadows = true;
            scene.Outline = false;
            Shoot("ow00_diag_no_outline", start, null);
            scene.Outline = true;
            scene.Occlusion = false;
            Shoot("ow00_diag_no_occlusion", start, null);
            scene.Occlusion = true;
        }

        var run = Find(s => s.Leg == 0 && s.Walked > 8);
        Shoot("ow01_meadow_run_day", run, PlaceOnWalk(scene, run, 0, 14, 5.5));

        var cutting = Find(s => s.Leg == 1 && s.Pos.X > -40500);
        var cuttingArm = PlaceOnWalk(scene, cutting, 0.35, 14, 6);
        Shoot("ow02_west_cutting_day", cutting, cuttingArm);

        var tunnel = Find(s => s.Leg == 2 && s.Pos.X > -15000);
        Shoot("ow03_tunnel_day", tunnel, PlaceOnWalk(scene, tunnel, 0, 12, 5.5));

        var cavern = Find(s => s.Leg == 4 && s.Pos.Z > -4000);
        Shoot("ow04_cavern_day", cavern, PlaceOnWalk(scene, cavern, 0.4, 14, 6));

        var outEast = Find(s => s.Leg == 6 && s.Pos.X > 39000);
        Shoot("ow05_east_mouth_day", outEast, PlaceOnWalk(scene, outEast, Math.PI, 12, 7.5));

        var climb = Find(s => s.Leg == 9 && s.Pos.Y > 5000);
        Shoot("ow06_climb_day", climb, PlaceOnWalk(scene, climb, -0.3, 4, 6));

        var top = Find(s => s.Leg == 11);
        // Looking past the player toward the Pokémon on the top, the player off to the side
        var toPokemon = Math.Atan2(scene.PokemonFeet.X - top.Pos.X / 1000.0, scene.PokemonFeet.Z - top.Pos.Z / 1000.0);
        PlaceOnWalk(scene, top, 0, 18, 6.5);
        var topOrbit = new OrbitCamera { Yaw = toPokemon - 0.5, Pitch = 16 * Math.PI / 180, Distance = 5 };
        var topPlaced = topOrbit.Place(scene.Collision, top.Pos.Metres);
        scene.Camera = new Camera3D(V(topPlaced.Position), V(topPlaced.Target), Vector3.UnitY, 50f, CameraProjection.Perspective);
        Shoot("ow07_top_pokemon_day", top, (topPlaced.Arm, topPlaced.Held));

        var fall = samples.Where(s => !s.Grounded).ToList();
        var midFall = fall.Count > 0 ? fall[fall.Count / 2] : samples[^1];
        Shoot("ow08_drop_day", midFall, PlaceOnWalk(scene, midFall, Math.PI / 2, 10, 7));

        // After dark
        scene.Hour = 22;
        PlaceOverview(scene, start);
        Shoot("ow09_overview_night", start, null);
        PlaceOnWalk(scene, cutting, 0.35, 14, 6);
        Shoot("ow02n_west_cutting_night", cutting, cuttingArm);
        PlaceOnWalk(scene, top, 0, 18, 6.5);
        scene.Camera = new Camera3D(V(topPlaced.Position), V(topPlaced.Target), Vector3.UnitY, 50f, CameraProjection.Perspective);
        Shoot("ow07n_top_pokemon_night", top, (topPlaced.Arm, topPlaced.Held));

        // The volumes and the navigation's path through the tunnel, drawn through the rock, from high over the hill
        scene.Hour = 12;
        PlaceOnWalk(scene, top, 0, 18, 6.5);
        scene.Camera = new Camera3D(new Vector3(-34, 40, 46), new Vector3(0, 2, 0), Vector3.UnitY, 50f, CameraProjection.Perspective);
        scene.ShowVolumes = true;
        scene.NavPath = through?.Path.Select(V).ToList();
        Shoot("ow07v_volumes_and_path", top, null);
        scene.ShowVolumes = false;
        scene.NavPath = null;

        scene.Unload();
        openScene = null;
    }

    /// <summary>
    /// `profile`'s open_prototype scene (plan 32 · OW0): the hill from the meadow's south-west by day, the busiest view
    /// of the gate's scene (the land at three levels to the horizon, the trees near and the impostors far, the two
    /// rigs), at 1920×1080 in the preset's target. Baked once, the first time a preset times it.
    /// </summary>
    void OpenPrototypeProfile()
    {
        const string label = "open_prototype";
        var only = (Environment.GetEnvironmentVariable("SHOTS_ONLY") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        bool look = Environment.GetEnvironmentVariable("SHOTS_LOOK") == "1";
        bool timed = only.Length == 0 || only.Any(word => label.Contains(word, StringComparison.OrdinalIgnoreCase));
        if (!timed && !look) return;
        var scene = openScene ??= BuildOpenScene();
        scene.Hour = 12;
        PlaceOnWalk(scene, openWalk.Samples.First(s => s.Leg == 0 && s.Walked > 8), 0, 14, 5.5);
        if (look) OpenShot(scene, "profile_" + label);
        TimeScene(label, () => OpenFrame(scene));
    }
}
