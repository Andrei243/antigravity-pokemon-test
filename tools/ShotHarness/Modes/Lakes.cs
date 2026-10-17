partial class Harness
{
    // The seventh chapter (plan 02 · S10; not part of `all`), played by its own scripts: the cameramen at Valor
    // Lakefront and Team Galactic's grunts in Mt. Coronet's northern hall as the chapter finds them; Riley and HM04 on
    // Iron Island, his partnership two floors down, a tag battle beside him, the grunts before the lift and the Riolu
    // Egg; the rival out of Canalave's Gym and before the library's door, the library's floors, the meeting, the
    // explosion and the television's news, the sailor outside; Lake Valor drained with its Magikarp, Valor Cavern and
    // Commander Saturn; Lake Verity with the professor, the grunts and Commander Mars; the hall clear, Route 211's TM,
    // Maylene on Route 217 and HM08 in its ball. LAKES_FROM=library starts at the rival outside the Gym,
    // LAKES_FROM=valor at Valor Lakefront after the explosion, LAKES_FROM=verity at Lake Verity and LAKES_FROM=north
    // in Mt. Coronet's hall, the story set as the scenes before would have left it.
    public void LakesMode()
    {
        engine.StartNewGame();
        PastTheOpening();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var story = game.Story;
        var bag = game.Bag;
        var team = game.Party;
        var empoleon = new Pokemon(PokemonDatabase.Get("Empoleon")!, 50);
        empoleon.Moves.Clear();
        foreach (string move in new[] { "Surf", "Drill Peck", "Metal Claw", "Brine" }) empoleon.Moves.Add(new Move(MoveDatabase.Get(move)!));
        team.Members.Insert(0, empoleon);
        // The Badges of the chapters before, and the rival beaten on Canalave's bridge
        foreach (var badge in new[] { "Coal", "Forest", "Relic", "Cobble", "Fen" }) story.GiveBadge("Sinnoh", badge);
        story.SetVar("VAR_CANALAVE_CITY_STATE", 1);

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
                if (text != null && Box().IsActive && Translated.Saying(Box().CurrentLine, text)) { Whole(); Console.WriteLine($"  said: {text}"); return; }
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
            for (int guard = 0; guard < 300 && !fight.IsBattleOver; guard++)
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
            Until(() => FieldAgain(), "the field again", 1500);
            foreach (var mine in team.Members.Where(p => !p.IsEgg)) { mine.CurrentHP = mine.MaxHP; mine.Status = StatusCondition.None; }
            Frames(10);
        }
        void IntoBattle(string what)
        {
            for (int i = 0; i < 1500 && State() != GameState.Battle; i++)
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
        void StepOnto(string map, int x, int y, Direction way, bool partner = false)
        {
            var (dx, dy) = way switch { Direction.Up => (0, -1), Direction.Down => (0, 1), Direction.Left => (-1, 0), _ => (1, 0) };
            At(map, x - dx, y - dy, way);
            if (partner) { game.KeepPartnerAlong(); Frames(6); }
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
            Frames(2);
        }
        // Talks to someone of a map (a room's people belong to no area: place null), from below unless told otherwise
        void TalkTo(string map, string key, string? place, int dx = 0, int dy = 1)
        {
            var m = MapDatabase.Get(map);
            var npc = m.NPCs.First(n => n.Key == key && (place == null || n.ScriptFile == place));
            var facing = (dx, dy) switch { (0, 1) => Direction.Up, (0, -1) => Direction.Down, (1, 0) => Direction.Left, _ => Direction.Right };
            At(map, npc.GridX + dx, npc.GridY + dy, facing);
            game.Interact();
        }
        void Where(string what) => Console.WriteLine($"{what}: player at {Me().GridX},{Me().GridY} of {game.Map.Name}");
        string Flags(params string[] flags) => string.Join(", ", flags.Select(f => $"{f.Replace("FLAG_", "")} {story.Has(f)}"));

        Frames(5);
        ReadTo(null, 600);

        string from = Environment.GetEnvironmentVariable("LAKES_FROM") ?? "";
        int stage = from switch { "" => 0, "library" => 1, "valor" => 2, "verity" => 3, "north" => 4, _ => 0 };

        if (stage == 0)
        {
            // ---- as the chapter finds them: the cameramen before Lake Valor, the grunts in Mt. Coronet's northern hall
            At("Sinnoh", 721, 760, Direction.Left); Frames(30); Shot("k01_valor_lakefront_the_cameramen");
            At("MtCoronet1FNorthRoom1", 17, 10, Direction.Left); Frames(30); Shot("k02_coronet_the_grunts_in_the_way");
            TalkTo("MtCoronet1FNorthRoom1", "grunt_m_1", null, 1, 0);
            ReadTo("Defog"); Frames(4); Shot("k03_coronet_the_grunts_talk");
            ReadTo(null);

            // ---- Iron Island: Riley at the tunnels' door, and HM04
            At("Sinnoh", 117, 493, Direction.Up); Frames(30); Shot("k04_iron_island_riley_at_the_door");
            TalkTo("Sinnoh", "riley", "iron_island");
            ReadTo("My name is Riley"); Frames(4); Shot("k05_iron_island_riley");
            ReadTo(null); Frames(20);
            Console.WriteLine($"iron island: HM04 {bag.GetQuantity(ItemDatabase.Get("HM04")!)}, {Flags("FLAG_HIDE_IRON_ISLAND_RILEY")}");
            Shot("k06_iron_island_the_door_clear");

            // ---- two floors down: he asks to team up, and walks behind
            ComeThrough("IronIslandB2FLeft", 37, 3, Direction.Left);
            Frames(30); Shot("k07_b2f_riley_waits");
            StepOnto("IronIslandB2FLeft", 35, 3, Direction.Left);
            ReadTo("team up"); Frames(4); Shot("k08_b2f_riley_asks");
            ReadTo("decided to go with Riley"); Frames(4); Shot("k09_b2f_together");
            ReadTo(null);
            Console.WriteLine($"b2f: partner {engine.ScriptRunning == false && story.Var("VAR_IRON_ISLAND_B2F_LEFT_ROOM_FOLLOWER_RILEY_STATE") == 1}");
            At("IronIslandB2FLeft", 33, 2, Direction.Left);
            game.KeepPartnerAlong();
            Frames(6);
            engine.Steering = (Direction.Left, false);
            Until(() => Me().GridX <= 30, "a few steps with Riley", 240);
            engine.Steering = null;
            Frames(20); Shot("k10_b2f_riley_follows");

            // ---- two trainers who face one another take on the pair of them
            At("IronIslandB2FLeft", 25, 30, Direction.Down);
            game.KeepPartnerAlong();
            Frames(10);
            engine.Steering = (Direction.Down, false);
            Until(() => Me().GridY == 31, "the step between the pair", 120);
            engine.Steering = null;
            Frames(30); Shot("k11_b2f_the_pair_see_the_player");
            Until(() => engine.ScriptRunning, "the pair's challenge", 300);
            IntoBattle("the pair's battle");
            ToMainMenu(game.Battle); Shot("k12_b2f_tag_battle_beside_riley");
            Win();
            ReadTo(null);
            Console.WriteLine($"b2f: after the tag battle, team healed {team.Members.All(p => p.CurrentHP == p.MaxHP)}");

            // ---- the grunts before the lift, and the Egg
            StepOnto("IronIslandB2FLeft", 21, 40, Direction.Left, partner: true);
            ReadTo("frightening the Pokémon"); Frames(4); Shot("k13_b2f_the_grunts");
            IntoBattle("the grunts");
            ToMainMenu(game.Battle); Shot("k14_b2f_the_grunts_battle");
            Win();
            ReadTo("Pokémon Egg"); Frames(4); Shot("k15_b2f_riley_offers_the_egg");
            ReadTo("received the Egg"); Frames(4); Shot("k16_b2f_the_egg");
            ReadTo(null); Frames(20);
            Shot("k17_b2f_the_lift_clear");
            Console.WriteLine($"b2f: egg {team.Members.Count(p => p.IsEgg)} ({team.Members.FirstOrDefault(p => p.IsEgg)?.Species.Name}), state {story.Var("VAR_IRON_ISLAND_B2F_LEFT_ROOM_FOLLOWER_RILEY_STATE")}, "
                + Flags("FLAG_RECEIVED_RIOLU_EGG_FROM_RILEY", "FLAG_HIDE_IRON_ISLAND_B2F_LEFT_ROOM_GRUNTS", "FLAG_HIDE_IRON_ISLAND_B2F_LEFT_ROOM_RILEY"));
        }
        else
        {
            // As Iron Island's scenes would have left it
            bag.AddItem(ItemDatabase.Get("HM04")!, 1);
            foreach (string flag in new[] { "FLAG_HIDE_IRON_ISLAND_RILEY", "FLAG_HIDE_IRON_ISLAND_B2F_LEFT_ROOM_GRUNTS", "FLAG_HIDE_IRON_ISLAND_B2F_LEFT_ROOM_RILEY", "FLAG_RECEIVED_RIOLU_EGG_FROM_RILEY" })
                story.Set(flag);
            story.SetVar("VAR_IRON_ISLAND_B2F_LEFT_ROOM_FOLLOWER_RILEY_STATE", 2);
        }

        if (stage <= 1)
        {
            // ---- Canalave City: Byron's Badge (as his Gym's script leaves the story), and the rival out of the Gym's door
            story.GiveBadge("Sinnoh", "Mine");
            foreach (string flag in new[] { "FLAG_HIDE_CANALAVE_CITY_RIVAL_BRIDGE", "FLAG_HIDE_CANALAVE_CITY_RIVAL_LIBRARY", "FLAG_HIDE_CANALAVE_LIBRARY_RIVAL",
                "FLAG_HIDE_CANALAVE_LIBRARY_COUNTERPART", "FLAG_HIDE_CANALAVE_LIBRARY_ROWAN" })
                story.Unset(flag);
            story.SetVar("VAR_CANALAVE_CITY_STATE", 2);
            ComeThrough("Sinnoh", 39, 732, Direction.Down);
            ReadTo("Byron's Badge"); Frames(4); Shot("k20_canalave_the_rival_outside_the_gym");
            ReadTo(null); Frames(10);
            Console.WriteLine($"canalave: state {story.Var("VAR_CANALAVE_CITY_STATE")}, library state {story.Var("VAR_CANALAVE_LIBRARY_STATE")}");

            // ---- the rival before the library's door
            At("Sinnoh", 37, 721, Direction.Up); Frames(30); Shot("k21_canalave_the_rival_at_the_library");
            TalkTo("Sinnoh", "rival_library", "canalave_city");
            ReadTo("too slow"); Frames(4); Shot("k22_canalave_the_rival_lets_the_player_in");
            ReadTo(null); Frames(10);

            // ---- the library's floors
            ComeThrough("CanalaveLibrary1F", 11, 11, Direction.Up);
            Frames(30); Shot("k23_library_1f");
            At("CanalaveLibrary1F", 6, 6, Direction.Left); Frames(20); Shot("k24_library_1f_the_stacks");
            ComeThrough("CanalaveLibrary2F", 3, 4, Direction.Left);
            Frames(30); Shot("k25_library_2f");

            // ---- the meeting, the explosion and the news
            ComeThrough("CanalaveLibrary3F", 3, 3, Direction.Left);
            ReadTo("Over here"); Frames(4); Shot("k26_library_3f_the_rival_at_the_stairs");
            ReadTo("safely delivered"); Frames(4); Shot("k27_library_3f_at_the_table");
            ReadTo("three great lakes"); Frames(4); Shot("k28_library_3f_the_professor");
            ReadTo("Lake Acuity is yours"); Frames(4); Shot("k29_library_3f_the_lakes_shared_out");
            ReadTo("library shook"); Frames(2); Shot("k30_library_3f_the_explosion");
            ReadTo("tremendous explosion"); Frames(4); Shot("k31_library_3f_the_television");
            ReadTo("Outside, all of you"); Frames(4); Shot("k32_library_3f_outside");
            ReadTo("Don't dawdle"); Frames(4); Shot("k33_library_3f_the_professor_at_the_stairs");
            ReadTo(null); Frames(20);
            Shot("k34_library_3f_after");
            Console.WriteLine($"library: city state {story.Var("VAR_CANALAVE_CITY_STATE")}, library state {story.Var("VAR_CANALAVE_LIBRARY_STATE")}, "
                + Flags("FLAG_LAKE_VALOR_EXPLODED", "FLAG_HIDE_VALOR_LAKEFRONT_CAMERAMEN"));
            Where("library");

            // ---- a book of myths
            At("CanalaveLibrary3F", 9, 4, Direction.Up);
            game.Interact();
            ReadTo("Read it?"); Frames(4); Shot("k35_library_3f_a_book");
            ReadTo("tall grass"); Frames(4); Shot("k36_library_3f_the_myth");
            ReadTo(null);

            // ---- outside: the sailor's news, the rival off to Lake Acuity, the professor's errand
            ComeThrough("Sinnoh", 37, 719, Direction.Down);
            ReadTo("no ordinary tremor"); Frames(4); Shot("k37_canalave_outside_the_library");
            ReadTo("huge explosion"); Frames(4); Shot("k38_canalave_the_sailors_news");
            ReadTo("bad feeling"); Frames(4); Shot("k39_canalave_the_rival");
            ReadTo("I need you at Lake Valor"); Frames(4); Shot("k40_canalave_the_professors_errand");
            ReadTo(null); Frames(10);
            Shot("k41_canalave_the_professor_and_the_assistant_wait");
            Console.WriteLine($"canalave: state {story.Var("VAR_CANALAVE_CITY_STATE")}, "
                + Flags("FLAG_HIDE_LAKE_VERITY_LOW_WATER_PROF_ROWAN", "FLAG_HIDE_LAKE_VERITY_LOW_WATER_COUNTERPART", "FLAG_HIDE_CANALAVE_CITY_SAILOR"));
        }
        else
        {
            // As Canalave City's scenes would have left it
            story.GiveBadge("Sinnoh", "Mine");
            foreach (string flag in new[] { "FLAG_LAKE_VALOR_EXPLODED", "FLAG_HIDE_VALOR_LAKEFRONT_CAMERAMEN", "FLAG_HIDE_CANALAVE_CITY_RIVAL_LIBRARY", "FLAG_HIDE_CANALAVE_CITY_PROF_ROWAN",
                "FLAG_HIDE_CANALAVE_CITY_COUNTERPART" })
                story.Set(flag);
            story.Unset("FLAG_HIDE_LAKE_VERITY_LOW_WATER_PROF_ROWAN");
            story.Unset("FLAG_HIDE_LAKE_VERITY_LOW_WATER_COUNTERPART");
            story.SetVar("VAR_CANALAVE_CITY_STATE", 6);
            story.SetVar("VAR_CANALAVE_LIBRARY_STATE", 2);
        }

        if (stage <= 2)
        {
            // ---- Lake Verity, before Team Galactic comes: the professor and the assistant find it quiet
            ComeThrough("LakeVerity", 46, 53, Direction.Up);
            At("LakeVerity", 48, 45, Direction.Up); Frames(30); Shot("k45_lake_verity_quiet");

            // ---- Valor Lakefront: the cameramen gone
            At("Sinnoh", 721, 760, Direction.Left); Frames(30); Shot("k50_valor_lakefront_the_way_open");

            // ---- Lake Valor, drained: the lake bed, its Magikarp, Team Galactic's grunts
            ComeThrough("LakeValorDrained", 51, 10, Direction.Left);
            Frames(30); Shot("k51_lake_valor_drained");
            At("LakeValorDrained", 36, 21, Direction.Up); Frames(30); Shot("k52_lake_valor_the_lake_bed");
            TalkTo("LakeValorDrained", "magikarp_1", null);
            ReadTo("Magikarp"); Frames(4); Shot("k53_lake_valor_a_magikarp");
            ReadTo(null);
            At("LakeValorDrained", 33, 35, Direction.Up); Frames(30); Shot("k54_lake_valor_the_cavern_mouth");
            TalkTo("LakeValorDrained", "grunt_m_1", null);
            ReadTo("Commander"); Frames(4); Shot("k55_lake_valor_a_grunt");
            ReadTo(null);

            // ---- Valor Cavern: Commander Saturn
            ComeThrough("ValorCavern", 14, 28, Direction.Up);
            Frames(30); Shot("k56_valor_cavern");
            At("ValorCavern", 14, 21, Direction.Up); Frames(30); Shot("k57_valor_cavern_saturn");
            TalkTo("ValorCavern", "saturn", null);
            ReadTo("without a hitch"); Frames(4); Shot("k58_valor_cavern_the_mission");
            ReadTo("I am Saturn"); Frames(4); Shot("k59_valor_cavern_saturn_speaks");
            IntoBattle("Saturn");
            ToMainMenu(game.Battle); Shot("k60_valor_cavern_saturns_battle");
            Win();
            ReadTo("Lake Verity"); Frames(4); Shot("k61_valor_cavern_saturn_beaten");
            ReadTo(null); Frames(20);
            Shot("k62_valor_cavern_empty");
            Console.WriteLine($"valor cavern: {Flags("FLAG_DEFEATED_COMMANDER_SATURN_VALOR_CAVERN", "FLAG_HIDE_LAKE_VALOR_GALACTIC")}");
            ComeThrough("LakeValorDrained", 32, 33, Direction.Down);
            Frames(30); Shot("k63_lake_valor_team_galactic_gone");
        }
        else
        {
            foreach (string flag in new[] { "FLAG_DEFEATED_COMMANDER_SATURN_VALOR_CAVERN", "FLAG_HIDE_LAKE_VALOR_GALACTIC", "FLAG_ALT_MUSIC_LAKE_VALOR" }) story.Set(flag);
        }

        if (stage <= 3)
        {
            // ---- Verity Lakefront now leads to the lake Team Galactic holds
            At("Sinnoh", 80, 845, Direction.Up); Frames(4);
            Console.WriteLine($"verity lakefront: leads to {game.Map.GetWarpAt(80, 843)?.TargetMap}");
            ComeThrough("LakeVerityGalactic", 46, 53, Direction.Up);
            ReadTo("Team Galactic was here"); Frames(4); Shot("k70_lake_verity_the_professor");
            ReadTo(null); Frames(10);
            Shot("k71_lake_verity_held_back");
            At("LakeVerityGalactic", 47, 47, Direction.Up); Frames(30); Shot("k72_lake_verity_the_grunts_on_the_shore");
            At("LakeVerityGalactic", 55, 39, Direction.Up); Frames(30); Shot("k73_lake_verity_mars_and_the_assistant");
            game.Interact();
            ReadTo("Valley Windworks"); Frames(4); Shot("k74_lake_verity_mars");
            IntoBattle("Mars");
            ToMainMenu(game.Battle); Shot("k75_lake_verity_mars_battle");
            Win();
            ReadTo("all three"); Frames(4); Shot("k76_lake_verity_mars_beaten");
            ReadTo("Lake Acuity"); Frames(4); Shot("k77_lake_verity_the_professor_after");
            ReadTo(null); Frames(10);
            Console.WriteLine($"lake verity: acuity state {story.Var("VAR_LAKE_ACUITY_STATE")}, "
                + Flags("FLAG_TEAM_GALACTIC_LEFT_LAKE_VERITY", "FLAG_HIDE_LAKE_VERITY_TEAM_GALACTIC", "FLAG_HIDE_MT_CORONET_1F_NORTH_ROOM_1_GRUNTS_M", "FLAG_HIDE_LAKE_ACUITY_JUPITER"));
            ComeThrough("LakeVerityGalactic", 46, 53, Direction.Up);
            At("LakeVerityGalactic", 52, 40, Direction.Up); Frames(30); Shot("k78_lake_verity_afterwards");
        }
        else
        {
            foreach (string flag in new[] { "FLAG_TEAM_GALACTIC_LEFT_LAKE_VERITY", "FLAG_HIDE_LAKE_VERITY_TEAM_GALACTIC", "FLAG_HIDE_MT_CORONET_1F_NORTH_ROOM_1_GRUNTS_M" }) story.Set(flag);
            story.SetVar("VAR_LAKE_ACUITY_STATE", 1);
        }

        // ---- the way north: Mt. Coronet's hall clear, Route 211's TM, Maylene on Route 217, HM08
        ComeThrough("MtCoronet1FNorthRoom1", 17, 10, Direction.Left);
        Frames(30); Shot("k80_coronet_the_way_clear");
        TalkTo("Sinnoh", "ace_trainer_m", "route_211_east", 1, 0);
        ReadTo("Nothing in this world"); Frames(4); Shot("k81_route211_the_tough_trainer");
        ReadTo("creep up"); Frames(4); Shot("k82_route211_startled");
        ReadTo(null);
        Console.WriteLine($"route 211: TM77 {bag.GetQuantity(ItemDatabase.Get("TM77")!)}");
        StepOnto("Sinnoh", 302, 376, Direction.Up);
        ReadTo("It's you"); Frames(4); Shot("k83_route217_maylene");
        ReadTo("freezing"); Frames(4); Shot("k84_route217_maylene_barefoot");
        ReadTo(null); Frames(10);
        Console.WriteLine($"route 217: state {story.Var("VAR_ROUTE_217_STATE")}, {Flags("FLAG_HIDE_ROUTE_217_MAYLENE")}");
        At("Sinnoh", 297, 305, Direction.Left); Frames(30); Shot("k85_route217_hm08_in_its_ball");
        game.Interact();
        ReadTo("HM08"); Frames(4); Shot("k86_route217_hm08");
        ReadTo(null);
        Console.WriteLine($"route 217: HM08 {bag.GetQuantity(ItemDatabase.Get("HM08")!)}");
    }
}
