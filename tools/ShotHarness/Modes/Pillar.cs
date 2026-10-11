partial class Harness
{
    // The ninth chapter (plan 02 · S12; not part of `all`), played by its own scripts: Team Galactic on the climb
    // through Mt. Coronet, the cave painting before and after it is broken open, Looker and the Black Flute; Spear
    // Pillar, the grunts' double battle, Commander Mars and Commander Jupiter closing in, the rival beside the player
    // and the tag battle, Cyrus calling Dialga and Palkia, the shadow that takes him; the torn pillar, Cynthia, the
    // three Pokémon of the lakes and her question; the Distortion World floor by floor: the portal and Cynthia on 1F,
    // Mesprit on B1F, Cynthia on B2F, Cyrus on B3F, Giratina's shadow on B4F, the three boulders on B5F and their pits
    // on B6F (a wrong pit and the three right ones), Cynthia and the stepping stones, Cyrus's battle on B7F, Giratina's
    // room, its battle and what follows, the portal out; Sendoff Spring, Cynthia before Turnback Cave's mouth, and
    // what the chapter leaves in Team Galactic's lobby and in Turnback Cave. PILLAR_FROM=summit starts at Spear
    // Pillar, PILLAR_FROM=distortion on the torn pillar, PILLAR_FROM=puzzle on B5F, PILLAR_FROM=cyrus on B7F,
    // PILLAR_FROM=giratina in its room, PILLAR_FROM=spring at the portal out, the story set as the scenes before
    // would have left it.
    public void PillarMode()
    {
        engine.StartNewGame();
        PastTheOpening();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var story = game.Story;
        var bag = game.Bag;
        var team = game.Party;
        var infernape = new Pokemon(PokemonDatabase.Get("Infernape")!, 62);
        infernape.Moves.Clear();
        foreach (string move in new[] { "Flamethrower", "Strength", "Close Combat", "Rock Climb" }) infernape.Moves.Add(new Move(MoveDatabase.Get(move)!));
        team.Members.Insert(0, infernape);
        // The Badges of the chapters before, and the story as the button in Team Galactic's control room leaves it
        // (GalacticHQControlRoom.txt): the painting on Mt. Coronet's second floor broken, the grunt back in its north hall
        foreach (var badge in new[] { Badge.Coal, Badge.Forest, Badge.Relic, Badge.Cobble, Badge.Fen, Badge.Mine, Badge.Icicle }) story.GiveBadge(badge);
        foreach (string flag in new[] { "FLAG_TEAM_GALACTIC_LEFT_LAKE_VERITY", "FLAG_HIDE_MT_CORONET_1F_NORTH_ROOM_1_GRUNTS_M", "FLAG_HIDE_GALACTIC_HQ_4F_CYRUS" }) story.Set(flag);

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
        // Reads on to the next question and leaves it open, for a shot and an answer of the mode's own
        void ToQuestion(string what, int most = 3000)
        {
            for (int i = 0; i < most && !engine.Choice.IsOpen; i++)
            {
                if (Box().IsActive && !Box().IsCurrentLineComplete) { Box().FinishLine(); Quick(); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Quick(); }
                else Quick();
            }
            if (!engine.Choice.IsOpen) Console.WriteLine($"  !! never asked: {what}");
            Frames(10);
        }
        // Reads on through whatever is still being said until a scene has brought the player to another map and that
        // map's own arrival has begun
        void OnTo(string map, int most = 3000)
        {
            for (int i = 0; i < most && !(game.Map.Name == map && engine.ScriptRunning); i++)
            {
                if (Box().IsActive && !Box().IsCurrentLineComplete) { Box().FinishLine(); Quick(); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Quick(); }
                else Quick();
            }
            if (game.Map.Name != map) Console.WriteLine($"  !! never came to {map}");
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
        // A jump over a gap of the Distortion World, from the stone before it, onto a stone that starts a scene
        void JumpOnto(string map, int fromX, int fromY, Direction way)
        {
            At(map, fromX, fromY, way);
            engine.Steering = (way, false);
            Until(() => engine.ScriptRunning, $"the scene beyond {fromX},{fromY}", 240);
            engine.Steering = null;
        }
        // Comes into a map through a warp onto a tile, as the game does: its own script, then the trigger stepped out onto
        void ComeThrough(string map, int x, int y, Direction facing)
        {
            At(map, x, y, facing);
            game.ArriveOnMap();
            game.SteppedOutOfWarp = true;
            Frames(2);
        }
        NPC? Someone(string map, string key) => MapDatabase.Get(map).NPCs.FirstOrDefault(n => n.Key == key);
        // Talks to someone of a map from a tile beside them (below unless told otherwise)
        void TalkTo(string map, string key, int dx = 0, int dy = 1)
        {
            var npc = MapDatabase.Get(map).NPCs.First(n => n.Key == key);
            var facing = (dx, dy) switch { (0, 1) => Direction.Up, (0, -1) => Direction.Down, (1, 0) => Direction.Left, _ => Direction.Right };
            At(map, npc.GridX + dx, npc.GridY + dy, facing);
            game.Interact();
        }
        // Pushes the boulder ahead until it stands on a tile, walking on behind it as a player does, with Strength in force
        void PushTo(NPC boulder, Direction way, int x, int y)
        {
            story.Set(FieldMoveRules.StrengthFlag);
            var (dx, dy) = FieldMovement.Delta(way);
            At(game.Map.Name, boulder.GridX - dx, boulder.GridY - dy, way);
            engine.Steering = (way, false);
            Until(() => (boulder.GridX, boulder.GridY) == (x, y) || engine.ScriptRunning, $"the boulder at {x},{y}", 900);
            engine.Steering = null;
            Until(() => !Me().IsPushing && !Me().IsMoving || engine.ScriptRunning, "the push to end", 120);
        }
        void Where(string what) => Console.WriteLine($"{what}: player at {Me().GridX},{Me().GridY} of {game.Map.Name}");
        string Flags(params string[] flags) => string.Join(", ", flags.Select(f => $"{f.Replace("FLAG_", "")} {story.Has(f)}"));
        int Progress() => story.Var("VAR_DISTORTION_WORLD_PROGRESS");

        Frames(5);
        ReadTo(null, 600);

        string from = Environment.GetEnvironmentVariable("PILLAR_FROM") ?? "";
        int stage = from switch { "" => 0, "summit" => 1, "distortion" => 2, "puzzle" => 3, "cyrus" => 4, "giratina" => 5, "spring" => 6, _ => 0 };

        if (stage == 0)
        {
            // ---- Mt. Coronet before Team Galactic comes through: the painting over the hole
            ComeThrough("MtCoronet2F", 7, 25, Direction.Up);
            Frames(30); Shot("sp01_coronet_2f_the_cave_painting");
            At("MtCoronet2F", 7, 24, Direction.Up);
            game.Interact();
            ReadTo("very old"); Frames(4); Shot("sp02_coronet_2f_the_painting_read");
            ReadTo(null);
            Console.WriteLine($"2F before: the hole solid {game.Map.IsSolid(7, 23)}, way {game.Map.GetWarpAt(7, 23)?.TargetMap ?? "none"}, {Flags("FLAG_HIDE_MT_CORONET_2F_CAVE_PAINTING", "FLAG_HIDE_MT_CORONET_2F_CAVE_PAINTING_SHARDS", "FLAG_HIDE_MT_CORONET_2F_LOOKER")}");
        }

        // The button in the control room (GalacticHQControlRoom.txt)
        foreach (string flag in new[] { "FLAG_FREED_GALACTIC_HQ_POKEMON", "FLAG_HIDE_GALACTIC_HQ_CONTROL_ROOM_LAKE_GUARDIANS", "FLAG_HIDE_GALACTIC_HQ_TEAM_GALACTIC", "FLAG_HIDE_MT_CORONET_2F_CAVE_PAINTING" }) story.Set(flag);
        story.Unset("FLAG_HIDE_MT_CORONET_1F_NORTH_ROOM_1_GRUNT_F");
        story.Unset("FLAG_HIDE_MT_CORONET_2F_CAVE_PAINTING_SHARDS");
        if (story.Var("VAR_MT_CORONET_2F_STATE") == 0) story.SetVar("VAR_MT_CORONET_2F_STATE", 1);

        if (stage == 0)
        {
            // ---- the climb: the grunt in the north hall, the hole broken open, Looker, the grunts of the upper floors
            ComeThrough("MtCoronet1FNorthRoom1", 19, 43, Direction.Up);
            Frames(30); Shot("sp03_coronet_north_hall_the_grunt");
            TalkTo("MtCoronet1FNorthRoom1", "grunt_f");
            ReadTo("no way up"); Frames(4); Shot("sp04_coronet_north_hall_no_way_up");
            ReadTo(null);

            ComeThrough("MtCoronet2F", 14, 25, Direction.Left);
            Frames(30); Shot("sp05_coronet_2f_the_hole_and_the_shards");
            StepOnto("MtCoronet2F", 11, 25, Direction.Left);
            Until(() => Someone("MtCoronet2F", "looker") != null, "Looker on the floor", 300);
            for (int i = 0; i < 40; i++) Quick();
            Frames(2); Shot("sp06_coronet_2f_looker_comes_running");
            ReadTo("It is I, Looker"); Frames(4); Shot("sp07_coronet_2f_looker");
            ReadTo("Observe that hole"); Frames(4); Shot("sp08_coronet_2f_observe_that_hole");
            ReadTo("far above me"); Frames(4); Shot("sp09_coronet_2f_looker_owns_up");
            ReadTo("Black Flute"); Frames(4); Shot("sp10_coronet_2f_the_black_flute");
            ReadTo(null); Frames(10);
            Console.WriteLine($"2F: state {story.Var("VAR_MT_CORONET_2F_STATE")}, Black Flute {bag.GetQuantity(ItemDatabase.Get("Black Flute")!)}, {Flags("FLAG_RECEIVED_MT_CORONET_2F_BLACK_FLUTE", "FLAG_HIDE_MT_CORONET_2F_LOOKER")}");
            TalkTo("MtCoronet2F", "cave_painting_shards_left", 0, 1);
            ReadTo("Broken stone"); Frames(4); Shot("sp11_coronet_2f_the_shards_read");
            ReadTo(null);
            At("MtCoronet2F", 7, 24, Direction.Up); Frames(20); Shot("sp12_coronet_2f_before_the_hole");
            Console.WriteLine($"2F after: the hole solid {game.Map.IsSolid(7, 23)}, way {game.Map.GetWarpAt(7, 23)?.TargetMap ?? "none"} {game.Map.GetWarpAt(7, 23)?.TargetX},{game.Map.GetWarpAt(7, 23)?.TargetY}");

            ComeThrough("MtCoronet1FTunnelRoom", 16, 16, Direction.Up);
            Frames(30); Shot("sp13_coronet_tunnel_the_lost_grunt");
            TalkTo("MtCoronet1FTunnelRoom", "grunt_m");
            ReadTo("hopelessly"); Frames(4); Shot("sp14_coronet_tunnel_hopelessly_lost");
            ReadTo(null);
            var guard = MapDatabase.Get("MtCoronet6F").NPCs.First(n => n.Key == "galactic_grunt");
            At("MtCoronet6F", guard.GridX - 3, guard.GridY + 2, Direction.Up); Frames(30); Shot("sp15_coronet_6f_the_last_guard");
            TalkTo("MtCoronet6F", "galactic_grunt", -1, 0);
            IntoBattle("the last guard");
            ToMainMenu(game.Battle); Shot("sp16_coronet_6f_the_guards_battle");
            Win();
            ReadTo(null);
            Console.WriteLine($"6F: the guard beaten {story.HasDefeated("galactic_grunt_mt_coronet_6f")}, way out leads to {MapDatabase.Get("MtCoronet6F").GetWarpAt(7, 5)?.TargetMap}");
        }
        else
        {
            story.SetVar("VAR_MT_CORONET_2F_STATE", 2);
            story.Unset("FLAG_HIDE_MT_CORONET_2F_LOOKER");
        }

        if (stage <= 1)
        {
            // ---- Spear Pillar: the grunts at the head of the steps
            ComeThrough("SpearPillar", 31, 52, Direction.Up);
            Frames(30); Shot("sp20_spear_pillar");
            StepOnto("SpearPillar", 31, 48, Direction.Up);
            ReadTo("Stop right there"); Frames(4); Shot("sp21_pillar_the_grunts_stop_the_player");
            IntoBattle("the grunts' double battle");
            ToMainMenu(game.Battle); Shot("sp22_pillar_the_grunts_battle");
            Win();
            ReadTo("kept you busy"); Frames(4); Shot("sp23_pillar_the_grunts_beaten");
            ReadTo(null);
            Console.WriteLine($"spear pillar: state {story.Var("VAR_SPEAR_PILLAR_STATE")}");

            // ---- Commander Mars and Commander Jupiter, and the rival
            At("SpearPillar", 31, 36, Direction.Up); Frames(30); Shot("sp24_pillar_the_commanders_before_the_altar");
            StepOnto("SpearPillar", 31, 32, Direction.Up);
            ReadTo("through me first"); Frames(4); Shot("sp25_pillar_mars");
            ReadTo("take turns"); Frames(4); Shot("sp26_pillar_jupiter");
            Box().Advance();
            for (int i = 0; i < 40; i++) Quick();
            Frames(2); Shot("sp27_pillar_the_rival_runs_up");
            ReadTo("start without me"); Frames(4); Shot("sp28_pillar_the_rival_beside_the_player");
            ReadTo("take them on together"); Frames(4); Shot("sp29_pillar_two_against_two");
            IntoBattle("the tag battle");
            ToMainMenu(game.Battle); Shot("sp30_pillar_the_tag_battle");
            Win();
            ReadTo("We did it"); Frames(4); Shot("sp31_pillar_the_rival_after");
            ReadTo("full health"); Frames(4); Shot("sp32_pillar_the_team_healed");
            ReadTo("your show"); Frames(4); Shot("sp33_pillar_its_your_show");
            Box().Advance();
            for (int i = 0; i < 40; i++) Quick();
            Frames(2); Shot("sp34_pillar_the_rival_leaves");

            // ---- Cyrus at the altar
            ReadTo("Everything is ready"); Frames(4); Shot("sp35_pillar_cyrus_at_the_altar");
            ReadTo("Red Chain"); Frames(4); Shot("sp36_pillar_the_red_chains");
            ReadTo("Dialga"); Frames(4); Shot("sp37_pillar_cyrus_calls_dialga");
            ReadTo("Palkia"); Frames(20); Shot("sp38_pillar_dialga_has_come");
            ReadTo("At last"); Frames(20); Shot("sp39_pillar_dialga_and_palkia");
            ReadTo("without heart"); Frames(4); Shot("sp40_pillar_a_world_without_heart");
            ReadTo("Something is angry"); Frames(4); Shot("sp41_pillar_something_is_angry");
            ReadTo("comes as a shadow"); Frames(20); Shot("sp42_pillar_the_shadow");
            ReadTo("you cannot"); Frames(4); Shot("sp43_pillar_cyrus_defies_it");
            ReadTo("sank into the floor"); Frames(4); Shot("sp44_pillar_cyrus_is_taken");
            // What the scene leaves is told once it has brought the player to the torn pillar (below)
            OnTo("SpearPillarDistorted");
            Console.WriteLine($"spear pillar: state {story.Var("VAR_SPEAR_PILLAR_STATE")}, dialga seen {game.Pokedex.IsSeen(PokemonDatabase.Get("Dialga")!.DexNumber)}, palkia seen {game.Pokedex.IsSeen(PokemonDatabase.Get("Palkia")!.DexNumber)}, "
                + Flags("FLAG_SPEAR_PILLAR_IS_DISTORTED", "FLAG_UNLOCKED_VS_SEEKER_LVL_3", "FLAG_HIDE_SPEAR_PILLAR_CYRUS", "FLAG_HIDE_SPEAR_PILLAR_MARS_JUPITER", "FLAG_HIDE_SPEAR_PILLAR_GRUNTS"));
        }
        else
        {
            foreach (string flag in new[] { "FLAG_HIDE_SPEAR_PILLAR_GRUNTS", "FLAG_HIDE_SPEAR_PILLAR_MARS_JUPITER", "FLAG_HIDE_SPEAR_PILLAR_CYRUS", "FLAG_UNLOCKED_VS_SEEKER_LVL_3", "FLAG_SPEAR_PILLAR_IS_DISTORTED" })
                story.Set(flag);
            story.SetVar("VAR_SPEAR_PILLAR_STATE", 3);
            story.SetVar("VAR_SPEAR_PILLAR_DISTORTED_STATE", 1);
            if (stage == 2) ComeThrough("SpearPillarDistorted", 30, 30, Direction.Up);
        }

        if (stage <= 2)
        {
            // ---- the torn pillar: Cynthia, the three Pokémon of the lakes, her question
            ReadTo("too late"); Frames(4); Shot("sp50_torn_pillar_cynthia_too_late");
            Where("the torn pillar");
            Box().Advance();
            Until(() => Someone("SpearPillarDistorted", "azelf") != null, "the three over the tear", 600);
            Frames(30); Shot("sp51_torn_pillar_the_three_of_the_lakes");
            ReadTo("as fast as I could"); Frames(4); Shot("sp52_torn_pillar_cynthia_beside_the_player");
            ReadTo("Giratina"); Frames(4); Shot("sp53_torn_pillar_giratina_named");
            ReadTo("come apart"); Frames(4); Shot("sp54_torn_pillar_the_worlds_bleed");
            ToQuestion("her question");
            Shot("sp55_torn_pillar_are_you_ready");
            engine.Choice.Move(1); Frames(4);
            engine.Choice.Confirm();
            ReadTo("wait for you here"); Frames(4); Shot("sp56_torn_pillar_she_waits");
            ReadTo(null);
            Console.WriteLine($"torn pillar: state {story.Var("VAR_SPEAR_PILLAR_DISTORTED_STATE")}, {Flags("FLAG_HIDE_GALACTIC_HQ_CONTROL_ROOM_CHARON", "FLAG_HIDE_SPEAR_PILLAR_DISTORTED_MESPRIT", "FLAG_HIDE_SPEAR_PILLAR_DISTORTED_CYNTHIA")}, way down leads to {game.Map.GetWarpAt(31, 53)?.TargetMap}");
            At("SpearPillarDistorted", 31, 20, Direction.Up); Frames(30); Shot("sp57_torn_pillar_the_rifts");
            At("SpearPillarDistorted", 29, 18, Direction.Up);
            game.Interact();
            ReadTo("A rift hangs"); Frames(4); Shot("sp58_torn_pillar_a_rift_read");
            ReadTo(null);
            At("SpearPillarDistorted", 31, 36, Direction.Up); Frames(30); Shot("sp59_torn_pillar_team_galactic_left_behind");
            TalkTo("SpearPillarDistorted", "mars");
            ReadTo("Where did he go"); Frames(4); Shot("sp60_torn_pillar_mars");
            ReadTo(null);
            TalkTo("SpearPillarDistorted", "cynthia", -1, 0);
            ToQuestion("her question again");
            engine.Choice.Confirm();
            ReadTo("have to hurry"); Frames(4); Shot("sp61_torn_pillar_into_the_tear");

            // ---- the Distortion World, 1F: Cynthia comes through, Giratina passes, the slab
            OnTo("DistortionWorld1F");
            ReadTo("This place"); Frames(4); Shot("sp70_1f_cynthia_comes_through");
            Where("the distortion world");
            ReadTo("distortion world"); Frames(4); Shot("sp71_1f_a_distortion_world");
            ReadTo("That was Giratina"); Frames(4); Shot("sp72_1f_giratina_passed");
            ReadTo("have to hurry"); Frames(4);
            Box().Advance();
            for (int i = 0; i < 60; i++) Quick();
            Frames(2); Shot("sp73_1f_cynthia_goes_ahead");
            ReadTo(null); Frames(20);
            Shot("sp74_1f_the_portal_behind");
            Console.WriteLine($"1F: progress {Progress()}, cynthia at {Someone("DistortionWorld1F", "cynthia")?.GridX},{Someone("DistortionWorld1F", "cynthia")?.GridY}");
            At("DistortionWorld1F", 34, 30, Direction.Up);
            game.Interact();
            ToQuestion("the portal's question");
            Shot("sp75_1f_the_portal_asks");
            engine.Choice.Move(1); Frames(4);
            engine.Choice.Confirm();
            ReadTo(null);
            JumpOnto("DistortionWorld1F", 19, 39, Direction.Down);
            ReadTo("isn't fixed"); Frames(4); Shot("sp76_1f_the_slab_moves");
            ReadTo(null);
            Console.WriteLine($"1F: progress {Progress()}");

            // ---- B1F: Cynthia at the slab's foot, and Mesprit
            ComeThrough("DistortionWorldB1F", 40, 19, Direction.Down);
            ReadTo("no sign of it"); Frames(4); Shot("sp80_b1f_cynthia_at_the_foot");
            ReadTo("Watch how I go");
            Box().Advance();
            for (int i = 0; i < 110; i++) Quick();
            Frames(2); Shot("sp81_b1f_cynthia_over_the_gaps");
            ReadTo(null); Frames(10);
            Console.WriteLine($"B1F: progress {Progress()}");
            StepOnto("DistortionWorldB1F", 15, 23, Direction.Left);
            ReadTo("Kyauuun"); Frames(4);
            Box().Advance();
            for (int i = 0; i < 50; i++) Quick();
            Frames(2); Shot("sp82_b1f_mesprit");
            for (int i = 0; i < 60; i++) Quick();
            Frames(2); Shot("sp83_b1f_mesprit_up_the_wall");
            ReadTo(null);
            Console.WriteLine($"B1F: progress {Progress()}");

            // ---- B2F: Cynthia by the wall; B3F: Cyrus
            ComeThrough("DistortionWorldB2F", 18, 25, Direction.Up);
            Frames(30); Shot("sp84_b2f_cynthia_by_the_wall");
            TalkTo("DistortionWorldB2F", "cynthia", -1, 0);
            ReadTo("more of it apart"); Frames(4); Shot("sp85_b2f_lets_split_up");
            ReadTo(null);
            Console.WriteLine($"B2F: progress {Progress()}");
            ComeThrough("DistortionWorldB3F", 18, 17, Direction.Down);
            Frames(20);
            engine.Steering = (Direction.Down, false);
            Until(() => engine.ScriptRunning, "Cyrus's scene at the head of the passage", 240);
            engine.Steering = null;
            Until(() => Someone("DistortionWorldB3F", "cyrus") is { GridY: <= 23 }, "Cyrus up the passage", 600);
            Frames(2); Shot("sp86_b3f_cyrus_comes_up_the_passage");
            ToQuestion("his question");
            Shot("sp87_b3f_do_you_know_what_genes_are");
            engine.Choice.Confirm();
            ReadTo("two strands"); Frames(4); Shot("sp88_b3f_two_strands");
            ReadTo("all that matters"); Frames(4); Shot("sp89_b3f_all_that_matters");
            ReadTo(null);
            Console.WriteLine($"B3F: progress {Progress()}");
        }
        else story.SetVar("VAR_DISTORTION_WORLD_PROGRESS", 6);
        story.SetVar("VAR_SPEAR_PILLAR_DISTORTED_STATE", 2);
        story.Unset("FLAG_HIDE_SPEAR_PILLAR_DISTORTED_CYNTHIA");

        if (stage <= 3)
        {
            // ---- B4F: Giratina's shadow
            StepOnto("DistortionWorldB4F", 41, 22, Direction.Left);
            ReadTo("vast shadow"); Frames(4); Shot("sp90_b4f_a_shadow_passes");
            ReadTo(null);

            // ---- B5F: the three boulders, the guardians showing what they are for, and the falls
            ComeThrough("DistortionWorldB5F", 30, 34, Direction.Down);
            Frames(30); Shot("sp91_b5f_the_slabs");
            var b5 = MapDatabase.Get("DistortionWorldB5F");
            StepOnto("DistortionWorldB5F", 29, 19, Direction.Down);
            Until(() => Someone("DistortionWorldB5F", "uxie") != null, "Uxie beyond the hole", 300);
            Frames(20); Shot("sp92_b5f_uxie_shows_its_boulder");
            ReadTo("Uxie hangs"); Frames(4); Shot("sp93_b5f_uxie_and_the_boulder");
            ReadTo(null);
            At("DistortionWorldB5F", 29, 21, Direction.Down);
            story.Set(FieldMoveRules.StrengthFlag);
            game.Interact();
            ReadTo(null, 900);
            var uxieBoulder = b5.NPCs.First(n => n.Key == "uxie_boulder");
            At("DistortionWorldB5F", 29, 21, Direction.Down);
            story.Set(FieldMoveRules.StrengthFlag);
            engine.Steering = (Direction.Down, false);
            Frames(12); Shot("sp94_b5f_the_boulder_pushed_to_the_hole");
            Until(() => engine.ScriptRunning, "the boulder's fall", 300);
            engine.Steering = null;
            ReadTo("dropped through the hole"); Frames(4); Shot("sp95_b5f_the_boulder_falls");
            ReadTo(null);
            StepOnto("DistortionWorldB5F", 43, 33, Direction.Left);
            Until(() => Someone("DistortionWorldB5F", "azelf") != null, "Azelf beyond the hole", 300);
            Frames(20); Shot("sp96_b5f_azelf_shows_its_boulder");
            ReadTo(null);
            PushTo(b5.NPCs.First(n => n.Key == "azelf_boulder"), Direction.Left, 40, 33);
            ReadTo("dropped through the hole"); ReadTo(null);
            StepOnto("DistortionWorldB5F", 23, 33, Direction.Left);
            Until(() => Someone("DistortionWorldB5F", "mesprit") != null, "Mesprit beyond the hole", 300);
            Frames(20); Shot("sp97_b5f_mesprit_shows_its_boulder");
            ReadTo(null);
            PushTo(b5.NPCs.First(n => n.Key == "mesprit_boulder"), Direction.Right, 19, 34);
            ReadTo("dropped through the hole"); ReadTo(null);
            Console.WriteLine($"B5F: {Flags("FLAG_DISTORTION_WORLD_MESPRIT_BOULDER_FELL", "FLAG_DISTORTION_WORLD_AZELF_BOULDER_FELL", "FLAG_DISTORTION_WORLD_UXIE_BOULDER_FELL", "FLAG_HIDE_DISTORTION_WORLD_B6F_MESPRIT_BOULDER")}, boulders left {b5.NPCs.Count(n => n.Obstacle == PropType.StrengthBoulder)}");

            // ---- B6F: Cynthia in the passage, the boulders fallen, a wrong pit, and the three right ones
            ComeThrough("DistortionWorldB6F", 31, 30, Direction.Down);
            var b6 = MapDatabase.Get("DistortionWorldB6F");
            At("DistortionWorldB6F", 29, 41, Direction.Down); Frames(30); Shot("sp100_b6f_cynthia_in_the_passage");
            game.Interact();
            ReadTo("one great puzzle"); Frames(4); Shot("sp101_b6f_one_great_puzzle");
            ReadTo(null);
            At("DistortionWorldB6F", 17, 31, Direction.Up); Frames(30); Shot("sp102_b6f_mesprits_wing");
            At("DistortionWorldB6F", 19, 31, Direction.Up);
            game.Interact();
            ReadTo(null, 900);
            // Straight into the pit above it: the wrong one
            PushTo(b6.NPCs.First(n => n.Key == "mesprit_boulder"), Direction.Up, 19, 29);
            ReadTo("out of sight"); Frames(4); Shot("sp103_b6f_a_wrong_pit");
            ReadTo(null);
            // Down from B5F again, and this time west along the wing to Mesprit's own
            ComeThrough("DistortionWorldB5F", 30, 34, Direction.Down);
            Console.WriteLine($"B6F, a wrong pit: {Flags("FLAG_DISTORTION_WORLD_MESPRIT_BOULDER_FELL", "FLAG_HIDE_DISTORTION_WORLD_B6F_MESPRIT_BOULDER")}, on B5F again {b5.NPCs.Any(n => n.Key == "mesprit_boulder")}");
            PushTo(b5.NPCs.First(n => n.Key == "mesprit_boulder"), Direction.Right, 19, 34);
            ReadTo("dropped through the hole"); ReadTo(null);
            ComeThrough("DistortionWorldB6F", 31, 30, Direction.Down);
            var mespritBoulder = b6.NPCs.First(n => n.Key == "mesprit_boulder");
            PushTo(mespritBoulder, Direction.Left, 15, 30);
            Frames(10); Shot("sp104_b6f_the_boulder_pushed_along_the_wing");
            PushTo(mespritBoulder, Direction.Left, 12, 30);
            At("DistortionWorldB6F", 12, 31, Direction.Up); Frames(20); Shot("sp105_b6f_before_mesprits_pit");
            PushTo(mespritBoulder, Direction.Up, 12, 29);
            ReadTo("Kyauuun"); Frames(4); Shot("sp106_b6f_the_boulder_at_rest_mesprit_cries");
            ReadTo(null); Frames(20);
            Shot("sp107_b6f_mesprit_gone_home");
            var azelfBoulder = b6.NPCs.First(n => n.Key == "azelf_boulder");
            At("DistortionWorldB6F", 44, 28, Direction.Down); Frames(30); Shot("sp108_b6f_azelfs_wing");
            PushTo(azelfBoulder, Direction.Left, 40, 29);
            PushTo(azelfBoulder, Direction.Down, 40, 30);
            ReadTo("Kyaaan"); Frames(4); Shot("sp109_b6f_azelfs_boulder_at_rest");
            ReadTo(null);
            var uxieBelow = b6.NPCs.First(n => n.Key == "uxie_boulder");
            At("DistortionWorldB6F", 31, 17, Direction.Down); Frames(30); Shot("sp110_b6f_uxies_wing");
            PushTo(uxieBelow, Direction.Down, 30, 22);
            PushTo(uxieBelow, Direction.Left, 29, 22);
            ReadTo("Kyuuun"); Frames(4); Shot("sp111_b6f_uxies_boulder_at_rest");
            ReadTo(null);
            Console.WriteLine($"B6F: {Flags("FLAG_DISTORTION_WORLD_MESPRIT_BOULDER_IN_PIT", "FLAG_DISTORTION_WORLD_AZELF_BOULDER_IN_PIT", "FLAG_DISTORTION_WORLD_UXIE_BOULDER_IN_PIT", "FLAG_DISTORTION_WORLD_PUZZLE_FINISHED")}, guardians left {b6.NPCs.Count(n => n.IsPokemon)}");

            // ---- Cynthia once the three are done: the stepping stones
            At("DistortionWorldB6F", 29, 41, Direction.Down);
            game.Interact();
            ReadTo("gone home to their lakes"); Frames(4); Shot("sp112_b6f_the_three_have_gone_home");
            ReadTo("our way down"); Frames(4); Shot("sp113_b6f_stones_out_of_nothing");
            ReadTo(null); Frames(20);
            At("DistortionWorldB6F", 29, 47, Direction.Up); Frames(20); Shot("sp114_b6f_cynthia_by_the_slab");
            Console.WriteLine($"B6F: progress {Progress()}, {Flags("FLAG_DISTORTION_WORLD_STEPPING_STONES")}, cynthia at {Someone("DistortionWorldB6F", "cynthia")?.GridX},{Someone("DistortionWorldB6F", "cynthia")?.GridY}");
        }
        else
        {
            foreach (string who in new[] { "MESPRIT", "AZELF", "UXIE" })
            {
                story.Set($"FLAG_DISTORTION_WORLD_{who}_BOULDER_FELL");
                story.Set($"FLAG_DISTORTION_WORLD_{who}_BOULDER_IN_PIT");
            }
            story.Set("FLAG_DISTORTION_WORLD_PUZZLE_FINISHED");
            story.Set("FLAG_DISTORTION_WORLD_STEPPING_STONES");
            story.SetVar("VAR_DISTORTION_WORLD_PROGRESS", stage switch { 4 => 7, 5 => 10, _ => 14 });
        }

        if (stage <= 4)
        {
            // ---- B7F: Cynthia at the slab's foot, Cyrus on the path, their argument, his battle
            ComeThrough("DistortionWorldB7F", 11, 54, Direction.Down);
            ReadTo("bottom of it"); Frames(4); Shot("sp120_b7f_cynthia_at_the_foot");
            ReadTo(null); Frames(10);
            Console.WriteLine($"B7F: progress {Progress()}");
            At("DistortionWorldB7F", 11, 46, Direction.Up); Frames(30); Shot("sp121_b7f_cynthia_and_cyrus_ahead");
            StepOnto("DistortionWorldB7F", 11, 44, Direction.Up);
            ReadTo("ahead of us"); Frames(4); Shot("sp122_b7f_cynthia_finds_cyrus");
            ReadTo("troubled no one"); Frames(4); Shot("sp123_b7f_why_unmake_a_world");
            ReadTo("my justice"); Frames(4); Shot("sp124_b7f_that_is_my_justice");
            ReadTo(null);
            Console.WriteLine($"B7F: progress {Progress()}");
            TalkTo("DistortionWorldB7F", "cyrus");
            ReadTo("third time"); Frames(4); Shot("sp125_b7f_cyrus_before_the_battle");
            IntoBattle("Cyrus");
            ToMainMenu(game.Battle); Shot("sp126_b7f_cyruss_battle");
            Win();
            ReadTo("Beaten"); Frames(4); Shot("sp127_b7f_cyrus_beaten");
            ReadTo("undo everything");
            Box().Advance();
            for (int i = 0; i < 90; i++) Quick();
            Frames(2); Shot("sp128_b7f_cyrus_walks_out");
            ReadTo("nothing in it to love"); Frames(4); Shot("sp129_b7f_dont_believe_him");
            ReadTo("full health"); Frames(4); Shot("sp130_b7f_cynthia_heals_the_team");
            ReadTo("meet Giratina");
            Box().Advance();
            for (int i = 0; i < 120; i++) Quick();
            Frames(2); Shot("sp131_b7f_up_to_the_last_stretch");
            ReadTo("furious"); Frames(4); Shot("sp132_b7f_it_is_furious");
            ReadTo(null);
            Where("B7F");
            Console.WriteLine($"B7F: progress {Progress()}, cyrus beaten {story.HasDefeated("galactic_boss_cyrus_distortion_world")}, cynthia at {Someone("DistortionWorldB7F", "cynthia")?.GridX},{Someone("DistortionWorldB7F", "cynthia")?.GridY}");
        }

        if (stage <= 5)
        {
            // ---- Giratina's room: its shadows, its coming, the battle and what follows
            ComeThrough("DistortionWorldGiratinaRoom", 15, 23, Direction.Up);
            ReadTo("from everywhere at once"); Frames(4); Shot("sp140_room_the_roar");
            ReadTo(null);
            JumpOnto("DistortionWorldGiratinaRoom", 15, 20, Direction.Up);
            ReadTo(null, 600);
            Console.WriteLine($"giratina's room: progress {Progress()}");
            JumpOnto("DistortionWorldGiratinaRoom", 15, 17, Direction.Up);
            ReadTo("do not leave"); Frames(4); Shot("sp141_room_giratina_has_come");
            ReadTo(null); Frames(20);
            Shot("sp142_room_giratina_before_the_player");
            Console.WriteLine($"giratina's room: progress {Progress()}, giratina there {Someone("DistortionWorldGiratinaRoom", "giratina") != null}");
            At("DistortionWorldGiratinaRoom", 15, 14, Direction.Up);
            game.Interact();
            IntoBattle("Giratina");
            ToMainMenu(game.Battle); Shot("sp143_room_giratinas_battle");
            Console.WriteLine($"giratina's battle: {game.Battle.EnemyPokemon?.ModelName} level {game.Battle.EnemyPokemon?.Level}");
            Win();
            ReadTo("gone quiet"); Frames(4); Shot("sp144_room_cynthia_comes_up");
            ReadTo("found again");
            Box().Advance();
            for (int i = 0; i < 80; i++) Quick();
            Frames(2); Shot("sp145_room_cyrus_on_the_stones");
            ReadTo("cannot be made"); Frames(4); Shot("sp146_room_a_new_world_cannot_be_made");
            ReadTo("worth keeping"); Frames(4); Shot("sp147_room_cynthia_answers");
            ReadTo("alone"); Frames(4); Shot("sp148_room_cyrus_goes_alone");
            ReadTo("being alive"); Frames(4); Shot("sp149_room_he_cant_feel_glad");
            ReadTo("go home"); Frames(4); Shot("sp150_room_the_portal_opens");
            ReadTo(null); Frames(20);
            Shot("sp151_room_the_portal");
            Console.WriteLine($"giratina's room: progress {Progress()}, {Flags("FLAG_CAUGHT_GIRATINA", "FLAG_HIDE_TURNBACK_CAVE_GIRATINA_ROOM_GIRATINA", "FLAG_HIDE_TURNBACK_CAVE_GIRATINA_ROOM_ITEM")}");
        }

        // ---- the portal out, and Sendoff Spring
        ComeThrough("DistortionWorldGiratinaRoom", 15, 14, Direction.Up);
        ReadTo(null, 300);
        Frames(20);
        game.Interact();
        ToQuestion("the portal's question");
        Shot("sp152_room_leap_into_the_portal");
        engine.Choice.Confirm();
        OnTo("SendoffSpring");
        ReadTo("We're out"); Frames(4); Shot("sp160_spring_out_of_the_distortion_world");
        Where("sendoff spring");
        ReadTo("Sendoff Spring"); Frames(4); Shot("sp161_spring_cynthia_knows_the_place");
        ReadTo("Professor Rowan"); Frames(4); Shot("sp162_spring_go_and_see_the_professor");
        ReadTo(null); Frames(20);
        Console.WriteLine($"sendoff spring: exited {story.Var("VAR_EXITED_DISTORTION_WORLD_STATE")}, lab {story.Var("VAR_SANDGEM_TOWN_LAB_STATE")}, spring path {story.Var("VAR_HIDDEN_LOCATION_SPRING_PATH")}, "
            + Flags("FLAG_SPEAR_PILLAR_IS_DISTORTED", "FLAG_HIDE_SPEAR_PILLAR_DISTORTED_TEAM_GALACTIC", "FLAG_HIDE_MT_CORONET_GALACTIC_GRUNTS", "FLAG_HIDE_MT_CORONET_1F_NORTH_ROOM_1_GRUNT_F",
                "FLAG_HIDE_MT_CORONET_2F_LOOKER", "FLAG_HIDE_GALACTIC_HQ_1F_SATURN", "FLAG_GALACTIC_LEFT_LAKE_VALOR", "FLAG_ALT_MUSIC_GALACTIC_HQ_1F"));
        At("SendoffSpring", 32, 18, Direction.Up); Frames(30); Shot("sp163_spring_cynthia_before_the_cave");
        TalkTo("SendoffSpring", "cynthia", 1, 0);
        ReadTo("stay here a while"); Frames(4); Shot("sp164_spring_cynthia_stays");
        ReadTo(null);
        At("SendoffSpring", 20, 40, Direction.Up); Frames(30); Shot("sp165_spring_the_water");

        // ---- what the chapter leaves: the mountain clear, Spear Pillar whole, Saturn in the lobby, Lake Valor full
        ComeThrough("MtCoronet2F", 7, 25, Direction.Up);
        bool looker = Someone("MtCoronet2F", "looker") != null;
        ComeThrough("MtCoronet6F", 7, 7, Direction.Up);
        Frames(20);
        Console.WriteLine($"afterwards: 6F's way out leads to {game.Map.GetWarpAt(7, 5)?.TargetMap}, its guard there {Someone("MtCoronet6F", "galactic_grunt") != null}, Looker on 2F {looker}");
        ComeThrough("SpearPillar", 31, 34, Direction.Up);
        Frames(30); Shot("sp170_pillar_afterwards");
        ComeThrough("GalacticHQ1F", 8, 22, Direction.Up);
        ReadTo(null, 300);
        var saturn = Someone("GalacticHQ1F", "saturn");
        if (saturn != null)
        {
            At("GalacticHQ1F", saturn.GridX, saturn.GridY + 2, Direction.Up); Frames(30); Shot("sp171_lobby_saturn");
            TalkTo("GalacticHQ1F", "saturn");
            ReadTo("Spear Pillar"); Frames(4); Shot("sp172_lobby_saturn_speaks");
            ReadTo(null);
        }
        else Console.WriteLine("  !! Saturn isn't in the lobby");
        Console.WriteLine($"afterwards: valor lakefront leads to {MapDatabase.Get("Sinnoh").Warps.Where(w => w.TargetMap.StartsWith("LakeValor", StringComparison.Ordinal) && !w.Shut).Select(w => w.TargetMap).Distinct().FirstOrDefault()}");

        // ---- Turnback Cave: Giratina, since it was beaten and not caught; and the portal once it is gone
        ComeThrough("TurnbackCaveGiratinaRoom", 11, 18, Direction.Up);
        ReadTo(null, 300);
        Frames(30); Shot("sp180_turnback_giratina_waits");
        Console.WriteLine($"turnback cave: giratina there {Someone("TurnbackCaveGiratinaRoom", "giratina") != null}, portal there {Someone("TurnbackCaveGiratinaRoom", "portal") != null}");
        story.Set("FLAG_HIDE_TURNBACK_CAVE_GIRATINA_ROOM_GIRATINA");
        Frames(2);
        ComeThrough("TurnbackCaveGiratinaRoom", 11, 16, Direction.Up);
        Frames(30); Shot("sp181_turnback_the_portal");
        At("TurnbackCaveGiratinaRoom", 11, 15, Direction.Up);
        game.Interact();
        ToQuestion("the portal's question");
        engine.Choice.Confirm();
        ReadTo(null);
        Frames(30); Shot("sp182_distortion_world_the_room_behind_turnback_cave");
        Where("through turnback cave's portal");
        var orb = MapDatabase.Get("DistortionWorldTurnbackCaveRoom").NPCs.FirstOrDefault(n => n.IsItemBall);
        if (orb != null)
        {
            At("DistortionWorldTurnbackCaveRoom", orb.GridX - 1, orb.GridY, Direction.Right); Frames(30); Shot("sp183_distortion_world_the_griseous_orb");
            game.Interact();
            ReadTo("Griseous Orb"); Frames(4); Shot("sp184_distortion_world_the_orb_found");
            ReadTo(null);
            Console.WriteLine($"griseous orb: {bag.GetQuantity(ItemDatabase.Get("Griseous Orb")!)}, {Flags("FLAG_OBTAINED_DISTORTION_WORLD_TURNBACK_CAVE_ROOM_GRISEOUS_ORB")}");
        }
        else Console.WriteLine("  !! no ball in the room behind Turnback Cave");
    }
}
