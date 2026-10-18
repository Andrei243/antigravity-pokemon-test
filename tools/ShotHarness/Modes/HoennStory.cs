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
    }
}
