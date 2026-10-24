partial class Harness
{
    /// <summary>
    /// Unova's first chapter played by its own scripts (plan 22 · U5, part a), the end of the <c>unova</c> mode, in a new
    /// game of its own: Mom's questions at the foot of the stairs, Hugh and his sister at their door, Hugh catching up at
    /// the outlook's steps, Bianca at the outlook and the choice of three, the Pokédex, Hugh's first battle, the Pokémon
    /// Center's tour, Mom's Running Shoes and the Town Maps, the gate's Potion, Bianca's lesson on Route 19, Alder, the
    /// ranch's Hugh, the lost Herdier and Team Plasma's grunt, Alder's lesson at home, Cheren's Gym and the Basic Badge,
    /// Cheren and Hugh outside the school, the hiker and Cheren's advice on Route 20 (<c>us01</c> to <c>us30</c>), and the
    /// Virbank chapter of part b (<c>us31</c> to <c>us46</c>: the gate's Great Balls, Roxie and the captain, the studios' man,
    /// the Gym's guide, Roxie's band and the Toxic Badge, the scout, Team Plasma at the dock and HM01, the grunt on Route 20,
    /// the captain back and the ship terminal), through
    /// the game's own steps, <c>TryInteract</c> and arrivals. It wins every battle a scene starts and prints what each
    /// scene left behind.
    /// </summary>
    private void UnovaScenes()
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
        void TalkTo(string map, string key, string area, int dx = 0, int dy = 1)
        {
            // (the overworld holds every area's people, numbered by each area's own file: the area tells them apart)
            var m = MapDatabase.Get(map);
            var npc = m.NPCs.First(n => n.Key == key && n.ScriptFile == area);
            var facing = (dx, dy) switch { (0, 1) => Direction.Up, (0, -1) => Direction.Down, (1, 0) => Direction.Left, _ => Direction.Right };
            At(map, npc.GridX + dx, npc.GridY + dy, facing);
            game.Interact();
        }
        void Left(string what) =>
            Console.WriteLine($"  {what}: house {story.Var("VAR_UNOVA_0x40a0")}, aspertia {story.Var("VAR_UNOVA_0x40a1")}, "
                + $"route 19 {story.Var("VAR_UNOVA_0x40a3")}, floccesy {story.Var("VAR_UNOVA_0x40a5")}, ranch {story.Var("VAR_UNOVA_0x40a7")}, "
                + $"route 20 {story.Var("VAR_UNOVA_0x40ab")}, team {string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} {p.Level}"))}, "
                + $"Pokédex {story.Has(StoryState.PokedexFlag)}, shoes {story.Has(StoryState.RunningShoesFlag)}, Basic Badge {story.HasBadge(RegionDatabase.Unova, "Basic")}, "
                + $"Hugh's map {story.Has("FLAG_UNOVA_HUGHS_TOWN_MAP")}, bag {string.Join(", ", game.Bag.AllItems.Select(s => $"{s.Name} x{s.Quantity}"))}");

        // A new game of Unova, in the player's room: down to the foot of the stairs
        engine.NewGameRegion = "Unova";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        Frames(2);
        StepOnto("AspertiaCity_PlayersHouse", 10, 8, Direction.Left);
        ReadTo("have you heard of Professor Juniper"); Frames(10); Shot("us01_moms_questions");
        ReadTo("just look for a big green hat"); Frames(10); Shot("us02_sent_to_find_bianca");
        ReadTo(null);
        Left("Mom's questions");

        // ---- out of the door: Hugh and his sister
        ComeThrough("Unova", 47, 762, Direction.Down);
        ReadTo("Got yourself a Pokémon yet?"); Frames(10); Shot("us03_hugh_at_the_door");
        ReadTo("be really, really kind to it"); Frames(10); Shot("us04_hughs_sister");
        ReadTo(null);
        Left("Hugh met");

        // ---- the outlook's steps, and Bianca at the top
        StepOnto("Unova", 37, 719, Direction.Up);
        ReadTo("The outlook!"); Frames(10); Shot("us05_hugh_catches_up");
        ReadTo(null);
        TalkTo("Unova", "2", "aspertia_city");
        ReadTo("You can see all the way to the sea"); Frames(10); Shot("us06_bianca_at_the_outlook");
        for (int i = 0; i < 1500 && State() != GameState.StarterSelect; i++)
        {
            if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
            else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
            else Frames(1);
        }
        Until(() => State() == GameState.StarterSelect, "the choice of three", 10);
        Frames(40); Shot("us07_the_choice_of_three");
        var choice = game.StarterSelect;
        choice.SelectedIndex = 0;
        choice.Confirm();
        choice.Move(1);
        Frames(20);
        InputManager.Feed(ActionFrame.Press(GameAction.Confirm)); Frames(1); InputManager.Feed(null);
        ReadTo("Your very own Pokédex"); Frames(10); Shot("us08_the_pokedex");
        ReadTo("How long were you going to leave me"); Frames(10); Shot("us09_hugh_comes_up");
        IntoBattle("Hugh's first battle");
        ToMainMenu(game.Battle); Shot("us10_hughs_first_battle");
        Win();
        ReadTo("I'm heading off first"); Frames(10); Shot("us11_hugh_heads_off");
        ReadTo("show you round the Pokémon Center"); Frames(10); Shot("us12_the_centers_tour");
        ReadTo("I'll give you some to start"); Frames(10); Shot("us13_poke_balls");
        ReadTo(null);
        Left("the outlook and the tour");

        // ---- out of the Pokémon Center: Mom and the Town Maps
        ComeThrough("Unova", 48, 740, Direction.Down);
        ReadTo("They're Running Shoes!"); Frames(10); Shot("us14_the_running_shoes");
        ReadTo("Why are there two Town Maps?"); Frames(10); Shot("us15_two_town_maps");
        ReadTo(null);
        Left("the gifts");

        // ---- the gate's Potion, Bianca's lesson and Alder on Route 19
        StepOnto("AspertiaGate", 4, 11, Direction.Up);
        ReadTo("a going-away present"); Frames(10); Shot("us16_the_gates_gift");
        ReadTo(null);
        StepOnto("Unova", 53, 699, Direction.Down);
        ReadTo("watch closely"); Frames(10); Shot("us17_biancas_lesson");
        IntoBattle("Bianca's lesson");
        Skip(2.0); Shot("us18_the_lesson_in_battle");
        Win();
        ReadTo(null);
        StepOnto("Unova", 91, 695, Direction.Right);
        ReadTo("You there! Young Trainer!"); Frames(10); Shot("us19_alder_on_route_19");
        ReadTo(null);
        Left("Route 19");

        // ---- Floccesy Town: Alder, and the second Town Map
        StepOnto("Unova", 108, 695, Direction.Right);
        ReadTo("Welcome to Floccesy Town!"); Frames(10); Shot("us20_alder_in_floccesy");
        ReadTo(null);
        StepOnto("Unova", 113, 670, Direction.Up);
        ReadTo("Why are you carrying two Town Maps?"); Frames(10); Shot("us21_the_second_map");
        ReadTo(null);

        // ---- the ranch: Hugh's battle, the owners, and the lost Herdier
        StepOnto("FloccesyRanch", 41, 45, Direction.Left);
        IntoBattle("Hugh at the ranch");
        ToMainMenu(game.Battle); Shot("us22_hugh_at_the_ranch");
        Win();
        ReadTo("Are you kidding me?!"); Frames(10); Shot("us23_the_owners");
        ReadTo(null);
        StepOnto("FloccesyRanch", 22, 33, Direction.Up);
        ReadTo(null);
        StepOnto("FloccesyRanch", 23, 12, Direction.Up);
        ReadTo(null);
        StepOnto("FloccesyRanch", 43, 12, Direction.Right);
        ReadTo(null);
        StepOnto("FloccesyRanch", 51, 24, Direction.Up);
        ReadTo("Do you know who you're dealing with?"); Frames(10); Shot("us24_team_plasma");
        ReadTo(null);
        Left("the ranch");

        // ---- Alder's lesson at home
        StepOnto("Unova", 113, 667, Direction.Up);
        ReadTo("Time for some training!"); Frames(10);
        IntoBattle("the first pupil");
        Win();
        ReadTo("Water beats Fire"); Frames(10); Shot("us25_alders_lesson");
        ReadTo(null);
        Left("Alder's lesson");

        // ---- Cheren's Gym: his two pupils, and the Basic Badge
        TalkTo("AspertiaCity_Gym", "1", "aspertia_city_gym");
        IntoBattle("Pedro"); Win(); ReadTo(null);
        TalkTo("AspertiaCity_Gym", "2", "aspertia_city_gym");
        IntoBattle("Serena"); Win(); ReadTo(null);
        TalkTo("AspertiaCity_Gym", "0", "aspertia_city_gym");
        ReadTo("my very first battle as a Gym Leader"); Frames(10); Shot("us26_cheren");
        IntoBattle("Cheren");
        ToMainMenu(game.Battle); Shot("us27_cherens_battle");
        Win();
        ReadTo("the Basic Badge"); Frames(10); Shot("us28_the_basic_badge");
        ReadTo(null);
        ComeThrough("Unova", 39, 739, Direction.Down);
        ReadTo("One, two, three, let's battle!"); Frames(10); Shot("us29_hugh_challenges_cheren");
        ReadTo(null);
        Left("the Basic Badge");

        // ---- Route 20: the hiker, and Cheren's advice
        StepOnto("Unova", 158, 645, Direction.Right);
        IntoBattle("the hiker"); Win(); ReadTo(null);
        StepOnto("Unova", 161, 660, Direction.Right);
        ReadTo("See the darker grass here?"); Frames(10); Shot("us30_cherens_advice");
        ReadTo(null);
        Left("Route 20");

        // Plan 22 · U5, part b: Virbank City, from the gate's Great Balls to the ship terminal
        void Virbank(string what) =>
            Console.WriteLine($"  {what}: virbank {story.Var("VAR_UNOVA_VIRBANK")}, route 20 {story.Var("VAR_UNOVA_ROUTE_20")}, "
                + $"gate {story.Var("VAR_UNOVA_VIRBANK_GATE")}, gym {story.Var("VAR_UNOVA_VIRBANK_GYM")}, "
                + $"Toxic Badge {story.HasBadge(RegionDatabase.Unova, "Toxic")}, dock hidden {story.Has("FLAG_UNOVA_HIDE_VIRBANK_DOCK")}, "
                + $"grunt hidden {story.Has("FLAG_UNOVA_HIDE_ROUTE_20_GRUNT")}, terminal hidden {story.Has("FLAG_UNOVA_HIDE_CAPTAIN_TERMINAL")}, "
                + $"team {string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} {p.Level}"))}, "
                + $"bag {string.Join(", ", game.Bag.AllItems.Select(s => $"{s.Name} x{s.Quantity}"))}");

        // ---- the gate from Route 20, and Roxie and her father in the street
        StepOnto("VirbankGate", 13, 5, Direction.Right);
        ReadTo("Here, these will help you make some new ones."); Frames(10); Shot("us31_the_gates_great_balls");
        ReadTo(null);
        StepOnto("Unova", 205, 651, Direction.Right);
        ReadTo("Pokéstar Studios is calling"); Frames(10); Shot("us32_roxie_and_the_captain");
        ReadTo("ARGH! You stubborn"); Frames(10); Shot("us33_roxie_furious");
        ReadTo(null);
        StepOnto("Unova", 216, 642, Direction.Up);
        ReadTo("so no visitors just now"); Frames(10); Shot("us34_the_studios_closed");
        ReadTo(null);
        Virbank("Virbank City");

        // ---- the Gym: the guide, Roxie's band and the Toxic Badge, the scout on the way out
        StepOnto("VirbankCity_Gym", 8, 3, Direction.Up);
        ReadTo("a rock club too"); Frames(10); Shot("us35_the_gym_guide");
        ReadTo(null);
        TalkTo("VirbankCity_GymStage", "1", "virbank_city_gym_2");
        IntoBattle("Nicky"); Win(); ReadTo(null);
        TalkTo("VirbankCity_GymStage", "2", "virbank_city_gym_2");
        IntoBattle("Billy Jo"); Win(); ReadTo(null);
        TalkTo("VirbankCity_GymStage", "0", "virbank_city_gym_2", -1, 0);
        ReadTo("singer of this band"); Frames(10); Shot("us36_roxie");
        IntoBattle("Roxie");
        ToMainMenu(game.Battle); Shot("us37_roxies_battle");
        Win();
        ReadTo("Here, the Toxic Badge!"); Frames(10); Shot("us38_the_toxic_badge");
        ReadTo(null);
        StepOnto("VirbankCity_GymStage", 3, 6, Direction.Down);
        ReadTo("You've got star quality!"); Frames(10); Shot("us39_the_scout");
        ReadTo("I have to drag him back to his ship!"); Frames(10); Shot("us40_roxie_runs_off");
        ReadTo(null);
        Virbank("the Toxic Badge");

        // ---- the dock: Team Plasma, the grunt's battle and HM01
        StepOnto("Unova", 237, 670, Direction.Right);
        ReadTo("Two years ago the king we served"); Frames(10); Shot("us41_team_plasma_at_the_dock");
        ReadTo(null);
        TalkTo("Unova", "9", "virbank_city", -1, 0);
        IntoBattle("the grunt at the dock");
        ToMainMenu(game.Battle); Shot("us42_the_grunts_battle");
        Win();
        ReadTo("Here, take this!"); Frames(10); Shot("us43_roxies_hm01");
        ReadTo(null);
        Virbank("the dock");

        // ---- Route 20: the grunt in the dark grass, and Hugh's guess
        TalkTo("Unova", "22", "unova_route_20");
        IntoBattle("the grunt on Route 20"); Win();
        ReadTo("They must be getting around by boat."); Frames(10); Shot("us44_hugh_guesses_the_boat");
        ReadTo(null);

        // ---- the dock again: the captain back, and the ship terminal
        StepOnto("Unova", 237, 670, Direction.Right);
        ReadTo("I was just a captain in a costume."); Frames(10); Shot("us45_the_captain_back");
        ReadTo(null);
        TalkTo("VirbankCity_Terminal", "3", "virbank_city_5");
        ReadTo("we'll charge across to Castelia City!"); Frames(10); Shot("us46_the_ship_terminal");
        ReadTo(null);
        Virbank("the ship terminal");
    }
}
