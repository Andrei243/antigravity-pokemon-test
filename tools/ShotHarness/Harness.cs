// The harness's game and the helpers every mode uses: stepping frames, saving shots, putting the player somewhere,
// starting a battle and reading it on. The modes are the other parts of this class, one file each in Modes/. They
// reach the engine through its Driver (GameEngine.Drive), which shows its state and its own steps to the tools.

partial class Harness
{
    readonly string[] args;
    readonly string mode;
    readonly string outDir;
    readonly string startDir;

    readonly GameEngine engine;
    readonly GameEngine.Driver game;
    readonly double startMs;
    long tick;

    readonly Party party;
    readonly Inventory inventory;
    readonly Pokedex pokedex;

    public Harness(string[] args, string mode, string outDir, string startDir)
    {
        this.args = args;
        this.mode = mode;
        this.outDir = outDir;
        this.startDir = startDir;

        var startClock = System.Diagnostics.Stopwatch.StartNew();
        engine = new GameEngine();
        engine.Initialize();
        game = engine.Drive;
        // People who move about of their own accord would stand elsewhere in every shot: they stay put but where a mode
        // shows them moving (plan 02 · S6)
        engine.PeopleStayPut = true;
        // A Pokémon standing in the field waits for its sprite rather than showing a Poké Ball for a few frames (plan 10 · F1)
        FieldSprites.Patient = true;
        startMs = startClock.Elapsed.TotalMilliseconds;

        // Muted, and by day whatever the real time is (the "times" mode sets the other times of day)
        engine.Settings.Muted = true;
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // The game opens on its title screen; everything but the "title" mode wants a game in progress
        // (in Sinnoh, which the shots below show)
        engine.NewGameRegion = "Sinnoh";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        // The harness moves between maps without arriving anywhere, so the location sign would sit in every field shot
        game.LocationSign.Hide();

        PastTheOpening();

        party = game.Party;
        party.Add(new Pokemon(PokemonDatabase.Get("Chimchar")!, 7));
        party.Add(new Pokemon(PokemonDatabase.Get("Piplup")!, 6));
        inventory = game.Bag;
        pokedex = game.Pokedex;
    }

    public bool Run(string section) => mode == "all" || mode == section;

    public void Close() => engine.Close();

    // A new game begins in the player's room with nothing (plan 02 · S4). The modes show the game past its first
    // chapter, as every new game began before the chapter was written: in front of the house with a Turtwig, the
    // Pokédex, the Running Shoes and a few items, and everyone the chapter sends away gone (common.OpeningDone)
    void PastTheOpening()
    {
        StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
        var team = game.Party;
        var dex = game.Pokedex;
        var bag = game.Bag;
        var first = new Pokemon(PokemonDatabase.Get("Turtwig")!, 5);
        team.Add(first);
        dex.RegisterSeen(first.Species.DexNumber);
        dex.RegisterCaught(first.Species.DexNumber);
        bag.AddItem(ItemDatabase.Get("Poké Ball")!, 10);
        bag.AddItem(ItemDatabase.Get("Potion")!, 5);
        bag.AddItem(ItemDatabase.Get("Revive")!, 2);
        game.Map = MapDatabase.Get("Sinnoh");
        game.Player.SetPosition(116, 886, Direction.Down);
    }

    void Frames(int n)
    {
        for (int i = 0; i < n; i++)
        {
            FrameClock.Fixed = ++tick / 60.0;
            engine.Update(1f / 60f);
            engine.Draw();
        }
    }

    // Advances the game by some seconds, drawing only every sixth frame: the battle's effects and camera are functions
    // of time, so the frames in between only cost time under software rendering
    void Skip(double seconds)
    {
        int n = Math.Max(1, (int)Math.Round(seconds * 60.0));
        for (int i = 0; i < n; i++)
        {
            FrameClock.Fixed = ++tick / 60.0;
            engine.Update(1f / 60f);
            if (i % 6 == 5 || i == n - 1) engine.Draw();
        }
    }

    Image Capture()
    {
        engine.Draw();
        var rt = game.VirtualScreen;
        var img = Raylib.LoadImageFromTexture(rt.Texture);
        Raylib.ImageFlipVertical(ref img);
        // The game's picture is opaque everywhere (the window shows it over black): a see-through pixel is drawn darker
        // in the game than in the shot
        if (img.Format == PixelFormat.UncompressedR8G8B8A8)
        {
            long seeThrough = 0;
            unsafe
            {
                byte* p = (byte*)img.Data;
                for (long i = 3, end = (long)img.Width * img.Height * 4; i < end; i += 4)
                    if (p[i] != 255) seeThrough++;
            }
            if (seeThrough > 0) Console.WriteLine($"!! the picture is see-through in {seeThrough} pixels");
        }
        return img;
    }

