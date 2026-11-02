using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;



partial class Harness
{
    /// <summary>
    /// Plan 20 · J15, part o: the last of HeartGold's Kanto (<c>jko01</c> to <c>jko34</c>), after part n's, so nothing
    /// before them moves: Rock Tunnel and Cerulean Cave dark and lit by Flash, Mewtwo's beat and battle, the Trainer
    /// House's hall and Cal's battle, Pal Park's gate, the S.S. Aqua's cabins, hold and bridge, the Celadon Game Corner's
    /// tables and coins, Rotom's room asking which move to forget, the Clefairy's dance on a Monday night, and Suicune's
    /// Kanto stops to its battle on the cape. It prints what each step left.
    /// </summary>
    void JohtoKantoPartOShots()
    {
        var box = game.Dialogue;
        void Spot(string map, int x, int y, Direction way, string shot, int frames = 10)
        {
            At(map, x, y, way);
            if (!game.Map.IsWalkable(x, y))
                (x, y) = Enumerable.Range(1, 4).SelectMany(r => Enumerable.Range(-r, 2 * r + 1).SelectMany(dy => Enumerable.Range(-r, 2 * r + 1).Select(dx => (X: x + dx, Y: y + dy))))
                    .First(t => game.Map.IsWalkable(t.X, t.Y));
            At(map, x, y, way); game.ArriveOnMap(); Frames(frames); Shot(shot);
            Console.WriteLine($"  {shot}: {map} {x},{y} open {game.Map.IsWalkable(x, y)}, {game.Map.ShownNameAt(x, y)}, dark {game.Map.IsDark}, lit {game.Map.Lit}");
        }
        void ReadTo(string? text, int most = 2400)
        {
            for (int i = 0; i < most; i++)
            {
                if (text != null && box.IsActive && box.CurrentLine.Contains(text, StringComparison.Ordinal) && box.IsCurrentLineComplete) return;
                if (text == null && !engine.ScriptRunning && !box.IsActive && game.State == GameState.Overworld) return;
                if (game.State == GameState.Battle) { Frames(1); continue; }
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (box.IsActive && box.IsCurrentLineComplete && !box.IsQuestion) { box.Advance(); Frames(2); }
                else Frames(1);
            }
            Console.WriteLine($"  !! never said: {text ?? "the script's end"}");
        }
        void TalkTo(string map, string key, int dx = 0, int dy = 1)
        {
            var npc = MapDatabase.Get(map).NPCs.First(n => n.Key == key);
            At(map, npc.GridX + dx, npc.GridY + dy, dy > 0 ? Direction.Up : dy < 0 ? Direction.Down : dx > 0 ? Direction.Left : Direction.Right);
            game.ArriveOnMap(); Frames(10);
            game.Interact();
        }
        void Battle(string shot)
        {
            for (int i = 0; i < 1500 && game.State != GameState.Battle; i++)
            {
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (box.IsActive && box.IsCurrentLineComplete && !box.IsQuestion) box.Advance();
                Frames(1);
            }
            if (game.State != GameState.Battle) { Console.WriteLine($"  !! never happened: {shot}"); return; }
            Skip(1.0); ToMainMenu(game.Battle); Shot(shot);
            game.State = GameState.Overworld; ReadTo(null); box.Close();
        }
        void StepOnto(string map, int x, int y, Direction way)
        {
            var (dx, dy) = FieldMovement.Delta(way);
            At(map, x - dx, y - dy, way);
            game.ArriveOnMap(); Frames(10);
            ReadTo(null);
            engine.Steering = (way, false);
            for (int i = 0; i < 120 && !engine.ScriptRunning; i++) Frames(1);
            engine.Steering = null;
            if (!engine.ScriptRunning) Console.WriteLine($"  !! never happened: the scene at {map} {x},{y}");
        }
        game.State = GameState.Overworld;
        game.Player.SetMode(TravelMode.OnFoot);
        game.Story.Set("JohtoHallOfFame");

        // ---- The dark caves, and Flash
        game.Story.Unset(FieldMoveRules.FlashFlag);
        Spot("Rock_Tunnel_1F", 30, 8, Direction.Down, "jko01_rock_tunnel_dark");
        game.Story.Set(FieldMoveRules.FlashFlag);
        Frames(30); Shot("jko02_rock_tunnel_lit_by_flash");
        Console.WriteLine($"  Rock Tunnel lit {game.Map.Lit}");

        // ---- Cerulean Cave: the man at its mouth, the floors, Mewtwo
        game.Story.Unset("FLAG_JOHTO_BEAT_RED");
        Spot("Johto", 1290, 110, Direction.Up, "jko03_the_man_before_cerulean_cave", 30);
        game.Story.Set("FLAG_JOHTO_BEAT_RED");
        Spot("Johto", 1290, 110, Direction.Up, "jko04_cerulean_cave_open", 30);
        game.Story.Unset(FieldMoveRules.FlashFlag);
        Spot("Cerulean_Cave_1F", 49, 40, Direction.Up, "jko05_cerulean_cave_1f_dark");
        game.Story.Set(FieldMoveRules.FlashFlag);
        Frames(30); Shot("jko06_cerulean_cave_1f_lit");
        Spot("Cerulean_Cave_2F", 46, 29, Direction.Up, "jko07_cerulean_cave_2f");
        Spot("Cerulean_Cave_B1F", 56, 28, Direction.Up, "jko08_cerulean_cave_b1f_mewtwo");
        game.Story.Unset("FLAG_HIDE_CERULEAN_CAVE_MEWTWO");
        game.Story.Unset("FLAG_JOHTO_FOUGHT_MEWTWO");
        TalkTo("Cerulean_Cave_B1F", "d03r0103_tsure_poke_static_mewtwo");
        Frames(150); Shot("jko09_mewtwos_beat");
        ReadTo("cold, sharp will"); Frames(10); Shot("jko10_mewtwos_will");
        box.Advance();
        Battle("jko11_mewtwos_battle");
        Console.WriteLine($"  Mewtwo fought {game.Story.Has("FLAG_JOHTO_FOUGHT_MEWTWO")}");
        game.Story.Unset(FieldMoveRules.FlashFlag);

        // ---- The Trainer House's hall, and Cal
        Spot("Viridian_TrainerHouse_1F", 12, 5, Direction.Up, "jko12_trainer_house_ground_floor");
        Spot("Viridian_TrainerHouse_B1F", 8, 6, Direction.Down, "jko13_trainer_house_hall");
        game.Story.Unset("FLAG_DAILY_JOHTO_TRAINER_HOUSE");
        TalkTo("Viridian_TrainerHouse_B1F", "t02r0302_gsman3");
        ReadTo("Want to battle me"); Frames(10); Shot("jko14_cal_asks");
        Battle("jko15_cals_battle");
        Console.WriteLine($"  Trainer House fought today {game.Story.Has("FLAG_DAILY_JOHTO_TRAINER_HOUSE")}");

        // ---- The S.S. Aqua's rooms
        Spot("SSAqua_1F", 24, 15, Direction.Up, "jko20_ss_aqua_corridor");
        Spot("SSAqua_1F_NorthwestRooms", 16, 9, Direction.Up, "jko21_ss_aqua_northwest_cabins");
        Spot("SSAqua_1F_SoutheastRooms", 16, 5, Direction.Down, "jko22_ss_aqua_southeast_cabins");
        Spot("SSAqua_B1F", 30, 15, Direction.Up, "jko23_ss_aqua_hold");
        Spot("SSAqua_CaptainRoom", 9, 8, Direction.Up, "jko24_ss_aqua_captains_room");

        // ---- The Celadon Game Corner's tables and coins
        Spot("Celadon_GameCorner", 7, 9, Direction.Up, "jko25_celadon_game_corner");
        TalkTo("Celadon_GameCorner", "t07sp0101_suit");
        ReadTo("50 for $1,000"); Frames(10); Shot("jko26_the_clerk_and_his_coins");
        ReadTo(null);
        Console.WriteLine($"  Coin Case {game.Bag.GetQuantity(ItemDatabase.Get("Coin Case")!)}");

        // ---- Rotom's room: a Rotom with four moves asked which to forget
        var rotom = new Pokemon(PokemonDatabase.Get("Rotom")!, 50);
        while (rotom.Moves.Count < 4) rotom.TryLearn(new[] { "Thunder Shock", "Astonish", "Double Team", "Thunder Wave", "Ominous Wind" }.First(m => !rotom.Knows(m)));
        game.Party.Add(rotom);
        TalkTo("Saffron_SilphCo_RotomRoom", "t11r0702_rotomf");
        ReadTo("knows four moves already"); Frames(10); Shot("jko27_rotom_wants_overheat");
        box.Advance();
        for (int i = 0; i < 300 && !engine.Choice.IsOpen; i++) { if (box.IsActive && box.IsCurrentLineComplete && !box.IsQuestion) box.Advance(); Frames(1); }
        Frames(20); Shot("jko28_which_move_to_forget");
        ReadTo(null);
        Console.WriteLine($"  Rotom: {rotom.Form}, {string.Join(", ", rotom.Moves.Select(m => m.Name))}");
        game.Party.Remove(rotom);

        // ---- The Clefairy's dance (the harness's day is a Monday)
        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        game.Story.Unset("FLAG_DAILY_JOHTO_CLEFAIRY_DANCE");
        Spot("MountMoon_Square", 9, 18, Direction.Up, "jko29_clefairy_in_a_ring", 30);
        StepOnto("MountMoon_Square", 9, 16, Direction.Up);
        ReadTo("dancing in a ring"); Frames(10); Shot("jko30_the_clefairy_dance");
        ReadTo("noticed"); Frames(10); Shot("jko31_the_clefairy_notice");
        ReadTo(null);
        Console.WriteLine($"  Moon Stone {game.Bag.GetQuantity(ItemDatabase.Get("Moon Stone")!)}, danced today {game.Story.Has("FLAG_DAILY_JOHTO_CLEFAIRY_DANCE")}");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // ---- Suicune's Kanto stops and its battle on the cape
        game.Story.SetVar("VAR_UNK_4076", 2);
        foreach (string v in new[] { "VAR_UNK_4070", "VAR_UNK_4086", "VAR_UNK_4088" }) game.Story.SetVar(v, 0);
        game.Story.Unset("FLAG_JOHTO_FOUGHT_SUICUNE");
        StepOnto("Johto", 1323, 332, Direction.Right);
        ReadTo("looking this way"); Frames(10); Shot("jko32_suicune_in_vermilion");
        ReadTo(null);
        StepOnto("Johto", 1360, 412, Direction.Right);
        ReadTo("straight at"); Frames(10); Shot("jko33_suicune_on_route_14");
        ReadTo(null);
        game.Story.Set("FLAG_JOHTO_MISTY_BACK_IN_GYM");
        game.Story.SetVar("VAR_JOHTO_MISTY_DATE", 1);
        StepOnto("Johto", 1418, 41, Direction.Right);
        ReadTo("waiting for someone"); Frames(10); Shot("jko34_eusine_on_the_cape");
        ReadTo(null);
        TalkTo("Johto", "r25_tsure_poke_static_suicune");
        Frames(150); Shot("jko35_suicunes_beat");
        ReadTo("fell still"); Frames(10); Shot("jko36_suicunes_cry");
        box.Advance();
        Battle("jko37_suicunes_battle");
        Console.WriteLine($"  Suicune: 4070 {game.Story.Var("VAR_UNK_4070")}, 4086 {game.Story.Var("VAR_UNK_4086")}, 4088 {game.Story.Var("VAR_UNK_4088")}, fought {game.Story.Has("FLAG_JOHTO_FOUGHT_SUICUNE")}");

        // (last: the crossing leaves the game in Sinnoh)
        // ---- Pal Park's gate in Fuchsia City
        Spot("Johto", 1214, 405, Direction.Up, "jko16_pal_parks_gate_from_the_street", 30);
        Spot("Fuchsia_PalParkGate", 7, 12, Direction.Up, "jko17_pal_parks_gate_inside");
        game.Pokedex.UnlockNational();
        TalkTo("Fuchsia_PalParkGate", "t08r0201_workman");
        ReadTo("go to Pal Park"); Frames(10); Shot("jko18_the_attendants_question");
        ReadTo(null);
        Console.WriteLine($"  after the gate: {game.Map.Name} {game.Player.GridX},{game.Player.GridY}");
        Frames(30); Shot("jko19_pal_park_lobby_from_kanto");
    }
}
