partial class Harness
{
    /// <summary>
    /// Plan 23 · Z4's shots (the last of the <c>kalos</c> mode, and the whole of it with <c>KALOS_FROM=league</c>): Route 13
    /// to the League by day and some after dark (<c>wk200</c> on), the new rooms and Victory Road (<c>wk240</c> on), and the
    /// story from Ramos to the Hall of Fame in a new game of its own (<c>wks60</c> on: Ramos, Aliana at the Power Plant, the
    /// Prism Tower and Clemont, the neighbour on Route 14, Valerie, Olympia and Lysandre's broadcast, the laboratory under the
    /// café, Team Flare's base and Xerneas, Wulfric, Victory Road's guard, the Elite Four, Diantha and the Hall of Fame). It
    /// wins every battle a scene starts and prints what each scene left behind.
    /// </summary>
    public void KalosLeague()
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
        void Win()
        {
            var fight = game.Battle;
            for (int guard = 0; guard < 600 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    foreach (var foe in fight.EnemyParty?.Members ?? new List<Pokemon>()) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    if (fight.EnemyPokemon is { } met) met.CurrentHP = Math.Min(met.CurrentHP, 1);
                    var lead = fight.PlayerPokemon;
                    if (lead != null) lead.CurrentHP = lead.MaxHP;
                    fight.SelectMainMenuOption(0);
                    if (lead != null) foreach (var m in lead.Moves) m.CurrentPP = m.MaxPP;
                    int hurts = lead == null ? 0 : lead.Moves.Select((m, i) => (m, i)).Where(p => p.m.Data.Category != MoveCategory.Status && p.m.Data.Type != PokemonType.Ghost)
                        .OrderByDescending(p => p.m.Data.Power).Select(p => p.i).DefaultIfEmpty(0).First();
                    fight.SelectMove(hurts);
                    if (fight.HUD.MenuState == BattleMenuState.SelectTarget) fight.SelectTarget(0);
                }
                else Confirm(fight);
                Skip(0.5);
            }
            Until(() => FieldAgain(), "the field again", 1500);
            Frames(10);
        }
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
        void ToBattle()
        {
            for (int i = 0; i < 1500 && State() != GameState.Battle; i++)
            {
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
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
        void Battle(string scene, string shot)
        {
            ToBattle();
            if (State() == GameState.Battle) { Skip(1.0); ToMainMenu(game.Battle); Shot(shot); Win(); }
            else Console.WriteLine($"  !! never happened: {scene}");
        }
        void Talk(string map, int x, int y, Direction facing) { At(map, x, y, facing); Frames(2); game.Interact(); }
        void Left(string what) =>
            Console.WriteLine($"  {what}: story {story.Var("VAR_KALOS_STORY")}, roads {story.Var("VAR_KALOS_LUMIOSE_ROADS")}, map {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY}), "
                + $"badges {story.BadgeCount(PokemonPlatinumEngine.Data.RegionDatabase.Kalos)}, hall of fame {story.Has("KalosHallOfFame")}, next {PokemonPlatinumEngine.Story.Objectives.Current(story, PokemonPlatinumEngine.Data.RegionDatabase.Kalos)?.Name}");

        // A new game of Kalos brought at once to where the Tower of Mastery leaves it: three Badges and the Key Stone
        engine.NewGameRegion = "Kalos";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        story.ChooseStarter("Froakie");
        foreach (var (species, level) in new[] { ("Greninja", 60), ("Talonflame", 58), ("Venusaur", 58), ("Lucario", 58) })
            game.Party.Add(new Pokemon(PokemonPlatinumEngine.Data.PokemonDatabase.Get(species)!, level));
        story.Set(StoryState.PokedexFlag);
        story.Set(StoryState.RunningShoesFlag);
        foreach (string badge in new[] { "Bug", "Cliff", "Rumble" }) story.GiveBadge(PokemonPlatinumEngine.Data.RegionDatabase.Kalos, badge);
        story.SetVar("VAR_KALOS_STORY", 22);
        story.SetVar("VAR_KALOS_LUMIOSE_ROADS", 1);
        Frames(2);
        Left("brought past the Tower of Mastery");

        (string Day, string? Night, int X, int Y, Direction Facing)[] places =
        {
            ("wk200_route13_north", null, 276, 384, Direction.Down),
            ("wk201_route13_plant", "wk202_route13_plant_night", 288, 398, Direction.Up),
            ("wk203_route13_badlands", null, 259, 430, Direction.Down),
            ("wk204_coumarine_north", null, 259, 466, Direction.Up),
            ("wk205_coumarine_gym", null, 268, 476, Direction.Up),
            ("wk206_prism_tower_foot", "wk207_prism_tower_foot_night", 359, 364, Direction.Up),
            ("wk208_lumiose_north_gate", null, 359, 304, Direction.Up),
            ("wk209_route14_marsh", "wk210_route14_marsh_night", 359, 270, Direction.Up),
            ("wk211_route14_north", null, 359, 244, Direction.Up),
            ("wk212_laverre_tree", "wk213_laverre_tree_night", 360, 200, Direction.Up),
            ("wk214_laverre_avenue", null, 345, 206, Direction.Up),
            ("wk215_route15_bridge", "wk216_route15_bridge_night", 440, 206, Direction.Right),
            ("wk217_dendemille", "wk218_dendemille_night", 500, 206, Direction.Up),
            ("wk219_route17_snow", "wk220_route17_snow_night", 560, 206, Direction.Right),
            ("wk221_anistar_square", "wk222_anistar_square_night", 640, 210, Direction.Up),
            ("wk223_anistar_sundial", null, 650, 222, Direction.Up),
            ("wk224_route18_valley", null, 633, 280, Direction.Down),
            ("wk225_couriway", "wk226_couriway_night", 634, 358, Direction.Up),
            ("wk227_route19", null, 687, 340, Direction.Up),
            ("wk228_snowbelle", "wk229_snowbelle_night", 687, 262, Direction.Up),
            ("wk230_route20_woods", null, 687, 220, Direction.Up),
            ("wk231_pokemon_village", null, 687, 130, Direction.Up),
            ("wk232_route21", null, 630, 128, Direction.Left),
            ("wk233_victory_road_mouth", null, 605, 123, Direction.Up),
            ("wk234_league_terrace", "wk235_league_terrace_night", 608, 72, Direction.Up),
            ("wk236_geosenge_stones", null, 108, 223, Direction.Up),
        };
        foreach (var (day, _, x, y, facing) in places)
        {
            At("Kalos", x, y, facing); Frames(2); game.LocationSign.Hide(); Shot(day);
        }

        (string Name, string Map, int X, int Y, Direction Facing)[] rooms =
        {
            ("wk240_coumarine_gym_inside", "CoumarineCity_Gym", 6, 13, Direction.Up),
            ("wk241_power_plant", "KalosPowerPlant", 8, 11, Direction.Up),
            ("wk242_prism_tower_gym", "LumioseCity_PrismTowerGym", 6, 13, Direction.Up),
            ("wk243_laverre_gym", "LaverreCity_Gym", 6, 13, Direction.Up),
            ("wk244_laverre_center", "LaverreCity_PokemonCenter_1F", 8, 9, Direction.Up),
            ("wk245_laverre_house", "LaverreCity_House", 4, 7, Direction.Up),
            ("wk246_dendemille_center", "DendemilleTown_PokemonCenter_1F", 8, 9, Direction.Up),
            ("wk247_anistar_gym", "AnistarCity_Gym", 6, 13, Direction.Up),
            ("wk248_anistar_center", "AnistarCity_PokemonCenter_1F", 8, 9, Direction.Up),
            ("wk249_lysandre_labs", "LumioseCity_LysandreLabs", 8, 13, Direction.Up),
            ("wk250_flare_hq", "GeosengeTown_FlareHQ", 10, 18, Direction.Up),
            ("wk251_flare_hq_chamber", "GeosengeTown_FlareHQ", 10, 8, Direction.Up),
            ("wk252_couriway_center", "CouriwayTown_PokemonCenter_1F", 8, 9, Direction.Up),
            ("wk253_snowbelle_gym", "SnowbelleCity_Gym", 6, 13, Direction.Up),
            ("wk254_snowbelle_center", "SnowbelleCity_PokemonCenter_1F", 8, 9, Direction.Up),
            ("wk255_victory_road_south", "KalosVictoryRoad", 28, 54, Direction.Up),
            ("wk256_victory_road_hall", "KalosVictoryRoad", 30, 30, Direction.Up),
            ("wk257_victory_road_north", "KalosVictoryRoad", 34, 10, Direction.Up),
            ("wk258_league_hall", "KalosLeague_Hall", 10, 12, Direction.Up),
            ("wk259_malva_room", "KalosLeague_MalvaRoom", 6, 9, Direction.Up),
            ("wk260_siebold_room", "KalosLeague_SieboldRoom", 6, 9, Direction.Up),
            ("wk261_wikstrom_room", "KalosLeague_WikstromRoom", 6, 9, Direction.Up),
            ("wk262_drasna_room", "KalosLeague_DrasnaRoom", 6, 9, Direction.Up),
            ("wk263_champion_room", "KalosLeague_ChampionRoom", 7, 11, Direction.Up),
            ("wk264_hall_of_fame", "KalosLeague_HallOfFame", 6, 9, Direction.Up),
        };
        foreach (var (name, map, x, y, facing) in rooms) { At(map, x, y, facing); Frames(2); Shot(name); }

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        foreach (var (_, night, x, y, facing) in places)
        {
            if (night == null) continue;
            At("Kalos", x, y, facing); Frames(2); game.LocationSign.Hide(); Shot(night);
        }
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // ---- Ramos
        Talk("CoumarineCity_Gym", 6, 5, Direction.Up);
        ReadTo("I'm Ramos"); Frames(10); Shot("wks60_ramos");
        Battle("Ramos's battle", "wks61_ramos_battle");
        ReadTo("Here's the Plant Badge"); Frames(10); Shot("wks62_plant_badge");
        ReadTo(null);
        Left("Ramos");

        // ---- the Power Plant
        Talk("KalosPowerPlant", 8, 5, Direction.Up);
        ReadTo("Team Flare is borrowing this plant's power"); Frames(10); Shot("wks63_aliana");
        Battle("Aliana's battle", "wks64_aliana_battle");
        ReadTo("You chased them off"); Frames(10); Shot("wks65_the_power_back");
        ReadTo(null);
        Left("the Power Plant");

        // ---- the Prism Tower and Clemont
        At("Kalos", 359, 362, Direction.Up); Frames(2); game.Interact();
        ReadTo("carries"); Frames(10); Shot("wks66_the_tower_lift");
        ReadTo(null);
        Frames(30);
        Talk("LumioseCity_PrismTowerGym", 6, 5, Direction.Up);
        ReadTo("I'm Clemont"); Frames(10); Shot("wks67_clemont");
        Battle("Clemont's battle", "wks68_clemont_battle");
        ReadTo("the Voltage Badge"); Frames(10); Shot("wks69_voltage_badge");
        ReadTo(null);
        Left("Clemont");

        // ---- the neighbour on Route 14
        StepOnto("Kalos", 359, 276, Direction.Up);
        ReadTo("You got the power running again"); Frames(10); Shot("wks70_neighbour_route14");
        Battle("the neighbour's battle", "wks71_neighbour_battle");
        ReadTo(null);
        Left("Route 14");

        // ---- Valerie
        Talk("LaverreCity_Gym", 6, 5, Direction.Up);
        ReadTo("I am Valerie"); Frames(10); Shot("wks72_valerie");
        Battle("Valerie's battle", "wks73_valerie_battle");
        ReadTo(null);
        Left("Valerie");

        // ---- Olympia and Lysandre's broadcast
        Talk("AnistarCity_Gym", 6, 5, Direction.Up);
        ReadTo("I am Olympia"); Frames(10); Shot("wks74_olympia");
        Battle("Olympia's battle", "wks75_olympia_battle");
        ReadTo("People of Kalos"); Frames(10); Shot("wks76_lysandres_broadcast");
        ReadTo(null);
        Left("Olympia");

        // ---- the laboratory under the café
        Talk("LumioseCity_LysandreCafe", 8, 5, Direction.Up);
        ReadTo("He is waiting for you below"); Frames(10); Shot("wks77_the_waiter");
        ReadTo(null);
        Frames(30);
        Shot("wks78_the_laboratory");
        Talk("LumioseCity_LysandreLabs", 8, 5, Direction.Up);
        ReadTo("Three thousand years ago"); Frames(10); Shot("wks79_lysandre_in_his_laboratory");
        Battle("Lysandre's battle", "wks80_lysandre_battle");
        ReadTo(null);
        Left("the laboratory");

        // ---- Team Flare's base and Xerneas
        StepOnto("Kalos", 108, 219, Direction.Up);
        ReadTo("Steps lead down"); Frames(10); Shot("wks81_the_hidden_door");
        ReadTo(null);
        Frames(30);
        Shot("wks82_team_flares_base");
        Talk("GeosengeTown_FlareHQ", 10, 7, Direction.Up);
        ReadTo("the Pokémon of life"); Frames(10); Shot("wks83_lysandre_at_the_weapon");
        Battle("Lysandre's second battle", "wks84_lysandre_battle_2");
        ReadTo(null);
        Talk("GeosengeTown_FlareHQ", 10, 4, Direction.Up);
        ReadTo("horns glow"); Frames(10); Shot("wks85_xerneas_wakes");
        ToBattle();
        if (State() == GameState.Battle) { Skip(1.0); ToMainMenu(game.Battle); Shot("wks86_xerneas_battle"); Win(); }
        ReadTo(null);
        Frames(30);
        Left("Team Flare's base");

        // ---- Wulfric
        Talk("SnowbelleCity_Gym", 6, 5, Direction.Up);
        ReadTo("I'm Wulfric"); Frames(10); Shot("wks87_wulfric");
        Battle("Wulfric's battle", "wks88_wulfric_battle");
        ReadTo(null);
        Left("Wulfric");

        // ---- Victory Road's guard
        StepOnto("Kalos", 605, 121, Direction.Up);
        ReadTo("Eight Badges"); Frames(10); Shot("wks89_victory_road_guard");
        ReadTo(null);

        // ---- the Elite Four, Diantha and the Hall of Fame
        foreach (var (room, who, first, shot) in new[]
                 {
                     ("KalosLeague_MalvaRoom", "Malva", "I'm Malva", "wks90_malva"), ("KalosLeague_SieboldRoom", "Siebold", "I am Siebold", "wks91_siebold"),
                     ("KalosLeague_WikstromRoom", "Wikstrom", "I am Wikstrom", "wks92_wikstrom"), ("KalosLeague_DrasnaRoom", "Drasna", "I'm Drasna", "wks93_drasna")
                 })
        {
            Talk(room, 6, 5, Direction.Up);
            ReadTo(first); Frames(10); Shot(shot);
            Battle($"{who}'s battle", shot + "_battle");
            ReadTo(null);
        }
        Left("the Elite Four");
        Talk("KalosLeague_ChampionRoom", 7, 5, Direction.Up);
        ReadTo("I'm Diantha"); Frames(10); Shot("wks94_diantha");
        Battle("Diantha's battle", "wks95_diantha_battle");
        ReadTo("This is the Hall of Fame"); Frames(10); Shot("wks96_hall_of_fame");
        for (int i = 0; i < 3000 && game.Map.Name != "VanivilleTown_PlayersHouse_2F"; i++)
        {
            if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
            else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
            else { Confirm(); Frames(2); }
        }
        Left("the Hall of Fame");
        game.DeleteSave();
    }
}
