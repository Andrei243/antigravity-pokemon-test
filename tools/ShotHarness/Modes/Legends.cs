partial class Harness
{
    // The time after the Hall of Fame, part b (plan 02 · S15; not part of `all`; le*), played by its own scripts:
    // Stark Mountain's pool of lava with Team Galactic's grunts going in across it; Commanders Mars and Jupiter in
    // the first room, their battles and their leaving, Charon going on; Buck running in after the player in the great
    // room, walking behind, a pair's tag battle beside him, waiting when the player turns back, going on ahead; the
    // mountain's heart: the Magma Stone, Charon taking it, Looker's Croagunk, Looker out of his rock, the grunts'
    // flight, the policemen, Charon led off; the parting before the mouth; Heatran once Buck has been spoken to; a
    // rematch at the League; Snowpoint Temple's guard, Candice, the hall, Regigigas asleep and woken; the sailor's son
    // in his nightmare, the sailing to Fullmoon Island, Cresselia and the Lunar Wing, the boy woken. It wins every
    // battle a scene starts (Heatran and Regigigas are beaten, not caught) and prints what each left behind.
    // LEGENDS_FROM=rooms starts in the great room, LEGENDS_FROM=heart at the way into the mountain's heart,
    // LEGENDS_FROM=heatran with Charon caught, LEGENDS_FROM=league at the League, LEGENDS_FROM=temple in Snowpoint
    // City, LEGENDS_FROM=nightmare in the sailor's house, LEGENDS_FROM=island on Fullmoon Island's pier.
    public void LegendsMode()
    {
        engine.StartNewGame();
        PastTheOpening();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var story = game.Story;
        var team = game.Party;
        team.Members.Clear();
        foreach (var (species, level) in new[] { ("Garchomp", 72), ("Torterra", 66), ("Staraptor", 65), ("Luxray", 65), ("Floatzel", 64), ("Lucario", 66) })
            team.Members.Add(new Pokemon(PokemonDatabase.Get(species)!, level));
        story.SetBadges("Sinnoh", 0xFF);

        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        Player Me() => game.Player;
        int quick = 0;
        void Quick()
        {
            FrameClock.Fixed = ++tick / 60.0;
            engine.Update(1f / 60f);
            if (++quick % 6 == 0) engine.Draw();
        }
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Quick();
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }
        // Reads on to a line, answering questions with their first answer (yes) unless told which
        void ReadTo(string? text, int most = 3000, int answer = 0)
        {
            for (int i = 0; i < most; i++)
            {
                if (text != null && Box().IsActive && Box().CurrentLine.Contains(text)) { Whole(); Console.WriteLine($"  said: {text}"); return; }
                if (text != null && !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld && i > 90) break;
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
        void Answer(int answer)
        {
            for (int a = 0; a < answer; a++) engine.Choice.Move(1);
            engine.Choice.Confirm();
            Quick();
        }
        // A battle won: every foe brought down to 1 HP and struck, the player's own team kept well
        void Win()
        {
            var fight = game.Battle;
            for (int guard = 0; guard < 600 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    foreach (var mine in team.Members.Where(p => p.CurrentHP > 0))
                    {
                        mine.CurrentHP = mine.MaxHP;
                        mine.Status = StatusCondition.None;
                        foreach (var move in mine.Moves) move.CurrentPP = move.MaxPP;
                    }
                    foreach (var foe in fight.EnemyParty?.Members ?? new List<Pokemon>()) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    foreach (var foe in fight.Core.EnemySlots.Where(b => b.Pokemon != null)) foe.Pokemon!.CurrentHP = Math.Min(foe.Pokemon.CurrentHP, 1);
                    var lead = fight.PlayerPokemon;
                    var target = fight.EnemyPokemon;
                    int pick = lead.Moves.FindIndex(m => m.Data.Category != MoveCategory.Status
                        && TypeChart.GetEffectiveness(m.Data.Type, target.PrimaryType, target.SecondaryType) > 0f);
                    fight.SelectMainMenuOption(0);
                    fight.SelectMove(Math.Max(0, pick));
                    if (fight.HUD.MenuState == BattleMenuState.SelectTarget) fight.SelectTarget(0);
                }
                else if (fight.HUD.MenuState == BattleMenuState.SwitchPokemon)
                {
                    int next = fight.PlayerParty.Members.FindIndex(p => !p.IsFainted && !fight.PlayerSlots.Any(s => s.Pokemon == p));
                    fight.SelectSwitch(Math.Max(0, next));
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
            for (int i = 0; i < 2400 && State() != GameState.Battle; i++)
            {
                if (engine.Choice.IsOpen) { Quick(); engine.Choice.Confirm(); Quick(); }
                else if (Box().IsActive && !Box().IsCurrentLineComplete) { Box().FinishLine(); Quick(); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Quick(); }
                else Quick();
            }
            if (State() != GameState.Battle) Console.WriteLine($"  !! never happened: {what}");
            else Skip(1.0);
        }
        string Foes() => string.Join(", ", game.Battle?.EnemyParty?.Members.Select(p => $"{p.Species.Name} {p.Level}") ?? new[] { $"{game.Battle?.EnemyPokemon?.Species.Name} {game.Battle?.EnemyPokemon?.Level}" });
        void StepOnto(string map, int x, int y, Direction way)
        {
            var (dx, dy) = FieldMovement.Delta(way);
            At(map, x - dx, y - dy, way);
            engine.Steering = (way, false);
            Until(() => engine.ScriptRunning, $"the scene at {x},{y}", 120);
            engine.Steering = null;
        }
        void ComeThrough(string map, int x, int y, Direction facing)
        {
            At(map, x, y, facing);
            game.ArriveOnMap();
            game.SteppedOutOfWarp = true;
            game.LocationSign.Hide();
            Frames(2);
        }
        NPC? Someone(string map, string key, string? place = null) => MapDatabase.Get(map).NPCs.FirstOrDefault(n => n.Key == key && (place == null || n.ScriptFile == place));
        void TalkTo(string map, string key, string? place = null, int dx = 0, int dy = 1)
        {
            var npc = MapDatabase.Get(map).NPCs.FirstOrDefault(n => n.Key == key && (place == null || n.ScriptFile == place));
            if (npc == null) { Console.WriteLine($"  !! {key} isn't on {map}"); return; }
            var facing = (dx, dy) switch { (0, 1) => Direction.Up, (0, -1) => Direction.Down, (1, 0) => Direction.Left, _ => Direction.Right };
            At(map, npc.GridX + dx, npc.GridY + dy, facing);
            game.Interact();
        }
        // Someone on their way: the shot is taken once they have come to where a walk passes
        void Walking(string map, string key, string? place, Func<NPC, bool> there, string what)
        {
            Until(() => Someone(map, key, place) is { } who && there(who), what, 600);
            Frames(2);
        }
        // The line on the screen read to its end and sent on, so that what follows it (a walk, a tremor) begins
        void On() { if (Box().IsActive) { Box().FinishLine(); Quick(); Box().Advance(); Quick(); } }
        string Flags(params string[] flags) => string.Join(", ", flags.Select(f => $"{f.Replace("FLAG_", "")} {story.Has(f)}"));
        string Vars(params string[] vars) => string.Join(", ", vars.Select(v => $"{v.Replace("VAR_", "")} {story.Var(v)}"));
        int Held(string item) => game.Bag.GetQuantity(ItemDatabase.Get(item)!);
        string Where(string map, string key, string? place = null) => Someone(map, key, place) is { } who ? $"{key} at {who.GridX},{who.GridY}" : $"{key} gone";
        void Do(string place, string lines)
        {
            engine.StartScript(ScriptParser.Parse(place, "script Test\n" + lines)[0]);
            ReadTo(null, 300);
        }
        const string Hall = "PokemonLeagueHallOfFame", Room1 = "StarkMountainRoom1", Room2 = "StarkMountainRoom2", Heart = "StarkMountainRoom3";
        const string House = "CanalaveCitySailorEldritchHouse", Wood = "FullmoonIslandForest", Deep = "SnowpointTempleB5F";

        Frames(5);
        ReadTo(null, 600);

        // A Champion with the National Pokédex (plan 02 · S14 and S15, part a), who has won the tag battle before the
        // Battle Frontier's gate and been asked along by Buck on Route 227
        Do(Hall, "setflag FLAG_UNLOCKED_VS_SEEKER_LVL_4\ncall Victory\nhalloffame\nsetflag FLAG_GAME_COMPLETED\nsetflag SinnohHallOfFame\nheal\nsetvar VAR_PLAYER_HOUSE_POSTGAME_STATE 2");
        foreach (var entry in Pokedex.Entries(Pokedex.Sinnoh)) game.Pokedex.RegisterSeen(entry.Species.DexNumber);
        Do("RowanLab", "nationaldex\nadditem \"Poké Radar\"");
        Do("fight_area", "setvar VAR_FIGHT_AREA_STATE 2\nsetflag FLAG_HIDE_FIGHT_AREA_RIVAL\nsetflag FLAG_HIDE_FIGHT_AREA_BLOCKADE\nsetflag FLAG_HIDE_FIGHT_AREA_BATTLERS_AND_SPECTATORS\nsetflag FLAG_HIDE_FIGHT_AREA_BUCK");
        Do("route_227", "setvar VAR_ROUTE_227_WAKE_RIVAL_STATE 1\nsetflag FLAG_HIDE_ROUTE_227_RIVAL\nsetflag FLAG_HIDE_ROUTE_227_CRASHER_WAKE\nsetvar VAR_ROUTE_227_BUCK_STATE 1");

        string from = Environment.GetEnvironmentVariable("LEGENDS_FROM") ?? "";
        int stage = from switch { "" => 0, "rooms" => 1, "heart" => 2, "heatran" => 3, "league" => 4, "temple" => 5, "nightmare" => 6, "island" => 7, _ => 0 };

        if (stage == 0)
        {
            // ---- Stark Mountain's foot: the road to the pool, and the grunts across the lava
            ComeThrough("Sinnoh", 758, 236, Direction.Left);
            Frames(80); Shot("le01_stark_mountain_the_road_to_the_pool");
            At("Sinnoh", 753, 235, Direction.Left);
            Frames(30); Shot("le02_stark_mountain_the_grunts_across_the_lava");
            StepOnto("Sinnoh", 752, 235, Direction.Left);
            Walking("Sinnoh", "grunt_m_1", "stark_mountain_outside", g => g.GridX >= 745, "the grunts setting off");
            Shot("le03_stark_mountain_the_grunts_set_off");
            ReadTo("get it over with"); Frames(4); Shot("le04_stark_mountain_lets_get_it_over_with");
            On();
            Walking("Sinnoh", "grunt_m_1", "stark_mountain_outside", g => g.GridX >= 749, "the grunts walking to the mouth");
            Shot("le05_stark_mountain_to_the_mouth");
            ReadTo("no Master Cyrus"); Frames(4); Shot("le06_stark_mountain_hes_no_master_cyrus");
            ReadTo("done with Team Galactic"); Frames(4); Shot("le07_stark_mountain_done_with_team_galactic");
            On();
            Walking("Sinnoh", "grunt_m_2", "stark_mountain_outside", g => g.GridY <= 232, "the grunts going in");
            Shot("le08_stark_mountain_they_go_in");
            ReadTo(null); Frames(60); Shot("le09_stark_mountain_the_pool_again");
            Console.WriteLine("the grunts: " + Vars("VAR_STARK_MOUNTAIN_OUTSIDE_STATE") + ", " + Flags("FLAG_HIDE_STARK_MOUNTAIN_OUTSIDE_GRUNTS", "FLAG_FIRST_ARRIVAL_STARK_MOUNTAIN_EXTERIOR")
                + $", {Where("Sinnoh", "grunt_m_1", "stark_mountain_outside")}, player at {Me().GridX},{Me().GridY}");
            At("Sinnoh", 750, 233, Direction.Up);
            Frames(40); Shot("le0a_stark_mountain_the_mouth");

            // ---- the first room: Team Galactic before the way in
            ComeThrough(Room1, 20, 28, Direction.Up);
            Whole(); Frames(4); Shot("le10_room_1_team_galactic");
            ReadTo("shake some out of you"); Frames(4); Shot("le11_room_1_mars_wont_believe_it");
            IntoBattle("Commander Mars");
            ToMainMenu(game.Battle); Shot("le12_room_1_mars_battle");
            Console.WriteLine($"mars: {Foes()}");
            Win();
            ReadTo("let me down again"); Frames(4); Shot("le13_room_1_mars_beaten");
            ReadTo("take my turn"); Frames(4); Shot("le14_room_1_jupiter_takes_her_turn");
            IntoBattle("Commander Jupiter");
            ToMainMenu(game.Battle); Shot("le15_room_1_jupiters_battle");
            Console.WriteLine($"jupiter: {Foes()}");
            Win();
            ReadTo("How dare you"); Frames(4); Shot("le16_room_1_how_dare_you");
            ReadTo("through with Team Galactic"); Frames(4); Shot("le17_room_1_mars_is_through");
            ReadTo("leaving as well"); Frames(4); Shot("le18_room_1_jupiter_leaves_too");
            ReadTo("ordinary girls"); Frames(4); Shot("le19_room_1_two_ordinary_girls");
            On();
            Walking(Room1, "mars", null, m => m.GridY >= 28, "Mars walking out");
            Shot("le1a_room_1_mars_walks_out");
            ReadTo("never be that"); Frames(4); Shot("le1b_room_1_jupiter_alone");
            ReadTo("A journey"); On();
            Walking(Room1, "jupiter", null, j => j.GridY >= 28, "Jupiter walking out");
            Shot("le1c_room_1_jupiter_walks_out");
            ReadTo("What did they ever see"); Frames(4); Shot("le1d_room_1_charon");
            ReadTo("collect the Magma Stone"); Frames(4); Shot("le1e_room_1_charon_goes_for_the_stone");
            On();
            Walking(Room1, "charon", null, c => c.GridY <= 21, "Charon going up the room");
            Shot("le1f_room_1_charon_goes_on");
            ReadTo(null); Frames(20); Shot("le1g_room_1_empty");
            Console.WriteLine("the first room: " + Vars("VAR_STARK_MOUNTAIN_ROOM_1_STATE") + ", " + Flags("FLAG_HIDE_STARK_MOUNTAIN_ROOM_1_TEAM_GALACTIC", "FLAG_HIDE_ROUTE_227_BUCK", "FLAG_FIRST_ARRIVAL_STARK_MOUNTAIN_INTERIOR")
                + $", player at {Me().GridX},{Me().GridY}, {Where(Room1, "mars")}, {Where(Room1, "charon")}, beaten {story.HasDefeated("commander_mars_stark_mountain")} {story.HasDefeated("commander_jupiter_stark_mountain")}");
        }
        else Do(Room1, "setvar VAR_STARK_MOUNTAIN_OUTSIDE_STATE 1\nsetflag FLAG_HIDE_STARK_MOUNTAIN_OUTSIDE_GRUNTS\nsetvar VAR_STARK_MOUNTAIN_ROOM_1_STATE 1\nsetflag FLAG_HIDE_STARK_MOUNTAIN_ROOM_1_TEAM_GALACTIC\nsetflag FLAG_HIDE_ROUTE_227_BUCK");

        if (stage <= 1)
        {
            // ---- the great room: Buck comes running in where the way opens out
            ComeThrough(Room2, 42, 85, Direction.Up);
            Frames(60); Shot("le20_room_2_the_way_in");
            At(Room2, 42, 72, Direction.Up);
            Frames(20); Shot("le21_room_2_where_the_way_opens_out");
            StepOnto(Room2, 42, 69, Direction.Up);
            Walking(Room2, "buck", null, b => b.GridY <= 73, "Buck running up");
            Shot("le22_room_2_buck_runs_up");
            ReadTo("Wait for me"); Frames(4); Shot("le23_room_2_wait_for_me");
            ReadTo("a team"); Frames(4); Shot("le24_room_2_were_a_team");
            ReadTo("decided to go with Buck"); Frames(4); Shot("le25_room_2_decided_to_go_with_buck");
            ReadTo(null);
            Console.WriteLine("buck: " + Vars("VAR_STARK_MOUNTAIN_ROOM_2_FOLLOWER_BUCK_STATE") + ", " + Flags("FLAG_HIDE_STARK_MOUNTAIN_ROOM_2_BUCK", "FLAG_TALKED_TO_STARK_MOUNTAIN_ROOM_2_BUCK") + $", travelling {game.PartnerAlong}");
            // A few steps with him behind
            engine.Steering = (Direction.Up, false);
            Until(() => Me().GridY <= 66, "three steps north", 300);
            engine.Steering = (Direction.Right, false);
            Until(() => Me().GridX >= 45, "three steps east", 300);
            engine.Steering = null;
            Frames(30); Shot("le26_room_2_he_walks_behind");
            Console.WriteLine($"walking: the player at {Me().GridX},{Me().GridY}, {Where(Room2, "buck")}");
            var him = Someone(Room2, "buck");
            if (him != null)
            {
                var way = (him.GridX - Me().GridX, him.GridY - Me().GridY) switch { (1, 0) => Direction.Right, (-1, 0) => Direction.Left, (0, 1) => Direction.Down, _ => Direction.Up };
                At(Room2, Me().GridX, Me().GridY, way);
                game.Interact();
                Whole(); Frames(4); Shot("le27_room_2_buck_spoken_to");
                ReadTo(null);
            }

            // ---- a pair of the room's trainers, who stand face to face and see the two together
            At(Room2, 53, 13, Direction.Up);
            game.KeepPartnerAlong();
            Frames(30); Shot("le28_room_2_a_pair_face_to_face");
            engine.Steering = (Direction.Up, false);
            Until(() => engine.ScriptRunning || State() == GameState.Battle || Me().GridY <= 10, "the pair seeing the player", 400);
            engine.Steering = null;
            IntoBattle("the pair's tag battle");
            if (State() == GameState.Battle)
            {
                Skip(3.0); Shot("le29_room_2_a_tag_battle_beside_buck");
                Console.WriteLine($"beside Buck: {string.Join(", ", game.Battle!.Core.PlayerSlots.Where(s => s.Pokemon != null).Select(s => s.Pokemon!.Species.Name))} against "
                    + string.Join(", ", game.Battle.Core.EnemySlots.Where(s => s.Pokemon != null).Select(s => s.Pokemon!.Species.Name)));
                Win();
            }
            ReadTo(null);

            // ---- turning back leaves him waiting; and he has other words for whoever comes back
            At(Room2, 42, 68, Direction.Down);
            game.KeepPartnerAlong();
            Frames(10);
            engine.Steering = (Direction.Down, false);
            Until(() => engine.ScriptRunning && Me().GridY == 70, "the step back towards the way out", 300);
            engine.Steering = null;
            ReadTo("all on my own"); Frames(4); Shot("le2a_room_2_youre_going_back");
            ReadTo(null); Frames(10); Shot("le2b_room_2_he_waits");
            Console.WriteLine("turned back: " + Vars("VAR_STARK_MOUNTAIN_ROOM_2_FOLLOWER_BUCK_STATE") + $", {Where(Room2, "buck")}, travelling {game.PartnerAlong}");
            StepOnto(Room2, 42, 69, Direction.Up);
            ReadTo("go together"); Frames(4); Shot("le2c_room_2_well_go_together");
            ReadTo(null);

            // ---- the far end: he says this is it and goes in ahead
            At(Room2, 47, 6, Direction.Up);
            game.KeepPartnerAlong();
            Frames(30); Shot("le2d_room_2_the_way_into_the_heart");
            engine.Steering = (Direction.Up, false);
            Until(() => engine.ScriptRunning && Me().GridY == 3, "the step before the way in", 400);
            engine.Steering = null;
            ReadTo("Here we are"); Frames(4); Shot("le2e_room_2_here_we_are");
            ReadTo("No more fooling"); On();
            Until(() => Someone(Room2, "buck") is null or { GridY: 2 }, "Buck going in", 300);
            Shot("le2f_room_2_he_goes_in");
            ReadTo(null); Frames(10);
            Console.WriteLine("gone ahead: " + Vars("VAR_STARK_MOUNTAIN_ROOM_2_FOLLOWER_BUCK_STATE") + ", " + Flags("FLAG_HIDE_STARK_MOUNTAIN_ROOM_2_BUCK") + $", {Where(Room2, "buck")}, travelling {game.PartnerAlong}, player at {Me().GridX},{Me().GridY}");
        }
        else Do(Room2, "setvar VAR_STARK_MOUNTAIN_ROOM_2_FOLLOWER_BUCK_STATE 2\nsetflag FLAG_TALKED_TO_STARK_MOUNTAIN_ROOM_2_BUCK");

        if (stage <= 2)
        {
            // ---- the mountain's heart: Charon and the Magma Stone
            ComeThrough(Heart, 7, 16, Direction.Up);
            Whole(); Frames(4); Shot("le30_heart_charon_before_the_stone");
            ReadTo("Fools"); Frames(4); Shot("le31_heart_fools_every_one");
            On();
            Frames(14); Shot("le32_heart_the_mountain_trembles");
            Until(() => Me().GridY <= 14, "the walk up the hall", 300);
            ReadTo("in my mountain"); Frames(4); Shot("le33_heart_buck_calls_out");
            On();
            Until(() => Someone(Heart, "magma_stone") == null, "Charon taking the stone", 300);
            Whole(); Frames(4); Shot("le34_heart_charon_takes_the_stone");
            ReadTo("wake the Pokémon Heatran"); Frames(4); Shot("le35_heart_what_he_means_to_do");
            ReadTo("Go! Go! Go!"); Frames(4); Shot("le36_heart_a_voice");
            On();
            Walking(Heart, "croagunk", null, c => c.GridY <= 11, "the Croagunk darting out");
            Shot("le37_heart_the_croagunk_snatches_it");
            ReadTo("The Magma Stone!"); Frames(4); Shot("le38_heart_no_the_magma_stone");
            ReadTo("Excellent work"); Frames(4); Shot("le39_heart_excellent_work_croagunk");
            ReadTo("as a rock"); Frames(4); Shot("le3a_heart_looker_out_of_his_rock");
            On();
            Walking(Heart, "looker", null, l => l.GridX <= 8, "Looker walking up");
            Shot("le3b_heart_looker_walks_up");
            ReadTo("most grateful"); Frames(4); Shot("le3c_heart_the_international_police");
            ReadTo("International Police!"); Frames(4); Shot("le3d_heart_the_grunts_take_fright");
            ReadTo("Run for it"); On();
            Walking(Heart, "grunt_m_1", null, g => g.GridY >= 14, "the grunts running");
            Shot("le3e_heart_the_grunts_run");
            ReadTo("harmless old man"); Frames(4); Shot("le3f_heart_a_harmless_old_man");
            ReadTo("Officers"); On();
            Walking(Heart, "policeman_1", null, p => p.GridY <= 13, "the policemen coming in");
            Shot("le3g_heart_the_policemen_come_in");
            ReadTo("somewhere cool"); Frames(4); Shot("le3h_heart_take_him_somewhere_cool");
            On();
            Walking(Heart, "charon", null, c => c.GridX == 8 && c.GridY >= 14, "Charon led off");
            Shot("le3i_heart_charon_is_led_off");
            ReadTo("far too hot"); Frames(4); Shot("le3j_heart_far_too_hot");
            On();

            // ---- the parting before the mountain's mouth
            Until(() => game.Map.Name == "Sinnoh" && Box().IsActive, "the three outside", 900);
            ReadTo("delicious"); Frames(4); Shot("le40_outside_the_air_of_the_outside");
            ReadTo("the Magma Stone"); Frames(4); Shot("le41_outside_the_magma_stone");
            ReadTo("back in its place"); Frames(4); Shot("le42_outside_buck_takes_it_back");
            On();
            Until(() => Someone("Sinnoh", "buck", "stark_mountain_outside") == null, "Buck going in", 300);
            ReadTo("energy of ten"); Frames(4); Shot("le43_outside_that_boy");
            ReadTo("my work in Sinnoh"); On();
            Walking("Sinnoh", "buck", "stark_mountain_outside", b => b.GridY == 233, "Buck coming out");
            Frames(6); Shot("le44_outside_buck_comes_out");
            ReadTo("Survival Area"); Frames(4); Shot("le45_outside_come_by_my_place");
            On();
            Walking("Sinnoh", "buck", "stark_mountain_outside", b => b.GridX <= 746, "Buck going off");
            Shot("le46_outside_buck_goes_off");
            ReadTo("on my way"); Frames(4); Shot("le47_outside_looker_takes_his_leave");
            ReadTo("Farewell"); Frames(4); Shot("le48_outside_farewell");
            ReadTo(null); Frames(30); Shot("le49_outside_alone_before_the_mouth");
            ComeThrough("Sinnoh", 751, 233, Direction.Up);
            Console.WriteLine("charon's arrest: " + Vars("VAR_STARK_MOUNTAIN_ROOM_3_STATE", "VAR_STARK_MOUNTAIN_OUTSIDE_STATE") + ", "
                + Flags("FLAG_ARRESTED_CHARON_STARK_MOUNTAIN", "FLAG_HIDE_SURVIVAL_AREA_BUCK", "FLAG_HIDE_SURVIVAL_AREA_RIVAL", "FLAG_HIDE_GAME_CORNER_LOOKER", "FLAG_HIDE_BATTLEGROUND_BUCK",
                    "FLAG_HIDE_STARK_MOUNTAIN_OUTSIDE_BUCK", "FLAG_HIDE_STARK_MOUNTAIN_OUTSIDE_LOOKER", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_HEATRAN")
                + $", {Where("Sinnoh", "looker", "stark_mountain_outside")}, {Where("Sinnoh", "buck", "stark_mountain_outside")}, player at {Me().GridX},{Me().GridY}");
            ComeThrough(Heart, 7, 16, Direction.Up);
            At(Heart, 7, 10, Direction.Up);
            Frames(30); Shot("le4a_heart_empty");
            Console.WriteLine($"the heart afterwards: {MapDatabase.Get(Heart).NPCs.Count} there, " + Vars("VAR_STARK_MOUNTAIN_ROOM_3_STATE"));
            // The Survival Area: Buck and the rival before the Battleground's door
            ComeThrough("Sinnoh", 652, 341, Direction.Up);
            Frames(30); Shot("le4b_survival_area_buck_and_the_rival");
            Console.WriteLine($"the survival area: {Where("Sinnoh", "buck", "survival_area")}, {Where("Sinnoh", "rival", "survival_area")}");
        }
        else
            Do(Heart, "setvar VAR_STARK_MOUNTAIN_ROOM_3_STATE 1\nsetvar VAR_STARK_MOUNTAIN_OUTSIDE_STATE 3\nsetflag FLAG_ARRESTED_CHARON_STARK_MOUNTAIN\nsetflag FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_TEAM_GALACTIC\n"
                + "setflag FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_BUCK\nsetflag FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_MAGMA_STONE\nsetflag FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_ROCK_SMASH_LOOKER\n"
                + "clearflag FLAG_HIDE_SURVIVAL_AREA_BUCK\nclearflag FLAG_HIDE_SURVIVAL_AREA_RIVAL\nclearflag FLAG_HIDE_GAME_CORNER_LOOKER\nclearflag FLAG_HIDE_BATTLEGROUND_BUCK");

        if (stage <= 3)
        {
            // ---- Heatran, once Buck has been spoken to at his place (the Battleground's scene, part c, sets the flag)
            Do(Heart, "setflag FLAG_TALKED_TO_BATTLEGROUND_BUCK");
            ComeThrough(Heart, 7, 16, Direction.Up);
            Frames(30); Shot("le50_heart_heatran_at_the_head_of_the_hall");
            At(Heart, 7, 9, Direction.Up);
            Frames(30); Shot("le51_heart_heatran");
            TalkTo(Heart, "heatran");
            ReadTo("grrrohh"); Frames(4); Shot("le52_heart_heatrans_cry");
            IntoBattle("Heatran");
            ToMainMenu(game.Battle); Shot("le53_heart_heatrans_battle");
            Console.WriteLine($"heatran's battle: {Foes()}");
            Win();
            ReadTo("sank back"); Frames(4); Shot("le54_heart_heatran_sank_back");
            ReadTo(null);
            Console.WriteLine("heatran: " + Vars("VAR_STARK_MOUNTAIN_ROOM_3_STATE") + ", "
                + Flags("FLAG_BATTLED_STARK_MOUNTAIN_ROOM_3_HEATRAN", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_HEATRAN", "FLAG_CAUGHT_HEATRAN", "FLAG_UNLOCKED_VS_SEEKER_LVL_5") + $", {Where(Heart, "heatran")}");
            // ...and back after the Hall of Fame is next entered
            Do(Hall, "call Victory");
            ComeThrough(Heart, 7, 16, Direction.Up);
            Console.WriteLine("after the Hall of Fame: " + Vars("VAR_STARK_MOUNTAIN_ROOM_3_STATE") + $", {Where(Heart, "heatran")}");
        }

        if (stage <= 4)
        {
            // ---- the League again: Aaron's rematch team
            ComeThrough("PokemonLeagueNorthPokemonCenter", 11, 11, Direction.Up);
            ComeThrough("PokemonLeagueAaronRoom", 8, 11, Direction.Up);
            ReadTo(null);
            TalkTo("PokemonLeagueAaronRoom", "aaron");
            IntoBattle("Aaron's rematch");
            ToMainMenu(game.Battle); Shot("le60_league_aarons_rematch");
            Console.WriteLine($"aaron's rematch: {Foes()}");
            Win();
            ReadTo(null);
            Console.WriteLine("the rematches: " + string.Join("; ", new[] { "elite_four_aaron", "elite_four_bertha", "elite_four_flint", "elite_four_lucian", "champion_cynthia" }
                .Select(id => $"{TrainerDatabase.Get(id + "_rematch")!.Name} {TrainerDatabase.Get(id)!.Party.Max(p => p.Level)} to {TrainerDatabase.Get(id + "_rematch")!.Party.Max(p => p.Level)}")));
        }

        if (stage <= 5)
        {
            // ---- Snowpoint Temple: the guard, Candice, the door
            ComeThrough("Sinnoh", 366, 201, Direction.Up);
            Frames(80); Shot("le70_snowpoint_the_temples_door");
            StepOnto("Sinnoh", 366, 198, Direction.Up);
            ReadTo("chosen"); Frames(4); Shot("le71_snowpoint_only_the_chosen");
            ReadTo("ask you to leave"); On();
            Walking("Sinnoh", "candice", "snowpoint_city", c => c.GridY <= 204, "Candice coming up the street");
            Shot("le72_snowpoint_candice_comes_up");
            ReadTo("let this one in"); Frames(4); Shot("le73_snowpoint_let_this_one_in");
            ReadTo("Cynthia"); Frames(4); Shot("le74_snowpoint_cynthia_told_me");
            ReadTo(null); Frames(10); Shot("le75_snowpoint_let_in");
            Console.WriteLine("the temple: " + Vars("VAR_SNOWPOINT_CITY_STATE") + $", {Where("Sinnoh", "candice", "snowpoint_city")}, player at {Me().GridX},{Me().GridY}, the door leads to {game.Map.GetWarpAt(366, 197)?.TargetMap}");
            engine.Steering = (Direction.Up, false);
            Until(() => game.Map.Name == "SnowpointTemple1F" && State() == GameState.Overworld, "the walk in at the temple's door", 600);
            engine.Steering = null;
            Frames(80); Shot("le76_temple_the_hall");
            Console.WriteLine($"inside: {game.Map.Name} at {Me().GridX},{Me().GridY}, " + Flags("FLAG_FIRST_ARRIVAL_SNOWPOINT_TEMPLE"));

            // ---- Regigigas, asleep and woken
            ComeThrough(Deep, 7, 3, Direction.Down);
            At(Deep, 11, 15, Direction.Up);
            Frames(80); Shot("le77_temple_regigigas_on_its_dais");
            TalkTo(Deep, "regigigas");
            ReadTo("heavy with strength"); Frames(4); Shot("le78_temple_a_statue_of_a_pokemon");
            ReadTo("One of rock"); Frames(4); Shot("le79_temple_the_words_on_the_dais");
            ReadTo(null);
            Console.WriteLine("without the three: " + Flags("FLAG_AWAKENED_REGIGIGAS") + $", {Where(Deep, "regigigas")}");
            var kept = team.Members.Skip(3).ToList();
            team.Members.RemoveRange(3, team.Members.Count - 3);
            foreach (string titan in new[] { "Regirock", "Regice", "Registeel" }) team.Members.Add(new Pokemon(PokemonDatabase.Get(titan)!, 40));
            TalkTo(Deep, "regigigas");
            ReadTo("One of rock"); On();
            Frames(40); Shot("le7a_temple_the_king_stirs");
            // The statue itself shakes, eight times (plan 09 · L12): close up, thrown east and thrown west
            var statue = Someone(Deep, "regigigas");
            var pieces = game.World.Pieces;
            int Thrown() => pieces.OffsetOf(statue, game.World.Life.Now);
            Until(() => Thrown() > 0, "the statue thrown east", 120);
            ShotCrop("le7ab_temple_the_statue_shaken_east", 770, 300, 320, 240, 6);
            Until(() => Thrown() < 0, "the statue thrown west", 120);
            ShotCrop("le7ac_temple_the_statue_shaken_west", 770, 300, 320, 240, 6);
            Console.WriteLine($"regigigas shaken: {Thrown()} texels off its tile, {Where(Deep, "regigigas")}");
            ReadTo("gigigi"); Frames(4); Shot("le7b_temple_regigigas_wakes");
            IntoBattle("Regigigas");
            ToMainMenu(game.Battle); Shot("le7c_temple_regigigas_battle");
            Console.WriteLine($"regigigas's battle: {Foes()}");
            Win();
            ReadTo("gone from sight"); Frames(4); Shot("le7d_temple_gone_from_sight");
            ReadTo(null); Frames(10); Shot("le7e_temple_the_dais_empty");
            Console.WriteLine("regigigas: " + Flags("FLAG_AWAKENED_REGIGIGAS", "FLAG_HIDE_SNOWPOINT_TEMPLE_B5F_REGIGIGAS", "FLAG_CAUGHT_REGIGIGAS") + $", {Where(Deep, "regigigas")}");
            team.Members.RemoveRange(3, team.Members.Count - 3);
            team.Members.AddRange(kept);
            // Out again by the hall's mat: Candice has gone back to her Gym
            ComeThrough("Sinnoh", 366, 198, Direction.Down);
            Frames(30); Shot("le7f_snowpoint_out_again");
            Console.WriteLine($"out again: {Where("Sinnoh", "candice", "snowpoint_city")}, script running {engine.ScriptRunning}");
        }

        if (stage <= 6)
        {
            // ---- the sailor's house: the boy in his nightmare
            ComeThrough(House, 4, 8, Direction.Up);
            Frames(40); Shot("le80_house_the_boy_in_his_bed");
            Console.WriteLine("the nightmare: " + Vars("VAR_LUNAR_WING_EVENT_STATE") + ", " + Flags("FLAG_CANALAVE_CITY_SAILOR_ELDRITCH_HOUSE_LITTLE_BOY_IN_BED")
                + $", {Where(House, "little_boy_in_bed")}, {Where(House, "pokefan_f")}, {Where(House, "little_boy")}");
            TalkTo(House, "pokefan_f", null, 0, -1);
            ReadTo("nightmare"); Frames(4); Shot("le81_house_his_mother");
            ReadTo(null);
            At(House, 7, 6, Direction.Right);
            Frames(10);
            game.Interact();
            ReadTo("tossing and turning"); Frames(4); Shot("le82_house_tossing_and_turning");
            ReadTo("watching me"); Frames(4); Shot("le83_house_its_watching_me");
            ReadTo(null);
            Console.WriteLine("looked at: " + Vars("VAR_LUNAR_WING_EVENT_STATE") + ", " + Flags("FLAG_TALKED_TO_CANALAVE_CITY_SAILOR_ELDRITCH_HOUSE_LITTLE_BOY"));

            // ---- his father at the quay, and the sailing
            ComeThrough("Sinnoh", 43, 750, Direction.Right);
            Frames(40); Shot("le84_canalave_the_sailor_at_his_boat");
            TalkTo("Sinnoh", "sailor_eldritch", "canalave_city", -1, 0);
            ToQuestion("the sailor's question"); Shot("le85_canalave_for_my_boy");
            Answer(0);
            ReadTo("counting on you"); Frames(4); Shot("le86_canalave_counting_on_you");
            ReadTo("couldn't find it"); On();
            Until(() => Me().GridX == 39 && Me().GridY == 277 && State() == GameState.Overworld && !engine.ScriptRunning, "the landing on Fullmoon Island", 900);
        }
        else
        {
            Do(House, "setvar VAR_LUNAR_WING_EVENT_STATE 2\nsetflag FLAG_TALKED_TO_CANALAVE_CITY_SAILOR_ELDRITCH_HOUSE_LITTLE_BOY\nsetflag FLAG_CANALAVE_CITY_SAILOR_ELDRITCH_HOUSE_LITTLE_BOY_IN_BED");
            ComeThrough("Sinnoh", 39, 277, Direction.Left);
        }

        // ---- Fullmoon Island: the pier, the wood, Cresselia and the Lunar Wing
        game.LocationSign.Hide();
        Frames(80); Shot("le87_fullmoon_island_the_pier");
        Console.WriteLine($"landed: {game.Map.AreaAt(Me().GridX, Me().GridY)?.Key} at {Me().GridX},{Me().GridY} facing {Me().Facing}, " + Flags("FLAG_FIRST_ARRIVAL_FULLMOON_ISLAND") + ", " + Vars("VAR_HIDDEN_LOCATION_FULL_MOON_ISLAND"));
        At("Sinnoh", 53, 270, Direction.Up);
        Frames(60); Shot("le88_fullmoon_island_the_way_into_the_wood");
        ComeThrough(Wood, 16, 20, Direction.Up);
        Frames(80); Shot("le89_wood_the_clearing");
        At(Wood, 16, 17, Direction.Up);
        Frames(40); Shot("le8a_wood_cresselia");
        At(Wood, 16, 16, Direction.Up);
        Frames(4);
        game.Interact();
        Until(() => Someone(Wood, "cresselia") is { GridY: 14 }, "Cresselia drawing back", 300);
        Frames(4); Shot("le8b_wood_she_draws_back");
        // It flickers six times and is gone (plan 09 · L12): a moment it isn't seen, and one it is seen again
        var flickering = Someone(Wood, "cresselia");
        bool Showing() => game.World.Pieces.Seen(flickering, game.World.Life.Now);
        Until(() => !Showing(), "Cresselia unseen", 300);
        Frames(2); Shot("le8ba_wood_she_flickers_out");
        Until(Showing, "Cresselia seen again", 60);
        Frames(2); Shot("le8bb_wood_she_flickers_back");
        Console.WriteLine($"cresselia flickering: on the map {Someone(Wood, "cresselia") != null}, {Where(Wood, "cresselia")}");
        ReadTo("flew away"); Frames(4); Shot("le8c_wood_flew_away");
        ReadTo("glittering"); Frames(4); Shot("le8d_wood_something_is_glittering");
        ReadTo(null); Frames(10);
        var roaming = game.Encounters.Roamers[Roamers.SlotOf("Cresselia")!.Value];
        Console.WriteLine("cresselia: " + Flags("FLAG_MET_CRESSELIA", "FLAG_HIDE_FULLMOON_ISLAND_FOREST_CRESSELIA", "FLAG_OBTAINED_FULLMOON_ISLAND_FOREST_LUNAR_WING")
            + $", roaming {roaming.Active} at {Roamers.PlaceOf(roaming)} level {roaming.Level}, {Where(Wood, "item_lunar_wing")}, {Where(Wood, "cresselia")}");
        // The ball is looked at from the side: a tile north of the player it would lie behind their head
        At(Wood, 17, 14, Direction.Left);
        Frames(30); Shot("le8e_wood_the_lunar_wing");
        game.Interact();
        ReadTo("Lunar Wing"); Frames(4); Shot("le8f_wood_found_the_lunar_wing");
        ReadTo(null);
        Console.WriteLine($"the wing: {Held("Lunar Wing")} in the bag, " + Flags("FLAG_OBTAINED_FULLMOON_ISLAND_FOREST_LUNAR_WING") + $", {Where(Wood, "item_lunar_wing")}");

        // ---- back with it: the sailor on the pier, the quay, and the boy woken
        ComeThrough("Sinnoh", 39, 277, Direction.Right);
        TalkTo("Sinnoh", "sailor", "fullmoon_island", -1, 0);
        ReadTo("Lunar Pokémon"); Frames(4); Shot("le90_fullmoon_island_the_sailor_sees_the_wing");
        ReadTo("wake my boy"); On();
        Until(() => game.Map.AreaAt(Me().GridX, Me().GridY)?.Key == "canalave_city" && State() == GameState.Overworld && !engine.ScriptRunning, "the landing in Canalave City", 900);
        Frames(30);
        TalkTo("Sinnoh", "sailor_eldritch", "canalave_city", -1, 0);
        ReadTo("take it to my son"); Frames(4); Shot("le91_canalave_take_it_to_my_son");
        ReadTo(null);
        ComeThrough(House, 4, 8, Direction.Up);
        At(House, 7, 6, Direction.Right);
        Frames(10);
        game.Interact();
        ReadTo("began to glow"); Frames(4); Shot("le92_house_the_wing_glows");
        ReadTo("sleeping as peacefully"); Frames(4); Shot("le93_house_sleeping_peacefully");
        ReadTo("woke up"); Frames(4); Shot("le94_house_the_boy_woke_up");
        On();
        Walking(House, "sailor_eldritch", null, f => f.GridY <= 6 && f.GridX >= 5, "his father running in");
        Shot("le95_house_his_father_runs_in");
        ReadTo("awake"); Frames(4); Shot("le96_house_youre_awake");
        ReadTo("not forget"); Frames(4); Shot("le97_house_well_not_forget");
        ReadTo("sail with me"); Frames(4); Shot("le98_house_sail_with_me");
        On();
        Walking(House, "sailor_eldritch", null, f => f.GridX <= 4, "his father going back to his boat");
        Shot("le99_house_he_goes_back_to_his_boat");
        ReadTo(null); Frames(10);
        Console.WriteLine("woken: " + Vars("VAR_LUNAR_WING_EVENT_STATE") + ", " + Flags("FLAG_WOKE_UP_CANALAVE_CITY_SAILOR_ELDRITCH_HOUSE_LITTLE_BOY", "FLAG_HIDE_CANALAVE_CITY_SAILOR_ELDRITCH_HOUSE_SAILOR_ELDRITCH")
            + $", the wing {Held("Lunar Wing")}, player at {Me().GridX},{Me().GridY}, {Where(House, "sailor_eldritch")}");
        At(House, 7, 6, Direction.Right);
        game.Interact();
        ReadTo("dark Pokémon"); Frames(4); Shot("le9a_house_the_boys_dream");
        ReadTo(null);
        ComeThrough(House, 4, 8, Direction.Up);
        Frames(40); Shot("le9b_house_all_well_again");
        Console.WriteLine("the next visit: " + Vars("VAR_LUNAR_WING_EVENT_STATE") + ", " + Flags("FLAG_CANALAVE_CITY_SAILOR_ELDRITCH_HOUSE_LITTLE_BOY_IN_BED") + $", {Where(House, "little_boy")}, {Where(House, "pokefan_f")}");
        ComeThrough("Sinnoh", 43, 750, Direction.Right);
        TalkTo("Sinnoh", "sailor_eldritch", "canalave_city", -1, 0);
        ToQuestion("where the sailor sails"); Shot("le9c_canalave_where_shall_i_take_you");
        Answer(2);
        ReadTo(null);
    }
}
