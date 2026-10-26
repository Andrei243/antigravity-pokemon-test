partial class Harness
{
    // ---------------------------------------------------------------- the soak run (plan 16 · T22)

    // Not part of `all`: `<out> soak [minutes]` (an hour unless said) plays laps of what a long session does, each
    // ending where it began, at the player's door in Twinleaf Town by day: the `world` mode's three walks there and
    // back at running pace, into Sandgem's Pokémon Center and out, a wild battle and a trainer's each brought to its
    // end, the evolution scene, the National Pokédex scrolled from Bulbasaur to Pecharunt with its sprites, and every
    // box. At each return it weighs the GPU ledger (plan 16 · T21), the managed heap after a collection, the gen-2
    // collections and the working set; every frame it times the update and the draw (the swap is the draw's last
    // step), the bytes allocated and the collections. It fails (exit 1, lines that begin with !!) when a count of the
    // ledger under an owner, or the managed memory by more than 5 %, is higher at a return than at the second lap's
    // (the first fills the caches T21 bounds), and gives each frame drawn over 16.7 ms its cause from T21's readings.
    // It writes soak.md and soak.png. Its milliseconds are read on the Windows machine only; under llvmpipe its counts
    // and memory alone mean anything.
    public void SoakMode()
    {
        double minutes = args.Length > 2 && double.TryParse(args[2], System.Globalization.CultureInfo.InvariantCulture, out double asked) ? asked : 60;
        var sinnoh = MapDatabase.Get("Sinnoh");
        var everyone = PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).ToList();
        var walker = game.Player;

        // What a lap needs once: everyone caught, the National Pokédex, its hundred boxes full, a lead nothing outlasts
        foreach (var species in everyone) pokedex.RegisterCaught(species.DexNumber);
        pokedex.UnlockNational();
        game.Story.Set("SinnohHallOfFame");
        var boxes = game.Boxes = new PcBoxes();
        boxes.Grow();
        foreach (var species in everyone) boxes.Store(new Pokemon(species, 50));
        party.Members.Insert(0, new Pokemon(PokemonDatabase.Get("Garchomp")!, 100));
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // Sandgem Town's Pokémon Center's door, found as the `life` mode finds it
        var center = MapStructures.BuildingsOf(sinnoh).First(b => b.Kind == BuildingKind.PokemonCenter && !b.Annex && sinnoh.AreaAt(b.X0, b.Y0)?.Key == "sandgem_town"
            && b.Doors.Exists(d => sinnoh.GetWarpAt(d.X, b.Y1) != null || sinnoh.GetWarpAt(d.X, b.Y1 + 1) != null));
        int doorX = center.Doors[0].X, doorY = sinnoh.GetWarpAt(doorX, center.Y1) != null ? center.Y1 : center.Y1 + 1;

        var walks = new[]
        {
            ((116, 886), (174, 801)), ((174, 801), (245, 749)), ((175, 760), (171, 706)),
        }.Select(w => (Way: WalkingPath(sinnoh, w.Item1, w.Item2), From: w.Item1)).ToList();

        var watch = soakWatch = new SoakWatch();
        var laps = new List<SoakLap>();
        var clock = System.Diagnostics.Stopwatch.StartNew();
        Console.WriteLine($"== soak: laps for {minutes:0.#} minutes, at least three");

        for (int lap = 1; lap <= 3 || clock.Elapsed.TotalMinutes < minutes; lap++)
        {
            watch.Lap = lap;
            var lapClock = System.Diagnostics.Stopwatch.StartNew();

            // The three walks, there and back, eight frames a tile held to a sixtieth of a second each
            watch.Part = "walks";
            watch.Paced = true;
            foreach (var (way, from) in walks)
            {
                At("Sinnoh", from.Item1, from.Item2, Direction.Up);
                foreach (var tiles in new[] { way, Enumerable.Reverse(way).Append(from).ToList() })
                    foreach (var (x, y) in tiles)
                    {
                        walker.SetPosition(x, y, Direction.Up);
                        Frames(8);
                    }
            }
            watch.Paced = false;

            // Into a Pokémon Center and out again, by the game's own steps through its door
            watch.Part = "pokemon center";
            At("Sinnoh", doorX, doorY + 2, Direction.Up);
            Steer(Direction.Up, () => game.Map != sinnoh, 300);
            Frames(60);
            Steer(Direction.Down, () => game.Map == sinnoh, 400);
            Frames(30);

            // A wild battle and a trainer's, each won
            watch.Part = "wild battle";
            At("Sinnoh", 115, 854, Direction.Up);
            game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Bidoof", MinLevel = 5, MaxLevel = 5, Weight = 100 });
            WinTheBattle();
            watch.Part = "trainer battle";
            At("Sinnoh", 115, 854, Direction.Up);
            if (TrainerDatabase.Get("youngster_tristan") is { } record)
            {
                var trainer = new Trainer { Id = record.Id };
                TrainerDatabase.Fill(trainer, record);
                game.StartTrainerBattle(new NPC { Name = trainer.Name, NpcType = "Youngster", TrainerData = trainer });
                WinTheBattle();
            }

            // The evolution scene, from its notice to its congratulations
            watch.Part = "evolution";
            var turtwig = new Pokemon(PokemonDatabase.Get("Turtwig")!, 18, Gender.Male, Nature.Hardy, false);
            var context = new EvolutionContext { Party = party, Bag = inventory };
            var scene = game.Evolution;
            game.State = GameState.Evolution;
            scene.Begin(turtwig, Evolution.Find(turtwig, EvolutionTrigger.LevelUp, context)!, context, cancellable: false);
            for (int i = 0; i < 2400 && scene.IsActive; i++)
            {
                if (scene.Phase == EvolutionPhase.Congratulate && i % 30 == 29) scene.PressConfirm();
                Frames(1);
            }
            for (int i = 0; i < 8 && scene.IsActive; i++) { scene.PressConfirm(); Frames(2); }
            game.State = GameState.Overworld;

            // The National Pokédex from Bulbasaur to Pecharunt, a species a frame
            watch.Part = "pokedex";
            var dex = game.PokedexScreen;
            game.State = GameState.PokedexMenu;
            dex.Open(pokedex, game.World.Portrait(PlayerIdentity.Character));
            for (int i = 0; i < 4 && dex.Focus == PokedexFocus.Diploma; i++) { Frames(20); dex.Confirm(); }
            dex.OpenSearch();
            while (dex.DraftMode != Pokedex.National) dex.Sideways(1);
            dex.Search();
            Frames(2);
            for (int i = 0; i < dex.Rows.Count - 1; i++) { dex.Move(1, held: true); Frames(1); }
            for (int i = 0; i < 600 && PokemonSprites.Busy; i++) Frames(1);
            dex.Close();

            // Every box, once
            watch.Part = "boxes";
            game.State = GameState.PCStorage;
            var pc = game.PcScreen;
            pc.Open(boxes);
            pc.Zone = StorageZone.BoxName;
            Frames(2);
            for (int i = 0; i < boxes.Boxes.Count; i++) { pc.Move(1, 0, party.Count); Frames(1); }
            for (int i = 0; i < 600 && PokemonSprites.Busy; i++) Frames(1);
            pc.Close();
            game.State = GameState.Overworld;

            // Home: the player's door by day, and a second for what was let go to go
            watch.Part = "home";
            At("Sinnoh", 116, 886, Direction.Down);
            Frames(PokemonSprites.FreshFrames + 10);

            laps.Add(watch.Return(lap, lapClock.Elapsed.TotalSeconds, game.World.LoadedChunks));
            var now = laps[^1];
            Console.WriteLine($"  lap {lap}: {now.Seconds:0} s, {now.Frames} frames (median {now.Median:0.0} ms, 99% {now.P99:0.0} ms), {now.Hitches} over 16.7 ms; "
                + $"the ledger {now.Gpu.Total} things, {now.Gpu.Bytes / 1048576.0:0} MB; managed {now.Managed / 1048576.0:0.0} MB, {now.Gen2} gen-2 collections, working set {now.WorkingSet / 1048576.0:0} MB");
            foreach (string fault in Faults(laps)) Console.WriteLine("  !! " + fault);
        }
        soakWatch = null;

        var faults = laps.SelectMany((_, i) => Faults(laps.Take(i + 1).ToList())).Distinct().ToList();
        WriteSoakReport(laps, watch, faults, minutes);
        if (faults.Count > 0) Environment.ExitCode = 1;

        // Steers the player with the game's own steps until something holds
        void Steer(Direction way, Func<bool> until, int most)
        {
            engine.Steering = (way, false);
            for (int i = 0; i < most && !until(); i++) Frames(1);
            engine.Steering = (null, false);
            if (!until()) Console.WriteLine($"  !! lap {watch.Lap}: never got through the Pokémon Center's door ({way})");
        }

        // Waits for the battle, wins it with the lead (every foe left a single HP), and reads on to the field
        void WinTheBattle()
        {
            for (int i = 0; i < 900 && game.State != GameState.Battle; i++) Frames(1);
            if (game.Battle is not { } fight) { Console.WriteLine($"  !! lap {watch.Lap}: the {watch.Part} never began"); return; }
            for (int guard = 0; guard < 400 && !fight.IsBattleOver; guard++)
            {
                if (fight.IsOfferingSwitch) fight.StayIn();
                else if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    foreach (var foe in fight.EnemyParty.Members) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    fight.SelectMainMenuOption(0);
                    fight.SelectMove(0);
                    if (fight.HUD.MenuState == BattleMenuState.SelectTarget) fight.SelectTarget(0);
                }
                else if (fight.HUD.MenuState is BattleMenuState.Message or BattleMenuState.LearnMove) fight.ConfirmMessage();
                Skip(0.5);
            }
            for (int i = 0; i < 1500 && !FieldAgain(); i++)
            {
                if (game.Battle is { } b && b.HUD.MenuState is BattleMenuState.Message or BattleMenuState.LearnMove) b.ConfirmMessage();
                Frames(1);
            }
            if (game.State != GameState.Overworld) { Console.WriteLine($"  !! lap {watch.Lap}: the {watch.Part} left the game at {game.State}"); game.State = GameState.Overworld; }
        }
    }

    // What the soak run counts while it runs, or nothing
    SoakWatch? soakWatch;

    // One frame of the game under the soak run's watch: the update and the draw timed, the bytes and collections counted
    void SoakFrame(bool draw)
    {
        var w = soakWatch!;
        FrameClock.Fixed = ++tick / 60.0;
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
        var paused = GC.GetTotalPauseDuration();
        int models = PokemonModels.BuiltCount;
        long start = System.Diagnostics.Stopwatch.GetTimestamp();
        engine.Update(1f / 60f);
        double update = System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        double drawn = 0;
        if (draw)
        {
            long before = System.Diagnostics.Stopwatch.GetTimestamp();
            engine.Draw();
            drawn = System.Diagnostics.Stopwatch.GetElapsedTime(before).TotalMilliseconds;
        }
        w.Count(new SoakFrameReading(update, drawn, draw, GC.GetAllocatedBytesForCurrentThread() - allocated,
            GC.CollectionCount(0) - gen0, GC.CollectionCount(1) - gen1, GC.CollectionCount(2) - gen2,
            (GC.GetTotalPauseDuration() - paused).TotalMilliseconds,
            draw ? game.World.Streaming.LastFrameMs : 0, draw ? PokemonSprites.LastServiceMs : 0, PokemonModels.BuiltCount > models));
        // A walk's frames are held to the screen's refresh, as in play: the chunks ahead get the time to bake they get there
        if (w.Paced)
            while (System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds < 1000.0 / 60.0) Thread.Sleep(1);
    }

    // Each lap after the second held to it: no owner's count of the ledger higher, the managed heap no more than 5 % bigger
    static IEnumerable<string> Faults(List<SoakLap> laps)
    {
        if (laps.Count < 3) yield break;
        var second = laps[1];
        var now = laps[^1];
        foreach (var row in now.Gpu.Rows)
        {
            int then = second.Gpu.Count(row.Kind, row.Owner);
            if (row.Count > then) yield return $"lap {now.Number}: the ledger holds {row.Count} {row.Kind} of {row.Owner}, where the second lap held {then}";
        }
        if (now.Managed > second.Managed * 1.05) yield return $"lap {now.Number}: the managed heap is {now.Managed / 1048576.0:0.0} MB after a collection, where the second lap's was {second.Managed / 1048576.0:0.0} MB";
    }

    void WriteSoakReport(List<SoakLap> laps, SoakWatch watch, List<string> faults, double minutes)
    {
        var md = new System.Text.StringBuilder();
        md.AppendLine("# The soak run");
        md.AppendLine();
        md.AppendLine($"{laps.Count} laps in {laps.Sum(l => l.Seconds) / 60:0.0} minutes (asked for {minutes:0.#}); the window {Raylib.GetScreenWidth()}×{Raylib.GetScreenHeight()}. Milliseconds mean something on the Windows machine only.");
        md.AppendLine();
        md.AppendLine(faults.Count == 0 ? "Nothing grew after the second lap." : "**What grew after the second lap:**");
        foreach (string fault in faults) md.AppendLine($"- {fault}");
        md.AppendLine();
        md.AppendLine("| Lap | Seconds | Frames drawn | Median ms | 99% ms | Over 16.7 ms | Allocated KB a frame | Ledger things | Ledger MB | Chunks | Managed MB | Gen-2 | Working set MB |");
        md.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
        foreach (var l in laps)
            md.AppendLine($"| {l.Number} | {l.Seconds:0} | {l.Frames} | {l.Median:0.0} | {l.P99:0.0} | {l.Hitches} | {l.AllocatedPerFrame / 1024.0:0.0} | {l.Gpu.Total} | {l.Gpu.Bytes / 1048576.0:0} | {l.Chunks} | {l.Managed / 1048576.0:0.0} | {l.Gen2} | {l.WorkingSet / 1048576.0:0} |");
        md.AppendLine();
        md.AppendLine("## The ledger at the last return, by owner");
        md.AppendLine();
        md.AppendLine("| Owner | Kind | Count | MB | At the second lap |");
        md.AppendLine("|---|---|---:|---:|---:|");
        if (laps.Count > 0)
            foreach (var row in laps[^1].Gpu.Rows)
                md.AppendLine($"| {row.Owner} | {row.Kind} | {row.Count} | {row.Bytes / 1048576.0:0.0} | {(laps.Count > 1 ? laps[1].Gpu.Count(row.Kind, row.Owner).ToString() : "")} |");
        md.AppendLine();
        md.AppendLine("## Frames over 16.7 ms and their causes");
        md.AppendLine();
        foreach (var cause in watch.Hitches.GroupBy(h => h.Cause).OrderByDescending(g => g.Count()))
            md.AppendLine($"- {cause.Key}: {cause.Count()}");
        md.AppendLine();
        md.AppendLine("| Lap | Part | Frame | Update ms | Draw ms | Uploads ms | Sprites ms | Collection pause ms | Cause |");
        md.AppendLine("|---:|---|---:|---:|---:|---:|---:|---:|---|");
        foreach (var h in watch.Hitches.OrderByDescending(h => h.Reading.UpdateMs + h.Reading.DrawMs).Take(200))
            md.AppendLine($"| {h.Lap} | {h.Part} | {h.Frame} | {h.Reading.UpdateMs:0.0} | {h.Reading.DrawMs:0.0} | {h.Reading.UploadMs:0.0} | {h.Reading.SpritesMs:0.0} | {h.Reading.PauseMs:0.0} | {h.Cause} |");
        File.WriteAllText(Path.Combine(outDir, "soak.md"), md.ToString());
        Console.WriteLine("wrote soak.md");

        // The counts and the memory lap by lap, each line drawn against its own most
        const int W = 1600, H = 900, Left = 80, Top = 70, Plot = 700;
        var img = Raylib.GenImageColor(W, H, new Color(24, 26, 34, 255));
        Raylib.ImageDrawText(ref img, "soak: lap by lap, each line against its own most", Left, 20, 30, Color.White);
        var lines = new (string Label, Color Colour, Func<SoakLap, double> Of)[]
        {
            ("ledger things", new Color(120, 200, 255, 255), l => l.Gpu.Total),
            ("ledger MB", new Color(90, 140, 255, 255), l => l.Gpu.Bytes),
            ("managed MB", new Color(255, 190, 90, 255), l => l.Managed),
            ("working set MB", new Color(255, 120, 120, 255), l => l.WorkingSet),
            ("frames over 16.7 ms", new Color(160, 230, 140, 255), l => l.Hitches),
        };
        Raylib.ImageDrawRectangleLines(ref img, new Rectangle(Left, Top, W - Left * 2, Plot), 2, new Color(90, 94, 110, 255));
        for (int k = 0; k < lines.Length; k++)
        {
            var (label, colour, of) = lines[k];
            double most = Math.Max(1e-9, laps.Max(of));
            for (int i = 0; i < laps.Count; i++)
            {
                int x = Left + (laps.Count == 1 ? 0 : i * (W - Left * 2) / (laps.Count - 1));
                int y = Top + Plot - (int)(of(laps[i]) / most * (Plot - 10));
                Raylib.ImageDrawCircle(ref img, x, y, 4, colour);
                if (i > 0)
                {
                    int px = Left + (i - 1) * (W - Left * 2) / (laps.Count - 1);
                    int py = Top + Plot - (int)(of(laps[i - 1]) / most * (Plot - 10));
                    Raylib.ImageDrawLine(ref img, px, py, x, y, colour);
                }
            }
            Raylib.ImageDrawText(ref img, $"{label} (most {(label.EndsWith("MB") ? most / 1048576.0 : most):0.#})", Left + k * 290, Top + Plot + 30, 20, colour);
        }
        for (int i = 0; i < laps.Count; i++)
            Raylib.ImageDrawText(ref img, laps[i].Number.ToString(), Left + (laps.Count == 1 ? 0 : i * (W - Left * 2) / (laps.Count - 1)) - 4, Top + Plot + 6, 16, Color.White);
        Raylib.ExportImage(img, Path.Combine(outDir, "soak.png"));
        Raylib.UnloadImage(img);
        Console.WriteLine("wrote soak.png");
    }
}

