partial class Harness
{
    /// <summary>
    /// Hoenn's first chapter played by its own scripts (plan 21 · H2), the end of the <c>hoenn</c> mode: the moving van
    /// told over the dark and Mom at the door, moving in, the clock and Mom upstairs, Dad on the television, the
    /// neighbour, the rival at the Poké Ball, the girl at the top of the road, Professor Birch chased on Route 101, his
    /// bag and the choice of three, the first battle, his thanks in the lab, the rival's battle on Route 103, the Pokédex,
    /// Mom's Running Shoes, Oldale's footprints, Petalburg's boy, Norman in his Gym, Wally's catch and his thanks
    /// (<c>hs01</c> to <c>hs22</c>), through the game's own steps, <c>TryInteract</c> and arrivals. It wins every battle a
    /// scene starts and prints what each scene left behind.
    /// </summary>
    private void HoennScenes()
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
                // (A warp's arrival script starts a few frames after the warp: the field is only done once it has stayed still)
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
                // The catching lesson plays itself: it is only read on
                if (fight.Kind != BattleKind.CatchingLesson && fight.HUD.MenuState == BattleMenuState.Main)
                {
                    foreach (var foe in fight.EnemyParty?.Members ?? new List<Pokemon>()) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    if (fight.EnemyPokemon is { } met) met.CurrentHP = Math.Min(met.CurrentHP, 1);
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
            Console.WriteLine($"  {what}: intro {story.Var("VAR_LITTLEROOT_INTRO_STATE")}, town {story.Var("VAR_LITTLEROOT_TOWN_STATE")}, "
                + $"rival {story.Var("VAR_LITTLEROOT_RIVAL_STATE")}, route 101 {story.Var("VAR_ROUTE101_STATE")}, lab {story.Var("VAR_BIRCH_LAB_STATE")}, "
                + $"petalburg {story.Var("VAR_PETALBURG_CITY_STATE")}, gym {story.Var("VAR_PETALBURG_GYM_STATE")}, "
                + $"team {string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} {p.Level}"))}, "
                + $"Pokédex {story.Has(StoryState.PokedexFlag)}, shoes {story.Has(StoryState.RunningShoesFlag)}, rival {PlayerIdentity.RivalName}, "
                + $"bag {string.Join(", ", game.Bag.AllItems.Select(s => $"{s.Name} x{s.Quantity}"))}");

        // A new game of Hoenn, from its first step: the van
        engine.NewGameRegion = "Hoenn";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        Frames(2);
        ReadTo("The moving van rumbles"); Frames(10); Shot("hs01_the_van");
        ReadTo("Welcome to Littleroot Town!"); Frames(10); Shot("hs02_mom_at_the_door");
        ReadTo("Your room is upstairs"); Frames(10); Shot("hs03_moving_in");
        ReadTo(null);
        Left("in from the van");

        // ---- the clock, and Mom come up to see it
        At("LittlerootTown_BrendansHouse_2F", 5, 2, Direction.Up);
        game.Interact();
        ReadTo("set the clock to the right time"); Frames(10); Shot("hs04_the_clock");
        ReadTo("Come back downstairs"); Frames(10); Shot("hs05_mom_upstairs");
        ReadTo(null);

        // ---- Dad on the television
        ComeThrough("LittlerootTown_BrendansHouse_1F", 8, 3, Direction.Down);
        ReadTo("It's Dad's Gym on the television"); Frames(10); Shot("hs06_dad_on_the_television");
        ReadTo(null);
        Left("the television");

        // ---- next door: the neighbour, and the rival at the Poké Ball
        ComeThrough("LittlerootTown_MaysHouse_1F", 2, 7, Direction.Up);
        ReadTo("You must be"); Frames(10); Shot("hs07_the_neighbour");
        ReadTo(null);
        ComeThrough("LittlerootTown_MaysHouse_2F", 5, 5, Direction.Up);
        Frames(20); Shot("hs08_the_rivals_room");
        game.Interact();
        ReadTo("Huh? Who are you?"); Frames(10); Shot("hs09_the_rival");
        ReadTo(null);
        Left("the rival met");

        // ---- the girl at the top of the road, and the professor in trouble
        StepOnto("Hoenn", 131, 283, Direction.Up);
        ReadTo("Somebody's yelling out there"); Frames(10); Shot("hs10_someone_calling");
        ReadTo(null);
        ComeThrough("Hoenn", 130, 282, Direction.Up);
        // (The professor's rig and the Zigzagoon's field sprite are made as they come near: let them be ready)
        At("Hoenn", 129, 277, Direction.Up); Frames(120);
        ComeThrough("Hoenn", 130, 282, Direction.Up);
        StepOnto("Hoenn", 130, 281, Direction.Up);
        ReadTo("H-help me!"); Frames(10); Shot("hs11_birch_in_trouble");
        ReadTo(null);

        // ---- the bag and the choice of three
        TalkTo("Hoenn", "birchs_bag", dx: 1, dy: 0);
        Until(() => State() == GameState.StarterSelect || (Box().IsActive && Box().IsCurrentLineComplete), "the bag", 600);
        ReadTo("Don't just stand there looking!");
        for (int i = 0; i < 900 && State() != GameState.StarterSelect; i++)
        {
            if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
            else Frames(1);
        }
        Until(() => State() == GameState.StarterSelect, "the choice of three", 10);
        Frames(40); Shot("hs12_birchs_bag");
        var choice = game.StarterSelect;
        choice.SelectedIndex = 1;
        choice.Confirm();
        choice.Move(1);
        Frames(20);
        InputManager.Feed(ActionFrame.Press(GameAction.Confirm)); Frames(1); InputManager.Feed(null);
        IntoBattle("the Zigzagoon");
        ToMainMenu(game.Battle); Shot("hs13_the_first_battle");
        Win();
        ReadTo("Come with me to my lab"); Frames(10); Shot("hs14_saved");
        ReadTo("Why don't you keep it?"); Frames(10); Shot("hs15_birchs_thanks");
        ReadTo(null);
        Left("the starter chosen");

        // ---- the rival on Route 103
        TalkTo("Hoenn", "route103_rival", dx: 1, dy: 0);
        ReadTo("Let's have a battle!"); Frames(10); Shot("hs16_the_rival_on_route_103");
        IntoBattle("the rival's battle");
        ToMainMenu(game.Battle); Shot("hs17_the_rivals_battle");
        Win();
        ReadTo(null);
        Left("the rival beaten");

        // ---- the Pokédex, and the Running Shoes
        ComeThrough("LittlerootTown_ProfessorBirchsLab", 6, 11, Direction.Up);
        ReadTo("It's my Pokédex."); Frames(10); Shot("hs18_the_pokedex");
        ReadTo(null);
        ComeThrough("Hoenn", 127, 291, Direction.Right);
        StepOnto("Hoenn", 128, 291, Direction.Right);
        ReadTo("put on the Running Shoes"); Frames(10); Shot("hs19_the_running_shoes");
        ReadTo(null);
        Left("the adventure begun");

        // ---- Petalburg: Norman, Wally's catch and his thanks
        ComeThrough("PetalburgCity_Gym", 4, 108, Direction.Up);
        Frames(20); Shot("hs20_the_gym_hall");
        TalkTo("PetalburgCity_Gym", "petalburg_gym_norman");
        ReadTo("My name is Wally"); Frames(10); Shot("hs21_norman_and_wally");
        IntoBattle("Wally's catch");
        Skip(2.0); Shot("hs22_wallys_catch");
        Win();
        ReadTo("Mr. Norman! I caught a Ralts!"); Frames(10); Shot("hs23_wallys_thanks");
        ReadTo(null);
        Left("the chapter's part");
    }
}
