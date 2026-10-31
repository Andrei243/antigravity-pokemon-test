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
        UntilALeadersBattle();
        Frames(30); Shot("jk18_lt_surges_battle");
        Tell();
        // leave the battle and read the scene to its end, or Surge's box stays over every later shot
        game.State = GameState.Overworld; ReadTo(null, 1200); box.Close();
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
        UntilALeadersBattle();
        Frames(30); Shot("jk28_sabrinas_battle");
        Console.WriteLine($"  Marsh Badge {game.Story.HasBadge(RegionDatabase.Kanto, "Marsh")}");
        game.State = GameState.Overworld; ReadTo(null); game.Dialogue.Close();

        // ---- The Magnet Train, from Goldenrod to Saffron
        game.State = GameState.Overworld;
        At("Goldenrod_MagnetTrainStation", 6, 12, Direction.Up); game.ArriveOnMap(); Frames(10); Shot("jk29_goldenrods_station");
        talkTo("Goldenrod_MagnetTrainStation", "t25r0501_policeman", 0, 1);
        ReadTo("glided out of Goldenrod"); Frames(10); Shot("jk30_the_magnet_train");
        ReadTo(null); Frames(20); Shot("jk31_saffrons_station");
        Console.WriteLine($"  the ride ended on {game.Map.Name} at {game.Player.GridX},{game.Player.GridY}");

        JohtoKantoCeladonShots(readTo, talkTo, until);
    }

    /// <summary>
    /// Plan 20 · J15, part d: Route 7 west to Celadon City, its rooms and Erika's garden Gym, and Route 8 east to
    /// Lavender Town and its rooms (<c>jk32</c> to <c>jk47</c>), after part c's, so nothing before them moves.
    /// </summary>
    void JohtoKantoCeladonShots(Action<string?, int> readTo, Action<string, string, int, int> talkTo, Action<Func<bool>, string, int> until)
    {
        void ReadTo(string? text) => readTo(text, 2400);
        game.State = GameState.Overworld;
        At("Johto", 1271, 240, Direction.Left); Frames(30); Shot("jk32_route_7");
        At("Route7_SaffronGate", 5, 7, Direction.Left); Frames(10); Shot("jk33_route_7s_gate");
        At("Johto", 1231, 240, Direction.Down); Frames(30); Shot("jk34_celadon_city");
        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        Frames(30); Shot("jk35_celadon_city_after_dark");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
        At("Celadon_PokemonCenter_1F", 8, 15, Direction.Up); Frames(10); Shot("jk36_celadons_pokemon_center");
        At("Celadon_DeptStore_1F", 12, 10, Direction.Up); Frames(10); Shot("jk37_the_department_store");
        At("Celadon_Condominiums_1F", 7, 11, Direction.Up); Frames(10); Shot("jk38_the_condominiums");
        At("Celadon_Restaurant", 5, 9, Direction.Up); Frames(10); Shot("jk39_the_restaurant");

        // ---- The Gym: the garden, then Erika
        At("Celadon_Gym", 7, 45, Direction.Up); game.ArriveOnMap(); Frames(10); Shot("jk40_the_celadon_gym");
        talkTo("Celadon_Gym", "t07gym0101_gsleader12", 0, 1);
        ReadTo("I must have dozed off"); Frames(10); Shot("jk41_erika");
        ReadTo("I shan't hold back"); game.Dialogue.Advance();
        UntilALeadersBattle();
        Frames(30); Shot("jk42_erikas_battle");
        Console.WriteLine($"  Rainbow Badge {game.Story.HasBadge(RegionDatabase.Kanto, "Rainbow")}");
        game.State = GameState.Overworld; ReadTo(null); game.Dialogue.Close();

        // ---- Route 8 east to Lavender Town
        game.State = GameState.Overworld;
        At("Johto", 1366, 241, Direction.Right); game.ArriveOnMap(); Frames(30); Shot("jk43_route_8");
        At("Johto", 1418, 238, Direction.Down); Frames(30); Shot("jk44_lavender_town");
        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        Frames(30); Shot("jk45_lavender_town_after_dark");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
        At("Lavender_HouseOfMemories", 4, 15, Direction.Up); Frames(10); Shot("jk46_the_house_of_memories");
        At("Lavender_VolunteerHouse", 5, 10, Direction.Up); Frames(10); Shot("jk47_the_volunteer_house");

        JohtoKantoCeruleanShots(readTo, talkTo);
    }

    /// <summary>
    /// A Leader's battle, waited for while the scene reads on (plan 20 · J15, part e): the wait of parts b to d looked
    /// only for the battle or its band, so a line still on the box after the one the harness read to (or a question) held
    /// the script and the battle never came. This one reads such lines on and answers a question with its first answer.
    /// </summary>
    void UntilALeadersBattle(int most = 900)
    {
        var box = game.Dialogue;
        for (int i = 0; i < most; i++)
        {
            if (game.State == GameState.Battle || BandLit()) return;
            if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
            else if (box.IsActive && box.IsCurrentLineComplete && !box.IsQuestion) { box.Advance(); Frames(2); }
            else Frames(1);
        }
        Console.WriteLine("  !! never happened: the battle");
    }

    /// <summary>
    /// Plan 20 · J15, part e: Route 5 north to Cerulean City, its rooms and Gym standing without Misty, Routes 24 and 25,
    /// Misty's date on the cape and the Sea Cottage, then Misty at home and her battle's band (<c>jk48</c> to <c>jk62</c>),
    /// after part d's, so nothing before them moves. It prints the flag the cape's scene left and Kanto's Cascade Badge.
    /// </summary>
    void JohtoKantoCeruleanShots(Action<string?, int> readTo, Action<string, string, int, int> talkTo)
    {
        void ReadTo(string? text) => readTo(text, 2400);
        game.State = GameState.Overworld;
        At("Route5_SaffronGate", 5, 7, Direction.Up); Frames(10); Shot("jk48_route_5s_gate");
        At("Johto", 1300, 180, Direction.Up); game.ArriveOnMap(); Frames(30); Shot("jk49_route_5");
        At("Johto", 1310, 128, Direction.Down); game.ArriveOnMap(); Frames(30); Shot("jk50_cerulean_city");
        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        Frames(30); Shot("jk51_cerulean_city_after_dark");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
        At("Cerulean_PokemonCenter_1F", 8, 15, Direction.Up); Frames(10); Shot("jk52_ceruleans_pokemon_center");
        At("Cerulean_EastHouse", 5, 8, Direction.Up); Frames(10); Shot("jk53_the_fishermans_house");

        // ---- The Gym without its Leader, and the guide's word
        At("Cerulean_Gym", 10, 24, Direction.Up); game.ArriveOnMap(); Frames(10); Shot("jk54_the_cerulean_gym");
        talkTo("Cerulean_Gym", "t04gym0101_sunglasses", 0, 1);
        ReadTo("Route 25"); Frames(10); Shot("jk55_the_guides_word"); ReadTo(null);

        // ---- Nugget Bridge, Route 25 and the cape
        game.State = GameState.Overworld;
        At("Johto", 1325, 84, Direction.Up); game.ArriveOnMap(); Frames(30); Shot("jk56_nugget_bridge");
        At("Johto", 1380, 44, Direction.Right); game.ArriveOnMap(); Frames(30); Shot("jk57_route_25");
        At("Johto", 1417, 41, Direction.Right); Frames(10); Shot("jk58_misty_on_the_cape");
        talkTo("Johto", "r25_gsleader11", 0, -1);
        ReadTo("one quiet afternoon"); Frames(10); Shot("jk59_the_date_spoiled"); ReadTo(null);
        Console.WriteLine($"  Misty home {game.Story.Has("FLAG_JOHTO_MISTY_BACK_IN_GYM")}");
        At("CeruleanCape_SeaCottage", 7, 9, Direction.Up); Frames(10); Shot("jk60_the_sea_cottage");

        // ---- Misty at home
        At("Cerulean_Gym", 10, 24, Direction.Up); game.ArriveOnMap(); Frames(10);
        talkTo("Cerulean_Gym", "t04gym0101_gsleader11", 0, 1);
        ReadTo("spoil my afternoon"); Frames(10); Shot("jk61_misty");
        ReadTo("Let's make a splash"); game.Dialogue.Advance();
        UntilALeadersBattle();
        Frames(30); Shot("jk62_mistys_battle");
        Console.WriteLine($"  Cascade Badge {game.Story.HasBadge(RegionDatabase.Kanto, "Cascade")}");
        game.State = GameState.Overworld; ReadTo(null); game.Dialogue.Close();

        JohtoKantoPewterShots(readTo, talkTo);
    }

    /// <summary>
    /// Plan 20 · J15, part f: Route 22's east end and Viridian City by day and after dark, its Pokémon Center and Trainer
    /// House, Route 2 and Viridian Forest, Pewter City by day and after dark, its museum and Gym, Brock and his battle's
    /// band, Route 3, Mt. Moon Square, the rival in Mt. Moon and his battle, and Route 4 (<c>jk63</c> to <c>jk81</c>), after
    /// part e's, so nothing before them moves. It prints Kanto's Boulder Badge and the Den's variable the rival left.
    /// </summary>
    void JohtoKantoPewterShots(Action<string?, int> readTo, Action<string, string, int, int> talkTo)
    {
        void ReadTo(string? text) => readTo(text, 2400);
        void Daylight(TimeOfDay time) { engine.Settings.TimeOfDay = time; engine.ApplySettings(window: false); }
        game.State = GameState.Overworld;

        // ---- Viridian City and Route 2
        At("Johto", 1000, 258, Direction.Right); game.ArriveOnMap(); Frames(30); Shot("jk63_route_22_into_viridian");
        At("Johto", 1030, 256, Direction.Up); game.ArriveOnMap(); Frames(30); Shot("jk64_viridian_city");
        Daylight(TimeOfDay.Night); Frames(30); Shot("jk65_viridian_city_after_dark"); Daylight(TimeOfDay.Day);
        At("Viridian_PokemonCenter_1F", 8, 15, Direction.Up); Frames(10); Shot("jk66_viridians_pokemon_center");
        At("Viridian_TrainerHouse_1F", 5, 12, Direction.Up); Frames(10); Shot("jk67_the_trainer_house");
        At("Johto", 1036, 214, Direction.Up); game.ArriveOnMap(); Frames(30); Shot("jk68_route_2");
        At("Viridian_Forest", 32, 75, Direction.Up); game.ArriveOnMap(); Frames(30); Shot("jk69_viridian_forest");

        // ---- Pewter City, its museum and Gym
        At("Johto", 1050, 98, Direction.Up); game.ArriveOnMap(); Frames(30); Shot("jk70_pewter_city");
        Daylight(TimeOfDay.Night); Frames(30); Shot("jk71_pewter_city_after_dark"); Daylight(TimeOfDay.Day);
        At("Pewter_Museum", 10, 16, Direction.Up); Frames(10); Shot("jk72_the_museum_of_science");
        At("Pewter_Gym", 7, 24, Direction.Up); game.ArriveOnMap(); Frames(10); Shot("jk73_the_pewter_gym");
        talkTo("Pewter_Gym", "t03gym0101_gsleader14", 0, 1);
        ReadTo("A Champion from Johto"); Frames(10); Shot("jk74_brock");
        ReadTo("Show me how a Champion"); game.Dialogue.Advance();
        UntilALeadersBattle();
        Frames(30); Shot("jk75_brocks_battle");
        Console.WriteLine($"  Boulder Badge {game.Story.HasBadge(RegionDatabase.Kanto, "Boulder")}");
        game.State = GameState.Overworld; ReadTo(null); game.Dialogue.Close();

        // ---- Route 3, Mt. Moon and the rival, Route 4
        At("Johto", 1130, 110, Direction.Right); game.ArriveOnMap(); Frames(30); Shot("jk76_route_3");
        At("MountMoon_Square", 28, 10, Direction.Up); game.ArriveOnMap(); Frames(30); Shot("jk77_mt_moon_square");
        game.Story.SetVar("VAR_UNK_40FC", 0);
        At("MountMoon", 12, 4, Direction.Left); game.ArriveOnMap(); Frames(30); Shot("jk78_the_rival_in_mt_moon");
        talkTo("MountMoon", "d02r0101_gsrivel", 1, 0);
        ReadTo("Of all the caves"); Frames(10); Shot("jk79_the_rivals_word");
        ReadTo("Battle me"); game.Dialogue.Advance();
        UntilALeadersBattle();
        Frames(30); Shot("jk80_the_rivals_battle");
        game.State = GameState.Overworld; ReadTo(null); game.Dialogue.Close();
        Console.WriteLine($"  VAR_UNK_40FC {game.Story.Var("VAR_UNK_40FC")}");
        At("Johto", 1220, 114, Direction.Left); game.ArriveOnMap(); Frames(30); Shot("jk81_route_4");

        JohtoKantoPalletShots(readTo, talkTo);
    }

    /// <summary>
    /// Plan 20 · J15, part g: Viridian's gate to Route 1, Route 1, Pallet Town by day and after dark, Red's house, Blue's
    /// house and Professor Oak's lab, Route 21 surfed, Cinnabar Island and Blue's word, its Pokémon Center, Route 20, the
    /// Seafoam Islands' first floor and Blaine's Gym, Blaine and his battle's band (<c>jk82</c> to <c>jk99</c>), after
    /// part f's, so nothing before them moves. It prints Kanto's Volcano Badge.
    /// </summary>
    void JohtoKantoPalletShots(Action<string?, int> readTo, Action<string, string, int, int> talkTo)
    {
        void ReadTo(string? text) => readTo(text, 2400);
        void Daylight(TimeOfDay time) { engine.Settings.TimeOfDay = time; engine.ApplySettings(window: false); }
        game.State = GameState.Overworld;
        game.Player.SetMode(TravelMode.OnFoot);

        // ---- Route 1 and Pallet Town
        At("Viridian_Route1Gate", 5, 10, Direction.Down); game.ArriveOnMap(); Frames(10); Shot("jk82_viridians_gate_to_route_1");
        At("Johto", 1037, 300, Direction.Down); game.ArriveOnMap(); Frames(30); Shot("jk83_route_1");
        At("Johto", 1039, 368, Direction.Up); game.ArriveOnMap(); Frames(30); Shot("jk84_pallet_town");
        Daylight(TimeOfDay.Night); Frames(30); Shot("jk85_pallet_town_after_dark"); Daylight(TimeOfDay.Day);
        At("Pallet_RedsHouse_1F", 5, 9, Direction.Up); game.ArriveOnMap(); Frames(10); Shot("jk86_reds_house");
        At("Pallet_BluesHouse_1F", 5, 9, Direction.Up); game.ArriveOnMap(); Frames(10); Shot("jk87_blues_house");
        At("Pallet_OaksLab", 8, 12, Direction.Up); game.ArriveOnMap(); Frames(10); Shot("jk88_professor_oaks_lab");

        // ---- Route 21 by sea to Cinnabar Island
        At("Johto", 1040, 440, Direction.Down); game.Player.SetMode(TravelMode.Surfing); game.ArriveOnMap(); Frames(30); Shot("jk89_route_21");
        game.Player.SetMode(TravelMode.OnFoot);
        At("Johto", 1038, 495, Direction.Left); game.ArriveOnMap(); Frames(30); Shot("jk90_cinnabar_island");
        Daylight(TimeOfDay.Night); Frames(30); Shot("jk91_cinnabar_island_after_dark"); Daylight(TimeOfDay.Day);
        talkTo("Johto", "t09_gsleader16", 1, 0);
        ReadTo("Look at this place"); Frames(10); Shot("jk92_blue_on_cinnabar");
        ReadTo(null); game.Dialogue.Close();
        At("Cinnabar_PokemonCenter_1F", 8, 17, Direction.Up); game.ArriveOnMap(); Frames(10); Shot("jk93_cinnabars_pokemon_center");

        // ---- Route 20, the Seafoam Islands and Blaine
        At("Johto", 1100, 500, Direction.Right); game.Player.SetMode(TravelMode.Surfing); game.ArriveOnMap(); Frames(30); Shot("jk94_route_20");
        game.Player.SetMode(TravelMode.OnFoot);
        At("Seafoam_1F", 55, 26, Direction.Up); game.ArriveOnMap(); Frames(30); Shot("jk95_the_seafoam_islands");
        At("Seafoam_Gym", 14, 24, Direction.Up); game.ArriveOnMap(); Frames(10); Shot("jk96_blaines_gym");
        talkTo("Seafoam_Gym", "d11r0106_gsleader15", 0, 1);
        ReadTo("found my hideaway"); Frames(10); Shot("jk97_blaine");
        ReadTo("turn up the heat"); game.Dialogue.Advance();
        UntilALeadersBattle();
        Frames(30); Shot("jk98_blaines_battle");
        game.State = GameState.Overworld; ReadTo(null); game.Dialogue.Close();
        Console.WriteLine($"  Volcano Badge {game.Story.HasBadge(RegionDatabase.Kanto, "Volcano")}");
        At("Seafoam_Gym", 13, 13, Direction.Up); Frames(10); Shot("jk99_blaine_beaten");
    }

}