/// <summary>One frame of the soak run.</summary>
readonly record struct SoakFrameReading(double UpdateMs, double DrawMs, bool Drawn, long Allocated, int Gen0, int Gen1, int Gen2,
    double PauseMs, double UploadMs, double SpritesMs, bool ModelMade);

/// <summary>A frame of the soak run over 16.7 ms, with what it was doing and why it was slow.</summary>
record SoakHitch(int Lap, string Part, int Frame, SoakFrameReading Reading, string Cause);

/// <summary>A lap's return: its frames and what the game held as it came home.</summary>
record SoakLap(int Number, double Seconds, int Frames, double Median, double P99, int Hitches, double AllocatedPerFrame,
    PokemonPlatinumEngine.Graphics.GpuSnapshot Gpu, long Managed, int Gen2, long WorkingSet, int Chunks);

/// <summary>The soak run's counts as it goes (plan 16 · T22).</summary>
sealed class SoakWatch
{
    public const double Budget = 1000.0 / 60.0;
    public int Lap;
    public string Part = "";
    public bool Paced;
    public readonly List<SoakHitch> Hitches = new();
    readonly List<double> times = new();
    int frames, hitches;
    long allocated;

    public void Count(SoakFrameReading r)
    {
        frames++;
        allocated += r.Allocated;
        if (!r.Drawn) return;
        double total = r.UpdateMs + r.DrawMs;
        times.Add(total);
        if (total <= Budget) return;
        hitches++;
        Hitches.Add(new SoakHitch(Lap, Part, frames, r, CauseOf(r)));
    }

