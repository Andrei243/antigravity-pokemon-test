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

        // ---- Plan 21 · H2, part b: the woods, Rustboro, the tunnel and the PokéNav
        void Rustboro(string what) =>
            Console.WriteLine($"  {what}: woods {story.Var("VAR_PETALBURG_WOODS_STATE")}, rustboro {story.Var("VAR_RUSTBORO_CITY_STATE")}, "
                + $"tunnel {story.Var("VAR_RUSTURF_TUNNEL_STATE")}, route 116 {story.Var("VAR_ROUTE116_STATE")}, devon {story.Var("VAR_DEVON_CORP_3F_STATE")}, "
                + $"briney {story.Var("VAR_BRINEY_HOUSE_STATE")}, gym {story.Var("VAR_PETALBURG_GYM_STATE")}, stone badge {story.HasBadge(RegionDatabase.Hoenn, "Stone")}, "
                + $"goods stolen {story.Has("FLAG_DEVON_GOODS_STOLEN")}, recovered {story.Has("FLAG_RECOVERED_DEVON_GOODS")}, pokenav {story.Has("FLAG_SYS_POKENAV_GET")}, "
                + $"match call {story.Has("FLAG_HAS_MATCH_CALL")}, numbers {string.Join(", ", PokeNav.Contacts(story).Select(c => c.Name))}, "
                + $"rival met {story.Has("FLAG_MET_RIVAL_RUSTBORO")} beaten {story.Has("FLAG_DEFEATED_RIVAL_RUSTBORO")}, "
                + $"bag {string.Join(", ", game.Bag.AllItems.Select(s => $"{s.Name} x{s.Quantity}"))}");

        // A team that holds its own on the road to Rustboro: a Swampert of 40 leads it (the battles are only shot, and won)
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Swampert")!, 40));
        game.Party.Swap(0, game.Party.Count - 1);
        StepOnto("PetalburgWoods", 26, 23, Direction.Up);
        ReadTo("Have you seen a Shroomish"); Frames(10); Shot("hs24_the_researcher");
        ReadTo("belong to Team Aqua now"); Frames(10); Shot("hs25_the_aqua_grunt");
        IntoBattle("the grunt in the woods");
        ToMainMenu(game.Battle); Shot("hs26_the_grunts_battle");
        Win();
        ReadTo("Please, take this as my thanks"); Frames(10); Shot("hs27_the_researchers_thanks");
        ReadTo(null);
        Rustboro("the woods");

        TalkTo("RustboroCity_Gym", "roxanne");
        ReadTo("I'm Roxanne"); Frames(10); Shot("hs28_roxanne");
        IntoBattle("Roxanne's battle");
        ToMainMenu(game.Battle); Shot("hs29_roxannes_battle");
        Win();
        ReadTo("received the Stone Badge"); Frames(10); Shot("hs30_the_stone_badge");
        ReadTo(null);
        Rustboro("the Stone Badge");

        StepOnto("Hoenn", 23, 143, Direction.Left);
        ReadTo("Outta my way!"); Frames(10); Shot("hs31_the_thief");
        ReadTo("Stop, thief!"); Frames(10); Shot("hs32_the_employee_after_him");
        ReadTo(null);
        StepOnto("Hoenn", 29, 132, Direction.Right);
        ReadTo("toward Rusturf Tunnel"); Frames(10); Shot("hs33_get_them_back");
        ReadTo(null);
        StepOnto("Hoenn", 87, 131, Direction.Up);
        ReadTo("snatched my dear Peeko"); Frames(10); Shot("hs34_mr_briney");
        ReadTo(null);
        Rustboro("the theft");

        ComeThrough("RusturfTunnel", 7, 4, Direction.Right);
        StepOnto("RusturfTunnel", 9, 4, Direction.Right);
        ReadTo("Don't come any closer!"); Frames(10); Shot("hs35_the_grunt_cornered");
        ReadTo(null);
        TalkTo("RusturfTunnel", "rusturf_tunnel_grunt", dx: -1, dy: 0);
        ReadTo("it's a battle you'll get"); Frames(10);
        IntoBattle("the grunt in the tunnel");
        ToMainMenu(game.Battle); Shot("hs36_the_tunnel_battle");
        Win();
        ReadTo("my dear Peeko, you're safe"); Frames(10); Shot("hs37_peeko_safe");
        ReadTo(null);
        Rustboro("the goods recovered");

        StepOnto("Hoenn", 30, 134, Direction.Up);
        ReadTo("You got the Devon Goods back"); Frames(10); Shot("hs38_the_goods_returned");
        ReadTo("our newest product, the PokéNav"); Frames(10); Shot("hs39_mr_stone");
        ReadTo(null);
        ComeThrough("Hoenn", 11, 138, Direction.Down);
        ReadTo("Let me add Match Call"); Frames(10); Shot("hs40_match_call");
        ReadTo(null);
        Rustboro("the PokéNav");

        game.OpenPokeNav();
        Frames(20); Shot("hs41_pokenav_map");
        game.PokeNavScreen.MovePage(1); Frames(10); Shot("hs42_pokenav_condition");
        game.PokeNavScreen.MovePage(1); Frames(10); Shot("hs43_pokenav_match_call");
        game.PokeNavScreen.Cancel(); Frames(10);

        StepOnto("Hoenn", 14, 175, Direction.Down);
        ReadTo("let's register each other"); Frames(10); Shot("hs44_the_rival_in_rustboro");
        IntoBattle("the rival's battle in Rustboro");
        ToMainMenu(game.Battle); Shot("hs45_the_rivals_second_battle");
        Win();
        ReadTo("have you heard of Mr. Briney"); Frames(10); Shot("hs46_briney_hint");
        ReadTo(null);
        TalkTo("RustboroCity_CuttersHouse", "pokefan_m", dx: 1, dy: 0);
        ReadTo("A gift from one cutter to another"); Frames(10); Shot("hs47_the_cutter");
        ReadTo(null);
        TalkTo("RusturfTunnel", "rusturf_tunnel_wandas_bf", dx: -1, dy: 0);
        ReadTo("I'll dig through to you"); Frames(10); Shot("hs48_wandas_young_man");
        ReadTo(null);
        Rustboro("the chapter");

        // ---- Plan 21 · H3, part a: Mr. Briney's boat, Dewford, Granite Cave, Brawly and Slateport's museum
        void Sea(string what) =>
            Console.WriteLine($"  {what}: briney at {story.Var("VAR_BRINEY_LOCATION")}, letter delivered {story.Has("FLAG_DELIVERED_STEVEN_LETTER")}, "
                + $"knuckle badge {story.HasBadge(RegionDatabase.Hoenn, "Knuckle")}, gym {story.Var("VAR_PETALBURG_GYM_STATE")}, dewford lights {DewfordLights.LevelFor(story)}, "
                + $"dock {story.Has("FLAG_DOCK_REJECTED_DEVON_GOODS")}, goods delivered {story.Has("FLAG_DELIVERED_DEVON_GOODS")}, "
                + $"museum outside {story.Var("VAR_SLATEPORT_OUTSIDE_MUSEUM_STATE")}, numbers {string.Join(", ", PokeNav.Contacts(story).Select(c => c.Name))}, "
                + $"on {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY}), bag {string.Join(", ", game.Bag.AllItems.Select(s => $"{s.Name} x{s.Quantity}"))}");
        void Pick(string option)
        {
            Until(() => engine.Choice.IsOpen, "the question's answers");
            Frames(10);
            int at = engine.Choice.Options.ToList().FindIndex(o => o.Contains(option, StringComparison.Ordinal));
            for (int i = engine.Choice.Cursor; i < at; i++) { engine.Choice.Move(1); Frames(2); }
            engine.Choice.Confirm();
            Frames(4);
        }

        TalkTo("Route104_MrBrineysHouse", "brineys_house_briney");
        ReadTo("You saved my darling Peeko"); Frames(10); Shot("hs49_briney_at_home");
        ReadTo("Anchors aweigh"); Frames(10);
        Box().Advance();
        Skip(4.0); Shot("hs50_at_sea");
        Skip(3.0); Shot("hs51_at_sea_nearing_dewford");
        ReadTo("Here we are, Dewford Town"); Frames(10); Shot("hs52_landed_in_dewford");
        ReadTo(null);
        Sea("in Dewford");

        TalkTo("GraniteCave_1F", "hiker");
        ReadTo("Take this!"); Frames(10); Shot("hs53_the_hiker_and_flash");
        ReadTo(null);
        TalkTo("GraniteCave_StevensRoom", "granite_cave_steven");
        ReadTo("A letter for me?"); Frames(10); Shot("hs54_steven");
        ReadTo("hurry along"); Frames(10); Shot("hs55_steven_leaving");
        ReadTo(null);
        Sea("the letter delivered");

        ComeThrough("DewfordTown_Gym", 5, 25, Direction.Up);
        Frames(30); Shot("hs56_the_dark_gym");
        story.Defeat("hoenn_takao");
        story.Defeat("hoenn_jocelyn");
        story.Defeat("hoenn_laura");
        Frames(60); Shot("hs57_the_gym_lighter");
        TalkTo("DewfordTown_Gym", "brawly");
        ReadTo("I'm Brawly"); Frames(10); Shot("hs58_brawly");
        IntoBattle("Brawly's battle");
        ToMainMenu(game.Battle); Shot("hs59_brawlys_battle");
        Win();
        ReadTo("received the Knuckle Badge"); Frames(10); Shot("hs60_the_knuckle_badge");
        ReadTo(null);
        Frames(30); Shot("hs61_the_gym_lit");
        Sea("the Knuckle Badge");

        TalkTo("Hoenn", "dewford_briney", dx: -1, dy: 0);
        Pick("Slateport");
        ReadTo("Hoist the sails"); Frames(10);
        Box().Advance();
        Skip(6.0); Shot("hs62_at_sea_to_slateport");
        ReadTo("Land ho!"); Frames(10); Shot("hs63_landed_on_route_109");
        ReadTo(null);
        Sea("on Route 109's beach");

        At("Hoenn", 219, 283, Direction.Up); game.EnterArea(); Frames(20);
        Shot("hs64_aqua_at_the_museum");
        TalkTo("SlateportCity_SternsShipyard_1F", "dock");
        ReadTo("He went over to the Oceanic Museum"); Frames(10); Shot("hs65_dock");
        ReadTo(null);
        StepOnto("SlateportCity_OceanicMuseum_1F", 9, 7, Direction.Up);
        ReadTo("Would you like to come in?"); Frames(10); Shot("hs66_the_museums_fee");
        ReadTo(null);
        TalkTo("SlateportCity_OceanicMuseum_2F", "oceanic_museum_2f_capt_stern", dx: -1, dy: 0);
        ReadTo("you've brought the parts from Devon"); Frames(10); Shot("hs67_captain_stern");
        ReadTo("Those parts are coming with us!"); Frames(10); Shot("hs68_the_grunts_come_up");
        IntoBattle("the first grunt's battle");
        ToMainMenu(game.Battle); Shot("hs69_the_museum_battle");
        Win();
        ReadTo("I'm Archie"); Frames(10); Shot("hs70_archie");
        ReadTo("You've saved us"); Frames(10); Shot("hs71_sterns_thanks");
        ReadTo(null);
        Sea("the goods delivered");

        ComeThrough("Hoenn", 230, 287, Direction.Down);
        ReadTo("The whole city's talking about it"); Frames(10); Shot("hs72_scott");
        ReadTo(null);
        Sea("the chapter");

        // ---- Plan 21 · H3, part b: Route 110 to Verdanturf Town (hs73 to hs96)
        void Mauville(string what) =>
            Console.WriteLine($"  {what}: route 110 {story.Var("VAR_ROUTE110_STATE")}, trick house {story.Var("VAR_TRICK_HOUSE_LEVEL")}/{story.Var("VAR_TRICK_HOUSE_ENTRANCE_STATE")}, "
                + $"wally {story.Has("FLAG_DEFEATED_WALLY_MAUVILLE")}, scott {story.Var("VAR_SCOTT_STATE")}, bike {story.Has("FLAG_RECEIVED_BIKE")}, "
                + $"barriers {story.Has("FLAG_MAUVILLE_GYM_BARRIERS_STATE")} (gym {story.Var("VAR_MAUVILLE_GYM_STATE")}), dynamo badge {story.HasBadge(RegionDatabase.Hoenn, "Dynamo")}, "
                + $"tunnel open {story.Has("FLAG_RUSTURF_TUNNEL_OPENED")}, numbers {string.Join(", ", PokeNav.Contacts(story).Select(c => c.Name))}, "
                + $"bag {string.Join(", ", game.Bag.AllItems.Select(s => $"{s.Name} x{s.Quantity}"))}");

        StepOnto("Hoenn", 234, 216, Direction.Up);
        ReadTo("I knew you'd come this way"); Frames(10); Shot("hs73_the_rival_on_route_110");
        IntoBattle("the rival's battle on Route 110");
        ToMainMenu(game.Battle); Shot("hs74_the_rivals_battle");
        Win();
        ReadTo("It's an Itemfinder!"); Frames(10); Shot("hs75_the_itemfinder");
        ReadTo(null);
        Frames(20); Shot("hs76_the_rival_gone");
        Mauville("the rival on Route 110");

        // The Trick House: being watched, the Trick Master under his desk, the scroll and the door, his reward
        StepOnto("Route110_TrickHouseEntrance", 5, 6, Direction.Up);
        ReadTo("Someone is watching"); Frames(10); Shot("hs77_being_watched");
        ReadTo(null);
        At("Route110_TrickHouseEntrance", 7, 3, Direction.Left);
        game.Interact();
        ReadTo("I am the Trick Master"); Frames(10); Shot("hs78_the_trick_master");
        ReadTo(null);
        ComeThrough("Route110_TrickHousePuzzle1", 1, 20, Direction.Up);
        Frames(20); Shot("hs79_the_first_puzzle");
        At("Route110_TrickHousePuzzle1", 3, 17, Direction.Up);
        game.Interact();
        ReadTo("found a scroll"); Frames(10); Shot("hs80_the_scroll");
        ReadTo(null);
        At("Route110_TrickHousePuzzle1", 13, 2, Direction.Up);
        game.Interact();
        ReadTo("Click! The lock opened!"); Frames(10); Shot("hs81_the_door_unlocked");
        ReadTo(null);
        TalkTo("Route110_TrickHouseEnd", "trick_master_end", dx: 1, dy: 0);
        ReadTo("You made it all the way to me"); Frames(10); Shot("hs82_the_trick_masters_reward");
        ReadTo(null);
        Mauville("the first puzzle");

        // Mauville City: Wally at the Gym's door, his battle, Scott
        At("Hoenn", 220, 150, Direction.Up); game.EnterArea(); Frames(20);
        Shot("hs83_mauville_city");
        TalkTo("Hoenn", "mauville_wally");
        ReadTo("Will you battle me?"); Frames(10); Shot("hs84_wally_asks");
        IntoBattle("Wally's battle");
        ToMainMenu(game.Battle); Shot("hs85_wallys_battle");
        Win();
        ReadTo("Come visit us in Verdanturf"); Frames(10); Shot("hs86_wallys_uncle");
        ReadTo("I watched the whole thing"); Frames(10); Shot("hs87_scott_in_mauville");
        ReadTo(null);
        Mauville("Wally");

        // Rydel's bikes and the Rock Smash man
        TalkTo("MauvilleCity_BikeShop", "man_2", dx: 1, dy: 0);
        ReadTo("If you want to try the other kind"); Frames(10); Shot("hs88_rydels_mach_bike");
        ReadTo(null);
        TalkTo("MauvilleCity_House1", "scientist_1", dx: -1, dy: 0);
        ReadTo(null);

        // Wattson's Gym: the barriers before and after a switch, his battle and the Dynamo Badge
        ComeThrough("MauvilleCity_Gym", 4, 19, Direction.Up);
        Frames(30); Shot("hs89_the_barriers");
        StepOnto("MauvilleCity_Gym", 0, 15, Direction.Left);
        ReadTo(null);
        Frames(20); Shot("hs90_the_barriers_flipped");
        TalkTo("MauvilleCity_Gym", "wattson");
        ReadTo("I'm Wattson"); Frames(10); Shot("hs91_wattson");
        IntoBattle("Wattson's battle");
        ToMainMenu(game.Battle); Shot("hs92_wattsons_battle");
        Win();
        ReadTo("received the Dynamo Badge"); Frames(10); Shot("hs93_the_dynamo_badge");
        ReadTo(null);
        Mauville("the Dynamo Badge");

        // Rusturf Tunnel from Verdanturf's side: the last rock smashed, the young man's HM04 and Wanda
        if (!game.Party.Members[0].Moves.Any(m => m.Name == "Rock Smash"))
        {
            if (game.Party.Members[0].Moves.Count >= 4) game.Party.Members[0].Moves.RemoveAt(3);
            game.Party.Members[0].Moves.Add(new Move(MoveDatabase.Get("Rock Smash")!));
        }
        story.Set("FLAG_HIDE_RUSTURF_TUNNEL_ROCK_1");
        ComeThrough("RusturfTunnel", 24, 6, Direction.Up);
        Frames(10);
        TalkTo("RusturfTunnel", "breakable_rock_2");
        ReadTo("smashed the rocks"); Frames(10); Shot("hs94_the_tunnel_cleared");
        ReadTo(null);
        Frames(20); Shot("hs95_the_tunnel_open");
        ComeThrough("VerdanturfTown_WandasHouse", 7, 6, Direction.Up);
        Frames(20); Shot("hs96_wandas_house");
        Mauville("the tunnel");

        // Both of Rydel's bikes on their own ground: the Mach Bike flat out on the Cycling Road, the Acro Bike on Route 117
        // (on the Cycling Road's own deck, the middle of the longest column of decks on Route 110, at the deck's height)
        var road = MapDatabase.Get("Hoenn");
        var decks = new List<(int X, int Y)>();
        if (road.AreaBounds("route_110") is { } bounds)
            for (int y = bounds.Y; y < bounds.Y + bounds.Height; y++)
                for (int x = bounds.X; x < bounds.X + bounds.Width; x++)
                    if (road.AreaAt(x, y)?.Key == "route_110" && road.DeckAt(x, y) != null && !road.IsSolid(x, y)) decks.Add((x, y));
        var (deckX, deckY) = decks.Count == 0 ? (221, 190) : decks.GroupBy(t => t.X).OrderByDescending(g => g.Count()).First().OrderBy(t => t.Y).ElementAt(decks.GroupBy(t => t.X).Max(g => g.Count()) / 2);
        At("Hoenn", deckX, deckY, Direction.Up);
        if (road.DeckAt(deckX, deckY) is { } deck) game.Player.SetHeight(deck);
        game.Player.SetCycling(true);
        game.Player.Bike = BikeKind.Mach;
        Frames(10); Shot("hs97_the_mach_bike_on_the_cycling_road");
        Console.WriteLine($"  the Mach Bike on the Cycling Road at ({deckX}, {deckY}), deck {road.DeckAt(deckX, deckY)}");
        At("Hoenn", 170, 149, Direction.Left);
        game.Player.SetCycling(true);
        game.Player.Bike = BikeKind.Acro;
        Frames(10); Shot("hs98_the_acro_bike_on_route_117");
        var mudLab = new Map(5, 5) { Name = "Lab" };
        mudLab.SetBehaviour(2, 1, TileBehavior.BikeSlopeBottom);
        mudLab.SetBehaviour(1, 3, TileBehavior.RailEastWest);
        foreach (var bike in new[] { BikeKind.Mach, BikeKind.Acro })
        {
            var walker = new Walker(TravelMode.Cycling, 0f, Bike: bike);
            Console.WriteLine($"  the {bike} bike: up a muddy slope {FieldMovement.Step(mudLab, 2, 2, Direction.Up, walker).Kind}, "
                + $"onto a rail {FieldMovement.Step(mudLab, 0, 3, Direction.Right, walker).Kind}, pace {FieldMovement.PaceOn(TileBehavior.None, walker)}");
        }
        game.Player.SetCycling(false);
        game.Player.Bike = BikeKind.Bicycle;

        // ---- Plan 21 · H4, part a: Route 111 to Meteor Falls (hs99 to hs112)
        void Desert(string what) =>
            Console.WriteLine($"  {what}: vicky beaten {story.HasDefeated("hoenn_vicky")}, macho brace {game.Bag.GetQuantity(ItemDatabase.Get("Macho Brace")!)}, "
                + $"meteor falls {story.Var("VAR_METEOR_FALLS_STATE")}, route 112 clear {story.Has("FLAG_HIDE_ROUTE_112_TEAM_MAGMA")}, "
                + $"archie met {story.Has("FLAG_MET_ARCHIE_METEOR_FALLS")}, lanette home {!story.Has("FLAG_HIDE_LANETTES_HOUSE_LANETTE")}, "
                + $"workshop {story.Var("VAR_GLASS_WORKSHOP_STATE")}, ash {story.Var("VAR_ASH_GATHER_COUNT")}, "
                + $"weather here {game.Map.WeatherAt(game.Player.GridX, game.Player.GridY)}, on {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY})");

        // Route 111: Victor asks for the family's battles, and each Winstrate comes out of the door in turn
        TalkTo("Hoenn", "route111_victor");
        ReadTo("take on the whole family"); Frames(10); Shot("hs99_victor_asks");
        IntoBattle("Victor's battle");
        ToMainMenu(game.Battle); Shot("hs100_the_winstrates_battle");
        Win();
        ReadTo("you'll have to deal with me next"); Frames(10); Shot("hs101_victoria_comes_out");
        ReadTo(null);
        TalkTo("Route111_WinstrateFamilysHouse", "winstrate_house_victoria");
        ReadTo("I'd like you to have this"); Frames(10); Shot("hs102_the_macho_brace");
        ReadTo(null);
        Desert("the Winstrates");

        // The desert's edge: the storm drives back whoever has no Go-Goggles; the storm itself, seen from inside it
        StepOnto("Hoenn", 212, 61, Direction.Up);
        ReadTo("sandstorm is far too fierce"); Frames(10); Shot("hs103_turned_back_by_the_storm");
        ReadTo(null);
        Desert("the desert's edge");
        At("Hoenn", 225, 60, Direction.Up); game.EnterArea(); Frames(40);
        Shot("hs104_inside_the_storm");
        Desert("inside the storm");

        // Route 112: Team Magma's grunts give away where their crew went
        TalkTo("Hoenn", "route112_grunt_1");
        ReadTo("went off to Fallarbor"); Frames(10); Shot("hs105_the_grunts_on_route_112");
        ReadTo(null);

        // Route 113's ash and the glass workshop's Soot Sack
        At("Hoenn", 150, 10, Direction.Left); game.EnterArea(); Frames(40);
        Shot("hs106_route_113_in_the_ash");
        TalkTo("Route113_GlassWorkshop", "man_1");
        ReadTo("Take this"); Frames(10); Shot("hs107_the_soot_sack");
        ReadTo(null);
        Desert("the glass workshop");

        // Fallarbor's Pokémon Center: Lanette, who goes home to Route 114
        TalkTo("FallarborTown_PokemonCenter_1F", "fallarbor_lanette");
        ReadTo("I'm Lanette"); Frames(10); Shot("hs108_lanette");
        ReadTo(null);
        Frames(10); Shot("hs109_lanette_gone");

        // Meteor Falls: Team Magma with the meteorite, Team Aqua after them, Archie's word and the cave afterwards
        StepOnto("MeteorFalls_1F_1R", 14, 18, Direction.Left);
        ReadTo("With this meteorite"); Frames(10); Shot("hs110_team_magma_and_the_meteorite");
        ReadTo("I've seen you before"); Frames(10); Shot("hs111_archie");
        ReadTo(null);
        Frames(20); Shot("hs112_meteor_falls_afterwards");
        Desert("Meteor Falls");

        // Mauville's Game Corner: a Coin Case traded for Harbor Mail and a machine sat at
        game.Bag.AddItem(ItemDatabase.Get("Harbor Mail")!, 1);
        TalkTo("MauvilleCity_House2", "woman_3");
        ReadTo(null);
        ComeThrough("MauvilleCity_GameCorner", 11, 9, Direction.Up);
        Frames(20); Shot("hs113_the_game_corner");
        Console.WriteLine($"  the Game Corner: coin case {game.Bag.GetQuantity(ItemDatabase.Get("Coin Case")!)}, harbor mail {game.Bag.GetQuantity(ItemDatabase.Get("Harbor Mail")!)}");

        // ---- Plan 21 · H6, part a: Route 118 to Fortree City (hs114 to hs138)
        void Fortree(string what) =>
            Console.WriteLine($"  {what}: route 118 {story.Var("VAR_ROUTE118_STATE")}, institute {story.Var("VAR_WEATHER_INSTITUTE_STATE")}, "
                + $"aqua gone {story.Has("FLAG_HIDE_ROUTE_119_TEAM_AQUA")}, castform {string.Join(", ", game.Party.Members.Where(p => p.Species.Name == "Castform").Select(p => $"{p.Level} holding {p.HeldItem?.Name ?? "nothing"}"))}, "
                + $"route 119 {story.Var("VAR_ROUTE119_STATE")}, scott {story.Var("VAR_SCOTT_STATE")}, hm02 {game.Bag.GetQuantity(ItemDatabase.Get("HM02")!)}, "
                + $"devon scope {game.Bag.GetQuantity(ItemDatabase.Get("Devon Scope")!)}, bridge kecleon {story.Has("FLAG_HIDE_ROUTE_120_KECLEON_BRIDGE")}, "
                + $"fortree kecleon fled {story.Has("FLAG_KECLEON_FLED_FORTREE")}, feather badge {story.HasBadge(RegionDatabase.Hoenn, "Feather")}, "
                + $"tm40 {game.Bag.GetQuantity(ItemDatabase.Get("TM40")!)}, numbers {string.Join(", ", PokeNav.Contacts(story).Select(c => c.Name))}, "
                + $"weather here {game.Map.WeatherAt(game.Player.GridX, game.Player.GridY)}, on {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY})");
        // Someone of the overworld by key, the one nearest a tile (Hoenn has a Kecleon on Route 119, five on Route 120 and Fortree's)
        void TalkNear(string key, int x, int y)
        {
            var m = MapDatabase.Get("Hoenn");
            var npc = m.NPCs.Where(n => n.Key == key).OrderBy(n => Math.Abs(n.GridX - x) + Math.Abs(n.GridY - y)).First();
            At("Hoenn", npc.GridX, npc.GridY + 1, Direction.Up);
            game.Interact();
        }

        // Route 118: Steven hops down from the ledge, has his say and walks off east
        StepOnto("Hoenn", 284, 151, Direction.Up);
        ReadTo("Funny, running into you"); Frames(10); Shot("hs114_steven_on_route_118");
        ReadTo(null);
        Frames(20); Shot("hs115_steven_gone");
        Fortree("Steven on Route 118");

        // The Weather Institute: held by Team Aqua, Shelly upstairs, the grunt's news and Castform
        ComeThrough("Route119_WeatherInstitute_1F", 9, 11, Direction.Up);
        Frames(20); Shot("hs116_the_institute_held");
        ComeThrough("Route119_WeatherInstitute_2F", 17, 2, Direction.Down);
        Frames(20); Shot("hs117_the_upper_floor");
        TalkTo("Route119_WeatherInstitute_2F", "weather_institute_2f_shelly");
        ReadTo("Archie told me all about you"); Frames(10); Shot("hs118_shelly");
        IntoBattle("Shelly's battle");
        ToMainMenu(game.Battle); Shot("hs119_shellys_battle");
        Win();
        ReadTo("Team Magma's been seen heading"); Frames(10); Shot("hs120_the_grunts_news");
        ReadTo("It's Castform"); Frames(10); Shot("hs121_castform");
        ReadTo(null);
        Frames(20); Shot("hs122_the_institute_free");
        Fortree("the Weather Institute");

        // The rival on the bridge north of the institute, HM02 and Scott; and the road on in the rain
        StepOnto("Hoenn", 305, 31, Direction.Up);
        ReadTo("There you are!"); Frames(10); Shot("hs123_the_rival_on_route_119");
        IntoBattle("the rival's battle on Route 119");
        ToMainMenu(game.Battle); Shot("hs124_the_rivals_battle");
        Win();
        ReadTo("That's Fly"); Frames(10); Shot("hs125_hm02");
        ReadTo("Fortree's Leader, Winona"); Frames(10); Shot("hs126_scott");
        ReadTo(null);
        Frames(20);
        Fortree("the rival on Route 119");
        At("Hoenn", 300, 60, Direction.Up); game.EnterArea(); Frames(40);
        Shot("hs127_route_119_in_the_rain");
        Fortree("Route 119's rain");

        // Fortree City: the bridge to the Gym, where something nobody can see stands in the way
        TalkNear("kecleon", 345, 9);
        ReadTo("Something unseeable is in the way"); Frames(10); Shot("hs128_something_unseeable");
        ReadTo(null);

        // Route 120: Steven, the Devon Scope, the Kecleon on the bridge shown and battled, the Scope handed over
        TalkTo("Hoenn", "route120_steven");
        ReadTo("nobody can see"); Frames(10); Shot("hs129_steven_on_route_120");
        ReadTo("This is the Devon Scope"); Frames(10); Shot("hs130_the_devon_scope");
        Box().Advance(); Frames(20); Shot("hs131_the_kecleon_shows_itself");
        IntoBattle("the bridge's Kecleon");
        ToMainMenu(game.Battle); Shot("hs132_the_kecleons_battle");
        Win();
        ReadTo("Take it"); Frames(10); Shot("hs133_the_scope_given");
        ReadTo(null);
        Frames(20); Shot("hs134_steven_gone");
        Fortree("Steven on Route 120");

        // Back in Fortree City, the Kecleon seen through the Scope flees
        TalkNear("kecleon", 345, 9);
        ReadTo("was startled and fled"); Frames(10); Shot("hs135_the_kecleon_flees");
        ReadTo(null);
        Frames(10); Shot("hs136_the_bridge_clear");
        Fortree("Fortree's Kecleon");

        // Winona: her battle, the Feather Badge and TM40
        TalkTo("FortreeCity_Gym", "winona");
        ReadTo("I am Winona"); Frames(10); Shot("hs137_winona");
        IntoBattle("Winona's battle");
        ToMainMenu(game.Battle); Shot("hs138_winonas_battle");
        Win();
        ReadTo("This is the Feather Badge"); Frames(10); Shot("hs139_the_feather_badge");
        ReadTo(null);
        Fortree("Winona");

        // ---- Plan 21 · H6, part b: Route 121, the Safari Zone and Fortree's houses (hs140 to hs154)
        void Safari(string what) =>
            Console.WriteLine($"  {what}: route 121 {story.Var("VAR_ROUTE121_STATE")}, grunts hidden {story.Has("FLAG_HIDE_ROUTE_121_TEAM_AQUA_GRUNTS")}, "
                + $"safari {(game.Safari.Active ? $"on, {game.Safari.Balls} balls, {game.Safari.Steps} steps" : "off")}, zone state {story.Var("VAR_SAFARI_ZONE_STATE")}, "
                + $"good luck {story.Has("FLAG_GOOD_LUCK_SAFARI_ZONE")}, money {game.Money}, trade {story.Has("FLAG_FORTREE_NPC_TRADE_COMPLETED")}, "
                + $"tm10 {game.Bag.GetQuantity(ItemDatabase.Get("TM10")!)}, sleep talk taught {story.Has("FLAG_MOVE_TUTOR_TAUGHT_SLEEP_TALK")}, "
                + $"team {string.Join(", ", game.Party.Members.Select(p => p.DisplayName))}, on {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY})");
        void ChooseFirst()
        {
            Until(() => game.State == GameState.PartyMenu, "the team to choose from");
            Frames(10);
            game.PartyScreen.SelectedIndex = 0;
            game.PartyScreen.Confirm(game.Party);
            Frames(3);
        }

        // Route 121: the three grunts at its west end, the order to move out to Mt. Pyre, and the road clear
        At("Hoenn", 424, 87, Direction.Right); game.EnterArea(); ReadTo(null);
        engine.Steering = (Direction.Right, false);
        Until(() => engine.ScriptRunning, "Team Aqua's scene on Route 121", 120);
        engine.Steering = null;
        ReadTo("we move out to Mt. Pyre"); Frames(10); Shot("hs140_team_aqua_on_route_121");
        ReadTo(null);
        Frames(20); Shot("hs141_the_grunts_gone");
        Safari("Team Aqua on Route 121");

        // The Safari Zone: the counter's question, the balls, the zone inside its door, its attendant's luck and the way out
        StepOnto("Route121_SafariZoneEntrance", 8, 4, Direction.Left);
        ReadTo("Would you like to play the Safari Game"); Frames(10); Shot("hs142_the_safari_counter");
        ReadTo("Here are your Safari Balls"); Frames(10); Shot("hs143_thirty_safari_balls");
        ReadTo(null);
        Frames(20); Shot("hs144_in_the_safari_zone");
        Safari("into the Safari Zone");
        TalkTo("HoennSafariZone", "safari_exit_attendant");
        ReadTo("Good luck out there"); Frames(10); Shot("hs145_good_luck");
        ReadTo(null);
        TalkTo("HoennSafariZone", "safari_exit_attendant");
        ReadTo("Would you like to leave the Safari Zone now"); Frames(10); Shot("hs146_leave_early");
        ReadTo(null);
        Frames(20); Shot("hs147_out_past_the_counter");
        Safari("out of the Safari Zone");

        // Fortree's first house: the boy's Plusle for a Volbeat
        game.Party.Members.Insert(0, new Pokemon(PokemonDatabase.Get("Volbeat")!, 20));
        TalkTo("FortreeCity_House1", "ninja_boy");
        ReadTo("what I really want is a Volbeat"); Frames(10); Shot("hs148_the_trade_offer");
        ReadTo("Which Pokémon will you trade");
        Box().Advance(); Frames(2);
        ChooseFirst();
        ReadTo("Look after Pluses"); Frames(10); Shot("hs149_pluses");
        ReadTo(null);
        Safari("the Fortree trade");

        // Fortree's second house: the coin in the old woman's hand three times, and the old man's Sleep Talk
        TalkTo("FortreeCity_House2", "expert_f");
        ReadTo("I hold a coin in one hand"); Frames(10); Shot("hs150_the_hidden_power_game");
        ReadTo("Which hand holds the coin"); Frames(10); Shot("hs151_which_hand");
        Pick("Right");
        ReadTo("Again, now"); Pick("Right");
        ReadTo("One more"); Pick("Left");
        ReadTo("Three in a row"); Frames(10); Shot("hs152_three_in_a_row");
        ReadTo(null);
        game.Party.Members.Insert(0, new Pokemon(PokemonDatabase.Get("Slakoth")!, 5));
        TalkTo("FortreeCity_House2", "expert_m");
        ReadTo("Shall I teach one of yours Sleep Talk"); Frames(10); Shot("hs153_the_sleep_talk_tutor");
        ReadTo("Which one");
        Box().Advance(); Frames(2);
        ChooseFirst();
        ReadTo("learned"); Frames(10); Shot("hs154_sleep_talk_learned");
        ReadTo(null);
        Safari("Fortree's second house");
    }
}
