partial class Harness
{
    /// <summary>
    /// Kanto's first chapter played by its own scripts (plan 19 · K3), the end of the <c>kanto</c> mode: Oak stopping the
    /// player in the grass, his lab and the rival, the three Poké Balls, the first battle, Oak's Parcel at Viridian's Mart,
    /// the Pokédex, the rival on Route 22, Pewter City's Gym guide, Brock's hall, his battle and the Boulder Badge, and the
    /// Running Shoes (<c>ks01</c> to <c>ks14</c>), through the game's own steps, <c>TryInteract</c> and arrivals. It wins
    /// every battle a scene starts and prints what each scene left behind.
    /// </summary>
    private void KantoScenes()
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
        bool keepLead = false;
        void Win()
        {
            var fight = game.Battle;
            for (int guard = 0; guard < 300 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    foreach (var foe in fight.EnemyParty?.Members ?? new List<Pokemon>()) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    // From the second chapter on the lead never tires: the foes move first more often there
                    if (keepLead && fight.PlayerPokemon is { } tired)
                    {
                        tired.CurrentHP = tired.MaxHP;
                        // and never runs out of PP: the third chapter's battles come one after another
                        foreach (var move in tired.Moves) move.CurrentPP = move.MaxPP;
                    }
                    fight.SelectMainMenuOption(0);
                    // The first move that hurts: a starter's first is often Growl or Tail Whip
                    var lead = fight.PlayerPokemon;
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
            Console.WriteLine($"  {what}: lab {story.Var("VAR_MAP_SCENE_PALLET_TOWN_PROFESSOR_OAKS_LAB")}, mart {story.Var("VAR_MAP_SCENE_VIRIDIAN_CITY_MART")}, "
                + $"old man {story.Var("VAR_MAP_SCENE_VIRIDIAN_CITY_OLD_MAN")}, route 22 {story.Var("VAR_MAP_SCENE_ROUTE22")}, pewter {story.Var("VAR_MAP_SCENE_PEWTER_CITY")}, "
                + $"team {string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} {p.Level}"))}, "
                + $"Pokédex {story.Has(StoryState.PokedexFlag)}, shoes {story.Has(StoryState.RunningShoesFlag)}, Boulder Badge {story.HasBadge("Kanto", "Boulder")}, "
                + $"bag {string.Join(", ", game.Bag.AllItems.Select(s => $"{s.Name} x{s.Quantity}"))}");

        // A new game of Kanto, from its first step
        engine.NewGameRegion = "Kanto";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        Frames(30);
        Left("the bedroom");

        // ---- Oak in the grass
        StepOnto("Kanto", 72, 261, Direction.Up);
        ReadTo("Hold it!"); Frames(10); Shot("ks01_oak_calls_out");
        ReadTo("Phew, I caught you"); Frames(20); Shot("ks02_oak_stops_you");
        // ---- the lab: Oak walks in, the rival has been waiting
        // The warp's fade, and the lab's own arrival after it
        for (int i = 0; i < 1200 && game.Map.Name != "PalletTown_ProfessorOaksLab"; i++)
        {
            if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
            else Frames(1);
        }
        Until(() => engine.ScriptRunning || Box().IsActive, "the lab's arrival", 600);
        ReadTo("Gramps! Finally!"); Frames(20); Shot("ks03_the_lab");
        ReadTo(null);
        Left("in the lab");

        // ---- the three Poké Balls
        TalkTo("PalletTown_ProfessorOaksLab", "bulbasaur_ball");
        Until(() => State() == GameState.StarterSelect, "the choice of three", 600);
        Frames(40); Shot("ks04_the_three_poke_balls");
        var choice = game.StarterSelect;
        choice.SelectedIndex = 1;
        choice.Confirm();
        choice.Move(1);
        Frames(20); Shot("ks05_charmander_asked");
        InputManager.Feed(ActionFrame.Press(GameAction.Confirm)); Frames(1); InputManager.Feed(null);
        ReadTo("Then I'll take this one!"); Frames(10); Shot("ks06_the_rival_takes_his");
        ReadTo(null);
        Left("a Pokémon each");

        // ---- the rival's challenge on the way out, the first battle
        StepOnto("PalletTown_ProfessorOaksLab", 6, 8, Direction.Down);
        ReadTo("Wait up,"); Frames(10); Shot("ks07_the_rivals_challenge");
        IntoBattle("the first battle");
        ToMainMenu(game.Battle); Shot("ks08_the_first_battle");
        Win();
        ReadTo(null);
        Left("the first battle");

        // ---- Oak's Parcel at Viridian's Mart, and the Pokédex
        ComeThrough("ViridianCity_Mart", 4, 6, Direction.Up);
        ReadTo("You're from Pallet Town"); Frames(10); Shot("ks09_the_parcel");
        ReadTo(null);
        TalkTo("PalletTown_ProfessorOaksLab", "oaks_lab_prof_oak");
        ReadTo("This is the Pokédex"); Frames(20); Shot("ks10_the_pokedex");
        ReadTo(null);
        Left("the Pokédex");

        // ---- the rival on Route 22
        StepOnto("Kanto", 33, 195, Direction.Left);
        ReadTo("Heading for the Pokémon League"); Frames(10); Shot("ks11_route22_rival");
        IntoBattle("the rival's battle on Route 22");
        Win();
        ReadTo(null);

        // ---- Pewter City: the Gym's guide on the road east, Brock and his Badge
        StepOnto("Kanto", 90, 82, Direction.Right);
        ReadTo("Brock's always looking"); Frames(10); Shot("ks12_the_gym_guide");
        ReadTo(null);
        Console.WriteLine($"  the guide left the player at ({game.Player.GridX}, {game.Player.GridY})");
        At("PewterCity_Gym", 6, 13, Direction.Up); Frames(30); Shot("ks13_brocks_hall");
        TalkTo("PewterCity_Gym", "brock");
        ReadTo("So you're the new challenger"); Frames(10); Shot("ks14_brock");
        IntoBattle("Brock's battle");
        ToMainMenu(game.Battle); Shot("ks15_brocks_battle");
        Win();
        ReadTo("Boulder Badge from Brock"); Frames(10); Shot("ks16_the_boulder_badge");
        ReadTo(null);
        StepOnto("Kanto", 94, 82, Direction.Right);
        ReadTo("Your mother sent these"); Frames(10); Shot("ks17_the_running_shoes");
        ReadTo(null);
        Left("the chapter's end");

        // ---- Plan 19 · K4: Mt. Moon to Bill
        keepLead = true;
        void LeftTwo(string what) =>
            Console.WriteLine($"  {what}: Mt. Moon B2F {story.Var("VAR_MAP_SCENE_MT_MOON_B2F")}, fossil {(story.Has("FLAG_GOT_DOME_FOSSIL") ? "Dome" : story.Has("FLAG_GOT_HELIX_FOSSIL") ? "Helix" : "none")}, "
                + $"Cerulean rival {story.Var("VAR_MAP_SCENE_CERULEAN_CITY_RIVAL")}, Nugget Bridge {story.Var("VAR_MAP_SCENE_ROUTE24")}, Bill helped {story.Has("FLAG_HELPED_BILL_IN_SEA_COTTAGE")}, "
                + $"S.S. Ticket {story.Has("FLAG_GOT_SS_TICKET")}, Cascade Badge {story.HasBadge("Kanto", "Cascade")}, "
                + $"bag {string.Join(", ", game.Bag.AllItems.Select(s => $"{s.Name} x{s.Quantity}"))}");

        // ---- Miguel and the fossils, deep in Mt. Moon
        StepOnto("MtMoon_B2F", 14, 11, Direction.Left);
        ReadTo("I found these fossils first"); Frames(10); Shot("ks18_miguel");
        IntoBattle("Miguel's battle");
        Win();
        ReadTo(null);
        TalkTo("MtMoon_B2F", "dome_fossil");
        ReadTo("Then the Helix Fossil is mine"); Frames(10); Shot("ks19_one_fossil_each");
        ReadTo(null);
        LeftTwo("the fossils");

        // ---- the rival at the foot of Nugget Bridge
        StepOnto("Kanto", 287, 46, Direction.Up);
        ReadTo("Well, look who finally made it"); Frames(10); Shot("ks20_cerulean_rival");
        IntoBattle("the rival's battle in Cerulean City");
        Win();
        ReadTo(null);

        // ---- Nugget Bridge's prize
        StepOnto("Kanto", 287, 15, Direction.Up);
        ReadTo("Here's your prize"); Frames(10); Shot("ks21_nugget_bridge_prize");
        IntoBattle("the recruiter's battle");
        Win();
        ReadTo(null);
        LeftTwo("the bridge");

        // ---- Bill in a muddle, the cell separator and the S.S. Ticket
        TalkTo("Route25_SeaCottage", "bill_clefairy");
        ReadTo("I'm Bill, the Pokémaniac"); Frames(10); Shot("ks22_bill_the_clefairy");
        ReadTo(null);
        At("Route25_SeaCottage", 4, 6, Direction.Up);
        game.Interact();
        ReadTo("started the teleporter's cell separator"); Frames(10); Shot("ks23_the_cell_separator");
        ReadTo(null); Frames(30);
        TalkTo("Route25_SeaCottage", "bill_human");
        ReadTo("It's a ticket for the S.S. Anne"); Frames(10); Shot("ks24_the_ss_ticket");
        ReadTo(null);

        // ---- Misty and the Cascade Badge (her Starmie fells the harness's Charmander at full health in one blow: a
        // Venusaur leads for her)
        game.Party.Members.Insert(0, new Pokemon(PokemonDatabase.Get("Venusaur")!, 50));
        At("CeruleanCity_Gym", 8, 16, Direction.Up); Frames(30); Shot("ks25_mistys_pool");
        TalkTo("CeruleanCity_Gym", "misty");
        ReadTo("Hi, you're a new face"); Frames(10); Shot("ks26_misty");
        IntoBattle("Misty's battle");
        ToMainMenu(game.Battle); Shot("ks27_mistys_battle");
        Win();
        ReadTo("Cascade Badge from Misty"); Frames(10); Shot("ks28_the_cascade_badge");
        ReadTo(null);
        LeftTwo("the chapter's first half");

        // ---- Kanto's badges on the Trainer Card, all eight won for the picture (style guide, "Badges")
        int had = story.BadgeMask("Kanto");
        story.SetBadges("Kanto", 0xFF);
        game.State = GameState.Overworld;
        game.ChooseFromStartMenu(StartMenuChoice.Trainer);
        Frames(40); Shot("ks29_kantos_badges");
        game.TrainerCard.Close();
        story.SetBadges("Kanto", had);
        Frames(10);

        // ---- Plan 19 · K4, part b: Vermilion City and the S.S. Anne
        void LeftThree(string what) =>
            Console.WriteLine($"  {what}: voucher {story.Has("FLAG_GOT_BIKE_VOUCHER")}, bicycle {story.Has("FLAG_GOT_BICYCLE")}, "
                + $"ticket check {story.Var("VAR_VERMILION_CITY_TICKET_CHECK_TRIGGER")}, ship's rival {story.Var("VAR_MAP_SCENE_S_S_ANNE_2F_CORRIDOR")}, "
                + $"HM01 {story.Has("FLAG_GOT_HM01")}, Vermilion {story.Var("VAR_MAP_SCENE_VERMILION_CITY")}, "
                + $"switches {story.Var("VAR_VERMILION_GYM_SWITCH_1")}/{story.Var("VAR_VERMILION_GYM_SWITCH_2")} first {story.Has("FLAG_MAP_LOCAL_VERMILION_GYM_FIRST_SWITCH")} both {story.Has("FLAG_FOUND_BOTH_VERMILION_GYM_SWITCHES")}, "
                + $"Thunder Badge {story.HasBadge("Kanto", "Thunder")}, aide hidden {story.Has("FLAG_HIDE_VERMILION_CITY_OAKS_AIDE")}, "
                + $"bag {string.Join(", ", game.Bag.AllItems.Select(s => $"{s.Name} x{s.Quantity}"))}");

        // ---- the Fan Club's chairman and his Bike Voucher, and the Bicycle for it
        TalkTo("VermilionCity_PokemonFanClub", "gentleman");
        ReadTo("My favourite is my Rapidash"); Frames(10); Shot("ks30_the_chairman");
        ReadTo(null);
        TalkTo("CeruleanCity_BikeShop", "man");
        ReadTo("That's a Bike Voucher"); Frames(10); Shot("ks31_the_bike_voucher");
        ReadTo(null);
        At("Kanto", 292, 104, Direction.Down);
        game.Player.SetCycling(true);
        Frames(30); Shot("ks32_on_the_bicycle");
        game.Player.SetCycling(false);
        LeftThree("the Bicycle");

        // ---- the pier: the sailor asks for the ticket
        StepOnto("Kanto", 286, 233, Direction.Down);
        ReadTo("May I see your ticket"); Frames(10); Shot("ks33_the_ticket_check");
        ReadTo(null);

        // ---- the rival on the S.S. Anne
        StepOnto("SSAnne_2F_Corridor", 31, 6, Direction.Up);
        ReadTo("Fancy running into you"); Frames(10); Shot("ks34_the_rival_on_the_ship");
        IntoBattle("the rival's battle on the S.S. Anne");
        Win();
        ReadTo(null);

        // ---- the seasick captain and HM01
        TalkTo("SSAnne_CaptainsOffice", "ss_anne_captain");
        ReadTo("rubbed the captain's back"); Frames(10); Shot("ks35_the_seasick_captain");
        ReadTo("That's Cut"); Frames(10); Shot("ks36_hm01_cut");
        ReadTo(null);
        LeftThree("the captain");

        // ---- off the ship, and she sails
        ComeThrough("SSAnne_Exterior", 32, 13, Direction.Down);
        ReadTo("sounded her horn"); Frames(10); Shot("ks37_the_ship_sails");
        for (int i = 0; i < 1200 && game.Map.Name != "Kanto"; i++)
        {
            if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
            else Frames(1);
        }
        ReadTo(null); Frames(30);
        Console.WriteLine($"  off the ship at ({game.Player.GridX}, {game.Player.GridY}) on {game.Map.Name}");
        Shot("ks38_the_ship_has_sailed");

        // ---- Cut, and the tree before Lt. Surge's Gym
        var lead = game.Party.Members[0];
        if (!lead.Moves.Any(m => m.Data.Name == "Cut"))
        {
            if (lead.Moves.Count >= 4) lead.Moves.RemoveAt(3);
            lead.Moves.Add(new Move(MoveDatabase.Get("Cut")!));
        }
        var vermilion = MapDatabase.Get("Kanto");
        var tree = vermilion.Everyone.First(n => n.Obstacle == PropType.CutTree && (n.GridX, n.GridY) == (283, 224));
        At("Kanto", 283, 225, Direction.Up); Frames(10);
        game.Interact();
        Until(() => engine.Choice.IsOpen, "the tree's question"); Frames(20); Shot("ks39_cut_asks");
        engine.Choice.Confirm(); Frames(4);
        ReadTo(null); Frames(30);
        Shot("ks40_the_tree_cut");
        Console.WriteLine($"  the tree before the Gym: gone {!vermilion.NPCs.Contains(tree)}");

        // ---- Lt. Surge's bins: the first switch, the second beside it, the gates down
        ComeThrough("VermilionCity_Gym", 5, 18, Direction.Up);
        Frames(20); Shot("ks41_the_electric_gates");
        void Bin(int n)
        {
            int x = 1 + 2 * ((n - 1) % 5), z = 10 + 2 * ((n - 1) / 5);
            At("VermilionCity_Gym", x, z + 1, Direction.Up); Frames(4);
            game.Interact();
        }
        LeftThree("the Gym");
        Bin(story.Var("VAR_VERMILION_GYM_SWITCH_1"));
        ReadTo("The first electric lock opened"); Frames(10); Shot("ks42_the_first_switch");
        ReadTo(null);
        Bin(story.Var("VAR_VERMILION_GYM_SWITCH_2"));
        ReadTo("the gates are down"); Frames(10); Shot("ks43_the_second_switch");
        ReadTo(null);
        At("VermilionCity_Gym", 5, 9, Direction.Up); Frames(20); Shot("ks44_the_gates_down");
        TalkTo("VermilionCity_Gym", "lt_surge");
        ReadTo("electric Pokémon saved me"); Frames(10); Shot("ks45_lt_surge");
        IntoBattle("Lt. Surge's battle");
        ToMainMenu(game.Battle); Shot("ks46_lt_surges_battle");
        Win();
        ReadTo("Thunder Badge from Lt. Surge"); Frames(10); Shot("ks47_the_thunder_badge");
        ReadTo(null);
        LeftThree("the chapter's end");

        // ---- Plan 19 · K5, part a: Rock Tunnel dark and lit by Flash
        ComeThrough("RockTunnel_1F", 17, 4, Direction.Down);
        Frames(20); Shot("ks48_rock_tunnel_dark");
        story.Set("FLAG_FLASH_ACTIVE");
        Frames(20); Shot("ks49_rock_tunnel_lit");
        Console.WriteLine($"  Rock Tunnel: dark {game.Map.IsDark}, lit {game.Map.Lit}");

        // ---- the rival in the Pokémon Tower, from the step east of him
        StepOnto("PokemonTower_2F", 17, 5, Direction.Left);
        ReadTo("Your Pokémon don't look dead"); Frames(10); Shot("ks50_the_rival_in_the_tower");
        IntoBattle("the rival's battle in the tower");
        ToMainMenu(game.Battle); Shot("ks51_the_rivals_battle_in_the_tower");
        Win();
        ReadTo("Smell you later"); Frames(10); Shot("ks52_the_rival_leaves");
        ReadTo(null); Frames(30);
        Console.WriteLine($"  the tower's rival: scene {story.Var("VAR_MAP_SCENE_POKEMON_TOWER_2F")}, hidden {story.Has("FLAG_HIDE_TOWER_RIVAL")}");

        // ---- the purified ground of the fifth floor, and the spirit on the sixth
        game.Party.Members[0].CurrentHP = 1;
        StepOnto("PokemonTower_5F", 11, 8, Direction.Down);
        ReadTo("fully healed"); Frames(10); Shot("ks53_the_purified_ground");
        ReadTo(null);
        Console.WriteLine($"  purified: lead's HP {game.Party.Members[0].CurrentHP}/{game.Party.Members[0].MaxHP}");
        StepOnto("PokemonTower_6F", 11, 15, Direction.Left);
        ReadTo("Be... gone"); Frames(10); Shot("ks54_the_ghost");
        ReadTo(null); Frames(20); Shot("ks55_pushed_back");
        Console.WriteLine($"  the ghost: the player at ({game.Player.GridX}, {game.Player.GridY}), scene {story.Var("VAR_MAP_SCENE_POKEMON_TOWER_6F")}");

        // ---- Saffron City's gate, and Route 4's Black Belt
        StepOnto("Route8_WestEntrance", 6, 5, Direction.Left);
        ReadTo("too thirsty"); Frames(10); Shot("ks56_the_thirsty_guard");
        ReadTo(null); Frames(20);
        Console.WriteLine($"  the gate: the player at ({game.Player.GridX}, {game.Player.GridY})");
        var kanto = MapDatabase.Get("Kanto");
        var black = kanto.NPCs.First(n => n.Key == "black_belt" && kanto.AreaAt(n.GridX, n.GridY)?.Key == "route_4");
        At("Kanto", black.GridX, black.GridY + 1, Direction.Up); Frames(4);
        game.Interact();
        Until(() => engine.Choice.IsOpen, "the Black Belt's question"); Frames(20); Shot("ks57_mega_punch_asked");
        // No, for now: yes opens the team to choose from, which this mode doesn't drive
        engine.Choice.Move(1); Frames(4); engine.Choice.Confirm(); Frames(4);
        ReadTo(null);
        Console.WriteLine($"  Mega Punch: taught {story.Has("FLAG_TUTOR_MEGA_PUNCH")}");

        // ---- Plan 19 · K5, part b: the Tea, Erika, the Game Corner's poster, the Rocket Hideout and Giovanni
        keepLead = true;
        TalkTo("CeladonCity_Condominiums_1F", "old_woman", 1, 0);
        ReadTo("have some of my tea"); Frames(10); Shot("ks58_the_tea");
        ReadTo(null);
        TalkTo("CeladonCity_Gym", "erika");
        ReadTo("I'm Erika"); Frames(10); Shot("ks59_erika");
        IntoBattle("Erika's battle");
        ToMainMenu(game.Battle); Shot("ks60_erikas_battle");
        Win();
        ReadTo("Rainbow Badge from Erika"); Frames(10); Shot("ks61_the_rainbow_badge");
        ReadTo(null);
        TalkTo("CeladonCity_GameCorner", "game_corner_grunt");
        IntoBattle("the Game Corner's grunt");
        Win();
        ReadTo(null);
        At("CeladonCity_GameCorner", 11, 2, Direction.Up); Frames(4);
        game.Interact();
        ReadTo("switch behind the poster"); Frames(10); Shot("ks62_the_posters_switch");
        ReadTo("hidden staircase"); Frames(10); Shot("ks63_the_stairs_open");
        ReadTo(null);
        Console.WriteLine($"  the poster: opened {story.Has("FLAG_OPENED_ROCKET_HIDEOUT")}, stairs shut {game.Map.Warps.Single(w => w.TargetMap == "RocketHideout_B1F").Shut}");
        story.Set("FLAG_ROCKET_HIDEOUT_B4F_BARRIER_OPEN");
        TalkTo("RocketHideout_B4F", "hideout_giovanni");
        ReadTo("I'm Giovanni"); Frames(10); Shot("ks64_giovanni");
        IntoBattle("Giovanni's battle");
        ToMainMenu(game.Battle); Shot("ks65_giovannis_battle");
        Win();
        ReadTo(null); Frames(20); Shot("ks66_the_silph_scope_left");
        Console.WriteLine($"  Giovanni: beaten {story.Has("FLAG_DEFEATED_ROCKET_HIDEOUT_GIOVANNI")}, Celadon's Rockets gone {story.Has("FLAG_HIDE_CELADON_ROCKETS")}");

        // ---- the ghost seen for Marowak, Mr. Fuji, the Poké Flute and a Snorlax woken
        game.Bag.AddItem(ItemDatabase.Get("Silph Scope")!, 1);
        StepOnto("PokemonTower_6F", 11, 15, Direction.Left);
        ReadTo("for what it is"); Frames(10); Shot("ks67_the_scope_shows_marowak");
        IntoBattle("the ghost's battle");
        ToMainMenu(game.Battle); Shot("ks68_marowaks_battle");
        Win();
        ReadTo("calmed"); Frames(10); Shot("ks69_the_spirit_at_rest");
        ReadTo(null);
        TalkTo("PokemonTower_7F", "mr_fuji");
        ReadTo("a long way to find an old man"); Frames(10); Shot("ks70_mr_fuji");
        ReadTo(null); Frames(30);
        TalkTo("LavenderTown_VolunteerPokemonHouse", "mr_fuji");
        ReadTo("Poké Flute"); Frames(10); Shot("ks71_the_poke_flute");
        ReadTo(null);
        At("Kanto", 398, 221, Direction.Up); Frames(4);
        game.Interact();
        ReadTo("played the Poké Flute"); Frames(10); Shot("ks72_the_flute_played");
        ReadTo("woke up"); Frames(10); Shot("ks73_snorlax_wakes");
        IntoBattle("the Snorlax's battle");
        Win();
        ReadTo(null);
        Console.WriteLine($"  Snorlax of Route 12: gone {story.Has("FLAG_HIDE_ROUTE_12_SNORLAX")}");

        // ---- Fly's map of Kanto
        At("Kanto", 288, 150, Direction.Down); Frames(4);
        game.OpenTownMap(TownMapMode.Fly);
        Frames(30); Shot("ks74_fly_over_kanto");
        game.TownMapScreen.Cancel(); Frames(4);
        game.State = GameState.Overworld;

        // ---- Silph Co.: a door and the Card Key, the rival, Giovanni, Sabrina
        game.Bag.AddItem(ItemDatabase.Get("Card Key")!, 1);
        TalkTo("SilphCo_5F", "door1_00", 0, 1);
        ReadTo("Card Key opened"); Frames(10); Shot("ks75_the_card_key");
        ReadTo(null);
        StepOnto("SilphCo_7F", 2, 4, Direction.Down);
        ReadTo("What kept you"); Frames(10); Shot("ks76_the_rival_at_silph");
        IntoBattle("the rival's battle at Silph Co.");
        Win();
        ReadTo(null);
        story.Set("FLAG_SILPH_11F_DOOR");
        StepOnto("SilphCo_11F", 5, 15, Direction.Up);
        ReadTo("We meet once more"); Frames(10); Shot("ks77_giovanni_at_silph");
        IntoBattle("Giovanni's second battle");
        Win();
        ReadTo(null); Frames(20);
        TalkTo("SilphCo_11F", "old_man_2");
        ReadTo("secret prototype"); Frames(10); Shot("ks78_the_master_ball");
        ReadTo(null);
        Console.WriteLine($"  Silph Co.: Rockets gone {story.Has("FLAG_HIDE_SAFFRON_ROCKETS")}, Master Ball {game.Bag.GetQuantity(ItemDatabase.Get("Master Ball")!)}");
        TalkTo("SaffronCity_Gym", "sabrina");
        ReadTo("I had a vision"); Frames(10); Shot("ks79_sabrina");
        IntoBattle("Sabrina's battle");
        ToMainMenu(game.Battle); Shot("ks80_sabrinas_battle");
        Win();
        ReadTo("Marsh Badge from Sabrina"); Frames(10); Shot("ks81_the_marsh_badge");
        ReadTo(null);
        Console.WriteLine($"  badges: {string.Join(", ", new[] { "Boulder", "Cascade", "Thunder", "Rainbow", "Marsh" }.Where(b => story.HasBadge("Kanto", b)))}");

        // ---- plan 19 · K6, part a: the Cycling Road's gate, the road, the Safari Zone, the Warden, Koga
        int bicycles = game.Bag.GetQuantity(ItemDatabase.Get("Bicycle")!);
        if (bicycles > 0) game.Bag.RemoveItem(ItemDatabase.Get("Bicycle")!, bicycles);
        // Come in by the door, as a player does: the arrival forgets FireRed's temporary variables (VAR_TEMP_1, which the
        // tower's fifth floor left set), on which the gate's steps wait
        ComeThrough("Route16_NorthEntrance_1F", 10, 12, Direction.Left);
        StepOnto("Route16_NorthEntrance_1F", 6, 12, Direction.Left);
        ReadTo("No one goes onto the Cycling Road"); Frames(10); Shot("ks82_the_gate_turns_back");
        ReadTo(null); Frames(20);
        Console.WriteLine($"  Route 16's gate: the player turned back to {game.Player.GridX},{game.Player.GridY}");
        game.Bag.AddItem(ItemDatabase.Get("Bicycle")!, 1);
        story.SetVar("VAR_MAP_SCENE_ROUTE16", 1);
        ComeThrough("Kanto", 136, 220, Direction.Down);
        Frames(30); Shot("ks83_riding_the_cycling_road");
        for (int i = 0; i < 90; i++) Frames(1);
        Shot("ks84_rolling_down_the_slope");
        Console.WriteLine($"  Cycling Road: riding {game.Player.Mode}, kept on {story.Has("FLAG_ON_CYCLING_ROAD")}, rolled from 220 at x 136 to {game.Player.GridY}");
        story.SetVar("VAR_MAP_SCENE_ROUTE16", 0);
        game.Player.SetCycling(false);
        ComeThrough("FuchsiaCity_SafariZone_Entrance", 4, 6, Direction.Up);
        StepOnto("FuchsiaCity_SafariZone_Entrance", 4, 3, Direction.Up);
        ReadTo("Would you like to play the Safari Game"); Frames(10); Shot("ks85_the_safari_counter");
        ReadTo(null); Frames(30); Shot("ks86_in_the_safari_zone");
        Console.WriteLine($"  Safari Zone: on {game.Safari.Active}, balls {game.Safari.Balls}, steps {game.Safari.Steps}, at {game.Map.Name}");
        game.Safari.End();
        TalkTo("FuchsiaCity_WardensHouse", "old_man_2");
        ReadTo("Hif fuff"); Frames(10); Shot("ks87_the_warden_mumbles");
        ReadTo(null);
        game.Bag.AddItem(ItemDatabase.Get("Gold Teeth")!, 1);
        TalkTo("FuchsiaCity_WardensHouse", "old_man_2");
        ReadTo("popped in his teeth"); Frames(10); Shot("ks88_the_gold_teeth");
        ReadTo(null);
        TalkTo("SafariZone_SecretHouse", "worker_m");
        ReadTo("secret house"); Frames(10); Shot("ks89_the_secret_house");
        ReadTo(null);
        TalkTo("FuchsiaCity_Gym", "koga");
        ReadTo("I am Koga"); Frames(10); Shot("ks90_koga");
        IntoBattle("Koga's battle");
        ToMainMenu(game.Battle); Shot("ks91_kogas_battle");
        Win();
        ReadTo("Soul Badge from Koga"); Frames(10); Shot("ks92_the_soul_badge");
        ReadTo(null);
        Console.WriteLine($"  Fuchsia: HM04 {game.Bag.GetQuantity(ItemDatabase.Get("HM04")!)}, HM03 {game.Bag.GetQuantity(ItemDatabase.Get("HM03")!)}, Soul Badge {story.HasBadge("Kanto", "Soul")}, TM06 {game.Bag.GetQuantity(ItemDatabase.Get("TM06")!)}");
    }
}
