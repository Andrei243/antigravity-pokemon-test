partial class Harness
{
    // The time after the Hall of Fame, part c (plan 02 · S15; not part of `all`; ep*), played by its own scripts, in
    // the order a game comes to it: Rotom in the Old Chateau's television, by day and after dark; the assistant's
    // word of the elder, the elder's book in Celestic Town and the assistant afterwards; the two orbs in Mt. Coronet,
    // the mountain's way out, Dialga's pillar, its rift asked and its battle, Palkia's the climb after, and what the
    // Hall of Fame puts back; the Battleground: its door, Buck's grandfather's welcome, the room with a day's four
    // guests, Buck leaving for the Tower, a guest refused, asked again and battled, Roark and his father, the old man
    // and another day's draw; and Cynthia before the ruins' painting, heard out and cut short. It wins every battle a
    // scene starts (Rotom, Dialga and Palkia are beaten, not caught) and prints what each left behind.
    // EPILOGUE_FROM=elder starts in Sandgem Town, EPILOGUE_FROM=summit in Mt. Coronet with the elder heard,
    // EPILOGUE_FROM=battleground before its door with Charon caught, EPILOGUE_FROM=painting in the ruins.
    public void EpilogueMode()
    {
        engine.StartNewGame();
        PastTheOpening();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var story = game.Story;
        var team = game.Party;
        team.Members.Clear();
        foreach (var (species, level) in new[] { ("Garchomp", 78), ("Torterra", 72), ("Staraptor", 70), ("Luxray", 70), ("Floatzel", 70), ("Lucario", 72) })
            team.Members.Add(new Pokemon(PokemonDatabase.Get(species)!, level));
        foreach (var badge in Enum.GetValues<Badge>()) story.GiveBadge(badge);

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
        void Walking(string map, string key, Func<NPC, bool> there, string what)
        {
            Until(() => Someone(map, key) is { } who && there(who), what, 600);
            Frames(2);
        }
        // The line on the screen read to its end and sent on, so that what follows it (a walk, a cry) begins
        void On() { if (Box().IsActive) { Box().FinishLine(); Quick(); Box().Advance(); Quick(); } }
        string Flags(params string[] flags) => string.Join(", ", flags.Select(f => $"{f.Replace("FLAG_", "")} {story.Has(f)}"));
        string Vars(params string[] vars) => string.Join(", ", vars.Select(v => $"{v.Replace("VAR_", "")} {story.Var(v)}"));
        void Do(string place, string lines)
        {
            engine.StartScript(ScriptParser.Parse(place, "script Test\n" + lines)[0]);
            ReadTo(null, 300);
        }
        void Hour(TimeOfDay time)
        {
            engine.Settings.TimeOfDay = time;
            engine.ApplySettings(window: false);
        }
        const string Hall = "PokemonLeagueHallOfFame", Chateau = "OldChateauBackMiddleWestRoom", Summit = "MtCoronet6F", Room = "Battleground", Ruins = "CelesticTownCave";
        string WayOut() => MapDatabase.Get(Summit).GetWarpAt(7, 5)?.TargetMap ?? "nowhere";
        string In() => string.Join(", ", MapDatabase.Get(Room).NPCs.Select(n => $"{n.Key} at {n.GridX},{n.GridY}"));

        Frames(5);
        ReadTo(null, 600);

        string from = Environment.GetEnvironmentVariable("EPILOGUE_FROM") ?? "";
        int stage = from switch { "" => 0, "elder" => 1, "summit" => 2, "battleground" => 3, "painting" => 4, _ => 0 };

        if (stage == 0)
        {
            // ---- Rotom: the television by day, which only stares
            ComeThrough(Chateau, 11, 7, Direction.Up);
            Frames(40); Shot("ep01_chateau_the_room_with_the_television");
            At(Chateau, 11, 4, Direction.Up);
            Frames(10); Shot("ep02_chateau_before_the_set");
            game.Interact();
            ReadTo("unfriendly air"); Frames(4); Shot("ep03_chateau_by_day_it_only_stares");
            ReadTo(null);
            Console.WriteLine("by day: " + Flags("FLAG_DAILY_BATTLED_OLD_CHATEAU_ROTOM", "FLAG_CAUGHT_OLD_CHATEAU_ROTOM"));

            // ---- after dark something is in it
            Hour(TimeOfDay.Night);
            ComeThrough(Chateau, 11, 7, Direction.Up);
            At(Chateau, 12, 4, Direction.Up);
            Frames(30); Shot("ep04_chateau_after_dark");
            game.Interact();
            ReadTo("Is that a Pokémon"); Frames(4); Shot("ep05_chateau_something_inside");
            ToQuestion("the thump"); Shot("ep06_chateau_give_it_a_thump");
            Answer(0);
            ReadTo("thumped the television"); Frames(4); Shot("ep07_chateau_thumped");
            IntoBattle("Rotom");
            ToMainMenu(game.Battle); Shot("ep08_chateau_rotom");
            Console.WriteLine($"rotom: {Foes()}");
            Win();
            ReadTo("slipped back"); Frames(4); Shot("ep09_chateau_back_into_the_set");
            ReadTo(null);
            game.Interact();
            ReadTo("unfriendly air"); Frames(4); Shot("ep0a_chateau_not_again_tonight");
            ReadTo(null);
            Console.WriteLine("rotom: " + Flags("FLAG_DAILY_BATTLED_OLD_CHATEAU_ROTOM", "FLAG_CAUGHT_OLD_CHATEAU_ROTOM"));
            Hour(TimeOfDay.Day);
        }

        // A Champion, home again (plan 02 · S14), with 120 of Sinnoh's Pokémon seen and no National Pokédex yet
        Do(Hall, "setflag FLAG_UNLOCKED_VS_SEEKER_LVL_4\ncall Victory\nhalloffame\nsetflag FLAG_GAME_COMPLETED\nsetflag SinnohHallOfFame\nheal\nsetvar VAR_PLAYER_HOUSE_POSTGAME_STATE 2\nsetvar VAR_EXITED_DISTORTION_WORLD_STATE 3");
        foreach (var entry in Pokedex.Entries(Pokedex.Sinnoh).Take(120)) game.Pokedex.RegisterSeen(entry.Species.DexNumber);
        Do("celestic_town", "clearflag FLAG_HIDE_CELESTIC_TOWN_NORTH_HOUSE_ELDER\nsetflag FLAG_HIDE_CELESTIC_TOWN_ELDER\nsetflag FLAG_HIDE_CELESTIC_TOWN_CAVE_CYRUS\nsetflag FLAG_EXAMINED_CELESTIC_TOWN_CAVE_PAINTING\nsetvar VAR_CELESTIC_TOWN_STATE 2");

        if (stage <= 1)
        {
            // ---- the assistant passes on the elder's word
            ComeThrough("Sinnoh", 167, 847, Direction.Up);
            TalkTo("Sinnoh", "sandgem_counterpart", "sandgem_town", -1, 0);
            ReadTo("elder of Celestic Town"); Frames(4); Shot("ep10_sandgem_the_elder_has_something_to_say");
            ReadTo(null);

            // ---- the elder at home, and her book
            ComeThrough("CelesticTownNorthHouse", 4, 8, Direction.Up);
            Frames(30); Shot("ep11_celestic_the_elders_house");
            TalkTo("CelesticTownNorthHouse", "elder");
            ReadTo("a very old one"); Frames(4); Shot("ep12_celestic_a_very_old_book");
            ReadTo("Adamant Orb"); Frames(4); Shot("ep13_celestic_the_adamant_orb");
            ReadTo("Lustrous Orb"); Frames(4); Shot("ep14_celestic_the_lustrous_orb");
            ReadTo("met them both"); Frames(4); Shot("ep15_celestic_they_met_them_both");
            ReadTo(null);
            Console.WriteLine("the elder: " + Flags("FLAG_UNLOCKED_DIALGA_PALKIA_SPEAR_PILLAR"));

            // ---- and the assistant afterwards: the professor's word of Spear Pillar
            ComeThrough("Sinnoh", 167, 847, Direction.Up);
            TalkTo("Sinnoh", "sandgem_counterpart", "sandgem_town", -1, 0);
            ReadTo("Spear Pillar"); Frames(4); Shot("ep16_sandgem_something_at_spear_pillar");
            ReadTo(null);
        }
        else Do("celestic_town", "setflag FLAG_UNLOCKED_DIALGA_PALKIA_SPEAR_PILLAR");

        if (stage <= 2)
        {
            // ---- the two orbs, side by side in Mt. Coronet
            var rooms = MapDatabase.Get("MtCoronet4FRooms1And2");
            var adamant = rooms.Everyone.FirstOrDefault(n => n.IsItemBall && n.Item == "Adamant Orb");
            if (adamant != null)
            {
                ComeThrough("MtCoronet4FRooms1And2", adamant.GridX + 2, adamant.GridY + 2, Direction.Up);
                Frames(60); Shot("ep20_mt_coronet_the_two_orbs");
                At("MtCoronet4FRooms1And2", adamant.GridX, adamant.GridY + 1, Direction.Up);
                game.Interact();
                ReadTo("Adamant Orb"); Frames(4); Shot("ep21_mt_coronet_the_adamant_orb");
                ReadTo(null);
            }
            Console.WriteLine($"the orbs: Adamant {game.Bag.GetQuantity(ItemDatabase.Get("Adamant Orb")!)}, Lustrous {game.Bag.GetQuantity(ItemDatabase.Get("Lustrous Orb")!)}");
            if (game.Bag.GetQuantity(ItemDatabase.Get("Adamant Orb")!) == 0) Do("mt_coronet_6f", "additem \"Adamant Orb\"");

            // ---- under the summit: the way out leads to Dialga's pillar
            ComeThrough(Summit, 7, 8, Direction.Up);
            Frames(40); Shot("ep22_mt_coronet_under_the_summit");
            Console.WriteLine($"with the Adamant Orb the way out leads to {WayOut()}; " + Flags("FLAG_SPEAR_PILLAR_HOLDS_DIALGA", "FLAG_SPEAR_PILLAR_HOLDS_PALKIA"));

            // ---- Dialga's pillar
            ComeThrough("SpearPillarDialga", 31, 52, Direction.Up);
            Frames(80); Shot("ep23_dialgas_pillar_from_the_way_up");
            At("SpearPillarDialga", 31, 22, Direction.Up);
            Frames(40); Shot("ep24_dialgas_pillar_the_altar");
            At("SpearPillarDialga", 29, 18, Direction.Up);
            Frames(30); Shot("ep25_dialgas_pillar_below_the_rift");
            game.Interact();
            ReadTo("looking back"); Frames(4); Shot("ep26_dialgas_pillar_looking_back");
            ToQuestion("the challenge"); Shot("ep27_dialgas_pillar_will_you_challenge_it");
            Answer(0);
            ReadTo("Gyugguh"); Frames(4); Shot("ep28_dialgas_pillar_its_cry");
            IntoBattle("Dialga");
            ToMainMenu(game.Battle); Shot("ep29_dialgas_pillar_dialga");
            Console.WriteLine($"dialga: {Foes()}");
            Win();
            ReadTo(null);
            game.Interact();
            ReadTo("Time itself"); Frames(4); Shot("ep2a_dialgas_pillar_the_rift_is_quiet");
            ReadTo(null);
            Console.WriteLine("dialga: " + Vars("VAR_SPEAR_PILLAR_DIALGA_STATE") + ", " + Flags("FLAG_CAUGHT_DIALGA"));

            // ---- the climb after: Palkia's, with the Lustrous Orb
            Do("mt_coronet_6f", "additem \"Lustrous Orb\"");
            ComeThrough(Summit, 7, 8, Direction.Up);
            Console.WriteLine($"with Dialga met and the Lustrous Orb the way out leads to {WayOut()}; " + Flags("FLAG_SPEAR_PILLAR_HOLDS_DIALGA", "FLAG_SPEAR_PILLAR_HOLDS_PALKIA"));
            ComeThrough("SpearPillarPalkia", 31, 52, Direction.Up);
            At("SpearPillarPalkia", 31, 22, Direction.Up);
            Frames(60); Shot("ep2b_palkias_pillar_the_altar");
            At("SpearPillarPalkia", 33, 18, Direction.Up);
            Frames(30); Shot("ep2c_palkias_pillar_below_the_rift");
            game.Interact();
            ToQuestion("the challenge"); Shot("ep2d_palkias_pillar_will_you_challenge_it");
            Answer(0);
            IntoBattle("Palkia");
            ToMainMenu(game.Battle); Shot("ep2e_palkias_pillar_palkia");
            Console.WriteLine($"palkia: {Foes()}");
            Win();
            ReadTo(null);
            Console.WriteLine("palkia: " + Vars("VAR_SPEAR_PILLAR_PALKIA_STATE") + ", " + Flags("FLAG_CAUGHT_PALKIA"));
            ComeThrough(Summit, 7, 8, Direction.Up);
            Console.WriteLine($"with both met the way out leads to {WayOut()}");
            // The Hall of Fame puts back whichever wasn't caught
            Do(Hall, "call Victory");
            ComeThrough(Summit, 7, 8, Direction.Up);
            Console.WriteLine("after the next victory: " + Vars("VAR_SPEAR_PILLAR_DIALGA_STATE", "VAR_SPEAR_PILLAR_PALKIA_STATE") + $", the way out leads to {WayOut()}");
        }

        // The National Pokédex, the Battle Zone, and Charon caught on Stark Mountain (plan 02 · S15, parts a and b)
        foreach (var entry in Pokedex.Entries(Pokedex.Sinnoh)) game.Pokedex.RegisterSeen(entry.Species.DexNumber);
        Do("RowanLab", "nationaldex");
        Do("stark_mountain_room_3", "setflag FLAG_ARRESTED_CHARON_STARK_MOUNTAIN\nsetvar VAR_STARK_MOUNTAIN_ROOM_3_STATE 1\nclearflag FLAG_HIDE_BATTLEGROUND_BUCK\nclearflag FLAG_HIDE_SURVIVAL_AREA_BUCK\nclearflag FLAG_HIDE_SURVIVAL_AREA_RIVAL");

        if (stage <= 3)
        {
            // ---- the Battleground from the street, Buck before its door
            ComeThrough("Sinnoh", 653, 342, Direction.Up);
            Frames(60); Shot("ep30_survival_area_the_battleground");
            TalkTo("Sinnoh", "buck", "survival_area");
            ReadTo("my place"); Frames(4); Shot("ep31_survival_area_this_is_my_place");
            ReadTo(null);
            At("Sinnoh", 652, 339, Direction.Up);
            Frames(20); Shot("ep32_survival_area_the_door");
            Console.WriteLine($"the door: solid {MapDatabase.Get("Sinnoh").IsSolid(652, 338)}, leads to {MapDatabase.Get("Sinnoh").GetWarpAt(652, 338)?.TargetMap}");

            // A day that has Roark, his father, Fantina and Cheryl in, so that every place is taken
            Do(Room, "setflag FLAG_DAILY_SET_BATTLEGROUND_TRAINERS\nsetvar VAR_BATTLEGROUND_TRAINER_1 0\nsetvar VAR_BATTLEGROUND_TRAINER_2 6\nsetvar VAR_BATTLEGROUND_TRAINER_3 4\nsetvar VAR_BATTLEGROUND_TRAINER_4 0\n"
                + "clearflag FLAG_HIDE_BATTLEGROUND_TRAINER_4\nsetflag FLAG_TRAVELED_WITH_CHERYL\nsetvar VAR_RANDOM_BATTLEGROUND_MESSAGE 0");

            // ---- in at the door: the old man sees who has come, and the player goes over to his bar
            ComeThrough(Room, 7, 11, Direction.Up);
            Until(() => engine.ScriptRunning, "the welcome", 60);
            Frames(20); Shot("ep33_battleground_the_old_man_looks_up");
            ReadTo("Buck's friend"); Frames(4); Shot("ep34_battleground_bucks_friend");
            ReadTo("private club"); Frames(4); Shot("ep35_battleground_a_private_club");
            ReadTo("Welcome to the Battleground"); Frames(4); Shot("ep36_battleground_welcome");
            ReadTo(null);
            Console.WriteLine("the welcome: " + Vars("VAR_BATTLEGROUND_STATE") + $", player at {Me().GridX},{Me().GridY}; in today: {In()}");
            At(Room, 7, 10, Direction.Up);
            Frames(30); Shot("ep37_battleground_the_room_from_its_door");
            At(Room, 5, 6, Direction.Up);
            Frames(30); Shot("ep38_battleground_the_west_of_the_room");
            At(Room, 10, 6, Direction.Right);
            Frames(30); Shot("ep39_battleground_the_bar");

            // ---- Buck: his place is the player's, and he is off to the Tower
            TalkTo(Room, "buck", null, -1, 0);
            ReadTo("toughest Trainers"); Frames(4); Shot("ep40_battleground_buck");
            ReadTo("Battle Frontier"); Frames(4); Shot("ep41_battleground_off_to_the_tower");
            On();
            Walking(Room, "buck", b => b.GridX <= 8, "Buck leaving");
            Shot("ep42_battleground_buck_leaves");
            ReadTo(null); Frames(10); Shot("ep43_battleground_without_buck");
            Console.WriteLine("buck: " + Flags("FLAG_TALKED_TO_BATTLEGROUND_BUCK", "FLAG_BUCK_LEFT_BATTLEGROUND", "FLAG_HIDE_BATTLEGROUND_BUCK"));

            // ---- a guest refused, asked again, battled
            TalkTo(Room, "roark", null, 0, -1);
            ToQuestion("Roark's question"); Shot("ep44_battleground_roark_asks");
            Answer(1);
            ReadTo("Spheres"); Frames(4); Shot("ep45_battleground_roark_refused");
            ReadTo(null);
            TalkTo(Room, "roark", null, 0, -1);
            ToQuestion("Roark's second question"); Shot("ep46_battleground_roark_asks_again");
            Answer(0);
            ReadTo("pride and joy"); Frames(4); Shot("ep47_battleground_roarks_pride_and_joy");
            IntoBattle("Roark");
            ToMainMenu(game.Battle); Shot("ep48_battleground_roarks_rematch");
            Console.WriteLine($"roark: {Foes()}");
            Win();
            ReadTo("my dad"); Frames(4); Shot("ep49_battleground_roark_of_his_father");
            ReadTo(null);
            Console.WriteLine("roark: " + Vars("VAR_TIMES_BATTLED_AT_BATTLEGROUND") + ", " + Flags("FLAG_DAILY_DEFEATED_BATTLEGROUND_ROARK"));

            // ---- his father, in the middle of the floor
            TalkTo(Room, "byron", null, 0, -1);
            ToQuestion("Byron's question"); Shot("ep4a_battleground_byron_asks");
            Answer(0);
            IntoBattle("Byron");
            ToMainMenu(game.Battle); Shot("ep4b_battleground_byrons_rematch");
            Console.WriteLine($"byron: {Foes()}");
            Win();
            ReadTo("old man"); Frames(4); Shot("ep4c_battleground_byron_of_his_son");
            ReadTo(null);

            // ---- Fantina at the big table, and Cheryl before the screen
            TalkTo(Room, "fantina", null, 1, 0);
            ToQuestion("Fantina's question"); Shot("ep4d_battleground_fantina_asks");
            Answer(0);
            IntoBattle("Fantina");
            ToMainMenu(game.Battle); Shot("ep4e_battleground_fantinas_rematch");
            Console.WriteLine($"fantina: {Foes()}");
            Win();
            ReadTo("purpose"); Frames(4); Shot("ep4f_battleground_fantinas_purpose");
            ReadTo(null);
            TalkTo(Room, "cheryl", null, -1, 0);
            ToQuestion("Cheryl's question"); Shot("ep50_battleground_cheryl_asks");
            Answer(0);
            IntoBattle("Cheryl");
            ToMainMenu(game.Battle); Shot("ep51_battleground_cheryls_battle");
            Console.WriteLine($"cheryl: {Foes()}");
            Win();
            ReadTo("tougher"); Frames(4); Shot("ep52_battleground_cheryl_afterwards");
            ReadTo(null);

            // ---- the old man, across his bar
            At(Room, 11, 9, Direction.Right);
            Frames(4);
            game.Interact();
            ReadTo("Stark Mountain"); Frames(4); Shot("ep53_battleground_the_old_man_of_stark_mountain");
            ReadTo(null);
            Console.WriteLine("the day: " + Vars("VAR_TIMES_BATTLED_AT_BATTLEGROUND") + ", "
                + Flags("FLAG_DAILY_DEFEATED_BATTLEGROUND_ROARK", "FLAG_DAILY_DEFEATED_BATTLEGROUND_BYRON", "FLAG_DAILY_DEFEATED_BATTLEGROUND_FANTINA", "FLAG_DAILY_DEFEATED_BATTLEGROUND_CHERYL"));

            // ---- another day: the game's own draw
            story.ClearDaily();
            ComeThrough(Room, 7, 11, Direction.Up);
            ReadTo(null);
            At(Room, 7, 10, Direction.Up);
            Frames(30); Shot("ep54_battleground_another_day");
            Console.WriteLine("another day: " + Vars("VAR_BATTLEGROUND_TRAINER_1", "VAR_BATTLEGROUND_TRAINER_2", "VAR_BATTLEGROUND_TRAINER_3", "VAR_BATTLEGROUND_TRAINER_4", "VAR_RANDOM_BATTLEGROUND_MESSAGE")
                + ", " + Flags("FLAG_HIDE_BATTLEGROUND_TRAINER_4") + $"; in today: {In()}");

            // ---- and Heatran, which Buck's word wakes
            Do("stark_mountain_room_3", "setflag FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_TEAM_GALACTIC\nsetflag FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_BUCK\nsetflag FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_MAGMA_STONE\nsetflag FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_ROCK_SMASH_LOOKER");
            ComeThrough("StarkMountainRoom3", 7, 16, Direction.Up);
            Console.WriteLine("stark mountain: " + Flags("FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_HEATRAN") + $", heatran {(Someone("StarkMountainRoom3", "heatran") != null ? "awake" : "not there")}");
        }

        // ---- Cynthia before the painting
        ComeThrough(Ruins, 4, 10, Direction.Up);
        At(Ruins, 9, 3, Direction.Up);
        Frames(30); Shot("ep60_ruins_before_the_painting");
        game.Interact();
        Walking(Ruins, "cynthia", c => c.GridY <= 6, "Cynthia coming in");
        Shot("ep61_ruins_cynthia_comes_in");
        ReadTo("always been read"); Frames(4); Shot("ep62_ruins_how_it_has_always_been_read");
        ToQuestion("may I go on"); Shot("ep63_ruins_may_i_go_on");
        Answer(0);
        ReadTo("another way to read it"); Frames(4); Shot("ep64_ruins_another_way_to_read_it");
        ToQuestion("a little longer"); Shot("ep65_ruins_a_little_longer");
        Answer(0);
        ReadTo("old Plates"); Frames(4); Shot("ep66_ruins_the_old_plates");
        ReadTo("where Giratina belongs"); Frames(4); Shot("ep67_ruins_where_giratina_belongs");
        ToQuestion("one last thing"); Shot("ep68_ruins_one_last_thing");
        Answer(0);
        ReadTo("studies myths"); Frames(4); Shot("ep69_ruins_someone_who_studies_myths");
        ReadTo("meet again"); On();
        Walking(Ruins, "cynthia", c => c.GridX <= 6, "Cynthia leaving");
        Shot("ep6a_ruins_cynthia_leaves");
        ReadTo(null); Frames(10); Shot("ep6b_ruins_the_painting_alone");
        Console.WriteLine("cynthia: " + Flags("FLAG_TALKED_TO_CELESTIC_TOWN_CAVE_CYNTHIA", "FLAG_HIDE_CELESTIC_TOWN_CAVE_CYNTHIA") + $", {(Someone(Ruins, "cynthia") != null ? "still there" : "gone")}");
        // Heard again after the next victory, and cut short at her first question
        Do(Hall, "call Victory");
        ComeThrough(Ruins, 4, 10, Direction.Up);
        At(Ruins, 10, 3, Direction.Up);
        game.Interact();
        ToQuestion("may I go on");
        Answer(1);
        ReadTo("carried away"); Frames(4); Shot("ep6c_ruins_she_got_carried_away");
        ReadTo(null);
        Console.WriteLine("cut short: " + Flags("FLAG_TALKED_TO_CELESTIC_TOWN_CAVE_CYNTHIA", "FLAG_HIDE_CELESTIC_TOWN_CAVE_CYNTHIA"));
    }
}
