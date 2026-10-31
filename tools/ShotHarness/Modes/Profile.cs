partial class Harness
{
    // ---------------------------------------------------------------- where the time goes (plan 04 · G11)

    // Not part of `all`: the heaviest scenes of the game, each timed and then taken apart by the profiler. Run it by
    // itself, with nothing else running, and with SHOTS_WINDOW=3840x2160 for the game as it is full screen. Each
    // scene's median, spread and passes go to profile.json, which `ab` compares (tools/dev/ab.sh runs the three).
    // SHOTS_LOOK=1 also saves a picture of each scene as it stands when it is timed (profile_<scene>), and with a
    // SHOTS_ONLY that names no scene the mode only goes to each place and times nothing: how a new scene is looked at.
    public void ProfileMode()
    {
        profileRun.Renderer = Gl.Renderer();
        profileRun.Window = $"{Raylib.GetScreenWidth()}x{Raylib.GetScreenHeight()}";
        Console.WriteLine($"window {profileRun.Window}, renderer {profileRun.Renderer ?? "unknown"}, preset {engine.Settings.Quality}, profiler waits for the GPU: {FrameProfiler.WaitsForGpu}");
        Console.WriteLine($"start-up: {startMs:F0} ms to initialise the engine");
        // Where the start-up went, step by step (the log's phases, plan 16 · T3)
        int told = 0;
        void TellPhases()
        {
            var phases = Log.Phases;
            for (; told < phases.Count; told++) Console.WriteLine($"  {phases[told].Name}: {phases[told].Milliseconds:F0} ms");
        }
        TellPhases();
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
        // And the first arrival of the game itself, as its log times it: the place the new game began in, from the
        // moment the player was put there to the end of the first frame drawn (which was the one above)
        TellPhases();
        var presets =(Environment.GetEnvironmentVariable("SHOTS_PRESETS") ?? "High").Split(',');
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
            // The lake from its east shore, the most water in the picture (plan 09 · L10: the mirror's pass is heaviest
            // where the water is widest and the trees stand all round it; its own line is "reflections")
            At("LakeVerity", 51, 40, Direction.Left); Frames(40); Profile("lake verity, its east shore");
            At("Sinnoh", 708, 780, Direction.Up); Frames(40); Profile("valor lakefront");
            At("Sinnoh", 175, 792, Direction.Up); Frames(40); Profile("jubilife city");
            At("Sinnoh", 302, 784, Direction.Down); Frames(40); Profile("oreburgh's mine yard");
            At("OreburghGate1F", 16, 22, Direction.Right); Frames(40); Profile("oreburgh gate");
            At("PokemonCenter", 8, 8, Direction.Up); Frames(40); Profile("pokemon center");

            // The rest of Sinnoh (plan 01 · M12: the places M6 to M11 opened, owed since plan 04 · G11). Towns by
            // the tile Fly lands on, before their Pokémon Centers; routes, caves and rooms by the open tile nearest
            // the middle of their open ground. The date is the harness's (the first of June), so the calendar's
            // weather is the same every run: rain on Route 212's south, snow in the north.
            void Town(string area, string label)
            {
                var spot = SpawnLocations.All.First(s => s.Area == area);
                At("Sinnoh", spot.X, spot.Y, Direction.Down); Frames(40); Profile(label);
            }
            void Place(string area, string label)
            {
                var (map, x, y) = AreaSpot(area);
                At(map.Name, x, y, Direction.Down); Frames(40); Profile(label);
            }
            void Room(string name, string label)
            {
                // A room's own arrival script may speak: its lines are read on, so the room is timed and not a text box
                var map = MapDatabase.Get(name);
                var open = new List<(int X, int Y)>();
                for (int y = 0; y < map.Height; y++)
                    for (int x = 0; x < map.Width; x++)
                        if (map.IsWalkable(x, y) && map.GetWarpAt(x, y) == null) open.Add((x, y));
                double cx = open.Average(t => t.X), cy = open.Average(t => t.Y);
                var (sx, sy) = open.OrderBy(t => (t.X - cx) * (t.X - cx) + (t.Y - cy) * (t.Y - cy)).First();
                At(name, sx, sy, Direction.Up);
                for (int i = 0; i < 40; i++)
                {
                    if (engine.Choice.IsOpen) engine.Choice.Confirm();
                    else if (game.Dialogue.IsActive) { game.Dialogue.FinishLine(); game.Dialogue.Advance(); }
                    Frames(4);
                }
                if (game.Map.Name != name) Console.WriteLine($"  !! {label}: the room's own script took the player to {game.Map.Name}");
                Profile(label);
            }

            // The centre (M6)
            Town("eterna_city", "eterna city");
            Place("route_206", "route 206, the cycling road");
            Place("eterna_forest", "eterna forest");
            Town("hearthome_city", "hearthome city");
            Place("amity_square", "amity square");
            Town("solaceon_town", "solaceon town");
            // The east and the sea (M7)
            Place("route_210_north", "route 210, in the fog");
            Town("celestic_town", "celestic town");
            Town("veilstone_city", "veilstone city");
            Town("pastoria_city", "pastoria city");
            Place("great_marsh_1", "great marsh");
            Place("route_212_south", "route 212, in the rain");
            Town("canalave_city", "canalave city");
            // The north and the end (M8)
            Place("mt_coronet_2f", "mt. coronet, second floor");
            Place("route_216", "route 216, in the snow");
            Town("snowpoint_city", "snowpoint city");
            Town("sunyshore_city", "sunyshore city");
            Place("victory_road_1f", "victory road");
            At("Sinnoh", 847, 560, Direction.Down); Frames(40); Profile("pokemon league");
            Place("distortion_world_b2f", "distortion world");
            // The Battle Zone and the post-game (M10)
            Town("fight_area", "fight area");
            Place("route_228", "route 228, in its sandstorm");
            Town("resort_area", "resort area");
            At("Sinnoh", 750, 248, Direction.Up); Frames(60); Profile("stark mountain's foot");
            Place("stark_mountain_room_1", "stark mountain's great room");
            Place("snowpoint_temple_b1f", "snowpoint temple");
            At("BattleFrontier", 48, 44, Direction.Up); Frames(40); Profile("battle frontier's plaza");
            Place("underground", "the underground");
            // The Gyms and their pieces (M9), and the largest rooms (M11 and the chapters')
            Room("EternaGym", "eterna gym, the flower clock");
            Room("PastoriaGym", "pastoria gym, the water");
            Room("CanalaveGym", "canalave gym, the lifts");
            Room("SnowpointGym", "snowpoint gym, the ice");
            Room("SunyshoreGym", "sunyshore gym, the gears");
            Room("PokemonLeagueHallOfFame", "hall of fame");
            Room("ContestHallLobby", "contest hall");
            Room("VeilstoneStore1F", "department store");
            Room("GalacticHQHall", "galactic hall and its ranks");
            Room("OldChateau", "old chateau");
            Room("BattleTower", "battle tower's lobby");
            Room("GlobalTerminal1F", "global terminal");

            // Hoenn's sea floor (plan 21 · H5, part b): the waves' light laid over the whole picture twice, the diver's
            // bubbles and the kelp swaying round them, by the open sand nearest the middle of Route 124's floor and in
            // the kelp nearest it
            {
                var (sea, sx, sy) = AreaSpot("underwater_route_124");
                At(sea.Name, sx, sy, Direction.Up);
                game.Player.SetMode(TravelMode.Diving);
                Frames(40); Profile("hoenn's sea floor");
                var area = sea.AreaAt(sx, sy);
                var kelp = Enumerable.Range(0, sea.Height).SelectMany(y => Enumerable.Range(0, sea.Width).Select(x => (X: x, Y: y)))
                    .Where(t => sea.AreaAt(t.X, t.Y) == area && sea.GetGroundTile(t.X, t.Y) == TileType.Kelp && sea.IsWalkable(t.X, t.Y))
                    .OrderBy(t => Math.Abs(t.X - sx) + Math.Abs(t.Y - sy)).Select(t => ((int, int)?)(t.X, t.Y)).FirstOrDefault();
                if (kelp is { } k)
                {
                    At(sea.Name, k.Item1, k.Item2, Direction.Up);
                    game.Player.SetMode(TravelMode.Diving);
                    Frames(40); Profile("hoenn's sea floor, in the kelp");
                }
                else Console.WriteLine("  !! no kelp on Route 124's floor");
                game.Player.SetMode(TravelMode.OnFoot);
            }
            // Fortree City (plan 21 · H6): houses built round the trunks, their canopies over the streets, the rope
            // bridges between them, and Route 119 beside it under its rain; and Winona's Gym with its eight gates
            {
                var (fortree, fx, fy) = AreaSpot("fortree_city");
                At(fortree.Name, fx, fy, Direction.Up); game.EnterArea();
                Frames(40); Profile("fortree city in the treetops");
                var (road, rx, ry) = AreaSpot("route_119");
                At(road.Name, rx, ry, Direction.Up); game.EnterArea();
                Frames(40); Profile("route 119 in the rain");
                Room("FortreeCity_Gym", "fortree gym, the rotating gates");
            }
            game.Map = MapDatabase.Get("Sinnoh");

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

            // A wild battle at its menu, then the scripted battle's heaviest moments, each held still. Every battle
            // here has a team, foes and a generator of its own, made from fixed numbers and not from the game's
            // seeded chance: whatever puts more people on the maps moves that chance, and with it a Pokémon's gender,
            // HP and ability, so a battle read a line more or less on its way to the menu and its scene was another
            // picture from one commit to the next (plan 01 · M12)
            static Pokemon Made(string species, int level, int seed) => new(PokemonDatabase.Get(species)!, level, new Random(seed));
            var team = new Party();
            team.Add(Made("Turtwig", 5, 21));
            team.Add(Made("Chimchar", 7, 22));
            team.Add(Made("Piplup", 6, 23));
            BattleEngine Wild(int seed, BattleConditions conditions = null)
            {
                var battle = new BattleEngine(new BattleSetup
                {
                    PlayerParty = team, Inventory = inventory, Pokedex = pokedex, WildPokemon = new List<Pokemon> { Made("Shinx", 5, 24) },
                    Random = new Random(seed), Conditions = conditions
                });
                game.Battle = battle;
                game.State = GameState.Battle;
                return battle;
            }
            // (On the map of Sinnoh the stage depends on where the battle starts: StartBattle's place on Route 201)
            var (stage, stageX, stageY) = Harness.Place("Route201", -1, -1);
            game.Map = MapDatabase.Get(stage);
            if (stageX >= 0) game.Player.SetPosition(stageX, stageY, Direction.Up);
            game.BattleRenderer.SetArena(game.Map, stageX, stageY);
            var wild = Wild(31);
            ToMainMenu(wild);
            Profile("battle, at the menu");

            // The same in the rain (plan 25 · Y1): the weather's layer over the whole stage
            var rainy = Wild(32, new BattleConditions { Weather = BattleWeather.Rain });
            ToMainMenu(rainy);
            Skip(1.0);
            Profile("battle in rain, at the menu");

            // On the sea floor's own stage (plan 21 · H5, part b): its kelp, its rocks and its bubbles rising all round
            game.BattleRenderer.SetArena(BattleArena.Underwater);
            var deep = Wild(34);
            ToMainMenu(deep);
            Skip(1.0);
            Profile("battle on the sea floor, at the menu");
            game.BattleRenderer.SetArena(game.Map, stageX, stageY);

            // Both tired at the menu, their bars in the red (plan 25 · Y5): the slow breath and the droop are poses,
            // and should cost what the battle at the menu costs
            var weary = new Party();
            var spent = Made("Turtwig", 5, 21);
            spent.CurrentHP = Math.Max(1, spent.MaxHP / 6);
            weary.Add(spent);
            var foeSpent = Made("Shinx", 5, 24);
            foeSpent.CurrentHP = Math.Max(1, foeSpent.MaxHP / 6);
            var tired = new BattleEngine(new BattleSetup
            {
                PlayerParty = weary, Inventory = inventory, Pokedex = pokedex, WildPokemon = new List<Pokemon> { foeSpent }, Random = new Random(33)
            });
            game.Battle = tired;
            game.State = GameState.Battle;
            ToMainMenu(tired);
            Skip(1.0);
            Profile("battle, both tired, at the menu");

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
            var one = new Trainer { Name = "Ana", TrainerClass = "Youngster" };
            one.Party.Add(Made("Shinx", 6, 25));
            var two = new Trainer { Name = "Cal", TrainerClass = "Lass" };
            two.Party.Add(Made("Piplup", 6, 26));
            var d = new BattleEngine(new BattleSetup
            {
                PlayerParty = team, Inventory = inventory, Pokedex = pokedex, Format = BattleFormat.Double,
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

            // The same in a sandstorm with every hazard and screen on both sides (plan 25 · Y2): the most the field's
            // look ever draws over the most a battle already draws (Modes/ConditionsField.cs)
            EverythingOnTheField();
            Profile("double battle in a sandstorm, everything on the field");

            // A horde (plan 14 · B4): one of the player's against five wild Pokémon on one platform, at the menu
            var h = new BattleEngine(new BattleSetup
            {
                PlayerParty = team, Inventory = inventory, Pokedex = pokedex, Format = BattleFormat.Horde, Rules = Ruleset.Modern,
                WildPokemon = Enumerable.Range(0, 5).Select(i => Made("Zubat", 20, 31 + i)).ToList(), Random = new Random(9)
            });
            game.BattleRenderer.SetArena(game.Map);
            game.Battle = h;
            game.State = GameState.Battle;
            Skip(130 / 60.0);
            for (int guard = 0; guard < 8 && h.HUD.MenuState == BattleMenuState.Message; guard++) { h.ConfirmMessage(); Frames(1); Skip(50 / 60.0); }
            Skip(0.8);
            Profile("horde battle, five at the menu");

            // Giants (plan 25 · YD0, the baseline of Y-dynamax-punch): a battle at its menu with a giant on each side, and
            // three Max Moves at their landing, started by the animator itself as `later` starts them
            var giantTeam = new Party();
            giantTeam.Add(Made("Charizard", 100, 41));
            var giantB = new BattleEngine(new BattleSetup
            {
                PlayerParty = giantTeam, Inventory = inventory, Pokedex = pokedex, WildPokemon = new List<Pokemon> { Made("Snorlax", 100, 42) },
                Random = new Random(43), Rules = Ruleset.Modern
            });
            game.Map = MapDatabase.Get("Sinnoh");
            game.BattleRenderer.SetArena(BattleArena.Grass);
            game.Battle = giantB;
            game.State = GameState.Battle;
            ToMainMenu(giantB);
            giantB.Anim.Dynamax(BattleSide.Enemy, 0, false);
            giantB.Anim.Dynamax(BattleSide.Player, 0, false);
            Skip(BattleAnimator.DynamaxTime + 0.5);
            Profile("battle, a giant on each side, at the menu");
            foreach (string maxMove in new[] { "Max Flare", "Max Quake", "G-Max Wildfire" })
            {
                var data = MoveDatabase.Get(maxMove)!;
                Skip(BattleAnimator.ZMoveTime + 0.4);
                giantB.Anim.Cue(new EffectCue
                {
                    Kind = CueKind.Move, Move = data.Name, Type = data.Type, Category = data.Category, MaxMove = true,
                    FromSide = BattleSide.Player, ToSide = BattleSide.Enemy
                });
                Skip(BattleAnimator.ImpactTime + 0.03);
                Profile($"{maxMove}, landing, a giant on each side", 120, frozen: true);
            }

            // Plan 25's leftovers, after the rest so nothing before them moves: the boss box over a Mega Max team's ace at
            // the menu, and a Mega Dynamax beside its partner in a double at the menu
            var bossTrainer = new Trainer { Id = "champion_cynthia_megamax", Name = "Cynthia", TrainerClass = "Champion", PrizeMoney = 0 };
            var bossAce = Made("Garchomp", 100, 44);
            bossAce.ChangeForm("Garchomp-Mega-Mmax");
            bossTrainer.Party.Add(bossAce);
            var bossTeam = new Party();
            bossTeam.Add(Made("Charizard", 100, 45));
            var bossB = new BattleEngine(new BattleSetup
            {
                PlayerParty = bossTeam, Inventory = inventory, Pokedex = pokedex, Trainers = new List<Trainer> { bossTrainer },
                Random = new Random(46), Rules = Ruleset.Modern
            });
            game.BattleRenderer.SetArena(BattleArena.Grass);
            game.Battle = bossB;
            game.State = GameState.Battle;
            ToMainMenu(bossB);
            bossB.Anim.Dynamax(BattleSide.Enemy, 0, false, megaMax: true);
            Skip(BattleAnimator.DynamaxTime + 0.5);
            Console.WriteLine($"  the boss box: {bossB.BossShare(bossB.Anim.Enemy):0.00}");
            Profile("battle, the boss box at the menu");

            var pairTeam = new Party();
            var pairAce = Made("Garchomp", 100, 47);
            pairAce.ChangeForm("Garchomp-Mega-Mmax");
            pairTeam.Add(pairAce);
            pairTeam.Add(Made("Blissey", 100, 48));
            var pairB = new BattleEngine(new BattleSetup
            {
                PlayerParty = pairTeam, Inventory = inventory, Pokedex = pokedex, Format = BattleFormat.Double,
                WildPokemon = new List<Pokemon> { Made("Snorlax", 100, 49), Made("Snorlax", 100, 50) },
                Random = new Random(51), Rules = Ruleset.Modern
            });
            game.BattleRenderer.SetArena(BattleArena.Grass);
            game.Battle = pairB;
            game.State = GameState.Battle;
            ToMainMenu(pairB);
            pairB.Anim.Dynamax(BattleSide.Player, 0, false, megaMax: true);
            Skip(BattleAnimator.DynamaxTime + 0.5);
            Profile("double battle, a Mega Dynamax beside its partner, at the menu");
            game.State = GameState.Overworld;
        }
        engine.Settings.Quality = GraphicsQuality.High;
        engine.ApplySettings(window: false);
        // What tools/dev/ab.sh hands to `ab`
        profileRun.Save(Path.Combine(outDir, "profile.json"));
        Console.WriteLine("wrote profile.json");
    }
}
