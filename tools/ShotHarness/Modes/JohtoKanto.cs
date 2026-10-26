partial class Harness
{
    /// <summary>
    /// Plan 20 · J15, part b, by itself (<c>JOHTO_FROM=kanto</c>, in a new game of Johto of its own, so no other shot of
    /// the <c>johto</c> mode moves): a Champion of Johto at Olivine's port hall and its sailor stepping aside (<c>jk01</c>,
    /// <c>jk02</c>), the quay and the S.S. Aqua at her berth (<c>jk03</c>), her deck and the crewman's crossing (<c>jk04</c>
    /// to <c>jk06</c>), Vermilion's quay and port hall (<c>jk07</c>, <c>jk08</c>), Vermilion City by day and after dark
    /// (<c>jk09</c>, <c>jk10</c>), its Pokémon Center, Mart and Fan Club (<c>jk11</c> to <c>jk13</c>) and its Gym: the
    /// gates, a bin with a switch, the gates gone, Lt. Surge and his battle's band (<c>jk14</c> to <c>jk18</c>). It prints
    /// the flags each step left (the passage, where the ship lies, the gates, Kanto's Thunder Badge). Written, not run.
    /// </summary>
    void JohtoKantoShots()
    {
        game.State = GameState.Overworld;
        game.Player.SetMode(TravelMode.OnFoot);
        var box = game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void ReadTo(string? text, int most = 2400)
        {
            int idle = 0;
            for (int i = 0; i < most; i++)
            {
                if (text != null && box.IsActive && Translated.Saying(box.CurrentLine, text)) { Until(() => box.IsActive, "text"); box.FinishLine(); Frames(2); Console.WriteLine($"  said: {text}"); return; }
                if (text != null && !engine.ScriptRunning && !box.IsActive && game.State == GameState.Overworld) { if (++idle > 60) break; }
                else idle = 0;
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
            game.Interact();
        }
        void Tell() => Console.WriteLine($"  passage {game.Story.Has("FLAG_JOHTO_SS_AQUA_PASSAGE")}, at Vermilion {game.Story.Has("FLAG_JOHTO_SS_AQUA_AT_VERMILION")}, "
            + $"gates down {game.Story.Has("FLAG_HIDE_VERMILION_GYM_STOPS")}, Thunder Badge {game.Story.HasBadge(RegionDatabase.Kanto, "Thunder")}");

        game.Story.Set("JohtoHallOfFame");
        game.Party.Members.Clear();
        foreach (string species in new[] { "Quagsire", "Typhlosion", "Ampharos" }) game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, 60));

        // ---- Olivine's port hall: the sailor asked, Vermilion City chosen, and the way to the quay
        At("Olivine_PortHall", 7, 6, Direction.Down); Frames(10); Shot("jk01_olivine_port_hall");
        TalkTo("Olivine_PortHall", "ss_aqua_olivine_port_interior.Sailor", 0, -1);
        ReadTo("Right this way"); Frames(10); ReadTo(null); Frames(10); Shot("jk02_the_sailor_aside");
        Tell();

        // ---- The quay and the ship
        At("SSAqua_OlivinePort", 27, 21, Direction.Up); Frames(10); Shot("jk03_the_ss_aqua_at_her_berth");
        At("SSAqua_1F", 24, 18, Direction.Up); Frames(10); Shot("jk04_the_deck");
        TalkTo("SSAqua_1F", "p01r0301_seaman_2_2");
        ReadTo("sped east"); Frames(10); Shot("jk05_the_crossing");
        ReadTo("docked at Vermilion"); Frames(10); Shot("jk06_docked_at_vermilion");
        ReadTo(null);
        Tell();

        // ---- Vermilion's quay, its port hall and the city
        At("SSAqua_VermilionPort", 23, 19, Direction.Down); Frames(10); Shot("jk07_vermilions_quay");
        At("Vermilion_PortHall", 7, 6, Direction.Up); game.ArriveOnMap(); Frames(10); Shot("jk08_vermilions_port_hall");
        At("Johto", 1305, 312, Direction.Down); Frames(30); Shot("jk09_vermilion_city");
        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        Frames(30); Shot("jk10_vermilion_city_after_dark");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
        At("Vermilion_PokemonCenter_1F", 8, 15, Direction.Up); Frames(10); Shot("jk11_vermilions_pokemon_center");
        At("Vermilion_Mart", 4, 8, Direction.Up); Frames(10); Shot("jk12_vermilions_mart");
        At("Vermilion_FanClub", 7, 9, Direction.Up); Frames(10); Shot("jk13_the_fan_club");

        // ---- The Gym: the gates, the bins, Lt. Surge
        At("Vermilion_Gym", 6, 20, Direction.Up); game.ArriveOnMap(); Frames(10); Shot("jk14_the_gym_and_its_gates");
        int first = game.Story.Var("VAR_JOHTO_VERMILION_GYM_SWITCH_1"), second = game.Story.Var("VAR_JOHTO_VERMILION_GYM_SWITCH_2");
        (int X, int Y) Bin(int n) => (2 + 2 * ((n - 1) % 5), 13 + 2 * ((n - 1) / 5));
        Console.WriteLine($"  the switches are in bins {first} and {second}");
        At("Vermilion_Gym", Bin(first).X, Bin(first).Y + 1, Direction.Up); game.Interact();
        ReadTo("first electric lock"); Frames(10); Shot("jk15_a_switch_in_a_bin"); ReadTo(null);
        At("Vermilion_Gym", Bin(second).X, Bin(second).Y + 1, Direction.Up); game.Interact(); ReadTo(null);
        At("Vermilion_Gym", 6, 11, Direction.Up); Frames(10); Shot("jk16_the_gates_down");
        TalkTo("Vermilion_Gym", "t06gym0101_gsleader9");
        ReadTo("came all the way from Johto"); Frames(10); Shot("jk17_lt_surge");
        ReadTo("jolt"); box.Advance();
        Until(() => game.State == GameState.Battle || BandLit(), "the battle", 600);
        Frames(30); Shot("jk18_lt_surges_battle");
        Tell();
        JohtoKantoSaffronShots(ReadTo, TalkTo, Until);
    }

    /// <summary>
    /// Plan 20 · J15, part c, after part b's in the same game: Route 6 and its gate (<c>jk19</c>, <c>jk20</c>), Saffron
    /// City by day and after dark (<c>jk21</c>, <c>jk22</c>), its Pokémon Center, the Copycat's room and the Fighting Dojo
    /// (<c>jk23</c> to <c>jk25</c>), the Gym's warp panels and Sabrina with her battle's band (<c>jk26</c> to <c>jk28</c>),
    /// and the Magnet Train: Goldenrod's station, the ride over the dark and Saffron's (<c>jk29</c> to <c>jk31</c>). It
    /// prints Kanto's Marsh Badge and the map the ride ends on. Written, not run.
    /// </summary>
    void JohtoKantoSaffronShots(Action<string?, int> readTo, Action<string, string, int, int> talkTo, Action<Func<bool>, string, int> until)
    {
        void ReadTo(string? text) => readTo(text, 2400);
        game.State = GameState.Overworld;
        At("Johto", 1301, 270, Direction.Up); Frames(30); Shot("jk19_route_6");
        At("Route6_SaffronGate", 5, 9, Direction.Up); Frames(10); Shot("jk20_route_6s_gate");
        At("Johto", 1303, 213, Direction.Down); Frames(30); Shot("jk21_saffron_city");
        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        Frames(30); Shot("jk22_saffron_city_after_dark");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
        At("Saffron_PokemonCenter_1F", 8, 15, Direction.Up); Frames(10); Shot("jk23_saffrons_pokemon_center");
        At("Saffron_CopycatHouse_2F", 5, 9, Direction.Up); Frames(10); Shot("jk24_the_copycats_room");
        At("Saffron_FightingDojo", 7, 12, Direction.Up); Frames(10); Shot("jk25_the_fighting_dojo");

        // ---- The Gym: a warp panel, then Sabrina
        At("Saffron_Gym", 15, 26, Direction.Up); game.ArriveOnMap(); Frames(10); Shot("jk26_the_saffron_gym");
        talkTo("Saffron_Gym", "t11gym0101_gsleader10", 0, 1);
        ReadTo("I knew you would come"); Frames(10); Shot("jk27_sabrina");
        ReadTo("Show me the strength"); game.Dialogue.Advance();
        until(() => game.State == GameState.Battle || BandLit(), "the battle", 600);
        Frames(30); Shot("jk28_sabrinas_battle");
        Console.WriteLine($"  Marsh Badge {game.Story.HasBadge(RegionDatabase.Kanto, "Marsh")}");

        // ---- The Magnet Train, from Goldenrod to Saffron
        game.State = GameState.Overworld;
        At("Goldenrod_MagnetTrainStation", 6, 12, Direction.Up); game.ArriveOnMap(); Frames(10); Shot("jk29_goldenrods_station");
        talkTo("Goldenrod_MagnetTrainStation", "t25r0501_policeman", 0, 1);
        ReadTo("glided out of Goldenrod"); Frames(10); Shot("jk30_the_magnet_train");
        ReadTo(null); Frames(20); Shot("jk31_saffrons_station");
        Console.WriteLine($"  the ride ended on {game.Map.Name} at {game.Player.GridX},{game.Player.GridY}");
    }

}