    void Save(Image img, string name)
    {
        Raylib.ExportImage(img, Path.Combine(outDir, name + ".png"));
        Raylib.UnloadImage(img);
        Console.WriteLine("wrote " + name);
    }

    // The game renders at 3840x2160. Shots are saved at half size (quick to write and review) unless SHOTS_4K=1;
    // coordinates everywhere in the harness are layout units (1920x1080).
    readonly bool native4K = Environment.GetEnvironmentVariable("SHOTS_4K") == "1";
    const int S = GameEngine.RenderScale;

    // SHOTS_FILTER=text saves only the shots whose name contains it, for quick looks at one thing. It also skips the
    // timings unless SHOTS_TIMING=1 (a filter that matches nothing then gives a run of timings alone)
    readonly string filter = Environment.GetEnvironmentVariable("SHOTS_FILTER") ?? "";
    bool Wanted(string name) => filter.Length == 0 || name.Contains(filter);

    void Shot(string name)
    {
        if (!Wanted(name)) return;
        var img = Capture();
        if (!native4K) Raylib.ImageResize(ref img, GameEngine.VirtualWidth, GameEngine.VirtualHeight);
        Save(img, name);
    }

    // A region of the screen at native resolution, enlarged with nearest-neighbour filtering for close inspection
    // (scale is relative to layout units, so 2 means the native 4K pixels)
    void ShotCrop(string name, int x, int y, int w, int h, int scale)
    {
        if (!Wanted(name)) return;
        var img = Capture();
        Raylib.ImageCrop(ref img, new Rectangle(x * S, y * S, w * S, h * S));
        if (scale > S) Raylib.ImageResizeNN(ref img, w * scale, h * scale);
        Save(img, name);
    }

    // Boards: the same frame from an earlier run next to this run's, at half size with a label over each
    void Boards(string beforeDir, string[] frames)
    {
        foreach (var frame in frames)
        {
            string before = Path.Combine(Path.GetFullPath(beforeDir, startDir), frame + ".png");
            string after = Path.Combine(outDir, frame + ".png");
            if (!File.Exists(before) || !File.Exists(after)) continue;
            var board = Raylib.GenImageColor(1920 + 30, 540 + 76, new Color(24, 26, 34, 255));
            int col = 0;
            foreach (var (path, label) in new[] { (before, "BEFORE"), (after, "AFTER") })
            {
                var img = Raylib.LoadImage(path);
                Raylib.ImageResize(ref img, 960, 540);
                int x = 10 + col * 970;
                Raylib.ImageDraw(ref board, img, new Rectangle(0, 0, 960, 540), new Rectangle(x, 66, 960, 540), Color.White);
                Raylib.ImageDrawText(ref board, label, x + 4, 18, 40, Color.White);
                Raylib.UnloadImage(img);
                col++;
            }
            Save(board, "compare_" + frame);
        }
    }

