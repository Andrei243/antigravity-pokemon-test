partial class Harness
{
    /// <summary>
    /// Kalos drawn by hand (plan 23 · Z1; not part of <c>all</c>): a new game begun there (<c>--region Kalos</c>) in the
    /// player's room, Vaniville Town, Route 1 and Aquacorde Town by day and after dark (<c>wk01</c> to <c>wk08</c>), the
    /// rooms (<c>wk10</c> to <c>wk15</c>), a skater on Vaniville's lane (<c>wk20</c>, <c>wk21</c>), and the first chapter
    /// played by its own scripts in a new game of its own (<c>wks01</c> to <c>wks12</c>: Mom on the first morning, the
    /// neighbours at the door, the friends at the café, the choice of three, the Pokédex, Shauna's battle, the letter,
    /// Mom and the roller skates). It wins the battle a scene starts and prints what each scene left behind.
    /// </summary>
    public void KalosMode()
    {
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
        };
        foreach (var (day, _, map, x, y, facing) in places)
        {
            At(map, x, y, facing); Frames(2); Shot(day);
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
        };
        foreach (var (name, map, x, y, facing) in rooms) { At(map, x, y, facing); Frames(2); Shot(name); }

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
            At(map, x, y, facing); Frames(2); Shot(night);
        }
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        KalosScenes();
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
        game.DeleteSave();
    }
}
