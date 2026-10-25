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
    /// the captain back and the ship terminal), and the Castelia chapter of part c (<c>us46</c> to <c>us80</c>: the voyage, Prime Pier,
    /// the Harlequin's Bicycle, Clyde and Iris, Thumb Pier, the sewers beside Hugh, Burgh and the Insect Badge, Colress on the
    /// plaza and on Route 4), through
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
                    // (a double battle's second foe is no member of the first trainer's team, and its move wants a target:
                    // the sewers' tag battle beside Hugh went round its Leers for half an hour)
                    if (fight.IsDouble)
                        foreach (var foe in fight.EnemySlots.Select(s => s.Pokemon))
                            if (foe != null) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    // (a foe's berry restores what was taken and its blow can still fell the lead: Burgh's Leavanny ate its
                    // Sitrus Berry coming in and knocked Snivy out with one Struggle Bug. The foes are left nothing to hold
                    // and only Struggle, as the Hoenn mode leaves a Leader's)
                    foreach (var foe in (fight.EnemyParty?.Members ?? new List<Pokemon>()).Concat(fight.EnemySlots.Select(s => s.Pokemon)))
                        if (foe != null) { foe.HeldItem = null; foreach (var move in foe.Moves) move.CurrentPP = 0; }
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
        ReadTo("You're heading to Castelia City too, right?"); Frames(10); Shot("us46_the_ship_terminal");
        Virbank("the ship terminal");

        // Plan 22 · U5, part c: the voyage and Castelia City, from Prime Pier to Colress on Route 4
        void Castelia(string what) =>
            Console.WriteLine($"  {what}: castelia {story.Var("VAR_UNOVA_CASTELIA")}, map {game.Map.Name} {game.Player.GridX},{game.Player.GridY}, "
                + $"Insect Badge {story.HasBadge(RegionDatabase.Unova, "Insect")}, partner {game.PartnerAlong ?? "none"}, "
                + $"Clyde hidden {story.Has("FLAG_UNOVA_HIDE_CLYDE_STREET")}, Burgh hidden {story.Has("FLAG_UNOVA_HIDE_BURGH_GYM")}, "
                + $"Crustle hidden {story.Has("FLAG_UNOVA_HIDE_CRUSTLE_ROUTE_4")}, "
                + $"team {string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} {p.Level}"))}, "
                + $"bag {string.Join(", ", game.Bag.AllItems.Select(s => $"{s.Name} x{s.Quantity}"))}");

        // ---- the voyage across the bay, and ashore at Prime Pier
        ReadTo("All aboard for Castelia City"); Frames(10); Shot("us47_all_aboard");
        ReadTo("churns across the bay"); Frames(10); Shot("us48_the_voyage");
        ReadTo("the biggest city in all of Unova!"); Frames(10); Shot("us49_ashore_at_prime_pier");
        ReadTo("That ship with the black sails"); Frames(10); Shot("us50_the_black_sails");
        ReadTo(null);
        Castelia("ashore");

        // ---- the avenue: the Harlequin's Bicycle, and the avenue itself
        StepOnto("CasteliaCity", 39, 11, Direction.Up);
        ReadTo("Is this your very first time in Castelia City?"); Frames(10); Shot("us51_the_harlequin");
        ReadTo(null);
        At("CasteliaCity", 28, 8, Direction.Down); Frames(30); Shot("us52_the_avenue");
        At("CasteliaCity", 84, 3, Direction.Up); Frames(30); Shot("us53_the_mouth_of_gym_street");
        Castelia("the Bicycle");

        // ---- Gym Street: Clyde at the door, and Iris
        At("CasteliaCity_GymStreet", 15, 40, Direction.Up); Frames(30); Shot("us54_gym_street");
        TalkTo("CasteliaCity_GymStreet", "0", "castelia_city_3", -1, 0);
        ReadTo("I'm sorry, he isn't here."); Frames(10); Shot("us55_clyde");
        ReadTo("Burgh's gone wandering off again?"); Frames(10); Shot("us56_iris_arrives");
        ReadTo("so I'll help you look!"); Frames(10); Shot("us57_iris_helps");
        ReadTo(null);
        Castelia("Gym Street");

        // ---- the avenue: Iris calls the player on to Thumb Pier
        StepOnto("CasteliaCity", 11, 8, Direction.Left);
        ReadTo("Hey, over here! Come on, come on!"); Frames(10); Shot("us58_iris_on_the_avenue");
        ReadTo(null);

        // ---- Thumb Pier: Hugh comes running and goes down into the sewers
        ComeThrough("CasteliaCity_ThumbPier", 2, 14, Direction.Right);
        ReadTo("Here's the way down into the sewers!"); Frames(10); Shot("us59_thumb_pier");
        ReadTo("Did you find Team Plasma?!"); Frames(10); Shot("us60_hugh_at_thumb_pier");
        ReadTo(null);
        Castelia("Thumb Pier");

        // ---- the sewers: Hugh comes along, Team Plasma's grunts, Burgh, HM04 and the stranger
        StepOnto("CasteliaSewers", 83, 20, Direction.Left);
        ReadTo("Team Plasma's down here somewhere"); Frames(10); Shot("us61_hugh_in_the_sewers");
        ReadTo(null);
        At("CasteliaSewers", 60, 10, Direction.Left); Frames(30); Shot("us62_the_sewers");
        StepOnto("CasteliaSewers", 51, 9, Direction.Up);
        ReadTo("Huh? What do you want?"); Frames(10); Shot("us63_plasma_in_the_sewers");
        IntoBattle("the grunts in the sewers");
        ToMainMenu(game.Battle); Shot("us64_the_tag_battle");
        Win();
        ReadTo("One and the same! I'm Burgh."); Frames(10); Shot("us65_burgh_in_the_sewers");
        ReadTo("Here, take this!"); Frames(10); Shot("us66_hughs_hm04");
        ReadTo("That was a remarkable display of battling."); Frames(10); Shot("us67_the_stranger");
        ReadTo("my muse and I!"); Frames(10); Shot("us68_burgh_goes_back");
        ReadTo(null);
        Castelia("the sewers");

        // ---- Thumb Pier again: Iris's word
        StepOnto("CasteliaCity_ThumbPier", 10, 19, Direction.Up);
        ReadTo("Did everything go all right down there?"); Frames(10); Shot("us69_iris_afterward");
        ReadTo(null);

        // ---- the Gym: Burgh's battle and the Insect Badge
        ComeThrough("CasteliaCity_Gym", 16, 19, Direction.Up); Frames(30); Shot("us70_the_castelia_gym");
        TalkTo("CasteliaCity_Gym", "burgh", "castelia_city_gym");
        ReadTo("painter of Bug Pokémon"); Frames(10); Shot("us71_burgh");
        IntoBattle("Burgh");
        ToMainMenu(game.Battle); Shot("us72_burghs_battle");
        Win();
        ReadTo("Here, the Insect Badge!"); Frames(10); Shot("us73_the_insect_badge");
        ReadTo(null);
        Castelia("the Insect Badge");

        // ---- the plaza: Colress
        At("CasteliaCity_Plaza", 16, 18, Direction.Up); Frames(30); Shot("us74_the_plaza");
        StepOnto("CasteliaCity_Plaza", 15, 3, Direction.Up);
        ReadTo("might I have a look at your Pokémon?"); Frames(10); Shot("us75_colress");
        ReadTo("my name is Colress"); Frames(10); Shot("us76_colress_names_himself");
        ReadTo(null);

        // ---- Route 4: the Crustle woken and Colress's battle
        StepOnto("Unova", 430, 584, Direction.Up);
        ReadTo("Curious about what's behind me?"); Frames(10); Shot("us77_colress_on_route_4");
        ReadTo("Were they lying here all along"); Frames(10); Shot("us78_the_crustle_woken");
        IntoBattle("Colress");
        ToMainMenu(game.Battle); Shot("us79_colress_battle");
        Win();
        ReadTo("Until we meet again"); Frames(10); Shot("us80_colress_goes_on");
        ReadTo(null);
        Castelia("Route 4");

        // Plan 22 · U6: Route 4's rest house, the Desert Resort, Nimbasa City's Subway Bosses, Elesa's Gym and Hugh
        // against Team Plasma
        void Nimbasa(string what) =>
            Console.WriteLine($"  {what}: nimbasa {story.Var("VAR_UNOVA_NIMBASA")}, runway {story.Var("VAR_UNOVA_NIMBASA_GYM")}, "
                + $"map {game.Map.Name} {game.Player.GridX},{game.Player.GridY}, Bolt Badge {story.HasBadge(RegionDatabase.Unova, "Bolt")}, "
                + $"partner {game.PartnerAlong ?? "none"}, bosses hidden {story.Has("FLAG_UNOVA_HIDE_SUBWAY_BOSSES")}, "
                + $"Rosa hidden {story.Has("FLAG_UNOVA_HIDE_ROSA_NIMBASA")}, Nate hidden {story.Has("FLAG_UNOVA_HIDE_NATE_NIMBASA")}, "
                + $"Hugh hidden {story.Has("FLAG_UNOVA_HIDE_HUGH_NIMBASA")}, Plasma hidden {story.Has("FLAG_UNOVA_HIDE_NIMBASA_PLASMA")}, "
                + $"weather here {game.Map.WeatherAt(game.Player.GridX, game.Player.GridY)}, "
                + $"team {string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} {p.Level}"))}, "
                + $"bag {string.Join(", ", game.Bag.AllItems.Select(s => $"{s.Name} x{s.Quantity}"))}");

        // ---- Route 4's rest house, and the Desert Resort's archaeologist
        TalkTo("Route4_RestHouse", "1", "unova_route_4_3");
        ReadTo("Sit down and rest a while."); Frames(10); Shot("us81_the_rest_house");
        ReadTo(null);
        TalkTo("DesertResort", "0", "desert_resort_2");
        ReadTo("built long ago by a hero"); Frames(10); Shot("us82_the_desert_resort");
        ReadTo(null);
        Nimbasa("the desert");

        // ---- Nimbasa City, in from Join Avenue: Rosa and the Subway Bosses, and the Vs. Recorder
        ComeThrough("Unova", 429, 478, Direction.Up); Frames(30); Shot("us83_nimbasa_city");
        TalkTo("Unova", "5", "nimbasa_city");
        ReadTo("I need a partner!"); Frames(10); Shot("us84_rosa_and_the_subway_bosses");
        ReadTo("Destination: victory. All aboard!"); Frames(10); Shot("us85_emmet");
        IntoBattle("the Subway Bosses");
        ToMainMenu(game.Battle); Shot("us86_the_subway_bosses_battle");
        Win();
        ReadTo("make a truly splendid team!"); Frames(10); Shot("us87_ingo_after");
        ReadTo("This is for you!"); Frames(10); Shot("us88_the_vs_recorder");
        ReadTo(null);
        Nimbasa("the Subway Bosses");

        // ---- the Gym in the amusement park: the guide, the three models down the runway, Elesa and the Bolt Badge
        ComeThrough("NimbasaCity_Gym", 15, 59, Direction.Up); Frames(30); Shot("us89_the_nimbasa_gym");
        TalkTo("NimbasaCity_Gym", "1", "nimbasa_city_gym", -1, 0);
        ReadTo("Here, a drink before the show!"); Frames(10); Shot("us90_the_guide");
        ReadTo(null);
        StepOnto("NimbasaCity_Gym", 15, 54, Direction.Up);
        ReadTo("you are walking in it!"); Frames(10); Shot("us91_nikola");
        IntoBattle("Nikola"); Win();
        ReadTo(null);
        StepOnto("NimbasaCity_Gym", 15, 39, Direction.Up);
        ReadTo("how beautiful you are as a Trainer!"); Frames(10); Shot("us92_fleming");
        IntoBattle("Fleming"); Win();
        ReadTo(null);
        StepOnto("NimbasaCity_Gym", 15, 24, Direction.Up);
        ReadTo("share the stage with Elesa!"); Frames(10); Shot("us93_ampere");
        IntoBattle("Ampère"); Win();
        ReadTo(null);
        TalkTo("NimbasaCity_Gym", "4", "nimbasa_city_gym");
        ReadTo("whose light shines brightest!"); Frames(10); Shot("us94_elesa");
        IntoBattle("Elesa");
        ToMainMenu(game.Battle); Shot("us95_elesas_battle");
        Win();
        ReadTo("Here, this is yours!"); Frames(10); Shot("us96_the_bolt_badge");
        ReadTo(null);
        Nimbasa("the Bolt Badge");

        // ---- the street to Route 5: Hugh and Team Plasma
        StepOnto("Unova", 409, 439, Direction.Left);
        ReadTo("What are you lot up to here?"); Frames(10); Shot("us97_hugh_and_plasma");
        ReadTo("Cover me!"); Frames(10); Shot("us98_cover_me");
        IntoBattle("the first grunt");
        ToMainMenu(game.Battle); Shot("us99_the_grunts_battle");
        Win();
        ReadTo("Team Plasma is searching for something."); Frames(10); Shot("us100_plasma_searching");
        ReadTo("my little sister's Purrloin"); Frames(10); Shot("us101_hughs_sister");
        ReadTo("I'm counting on you!"); Frames(10); Shot("us102_hugh_goes_on");
        ReadTo(null);
        Nimbasa("Hugh and Team Plasma");

        // Plan 22 · U6, part b: the Ferris wheel, Bianca's Fly on Route 5, Driftveil's bridge and Rood, Clay's Gym and the
        // World Tournament's word
        void Driftveil(string what) =>
            Console.WriteLine($"  {what}: route 5 {story.Var("VAR_UNOVA_ROUTE_5")}, driftveil {story.Var("VAR_UNOVA_DRIFTVEIL")}, "
                + $"map {game.Map.Name} {game.Player.GridX},{game.Player.GridY}, Quake Badge {story.HasBadge(RegionDatabase.Unova, "Quake")}, "
                + $"grunt hidden {story.Has("FLAG_UNOVA_HIDE_DRIFTVEIL_GRUNT")}, Rood at the door hidden {story.Has("FLAG_UNOVA_HIDE_ROOD_DOOR")}, "
                + $"worker hidden {story.Has("FLAG_UNOVA_HIDE_DRIFTVEIL_GYM_WORKER")}, Clay outside hidden {story.Has("FLAG_UNOVA_HIDE_CLAY_DRIFTVEIL")}, "
                + $"camera {game.Map.Camera}, deck here {game.Map.DeckAt(game.Player.GridX, game.Player.GridY)?.ToString() ?? "none"}, "
                + $"team {string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} {p.Level}"))}, "
                + $"bag {string.Join(", ", game.Bag.AllItems.Select(s => $"{s.Name} x{s.Quantity}"))}");

        // ---- the Ferris wheel in the amusement park
        TalkTo("NimbasaCity_AmusementPark", "wheel", "nimbasa_city_2");
        ReadTo("One turn takes in the whole of Nimbasa City"); Frames(10); Shot("us103_the_ferris_wheel");
        ReadTo("The car climbs slowly"); Frames(10); Shot("us104_the_ride");
        ReadTo("Welcome back!"); Frames(10); Shot("us105_back_on_the_ground");
        ReadTo(null);

        // ---- Route 5: Bianca runs after the player with HM02
        StepOnto("Unova", 372, 438, Direction.Left);
        ReadTo("Heeey! Wait for me!"); Frames(10); Shot("us106_bianca_runs_up");
        ReadTo("It's Fly!"); Frames(10); Shot("us107_hm02");
        ReadTo(null);
        Driftveil("Route 5");

        // ---- the drawbridge, walked onto its deck
        ComeThrough("DriftveilDrawbridge", 209, 47, Direction.Left); Frames(30); Shot("us108_on_the_drawbridge");
        Driftveil("the drawbridge");

        // ---- Driftveil's bridge into town: the grunt, Hugh's chase and the former member
        StepOnto("Unova", 221, 433, Direction.Left);
        ReadTo("The boss wants you back."); Frames(10); Shot("us109_plasma_on_the_bridge");
        ReadTo("Answer me!"); Frames(10); Shot("us110_hugh_chases");
        ReadTo("We live in the old church"); Frames(10); Shot("us111_the_former_member");
        ReadTo(null);
        Driftveil("the bridge");

        // ---- the old church: Rood's test, Hugh back, and the tale inside
        StepOnto("Unova", 194, 398, Direction.Right);
        ReadTo("I would see what kind of person you are."); Frames(10); Shot("us112_rood");
        IntoBattle("Rood");
        ToMainMenu(game.Battle); Shot("us113_roods_battle");
        Win();
        ReadTo("He got away from me..."); Frames(10); Shot("us114_hugh_comes_back");
        ReadTo("Everyone here once wore Team Plasma's uniform."); Frames(10); Shot("us115_roods_tale");
        ReadTo("Saying sorry doesn't give anyone back"); Frames(10); Shot("us116_hugh_answers");
        ReadTo(null);
        Driftveil("Rood's tale");

        // ---- the Gym: the guide's Fresh Water, the lift down, Clay and the Quake Badge
        ComeThrough("DriftveilCity_GymLobby", 7, 13, Direction.Up); Frames(30); Shot("us117_the_gym_lobby");
        TalkTo("DriftveilCity_GymLobby", "0", "driftveil_city_gym_2", 1, 0);
        ReadTo("have a drink before you go down!"); Frames(10); Shot("us118_the_guide");
        ReadTo(null);
        ComeThrough("DriftveilCity_Gym", 10, 26, Direction.Up); Frames(30); Shot("us119_the_mine");
        At("DriftveilCity_Gym", 10, 4, Direction.Up);
        game.Interact();
        ReadTo("I'm Clay, boss of these mines"); Frames(10); Shot("us120_clay");
        IntoBattle("Clay");
        ToMainMenu(game.Battle); Shot("us121_clays_battle");
        Win();
        ReadTo("You struck a seam"); Frames(10); Shot("us122_the_quake_badge");
        ReadTo(null);
        Driftveil("the Quake Badge");

        // ---- out of the Gym's door: Clay and Hugh, and the World Tournament
        StepOnto("Unova", 212, 401, Direction.Right);
        ReadTo("You got your Badge too?"); Frames(10); Shot("us123_hugh_outside_the_gym");
        ReadTo("South of town there's the Pokémon World Tournament."); Frames(10); Shot("us124_clay_and_the_tournament");
        ReadTo(null);
        Driftveil("Clay and Hugh");

        // Plan 22 · U6, part c: the Lostlorn Forest's traveller, the train to Anville Town and back, a sand pit of the Relic
        // Castle, Volcarona, and the Plasma Frigate at the World Tournament's pier
        void Frigate(string what) =>
            Console.WriteLine($"  {what}: pwt {story.Var("VAR_UNOVA_PWT")}, map {game.Map.Name} {game.Player.GridX},{game.Player.GridY}, "
                + $"moored {story.Has("FLAG_UNOVA_FRIGATE_AT_PIER")}, sailed {story.Has("FLAG_UNOVA_FRIGATE_SAILED")}, "
                + $"grunts hidden {story.Has("FLAG_UNOVA_HIDE_FRIGATE_GRUNTS")}, Zoroark's traveller hidden {story.Has("FLAG_UNOVA_HIDE_LOSTLORN_TRAVELLER")}, "
                + $"Volcarona hidden {story.Has("FLAG_UNOVA_HIDE_VOLCARONA")}, "
                + $"team {string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} {p.Level}"))}, "
                + $"bag {string.Join(", ", game.Bag.AllItems.Select(s => $"{s.Name} x{s.Quantity}"))}");
        void TalkToKey(string map, string key, int dx = 0, int dy = 1)
        {
            // (someone of a map file of ours, the Gear Station's agents, by their id alone)
            var m = MapDatabase.Get(map);
            var npc = m.NPCs.First(n => n.Key == key);
            var facing = (dx, dy) switch { (0, 1) => Direction.Up, (0, -1) => Direction.Down, (1, 0) => Direction.Left, _ => Direction.Right };
            At(map, npc.GridX + dx, npc.GridY + dy, facing);
            game.Interact();
        }

        // ---- the Lostlorn Forest: the traveller, and the Zoroark he was
        TalkTo("LostlornForest", "7", "lostlorn_forest");
        ReadTo("do you know of a Pokémon called Zoroark?"); Frames(10); Shot("us125_the_traveller");
        ReadTo("there's always a grain of truth in a rumour"); Frames(10); Shot("us126_the_rumour");
        ReadTo("Grrrowf!"); Frames(10); Shot("us127_the_zoroark");
        ReadTo(null);
        Frigate("the Lostlorn Forest");

        // ---- the Gear Station, and the train out to Anville Town and back
        ComeThrough("GearStation", 11, 12, Direction.Up); Frames(30); Shot("us128_the_gear_station");
        TalkToKey("GearStation", "anville_desk");
        ReadTo("Will you ride the train to Anville Town?"); Frames(10); Shot("us129_the_anville_line");
        ReadTo("rattles east past fields and woods"); Frames(10); Shot("us130_on_the_train");
        ReadTo(null); Frames(40); Shot("us131_anville_town");
        TalkTo("AnvilleTown", "2", "anville_town", -1, 0);
        ReadTo("The train back to Nimbasa City leaves shortly."); Frames(10); Shot("us132_the_train_home");
        ReadTo(null); Frames(40); Shot("us133_back_at_the_gear_station");
        Frigate("the train");

        // ---- the Relic Castle: a sand pit down to the floor below, and Volcarona in the deepest room
        ComeThrough("RelicCastle", 24, 23, Direction.Up); Frames(30); Shot("us134_the_relic_castle");
        StepOnto("RelicCastle", 18, 15, Direction.Left);
        ReadTo("The sand gives way underfoot...!"); Frames(10); Shot("us135_the_sand_pit");
        ReadTo(null); Frames(40); Shot("us136_the_floor_below");
        TalkTo("RelicCastle_Depths", "0", "relic_castle_23");
        ReadTo("spreads its burning wings!"); Frames(10); Shot("us137_volcarona");
        IntoBattle("Volcarona");
        ToMainMenu(game.Battle); Shot("us138_volcaronas_battle");
        Win();
        ReadTo(null);
        Frigate("Volcarona");

        // Plan 22 · U6, part d: the team screen answered (the first of the party, in their order), and a shop's counter looked at
        void EnterTheTeam(int count = 3)
        {
            for (int i = 0; i < 900 && State() != GameState.PartyMenu; i++)
            {
                if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else Frames(1);
            }
            if (State() != GameState.PartyMenu) { Console.WriteLine("  !! never happened: the team to choose from"); return; }
            var screen = game.PartyScreen;
            Frames(60);
            for (int slot = 0; slot < count; slot++) { screen.SelectedIndex = slot; Frames(2); screen.Confirm(game.Party); Frames(6); }
            screen.Confirm(game.Party);
            Frames(4);
        }
        void AtTheCounter(string shot)
        {
            for (int i = 0; i < 900 && State() != GameState.Shop; i++)
            {
                if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            if (State() != GameState.Shop) { Console.WriteLine("  !! never happened: the counter"); return; }
            Frames(20);
            Shot(shot);
            game.Shop.Close();
            Frames(6);
        }
        void Hall(string what) =>
            Console.WriteLine($"  {what}: first tournament {story.Has("FLAG_UNOVA_PWT_FIRST_TOURNAMENT")}, BP {game.Frontier.BattlePoints}, "
                + $"run {story.Var(PokemonPlatinumEngine.Models.Frontier.BattleSubway.Vars.State)}, map {game.Map.Name} {game.Player.GridX},{game.Player.GridY}, "
                + $"team {string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} {p.Level}"))}");

        // ---- the hall's first tournament (plan 22 · U6, part d): Clay's welcome, the offer, the team of three, and the
        // bracket against Cheren, Hugh and Clay, the prize and the pier waiting
        foreach (string species in new[] { "Lillipup", "Pidove" }) game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, 30));
        ComeThrough("PokemonWorldTournament", 11, 14, Direction.Up); Frames(30);
        ReadTo("Welcome to the Pokémon World Tournament!"); Frames(10); Shot("us150_clay_welcomes");
        ReadTo("I'd like you in it too."); Frames(10); Shot("us151_the_entrants");
        ReadTo("Will you enter the Driftveil Tournament?"); Frames(10); Shot("us152_the_offer");
        ReadTo("Then choose the Pokémon you'll enter.");
        EnterTheTeam(); Frames(30);
        ReadTo("Eight Trainers took the stage"); Frames(10); Shot("us153_the_stage");
        ReadTo("I'm not holding back"); Frames(10); Shot("us154_cheren_on_the_stage");
        IntoBattle("Cheren");
        ToMainMenu(game.Battle); Shot("us155_the_first_round");
        Win();
        ReadTo("You've earned your place in the next round."); Frames(10); Shot("us156_cheren_beaten");
        ReadTo("I've been waiting for this since we left the Gym."); Frames(10); Shot("us157_hugh_on_the_stage");
        ReadTo("Final round"); Frames(10); Shot("us158_clay_on_the_stage");
        IntoBattle("Clay");
        ToMainMenu(game.Battle); Shot("us159_the_final");
        Win();
        ReadTo("Take these Battle Points"); Frames(10); Shot("us160_the_prize");
        ReadTo("There's a big ship moored"); Frames(10); Shot("us161_clay_points_to_the_pier");
        ReadTo(null); Frames(20); Shot("us162_the_hall_afterwards");
        Hall("the tournament");
        TalkToKey("PokemonWorldTournament", "receptionist");
        ReadTo("has been played"); Frames(10); Shot("us163_the_desk_afterwards");
        ReadTo(null);

        // ---- the World Tournament's pier: Cheren, Hugh, the running grunt and Colress
        StepOnto("Unova", 197, 476, Direction.Down);
        ReadTo("I hardly knew you."); Frames(10); Shot("us139_cheren_and_hugh");
        ReadTo("Out of the way, out of the way!"); Frames(10); Shot("us140_the_grunt_runs");
        ReadTo("Children have no business"); Frames(10); Shot("us141_colress");
        ReadTo("I would very much like to see what you do."); Frames(10); Shot("us142_colress_steps_back");
        ReadTo(null);
        Frigate("the pier");

        // ---- aboard the Plasma Frigate: two grunts, Zinzolin and the Shadow Triad, and ashore again
        ComeThrough("PlasmaFrigate", 9, 29, Direction.Right);
        ReadTo("This ship's freezing"); Frames(10); Shot("us143_aboard_the_frigate");
        ReadTo("you're up first!");
        IntoBattle("the first grunt");
        ToMainMenu(game.Battle); Shot("us144_the_frigates_grunt");
        Win();
        ReadTo("You won't get past me so easy!");
        IntoBattle("the second grunt");
        Win();
        ReadTo("Once again, we are made to feel unwelcome."); Frames(10); Shot("us145_zinzolin");
        ReadTo("you leave this ship. Now."); Frames(10); Shot("us146_the_shadow_triad");
        ReadTo("There is a rush of wind"); Frames(10); Shot("us147_put_ashore");
        ReadTo("And the ship's leaving!"); Frames(10); Shot("us148_hugh_on_the_pier");
        ReadTo("I'm going on to Route 6."); Frames(10); Shot("us149_cheren_goes_on");
        ReadTo(null);
        Frigate("ashore");

        // Plan 22 · U6, part d: the Battle Subway's Single Train from the Gear Station's desk to the end of a set, Anville's
        // lost Pansage and Driftveil Market's stalls
        void Subway(string what) =>
            Console.WriteLine($"  {what}: run {story.Var(PokemonPlatinumEngine.Models.Frontier.BattleSubway.Vars.State)}, "
                + $"streak {story.Var(PokemonPlatinumEngine.Models.Frontier.BattleSubway.Vars.Wins)}, "
                + $"best single {story.Var(PokemonPlatinumEngine.Models.Frontier.BattleSubway.Vars.Best(PokemonPlatinumEngine.Models.Frontier.SubwayTrain.Single))}, "
                + $"BP {game.Frontier.BattlePoints}, map {game.Map.Name} {game.Player.GridX},{game.Player.GridY}");

        // ---- the Battle Subway: the desk, the three taken aboard, seven cars, and off again with the points
        ComeThrough("GearStation", 11, 4, Direction.Up); Frames(30);
        TalkToKey("GearStation", "single_desk");
        ReadTo("Welcome to the Battle Subway!"); Frames(10); Shot("us164_the_single_desk");
        ReadTo("What would you like to do?"); Frames(10); Shot("us165_the_desks_choices");
        ReadTo("Please choose the Pokémon you will take aboard.");
        EnterTheTeam(); Frames(30);
        ReadTo("Car 1"); Frames(20); Shot("us166_the_first_car");
        IntoBattle("the first car");
        ToMainMenu(game.Battle); Shot("us167_a_subway_battle");
        Win();
        ReadTo("Well done! The next car is coming up."); Frames(10); Shot("us168_the_next_car");
        ReadTo("That's the end of the line for this set"); Frames(10); Shot("us169_a_set_cleared");
        ReadTo("Here are the Battle Points you earned."); Frames(10); Shot("us170_the_points");
        ReadTo(null); Frames(40); Shot("us171_back_at_the_gear_station");
        Subway("the Single Train");

        // ---- Anville Town: the girl's lost Pansage, found in the plaza and returned
        ComeThrough("AnvilleTown", 20, 52, Direction.Right); Frames(30);
        TalkTo("AnvilleTown", "16", "anville_town", 1, 0);
        ReadTo("My Pansage hopped onto a train"); Frames(10); Shot("us172_the_lost_pansage");
        ReadTo(null);
        Frames(20); Shot("us173_the_pansage_in_the_plaza");
        TalkTo("AnvilleTown", "15", "anville_town");
        ReadTo("Pansage: Sage! Sage!"); Frames(10); Shot("us174_the_pansage_found");
        ReadTo(null);
        TalkTo("AnvilleTown", "16", "anville_town", 1, 0);
        ReadTo("That's my Pansage!"); Frames(10); Shot("us175_the_pansage_returned");
        ReadTo(null);
        Frames(20); Shot("us176_the_pansage_at_her_side");

        // ---- Driftveil Market: three sellers at their stalls
        ComeThrough("DriftveilCity_Market", 9, 16, Direction.Up); Frames(30); Shot("us177_driftveil_market");
        TalkTo("DriftveilCity_Market", "1", "driftveil_city_7");
        AtTheCounter("us178_the_herb_stall");
        ReadTo(null);
        TalkTo("DriftveilCity_Market", "3", "driftveil_city_7");
        AtTheCounter("us179_the_sailors_goods");
        ReadTo(null);
        TalkTo("DriftveilCity_Market", "4", "driftveil_city_7");
        AtTheCounter("us180_the_sweets");
        ReadTo(null);
    }
}
