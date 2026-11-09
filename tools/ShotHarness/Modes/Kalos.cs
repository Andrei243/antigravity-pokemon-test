partial class Harness
{
    /// <summary>
    /// Kalos drawn by hand (plan 23 · Z1; not part of <c>all</c>): a new game begun there (<c>--region Kalos</c>) in the
    /// player's room, Vaniville Town, Route 1 and Aquacorde Town by day and after dark (<c>wk01</c> to <c>wk08</c>), the
    /// rooms (<c>wk10</c> to <c>wk15</c>), a skater on Vaniville's lane (<c>wk20</c>, <c>wk21</c>), and the first chapter
    /// played by its own scripts in a new game of its own (<c>wks01</c> to <c>wks12</c>: Mom on the first morning, the
    /// neighbours at the door, the friends at the café, the choice of three, the Pokédex, Shauna's battle, the letter,
    /// Mom and the roller skates), and on through the second part (<c>wk30</c> to <c>wk48</c>: Route 2, Santalune Forest,
    /// Route 3, Santalune City and its rooms; <c>wks13</c> to <c>wks24</c>: the neighbour's lesson, Shauna in the forest and
    /// its Pikachu, Tierno's battle, the neighbour's, the friends on the square, Viola and the Bug Badge), and plan 23 · Z2's
    /// first part (<c>wk50</c> to <c>wk67</c>: Route 4's garden and Lumiose City's gate, avenue, tower, boulevard and plazas;
    /// <c>wk70</c> to <c>wk73</c>: its rooms; <c>wks25</c> to <c>wks29</c>: the assistants at the gate, the professor, his
    /// battle, Kanto's first three and Lysandre's café). It wins every battle a scene starts and prints what each scene left behind.
    /// </summary>
    public void KalosMode()
    {
        // KALOS_FROM=central: plan 23 · Z2's second part alone (wk74 on, wks30 on)
        if (Environment.GetEnvironmentVariable("KALOS_FROM") == "central") { KalosCentral(); return; }
        if (Environment.GetEnvironmentVariable("KALOS_FROM") == "coast") { KalosCoast(); return; }
        // KALOS_FROM=league: plan 23 · Z4 alone (wk200 on, wks60 on)
        if (Environment.GetEnvironmentVariable("KALOS_FROM") == "league") { KalosLeague(); return; }
        // KALOS_FROM=loose: plan 23 · Z4's loose ends alone (wk300 on, wks100 on)
        if (Environment.GetEnvironmentVariable("KALOS_FROM") == "loose") { KalosLooseEnds(); return; }
        // KALOS_FROM=postgame: plan 23 · Z4b, Kalos after its Hall of Fame, alone (wk400 on, wks200 on)
        if (Environment.GetEnvironmentVariable("KALOS_FROM") == "postgame") { KalosPostGame(); return; }
        // KALOS_FROM=lastplaces: Kalos's last places alone (wk500 on, wks300 on), not part of the whole mode
        if (Environment.GetEnvironmentVariable("KALOS_FROM") == "lastplaces") { KalosLastPlaces(); return; }
        // KALOS_FROM=lastspecies: Kalos's last species and Lumiose's restaurants' shots alone (fc50 on, wk600 on, wks400 on)
        if (Environment.GetEnvironmentVariable("KALOS_FROM") == "lastspecies") { KalosLastSpecies(); return; }
        // KALOS_FROM=finished: what finished Kalos alone (wk700 on, wks500 on): the Friend Safari, Parfum Palace, the
        // bicycle shop and Malva's second round, not part of the whole mode
        if (Environment.GetEnvironmentVariable("KALOS_FROM") == "finished") { KalosFinished(); return; }
        // KALOS_FROM=garden: after Kalos finished, the palace garden, its fireworks, the castle tour and the designer (wk720 on, wks540 on), not part of the whole mode
        if (Environment.GetEnvironmentVariable("KALOS_FROM") == "garden") { KalosGarden(); return; }
        engine.NewGameRegion = "Kalos";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        Frames(30);
        Console.WriteLine($"a new game in Kalos begins on {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY}), "
            + $"with {(game.Party.Members.Count == 0 ? "nobody" : string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} {p.Level}")))}");
        Shot("wk00_new_game");

        (string Day, string Night, string Map, int X, int Y, Direction Facing)[] places =
        {
            ("wk01_vaniville_day", "wk02_vaniville_night", "Kalos", 255, 716, Direction.Up),
            ("wk03_vaniville_homes_day", "wk04_vaniville_homes_night", "Kalos", 246, 723, Direction.Up),
            ("wk05_route1_day", "wk06_route1_night", "Kalos", 255, 692, Direction.Up),
            ("wk07_aquacorde_day", "wk08_aquacorde_night", "Kalos", 255, 664, Direction.Up),
            ("wk09_aquacorde_bridge_day", "wk09b_aquacorde_bridge_night", "Kalos", 255, 656, Direction.Up),
            // Plan 23 · Z1, second part: Route 2, Santalune Forest, Route 3 and Santalune City
            ("wk30_route2_day", "wk31_route2_night", "Kalos", 255, 624, Direction.Up),
            ("wk32_forest_way_in_day", "wk33_forest_way_in_night", "Kalos", 255, 596, Direction.Up),
            ("wk34_forest_grove_day", "wk35_forest_grove_night", "Kalos", 260, 586, Direction.Up),
            ("wk36_route3_day", "wk37_route3_night", "Kalos", 290, 531, Direction.Right),
            ("wk38_santalune_square_day", "wk39_santalune_square_night", "Kalos", 340, 524, Direction.Up),
            ("wk40_santalune_gym_day", "wk41_santalune_gym_night", "Kalos", 341, 513, Direction.Up),
            ("wk42_santalune_center_day", "wk43_santalune_center_night", "Kalos", 325, 524, Direction.Up),
            ("wk44_santalune_south_day", "wk45_santalune_south_night", "Kalos", 340, 540, Direction.Up),
            // Plan 23 · Z2: Route 4's garden and Lumiose City
            ("wk50_route4_day", "wk51_route4_night", "Kalos", 358, 482, Direction.Up),
            ("wk52_route4_fountain_day", "wk53_route4_fountain_night", "Kalos", 358, 443, Direction.Up),
            ("wk54_lumiose_gate_day", "wk55_lumiose_gate_night", "Kalos", 359, 413, Direction.Up),
            ("wk56_lumiose_avenue_day", "wk57_lumiose_avenue_night", "Kalos", 360, 392, Direction.Up),
            ("wk58_lumiose_tower_day", "wk59_lumiose_tower_night", "Kalos", 359, 371, Direction.Up),
            ("wk60_lumiose_boulevard_day", "wk61_lumiose_boulevard_night", "Kalos", 346, 352, Direction.Up),
            ("wk62_lumiose_vert_day", "wk63_lumiose_vert_night", "Kalos", 314, 366, Direction.Up),
            ("wk64_lumiose_magenta_day", "wk65_lumiose_magenta_night", "Kalos", 390, 334, Direction.Up),
            ("wk66_lumiose_jaune_day", "wk67_lumiose_jaune_night", "Kalos", 360, 322, Direction.Up),
        };
        foreach (var (day, _, map, x, y, facing) in places)
        {
            At(map, x, y, facing); Frames(2); game.LocationSign.Hide(); Shot(day);
            ShotCrop(day + "_native", 640, 300, 640, 360, 2);
        }
        (string Name, string Map, int X, int Y, Direction Facing)[] rooms =
        {
            ("wk10_bedroom", "VanivilleTown_PlayersHouse_2F", 4, 6, Direction.Down),
            ("wk11_downstairs", "VanivilleTown_PlayersHouse_1F", 6, 9, Direction.Up),
            ("wk12_neighbours_house", "VanivilleTown_NeighboursHouse", 4, 8, Direction.Up),
            ("wk13_vaniville_house", "VanivilleTown_NorthwestHouse", 4, 8, Direction.Up),
            ("wk14_aquacorde_house", "AquacordeTown_WestHouse", 4, 8, Direction.Up),
            ("wk15_aquacorde_house_2", "AquacordeTown_SoutheastHouse", 4, 8, Direction.Up),
            ("wk46_santalune_center_inside", "SantaluneCity_PokemonCenter_1F", 8, 9, Direction.Up),
            ("wk47_santalune_gym_inside", "SantaluneCity_Gym", 6, 13, Direction.Up),
            ("wk48_santalune_house", "SantaluneCity_NorthwestHouse", 4, 8, Direction.Up),
            ("wk70_lumiose_center_inside", "LumioseCity_PokemonCenter_1F", 8, 9, Direction.Up),
            ("wk71_sycamore_lab", "LumioseCity_SycamoreLab", 7, 9, Direction.Up),
            ("wk72_lysandre_cafe", "LumioseCity_LysandreCafe", 6, 8, Direction.Up),
            ("wk73_lumiose_cafe", "LumioseCity_Cafe", 4, 8, Direction.Up),
        };
        foreach (var (name, map, x, y, facing) in rooms) { At(map, x, y, facing); Frames(2); Shot(name); }

        // Plan 23 · Z2: Lumiose from further off, the camera's lens zoomed out, so the tower and the blocks round it are seen whole
        var rig = game.World.Camera;
        foreach (var (name, x, y) in new[] { ("wk68_lumiose_tower_wide", 359, 366), ("wk69_lumiose_blocks_wide", 333, 350) })
        {
            At("Kalos", x, y, Direction.Up); Frames(2);
            rig.ZoomTo(0.45f, 0f);
            Frames(30); game.LocationSign.Hide(); Shot(name);
            rig.Reset();
            Frames(2);
        }

        // A skater on Vaniville's south lane: the skates put on with the run button, mid-step and gliding to a stop
        game.Story.Set(PokemonPlatinumEngine.Overworld.SkateRules.Flag);
        At("Kalos", 246, 723, Direction.Right);
        engine.Steering = (Direction.Right, true);
        Frames(6);
        Console.WriteLine($"  skating: {game.Player.Mode}, moving {game.Player.IsMoving}");
        Shot("wk20_skating");
        Frames(20);
        engine.Steering = null;
        Frames(4);
        Shot("wk21_skating_glide");
        Frames(40);
        Console.WriteLine($"  after the glide: {game.Player.Mode} at ({game.Player.GridX}, {game.Player.GridY})");
        game.Story.Unset(PokemonPlatinumEngine.Overworld.SkateRules.Flag);

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        foreach (var (_, night, map, x, y, facing) in places)
        {
            At(map, x, y, facing); Frames(2); game.LocationSign.Hide(); Shot(night);
        }
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        KalosScenes();
        KalosCentral();
        KalosCoast();
        KalosLeague();
        KalosLooseEnds();
        KalosPostGame();
    }

    private void KalosScenes()
    {
        var story = game.Story;
        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }
        void ReadTo(string? text, int most = 2400)
        {
            int idle = 0;
            for (int i = 0; i < most; i++)
            {
                if (text != null && Box().IsActive && Translated.Saying(Box().CurrentLine, text)) { Whole(); Console.WriteLine($"  said: {text}"); return; }
                idle = !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld ? idle + 1 : 0;
                if (text != null && idle > 90) break;
                if (text == null && idle > 30) return;
                if (State() == GameState.Battle) { Win(); continue; }
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            Console.WriteLine($"  !! never said: {text ?? "the script's end"}");
        }
        void Win()
        {
            var fight = game.Battle;
            for (int guard = 0; guard < 400 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    foreach (var foe in fight.EnemyParty?.Members ?? new List<Pokemon>()) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    if (fight.EnemyPokemon is { } met) met.CurrentHP = Math.Min(met.CurrentHP, 1);
                    var lead = fight.PlayerPokemon;
                    if (lead != null) lead.CurrentHP = lead.MaxHP;
                    fight.SelectMainMenuOption(0);
                    int hurts = lead == null ? 0 : Math.Max(0, lead.Moves.FindIndex(m => m.Data.Category != MoveCategory.Status));
                    fight.SelectMove(hurts);
                    if (fight.HUD.MenuState == BattleMenuState.SelectTarget) fight.SelectTarget(0);
                }
                else Confirm(fight);
                Skip(0.5);
            }
            Until(() => FieldAgain(), "the field again", 1500);
            Frames(10);
        }
        void StepOnto(string map, int x, int y, Direction way)
        {
            var (dx, dy) = way switch { Direction.Up => (0, -1), Direction.Down => (0, 1), Direction.Left => (-1, 0), _ => (1, 0) };
            At(map, x - dx, y - dy, way);
            game.EnterArea();
            Frames(2);
            engine.Steering = (way, false);
            Until(() => engine.ScriptRunning, $"the scene at {x},{y}", 120);
            engine.Steering = null;
        }
        void ComeThrough(string map, int x, int y, Direction facing)
        {
            At(map, x, y, facing);
            game.ArriveOnMap();
            Frames(2);
        }
        void Talk(string map, string name, int dx = 0, int dy = 1)
        {
            var m = MapDatabase.Get(map);
            var npc = m.NPCs.First(n => n.Name == name);
            var facing = (dx, dy) switch { (0, 1) => Direction.Up, (0, -1) => Direction.Down, (1, 0) => Direction.Left, _ => Direction.Right };
            At(map, npc.GridX + dx, npc.GridY + dy, facing);
            game.Interact();
        }
        void Left(string what) =>
            Console.WriteLine($"  {what}: story {story.Var("VAR_KALOS_STORY")}, team {string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} {p.Level}"))}, "
                + $"Pokédex {story.Has(StoryState.PokedexFlag)}, shoes {story.Has(StoryState.RunningShoesFlag)}, skates {story.Has(PokemonPlatinumEngine.Overworld.SkateRules.Flag)}");

        // A new game of Kalos, in the player's room: down the stairs to Mom
        engine.NewGameRegion = "Kalos";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        Frames(2);
        ComeThrough("VanivilleTown_PlayersHouse_1F", 11, 3, Direction.Left);
        ReadTo("Did you sleep well"); Frames(10); Shot("wks01_mom_first_morning");
        ReadTo(null);
        Left("Mom's first morning");

        // ---- out of the door: the neighbour and Shauna
        ComeThrough("Kalos", 242, 722, Direction.Down);
        ReadTo("Are you the one who just moved in?"); Frames(10); Shot("wks02_the_neighbours_arrive");
        ReadTo("We came to fetch you"); Frames(10); Shot("wks03_the_neighbours_at_the_door");
        ReadTo(null);
        Left("the neighbours met");

        // ---- the café in Aquacorde Town
        StepOnto("Kalos", 255, 661, Direction.Up);
        ReadTo("Everybody, this is"); Frames(10); Shot("wks04_the_friends_at_the_cafe");
        ReadTo("I-I'm Trevor"); Frames(10); Shot("wks05_trevor");
        for (int i = 0; i < 1500 && State() != GameState.StarterSelect; i++)
        {
            if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
            else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
            else Frames(1);
        }
        Until(() => State() == GameState.StarterSelect, "the choice of three", 10);
        Frames(40); Shot("wks06_the_choice_of_three");
        var choice = game.StarterSelect;
        choice.SelectedIndex = 0;
        choice.Confirm();
        choice.Move(1);
        Frames(20);
        InputManager.Feed(ActionFrame.Press(GameAction.Confirm)); Frames(1); InputManager.Feed(null);
        ReadTo("They're Pokédexes"); Frames(10); Shot("wks07_the_pokedex");
        for (int i = 0; i < 1500 && State() != GameState.Battle; i++)
        {
            if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
            else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
            else Frames(1);
        }
        if (State() == GameState.Battle) { Skip(1.0); ToMainMenu(game.Battle); Shot("wks08_shaunas_battle"); Win(); }
        else Console.WriteLine("  !! never happened: Shauna's battle");
        ReadTo("The professor sent a letter"); Frames(10); Shot("wks09_the_letter");
        ReadTo(null);
        Left("the café");

        // ---- home: Mom reads the letter and hands over the skates
        Talk("VanivilleTown_PlayersHouse_1F", "Mom");
        ReadTo("these are for you"); Frames(10); Shot("wks10_mom_and_the_letter");
        ReadTo("Everyone in Kalos skates"); Frames(10); Shot("wks11_the_skates");
        ReadTo(null);
        Left("Mom and the skates");

        // ---- out on skates: Route 1 north at a skater's pace
        At("Kalos", 255, 699, Direction.Up);
        engine.Steering = (Direction.Up, true);
        Frames(30);
        Shot("wks12_skating_route1");
        engine.Steering = null;
        Frames(30);
        Console.WriteLine($"  on Route 1: {game.Player.Mode} at ({game.Player.GridX}, {game.Player.GridY})");

        // ---- Route 2 (plan 23 · Z1, second part): the neighbour's lesson and the Poké Balls
        StepOnto("Kalos", 255, 636, Direction.Up);
        ReadTo("Wild Pokémon live in there"); Frames(10); Shot("wks13_the_neighbours_lesson");
        ReadTo("take some Poké Balls"); Frames(10); Shot("wks14_the_poke_balls");
        ReadTo(null);
        Left("the lesson on Route 2");

        // ---- Santalune Forest: Shauna at the way in, and the grove's Pikachu
        StepOnto("Kalos", 255, 600, Direction.Up);
        ReadTo("Isn't it amazing in here"); Frames(10); Shot("wks15_shauna_in_the_forest");
        ReadTo(null);
        At("Kalos", 260, 584, Direction.Up); Frames(2);
        game.Interact();
        ReadTo("Pika pika"); Frames(10); Shot("wks16_the_forests_pikachu");
        ReadTo(null);
        Left("the forest");

        // ---- Route 3: Tierno and Trevor where the way up meets the road
        StepOnto("Kalos", 267, 531, Direction.Up);
        ReadTo("Wanna see them"); Frames(10); Shot("wks17_tierno_on_route3");
        ReadTo("Santalune City is just east"); Frames(10); Shot("wks18_after_tiernos_battle");
        ReadTo(null);
        Left("Route 3");

        // ---- Santalune City: the neighbour's battle, the friends on the square
        StepOnto("Kalos", 319, 527, Direction.Right);
        ReadTo("I've been waiting"); Frames(10); Shot("wks19_the_neighbour_in_santalune");
        ReadTo("The Gym here is north"); Frames(10); Shot("wks20_after_the_neighbours_battle");
        ReadTo(null);
        Left("Santalune City");
        At("Kalos", 340, 524, Direction.Up); Frames(4); Shot("wks21_friends_on_the_square");

        // ---- the Gym: Viola, her battle and the Bug Badge
        At("SantaluneCity_Gym", 6, 5, Direction.Up); Frames(2);
        game.Interact();
        ReadTo("I'm Viola"); Frames(10); Shot("wks22_viola");
        for (int i = 0; i < 1500 && State() != GameState.Battle; i++)
        {
            if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
            else Frames(1);
        }
        if (State() == GameState.Battle) { Skip(1.0); ToMainMenu(game.Battle); Shot("wks23_violas_battle"); Win(); }
        else Console.WriteLine("  !! never happened: Viola's battle");
        ReadTo("The Bug Badge, proof"); Frames(10); Shot("wks24_the_bug_badge");
        ReadTo(null);
        Left("the Gym");
        Console.WriteLine($"  badges of Kalos: {game.Story.BadgeCount(PokemonPlatinumEngine.Data.RegionDatabase.Kalos)}");

        // ---- plan 23 · Z2: up Route 4 to Lumiose City's south gate, where the professor's assistants wait
        StepOnto("Kalos", 359, 412, Direction.Up);
        ReadTo("watching the gate all morning"); Frames(10); Shot("wks25_the_assistants");
        ReadTo(null);
        Left("Lumiose's south gate");

        // ---- the laboratory: the professor, his battle and Kanto's first three
        At("LumioseCity_SycamoreLab", 7, 5, Direction.Up); Frames(2);
        game.Interact();
        ReadTo("At last! I'm Sycamore"); Frames(10); Shot("wks26_professor_sycamore");
        for (int i = 0; i < 1500 && State() != GameState.Battle; i++)
        {
            if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
            else Frames(1);
        }
        if (State() == GameState.Battle) { Skip(1.0); ToMainMenu(game.Battle); Shot("wks27_sycamores_battle"); Win(); }
        else Console.WriteLine("  !! never happened: the professor's battle");
        for (int i = 0; i < 1500 && !engine.Choice.IsOpen; i++)
        {
            if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
            else Frames(1);
        }
        Frames(10); Shot("wks28_kanto_three");
        ReadTo(null);
        Left("the laboratory");

        // ---- Lysandre's café in the north of the city
        ComeThrough("LumioseCity_LysandreCafe", 6, 8, Direction.Up);
        ReadTo("My name is Lysandre"); Frames(10); Shot("wks29_lysandre");
        ReadTo(null);
        Left("the café");
        Console.WriteLine($"  what next: {PokemonPlatinumEngine.Story.Objectives.Current(story, PokemonPlatinumEngine.Data.RegionDatabase.Kalos)?.Name}");
        game.DeleteSave();
    }
}
