partial class Harness
{
    /// <summary>
    /// Johto's first chapter played by its own scripts (plan 20 · J4), the end of the <c>johto</c> mode: Mom and the
    /// Pokégear, the boy at the lab's window, Elm's favour, the three in his machine, the Running Shoes, the Guide Gent,
    /// Mr. Pokémon's Egg and Oak's Pokédex, the red-haired boy in Cherrygrove City and his battle, the policeman, the
    /// catching lesson, Sprout Tower's top and Elder Li, Falkner's Gym, his battle and the Zephyr Badge, the Egg in
    /// Violet City and Route 32's head (<c>jst01</c> to <c>jst22</c>), through the game's own steps, <c>TryInteract</c>
    /// and arrivals. It wins every battle a scene starts (the lead never tires) and prints what each scene left behind.
    /// </summary>
    private void JohtoScenes(bool second = false, bool third = false, bool fourth = false)
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
            // The field must have been idle for a second before the line counts as never said: counting from the call
            // gave up between two scripts of one scene (Kurt's, from the well to his house) once the lines before had
            // taken a second to read, which Romanian's longer lines did (plan 17 · N6h)
            int idle = 0;
            for (int i = 0; i < most; i++)
            {
                if (text != null && Box().IsActive && Translated.Saying(Box().CurrentLine, text)) { Whole(); Console.WriteLine($"  said: {text}"); return; }
                if (text != null && !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld) { if (++idle > 60) break; }
                else idle = 0;
                if (text == null && !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld) return;
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
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    foreach (var foe in fight.EnemyParty?.Members ?? new List<Pokemon>()) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
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
            Console.WriteLine($"  {what}: house {story.Var("VAR_SCENE_PLAYERS_HOUSE_1F")}, lab {story.Var("VAR_SCENE_ELMS_LAB")}, town {story.Var("VAR_SCENE_NEW_BARK_TOWN_OW")}, "
                + $"cherrygrove {story.Var("VAR_SCENE_CHERRYGROVE_CITY_OW")}, lesson {story.Var("VAR_UNK_408B")}, "
                + $"team {string.Join(", ", game.Party.Members.Select(p => p.IsEgg ? "an Egg" : $"{p.Species.Name} {p.Level}"))}, "
                + $"Pokégear {story.Has("FLAG_GOT_POKEGEAR")}, map card {story.Has("FLAG_GOT_MAP_CARD")}, Pokédex {story.Has(StoryState.PokedexFlag)}, "
                + $"shoes {story.Has(StoryState.RunningShoesFlag)}, Zephyr Badge {story.HasBadge("Johto", "Zephyr")}, "
                + $"bag {string.Join(", ", game.Bag.AllItems.Select(s => $"{s.Name} x{s.Quantity}"))}");

        // The second chapter (plan 20 · J7) is played in a new game set as the first leaves it, at the mode's end; the
        // third (plan 20 · J9) goes on from where the second ends, or by itself (JOHTO_FROM=sea) in a game set as the
        // second leaves it
        if (second || third || fourth)
        {
            engine.NewGameRegion = "Johto";
            engine.StartNewGame();
            game.State = GameState.Overworld;
            game.SaveSlot = 1;
            game.LocationSign.Hide();
            foreach (string flag in new[] { "FLAG_GOT_POKEGEAR", "FLAG_GOT_STARTER", "FLAG_GOT_MAP_CARD", "FLAG_GOT_MYSTERY_EGG",
                         "FLAG_GAVE_MYSTERY_EGG_TO_ELM", "FLAG_HIDE_NEW_BARK_RIVAL", "FLAG_HIDE_MR_POKEMONS_HOUSE_OAK",
                         "FLAG_HIDE_CHERRYGROVE_GUIDE_GENT", StoryState.PokedexFlag, StoryState.RunningShoesFlag })
                story.Set(flag);
            story.SetVar("VAR_SCENE_PLAYERS_HOUSE_1F", 1);
            story.SetVar("VAR_SCENE_ELMS_LAB", 5);
            story.ChooseStarter("Cyndaquil");
            story.GiveBadge("Johto", "Zephyr");
            game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 40));
            if (!third && !fourth) goto ChapterTwo;
            foreach (string badge in new[] { "Hive", "Plain", "Fog" }) story.GiveBadge("Johto", badge);
            story.SetVar("VAR_UNK_4079", 4);
            game.Bag.AddItem(ItemDatabase.Get("HM03")!, 1);
            if (!fourth) goto ChapterThree;
            foreach (string badge in new[] { "Storm", "Mineral" }) story.GiveBadge("Johto", badge);
            story.Set("FLAG_GOT_HM02");
            game.Bag.AddItem(ItemDatabase.Get("HM02")!, 1);
            goto ChapterFour;
        }

        // A new game of Johto, from its first step
        engine.NewGameRegion = "Johto";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        Frames(30);
        Shot("jst00_the_bedroom");
        Left("the bedroom");

        // ---- Mom and the Pokégear
        ComeThrough("NewBark_PlayersHouse_1F", 4, 3, Direction.Down);
        ReadTo("Your Pokégear!"); Frames(10); Shot("jst01_mom_and_the_pokegear");
        ReadTo(null);
        Left("downstairs");

        // ---- the boy at the lab's window, and Elm's favour
        TalkTo("Johto", "t20_gsrivel");
        ReadTo("Get lost."); Frames(10); Shot("jst02_the_boy_at_the_window");
        ReadTo(null);
        StepOnto("NewBark_ElmsLab", 4, 10, Direction.Up);
        ReadTo("In the machine behind my desk"); Frames(10); Shot("jst03_elms_favour");
        ReadTo(null);

        // ---- the three in the machine
        At("NewBark_ElmsLab", 6, 4, Direction.Up);
        game.Interact();
        Until(() => State() == GameState.StarterSelect, "the choice of three", 600);
        Frames(40); Shot("jst04_the_three");
        var choice = game.StarterSelect;
        choice.SelectedIndex = 1;
        choice.Confirm();
        choice.Move(1);
        Frames(20);
        InputManager.Feed(ActionFrame.Press(GameAction.Confirm)); Frames(1); InputManager.Feed(null);
        ReadTo("A wonderful choice!"); Frames(10); Shot("jst05_a_wonderful_choice");
        ReadTo(null);
        StepOnto("NewBark_ElmsLab", 4, 11, Direction.Down);
        ReadTo(null);
        Left("a Pokémon of their own");

        // ---- Mom's Running Shoes on the lab's step
        StepOnto("Johto", 684, 394, Direction.Down);
        ReadTo("received the Running Shoes"); Frames(10); Shot("jst06_the_running_shoes");
        ReadTo(null);

        // ---- the Guide Gent's tour
        StepOnto("Johto", 566, 397, Direction.Left);
        ReadTo("Shall I show you round Cherrygrove?"); Frames(10); Shot("jst07_the_guide_gent");
        ReadTo("And this is my house"); Frames(10); Shot("jst08_the_guide_gents_house");
        ReadTo(null);

        // ---- Mr. Pokémon's Egg and Oak's Pokédex
        ComeThrough("Route30_MrPokemonsHouse", 4, 8, Direction.Up);
        ReadTo("It's an Egg!"); Frames(10); Shot("jst09_mr_pokemon");
        ReadTo("It's a Pokédex."); Frames(10); Shot("jst10_professor_oak");
        ReadTo("Something terrible has happened!"); Frames(10); Shot("jst11_elms_call");
        ReadTo(null);
        Left("the errand run");

        // ---- the red-haired boy in Cherrygrove
        StepOnto("Johto", 575, 398, Direction.Left);
        ReadTo("A waste on someone like you."); Frames(10); Shot("jst12_the_red_haired_boy");
        IntoBattle("the rival's battle");
        ToMainMenu(game.Battle); Shot("jst13_the_rivals_battle");
        Win();
        ReadTo("My name?"); Frames(10); Shot("jst14_his_name");
        ReadTo(null);

        // ---- the theft and the policeman; the catching lesson
        ComeThrough("NewBark_ElmsLab", 4, 13, Direction.Up);
        ReadTo("So it was you, was it?"); Frames(10); Shot("jst15_the_policeman");
        ReadTo(null);
        StepOnto("Johto", 666, 398, Direction.Left);
        ReadTo("Watch me!"); Frames(10); Shot("jst16_the_catching_lesson");
        ReadTo(null);
        Left("the lesson");

        // ---- Sprout Tower's top and Elder Li
        StepOnto("SproutTower_3F", 13, 17, Direction.Up);
        ReadTo("you treat your Pokémon as tools"); Frames(10); Shot("jst17_sprout_tower_top");
        ReadTo(null);
        TalkTo("SproutTower_3F", "d15r0103_chourou");
        IntoBattle("Elder Li's battle");
        Win();
        ReadTo("TM70 holds Flash."); Frames(10); Shot("jst18_elder_li");
        ReadTo(null);

        // ---- Falkner and the Zephyr Badge
        At("Violet_Gym", 15, 26, Direction.Up); Frames(30); Shot("jst19_violet_gym");
        TalkTo("Violet_Gym", "t22gym0101_gsleader1");
        ReadTo("I'm Falkner"); Frames(10); Shot("jst20_falkner");
        IntoBattle("Falkner's battle");
        ToMainMenu(game.Battle); Shot("jst21_falkners_battle");
        Win();
        ReadTo("received the Zephyr Badge"); Frames(10); Shot("jst22_the_zephyr_badge");
        ReadTo(null);

        // ---- the Egg in Violet City's Pokémon Center, and Route 32's head
        TalkTo("Violet_PokemonCenter_1F", "violet_pokecenter_1f.Aide");
        ReadTo("Will you look after it?"); Frames(10); Shot("jst23_the_egg");
        ReadTo(null);
        StepOnto("Johto", 475, 305, Direction.Down);
        ReadTo("That's the Zephyr Badge"); Frames(10); Shot("jst24_route_32");
        ReadTo(null);
        Left("the chapter's end");
        return;

    // ================= The second chapter (plan 20 · J7): the Slowpoke Well to the Fog Badge (jst30 to jst57)
    ChapterTwo:
        void Second(string what) =>
            Console.WriteLine($"  {what}: well {story.Var("VAR_UNK_4080")}, rival {story.Var("VAR_UNK_4075")}, ecruteak {story.Var("VAR_UNK_4079")}, "
                + $"theater {story.Var("VAR_UNK_410C")}, rockets beaten {story.Has("FLAG_BEAT_AZALEA_ROCKETS")}, HM01 {story.Has("FLAG_GOT_HM01")}, "
                + $"tree {story.Has("FLAG_HIDE_ROUTE_36_SUDOWOODO")}, Bill {story.Has("FLAG_SYS_MET_BILL")}, radio {story.Has("FLAG_GOT_RADIO_CARD")}, "
                + $"badges {string.Join(", ", new[] { "Zephyr", "Hive", "Plain", "Fog" }.Where(b => story.HasBadge("Johto", b)))}, "
                + $"roaming {string.Join(", ", game.Encounters.Roamers.Where(r => r.Active).Select(r => $"{r.Species} on {Roamers.PlaceOf(r)}"))}, "
                + $"team {string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} {p.Level}"))}, "
                + $"bag {string.Join(", ", game.Bag.AllItems.Select(s => $"{s.Name} x{s.Quantity}"))}");
        void Answer(int pick)
        {
            for (int i = 0; i < 900 && !engine.Choice.IsOpen; i++)
            {
                if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            Frames(10);
            engine.Choice.Move(pick);
            engine.Choice.Confirm();
            Frames(4);
        }
        var lead = game.Party.Members[0];

        // ---- Azalea Town: the grunt by the well, and Kurt's run
        StepOnto("Johto", 434, 466, Direction.Up);
        ReadTo("Nobody goes near that well"); Frames(10); Shot("jst30_the_shove");
        ReadTo(null);
        TalkTo("Azalea_KurtsHouse", "t23r0501_gantetsu");
        ReadTo("Stand aside!"); Frames(10); Shot("jst31_kurt");
        ReadTo(null);
        Second("Kurt has gone");

        // ---- the Slowpoke Well: Proton, Kurt and the Lure Ball
        TalkTo("SlowpokeWell_B1F", "d26r0102_rkanbum3");
        ReadTo("I'm Proton"); Frames(10); Shot("jst32_proton");
        IntoBattle("Proton's battle");
        ToMainMenu(game.Battle); Shot("jst33_protons_battle");
        Win();
        ReadTo("They've gone!"); Frames(10); Shot("jst34_kurt_in_the_well");
        ReadTo("one of my own"); Frames(10); Shot("jst35_kurts_thanks");
        ReadTo(null);
        Second("the well cleared");

        // ---- the Azalea Gym: its carts, Bugsy and the Hive Badge
        At("Azalea_Gym", 9, 35, Direction.Up); Frames(30); Shot("jst36_azalea_gym");
        TalkTo("Azalea_Gym", "t23gym0102_gsleader2");
        ReadTo("I'm Bugsy"); Frames(10); Shot("jst37_bugsy");
        IntoBattle("Bugsy's battle");
        Win();
        ReadTo("received the Hive Badge"); Frames(10); Shot("jst38_the_hive_badge");
        ReadTo(null);
        lead.CurrentHP = lead.MaxHP;

        // ---- the red-haired boy at the gate to Ilex Forest
        StepOnto("Johto", 395, 463, Direction.Left);
        ReadTo("chased Team Rocket out of the well"); Frames(10); Shot("jst39_the_rival_in_azalea");
        IntoBattle("the rival's battle");
        Win();
        ReadTo(null);

        // ---- Ilex Forest: the apprentice, both Farfetch'd and HM01
        At("IlexForest", 15, 67, Direction.Up); Frames(30); Shot("jst40_the_apprentice");
        foreach (string bird in new[] { "d36r0101_tsure_poke_static_farfetchd", "d36r0101_tsure_poke_static_farfetchd_2" })
            for (int tries = 0; tries < 3; tries++)
            {
                var ff = game.Map.Name == "IlexForest" ? game.Map.NPCs.FirstOrDefault(n => n.Key == bird) : null;
                if (ff == null) { At("IlexForest", 15, 67, Direction.Up); ff = game.Map.NPCs.FirstOrDefault(n => n.Key == bird); }
                if (ff == null) break;
                At("IlexForest", ff.GridX + 1, ff.GridY, Direction.Left);
                game.Interact();
                if (tries == 0 && bird.EndsWith("farfetchd")) { Until(() => Box().IsActive, "the bird's cry"); Frames(20); Shot("jst41_a_farfetchd"); }
                ReadTo(null);
            }
        ReadTo(null);
        Second("both birds found");
        Shot("jst42_hm01");

        // ---- Goldenrod City: the SquirtBottle, the Bicycle and the Radio Card (Whitney's Badge is given)
        story.GiveBadge("Johto", "Plain");
        TalkTo("Goldenrod_FlowerShop", "t25r0601_gswoman1");
        ReadTo("give the tree a good squirt"); Frames(10); Shot("jst43_the_squirtbottle");
        ReadTo(null);
        TalkTo("Goldenrod_BikeShop", "t25r0201_gsmiddleman1");
        ReadTo("Ride it everywhere you go"); Frames(10); Shot("jst44_the_bicycle");
        ReadTo(null);
        TalkTo("Goldenrod_RadioTower_1F", "d23r0101_gswoman6_3");
        foreach (int pick in new[] { 0, 0, 0, 1, 0, 1 }) Answer(pick);
        ReadTo("Here's your prize"); Frames(10); Shot("jst45_the_radio_card");
        ReadTo(null);

        // ---- Route 36: the strange tree watered and battled, and the way north
        TalkTo("Johto", "r36_usokky", 1, 0);
        ReadTo("Use the SquirtBottle on it?"); Frames(10); Shot("jst46_the_strange_tree");
        IntoBattle("the tree's battle");
        ToMainMenu(game.Battle); Shot("jst47_sudowoodo");
        Win();
        ReadTo("These are from my sister's garden"); Frames(10); Shot("jst48_the_sister");
        ReadTo(null);
        Second("the tree gone");
        lead.CurrentHP = lead.MaxHP;

        // ---- Ecruteak City: Bill, the Burned Tower and the three set loose
        ComeThrough("Ecruteak_PokemonCenter_1F", 8, 18, Direction.Up);
        ReadTo("I'm Bill"); Frames(10); Shot("jst49_bill");
        ReadTo(null);
        StepOnto("BurnedTower_1F", 22, 19, Direction.Up);
        ReadTo("Let's battle!"); Frames(10); Shot("jst50_the_rival_in_the_burned_tower");
        IntoBattle("the rival's battle in the tower");
        Win();
        ReadTo(null);
        StepOnto("BurnedTower_B1F", 20, 16, Direction.Left);
        ReadTo("The three Pokémon have woken up!"); Frames(10); Shot("jst51_the_three_wake");
        ReadTo("Was that... Suicune?"); Frames(10); Shot("jst52_eusine");
        ReadTo(null);
        Second("the three set loose");

        // ---- the Dance Theater: the grunt and Surf
        ComeThrough("Ecruteak_DanceTheater", 7, 16, Direction.Up);
        ReadTo("Somebody, help!"); Frames(10); Shot("jst53_the_grunt_on_the_stage");
        ReadTo(null);
        TalkTo("Ecruteak_DanceTheater", "t27r0501_rocketm");
        IntoBattle("the grunt's battle");
        Win();
        ReadTo(null);
        StepOnto("Ecruteak_DanceTheater", 3, 8, Direction.Up);
        ReadTo("That's Surf."); Frames(10); Shot("jst54_surf");
        ReadTo(null);

        // ---- Morty and the Fog Badge, and the Kimono Girls on their stage
        ComeThrough("Ecruteak_Gym", 16, 52, Direction.Up);
        Frames(20); Shot("jst55_ecruteak_gym");
        TalkTo("Ecruteak_Gym", "t27gym0101_gsleader4");
        ReadTo("Good of you to come."); Frames(10); Shot("jst56_morty");
        IntoBattle("Morty's battle");
        Win();
        ReadTo("received the Fog Badge"); Frames(10); Shot("jst57_the_fog_badge");
        ReadTo(null);
        StepOnto("Johto", 397, 174, Direction.Up);
        ReadTo("Watch where you're going!"); Frames(10); Shot("jst58_the_rival_in_ecruteak");
        ReadTo(null);
        ComeThrough("Ecruteak_DanceTheater", 7, 16, Direction.Up);
        Frames(20); Shot("jst59_the_kimono_girls");
        Second("the chapter's end");

    // ================= The third chapter (plan 20 · J9): Olivine's lighthouse to the Mineral Badge (jst60 to jst79)
    ChapterThree:
        void Third(string what) =>
            Console.WriteLine($"  {what}: rival at the Gym {story.Var("VAR_UNK_4078")}, lighthouse {story.Var("VAR_SCENE_LIGHTHOUSE_JASMINE")}, "
                + $"lift open {story.Has("FLAG_UNK_1D8")}, potion {story.Has("FLAG_GOT_SECRETPOTION")}, Fly {story.Has("FLAG_GOT_HM02")}, "
                + $"Suicune {story.Var("VAR_UNK_4076")}, farm {story.Var("VAR_MOOMOO_FARM_BERRIES_FED")} berries, cured {story.Has("FLAG_UNK_0AA")}, "
                + $"badges {string.Join(", ", new[] { "Zephyr", "Hive", "Plain", "Fog", "Storm", "Mineral" }.Where(b => story.HasBadge("Johto", b)))}, "
                + $"Fly to {string.Join(", ", SpawnLocations.In("Johto").Where(s => story.Has(SpawnLocations.FlyFlag(s))).Select(s => s.Area))}, "
                + $"bag {string.Join(", ", game.Bag.AllItems.Select(s => $"{s.Name} x{s.Quantity}"))}");
        var chief = game.Party.Members[0];
        chief.CurrentHP = chief.MaxHP;

        // ---- Olivine City: the red-haired boy at the Gym's door
        StepOnto("Johto", 272, 243, Direction.Up);
        ReadTo("The Leader's not even in it"); Frames(10); Shot("jst60_the_rival_in_olivine");
        ReadTo(null);
        ComeThrough("Olivine_Gym", 6, 26, Direction.Up);
        Frames(20); Shot("jst61_the_gym_without_jasmine");

        // ---- the light room: Jasmine by Amphy, and the lift (come into by its stairs, so the car knows to go down)
        ComeThrough("OlivineLighthouse_LightRoom", 9, 9, Direction.Up);
        ReadTo(null);
        TalkTo("OlivineLighthouse_LightRoom", "d27r0107_gsleader6");
        ReadTo("This is Amphy"); Frames(10); Shot("jst62_jasmine_and_amphy");
        ReadTo(null);
        Third("Jasmine has asked");
        ComeThrough("OlivineLighthouse_Elevator", 3, 6, Direction.Up);
        Until(() => engine.ScriptRunning, "the lift's ride", 60);
        Frames(90); Shot("jst63_the_lift");
        ReadTo(null);
        Frames(20); Console.WriteLine($"  the lift came down to {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY})");

        // ---- Cianwood: the pharmacy's Secret Potion
        TalkTo("Cianwood_Pharmacy", "t24r0501_sunglasses");
        ReadTo("Then you'll want this"); Frames(10); Shot("jst64_the_pharmacist");
        ReadTo(null);

        // ---- the Cianwood Gym: the waterfall stopped, Chuck and the Storm Badge
        ComeThrough("Cianwood_Gym", 13, 18, Direction.Up);
        Frames(20); Shot("jst65_cianwood_gym");
        At("Cianwood_Gym", 10, 3, Direction.Up);
        game.Interact();
        ReadTo("A big winch works the waterfall's sluice");
        ReadTo("The waterfall slowed to a trickle!"); Frames(10); Shot("jst66_the_winch");
        ReadTo(null);
        TalkTo("Cianwood_Gym", "t24gym0101_gsleader5");
        ReadTo("So you stopped my waterfall"); Frames(10); Shot("jst67_chuck");
        IntoBattle("Chuck's battle");
        ToMainMenu(game.Battle); Shot("jst68_chucks_battle");
        Win();
        ReadTo("received the Storm Badge"); Frames(10); Shot("jst69_the_storm_badge");
        ReadTo(null);
        chief.CurrentHP = chief.MaxHP;
        ComeThrough("Johto", 172, 358, Direction.Down);
        ReadTo("HM02 is Fly"); Frames(10); Shot("jst70_fly");
        ReadTo(null);
        Third("the Storm Badge");

        // ---- Suicune on the north shore, and Eusine
        StepOnto("Johto", 176, 334, Direction.Up);
        ReadTo("Suicune is looking this way!"); Frames(10); Shot("jst71_suicune");
        ReadTo("Did you see it?"); Frames(10); Shot("jst72_eusine");
        IntoBattle("Eusine's battle");
        Win();
        ReadTo(null);
        chief.CurrentHP = chief.MaxHP;

        // ---- Amphy cured, Jasmine's Gym and the Mineral Badge
        TalkTo("OlivineLighthouse_LightRoom", "d27r0107_gsleader6");
        ReadTo("Its tail is shining again!"); Frames(10); Shot("jst73_amphy_cured");
        ReadTo(null);
        ComeThrough("Olivine_Gym", 6, 26, Direction.Up);
        TalkTo("Olivine_Gym", "t26gym0101_gsleader6");
        ReadTo("a Gym Leader's"); Frames(10); Shot("jst74_jasmine");
        IntoBattle("Jasmine's battle");
        ToMainMenu(game.Battle); Shot("jst75_jasmines_battle");
        Win();
        ReadTo("received the Mineral Badge"); Frames(10); Shot("jst76_the_mineral_badge");
        ReadTo(null);

        // ---- the Moomoo Farm: the sick Miltank fed back to health, and its milk
        TalkTo("Route39_MoomooFarmHouse", "r39r0101_gsmiddleman1");
        ReadTo(null);
        game.Bag.AddItem(ItemDatabase.Get("Oran Berry")!, 7);
        for (int berry = 0; berry < 7; berry++)
        {
            TalkTo("Route39_MoomooFarmStable", "r39r0201_tsure_poke_static_miltank");
            if (berry == 6) { ReadTo("It's back on its feet!"); Frames(10); Shot("jst77_miltank_well_again"); }
            ReadTo(null);
        }
        TalkTo("Route39_MoomooFarmHouse", "r39r0101_gsmiddleman1");
        ReadTo("Drink it up while it's fresh"); Frames(10); Shot("jst78_moomoo_milk");
        ReadTo(null);

        // ---- Fly's map over Johto, its towns open as they were come to
        foreach (string town in new[] { "olivine", "cianwood", "ecruteak", "goldenrod" })
            story.Set(SpawnLocations.ArrivedIn("Johto", town)!.ArrivalFlag);
        At("Johto", 272, 258, Direction.Down);
        game.OpenTownMap(TownMapMode.Fly);
        var fly = game.TownMapScreen;
        for (int i = 0; i < 60 && (fly.CursorX, fly.CursorY) != (5, 11); i++) fly.Move(Math.Sign(5 - fly.CursorX), Math.Sign(11 - fly.CursorY));
        Frames(30); Shot("jst79_fly_over_johto");
        Console.WriteLine($"  fly: over {fly.HoveredSpot?.Town.Area ?? "nothing"}, open {fly.HoveredSpot?.Open}");
        fly.Cancel(); Frames(4);
        game.State = GameState.Overworld;
        Third("the chapter's end");

    // ================= The fourth chapter, part a (plan 20 · J11): the Lake of Rage to the Glacier Badge (jst80 to jst8m)
    ChapterFour:
        void Fourth(string what) =>
            Console.WriteLine($"  {what}: lake {story.Has("FLAG_HIDE_LAKE_OF_RAGE_RED_GYARADOS")}, scale {story.Has("FLAG_GOT_RED_SCALE")}, "
                + $"Lance {story.Var("VAR_UNK_40A8")}, stairs {story.Has("FLAG_MAHOGANY_SHOP_STAIRS_FOUND")}, hideout {story.Var("VAR_UNK_40AC")}, "
                + $"B2F {story.Var("VAR_UNK_40A9")}, statues {story.Var("VAR_UNK_40AD")} {story.Var("VAR_UNK_40AE")} {story.Var("VAR_UNK_40AF")} {story.Var("VAR_UNK_40B0")} {story.Var("VAR_UNK_40B1")}, "
                + $"password {story.Has("FLAG_UNK_0D3")}, shutters {story.Has("FLAG_ROCKET_HIDEOUT_B3F_DOOR_OPEN")} {story.Has("FLAG_ROCKET_HIDEOUT_B2F_DOOR_OPEN")}, "
                + $"cleared {story.Has("FLAG_ROCKET_HIDEOUT_CLEARED")}, Glacier Badge {story.HasBadge("Johto", "Glacier")}, takeover {story.Var("VAR_SCENE_ROCKET_TAKEOVER")}, "
                + $"money {game.Money}, bag {string.Join(", ", game.Bag.AllItems.Select(s => $"{s.Name} x{s.Quantity}"))}");
        var fourthLead = game.Party.Members[0];
        fourthLead.CurrentHP = fourthLead.MaxHP;

        // ---- Route 43's gate house: the toll, asked on arrival
        ComeThrough("Route43_Gate", 5, 11, Direction.Up);
        ReadTo("This here's a toll gate"); Frames(10); Shot("jst80_the_toll");
        ReadTo(null);
        Fourth("the toll");

        // ---- the Lake of Rage: the red Gyarados, met shiny, and Lance on the shore
        var lake = MapDatabase.Get("Johto");
        var red = lake.NPCs.First(n => n.Key == "t29_rgyaradosu");
        At("Johto", red.GridX, red.GridY + 1, Direction.Up);
        game.Player.SetMode(TravelMode.Surfing);
        game.EnterArea();
        Frames(10); Shot("jst81_the_red_gyarados");
        game.Interact();
        ReadTo("The red Gyarados is thrashing wildly!");
        IntoBattle("the red Gyarados");
        ToMainMenu(game.Battle); Shot("jst82_the_red_gyarados_in_battle");
        Win();
        ReadTo(null);
        fourthLead.CurrentHP = fourthLead.MaxHP;
        var lance = lake.NPCs.FirstOrDefault(n => n.Key == "t29_wataru");
        if (lance == null) Console.WriteLine("  !! Lance isn't on the lake's shore");
        else
        {
            At("Johto", lance.GridX, lance.GridY + 1, Direction.Up);
            game.Player.SetMode(TravelMode.OnFoot);
            game.Interact();
            ReadTo("Someone is forcing them to evolve"); Frames(10); Shot("jst83_lance_by_the_lake");
            ReadTo(null);
        }
        Fourth("the lake");

        // ---- the souvenir shop: Lance's Dragonite and the stairs behind the counter
        ComeThrough("Mahogany_SouvenirShop", 5, 8, Direction.Up);
        ReadTo("You came. Watch this"); Frames(10); Shot("jst84_lance_in_the_shop");
        ReadTo("Stairs, hidden behind the counter"); Frames(10); Shot("jst85_the_stairs");
        ReadTo(null);

        // ---- B1F: the statues' alarm
        ComeThrough("TeamRocketHQ_B1F", 50, 4, Direction.Left);
        Frames(20); Shot("jst86_the_hideout");
        StepOnto("TeamRocketHQ_B1F", 44, 4, Direction.Right);
        ReadTo("You tripped the alarm"); Frames(10); Shot("jst87_the_alarm");
        ReadTo(null);
        fourthLead.CurrentHP = fourthLead.MaxHP;

        // ---- B2F: Lance at the foot of the stairs
        ComeThrough("TeamRocketHQ_B2F", 11, 29, Direction.Right);
        ReadTo("Let me see to them"); Frames(10); Shot("jst88_lance_in_the_hideout");
        ReadTo(null);

        // ---- B3F: Lance, the two grunts' halves of the password, the shutters, the rival and Petrel
        ComeThrough("TeamRocketHQ_B3F", 50, 29, Direction.Left);
        ReadTo(null);
        TalkTo("TeamRocketHQ_B3F", "d35r0104_rocketw");
        ReadTo(null);
        TalkTo("TeamRocketHQ_B3F", "d35r0104_rocketm_2");
        ReadTo(null);
        fourthLead.CurrentHP = fourthLead.MaxHP;
        TalkTo("TeamRocketHQ_B3F", "d35r0104_babyboy1_9");
        ReadTo(null);
        StepOnto("TeamRocketHQ_B3F", 20, 18, Direction.Right);
        ReadTo("What are you doing in a dump like this"); Frames(10); Shot("jst89_the_rival_in_the_hideout");
        ReadTo(null);
        StepOnto("TeamRocketHQ_B3F", 23, 12, Direction.Up);
        ReadTo("I am Giovanni"); Frames(10); Shot("jst8a_giovanni");
        ReadTo("Fooled you"); Frames(10); Shot("jst8b_petrel");
        IntoBattle("Petrel's battle");
        ToMainMenu(game.Battle); Shot("jst8c_petrels_battle");
        Win();
        ReadTo(null);
        fourthLead.CurrentHP = fourthLead.MaxHP;
        Fourth("Petrel");

        // ---- the Murkrow's chase, and the password it says
        StepOnto("TeamRocketHQ_B2F", 11, 4, Direction.Left);
        Frames(20); Shot("jst8d_the_murkrow");
        ReadTo(null);
        StepOnto("TeamRocketHQ_B3F", 50, 4, Direction.Up);
        ReadTo(null);
        StepOnto("TeamRocketHQ_B2F", 50, 29, Direction.Up);
        ReadTo(null);
        StepOnto("TeamRocketHQ_B2F", 30, 27, Direction.Up);
        ReadTo("The boss is back!"); Frames(10); Shot("jst8e_the_password");
        ReadTo(null);
        TalkTo("TeamRocketHQ_B2F", "d35r0103_babyboy1_9");
        ReadTo(null);
        Fourth("the password");

        // ---- Ariana and a grunt, battled beside Lance
        StepOnto("TeamRocketHQ_B2F", 30, 19, Direction.Up);
        ReadTo("This is where your little adventure ends"); Frames(10); Shot("jst8f_ariana");
        ReadTo("let's take them on together"); Frames(10); Shot("jst8g_lance_beside_the_player");
        IntoBattle("the tag battle with Ariana");
        ToMainMenu(game.Battle); Shot("jst8h_the_tag_battle");
        Win();
        ReadTo(null);
        fourthLead.CurrentHP = fourthLead.MaxHP;

        // ---- the generator's three Electrode
        foreach (string electrode in new[] { "d35r0103_tsure_poke_static_electrode", "d35r0103_tsure_poke_static_electrode_2", "d35r0103_tsure_poke_static_electrode_3" })
        {
            TalkTo("TeamRocketHQ_B2F", electrode, -1, 0);
            if (electrode.EndsWith("electrode")) { ReadTo("An Electrode wired into the generator"); Frames(10); Shot("jst8i_the_generator"); }
            ReadTo("An Electrode wired into the generator");
            fourthLead.CurrentHP = fourthLead.MaxHP;
            if (electrode.EndsWith("_3")) { ReadTo("The signal has stopped"); Frames(10); Shot("jst8j_lance_at_the_generator"); }
            ReadTo(null);
        }
        Fourth("the hideout cleared");

        // ---- the gate house's guard, and Pryce's Gym: a block of ice pushed, Pryce and the Glacier Badge
        ComeThrough("Route43_Gate", 5, 11, Direction.Up);
        ReadTo(null);
        TalkTo("Route43_Gate", "r43r0201_counterm");
        ReadTo(null);
        ComeThrough("Mahogany_Gym", 4, 13, Direction.Up);
        engine.Steering = (Direction.Up, false);
        Frames(12);
        engine.Steering = null;
        Frames(6); Shot("jst8k_a_block_of_ice");
        Frames(60);
        var block = MapDatabase.Get("Mahogany_Gym").NPCs.FirstOrDefault(n => n.Key == "t28gym0103_ice");
        Console.WriteLine($"  the block of ice pushed north from (4, 12) came to rest at ({block?.GridX}, {block?.GridY})");
        ComeThrough("Mahogany_Gym_3", 5, 18, Direction.Up);
        TalkTo("Mahogany_Gym_3", "t28gym0101_gsleader7");
        ReadTo("I am Pryce"); Frames(10); Shot("jst8l_pryce");
        IntoBattle("Pryce's battle");
        Win();
        ReadTo("received the Glacier Badge"); Frames(10); Shot("jst8m_the_glacier_badge");
        ReadTo(null);
        Fourth("the chapter's part a");
    }
}
