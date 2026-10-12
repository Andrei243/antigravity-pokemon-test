partial class Harness
{
    // The tenth chapter (plan 02 · S13; not part of `all`), played by its own scripts: the professor and the assistant
    // just inside the lab's door after the Distortion World, his news of the lakes, and the two afterwards; the road
    // east of Valor Lakefront open; Lake Verity's island, Mesprit in its cavern, gone, and the professor come in after
    // it; Lake Valor full again, Azelf in its cavern, its battle and the cavern empty; Lake Acuity's island and Uxie the
    // same; Route 222's rich boy and his TM; Flint at Sunyshore City's gate and before its Gym's door; the city's
    // Pokémon Center and Mart with their people; the Vista Lighthouse's door, its lift on the way up, the gallery,
    // Volkner before the binoculars, his walk round to the lift, the view through the binoculars and the ride down;
    // Flint leaving the Gym's door, Volkner's battle and the Beacon Badge; and the beach: Jasmine, the rival running up,
    // HM07. SUNYSHORE_FROM=lakes starts at the lakes with the professor's scene played, SUNYSHORE_FROM=city on Route
    // 222, SUNYSHORE_FROM=lighthouse at the lighthouse's door with Flint met, SUNYSHORE_FROM=gym before the Gym with
    // Volkner back in it, SUNYSHORE_FROM=beach on the beach with the Badge, the story set as the scenes before would
    // have left it.
    public void SunyshoreMode()
    {
        engine.StartNewGame();
        PastTheOpening();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var story = game.Story;
        var bag = game.Bag;
        var team = game.Party;
        var infernape = new Pokemon(PokemonDatabase.Get("Infernape")!, 64);
        infernape.Moves.Clear();
        foreach (string move in new[] { "Flamethrower", "Close Combat", "Earthquake", "Mach Punch" }) infernape.Moves.Add(new Move(MoveDatabase.Get(move)!));
        team.Members.Insert(0, infernape);
        // The Badges of the chapters before, and the story as the scene at Sendoff Spring leaves it (sendoff_spring.txt):
        // Team Galactic gone from the three lakes, Lake Valor full again, the professor's lab waiting to hear it all
        foreach (var badge in new[] { Badge.Coal, Badge.Forest, Badge.Relic, Badge.Cobble, Badge.Fen, Badge.Mine, Badge.Icicle }) story.GiveBadge(badge);
        foreach (string flag in new[] { "FLAG_GALACTIC_LEFT_LAKE_VALOR", "FLAG_DEFEATED_COMMANDER_SATURN_VALOR_CAVERN", "FLAG_HIDE_LAKE_VALOR_GALACTIC",
            "FLAG_TEAM_GALACTIC_LEFT_LAKE_VERITY", "FLAG_HIDE_LAKE_VERITY_TEAM_GALACTIC", "FLAG_HIDE_LAKE_VERITY_PROF_ROWAN", "FLAG_HIDE_LAKE_VERITY_COUNTERPART",
            "FLAG_HIDE_LAKE_ACUITY_JUPITER", "FLAG_HIDE_LAKE_ACUITY_RIVAL", "FLAG_HIDE_VALOR_LAKEFRONT_CAMERAMEN" })
            story.Set(flag);
        story.SetVar("VAR_LAKE_ACUITY_STATE", 2);
        story.SetVar("VAR_EXITED_DISTORTION_WORLD_STATE", 2);
        story.SetVar("VAR_SANDGEM_TOWN_LAB_STATE", 2);

        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        Player Me() => game.Player;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Quick();
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }
        // Between shots the game is moved on a frame at a time but drawn only every sixth frame (llvmpipe fills the 4K
        // screen slowly), and a line of text is shown whole at once
        int quick = 0;
        void Quick()
        {
            FrameClock.Fixed = ++tick / 60.0;
            engine.Update(1f / 60f);
            if (++quick % 6 == 0) engine.Draw();
        }
        void ReadTo(string? text, int most = 3000)
        {
            for (int i = 0; i < most; i++)
            {
                if (text != null && Box().IsActive && Box().CurrentLine.Contains(text)) { Whole(); Console.WriteLine($"  said: {text}"); return; }
                if (text != null && !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld && i > 60) break;
                if (text == null && !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld) return;
                if (State() == GameState.Battle) { Win(); continue; }
                if (engine.Choice.IsOpen) { Quick(); engine.Choice.Confirm(); Quick(); }
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
            for (int i = 0; i < 2000 && State() != GameState.Battle; i++)
            {
                if (engine.Choice.IsOpen) { Quick(); engine.Choice.Confirm(); Quick(); }
                else if (Box().IsActive && !Box().IsCurrentLineComplete) { Box().FinishLine(); Quick(); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Quick(); }
                else Quick();
            }
            if (State() != GameState.Battle) Console.WriteLine($"  !! never happened: {what}");
            else Skip(1.0);
        }
        // A step onto a tile of a map, from the tile before it, that starts a scene
        void StepOnto(string map, int x, int y, Direction way)
        {
            var (dx, dy) = FieldMovement.Delta(way);
            At(map, x - dx, y - dy, way);
            engine.Steering = (way, false);
            Until(() => engine.ScriptRunning, $"the scene at {x},{y}", 120);
            engine.Steering = null;
        }
        // Comes into a map through a warp onto a tile, as the game does: its own script, then the trigger stepped out onto
        void ComeThrough(string map, int x, int y, Direction facing)
        {
            At(map, x, y, facing);
            game.ArriveOnMap();
            game.SteppedOutOfWarp = true;
            game.LocationSign.Hide();
            Frames(2);
        }
        NPC? Someone(string map, string key, string? place = null) => MapDatabase.Get(map).NPCs.FirstOrDefault(n => n.Key == key && (place == null || n.ScriptFile == place));
        // Talks to someone of a map (a room's people belong to no area: place null), from below unless told otherwise
        void TalkTo(string map, string key, string? place = null, int dx = 0, int dy = 1)
        {
            var npc = MapDatabase.Get(map).NPCs.First(n => n.Key == key && (place == null || n.ScriptFile == place));
            var facing = (dx, dy) switch { (0, 1) => Direction.Up, (0, -1) => Direction.Down, (1, 0) => Direction.Left, _ => Direction.Right };
            At(map, npc.GridX + dx, npc.GridY + dy, facing);
            game.Interact();
        }
        // Lets what is being said go and the scene run on for a while, for a shot of someone on their way
        void Onward(int frames)
        {
            if (Box().IsActive) Box().Advance();
            for (int i = 0; i < frames; i++) Quick();
            Frames(2);
        }
        void Where(string what) => Console.WriteLine($"{what}: player at {Me().GridX},{Me().GridY} of {game.Map.Name}, facing {Me().Facing}");
        string Flags(params string[] flags) => string.Join(", ", flags.Select(f => $"{f.Replace("FLAG_", "")} {story.Has(f)}"));
        string Vars(params string[] vars) => string.Join(", ", vars.Select(v => $"{v.Replace("VAR_", "")} {story.Var(v)}"));

        Frames(5);
        ReadTo(null, 600);

        string from = Environment.GetEnvironmentVariable("SUNYSHORE_FROM") ?? "";
        int stage = from switch { "" => 0, "lakes" => 1, "city" => 2, "lighthouse" => 3, "gym" => 4, "beach" => 5, _ => 0 };

        if (stage == 0)
        {
            // ---- the road east of Valor Lakefront, shut until the professor has heard it all
            ComeThrough("Sinnoh", 721, 790, Direction.Right);
            Frames(30); Shot("sy01_lakefront_the_road_east_shut");
            Console.WriteLine($"valor lakefront before: collector there {Someone("Sinnoh", "collector", "valor_lakefront") != null}, {Vars("VAR_VALOR_LAKEFRONT_BLOCK_SUNYSHORE_STATE")}");

            // ---- the professor's lab: the two wait just inside the door, and the scene plays as the player comes in
            ComeThrough("RowanLab", 7, 15, Direction.Up);
            ReadTo("You are back"); Frames(4); Shot("sy02_lab_the_professor_and_the_assistant");
            ReadTo("Sixty years"); Frames(4); Shot("sy03_lab_never_pounded_so_hard");
            ReadTo("asking after you"); Frames(4); Shot("sy04_lab_the_assistant");
            ReadTo("all three lakes"); Frames(4); Shot("sy05_lab_the_lakes_pokemon_are_home");
            ReadTo("Sunyshore City's Gym"); Frames(4); Shot("sy06_lab_sunyshores_gym_is_left");
            ReadTo("Pokémon League"); Frames(4); Shot("sy07_lab_and_the_league_after_it");
            ReadTo(null); Frames(20);
            Shot("sy08_lab_the_two_by_the_door");
            Where("the lab");
            Console.WriteLine("the lab: " + Vars("VAR_SANDGEM_TOWN_LAB_STATE", "VAR_EXITED_DISTORTION_WORLD_STATE", "VAR_VALOR_LAKEFRONT_BLOCK_SUNYSHORE_STATE") + ", "
                + Flags("FLAG_HIDE_VALOR_LAKEFRONT_COLLECTOR", "FLAG_HIDE_VERITY_CAVERN_MESPRIT", "FLAG_HIDE_VALOR_CAVERN_AZELF", "FLAG_HIDE_ACUITY_CAVERN_UXIE"));
            TalkTo("RowanLab", "prof_rowan");
            ReadTo("Cynthia"); Frames(4); Shot("sy09_lab_regards_to_cynthia");
            ReadTo(null);
            TalkTo("RowanLab", "counterpart");
            ReadTo("Sinnoh began with"); Frames(4); Shot("sy0a_lab_the_assistant_wonders");
            ReadTo(null);

            // ---- the road east, open
            ComeThrough("Sinnoh", 721, 790, Direction.Right);
            Frames(30); Shot("sy0b_lakefront_the_road_east_open");
            Console.WriteLine($"valor lakefront after: collector there {Someone("Sinnoh", "collector", "valor_lakefront") != null}");
        }
        else
        {
            story.SetVar("VAR_EXITED_DISTORTION_WORLD_STATE", 3);
            story.SetVar("VAR_SANDGEM_TOWN_LAB_STATE", 3);
            story.SetVar("VAR_VALOR_LAKEFRONT_BLOCK_SUNYSHORE_STATE", 1);
            story.Set("FLAG_HIDE_VALOR_LAKEFRONT_COLLECTOR");
            foreach (string flag in new[] { "FLAG_HIDE_VERITY_CAVERN_MESPRIT", "FLAG_HIDE_VALOR_CAVERN_AZELF", "FLAG_HIDE_ACUITY_CAVERN_UXIE", "FLAG_HIDE_SANDGEM_TOWN_LAB_COUNTERPART" }) story.Unset(flag);
            Frames(2);
        }

        if (stage <= 1)
        {
            // ---- Lake Verity: the island and the cavern's mouth; Mesprit in its cavern, gone, and the professor after it
            ComeThrough("LakeVerityGalactic", 32, 34, Direction.Up);
            Frames(30); Shot("sy10_lake_verity_the_island");
            ComeThrough("VerityCavern", 14, 28, Direction.Up);
            Frames(30); Shot("sy11_verity_cavern_from_its_mouth");
            At("VerityCavern", 12, 16, Direction.Up); Frames(30); Shot("sy12_verity_cavern_mesprit");
            Console.WriteLine($"verity cavern: mesprit there {Someone("VerityCavern", "mesprit") != null}, the professor there {Someone("VerityCavern", "prof_rowan") != null}");
            TalkTo("VerityCavern", "mesprit");
            ReadTo("Kyuoon"); Frames(4); Shot("sy13_verity_cavern_mesprits_cry");
            ReadTo("flew off"); Frames(4); Shot("sy14_verity_cavern_mesprit_gone");
            Onward(100); Shot("sy15_verity_cavern_the_professor_comes_in");
            ReadTo("that was Mesprit"); Frames(4); Shot("sy16_verity_cavern_the_professor");
            ReadTo("riddle left to solve"); Frames(4); Shot("sy17_verity_cavern_a_riddle_left");
            ReadTo("Marking Map on your"); Frames(4); Shot("sy18_verity_cavern_the_marking_map");
            ReadTo("Look after yourself"); Frames(4);
            Onward(40); Shot("sy19_verity_cavern_the_professor_leaves");
            ReadTo(null); Frames(20);
            Shot("sy1a_verity_cavern_empty");
            Where("verity cavern");
            var roaming = game.Encounters.Roamers[Roamers.SlotOf("Mesprit")!.Value];
            Console.WriteLine($"mesprit: roaming {roaming.Active} on {Roamers.PlaceOf(roaming)}, level {roaming.Level}, {Vars("VAR_ROAMING_MESPRIT_STATE")}, "
                + Flags("FLAG_HIDE_VERITY_CAVERN_MESPRIT", "FLAG_HIDE_VERITY_CAVERN_PROF_OAK", "FLAG_FIRST_ARRIVAL_VERITY_CAVERN"));

            // ---- Lake Valor, full again: its island, Azelf in its cavern, the battle, the cavern empty
            ComeThrough("Sinnoh", 718, 761, Direction.Left);
            Console.WriteLine($"valor lakefront: way in leads to {game.Map.GetWarpAt(717, 760)?.TargetMap}");
            ComeThrough("LakeValor", 32, 34, Direction.Up);
            Frames(30); Shot("sy20_lake_valor_full_again");
            ComeThrough("ValorCavern", 14, 28, Direction.Up);
            At("ValorCavern", 16, 17, Direction.Up); Frames(30); Shot("sy21_valor_cavern_azelf");
            TalkTo("ValorCavern", "azelf");
            ReadTo("Kyuuoh"); Frames(4); Shot("sy22_valor_cavern_azelfs_cry");
            IntoBattle("Azelf");
            ToMainMenu(game.Battle); Shot("sy23_valor_cavern_azelfs_battle");
            Console.WriteLine($"azelf's battle: {game.Battle.EnemyPokemon?.Species.Name} level {game.Battle.EnemyPokemon?.Level}");
            Win();
            ReadTo("slipped away"); Frames(4); Shot("sy24_valor_cavern_azelf_slipped_away");
            ReadTo(null); Frames(20);
            Shot("sy25_valor_cavern_empty");
            Console.WriteLine("valor cavern: " + Flags("FLAG_HIDE_VALOR_CAVERN_AZELF", "FLAG_CAUGHT_AZELF", "FLAG_FIRST_ARRIVAL_VALOR_CAVERN"));

            // ---- Lake Acuity: its island, Uxie in its cavern, the battle
            ComeThrough("LakeAcuityGalactic", 32, 34, Direction.Up);
            Frames(30); Shot("sy30_lake_acuity_the_island");
            ComeThrough("AcuityCavern", 14, 28, Direction.Up);
            At("AcuityCavern", 14, 13, Direction.Up); Frames(30); Shot("sy31_acuity_cavern_uxie");
            TalkTo("AcuityCavern", "uxie");
            ReadTo("Kyuuuhn"); Frames(4); Shot("sy32_acuity_cavern_uxies_cry");
            IntoBattle("Uxie");
            ToMainMenu(game.Battle); Shot("sy33_acuity_cavern_uxies_battle");
            Console.WriteLine($"uxie's battle: {game.Battle.EnemyPokemon?.Species.Name} level {game.Battle.EnemyPokemon?.Level}");
            Win();
            ReadTo("slipped away"); Frames(4); Shot("sy34_acuity_cavern_uxie_slipped_away");
            ReadTo(null); Frames(20);
            Console.WriteLine("acuity cavern: " + Flags("FLAG_HIDE_ACUITY_CAVERN_UXIE", "FLAG_CAUGHT_UXIE", "FLAG_FIRST_ARRIVAL_ACUITY_CAVERN"));
        }

        if (stage <= 2)
        {
            // ---- Route 222: the rich boy and his Technical Machine
            var boy = Someone("Sinnoh", "rich_boy", "route_222");
            if (boy != null)
            {
                ComeThrough("Sinnoh", boy.GridX, boy.GridY + 2, Direction.Up);
                Frames(30); Shot("sy40_route_222_the_rich_boy");
                TalkTo("Sinnoh", "rich_boy", "route_222");
                ReadTo("Technical Machine"); Frames(4); Shot("sy41_route_222_he_hands_it_out");
                ReadTo("TM56"); Frames(4); Shot("sy42_route_222_tm56");
                ReadTo(null);
                Console.WriteLine($"route 222: TM56 {bag.GetQuantity(ItemDatabase.Get("TM56")!)}, {Flags("FLAG_RECEIVED_ROUTE_222_TM56")}");
            }
            else Console.WriteLine("  !! no rich boy on Route 222");

            // ---- Sunyshore City: Flint at the gate
            ComeThrough("Sinnoh", 833, 790, Direction.Right);
            Until(() => engine.ScriptRunning, "Flint's scene", 60);
            for (int i = 0; i < 40; i++) Quick();
            Frames(2); Shot("sy43_sunyshore_flint_comes_up");
            ReadTo("You there"); Frames(4); Shot("sy44_sunyshore_flint");
            ReadTo("Elite Four"); Frames(4); Shot("sy45_sunyshore_one_of_the_elite_four");
            ReadTo("Gym Leader"); Frames(4); Shot("sy46_sunyshore_about_the_gym_leader");
            ReadTo("riding on you"); Frames(4); Shot("sy47_sunyshore_counting_on_you");
            Onward(110); Shot("sy48_sunyshore_flint_goes_on_ahead");
            ReadTo(null); Frames(20);
            Shot("sy49_sunyshore_the_gate");
            Where("the gate");
            Console.WriteLine($"sunyshore: {Vars("VAR_SUNYSHORE_CITY_STATE", "VAR_SANDGEM_TOWN_LAB_STATE")}, {Flags("FLAG_HIDE_SUNYSHORE_CITY_FLINT", "FLAG_HIDE_SANDGEM_TOWN_LAB_COUNTERPART")}, "
                + $"flint at {Someone("Sinnoh", "flint", "sunyshore_city")?.GridX},{Someone("Sinnoh", "flint", "sunyshore_city")?.GridY}");

            // ---- Flint before the Gym's door, Volkner not back
            ComeThrough("Sinnoh", 845, 751, Direction.Up);
            Frames(30); Shot("sy4a_sunyshore_flint_before_the_gym");
            TalkTo("Sinnoh", "flint", "sunyshore_city");
            ReadTo("not back yet"); Frames(4); Shot("sy4b_sunyshore_volkner_isnt_back");
            ReadTo(null);
            Where("before the gym");

            // ---- the city's Pokémon Center and Mart, with their people
            ComeThrough("SunyshorePokemonCenter", 8, 12, Direction.Up);
            Frames(30); Shot("sy4c_pokemon_center");
            TalkTo("SunyshorePokemonCenter", "ninja_boy", null, -1, 0);
            ReadTo("seven Gym Badges"); Frames(4); Shot("sy4d_pokemon_center_the_boy_counts_badges");
            ReadTo(null);
            ComeThrough("SunyshorePokeMart", 3, 11, Direction.Up);
            Frames(30); Shot("sy4e_mart");

            // ---- the lab once the player has been to Sunyshore: the professor behind his desk, the assistant gone
            ComeThrough("RowanLab", 7, 15, Direction.Up);
            ReadTo(null, 300); Frames(30); Shot("sy4f_lab_afterwards");
            Console.WriteLine($"the lab afterwards: the assistant there {Someone("RowanLab", "counterpart") != null}, the professor at {Someone("RowanLab", "prof_rowan")?.GridX},{Someone("RowanLab", "prof_rowan")?.GridY}");
        }
        else
        {
            story.SetVar("VAR_SUNYSHORE_CITY_STATE", 1);
            story.SetVar("VAR_SANDGEM_TOWN_LAB_STATE", 4);
            story.Set("FLAG_HIDE_SANDGEM_TOWN_LAB_COUNTERPART");
            Frames(2);
        }

        if (stage <= 3)
        {
            // ---- the Vista Lighthouse: its door, the lift, the gallery
            ComeThrough("Sinnoh", 886, 792, Direction.Up);
            Frames(30); Shot("sy50_lighthouse_the_door");
            ComeThrough("VistaLighthouseElevator", 3, 6, Direction.Up);
            Until(() => engine.ScriptRunning, "the lift's ride", 60);
            for (int i = 0; i < 70; i++) Quick();
            Frames(2); Shot("sy51_lighthouse_the_lift_on_its_way_up");
            Until(() => game.Map.Name == "VistaLighthouse" && State() == GameState.Overworld && !engine.ScriptRunning, "the gallery", 1500);
            Frames(30); Shot("sy52_lighthouse_the_gallery");
            Where("the gallery");
            Console.WriteLine($"lighthouse: {Vars("VAR_VISTA_LIGHTHOUSE_FLOOR")}, volkner there {Someone("VistaLighthouse", "volkner") != null}");

            // ---- Volkner before the binoculars
            At("VistaLighthouse", 4, 5, Direction.Right); Frames(20); Shot("sy53_lighthouse_volkner_looks_out");
            TalkTo("VistaLighthouse", "volkner");
            ReadTo("So you're the next one"); Frames(4); Shot("sy54_lighthouse_volkner");
            ReadTo("made up my mind"); Frames(4); Shot("sy55_lighthouse_he_may_leave_for_the_league");
            ReadTo("everything I have"); Frames(4); Shot("sy56_lighthouse_everything_he_has");
            Onward(75); Shot("sy57_lighthouse_volkner_walks_round");
            Until(() => !engine.ScriptRunning, "Volkner gone", 1500);
            Frames(20); Shot("sy58_lighthouse_volkner_gone");
            Where("the gallery, volkner gone");
            Console.WriteLine("lighthouse: " + Flags("FLAG_VOLKNER_RETURNED_TO_GYM", "FLAG_HIDE_VISTA_LIGHTHOUSE_VOLKNER"));

            // ---- the binoculars, the two visitors, and the lift's car in the shaft
            At("VistaLighthouse", 6, 4, Direction.Up); Frames(10);
            game.Interact();
            ReadTo("looked through"); Frames(4); Shot("sy59_lighthouse_the_binoculars");
            ReadTo("wall of rock"); Frames(4); Shot("sy5a_lighthouse_the_view");
            ReadTo("Pokémon League"); Frames(4); Shot("sy5b_lighthouse_the_league_across_the_sea");
            ReadTo(null);
            TalkTo("VistaLighthouse", "sailor", null, 0, 1);
            ReadTo("safe home"); Frames(4); Shot("sy5c_lighthouse_the_sailor");
            ReadTo(null);
            At("VistaLighthouse", 6, 10, Direction.Up); Frames(30); Shot("sy5d_lighthouse_the_car_in_the_shaft");
            At("VistaLighthouse", 2, 7, Direction.Right); Frames(30); Shot("sy5e_lighthouse_west_of_the_shaft");

            // ---- down again: into the car, and out at the lighthouse's foot
            StepOnto("VistaLighthouse", 6, 9, Direction.Up);
            Until(() => game.Map.Name == "VistaLighthouseElevator", "the car", 300);
            Until(() => engine.ScriptRunning, "the lift's ride", 60);
            for (int i = 0; i < 70; i++) Quick();
            Frames(2); Shot("sy5f_lighthouse_the_lift_on_its_way_down");
            Until(() => game.Map.Name == "Sinnoh" && State() == GameState.Overworld && !engine.ScriptRunning, "the city", 1500);
            Frames(30); Shot("sy5g_lighthouse_out_at_its_foot");
            Where("down from the lighthouse");
            Console.WriteLine($"lighthouse: {Vars("VAR_VISTA_LIGHTHOUSE_FLOOR")}");
        }
        else
        {
            story.Set("FLAG_VOLKNER_RETURNED_TO_GYM");
            story.Set("FLAG_HIDE_VISTA_LIGHTHOUSE_VOLKNER");
            Frames(2);
        }

        if (stage <= 4)
        {
            // ---- Flint leaves the Gym's door
            ComeThrough("Sinnoh", 845, 751, Direction.Up);
            TalkTo("Sinnoh", "flint", "sunyshore_city");
            ReadTo("back at last"); Frames(4); Shot("sy60_sunyshore_volkner_is_back");
            ReadTo("Pokémon League"); Frames(4); Shot("sy61_sunyshore_flint_will_wait_at_the_league");
            Onward(80); Shot("sy62_sunyshore_flint_leaves");
            ReadTo(null); Frames(20);
            Shot("sy63_sunyshore_the_gyms_door_free");
            Where("before the gym, flint gone");
            Console.WriteLine($"sunyshore: {Flags("FLAG_HIDE_SUNYSHORE_CITY_FLINT")}, flint there {Someone("Sinnoh", "flint", "sunyshore_city") != null}");

            // ---- the Gym: Volkner and the Beacon Badge (the Gym itself is plan 01 · M9's; `gyms sunyshore` shows its gears)
            TalkTo("SunyshoreGymRoom3", "volkner");
            ReadTo("A challenger"); Frames(4); Shot("sy64_gym_volkner");
            IntoBattle("Volkner");
            ToMainMenu(game.Battle); Shot("sy65_gym_volkners_battle");
            Win();
            ReadTo("Beacon Badge from Volkner"); Frames(4); Shot("sy66_gym_the_beacon_badge");
            ReadTo("Charge Beam"); Frames(4); Shot("sy67_gym_tm57");
            ReadTo(null); Frames(10);
            Console.WriteLine($"gym: beacon badge {story.HasBadge(Badge.Beacon)}, TM57 {bag.GetQuantity(ItemDatabase.Get("TM57")!)}, {Vars("VAR_SUNYSHORE_CITY_STATE")}");
        }
        else
        {
            story.Set("FLAG_HIDE_SUNYSHORE_CITY_FLINT");
            story.GiveBadge(Badge.Beacon);
            story.Set("FLAG_RECEIVED_VOLKNER_TM57");
            story.SetVar("VAR_SUNYSHORE_CITY_STATE", 2);
            Frames(2);
        }

        // ---- the beach: Jasmine, the rival running up, HM07
        ComeThrough("Sinnoh", 855, 746, Direction.Up);
        Frames(30); Shot("sy70_beach_jasmine_by_the_water");
        StepOnto("Sinnoh", 854, 743, Direction.Up);
        for (int i = 0; i < 30; i++) Quick();
        Frames(2); Shot("sy71_beach_jasmine_notices");
        for (int i = 0; i < 50; i++) Quick();
        Frames(2); Shot("sy71b_beach_the_rival_comes_running");
        ReadTo("Eight Badges?!"); Frames(4); Shot("sy72_beach_all_eight_badges");
        ReadTo("wait and see"); Frames(4); Shot("sy73_beach_the_toughest_trainer");
        ReadTo("battle today"); Frames(4); Shot("sy74_beach_no_battle_today");
        ReadTo("my back"); Frames(4);
        Onward(22); Shot("sy75_beach_the_rival_runs_off");
        ReadTo("friend of yours"); Frames(4); Shot("sy76_beach_jasmine_comes_down");
        ReadTo("small thing"); Frames(4); Shot("sy77_beach_her_thanks");
        ReadTo("HM07"); Frames(4); Shot("sy78_beach_hm07");
        ReadTo("Waterfall"); Frames(4); Shot("sy79_beach_waterfall");
        ReadTo(null); Frames(20);
        Shot("sy7a_beach_afterwards");
        Where("the beach");
        Console.WriteLine($"the beach: HM07 {bag.GetQuantity(ItemDatabase.Get("HM07")!)}, {Vars("VAR_SUNYSHORE_CITY_STATE")}, "
            + Flags("FLAG_RECEIVED_SUNYSHORE_CITY_HM07", "FLAG_HIDE_SUNYSHORE_CITY_RIVAL")
            + $", rival there {Someone("Sinnoh", "rival", "sunyshore_city") != null}, jasmine at {Someone("Sinnoh", "jasmine", "sunyshore_city")?.GridX},{Someone("Sinnoh", "jasmine", "sunyshore_city")?.GridY}");
        TalkTo("Sinnoh", "jasmine", "sunyshore_city");
        ReadTo("become stronger"); Frames(4); Shot("sy7b_beach_jasmine_afterwards");
        ReadTo(null);
    }
}
