partial class Harness
{
    /// <summary>
    /// Johto's first chapter played by its own scripts (plan 20 · J4), the end of the <c>johto</c> mode: Mom and the
    /// Pokégear, the boy at the lab's window, Elm's favour, the three in his machine, the Running Shoes, the Guide Gent,
    /// Mr. Pokémon's Egg and Oak's Pokédex, the red-haired boy in Cherrygrove City and his battle, the policeman, the
    /// catching lesson, Sprout Tower's top and Elder Li, Falkner's Gym, his battle and the Zephyr Badge, the Egg in
    /// Violet City and Route 32's head (<c>jst01</c> to <c>jst22</c>), through the game's own steps, <c>TryInteract</c>
    /// and arrivals. It wins every battle a scene starts (the lead never tires) and prints what each scene left behind.
    /// </summary>
    private void JohtoScenes()
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
            for (int i = 0; i < most; i++)
            {
                if (text != null && Box().IsActive && Translated.Saying(Box().CurrentLine, text)) { Whole(); Console.WriteLine($"  said: {text}"); return; }
                if (text != null && !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld && i > 60) break;
                if (text == null && !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld) return;
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
                    var lead = fight.PlayerPokemon;
                    if (lead != null) lead.CurrentHP = lead.MaxHP;
                    fight.SelectMainMenuOption(0);
                    int hurts = lead == null ? 0 : Math.Max(0, lead.Moves.FindIndex(m => m.Data.Category != MoveCategory.Status));
                    fight.SelectMove(hurts);
                }
                else Confirm(fight);
                Skip(0.5);
            }
            Until(() => FieldAgain(), "the field again", 1500);
            Frames(10);
        }
        void IntoBattle(string what)
        {
            for (int i = 0; i < 1500 && State() != GameState.Battle; i++)
            {
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            if (State() != GameState.Battle) Console.WriteLine($"  !! never happened: {what}");
            else Skip(1.0);
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
        void TalkTo(string map, string key, int dx = 0, int dy = 1)
        {
            var m = MapDatabase.Get(map);
            var npc = m.NPCs.First(n => n.Key == key);
            var facing = (dx, dy) switch { (0, 1) => Direction.Up, (0, -1) => Direction.Down, (1, 0) => Direction.Left, _ => Direction.Right };
            At(map, npc.GridX + dx, npc.GridY + dy, facing);
            game.Interact();
        }
        void Left(string what) =>
            Console.WriteLine($"  {what}: house {story.Var("VAR_SCENE_PLAYERS_HOUSE_1F")}, lab {story.Var("VAR_SCENE_ELMS_LAB")}, town {story.Var("VAR_SCENE_NEW_BARK_TOWN_OW")}, "
                + $"cherrygrove {story.Var("VAR_SCENE_CHERRYGROVE_CITY_OW")}, lesson {story.Var("VAR_UNK_408B")}, "
                + $"team {string.Join(", ", game.Party.Members.Select(p => p.IsEgg ? "an Egg" : $"{p.Species.Name} {p.Level}"))}, "
                + $"Pokégear {story.Has("FLAG_GOT_POKEGEAR")}, map card {story.Has("FLAG_GOT_MAP_CARD")}, Pokédex {story.Has(StoryState.PokedexFlag)}, "
                + $"shoes {story.Has(StoryState.RunningShoesFlag)}, Zephyr Badge {story.HasBadge("Johto", "Zephyr")}, "
                + $"bag {string.Join(", ", game.Bag.AllItems.Select(s => $"{s.Name} x{s.Quantity}"))}");

        // A new game of Johto, from its first step
        engine.NewGameRegion = "Johto";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        Frames(30);
        Shot("jst00_the_bedroom");
        Left("the bedroom");

        // ---- Mom and the Pokégear
        ComeThrough("NewBark_PlayersHouse_1F", 4, 3, Direction.Down);
        ReadTo("Your Pokégear!"); Frames(10); Shot("jst01_mom_and_the_pokegear");
        ReadTo(null);
        Left("downstairs");

        // ---- the boy at the lab's window, and Elm's favour
        TalkTo("Johto", "t20_gsrivel");
        ReadTo("Get lost."); Frames(10); Shot("jst02_the_boy_at_the_window");
        ReadTo(null);
        StepOnto("NewBark_ElmsLab", 4, 10, Direction.Up);
        ReadTo("In the machine behind my desk"); Frames(10); Shot("jst03_elms_favour");
        ReadTo(null);

        // ---- the three in the machine
        At("NewBark_ElmsLab", 6, 4, Direction.Up);
        game.Interact();
        Until(() => State() == GameState.StarterSelect, "the choice of three", 600);
        Frames(40); Shot("jst04_the_three");
        var choice = game.StarterSelect;
        choice.SelectedIndex = 1;
        choice.Confirm();
        choice.Move(1);
        Frames(20);
        InputManager.Feed(ActionFrame.Press(GameAction.Confirm)); Frames(1); InputManager.Feed(null);
        ReadTo("A wonderful choice!"); Frames(10); Shot("jst05_a_wonderful_choice");
        ReadTo(null);
        StepOnto("NewBark_ElmsLab", 4, 11, Direction.Down);
        ReadTo(null);
        Left("a Pokémon of their own");

        // ---- Mom's Running Shoes on the lab's step
        StepOnto("Johto", 684, 394, Direction.Down);
        ReadTo("received the Running Shoes"); Frames(10); Shot("jst06_the_running_shoes");
        ReadTo(null);

        // ---- the Guide Gent's tour
        StepOnto("Johto", 566, 397, Direction.Left);
        ReadTo("Shall I show you round Cherrygrove?"); Frames(10); Shot("jst07_the_guide_gent");
        ReadTo("And this is my house"); Frames(10); Shot("jst08_the_guide_gents_house");
        ReadTo(null);

        // ---- Mr. Pokémon's Egg and Oak's Pokédex
        ComeThrough("Route30_MrPokemonsHouse", 4, 8, Direction.Up);
        ReadTo("It's an Egg!"); Frames(10); Shot("jst09_mr_pokemon");
        ReadTo("It's a Pokédex."); Frames(10); Shot("jst10_professor_oak");
        ReadTo("Something terrible has happened!"); Frames(10); Shot("jst11_elms_call");
        ReadTo(null);
        Left("the errand run");

        // ---- the red-haired boy in Cherrygrove
        StepOnto("Johto", 575, 398, Direction.Left);
        ReadTo("A waste on someone like you."); Frames(10); Shot("jst12_the_red_haired_boy");
        IntoBattle("the rival's battle");
        ToMainMenu(game.Battle); Shot("jst13_the_rivals_battle");
        Win();
        ReadTo("My name?"); Frames(10); Shot("jst14_his_name");
        ReadTo(null);

        // ---- the theft and the policeman; the catching lesson
        ComeThrough("NewBark_ElmsLab", 4, 13, Direction.Up);
        ReadTo("So it was you, was it?"); Frames(10); Shot("jst15_the_policeman");
        ReadTo(null);
        StepOnto("Johto", 666, 398, Direction.Left);
        ReadTo("Watch me!"); Frames(10); Shot("jst16_the_catching_lesson");
        ReadTo(null);
        Left("the lesson");

        // ---- Sprout Tower's top and Elder Li
        StepOnto("SproutTower_3F", 13, 17, Direction.Up);
        ReadTo("you treat your Pokémon as tools"); Frames(10); Shot("jst17_sprout_tower_top");
        ReadTo(null);
        TalkTo("SproutTower_3F", "d15r0103_chourou");
        IntoBattle("Elder Li's battle");
        Win();
        ReadTo("TM70 holds Flash."); Frames(10); Shot("jst18_elder_li");
        ReadTo(null);

        // ---- Falkner and the Zephyr Badge
        At("Violet_Gym", 15, 26, Direction.Up); Frames(30); Shot("jst19_violet_gym");
        TalkTo("Violet_Gym", "t22gym0101_gsleader1");
        ReadTo("I'm Falkner"); Frames(10); Shot("jst20_falkner");
        IntoBattle("Falkner's battle");
        ToMainMenu(game.Battle); Shot("jst21_falkners_battle");
        Win();
        ReadTo("received the Zephyr Badge"); Frames(10); Shot("jst22_the_zephyr_badge");
        ReadTo(null);

        // ---- the Egg in Violet City's Pokémon Center, and Route 32's head
        TalkTo("Violet_PokemonCenter_1F", "violet_pokecenter_1f.Aide");
        ReadTo("Will you look after it?"); Frames(10); Shot("jst23_the_egg");
        ReadTo(null);
        StepOnto("Johto", 475, 305, Direction.Down);
        ReadTo("That's the Zephyr Badge"); Frames(10); Shot("jst24_route_32");
        ReadTo(null);
        Left("the chapter's end");
    }
}
