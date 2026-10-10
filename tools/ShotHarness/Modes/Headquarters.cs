partial class Harness
{
    // The eighth chapter's second half (plan 02 · S11, part b; not part of `all`), played by its own scripts: the grunt
    // on the terrace before the Galactic Veilstone Building and the Storage Key he lets fall, Looker's question, the
    // building's lobby and its shutter, the room its third door leads to; the warehouse, Looker unlocking its door
    // and going on ahead; the basement's store rooms, a warp panel ridden (turning, and on the far panel), the floors
    // one by one with their rooms (the nap room's bed, the kitchen's report, the television room), the Galactic Key
    // and the shutters it opens; the hall with its ranks, Looker, Cyrus's speech and the hall empty; Cyrus at his desk,
    // his battle and the Master Ball; the laboratory's tanks; the control room, Commander Saturn's battle, the button
    // and the three Pokémon of the lakes set free; the way out by the green panel. HQ_FROM=warehouse starts at the
    // warehouse's door with Looker's word given, HQ_FROM=floors in the basement, HQ_FROM=hall before the speech,
    // HQ_FROM=cyrus on the fourth floor with the key, HQ_FROM=lab in the laboratory with Cyrus beaten.
    public void HeadquartersMode()
    {
        engine.StartNewGame();
        PastTheOpening();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var story = game.Story;
        var bag = game.Bag;
        var team = game.Party;
        var infernape = new Pokemon(PokemonDatabase.Get("Infernape")!, 60);
        infernape.Moves.Clear();
        foreach (string move in new[] { "Flamethrower", "Earthquake", "Close Combat", "Mach Punch" }) infernape.Moves.Add(new Move(MoveDatabase.Get(move)!));
        team.Members.Insert(0, infernape);
        // The Badges of the chapters before, and the story as the scene at Lake Acuity leaves it (lake_acuity.txt): the
        // grunt with the Storage Key on the terrace, Looker long gone from the warehouse with the explosion in Pastoria
        foreach (var badge in new[] { Badge.Coal, Badge.Forest, Badge.Relic, Badge.Cobble, Badge.Fen, Badge.Mine, Badge.Icicle }) story.GiveBadge(badge);
        foreach (string flag in new[] { "FLAG_TEAM_GALACTIC_LEFT_LAKE_VERITY", "FLAG_HIDE_VEILSTONE_GALACTIC_GRUNTS", "FLAG_HIDE_VEILSTONE_COUNTERPART",
            "FLAG_HIDE_VEILSTONE_CRASHER_WAKE", "FLAG_HIDE_VEILSTONE_CITY_LOOKER", "FLAG_HIDE_VEILSTONE_CITY_GALACTIC_WAREHOUSE_LOOKER",
            "FLAG_OBTAINED_VEILSTONE_CITY_GALACTIC_WAREHOUSE_HM02", "FLAG_HIDE_LAKE_ACUITY_JUPITER", "FLAG_HIDE_LAKE_ACUITY_RIVAL" })
            story.Set(flag);
        story.Unset("FLAG_HIDE_VEILSTONE_CITY_GRUNT_M_STORAGE_KEY");
        story.SetVar("VAR_LAKE_ACUITY_STATE", 2);
        story.SetVar("VAR_VEILSTONE_CITY_GALACTIC_WAREHOUSE_STATE", 2);
        story.SetVar("VAR_VEILSTONE_CITY_CRASHER_WAKE_STATE", 1);
        story.SetVar("VAR_VEILSTONE_WAREHOUSE_GUARDS_FIGHTABLE", 1);
        story.SetVar("VAR_VEILSTONE_CITY_COUNTERPART_NEEDS_HELP_STATE", 3);

        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        Player Me() => game.Player;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Quick();
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }
        int quick = 0;
        void Quick()
        {
            FrameClock.Fixed = ++tick / 60.0;
            engine.Update(1f / 60f);
            if (++quick % 6 == 0) engine.Draw();
        }
        // Reads on to a line, answering questions with their first answer (yes) unless told which
        void ReadTo(string? text, int most = 3000, int answer = 0)
        {
            for (int i = 0; i < most; i++)
            {
                if (text != null && Box().IsActive && Box().CurrentLine.Contains(text)) { Whole(); Console.WriteLine($"  said: {text}"); return; }
                if (text != null && !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld && i > 60) break;
                if (text == null && !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld) return;
                if (State() == GameState.Battle) { Win(); continue; }
                if (engine.Choice.IsOpen)
                {
                    Quick();
                    for (int a = 0; a < answer; a++) engine.Choice.Move(1);
                    engine.Choice.Confirm();
                    Quick();
                }
                else if (Box().IsActive && !Box().IsCurrentLineComplete) { Box().FinishLine(); Quick(); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Quick(); }
                else Quick();
            }
            Console.WriteLine($"  !! never said: {text ?? "the script's end"}");
        }
        // A battle won: every foe brought down to 1 HP and struck, the player's own team kept well
        void Win()
        {
            var fight = game.Battle;
            for (int guard = 0; guard < 400 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    foreach (var mine in team.Members.Where(p => p.CurrentHP > 0)) { mine.CurrentHP = mine.MaxHP; mine.Status = StatusCondition.None; }
                    foreach (var foe in fight.EnemyParty?.Members ?? new List<Pokemon>()) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    foreach (var wild in fight.Core.EnemySlots.Where(b => b.Pokemon != null)) wild.Pokemon!.CurrentHP = Math.Min(wild.Pokemon.CurrentHP, 1);
                    fight.SelectMainMenuOption(0);
                    fight.SelectMove(0);
                    if (fight.HUD.MenuState == BattleMenuState.SelectTarget) fight.SelectTarget(0);
                }
                else Confirm(fight);
                Skip(0.5);
            }
            Until(() => State() == GameState.Overworld, "the field again", 1500);
            foreach (var mine in team.Members.Where(p => !p.IsEgg)) { mine.CurrentHP = mine.MaxHP; mine.Status = StatusCondition.None; }
            Frames(10);
        }
        void IntoBattle(string what)
        {
            for (int i = 0; i < 2500 && State() != GameState.Battle; i++)
            {
                if (engine.Choice.IsOpen) { Quick(); engine.Choice.Confirm(); Quick(); }
                else if (Box().IsActive && !Box().IsCurrentLineComplete) { Box().FinishLine(); Quick(); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Quick(); }
                else Quick();
            }
            if (State() != GameState.Battle) Console.WriteLine($"  !! never happened: {what}");
            else Skip(1.0);
        }
        // A step onto a tile of a map, from the tile before it
        void StepOnto(string map, int x, int y, Direction way, Func<bool>? until = null, string? what = null)
        {
            var (dx, dy) = way switch { Direction.Up => (0, -1), Direction.Down => (0, 1), Direction.Left => (-1, 0), _ => (1, 0) };
            At(map, x - dx, y - dy, way);
            engine.Steering = (way, false);
            Until(until ?? (() => engine.ScriptRunning), what ?? $"the scene at {x},{y}", 120);
            engine.Steering = null;
        }
        // Comes into a map through a warp onto a tile, as the game does: its own script first
        void ComeThrough(string map, int x, int y, Direction facing)
        {
            At(map, x, y, facing);
            game.ArriveOnMap();
            Frames(2);
            ReadTo(null, 300);
        }
        void TalkTo(string map, string key, int dx = 0, int dy = 1)
        {
            var npc = MapDatabase.Get(map).NPCs.First(n => n.Key == key);
            var facing = (dx, dy) switch { (0, > 0) => Direction.Up, (0, < 0) => Direction.Down, ( > 0, 0) => Direction.Left, _ => Direction.Right };
            At(map, npc.GridX + dx, npc.GridY + dy, facing);
            game.Interact();
        }
        // Faces a tile from the one beside it and presses the button
        void Read(string map, int x, int y, Direction facing)
        {
            var (dx, dy) = facing switch { Direction.Up => (0, -1), Direction.Down => (0, 1), Direction.Left => (-1, 0), _ => (1, 0) };
            At(map, x - dx, y - dy, facing);
            game.Interact();
        }
        // A look at a room from a tile of it
        void Look(string map, int x, int y, Direction facing, string shot)
        {
            At(map, x, y, facing);
            Frames(30);
            Shot(shot);
        }
        void Where(string what) => Console.WriteLine($"{what}: player at {Me().GridX},{Me().GridY} of {game.Map.Name}, facing {Me().Facing}");
        string Flags(params string[] flags) => string.Join(", ", flags.Select(f => $"{f.Replace("FLAG_", "")} {story.Has(f)}"));
        bool There(string map, string key) => MapDatabase.Get(map).NPCs.Any(n => n.Key == key);
        // Rides the warp panel at a tile, stepping onto it from the tile before; shots of the turn and of the far panel
        void Ride(string map, int x, int y, Direction way, string? shot = null)
        {
            var before = (game.Map.Name, x, y);
            StepOnto(map, x, y, way, () => Me().GridX == x && Me().GridY == y && !Me().IsMoving, $"the step onto the panel at {x},{y}");
            for (int i = 0; i < 14; i++) Quick();
            if (shot != null) { Frames(1); Shot(shot + "_turning"); }
            Until(() => (game.Map.Name, Me().GridX, Me().GridY) != before && State() == GameState.Overworld, $"the far end of the panel at {x},{y}", 400);
            for (int i = 0; i < 40; i++) Quick();
            ReadTo(null, 300);
            if (shot != null) { Frames(10); Shot(shot + "_arrived"); }
            Where($"panel {map} {x},{y}");
        }

        Frames(5);
        ReadTo(null, 600);

        string from = Environment.GetEnvironmentVariable("HQ_FROM") ?? "";
        int stage = from switch { "" => 0, "warehouse" => 1, "floors" => 2, "hall" => 3, "cyrus" => 4, "lab" => 5, _ => 0 };

        if (stage == 0)
        {
            // ---- Veilstone City: the grunt on the terrace before the building, and the Storage Key he lets fall
            ComeThrough("Sinnoh", 723, 598, Direction.Up);
            Look("Sinnoh", 719, 594, Direction.Up, "hq01_veilstone_the_terrace");
            TalkTo("Sinnoh", "grunt_m_storage_key", -1, 0);
            ReadTo("antennae like it"); Frames(4); Shot("hq02_veilstone_the_grunt_and_the_antennae");
            ReadTo("It's you"); Frames(4); Shot("hq03_veilstone_the_grunt_knows_the_player");
            ReadTo("Storage Key"); Frames(4); Shot("hq04_veilstone_no_storage_key_here");
            Box().Advance();
            for (int i = 0; i < 40; i++) Quick();
            Frames(2); Shot("hq05_veilstone_the_grunt_runs");
            ReadTo("I have it, right here"); Frames(4); Shot("hq06_veilstone_looker_has_the_key");
            ReadTo("come into the hideout"); Frames(4); Shot("hq07_veilstone_lookers_question");
            ReadTo("no small thing", answer: 1); Frames(4); Shot("hq08_veilstone_not_yet");
            ReadTo(null);
            Console.WriteLine($"veilstone, refused: warehouse state {story.Var("VAR_VEILSTONE_CITY_GALACTIC_WAREHOUSE_STATE")}, "
                + Flags("FLAG_VEILSTONE_CITY_GRUNT_DROPPED_STORAGE_KEY", "FLAG_HIDE_VEILSTONE_CITY_GRUNT_M_STORAGE_KEY", "FLAG_HIDE_VEILSTONE_CITY_LOOKER", "FLAG_AGREED_WITH_LOOKER_TO_ENTER_HIDEOUT"));
            // Come to again, Looker waits before the building's middle door
            ComeThrough("Sinnoh", 723, 598, Direction.Up);
            Look("Sinnoh", 716, 594, Direction.Up, "hq09_veilstone_looker_before_the_building");
            TalkTo("Sinnoh", "looker");
            ReadTo("we meet"); Frames(4); Shot("hq10_veilstone_inside_the_warehouse_we_meet");
            ReadTo(null);
            Console.WriteLine($"veilstone, agreed: warehouse state {story.Var("VAR_VEILSTONE_CITY_GALACTIC_WAREHOUSE_STATE")}, {Flags("FLAG_AGREED_WITH_LOOKER_TO_ENTER_HIDEOUT")}");

            // ---- the building's doors: the lobby, its people, the shutter; the room behind the third door
            ComeThrough("GalacticHQ1F", 29, 22, Direction.Up);
            Frames(30); Shot("hq11_lobby_from_the_middle_door");
            Read("GalacticHQ1F", 29, 16, Direction.Up);
            ReadTo("Veilstone Building"); Frames(4); Shot("hq12_lobby_the_receptionist");
            ReadTo(null);
            Look("GalacticHQ1F", 8, 21, Direction.Up, "hq13_lobby_the_west_door");
            Read("GalacticHQ1F", 22, 18, Direction.Up);
            ReadTo("special key"); Frames(4); Shot("hq14_lobby_the_shutter_locked");
            ReadTo(null);
            ComeThrough("GalacticHQ1F", 42, 22, Direction.Up);
            Frames(30); Shot("hq15_the_room_behind_the_third_door");
            At("GalacticHQ1F", 41, 15, Direction.Up);
            engine.Steering = (Direction.Up, false);
            for (int i = 0; i < 40; i++) Quick();
            engine.Steering = null;
            Frames(10); Shot("hq16_the_green_pad_carries_nobody");
            Where("on the green pad");
            Console.WriteLine($"lobby: {Flags("FLAG_FIRST_ARRIVAL_GALACTIC_HQ", "FLAG_HIDE_GALACTIC_HQ_1F_DOOR")}");
        }
        else
        {
            story.Set("FLAG_VEILSTONE_CITY_GRUNT_DROPPED_STORAGE_KEY");
            story.Set("FLAG_HIDE_VEILSTONE_CITY_GRUNT_M_STORAGE_KEY");
            story.Set("FLAG_AGREED_WITH_LOOKER_TO_ENTER_HIDEOUT");
            story.Unset("FLAG_HIDE_VEILSTONE_CITY_LOOKER");
            story.SetVar("VAR_VEILSTONE_CITY_GALACTIC_WAREHOUSE_STATE", 3);
        }

        if (stage <= 1)
        {
            // ---- the warehouse: the rusty door, and Looker with the Storage Key
            ComeThrough("VeilstoneGalacticWarehouse", 8, 11, Direction.Up);
            Look("VeilstoneGalacticWarehouse", 8, 9, Direction.Up, "hq20_warehouse_the_door");
            StepOnto("VeilstoneGalacticWarehouse", 8, 8, Direction.Up);
            ReadTo("kept you waiting"); Frames(4); Shot("hq21_warehouse_looker_comes");
            ReadTo("In it goes"); Frames(4); Shot("hq22_warehouse_the_storage_key");
            ReadTo("It is open"); Frames(4); Shot("hq23_warehouse_the_door_open");
            ReadTo("go on ahead"); Frames(4); Shot("hq24_warehouse_looker_goes_ahead");
            Box().Advance();
            for (int i = 0; i < 110; i++) Quick();
            Frames(2); Shot("hq25_warehouse_looker_at_the_stairs");
            ReadTo(null); Frames(20);
            Look("VeilstoneGalacticWarehouse", 11, 4, Direction.Right, "hq26_warehouse_the_stairs_down");
            Console.WriteLine($"warehouse: state {story.Var("VAR_VEILSTONE_CITY_GALACTIC_WAREHOUSE_STATE")}, "
                + Flags("FLAG_USED_STORAGE_KEY", "FLAG_HIDE_VEILSTONE_CITY_GALACTIC_WAREHOUSE_GALACTIC_HQ_DOOR", "FLAG_HIDE_VEILSTONE_CITY_LOOKER", "FLAG_HIDE_VEILSTONE_CITY_GALACTIC_WAREHOUSE_LOOKER"));
            // Down the stairs
            StepOnto("VeilstoneGalacticWarehouse", 13, 3, Direction.Right, () => game.Map.Name == "GalacticHQB2F" && State() == GameState.Overworld, "the stairs down to the basement");
            ReadTo(null, 300); Frames(30); Shot("hq27_basement_from_the_warehouse_stairs");
            Where("down the stairs");
        }
        else
        {
            story.SetVar("VAR_VEILSTONE_CITY_GALACTIC_WAREHOUSE_STATE", 4);
            foreach (string flag in new[] { "FLAG_USED_STORAGE_KEY", "FLAG_HIDE_VEILSTONE_CITY_GALACTIC_WAREHOUSE_GALACTIC_HQ_DOOR", "FLAG_HIDE_VEILSTONE_CITY_LOOKER" }) story.Set(flag);
        }

        if (stage <= 2)
        {
            // ---- the basement: store rooms, the shutter from the store rooms' side, a trainer, and up to the ground floor
            Look("GalacticHQB2F", 12, 5, Direction.Right, "hq30_basement_the_shutter_from_the_store_rooms");
            Read("GalacticHQB2F", 14, 8, Direction.Down);
            ReadTo("special key"); Frames(4); Shot("hq31_basement_the_shutter_locked");
            ReadTo(null);
            Look("GalacticHQB2F", 30, 18, Direction.Right, "hq32_basement_the_long_room");
            TalkTo("GalacticHQB2F", "galactic_grunt_1", -1, 0);
            IntoBattle("the basement's grunt");
            ToMainMenu(game.Battle); Shot("hq33_basement_a_grunts_battle");
            Win();
            ReadTo(null);
            Look("GalacticHQB2F", 60, 11, Direction.Right, "hq34_basement_the_east_corridor");
            Look("GalacticHQB2F", 78, 5, Direction.Up, "hq35_basement_the_stairs_up");
            ComeThrough("GalacticHQB1F", 4, 7, Direction.Right);
            Frames(30); Shot("hq36_upper_basement");
            TalkTo("GalacticHQB1F", "grunt_m", 1, 0);
            ReadTo("warp panel"); Frames(4); Shot("hq37_upper_basement_the_grunt_without_pokemon");
            ReadTo(null);

            // ---- the ground floor's north-east rooms, and a warp panel ridden
            ComeThrough("GalacticHQ1F", 47, 3, Direction.Right);
            Frames(30); Shot("hq40_ground_floor_from_the_basement_stairs");
            Look("GalacticHQ1F", 42, 8, Direction.Up, "hq41_ground_floor_the_panels_room");
            Ride("GalacticHQ1F", 39, 3, Direction.Up, "hq42_panel");
            Look("GalacticHQ1F", 34, 6, Direction.Up, "hq43_ground_floor_between_panels");
            Ride("GalacticHQ1F", 36, 8, Direction.Right);
            Frames(20); Shot("hq44_ground_floor_the_west_room");
            // Back onto the same panel without stepping off it: it carries nobody
            Console.WriteLine($"ground floor: on the far panel, a warp panel here {WarpPanels.IsPanel(game.Map, Me().GridX, Me().GridY)}, the game's state {State()}");

            // ---- the second floor: the stairs' room, the machines, the television room, the nap room, the kitchen
            ComeThrough("GalacticHQ2F", 8, 3, Direction.Right);
            Frames(30); Shot("hq50_second_floor_the_stairs");
            Ride("GalacticHQ2F", 1, 3, Direction.Left);
            Frames(20); Shot("hq51_second_floor_the_east_stairs_room");
            Look("GalacticHQ2F", 22, 14, Direction.Up, "hq52_second_floor_the_machines");
            Look("GalacticHQ2F", 33, 8, Direction.Up, "hq53_second_floor_the_television_room");
            Read("GalacticHQ2F", 31, 18, Direction.Up);
            ReadTo("TV Room"); Frames(4); Shot("hq54_second_floor_the_television_rooms_sign");
            ReadTo(null);
            Look("GalacticHQ2F", 42, 6, Direction.Up, "hq55_second_floor_the_nap_room");
            Read("GalacticHQ2F", 40, 4, Direction.Right);
            ReadTo("Lie down"); Frames(4); Shot("hq56_second_floor_the_bed");
            ReadTo("asleep"); Frames(4); Shot("hq57_second_floor_asleep");
            ReadTo("fully rested"); Frames(4); Shot("hq58_second_floor_rested");
            ReadTo(null);
            Look("GalacticHQ2F", 43, 15, Direction.Up, "hq59_second_floor_the_kitchen");
            Read("GalacticHQ2F", 41, 17, Direction.Right);
            ReadTo("Read it"); Frames(4); Shot("hq60_second_floor_the_report");
            ReadTo("DIALGA"); Frames(4); Shot("hq61_second_floor_the_first_plate");
            ReadTo("PALKIA"); Frames(4); Shot("hq62_second_floor_the_second_plate");
            ReadTo(null);
            Look("GalacticHQ2F", 10, 21, Direction.Up, "hq63_second_floor_the_south_west_room");

            // ---- down to the scientists' room and the basement's north side: the Galactic Key
            ComeThrough("GalacticHQ1F", 28, 3, Direction.Right);
            Frames(30); Shot("hq64_ground_floor_the_scientists_room");
            ComeThrough("GalacticHQB2F", 49, 3, Direction.Left);
            Frames(30); Shot("hq65_basement_the_keys_side");
            var key = MapDatabase.Get("GalacticHQB2F").NPCs.FirstOrDefault(n => n.Item == "Galactic Key");
            if (key != null)
            {
                Look("GalacticHQB2F", key.GridX + 1, key.GridY, Direction.Left, "hq66_basement_the_galactic_key");
                game.Interact();
                ReadTo("Galactic Key"); Frames(4); Shot("hq67_basement_the_galactic_key_found");
                ReadTo(null);
            }
            else Console.WriteLine("  !! the Galactic Key isn't on the basement's floor");
            Read("GalacticHQB2F", 14, 8, Direction.Up);
            ReadTo("Use the Galactic Key"); Frames(4); Shot("hq68_basement_the_shutter_asks");
            ReadTo("slid open"); Frames(4); Shot("hq69_basement_the_shutter_open");
            ReadTo(null);
            Console.WriteLine($"basement: galactic key {bag.GetQuantity(ItemDatabase.Get("Galactic Key")!)}, {Flags("FLAG_OBTAINED_GALACTIC_HQ_B2F_GALACTIC_KEY", "FLAG_HIDE_GALACTIC_HQ_B2F_DOOR")}, the leaves there {There("GalacticHQB2F", "galactic_hq_door_west")}");

            // ---- the lobby's shutter, opened from the lobby's side
            Read("GalacticHQ1F", 23, 18, Direction.Up);
            ReadTo("slid open"); Frames(4); Shot("hq70_lobby_the_shutter_open");
            ReadTo(null);
            Look("GalacticHQ1F", 22, 15, Direction.Left, "hq71_lobby_the_stairs_behind_the_shutter");

            // ---- the third floor
            ComeThrough("GalacticHQ3F", 16, 3, Direction.Right);
            Frames(30); Shot("hq72_third_floor_the_north_west_room");
            Look("GalacticHQ3F", 12, 21, Direction.Up, "hq73_third_floor_the_south_corridor");
            Read("GalacticHQ3F", 27, 19, Direction.Down);
            ReadTo("slid open"); Frames(4); Shot("hq74_third_floor_the_shutter_open");
            ReadTo(null);
            Look("GalacticHQ3F", 31, 12, Direction.Up, "hq75_third_floor_the_machine_room");
            Look("GalacticHQ3F", 44, 6, Direction.Up, "hq76_third_floor_the_north_east_room");
            Look("GalacticHQ3F", 44, 19, Direction.Up, "hq77_third_floor_the_south_east_room");
            Look("GalacticHQ3F", 9, 13, Direction.Up, "hq78_third_floor_the_stairs_to_the_fourth");
            Console.WriteLine($"shutters: {Flags("FLAG_HIDE_GALACTIC_HQ_1F_DOOR", "FLAG_HIDE_GALACTIC_HQ_3F_DOOR")}");
        }
        else
        {
            if (ItemDatabase.Get("Galactic Key") is { } galacticKey && bag.GetQuantity(galacticKey) == 0) bag.AddItem(galacticKey);
            foreach (string flag in new[] { "FLAG_OBTAINED_GALACTIC_HQ_B2F_GALACTIC_KEY", "FLAG_HIDE_GALACTIC_HQ_B2F_DOOR", "FLAG_HIDE_GALACTIC_HQ_1F_DOOR", "FLAG_HIDE_GALACTIC_HQ_3F_DOOR" }) story.Set(flag);
        }

        if (stage <= 3)
        {
            // ---- the hall: the ranks, Looker at the back, Cyrus's speech
            ComeThrough("GalacticHQHall", 24, 6, Direction.Down);
            Frames(30); Shot("hq80_hall_from_the_east_panel");
            Look("GalacticHQHall", 22, 13, Direction.Left, "hq81_hall_the_ranks");
            StepOnto("GalacticHQHall", 20, 12, Direction.Left);
            ReadTo("Over here"); Frames(4); Shot("hq82_hall_looker_waves");
            ReadTo("What can it mean"); Frames(4); Shot("hq83_hall_what_can_it_mean");
            ReadTo("I stand before you"); Frames(4); Shot("hq84_hall_cyrus_on_the_dais");
            ReadTo("must change"); Frames(4); Shot("hq85_hall_the_world_must_change");
            ReadTo("every one of you"); Frames(4); Shot("hq86_hall_who_will_change_it");
            ReadTo("Glory to Team Galactic"); Frames(4); Shot("hq87_hall_glory");
            ReadTo("RAAAAAH"); Frames(4); Shot("hq88_hall_the_roar");
            ReadTo("nobody like him"); Frames(4);
            Box().Advance();
            for (int i = 0; i < 60; i++) Quick();
            Frames(2); Shot("hq89_hall_cyrus_leaves_the_dais");
            ReadTo("twenty-seven"); Frames(4); Shot("hq90_hall_looker_afterwards");
            ReadTo("International Police"); Frames(4); Shot("hq91_hall_the_international_police");
            ReadTo("Take every care"); Frames(4);
            Box().Advance();
            for (int i = 0; i < 90; i++) Quick();
            Frames(2); Shot("hq92_hall_looker_goes");
            ReadTo(null); Frames(20);
            Shot("hq93_hall_empty");
            Console.WriteLine($"hall: state {story.Var("VAR_GALACTIC_HQ_HALL_STATE")}, {Flags("FLAG_HIDE_GALACTIC_HQ_HALL_CYRUS", "FLAG_HIDE_GALACTIC_HQ_HALL_GRUNTS", "FLAG_HIDE_GALACTIC_HQ_HALL_LOOKER")}, "
                + $"people left in the hall {MapDatabase.Get("GalacticHQHall").NPCs.Count}");
            Where("the hall");
        }
        else
        {
            story.SetVar("VAR_GALACTIC_HQ_HALL_STATE", 1);
            foreach (string flag in new[] { "FLAG_HIDE_GALACTIC_HQ_HALL_GRUNTS", "FLAG_HIDE_GALACTIC_HQ_HALL_LOOKER" }) story.Set(flag);
        }

        if (stage <= 4)
        {
            // ---- the fourth floor: the stairs' room, the shutter, Cyrus
            ComeThrough("GalacticHQ4F", 4, 15, Direction.Right);
            Frames(30); Shot("hq100_fourth_floor_from_the_stairs");
            Read("GalacticHQ4F", 8, 14, Direction.Up);
            ReadTo("slid open"); Frames(4); Shot("hq101_fourth_floor_the_shutter_open");
            ReadTo(null);
            Look("GalacticHQ4F", 9, 13, Direction.Up, "hq102_fourth_floor_cyrus_at_his_desk");
            StepOnto("GalacticHQ4F", 8, 11, Direction.Up);
            ReadTo("You have come"); Frames(4); Shot("hq103_cyrus_turns");
            ReadTo("myself alone"); Frames(4); Shot("hq104_cyrus_a_world_for_himself");
            ReadTo("You want them saved"); Frames(4); Shot("hq105_cyrus_knows_why");
            ReadTo("regret listening"); Frames(4); Shot("hq106_cyrus_before_the_battle");
            IntoBattle("Cyrus");
            ToMainMenu(game.Battle); Shot("hq107_cyrus_the_battle");
            Win();
            ReadTo("This is your reward"); Frames(4); Shot("hq108_cyrus_beaten");
            ReadTo("Master Ball"); Frames(4); Shot("hq109_the_master_ball");
            ReadTo("my own"); Frames(4); Shot("hq110_cyrus_the_power_of_pokemon");
            ReadTo("warp panel ahead"); Frames(4); Shot("hq111_cyrus_points_to_the_panel");
            ReadTo("new world"); Frames(4);
            Box().Advance();
            for (int i = 0; i < 150; i++) Quick();
            Frames(2); Shot("hq112_cyrus_leaves_by_the_panel");
            ReadTo(null); Frames(20);
            Shot("hq113_fourth_floor_cyrus_gone");
            Console.WriteLine($"fourth floor: state {story.Var("VAR_GALACTIC_HQ_4F_STATE")}, master ball {bag.GetQuantity(ItemDatabase.Get("Master Ball")!)}, "
                + Flags("FLAG_HIDE_GALACTIC_HQ_4F_CYRUS", "FLAG_HIDE_GALACTIC_HQ_4F_DOOR") + $", cyrus beaten {story.HasDefeated("galactic_boss_cyrus_galactic_hq")}");
            Read("GalacticHQ4F", 1, 6, Direction.Left);
            ReadTo("Read which one"); Frames(4); Shot("hq114_fourth_floor_the_reports");
            ReadTo("Red Chain can be made", answer: 2); Frames(4); Shot("hq115_fourth_floor_the_red_chain");
            ReadTo(null);
            Look("GalacticHQ4F", 19, 12, Direction.Down, "hq116_fourth_floor_the_bollards");
            Ride("GalacticHQ4F", 22, 3, Direction.Up);
        }
        else
        {
            story.SetVar("VAR_GALACTIC_HQ_4F_STATE", 1);
            foreach (string flag in new[] { "FLAG_HIDE_GALACTIC_HQ_4F_CYRUS", "FLAG_HIDE_GALACTIC_HQ_4F_DOOR" }) story.Set(flag);
            ComeThrough("GalacticHQLaboratory", 1, 11, Direction.Right);
        }

        // ---- the laboratory: its tanks and its two scientists, and the doorway to the control room
        Frames(30); Shot("hq120_laboratory_from_the_panel");
        Look("GalacticHQLaboratory", 16, 11, Direction.Up, "hq121_laboratory_the_tanks");
        Read("GalacticHQLaboratory", 17, 9, Direction.Up);
        ReadTo("green fluid"); Frames(4); Shot("hq122_laboratory_something_in_the_fluid");
        ReadTo(null);
        TalkTo("GalacticHQLaboratory", "scientist_m", 1, 0);
        ReadTo("too far"); Frames(4); Shot("hq123_laboratory_the_scientist");
        ReadTo(null);
        Look("GalacticHQLaboratory", 36, 6, Direction.Up, "hq124_laboratory_the_doorway");
        StepOnto("GalacticHQLaboratory", 36, 2, Direction.Up, () => game.Map.Name == "GalacticHQControlRoom" && State() == GameState.Overworld, "the doorway to the control room");
        ReadTo(null, 300);

        // ---- the control room: Saturn and Charon, the three held, Saturn's battle, the button
        Frames(30); Shot("hq130_control_room");
        Where("the control room");
        TalkTo("GalacticHQControlRoom", "mesprit");
        ReadTo("Mesprit is shut inside"); Frames(4); Shot("hq131_control_room_mesprit");
        ReadTo(null);
        TalkTo("GalacticHQControlRoom", "charon", 0, 1);
        ReadTo("Where do you suppose"); Frames(4); Shot("hq132_control_room_charon");
        ReadTo("encouragement"); Frames(4); Shot("hq133_control_room_saturn_answers");
        ReadTo(null);
        // Making for the door before Saturn is beaten is nobody's business
        TalkTo("GalacticHQControlRoom", "saturn");
        ReadTo("save a few"); Frames(4); Shot("hq134_control_room_saturn");
        IntoBattle("Saturn");
        ToMainMenu(game.Battle); Shot("hq135_control_room_saturns_battle");
        Win();
        ReadTo("sets them free"); Frames(4); Shot("hq136_control_room_saturn_beaten");
        ReadTo(null); Frames(20);
        Shot("hq137_control_room_saturn_steps_aside");
        Console.WriteLine($"control room: state {story.Var("VAR_GALACTIC_HQ_CONTROL_ROOM_STATE")}, {Flags("FLAG_DEFEATED_GALACTIC_HQ_CONTROL_ROOM_SATURN")}, saturn beaten {story.HasDefeated("commander_saturn_galactic_hq")}");
        StepOnto("GalacticHQControlRoom", 8, 13, Direction.Down);
        ReadTo("what you came for"); Frames(4); Shot("hq138_control_room_called_back");
        ReadTo(null); Frames(10);
        Where("called back");
        TalkTo("GalacticHQControlRoom", "charon", 0, 1);
        ReadTo("genius of Charon"); Frames(4); Shot("hq139_control_room_charon_afterwards");
        ReadTo(null);
        Read("GalacticHQControlRoom", 8, 5, Direction.Up);
        ReadTo("Press it"); Frames(4); Shot("hq140_control_room_the_button");
        ReadTo("fell silent"); Frames(4); Shot("hq141_control_room_the_button_pressed");
        Box().Advance();
        for (int i = 0; i < 110; i++) Quick();
        Frames(2); Shot("hq142_control_room_the_three_come_out");
        ReadTo("Red Chain"); Frames(4); Shot("hq143_control_room_the_red_chain");
        ReadTo(null); Frames(20);
        Shot("hq144_control_room_afterwards");
        Console.WriteLine($"control room: state {story.Var("VAR_GALACTIC_HQ_CONTROL_ROOM_STATE")}, mt coronet 2f state {story.Var("VAR_MT_CORONET_2F_STATE")}, "
            + Flags("FLAG_FREED_GALACTIC_HQ_POKEMON", "FLAG_HIDE_GALACTIC_HQ_CONTROL_ROOM_LAKE_GUARDIANS", "FLAG_HIDE_GALACTIC_HQ_TEAM_GALACTIC",
                "FLAG_HIDE_MT_CORONET_1F_NORTH_ROOM_1_GRUNT_F", "FLAG_HIDE_MT_CORONET_2F_CAVE_PAINTING", "FLAG_HIDE_MT_CORONET_2F_CAVE_PAINTING_SHARDS"));
        Read("GalacticHQControlRoom", 8, 5, Direction.Up);
        ReadTo("pressed already"); Frames(4); Shot("hq145_control_room_the_button_again");
        ReadTo(null);
        Read("GalacticHQControlRoom", 8, 10, Direction.Up);
        ReadTo("Uxie was held"); Frames(4); Shot("hq146_control_room_an_empty_machine");
        ReadTo(null);

        // ---- the way out: the laboratory empty of its scientists, the bollards gone, the green panel, the street
        ComeThrough("GalacticHQLaboratory", 36, 3, Direction.Down);
        Frames(30); Shot("hq150_laboratory_empty");
        ComeThrough("GalacticHQ4F", 22, 3, Direction.Down);
        Look("GalacticHQ4F", 19, 12, Direction.Down, "hq151_fourth_floor_the_bollards_gone");
        Ride("GalacticHQ4F", 21, 17, Direction.Down, "hq152_the_green_panel");
        Where("the way out");
        StepOnto("GalacticHQ1F", 42, 23, Direction.Down, () => game.Map.Name == "Sinnoh" && State() == GameState.Overworld, "the street");
        ReadTo(null, 300); Frames(30); Shot("hq153_veilstone_out_of_the_building");
        Where("out of the building");
    }
}
