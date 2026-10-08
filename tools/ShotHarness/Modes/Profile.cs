partial class Harness
{
    // ---------------------------------------------------------------- where the time goes (plan 04 · G11)

    // Not part of `all`: the heaviest scenes of the game, each timed and then taken apart by the profiler. Run it by
    // itself, with nothing else running, and with SHOTS_WINDOW=3840x2160 for the game as it is full screen. Each
    // scene's median, spread and passes go to profile.json, which `ab` compares (tools/dev/ab.sh runs the three).
    public void ProfileMode()
    {
        profileRun.Renderer = Gl.Renderer();
        profileRun.Window = $"{Raylib.GetScreenWidth()}x{Raylib.GetScreenHeight()}";
        Console.WriteLine($"window {profileRun.Window}, renderer {profileRun.Renderer ?? "unknown"}, preset {engine.Settings.Quality}, profiler waits for the GPU: {FrameProfiler.WaitsForGpu}");
        Console.WriteLine($"start-up: {startMs:F0} ms to initialise the engine");
        // Arriving somewhere new: its chunks baked and uploaded behind the fade
        var fieldRenderer = game.World;
        fieldRenderer.ResetStreamingStats();
        var arrival = System.Diagnostics.Stopwatch.StartNew();
        game.Map = MapDatabase.Get("Sinnoh");
        game.Player.SetPosition(178, 845, Direction.Down);
        game.State = GameState.Overworld;
        Frames(1);
        double firstFrame = arrival.Elapsed.TotalMilliseconds;
        Frames(1);
        Console.WriteLine($"arriving in a town not seen yet: {firstFrame:F0} ms to its first frame, {arrival.Elapsed.TotalMilliseconds - firstFrame:F0} ms for the next");
        Console.WriteLine($"  streaming: {fieldRenderer.Streaming}");
        var presets = (Environment.GetEnvironmentVariable("SHOTS_PRESETS") ?? "High").Split(',');
        foreach (string preset in presets)
        {
            engine.Settings.Quality = Enum.Parse<GraphicsQuality>(preset);
            profilePreset = preset;
            engine.Settings.TimeOfDay = TimeOfDay.Day;
            engine.ApplySettings(window: false);
            Console.WriteLine($"--- {preset}");

            At("Sinnoh", 112, 880, Direction.Down); Frames(40); Profile("twinleaf town");
            At("Sinnoh", 115, 854, Direction.Up); Frames(40); Profile("route 201");
            At("Sinnoh", 110, 850, Direction.Up); Frames(40); Profile("route 201, in the trees");
            At("Sinnoh", 178, 845, Direction.Down); Frames(40); Profile("sandgem town");
            At("Sinnoh", 174, 815, Direction.Up); Frames(40); Profile("route 202");
            At("LakeVerity", 44, 46, Direction.Up); Frames(40); Profile("lake verity");
            At("Sinnoh", 175, 792, Direction.Up); Frames(40); Profile("jubilife city");
            At("Sinnoh", 302, 784, Direction.Down); Frames(40); Profile("oreburgh's mine yard");
            At("OreburghGate1F", 16, 22, Direction.Right); Frames(40); Profile("oreburgh gate");
            At("PokemonCenter", 5, 6, Direction.Up); Frames(40); Profile("pokemon center");

            engine.Settings.TimeOfDay = TimeOfDay.Night;
            engine.ApplySettings(window: false);
            At("Sinnoh", 112, 880, Direction.Down); Frames(40); Profile("twinleaf town at night");
            engine.Settings.TimeOfDay = TimeOfDay.Day;
            engine.ApplySettings(window: false);

            var town = MapDatabase.Get("Sinnoh").AreaAt(112, 880);
            town.Weather = FieldWeather.HeavyRain;
            At("Sinnoh", 112, 880, Direction.Down); Frames(40); Profile("twinleaf town in heavy rain");
            town.Weather = FieldWeather.Fog;
            Frames(40); Profile("twinleaf town in fog");
            town.Weather = FieldWeather.Clear;

            // A wild battle at its menu, then the scripted battle's heaviest moments, each held still
            party.HealAll();
            var wild = StartBattle("Shinx", 5);
            ToMainMenu(wild);
            Profile("battle, at the menu");

            var show = new Party();
            var lead = new Pokemon(PokemonDatabase.Get("Infernape")!, 60, new Random(3));
            show.Add(lead);
            var rival = new Trainer { Name = "Barry", TrainerClass = "Rival" };
            var torterra = new Pokemon(PokemonDatabase.Get("Torterra")!, 90, new Random(4));
            rival.Party.Add(torterra);
            game.Map = MapDatabase.Get("Sinnoh");
            game.BattleRenderer.SetArena(BattleArena.Grass);
            var b = new BattleEngine(new BattleSetup
            {
                PlayerParty = show, Inventory = inventory, Pokedex = pokedex, Trainers = new List<Trainer> { rival }, Random = new Random(11)
            });
            game.Battle = b;
            game.State = GameState.Battle;
            Skip(1.0); Profile("trainer battle, both trainers in the sweep", 120, frozen: true);
            Skip(1.05); b.ConfirmMessage(); Frames(1);
            Skip(1.4); b.ConfirmMessage(); Frames(1);
            Skip(0.85);
            for (int guard = 0; guard < 6 && b.HUD.MenuState == BattleMenuState.Message; guard++) { b.ConfirmMessage(); Frames(1); Skip(0.3); }
            Skip(0.6);
            foreach (string move in new[] { "Flamethrower", "Surf", "Earthquake", "Close Combat" })
            {
                Skip(1.0);
                lead.Moves.Clear();
                lead.Moves.Add(new Move(MoveDatabase.Get(move)!));
                torterra.Moves.Clear();
                torterra.Moves.Add(new Move(MoveDatabase.Get("Splash")!));
                lead.CurrentHP = lead.MaxHP;
                b.EnemyPokemon.CurrentHP = b.EnemyPokemon.MaxHP;
                b.EnemyPokemon.StatStages[StatType.Speed] = -6;
                b.SelectMove(0);
                Skip(0.12); Profile($"{move}, winding up", 120, frozen: true);
                Skip(0.16); Profile($"{move}, arriving", 120, frozen: true);
                Skip(0.12); Profile($"{move}, landing", 120, frozen: true);
                for (int guard = 0; guard < 12 && b.HUD.MenuState == BattleMenuState.Message && !b.IsBattleOver; guard++) { b.ConfirmMessage(); Frames(1); Skip(0.35); }
            }

            // Two trainers and four Pokémon
            party.HealAll();
            var one = new Trainer { Name = "Ana", TrainerClass = "Youngster" };
            one.Party.Add(new Pokemon(PokemonDatabase.Get("Shinx")!, 6));
            var two = new Trainer { Name = "Cal", TrainerClass = "Lass" };
            two.Party.Add(new Pokemon(PokemonDatabase.Get("Piplup")!, 6));
            var d = new BattleEngine(new BattleSetup
            {
                PlayerParty = party, Inventory = inventory, Pokedex = pokedex, Format = BattleFormat.Double,
                Trainers = new List<Trainer> { one, two }, WildPokemon = new List<Pokemon>(), Random = new Random(5)
            });
            game.BattleRenderer.SetArena(game.Map);
            game.Battle = d;
            game.State = GameState.Battle;
            Skip(130 / 60.0); Profile("double battle, two trainers in the sweep", 120, frozen: true);
            d.ConfirmMessage(); Frames(1); Skip(70 / 60.0);
            d.ConfirmMessage(); Frames(1); Skip(70 / 60.0);
            for (int guard = 0; guard < 6 && d.HUD.MenuState == BattleMenuState.Message; guard++) { d.ConfirmMessage(); Frames(1); Skip(20 / 60.0); }
            Skip(0.8);
            Profile("double battle, at the menu");
            game.State = GameState.Overworld;
        }
        engine.Settings.Quality = GraphicsQuality.High;
        engine.ApplySettings(window: false);
        // What tools/dev/ab.sh hands to `ab`
        profileRun.Save(Path.Combine(outDir, "profile.json"));
        Console.WriteLine("wrote profile.json");
    }
}
