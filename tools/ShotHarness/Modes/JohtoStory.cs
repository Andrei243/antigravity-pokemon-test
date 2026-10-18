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
    private void JohtoScenes(bool second = false)
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

        // The second chapter (plan 20 · J7) is played in a new game set as the first leaves it, at the mode's end
        if (second)
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
            goto ChapterTwo;
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
    }
}
