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
        // Reads on, a line at a time, until something holds (a picker or a question opened past the lines before it)
        void ReadUntil(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++)
            {
                if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
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
                // A phrase asked for again (Gabby and Ty's second interview): the one given last time, kept
                if (State() == GameState.Phrases && game.Phrases.IsActive)
                {
                    Frames(10); game.Phrases.ToDone(); game.Phrases.Confirm(); Frames(4);
                    if (game.Phrases.IsActive) { game.Phrases.Cancel(); game.Phrases.Cancel(); Frames(4); }
                    continue;
                }
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
                    // (In a double battle the lead's partner is a Pokémon of the twenties against Leaders of the forties,
                    // felled at full HP by one blow: the foes are left only Struggle, as the arcade mode leaves its Star's)
                    if (fight.IsDouble)
                        foreach (var foe in (fight.EnemyParty?.Members ?? new List<Pokemon>()).Concat(fight.EnemySlots.Select(s => s.Pokemon)))
                            if (foe != null) foreach (var move in foe.Moves) move.CurrentPP = 0;
                    var lead = fight.PlayerPokemon;
                    // (Whoever stands for the player is kept whole, both of a double battle's, and a long chapter's
                    // battles spend their PP: they are kept able, as the other modes keep theirs)
                    foreach (var mine in fight.PlayerSlots.Select(s => s.Pokemon).Append(lead))
                        if (mine != null && !mine.IsFainted)
                        {
                            mine.CurrentHP = mine.MaxHP;
                            foreach (var move in mine.Moves) move.CurrentPP = move.MaxPP;
                        }
                    fight.SelectMainMenuOption(0);
                    int hurts = lead == null ? 0 : Math.Max(0, lead.Moves.FindIndex(m => m.Data.Category != MoveCategory.Status));
                    fight.SelectMove(hurts);
                    // (A double battle asks which foe the move is for: Gabby and Ty's)
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
        void TalkTo(string map, string key, int dx = 0, int dy = 1)
        {
            var m = MapDatabase.Get(map);
            var npc = m.NPCs.First(n => n.Key == key);
            var facing = (dx, dy) switch { (0, 1) => Direction.Up, (0, -1) => Direction.Down, (1, 0) => Direction.Left, _ => Direction.Right };
            At(map, npc.GridX + dx, npc.GridY + dy, facing);
            // (Put down, the player stands on the ground: someone who stands on a bridge's deck, Route 120's Steven, is
            // spoken to from the deck, and someone spoken to from the water by a surfer)
            if (m.DeckAt(npc.GridX + dx, npc.GridY + dy) is { } deck && npc.Level is { } level && MathF.Abs(level - deck) < 0.5f)
            {
                game.Player.SetHeight(deck);
                Frames(2);
            }
            else if (m.DeckAt(npc.GridX + dx, npc.GridY + dy) == null && m.IsDeepWater(npc.GridX + dx, npc.GridY + dy))
            {
                game.Player.SetMode(TravelMode.Surfing);
                Frames(2);
            }
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
        // (The Slakoth came only to learn: the Swampert leads again, or a level 9 Slakoth leads the battles to come and faints)
        var strongest = game.Party.Members.First(p => p.Species.Name == "Swampert");
        game.Party.Members.Remove(strongest);
        game.Party.Members.Insert(0, strongest);
        Safari("Fortree's second house");

        // ---- Plan 21 · H6, part c: Gabby and Ty, Fortree's Wingull and the plank bridges (hs155 to hs167)
        void Reporters(string what) =>
            Console.WriteLine($"  {what}: battles {story.Var("VAR_GABBY_AND_TY_BATTLE_NUM")}, route {story.Var("VAR_GABBY_AND_TY_ROUTE")}, trivia {story.Var("VAR_GABBY_AND_TY_TRIVIA")}, "
                + $"mons {story.Var("VAR_GABBY_AND_TY_MON_1")}/{story.Var("VAR_GABBY_AND_TY_MON_2")}, on air {story.Has("FLAG_GABBY_AND_TY_ON_AIR")}, "
                + $"show new {story.Has("FLAG_GABBY_AND_TY_SHOW_NEW")}, quote pending {story.Has("FLAG_GABBY_AND_TY_QUOTE_PENDING")}, "
                + $"phrase {PhraseGroups.Shown(story.PhraseOf("GABBY_AND_TY") ?? Array.Empty<string>())}, "
                + $"wingull errand {story.Has("FLAG_WINGULL_SENT_ON_ERRAND")}, wingull hidden {story.Has("FLAG_HIDE_FORTREE_CITY_HOUSE_4_WINGULL")}, "
                + $"mossdeep wingull hidden {story.Has("FLAG_HIDE_MOSSDEEP_CITY_HOUSE_2_WINGULL")}");

        // Route 111: the pair where their first battle puts them, the battle for the cameras and the interview
        story.SetVar("VAR_GABBY_AND_TY_BATTLE_NUM", 0);
        story.ClearLocal();
        At("Hoenn", 213, 90, Direction.Up); game.EnterArea(); Frames(20); Shot("hs155_gabby_and_ty_on_route_111");
        TalkTo("Hoenn", "route111_gabby_1");
        ReadTo("Wait, wait!"); Frames(10); Shot("hs156_gabby_spots_the_player");
        ReadTo("two against two"); Frames(10); Shot("hs157_the_offer");
        IntoBattle("Gabby and Ty's battle");
        ToMainMenu(game.Battle); Shot("hs158_gabby_and_tys_double_battle");
        Win();
        ReadTo("We lost! Wow"); Frames(10); Shot("hs159_gabby_beaten");
        ReadTo("who ARE you"); Frames(10); Shot("hs15a_the_interview_asked");
        ReadTo("tell us in two words"); Frames(10);
        ReadUntil(() => State() == GameState.Phrases, "the phrase picker", 300);
        var phrasePicker = game.Phrases;
        Frames(15); Shot("hs15b_the_phrase_picker");
        phrasePicker.Confirm(); phrasePicker.Move(1, 0); phrasePicker.Move(0, 1); Frames(4);
        phrasePicker.Confirm(); phrasePicker.Move(0, 1); Frames(4);
        phrasePicker.Move(0, -1); phrasePicker.Confirm(); Frames(4);
        phrasePicker.Entry!.Choose(1, "small:together"); Frames(4); Shot("hs15c_the_phrase_whole");
        phrasePicker.Confirm();
        Until(() => State() != GameState.Phrases, "the picker put away", 60);
        ReadTo("That's just what the viewers want"); Frames(10); Shot("hs15d_the_viewers_will_like_it");
        ReadTo(null);
        Reporters("the first interview");

        // Route 118 a battle on: the pair quote the answer back and ask after the battle they remember
        story.ClearLocal();
        At("Hoenn", 270, 148, Direction.Up); game.EnterArea(); Frames(20); Shot("hs15e_gabby_and_ty_on_route_118");
        TalkTo("Hoenn", "route118_gabby_1");
        ReadTo("Last time you told us"); Frames(10); Shot("hs15f_the_answer_quoted");
        ReadTo(null);
        Reporters("the second battle");

        // Route 120, and the segment on the air
        story.ClearLocal();
        At("Hoenn", 373, 17, Direction.Up); game.EnterArea(); Frames(20); Shot("hs160_gabby_and_ty_on_route_120");
        Reporters("on Route 120");

        // Fortree's fourth house: the boy sends his Wingull off, the bird runs out of the door
        TalkTo("FortreeCity_House4", "boy_3");
        ReadTo("Off you go"); Frames(10); Shot("hs161_the_boy_sends_his_wingull");
        Frames(15); Shot("hs162_the_wingull_running");
        ReadTo(null);
        Frames(20); Shot("hs163_the_wingull_gone");
        TalkTo("FortreeCity_House4", "boy_3");
        ReadTo("errand"); Frames(10); Shot("hs164_the_errand");
        ReadTo(null);
        Reporters("the Wingull's errand");

        // The plank bridges: a walker's sprite drops as their feet come onto a plank, springs back and settles lower than on the ground
        (int X, int Y)? plank = null;
        var fortreeMap = MapDatabase.Get("Hoenn");
        for (int py = 0; py < 30 && plank == null; py++)
            for (int px = 320; px < 380 && plank == null; px++)
                if (FieldLife.IsPlank(fortreeMap, px, py) && FieldLife.IsPlank(fortreeMap, px + 1, py)) plank = (px, py);
        if (plank is { } plankAt)
        {
            At("Hoenn", plankAt.X - 1, plankAt.Y, Direction.Right); Frames(20); Shot("hs165_before_the_plank");
            game.Player.SetPosition(plankAt.X, plankAt.Y, Direction.Right);
            Frames(1); Shot("hs166_the_plank_gives");
            Frames(5); Shot("hs166b_the_plank_springs_back");
            Frames(40); Shot("hs167_standing_on_the_plank");
            Console.WriteLine($"  plank: tile ({plankAt.X}, {plankAt.Y}), rows now {game.World.Life.PlankRows(fortreeMap, plankAt.X + 0.5f, plankAt.Y + 0.5f)}");
        }
        else Console.WriteLine("  !! no plank tile found round Fortree City");

        // ---- Plan 21 · H7, part a: Lilycove City and Mt. Pyre (hs168 to hs17b)
        void Pyre(string what) =>
            Console.WriteLine($"  {what}: lilycove visited {story.Has("FLAG_VISITED_LILYCOVE_CITY")}, chapter eight {story.Has("FLAG_HOENN_CHAPTER_EIGHT")}, "
                + $"rival met {story.Has("FLAG_MET_RIVAL_LILYCOVE")}, declined {story.Has("FLAG_DECLINED_RIVAL_BATTLE_LILYCOVE")}, "
                + $"mt pyre {story.Var("VAR_MT_PYRE_STATE")}, orbs taken {story.Has("FLAG_RECEIVED_RED_OR_BLUE_ORB")}, "
                + $"archie hidden {story.Has("FLAG_HIDE_MT_PYRE_SUMMIT_ARCHIE")}, aqua hidden {story.Has("FLAG_HIDE_MT_PYRE_SUMMIT_TEAM_AQUA")}, "
                + $"magma guard hidden {story.Has("FLAG_HIDE_JAGGED_PASS_MAGMA_GUARD")}, emblem {game.Bag.GetQuantity(ItemDatabase.Get("Magma Emblem")!)}, "
                + $"cleanse tag {game.Bag.GetQuantity(ItemDatabase.Get("Cleanse Tag")!)}, loto today {story.Has("FLAG_DAILY_PICKED_LOTO_TICKET")}, "
                + $"on {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY})");

        // Lilycove City: the rival before the Department Store, the battle and the rival gone home
        At("Hoenn", 507, 80, Direction.Up); game.EnterArea(); ReadTo(null); Frames(20); Shot("hs168_lilycove_and_the_rival");
        Pyre("into Lilycove");
        TalkTo("Hoenn", "var_0");
        ReadTo("how about a battle"); Frames(10); Shot("hs169_the_rivals_challenge");
        IntoBattle("the rival's battle in Lilycove");
        ToMainMenu(game.Battle); Shot("hs16a_the_rival_in_lilycove");
        Win();
        ReadTo("head home to Littleroot"); Frames(10); Shot("hs16b_the_rival_beaten");
        ReadTo(null);
        Frames(20); Shot("hs16c_the_rival_gone");
        Pyre("the rival in Lilycove");

        // The Lottery Corner on the store's ground floor
        TalkTo("LilycoveCity_DepartmentStore_1F", "lottery_clerk");
        ReadTo("Will you draw"); Frames(10); Shot("hs16d_the_lottery_corner");
        ReadTo(null);
        Pyre("the Lottery Corner");

        // Mt. Pyre: the old woman's Cleanse Tag, the summit held by Team Aqua, Archie's words and the Magma Emblem
        TalkTo("MtPyre_1F", "old_woman");
        ReadTo("keep the wilder ones away"); Frames(10); Shot("hs16e_the_cleanse_tag");
        ReadTo(null);
        ComeThrough("MtPyre_Summit", 23, 9, Direction.Up); Frames(20); Shot("hs170_team_aqua_on_the_summit");
        StepOnto("MtPyre_Summit", 23, 7, Direction.Up);
        ReadTo("You turn up everywhere"); Frames(10); Shot("hs171_archie_turns");
        ReadTo("The Red Orb is ours"); Frames(10); Shot("hs172_the_red_orb");
        ReadTo("Both of the orbs are gone"); Frames(10); Shot("hs173_team_aqua_gone");
        ReadTo("they dropped this"); Frames(10); Shot("hs174_the_old_woman");
        ReadTo("received"); Frames(10); Shot("hs175_the_magma_emblem");
        ReadTo(null);
        Frames(20); Shot("hs176_the_summit_after");
        Pyre("Mt. Pyre's summit");
        TalkTo("MtPyre_Summit", "old_man");
        ReadTo("hear out an old man"); Frames(10); Shot("hs177_the_old_mans_tale");
        ReadTo(null);

        // ---- Plan 21 · H7, part b: the two teams' hideouts (hs178 to hs199)
        void Hideouts(string what) =>
            Console.WriteLine($"  {what}: jagged pass {story.Var("VAR_JAGGED_PASS_STATE")}, hideout open {story.Has("FLAG_HOENN_MAGMA_HIDEOUT_OPEN")}, "
                + $"groudon awake {story.Has("FLAG_GROUDON_AWAKENED_MAGMA_HIDEOUT")}, magma grunts hidden {story.Has("FLAG_HIDE_MAGMA_HIDEOUT_GRUNTS")}, "
                + $"slateport {story.Var("VAR_SLATEPORT_CITY_STATE")}, harbour {story.Var("VAR_SLATEPORT_HARBOR_STATE")}, "
                + $"aqua guards hidden {story.Has("FLAG_HIDE_AQUA_HIDEOUT_1F_GRUNT_1_BLOCKING_ENTRANCE")}, "
                + $"electrode {story.Has("FLAG_DEFEATED_ELECTRODE_1_AQUA_HIDEOUT")}/{story.Has("FLAG_DEFEATED_ELECTRODE_2_AQUA_HIDEOUT")}, "
                + $"submarine gone {story.Has("FLAG_TEAM_AQUA_ESCAPED_IN_SUBMARINE")}, lilycove grunts hidden {story.Has("FLAG_HIDE_LILYCOVE_CITY_AQUA_GRUNTS")}, "
                + $"rival home {!story.Has("FLAG_HIDE_LITTLEROOT_TOWN_MAYS_HOUSE_RIVAL_BEDROOM")}, on {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY})");

        // Jagged Pass: the Magma Emblem shakes the rock open
        At("JaggedPass", 16, 22, Direction.Up); game.EnterArea(); ReadTo(null); Frames(20); Shot("hs178_jagged_pass_the_rock");
        Hideouts("onto Jagged Pass");
        StepOnto("JaggedPass", 21, 15, Direction.Left);
        Frames(15); Shot("hs179_the_mountain_shakes");
        ReadTo("hole has opened"); Frames(10); Shot("hs17a_the_way_in");
        ReadTo(null);
        Hideouts("the rock opened");

        // Team Magma's hideout: the ground floor, Tabitha, Maxie before the sleeping Groudon and its waking
        ComeThrough("MagmaHideout_1F", 10, 33, Direction.Up); Frames(20); Shot("hs180_magma_hideout");
        At("MagmaHideout_4F", 22, 6, Direction.Up); Frames(20); Shot("hs181_tabitha");
        At("MagmaHideout_4F", 16, 22, Direction.Up); Frames(20); Shot("hs182_maxie_and_groudon");
        TalkTo("MagmaHideout_4F", "magma_hideout_4f_maxie");
        ReadTo("This is the Blue Orb"); Frames(10); Shot("hs183_the_blue_orb");
        ReadTo("Let its light wake you"); Box().Advance();
        Frames(60); Shot("hs184_the_orbs_light");
        Frames(150); Shot("hs185_groudon_wakes");
        Frames(80); Shot("hs186_the_hall_shakes");
        ReadTo("What is the matter"); Frames(10); Shot("hs187_groudon_gone");
        ReadTo("cheap trick");
        IntoBattle("Maxie's battle in the hideout");
        ToMainMenu(game.Battle); Shot("hs188_maxies_battle");
        Win();
        ReadTo("This is where we part"); Frames(10); Shot("hs189_maxie_goes_after_groudon");
        ReadTo(null);
        Frames(30); Shot("hs18a_the_hall_empty");
        Hideouts("Groudon woken");

        // Slateport: Captain Stern's interview, and Team Aqua takes his submarine from the harbour
        At("Hoenn", 227, 273, Direction.Right); game.EnterArea(); ReadTo(null); Frames(20); Shot("hs190_sterns_interview");
        TalkTo("Hoenn", "slateport_capt_stern", -1, 0);
        ReadTo("My first interview"); Frames(10); Shot("hs191_stern_and_the_player");
        ReadTo("taking charge of this submarine"); Frames(10); Shot("hs192_a_shout_from_the_harbour");
        ReadTo(null);
        Frames(40); Shot("hs193_into_the_harbour");
        Hideouts("into the harbour");
        StepOnto("SlateportCity_Harbor", 8, 13, Direction.Left);
        ReadTo("hideout in Lilycove"); Frames(10); Shot("hs194_archie_at_the_berth");
        ReadTo("slid out of the harbour"); Frames(10); Shot("hs195_the_submarine_gone");
        ReadTo("I don't know what to do"); Frames(10); Shot("hs196_captain_stern");
        ReadTo(null);
        Hideouts("the submarine taken");

        // Team Aqua's hideout: the way in clear, an Electrode, Matt and the submarine sailing, and Lilycove after
        At("AquaHideout_1F", 13, 20, Direction.Up); Frames(20); Shot("hs197_aqua_hideout");
        At("AquaHideout_B1F", 16, 12, Direction.Up); Frames(20); Shot("hs198_aqua_hideout_panels");
        TalkTo("AquaHideout_B1F", "item_ball_3", 0, -1);
        ReadTo("was an Electrode"); Frames(10); Shot("hs198b_an_electrode");
        IntoBattle("an Electrode in the hideout");
        ToMainMenu(game.Battle); Win();
        ReadTo(null);
        At("AquaHideout_B2F", 24, 19, Direction.Left); Frames(20); Shot("hs199_matt");
        TalkTo("AquaHideout_B2F", "aqua_hideout_matt", 1, 0);
        IntoBattle("Matt's battle");
        ToMainMenu(game.Battle); Shot("hs19a_matts_battle");
        Win();
        ReadTo("slid out of its pen"); Frames(10); Shot("hs19b_the_submarine_sails");
        ReadTo(null);
        Hideouts("the submarine sailed");
        At("Hoenn", 552, 84, Direction.Right); game.EnterArea(); ReadTo(null); Frames(20); Shot("hs19c_the_channel_open");
        At("LittlerootTown_MaysHouse_2F", 3, 4, Direction.Up); Frames(20); Shot("hs19d_the_rival_at_home");
        Hideouts("the rival at home");

        // ---- Plan 21 · H8, part a: Mossdeep City (hs200 to hs229)
        void Mossdeep(string what) =>
            Console.WriteLine($"  {what}: mind badge {story.HasBadge("Hoenn", "Mind")}, mossdeep {story.Var("VAR_MOSSDEEP_CITY_STATE")}, "
                + $"space center {story.Var("VAR_MOSSDEEP_SPACE_CENTER_STATE")}, stairs {story.Var("VAR_MOSSDEEP_SPACE_CENTER_STAIR_GUARD_STATE")}, "
                + $"steven's house {story.Var("VAR_STEVENS_HOUSE_STATE")}, magma hidden {story.Has("FLAG_HIDE_MOSSDEEP_CITY_TEAM_MAGMA")}, "
                + $"dive {story.Has("FLAG_RECEIVED_HM_DIVE")}, mental herb {story.Has("FLAG_RECEIVED_MENTAL_HERB")}, "
                + $"on {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY})");
        void Where(string key)
        {
            var who = game.Map.NPCs.FirstOrDefault(n => n.Key == key);
            Console.WriteLine($"    {key}: {(who == null ? "gone" : $"({who.GridX}, {who.GridY}) facing {who.Facing}")}");
        }

        // The town: Scott, who goes on his way
        At("Hoenn", 668, 118, Direction.Up); game.EnterArea(); ReadTo(null); Frames(20); Shot("hs200_mossdeep");
        Mossdeep("in Mossdeep");
        TalkTo("Hoenn", "mossdeep_scott");
        ReadTo("something's off about this town"); Frames(10); Shot("hs201_scott");
        ReadTo(null); Frames(20); Shot("hs202_scott_gone");

        // The Gym: a switch turns the people on its arrows; Tate and Liza; the Mind Badge
        ComeThrough("MossdeepCity_Gym", 7, 32, Direction.Up); Frames(20); Shot("hs203_the_gym");
        At("MossdeepCity_Gym", 3, 31, Direction.Up); Frames(10);
        Where("psychic_m");
        Shot("hs204_before_the_yellow_switch");
        StepOnto("MossdeepCity_Gym", 3, 30, Direction.Up);
        Frames(8); Shot("hs205_the_arrows_turning");
        ReadTo(null); Frames(10); Shot("hs206_the_arrows_turned");
        Where("psychic_m");
        At("MossdeepCity_Gym", 23, 9, Direction.Up); Frames(20); Shot("hs207_tate_and_liza");
        TalkTo("MossdeepCity_Gym", "tate");
        ReadTo("We're twins"); Frames(10); Shot("hs208_the_twins");
        IntoBattle("Tate and Liza's battle");
        ToMainMenu(game.Battle); Shot("hs209_the_twins_double_battle");
        Win();
        ReadTo("received the Mind Badge"); Frames(10); Shot("hs20a_the_mind_badge");
        ReadTo(null);
        Mossdeep("the Mind Badge");

        // Team Magma before the Space Center, and in through its door
        At("Hoenn", 680, 121, Direction.Up); Frames(20); Shot("hs210_magma_before_the_space_center");
        StepOnto("Hoenn", 681, 122, Direction.Right);
        ReadTo("The fuel is in that building"); Frames(10); Shot("hs211_maxie_points");
        Box().Advance(); Frames(40); Shot("hs212_magma_filing_in");
        ReadTo(null); Frames(20); Shot("hs213_magma_gone_in");
        Mossdeep("Team Magma in the Space Center");

        // The ground floor held, the grunt on the stairs and the letter
        ComeThrough("MossdeepCity_SpaceCenter_1F", 7, 8, Direction.Up); Frames(20); Shot("hs214_space_center_held");
        TalkTo("MossdeepCity_SpaceCenter_1F", "space_center_1f_stair_grunt", 1, 0);
        IntoBattle("the grunt on the stairs");
        ToMainMenu(game.Battle); Win();
        ReadTo(null); Frames(20); Shot("hs215_the_stairs_clear");
        Where("space_center_1f_stair_grunt");

        // Upstairs: the three grunts, Steven, Maxie's plan and the battle beside Steven
        ComeThrough("MossdeepCity_SpaceCenter_2F", 13, 1, Direction.Down);
        ReadTo("three of us to one of you"); Frames(10); Shot("hs216_three_grunts");
        IntoBattle("the first of three grunts");
        ToMainMenu(game.Battle); Win();
        ReadTo(null); Frames(20); Shot("hs217_three_grunts_beaten");
        TalkTo("MossdeepCity_SpaceCenter_2F", "space_center_2f_steven", 1, 0);
        ReadTo("pour every drop"); Frames(10); Shot("hs218_maxies_plan");
        ReadTo(null);
        TalkTo("MossdeepCity_SpaceCenter_2F", "space_center_2f_steven", 1, 0);
        ReadTo("side by side"); Frames(10); Shot("hs219_steven_asks");
        IntoBattle("the battle beside Steven");
        ToMainMenu(game.Battle); Shot("hs21a_beside_steven");
        Win();
        ReadTo("might Team Aqua's wish"); Frames(10); Shot("hs21b_maxie_doubts");
        ReadTo("come and see me at home"); Frames(10); Shot("hs21c_steven_thanks");
        ReadTo(null); Frames(20); Shot("hs21d_space_center_saved");
        Mossdeep("the Space Center saved");

        // Steven's house: HM09, Dive
        ComeThrough("MossdeepCity_StevensHouse", 3, 7, Direction.Up);
        Frames(60); Shot("hs220_steven_comes_over");
        ReadTo("It's an HM"); Frames(10); Shot("hs221_hm_dive");
        ReadTo(null); Frames(20); Shot("hs222_stevens_house_after");
        Mossdeep("Dive given");

        // The Wingull's errand: from Fortree's boy to Mossdeep and home with the Mental Herb
        TalkTo("FortreeCity_House4", "boy_3");
        ReadTo(null);
        ComeThrough("MossdeepCity_House2", 3, 7, Direction.Up); Frames(20); Shot("hs223_the_wingull_in_mossdeep");
        TalkTo("MossdeepCity_House2", "mossdeep_house_wingull", 0, 1);
        Frames(30); Shot("hs224_the_wingull_flies_home");
        ReadTo(null);
        ComeThrough("FortreeCity_House4", 3, 6, Direction.Up);
        TalkTo("FortreeCity_House4", "boy_3");
        ReadTo("Thanks for keeping an eye on it"); Frames(10); Shot("hs225_the_mental_herb");
        ReadTo(null);
        Mossdeep("the Wingull home");

        // The treasure hunter's house on Route 124
        ComeThrough("Route124_DivingTreasureHuntersHouse", 3, 7, Direction.Up);
        TalkTo("Route124_DivingTreasureHuntersHouse", "man_5", -1, 0);
        ReadTo("diving treasure hunter"); Frames(10); Shot("hs226_the_treasure_hunter");
        ReadTo(null);

        // ---- Plan 21 · H8, part b: the Seafloor Cavern, Kyogre, Sootopolis and Wallace (hs230 to hs24x)
        void Titans(string what) =>
            Console.WriteLine($"  {what}: seafloor {story.Var("VAR_SEAFLOOR_CAVERN_STATE")}, route 128 {story.Var("VAR_ROUTE128_STATE")}, "
                + $"sootopolis {story.Var("VAR_SOOTOPOLIS_CITY_STATE")}, kyogre escaped {story.Has("FLAG_KYOGRE_ESCAPED_SEAFLOOR_CAVERN")}, "
                + $"weather taken {story.Has("FLAG_SYS_WEATHER_CTRL")}, clash seen {story.Has("FLAG_HOENN_SOOTOPOLIS_CLASH_SEEN")}, "
                + $"steven guides {story.Has("FLAG_STEVEN_GUIDES_TO_CAVE_OF_ORIGIN")}, wallace to sky pillar {story.Has("FLAG_WALLACE_GOES_TO_SKY_PILLAR")}, "
                + $"weather here {game.Map.WeatherAt(game.Player.GridX, game.Player.GridY)}, song {game.Map.BgmTrackAt(game.Player.GridX, game.Player.GridY)}, "
                + $"on {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY}), {game.Player.Mode}");

        // The cavern's mouth, come up into from the pool under the sea (Steven's gift hid its guard)
        story.Set("FLAG_HIDE_SEAFLOOR_CAVERN_ENTRANCE_AQUA_GRUNT");
        At("Underwater_SeafloorCavern", 6, 5, Direction.Up);
        game.Player.SetMode(TravelMode.Diving);
        game.ArriveOnMap(); Frames(20); Shot("hs230_the_pool_before_the_cavern");
        At("SeafloorCavern_Entrance", 10, 17, Direction.Up);
        game.Player.SetMode(TravelMode.Surfing);
        game.ArriveOnMap(); Frames(20); Shot("hs231_the_cavern_mouth");
        Titans("in the Seafloor Cavern");

        // Shelly among the boulders
        // (From behind her: on her west the grunt across the hall sees the player, and his walk up carried into Kyogre's hall)
        TalkTo("SeafloorCavern_Room3", "aqua_member_f", 1, 0);
        ReadTo("keep anybody who came this far busy"); Frames(10); Shot("hs232_shelly");
        IntoBattle("Shelly's battle");
        ToMainMenu(game.Battle); Win();
        ReadTo(null);

        // Kyogre's hall: Archie, his battle, the Red Orb, Kyogre waking and gone, Maxie
        ComeThrough("SeafloorCavern_Room9", 17, 44, Direction.Up); Frames(20); Shot("hs233_kyogre_asleep");
        StepOnto("SeafloorCavern_Room9", 17, 42, Direction.Up);
        ReadTo("So it was you all along"); Frames(10); Shot("hs234_archie");
        IntoBattle("Archie's battle");
        ToMainMenu(game.Battle); Shot("hs235_archies_battle");
        Win();
        ReadTo("shine all by itself"); Frames(10); Shot("hs236_the_red_orb");
        Box().Advance(); Frames(90); Shot("hs237_kyogre_wakes");
        ReadTo("Where has Kyogre gone"); Frames(10); Shot("hs238_kyogre_gone");
        ReadTo("What have you done, Archie"); Frames(10); Shot("hs239_maxie_comes");
        Titans("Kyogre woken");

        // Route 128's islet under the downpour: Archie and Maxie leave, Steven flies in and on
        ReadTo("Did I... Have I made some terrible mistake"); Frames(10); Shot("hs23a_route128_the_storm");
        ReadTo("Their power is beyond anything"); Frames(10); Shot("hs23b_maxie_and_archie");
        ReadTo("What is going on here"); Frames(10); Shot("hs23c_steven_flies_in");
        ReadTo(null); Frames(20); Shot("hs23d_route128_after");
        Titans("Route 128");

        // Sootopolis City, come up into its harbour: the titans clash, the weather breaks and turns
        At("SootopolisCity", 29, 53, Direction.Up);
        game.Player.SetMode(TravelMode.Surfing);
        game.ArriveOnMap();
        Frames(150); Shot("hs240_sootopolis_the_titans");
        Frames(150); Shot("hs241_the_clash");
        ReadTo(null); Frames(20); Shot("hs242_after_the_clash");
        Titans("the clash seen");
        Until(() => game.Map.WeatherAt(game.Player.GridX, game.Player.GridY) == FieldWeather.HeavyRain, "the downpour", 1200);
        Frames(30); Shot("hs243_the_downpour");
        Until(() => game.Map.WeatherAt(game.Player.GridX, game.Player.GridY) == FieldWeather.Drought, "the drought", 1200);
        Frames(30); Shot("hs244_the_drought");
        ShotCrop("hs244_the_drought_native", 640, 300, 640, 360, 2);

        // Steven leads the player to the Cave of Origin; Wallace below
        TalkTo("SootopolisCity", "sootopolis_steven");
        ReadTo("ready to be part of this"); Frames(10); Shot("hs245_steven_in_sootopolis");
        ReadTo("you'll find a man named Wallace"); Frames(10); Shot("hs246_at_the_cave");
        ReadTo(null); Frames(20); Shot("hs247_the_cave_of_origin");
        ComeThrough("CaveOfOrigin_B1F", 9, 15, Direction.Up); Frames(20);
        TalkTo("CaveOfOrigin_B1F", "cave_of_origin_wallace");
        ReadTo("My name is Wallace"); Frames(10); Shot("hs248_wallace");
        ReadUntil(() => engine.Choice.IsOpen, "Wallace's question"); Frames(20); Shot("hs249_where_is_rayquaza");
        ReadTo(null); Frames(20); Shot("hs24a_wallace_gone");
        Titans("Wallace gone to Sky Pillar");

        // Back in the city: the old man steps aside for a player from the south, Steven by the cave's mouth
        ComeThrough("SootopolisCity", 31, 20, Direction.Up); Frames(10);
        TalkTo("SootopolisCity", "sootopolis_expert");
        ReadTo(null); Frames(20); Shot("hs24b_the_way_in_open");

        // ---- Plan 21 · H8, part c: Sky Pillar, Rayquaza, Sootopolis calmed, Waterfall and Juan (hs250 to hs27x)
        void Pillar(string what) =>
            Console.WriteLine($"  {what}: sootopolis {story.Var("VAR_SOOTOPOLIS_CITY_STATE")}, sky pillar {story.Var("VAR_SKY_PILLAR_STATE")}, "
                + $"cry done {story.Var("VAR_SKY_PILLAR_RAYQUAZA_CRY_DONE")}, weather taken {story.Has("FLAG_SYS_WEATHER_CTRL")}, "
                + $"clash seen {story.Has("FLAG_HOENN_SOOTOPOLIS_CLASH_SEEN")}, titans in the city {story.Has("FLAG_LEGENDARIES_IN_SOOTOPOLIS")}, "
                + $"leaders gone {story.Has("FLAG_SOOTOPOLIS_ARCHIE_MAXIE_LEAVE")}, waterfall {story.Has("FLAG_RECEIVED_HM_WATERFALL")}, "
                + $"wallace {story.Var("VAR_SOOTOPOLIS_WALLACE_STATE")}, rain badge {story.HasBadge(RegionDatabase.Hoenn, "Rain")}, "
                + $"weather here {game.Map.WeatherAt(game.Player.GridX, game.Player.GridY)}, song {game.Map.BgmTrackAt(game.Player.GridX, game.Player.GridY)}, "
                + $"on {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY}), {game.Player.Mode}");
        // One step a way, taken as the player takes it, and the field let settle
        void Stride(Direction way)
        {
            var from = (game.Player.GridX, game.Player.GridY, game.Map.Name);
            engine.Steering = (way, false);
            for (int i = 0; i < 60 && (game.Player.GridX, game.Player.GridY, game.Map.Name) == from && !engine.ScriptRunning; i++) Frames(1);
            engine.Steering = null;
            for (int i = 0; i < 60 && game.Player.IsMoving; i++) Frames(1);
            Frames(4);
        }

        // Route 131's shore before Sky Pillar's cave, and up through it to the island: Wallace comes over, the quake
        At("Hoenn", 536, 268, Direction.Up); game.EnterArea(); Frames(20); Shot("hs250_route131_sky_pillar");
        ComeThrough("SkyPillar_Outside", 17, 14, Direction.Down);
        ReadTo("I was in such a hurry that I left you behind"); Frames(10); Shot("hs251_wallace_at_sky_pillar");
        ReadTo("An earthquake! We haven't a moment to spare!"); Frames(10); Shot("hs252_the_earthquake");
        ReadTo("Even here, the weather has gone wrong"); Frames(10); Shot("hs253_the_weather_spreads");
        ReadTo(null); Frames(20); Shot("hs254_wallace_gone_back");
        Pillar("Wallace at Sky Pillar");
        At("SkyPillar_Outside", 14, 6, Direction.Up); Frames(10); Shot("hs255_the_door_open");

        // Inside: the first floor, and the fourth's cracked floor giving way into the third's middle
        ComeThrough("SkyPillar_1F", 6, 12, Direction.Up); Frames(10); Shot("hs256_sky_pillar_1f");
        ComeThrough("SkyPillar_4F", 5, 4, Direction.Right); Frames(10); Shot("hs257_the_cracked_floor");
        Stride(Direction.Right);
        Until(() => game.Map.Name == "SkyPillar_3F" && State() == GameState.Overworld, "the fall to the third floor", 300);
        Frames(20); Shot("hs258_fallen_to_the_third_floor");
        Pillar("through Sky Pillar's floor");

        // The top: Rayquaza asleep, and the step before it
        ComeThrough("SkyPillar_Top", 14, 11, Direction.Up); Frames(20); Shot("hs259_rayquaza_asleep");
        StepOnto("SkyPillar_Top", 14, 9, Direction.Up);
        Frames(90); Shot("hs25a_rayquaza_stirs");
        ReadTo("The awakened Rayquaza flew off..."); Frames(10); Shot("hs25b_rayquaza_flies_off");
        ReadTo(null); Frames(20); Shot("hs25c_the_top_empty");
        Pillar("Rayquaza woken");

        // Sootopolis: Rayquaza comes down, the titans go, the storm breaks
        ComeThrough("SootopolisCity", 43, 32, Direction.Up);
        Frames(150); Shot("hs260_rayquaza_over_sootopolis");
        ReadTo("Rayquaza's roar rang round the crater"); Frames(10); Shot("hs261_the_storm_breaks");
        ShotCrop("hs261_the_storm_breaks_native", 640, 300, 640, 360, 2);
        ReadTo("Rayquaza flew off into the clear sky..."); Frames(10); Shot("hs262_rayquaza_gone");
        ReadTo(null); Frames(30); Shot("hs263_sootopolis_calm");
        Pillar("Rayquaza come");

        // By the Gym: Maxie and Archie have their say and go; Wallace gives Waterfall and steps aside
        TalkTo("SootopolisCity", "sootopolis_maxie");
        ReadTo("All our schemes"); Frames(10); Shot("hs264_maxie");
        ReadTo(null);
        TalkTo("SootopolisCity", "sootopolis_archie");
        ReadTo("What we tried to do meant nothing"); Frames(10); Shot("hs265_archie");
        ReadTo(null); Frames(20); Shot("hs266_the_leaders_gone");
        TalkTo("SootopolisCity", "sootopolis_wallace");
        ReadTo("Please, take this as a token of my gratitude."); Frames(10); Shot("hs267_wallaces_thanks");
        ReadTo("That holds Waterfall"); Frames(10); Shot("hs268_hm07_waterfall");
        ReadTo(null); Frames(20); Shot("hs269_the_gym_door_open");
        Pillar("Waterfall given");

        // Juan's Gym: the first field of ice crossed tile by tile, its stairs coming clear, and a tile stepped on twice
        ComeThrough("SootopolisCity_Gym_1F", 8, 24, Direction.Up); Frames(20); Shot("hs270_the_gym_of_ice");
        At("SootopolisCity_Gym_1F", 8, 20, Direction.Up); Frames(4);
        foreach (var way in new[] { Direction.Up, Direction.Left, Direction.Up, Direction.Right, Direction.Right, Direction.Up, Direction.Left })
            Stride(way);
        Frames(60); Shot("hs271_the_first_field_crossed");
        if (game.Map.Puzzle is SootopolisIce ice)
            Console.WriteLine($"  the ice: count {ice.Count}, flights open {ice.FlightsOpen}, cracked {ice.Cracked.Count}");
        Stride(Direction.Down);
        Until(() => game.Map.Name == SootopolisIce.Basement && State() == GameState.Overworld, "the fall to the basement", 300);
        Frames(20); Shot("hs272_fallen_through_the_ice");

        // Juan, his battle and the Rain Badge
        ComeThrough("SootopolisCity_Gym_1F", 8, 24, Direction.Up); Frames(4);
        TalkTo("SootopolisCity_Gym_1F", "juan");
        ReadTo("It was I who taught Wallace everything he knows."); Frames(10); Shot("hs273_juan");
        IntoBattle("Juan's battle");
        ToMainMenu(game.Battle); Shot("hs274_juans_battle");
        Win();
        ReadTo("received the Rain Badge from Juan!"); Frames(10); Shot("hs275_the_rain_badge");
        ReadTo(null); Frames(20); Shot("hs276_juan_after");
        Pillar("the Rain Badge");

        // ---- Plan 21 · H8, part d: Shoal Cave's salt, shells, Shell Bell and Focus Band, and Pacifidlog's three (hs280 to hs28b)
        int Held(string item) => game.Bag.GetQuantity(ItemDatabase.Get(item)!);
        void Shoal(string what) =>
            Console.WriteLine($"  {what}: tide high {story.Has(ShoalTide.Flag)}, salt {Held("Shoal Salt")}, shells {Held("Shoal Shell")}, "
                + $"shell bell {Held("Shell Bell")}, focus band {story.Has("FLAG_RECEIVED_FOCUS_BAND")}, return {Held("TM27")}, frustration {Held("TM21")}, "
                + $"pacifidlog tm today {story.Has("FLAG_DAILY_PACIFIDLOG_RECEIVED_TM")}, trade {story.Has("FLAG_PACIFIDLOG_NPC_TRADE_COMPLETED")}, "
                + $"explosion taught {story.Has("FLAG_MOVE_TUTOR_TAUGHT_EXPLOSION")}, team {string.Join(", ", game.Party.Members.Select(p => p.DisplayName))}");
        void Lead(string species)
        {
            var first = game.Party.Members.First(p => p.Species.Name == species);
            game.Party.Members.Remove(first);
            game.Party.Members.Insert(0, first);
        }

        // Shoal Cave at low tide: a pile of salt in the inner room, then the old man's Shell Bell of four and four
        story.Unset(ShoalTide.Flag);
        At("ShoalCave_LowTideInnerRoom", 31, 9, Direction.Up); Frames(20);
        game.Interact();
        ReadTo("Shoal Salt"); Frames(10); Shot("hs280_shoal_salt_found");
        ReadTo(null);
        Shoal("a pile of salt");
        game.Bag.AddItem(ItemDatabase.Get("Shoal Salt")!, 4 - Held("Shoal Salt"));
        game.Bag.AddItem(ItemDatabase.Get("Shoal Shell")!, 4);
        TalkTo("ShoalCave_LowTideEntranceRoom", "expert_m");
        ReadTo("Shall I make them into a Shell Bell"); Frames(10); Shot("hs281_the_shell_bell_offer");
        ReadTo("Let a Pokémon hold the Shell Bell"); Frames(10); Shot("hs282_the_shell_bell");
        ReadTo(null);
        Shoal("the Shell Bell");
        TalkTo("ShoalCave_LowTideLowerRoom", "black_belt");
        ReadTo("take this Focus Band"); Frames(10); Shot("hs283_the_focus_band");
        ReadTo(null);
        Shoal("the Focus Band");

        // Pacifidlog: the Fan Club chairman's brother reads a loving lead and gives Return, once today
        game.Party.Members[0].Friendship = 255;
        TalkTo("PacifidlogTown_House2", "gentleman");
        ReadTo("Let me have a look at the Pokémon"); Frames(10); Shot("hs284_the_fan_club_brother");
        ReadTo("simply adores you"); Frames(10); Shot("hs285_return_for_a_loving_pokemon");
        ReadTo(null);
        TalkTo("PacifidlogTown_House2", "gentleman");
        ReadTo("Come back tomorrow"); Frames(10); Shot("hs286_come_back_tomorrow");
        ReadTo(null);
        Shoal("the Fan Club's brother");

        // The man of the third house: his Horsea, Seasor, for a Bagon
        string lead = game.Party.Members[0].Species.Name;
        game.Party.Members.Insert(0, new Pokemon(PokemonDatabase.Get("Bagon")!, 20));
        TalkTo("PacifidlogTown_House3", "man_3");
        ReadTo("what I've always wanted is a Bagon"); Frames(10); Shot("hs287_the_bagon_trade_offer");
        ReadTo("Which Pokémon will you trade");
        Box().Advance(); Frames(2);
        ChooseFirst();
        ReadTo("A Bagon, at last!"); Frames(10); Shot("hs288_seasor");
        ReadTo(null);
        Shoal("the Pacifidlog trade");

        // The man in the Pokémon Center who teaches Explosion
        game.Party.Members.Insert(0, new Pokemon(PokemonDatabase.Get("Geodude")!, 20));
        TalkTo("PacifidlogTown_PokemonCenter_1F", "man_5");
        ReadTo("Shall I teach one of your Pokémon Explosion"); Frames(10); Shot("hs289_the_explosion_tutor");
        ReadTo("Which one");
        Box().Advance(); Frames(2);
        ChooseFirst();
        ReadTo("learned"); Frames(10); Shot("hs28a_explosion_learned");
        ReadTo(null);
        Lead(lead);
        // (the Geodude was only lent for the lesson: left in, the team stood at seven)
        game.Party.Members.RemoveAll(p => p.Species.Name == "Geodude");
        Frames(10); Shot("hs28b_pacifidlog_center_after");
        Shoal("the Explosion tutor");
    }
}