    // The five hand-made maps of the first towns and routes gave way to the imported world (plan 01 · M2). The shots
    // that stood on them now stand at the same kind of place on the map of Sinnoh, or by the lake: each old spot has
    // its new one here, and an old map's name alone stands for a typical spot of the area that replaced it.
    static readonly Dictionary<(string, int, int), (string Map, int X, int Y)> movedSpots = new()
    {
        [("TwinleafTown", 11, 8)] = ("Sinnoh", 112, 880), [("TwinleafTown", 6, 9)] = ("Sinnoh", 116, 886), [("TwinleafTown", 11, 1)] = ("Sinnoh", 112, 866),
        [("TwinleafTown", 9, 14)] = ("Sinnoh", 111, 890), [("TwinleafTown", 17, 9)] = ("Sinnoh", 105, 876), [("TwinleafTown", 12, 7)] = ("Sinnoh", 116, 876),
        [("TwinleafTown", 3, 3)] = ("Sinnoh", 103, 868), [("TwinleafTown", 11, 9)] = ("Sinnoh", 112, 881), [("TwinleafTown", 12, 10)] = ("Sinnoh", 112, 882),
        [("Route201", 14, 10)] = ("Sinnoh", 112, 857), [("Route201", 24, 8)] = ("Sinnoh", 115, 854), [("Route201", 22, 11)] = ("Sinnoh", 110, 850),
        [("Route201", 24, 9)] = ("Sinnoh", 166, 815), [("Route201", 24, 14)] = ("Sinnoh", 120, 854), [("Route201", 22, 15)] = ("Sinnoh", 120, 854),
        [("Route201", 22, 10)] = ("Sinnoh", 110, 850), [("Route201", 32, 3)] = ("Sinnoh", 124, 850), [("Route201", 12, 10)] = ("Sinnoh", 130, 854),
        [("SandgemTown", 14, 8)] = ("Sinnoh", 178, 845), [("SandgemTown", 8, 19)] = ("Sinnoh", 168, 844), [("SandgemTown", 6, 8)] = ("Sinnoh", 177, 843),
        [("SandgemTown", 22, 8)] = ("Sinnoh", 187, 843), [("SandgemTown", 7, 19)] = ("Sinnoh", 168, 843), [("SandgemTown", 12, 10)] = ("Sinnoh", 178, 846),
        [("LakeVerity", 14, 11)] = ("LakeVerity", 44, 46), [("LakeVerity", 14, 10)] = ("LakeVerity", 44, 46), [("LakeVerity", 24, 5)] = ("LakeVerity", 51, 40),
        [("Route202", 14, 10)] = ("Sinnoh", 174, 815), [("Route202", 15, 2)] = ("Sinnoh", 174, 802),
        // Jubilife City was a hand-made map until its area was opened (plan 01 · M5)
        [("JubilifeCity", 20, 30)] = ("Sinnoh", 175, 792), [("JubilifeCity", 19, 18)] = ("Sinnoh", 175, 760), [("JubilifeCity", 19, 17)] = ("Sinnoh", 175, 760),
        [("JubilifeCity", 7, 9)] = ("Sinnoh", 168, 779), [("JubilifeCity", 30, 10)] = ("Sinnoh", 153, 755), [("JubilifeCity", 30, 9)] = ("Sinnoh", 153, 755),
        [("JubilifeCity", 28, 29)] = ("Sinnoh", 180, 779), [("JubilifeCity", 28, 28)] = ("Sinnoh", 180, 779),
        [("JubilifeCity", 9, 29)] = ("Sinnoh", 149, 781), [("JubilifeCity", 9, 28)] = ("Sinnoh", 149, 781)
    };
    static readonly Dictionary<string, (string Map, int X, int Y)> movedMaps = new()
    {
        ["TwinleafTown"] = ("Sinnoh", 112, 880), ["Route201"] = ("Sinnoh", 115, 854), ["LakeVerity"] = ("LakeVerity", 44, 46),
        ["SandgemTown"] = ("Sinnoh", 178, 845), ["Route202"] = ("Sinnoh", 174, 815), ["JubilifeCity"] = ("Sinnoh", 175, 760)
    };

    static (string Map, int X, int Y) Place(string map, int x, int y) =>
        movedSpots.TryGetValue((map, x, y), out var spot) ? spot : movedMaps.TryGetValue(map, out var typical) ? typical : (map, x, y);

    // Puts the player on a tile of a map as it is today
    void At(string map, int x, int y, Direction facing)
    {
        game.Place(MapDatabase.Get(map), x, y, facing);
        Frames(2);
    }

    // Like At, for the shots written when the first towns were hand-made maps of their own
    void GoTo(string map, int x, int y, Direction facing)
    {
        (map, x, y) = Place(map, x, y);
        At(map, x, y, facing);
    }

    // The open tile of an area of the imported world nearest the middle of its open ground
    static (Map Map, int X, int Y) AreaSpot(string key)
    {
        foreach (var name in MapDatabase.MapNames)
        {
            var map = MapDatabase.Get(name);
            if (map.AreaBounds(key) is not { } b) continue;
            var area = map.FindArea(key);
            var open = new List<(int X, int Y)>();
            for (int y = b.Y; y < b.Y + b.Height; y++)
                for (int x = b.X; x < b.X + b.Width; x++)
                    if (map.AreaAt(x, y) == area && map.IsWalkable(x, y)) open.Add((x, y));
            if (open.Count == 0) throw new InvalidOperationException($"The area {key} has no open ground");
            double cx = open.Average(t => t.X), cy = open.Average(t => t.Y);
            var (sx, sy) = open.OrderBy(t => (t.X - cx) * (t.X - cx) + (t.Y - cy) * (t.Y - cy)).First();
            return (map, sx, sy);
        }
        throw new ArgumentException($"No map has an area called {key}");
    }

