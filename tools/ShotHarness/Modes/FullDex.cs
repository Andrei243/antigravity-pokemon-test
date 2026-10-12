partial class Harness
{
    // ---------------------------------------------------------------- the game with every species in it (plan 03 · D13)

    // Not part of `all`: the National Pokédex complete and the boxes full, weighed and timed. It prints where the
    // start went, what a Pokémon of every species and form costs to make, what an upload leaves behind once it is
    // freed, how the Pokédex's list and the PC's boxes run with everything in them (the frames, the menu sprites and
    // the models held, the memory), what a save of it all weighs and how long it takes to write and read, and what
    // a run of battles against species never met leaves in memory; and it shoots the screens a full game shows
    // (fd*). A line that begins with !! is something that should not be: memory that grew where it must not, more
    // models or sprites held than the game's bounds allow. FULLDEX_ONLY=start,data,uploads,pokedex,boxes,save,
    // battles,shots runs only those parts, and FULLDEX_WALK=150 goes only that far down the Pokédex. Its
    // milliseconds mean something only on a quiet machine; its counts and its megabytes are the same on any.
    // With a POKEMON_CACHE that has no sprites in it, the walk down the list bakes every one: the first time a
    // player opens a complete Pokédex.
    public void FullDexMode()
    {
        var only = (Environment.GetEnvironmentVariable("FULLDEX_ONLY") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        bool Part(string name) => only.Length == 0 || only.Contains(name, StringComparer.OrdinalIgnoreCase);
        var context = game.RenderContext;
        var everyone = PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).ToList();
        var forms = everyone.SelectMany(s => (s.Forms ?? new()).Select(f => f.Name)).ToList();

        if (Part("start"))
        {
            Console.WriteLine($"== the start: {startMs:0} ms to initialise the engine, {everyone.Count} species and {forms.Count} forms in the data");
            // Where it went, step by step, the longest first (the log's phases, plan 16 · T3)
            foreach (var phase in Log.Phases.OrderByDescending(p => p.Milliseconds)) Console.WriteLine($"  {phase.Name}: {phase.Milliseconds:0} ms");
            Weigh("after the start");
        }

        if (Part("data"))
        {
            // One Pokémon of every species and of every form, at three levels: what the data costs to use
            var clock = System.Diagnostics.Stopwatch.StartNew();
            int made = 0, moves = 0;
            foreach (int level in new[] { 5, 50, 100 })
            {
                foreach (var species in everyone) { moves += new Pokemon(species, level).Moves.Count; made++; }
                foreach (string form in forms) { moves += Meet(form, level).Moves.Count; made++; }
            }
            Console.WriteLine($"== the data: {made} Pokémon made in {clock.Elapsed.TotalMilliseconds:0} ms ({clock.Elapsed.TotalMilliseconds * 1000 / made:0} µs each), {moves} moves between them");
            clock.Restart();
            foreach (var species in everyone) PokemonModels.Signature(species.Name);
            foreach (string form in forms) PokemonModels.Signature(form);
            Console.WriteLine($"  a signature for each of their {everyone.Count + forms.Count} models: {clock.Elapsed.TotalMilliseconds:0} ms ({clock.Elapsed.TotalMilliseconds / (everyone.Count + forms.Count):0.00} ms each)");
        }

        if (Part("uploads"))
        {
            // What a model leaves behind once it has been drawn and let go again, two hundred times over: nothing.
            // (Each skinned mesh's two bone buffers once stayed: 2 MB a mesh, for every battle's foe.)
            Console.WriteLine("== uploads: a model drawn and let go two hundred times");
            var model = PokemonModels.Get("Torterra");
            var target = Raylib.LoadRenderTexture(128, 128);
            for (int i = 0; i < 10; i++) { PokemonSprites.Render(context, model, SpriteView.Front, new PokePose { Time = 0.35f }, target); PokemonRenderer.Unload(model); }
            Gl.Finish();
            long before = Weigh("before");
            for (int i = 0; i < 200; i++) { PokemonSprites.Render(context, model, SpriteView.Front, new PokePose { Time = 0.35f }, target); PokemonRenderer.Unload(model); }
            Gl.Finish();
            long grew = Weigh("after") - before;
            Raylib.UnloadRenderTexture(target);
            PokemonModels.Release("Torterra");
            Console.WriteLine($"  {(grew > 100 << 20 ? "!! " : "")}the process grew by {grew / 1048576.0:0} MB");
        }

        // The whole National Pokédex seen and caught, and the PC as it is once the Pokédex is the National one
        foreach (var species in everyone) pokedex.RegisterCaught(species.DexNumber);
        pokedex.UnlockNational();
        game.Story.Set("SinnohHallOfFame");

        if (Part("pokedex"))
        {
            Console.WriteLine($"== the Pokédex: {pokedex.CaughtCount} caught of {everyone.Count}");
            var dex = game.PokedexScreen;
            game.State = GameState.PokedexMenu;
            dex.Open(pokedex, game.World.Portrait(PlayerIdentity.Character));
            // The diplomas come first, one after the other
            for (int i = 0; i < 4 && dex.Focus == PokedexFocus.Diploma; i++) { Frames(20); dex.Confirm(); }
            dex.OpenSearch();
            while (dex.DraftMode != PokedexMode.National) dex.Sideways(1);
            dex.Search();
            Frames(2);
            Console.WriteLine($"  the list shows {dex.Rows.Count} species under {dex.Mode}");
            long opened = Weigh("the Pokédex open");

            // Down the whole list a species a frame, which is faster than a key held down takes it (one each 0.04 s)
            int far = int.TryParse(Environment.GetEnvironmentVariable("FULLDEX_WALK"), out int asked) ? Math.Min(asked, dex.Rows.Count - 1) : dex.Rows.Count - 1;
            var pass = Walk(far, () => dex.Move(1, held: true));
            Console.WriteLine($"  down the list, first time: {pass}");
            Weigh("the list walked once");
            // The sprites still on their way
            var settle = System.Diagnostics.Stopwatch.StartNew();
            int waited = 0;
            while (PokemonSprites.Busy && settle.Elapsed.TotalSeconds < 900) { Frames(1); waited++; }
            Console.WriteLine($"  the sprites still to come took {waited} frames and {settle.Elapsed.TotalSeconds:0.0} s more");
            long walked = Weigh("every sprite made");
            Bounds("after the walk");
            // Every species' sprite has been made and most let go again: a few hundred megabytes is a model kept for each
            if (walked - opened > 500 << 20) Console.WriteLine($"  !! the walk left {(walked - opened) / 1048576.0:0} MB behind");
            pass = Walk(far, () => dex.Move(-1, held: true));
            Console.WriteLine($"  back up the list, everything made: {pass}");

            // An entry's pages from one species to the next
            dex.Confirm();
            pass = Walk(Math.Min(200, far), () => dex.Move(1, held: true));
            Console.WriteLine($"  through {Math.Min(200, far)} entries: {pass}");
            Weigh("the entries shown");
            dex.Cancel();

            // The search's orders over everything
            var search = System.Diagnostics.Stopwatch.StartNew();
            foreach (var order in Enum.GetValues<PokedexOrder>())
                PokedexSearch.Run(pokedex, PokedexMode.National, new PokedexQuery(order));
            Console.WriteLine($"  the search's {Enum.GetValues<PokedexOrder>().Length} orders over the National Pokédex: {search.Elapsed.TotalMilliseconds:0.0} ms");
            dex.Close();
            game.State = GameState.Overworld;
        }

        if (Part("boxes"))
        {
            // Platinum's eighteen boxes take 540 of them; the boxes that come with the National Pokédex take them all
            var boxes = game.Boxes = new PcBoxes();
            int stored = everyone.TakeWhile(species => boxes.Store(new Pokemon(species, 50)) != null).Count();
            Console.WriteLine($"== the boxes: {stored} of {everyone.Count} species in Platinum's {boxes.Boxes.Count} boxes of {PcBoxes.BoxSize}, full: {boxes.IsFull}");
            boxes.Grow();
            foreach (var species in everyone.Skip(stored)) if (boxes.Store(new Pokemon(species, 50)) != null) stored++;
            Console.WriteLine($"  {(stored < everyone.Count ? "!! " : "")}{stored} in the National Pokédex's {boxes.Boxes.Count} boxes, {boxes.Free} places left");
            game.State = GameState.PCStorage;
            var pc = game.PcScreen;
            pc.Open(boxes);
            pc.Zone = StorageZone.BoxName;
            Frames(2);
            var pass = Walk(boxes.Boxes.Count * 2, () => pc.Move(1, 0, party.Count));
            Console.WriteLine($"  through every box twice: {pass}");
            for (int i = 0; i < 3600 && PokemonSprites.Busy; i++) Frames(1);
            Weigh("every box shown");
            // Every box was on the screen a moment ago, so every sprite is still fresh: once that has passed, the bound holds
            Frames(PokemonSprites.FreshFrames + 4);
            Weigh("two seconds on");
            Bounds("after the boxes");
            pc.Close();
            game.State = GameState.Overworld;
        }

        if (Part("save"))
        {
            if (game.Boxes.Count == 0)
            {
                game.Boxes.Grow();
                foreach (var species in everyone) game.Boxes.Store(new Pokemon(species, 50));
            }
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var save = game.BuildSave();
            double build = clock.Elapsed.TotalMilliseconds;
            clock.Restart();
            string json = SaveManager.Write(save);
            double write = clock.Elapsed.TotalMilliseconds;
            clock.Restart();
            var read = SaveManager.Read(json)!;
            double parse = clock.Elapsed.TotalMilliseconds;
            clock.Restart();
            var back = read.Boxes!.ToBoxes();
            double restore = clock.Elapsed.TotalMilliseconds;
            string path = Path.Combine(outDir, "fulldex-save.json");
            clock.Restart();
            SaveManager.SaveGame(save, path);
            double disk = clock.Elapsed.TotalMilliseconds;
            Console.WriteLine($"== the save: {json.Length / 1024.0:0} KB with {game.Boxes.Count} Pokémon in {game.Boxes.Boxes.Count} boxes and {pokedex.CaughtCount} in the Pokédex");
            Console.WriteLine($"  built in {build:0.0} ms, written as text in {write:0.0} ms, to the disk with its backup in {disk:0.0} ms; read in {parse:0.0} ms, its boxes made again in {restore:0.0} ms");
            if (back.Count != game.Boxes.Count || back.Boxes.Count != game.Boxes.Boxes.Count) Console.WriteLine($"  !! read back as {back.Count} Pokémon in {back.Boxes.Count} boxes");
            foreach (string file in new[] { path, SaveManager.BackupOf(path), SaveManager.TempOf(path) }) File.Delete(file);
        }

        if (Part("battles"))
        {
            // Wild battles against species never met, one after another: what the models and the cries leave held
            Console.WriteLine("== battles against forty species never met");
            var foes = everyone.Where(s => s.DexNumber > 493).Where((_, i) => i % 13 == 0).Take(40).ToList();
            // A lead nothing outruns, so RUN is the way out of every one
            party.Members.Insert(0, new Pokemon(PokemonDatabase.Get("Garchomp")!, 100));
            int fought = 0;
            long afterTen = 0, last = 0;
            foreach (var foe in foes)
            {
                game.Place(MapDatabase.Get("Sinnoh"), 115, 854, Direction.Up);
                game.StartWildBattle(new WildEncounterEntry { SpeciesName = foe.Name, MinLevel = 30, MaxLevel = 30, Weight = 100 });
                for (int i = 0; i < 600 && game.State != GameState.Battle; i++) Frames(1);
                if (game.Battle is not { } battle) { Console.WriteLine($"  !! no battle against {foe.Name}"); continue; }
                ToMainMenu(battle);
                // Run from it: the game's own way out, which lets the models go
                battle.SelectMainMenuOption(3);
                for (int i = 0; i < 40 && !battle.IsBattleOver; i++) { Skip(0.5); if (battle.HUD.MenuState == BattleMenuState.Message) battle.ConfirmMessage(); else battle.SelectMainMenuOption(3); }
                for (int i = 0; i < 600 && game.State != GameState.Overworld; i++) { if (game.Battle is { } b && b.HUD.MenuState == BattleMenuState.Message) b.ConfirmMessage(); Frames(1); }
                fought++;
                if (fought % 10 != 0) continue;
                last = Weigh($"after {fought} battles");
                if (fought == 10) afterTen = last;
            }
            Bounds("after the battles");
            // The first battles bring the stage, its textures and the cries; after them a battle leaves nothing
            if (last - afterTen > 150 << 20) Console.WriteLine($"  !! thirty battles more left {(last - afterTen) / 1048576.0:0} MB behind");
        }

        if (Part("shots"))
        {
            var dex = game.PokedexScreen;
            pokedex.Diplomas.Clear();
            game.State = GameState.PokedexMenu;
            dex.Open(pokedex, game.World.Portrait(PlayerIdentity.Character));
            Frames(30); Shot("fd01_diploma_sinnoh");
            dex.Confirm();
            Frames(30); Shot("fd02_diploma_national");
            dex.Confirm();
            dex.OpenSearch();
            while (dex.DraftMode != PokedexMode.National) dex.Sideways(1);
            dex.Search();
            Wait(); Shot("fd03_national_list_first");
            for (int i = 0; i < 493; i++) dex.Move(1, held: true);
            Wait(); Shot("fd04_national_list_arceus");
            for (int i = 0; i < 531; i++) dex.Move(1, held: true);
            Wait(); Shot("fd05_national_list_last");
            dex.Confirm();
            Wait(); Shot("fd06_entry_pecharunt");
            dex.Sideways(1);
            Wait(); Shot("fd07_entry_area_unknown");
            dex.Sideways(1);
            Wait(); Shot("fd08_entry_size");
            dex.Close();

            // The PC as the story has it, its eighteen boxes full, and as it is with the National Pokédex
            var boxes = game.Boxes = new PcBoxes();
            int stored = everyone.TakeWhile(species => boxes.Store(new Pokemon(species, 50)) != null).Count();
            game.State = GameState.PCStorage;
            var pc = game.PcScreen;
            pc.Open(boxes);
            pc.Zone = StorageZone.BoxName;
            pc.Move(-1, 0, party.Count);
            Wait(); Shot("fd10_boxes_eighteen_full");
            pc.Close();
            boxes.Grow();
            foreach (var species in everyone.Skip(stored)) boxes.Store(new Pokemon(species, 50));
            pc.Open(boxes);
            Wait(); Shot("fd11_boxes_national_first");
            pc.Zone = StorageZone.BoxName;
            pc.Move(-1, 0, party.Count);
            Wait(); Shot("fd12_boxes_national_last");
            pc.Move(-1, 0, party.Count);
            pc.Move(0, 1, party.Count);
            for (int i = 0; i < 4; i++) pc.Move(1, 0, party.Count);
            Wait(); Shot("fd13_boxes_national_pecharunt");
            pc.Close();
            game.State = GameState.Overworld;
        }

        // A screen's sprites are made over the next frames: wait for them before a shot
        void Wait()
        {
            Frames(3);
            for (int i = 0; i < 3600 && PokemonSprites.Busy; i++) Frames(1);
            Frames(2);
        }
    }

    // A run of steps, a frame after each: how long the frames took
    string Walk(int steps, Action step)
    {
        var times = new List<double>(steps);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        double then = 0;
        for (int i = 0; i < steps; i++)
        {
            step();
            Frames(1);
            double now = clock.Elapsed.TotalMilliseconds;
            times.Add(now - then);
            then = now;
        }
        times.Sort();
        return $"{steps} frames in {then / 1000:0.0} s, median {ProfileRun.Percentile(times, 0.5):0.0} ms, 90% {ProfileRun.Percentile(times, 0.9):0.0} ms, longest {times[^1]:0} ms";
    }

    // What the game holds now: the process's memory (all of it, and what the managed heap has taken from the system
    // whether it holds anything in it or not), what is alive on the managed heap, and the models, menu sprites and
    // cries kept. Gives the process's private bytes
    long Weigh(string label)
    {
        System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        long heap = GC.GetTotalMemory(forceFullCollection: true);
        var info = GC.GetGCMemoryInfo();
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        process.Refresh();
        double MB(long bytes) => bytes / 1048576.0;
        Console.WriteLine($"  [{label}] {MB(process.PrivateMemorySize64):0} MB private ({MB(info.TotalCommittedBytes):0} MB of it the managed heap's, {MB(heap):0} MB alive there), {MB(process.WorkingSet64):0} MB in use; {PokemonModels.BuiltCount} models, {PokemonSprites.KeptCount} species' sprites and {PokemonPlatinumEngine.Audio.Cries.KeptCount} cries held");
        return process.PrivateMemorySize64;
    }

    // What the game may hold however many species it has shown: the menu sprites within their bound (and those a
    // screen is still showing), the models of a team and a scene, the cries within theirs
    void Bounds(string label)
    {
        int sprites = PokemonSprites.KeptCount, models = PokemonModels.BuiltCount, cries = PokemonPlatinumEngine.Audio.Cries.KeptCount;
        if (sprites > PokemonSprites.Kept + 60) Console.WriteLine($"  !! {label}: {sprites} species' sprites are held, where {PokemonSprites.Kept} may be");
        if (models > 24) Console.WriteLine($"  !! {label}: {models} models are held");
        if (cries > PokemonPlatinumEngine.Audio.Cries.Kept) Console.WriteLine($"  !! {label}: {cries} cries are held, where {PokemonPlatinumEngine.Audio.Cries.Kept} may be");
    }
}