    // The largest of what T21 measures that can make a frame long; a model made ready says so when nothing else does
    static string CauseOf(SoakFrameReading r)
    {
        var known = new (string Name, double Ms)[]
        {
            ("chunk uploads", r.UploadMs), ("sprite baking", r.SpritesMs),
            (r.Gen2 > 0 ? "a full collection" : "a collection", r.Gen0 + r.Gen1 + r.Gen2 > 0 ? Math.Max(r.PauseMs, 0.01) : 0),
        };
        var (name, ms) = known.MaxBy(k => k.Ms);
        if (ms >= 2) return name;
        if (r.ModelMade) return "a model made ready (perhaps meshed on the main thread)";
        return ms > 0 ? name : "none known";
    }

    public SoakLap Return(int number, double seconds, int chunks)
    {
        var sorted = times.OrderBy(t => t).ToList();
        double At(double share) => sorted.Count == 0 ? 0 : sorted[Math.Min(sorted.Count - 1, (int)(share * sorted.Count))];
        long managed = GC.GetTotalMemory(forceFullCollection: true);
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        process.Refresh();
        var lap = new SoakLap(number, seconds, times.Count, At(0.5), At(0.99), hitches, frames == 0 ? 0 : (double)allocated / frames,
            PokemonPlatinumEngine.Graphics.GpuLedger.Game.Snapshot(), managed, GC.CollectionCount(2), process.WorkingSet64, chunks);
        times.Clear();
        frames = hitches = 0;
        allocated = 0;
        return lap;
    }
}