    void SetPlayerAnim(float walk, float blend, bool running = false) => game.Player.Pose(walk, blend, running);

    void Timing(string label, int frames = 300)
    {
        if (filter.Length > 0 && Environment.GetEnvironmentVariable("SHOTS_TIMING") != "1") return;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Frames(frames);
        Console.WriteLine($"{label}: {sw.Elapsed.TotalMilliseconds / frames:F2} ms/frame");
    }

    // A scene's frame time, then the same frames with the profiler on: each pass's share of the frame, and the meshes
    // and triangles it draws. `frozen` draws one moment over and over (a move's effect at its height, a camera's
    // close-up), which the game's own clock would move on from.
    // What `profile` measured, scene by scene, written to profile.json as the mode ends (Timings.cs)
    readonly ProfileRun profileRun = new();
    string profilePreset = "";

    void Profile(string label, int frames = 240, bool frozen = false)
    {
        // SHOTS_ONLY=battle,route measures only the scenes whose name has one of those words in it
        var only = (Environment.GetEnvironmentVariable("SHOTS_ONLY") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (only.Length > 0 && !only.Any(word => label.Contains(word, StringComparison.OrdinalIgnoreCase))) return;
        void One() { if (!frozen) { FrameClock.Fixed = ++tick / 60.0; engine.Update(1f / 60f); } engine.Draw(); }
        for (int i = 0; i < 20; i++) One();
        var times = new List<double>(frames);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        double then = 0;
        for (int i = 0; i < frames; i++)
        {
            One();
            double now = sw.Elapsed.TotalMilliseconds;
            times.Add(now - then);
            then = now;
        }
        double plain = then / frames;
        FrameProfiler.Enabled = true;
        for (int i = 0; i < 5; i++) One();
        FrameProfiler.Reset();
        var passes = Enum.GetValues<FrameSection>().ToDictionary(s => s, _ => new List<double>());
        for (int i = 0; i < frames / 2; i++)
        {
            One();
            foreach (var (section, list) in passes) list.Add(FrameProfiler.LastFrame(section));
        }
        FrameProfiler.Enabled = false;
        times.Sort();
        double Median(List<double> sorted) => ProfileRun.Percentile(sorted, 0.5);
        var scene = new ProfileScene
        {
            Preset = profilePreset, Name = label, Mean = plain,
            Median = Median(times), P10 = ProfileRun.Percentile(times, 0.1), P90 = ProfileRun.Percentile(times, 0.9),
        };
        foreach (var (section, list) in passes)
        {
            list.Sort();
            scene.Passes[section.ToString().ToLowerInvariant()] = Median(list);
        }
        profileRun.Scenes.Add(scene);
        Console.WriteLine($"{label}: {plain:F2} ms/frame (median {scene.Median:F2}, 10% {scene.P10:F2}, 90% {scene.P90:F2})");
        Console.WriteLine($"    {FrameProfiler.Report()}");
    }

    // Test map with every character type in a row, for close-ups of the 3D models
    static Map BuildLineup()
    {
        var m = new Map(18, 12) { Name = "Lineup", DisplayName = "Lineup" };
        for (int x = 0; x < 18; x++) { m.SetGroundTile(x, 0, TileType.Tree, true); m.SetGroundTile(x, 11, TileType.Tree, true); }
        for (int y = 0; y < 12; y++) { m.SetGroundTile(0, y, TileType.Tree, true); m.SetGroundTile(17, y, TileType.Tree, true); }
        for (int x = 1; x < 17; x++) m.SetGroundTile(x, 6, TileType.Path);
        string[] types = { "Rival", "Rowan", "Nurse", "Mom", "Lady", "Clerk", "Youngster", "Lass", "StarterBriefcase", "Rift" };
        for (int i = 0; i < types.Length; i++)
            m.NPCs.Add(new NPC { Name = types[i] + "_d", NpcType = types[i], GridX = 3 + i, GridY = 4, Facing = Direction.Down });
        Direction[] dirs = { Direction.Left, Direction.Up, Direction.Right, Direction.Down };
        for (int i = 0; i < 8; i++)
            m.NPCs.Add(new NPC { Name = types[i] + "_s", NpcType = types[i], GridX = 3 + i, GridY = 7, Facing = dirs[i % 4] });
        return m;
    }

    // The same characters inside the field's focus band, around the player at (10, 7)
    static Map BuildFocusLineup()
    {
        var m = new Map(22, 14) { Name = "LineupFocus", DisplayName = "Lineup" };
        for (int x = 0; x < 22; x++) { m.SetGroundTile(x, 0, TileType.Tree, true); m.SetGroundTile(x, 13, TileType.Tree, true); }
        for (int y = 0; y < 14; y++) { m.SetGroundTile(0, y, TileType.Tree, true); m.SetGroundTile(21, y, TileType.Tree, true); }
        for (int x = 1; x < 21; x++) m.SetGroundTile(x, 9, TileType.Path);
        string[] front = { "Rival", "Rowan", "Nurse", "Mom", "Lady", "Clerk", "Youngster", "Lass", "Clown", "Looker", "Gentleman", "StarterBriefcase", "Rift" };
        for (int i = 0, x = 3; i < front.Length; i++, x++)
        {
            if (x == 10) x++;   // the player's place
            m.NPCs.Add(new NPC { Name = front[i] + "_f", NpcType = front[i], GridX = x, GridY = 7, Facing = Direction.Down });
        }
        string[] turned = { "Player", "Rival", "Rowan", "Nurse", "Mom", "Lady", "Clerk", "Youngster", "Lass", "Clown", "Looker", "Gentleman" };
        Direction[] dirs = { Direction.Left, Direction.Up, Direction.Right };
        for (int i = 0; i < turned.Length; i++)
            m.NPCs.Add(new NPC { Name = turned[i] + "_t", NpcType = turned[i], GridX = 4 + i, GridY = 9, Facing = dirs[i % 3] });
        return m;
    }

    void Confirm(BattleEngine b)
    {
        Console.WriteLine("  msg: " + b.CurrentMessage);
        b.ConfirmMessage();
        Frames(1);
    }

    // A Pokémon of a species, or of a species' form by the form's name (Meowth-Galar, Charizard-Mega-X)
    static Pokemon Meet(string name, int level)
    {
        if (PokemonDatabase.Get(name) is { } species) return new Pokemon(species, level);
        var p = new Pokemon(PokemonDatabase.SpeciesOfForm(name) ?? throw new ArgumentException($"No species or form is called {name}"), level);
        p.ChangeForm(name);
        return p;
    }

    BattleEngine StartBattle(string foe, int level, Trainer trainer = null, string map = "Route201", Random chance = null, bool hints = false)
    {
        // On the map of Sinnoh the stage depends on where the battle starts: the area's trees, water within sight
        var (name, x, y) = Place(map, -1, -1);
        game.Map = MapDatabase.Get(name);
        if (x >= 0) game.Player.SetPosition(x, y, Direction.Up);
        if (trainer != null && trainer.Party.Count == 0) trainer.Party.Add(new Pokemon(PokemonDatabase.Get("Shinx")!, 5));
        var enemy = trainer?.Party.Members[0] ?? Meet(foe, level);
        // (A wild battle can be given its own chance, with the rolls a shot depends on fixed)
        // (and the move cards can say how each move will do, plan 12 · Q10)
        var b = chance == null && !hints
            ? new BattleEngine(party, enemy, inventory, pokedex, trainer)
            : new BattleEngine(new BattleSetup { PlayerParty = party, Inventory = inventory, Pokedex = pokedex, WildPokemon = new List<Pokemon> { enemy }, Random = chance, MoveHints = hints });
        game.BattleRenderer.SetArena(game.Map, x, y);
        game.Battle = b;
        game.State = GameState.Battle;
        return b;
    }

    // Waits out the camera sweep and sends the player's Pokémon out, ending on the main battle menu once the camera
    // has eased back to the overview
    void ToMainMenu(BattleEngine b)
    {
        Skip(130 / 60.0);
        Confirm(b);
        Skip(50 / 60.0);
        Confirm(b);
        Skip(0.8);
        // What acts as the Pokémon come in has lines of its own (Intimidate, Pressure): read on until the menu is really open
        for (int i = 0; i < 12 && b.HUD.MenuState == BattleMenuState.Message && !b.IsBattleOver; i++)
        {
            Confirm(b);
            Skip(0.8);
        }
    }
}
