partial class Harness
{
    /// <summary>
    /// Unova as Black 2's maps make it (plan 22 · U2; not part of <c>all</c>, since its people would move every battle of
    /// the modes after it): a new game begun there (<c>--region Unova</c>) in the player's room, Aspertia City, Route 19,
    /// Floccesy Town and Floccesy Ranch by day and after dark (u01 to u08), a few of their rooms (u10 to u16), Route 4,
    /// the Desert Resort's edge and the desert in their sandstorm, Nimbasa City and its amusement park by day and after
    /// dark (plan 22 · U6: u21 to u30) and a few of their rooms (u20, u31 to u34), Route 5, the drawbridge's end and its
    /// deck over the sea and Driftveil City by day and after dark (plan 22 · U6, part b: u35 to u42) and Nimbasa's and
    /// Driftveil's rooms with Clay's mine (u43 to u53), Route 16, the Lostlorn Forest, Anville Town, the Relic Castle's hall
    /// and the World Tournament's grounds by day and after dark (plan 22 · U6, part c: u54 to u63) with the gate to Route 16,
    /// the Gear Station, a house of Anville, the Relic Passage's halls, the Relic Castle's rooms and the frigate's deck
    /// (u64 to u73), and the
    /// walk from the player's door in Aspertia City down Route 19 to Floccesy Town's Pokémon Center at running pace, every
    /// frame timed, with what streaming cost.
    /// </summary>
    public void UnovaMode()
    {
        engine.NewGameRegion = "Unova";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        Frames(30);
        Console.WriteLine($"a new game in Unova begins on {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY}), "
            + $"with {(game.Party.Members.Count == 0 ? "nobody" : string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} {p.Level}")))}");
        Shot("u00_new_game");

        // Each place by day, then the same tiles after dark
        (string Day, string Night, string Map, int X, int Y, Direction Facing)[] places =
        {
            ("u01_aspertia_day", "u02_aspertia_night", "Unova", 48, 739, Direction.Up),
            ("u03_route19_day", "u04_route19_night", "Unova", 73, 693, Direction.Right),
            ("u05_floccesy_day", "u06_floccesy_night", "Unova", 113, 678, Direction.Up),
            ("u07_ranch_day", "u08_ranch_night", "FloccesyRanch", 30, 38, Direction.Up),
            // Plan 22 · U6: Route 4, the Desert Resort's edge and the desert in their sandstorm, Nimbasa City and its park
            ("u21_route4_day", "u22_route4_night", "Unova", 405, 560, Direction.Up),
            ("u23_desert_edge_day", "u24_desert_edge_night", "Unova", 372, 492, Direction.Up),
            ("u25_desert_day", "u26_desert_night", "DesertResort", 50, 50, Direction.Up),
            ("u27_nimbasa_day", "u28_nimbasa_night", "Unova", 430, 470, Direction.Up),
            ("u29_amusement_park_day", "u30_amusement_park_night", "NimbasaCity_AmusementPark", 60, 14, Direction.Right),
            // Plan 22 · U6, part b: Route 5, the drawbridge's east end over the sea, and Driftveil City
            ("u35_route5_day", "u36_route5_night", "Unova", 350, 434, Direction.Up),
            ("u37_drawbridge_end_day", "u38_drawbridge_end_night", "DriftveilDrawbridge", 209, 47, Direction.Left),
            ("u39_driftveil_day", "u40_driftveil_night", "Unova", 190, 415, Direction.Up),
            // Plan 22 · U6, part c: Route 16, the Lostlorn Forest, Anville Town, the Relic Castle's hall and the World
            // Tournament's grounds with the frigate at its pier
            ("u54_route16_day", "u55_route16_night", "Unova", 480, 440, Direction.Right),
            ("u56_lostlorn_day", "u57_lostlorn_night", "LostlornForest", 12, 50, Direction.Up),
            ("u58_anville_day", "u59_anville_night", "AnvilleTown", 30, 40, Direction.Up),
            ("u60_relic_castle_day", "u61_relic_castle_night", "RelicCastle", 24, 18, Direction.Up),
            ("u62_pwt_day", "u63_pwt_night", "Unova", 197, 474, Direction.Down),
            // Plan 22 · U7, part a: Route 6 before the Season Research Lab, Chargestone Cave's first floor, Mistralton City's runway,
            // Route 7 before the Celestial Tower and Lentimas Town's strip
            ("u79_route6_day", "u80_route6_night", "Unova", 135, 370, Direction.Up),
            ("u81_chargestone_day", "u82_chargestone_night", "ChargestoneCave", 11, 47, Direction.Up),
            ("u83_mistralton_day", "u84_mistralton_night", "Unova", 102, 296, Direction.Right),
            ("u85_route7_day", "u86_route7_night", "Unova", 107, 235, Direction.Up),
            ("u87_lentimas_day", "u88_lentimas_night", "Unova", 616, 308, Direction.Up),
            // Plan 22 · U7, part b: Reversal Mountain's slopes before the cave's mouth, the Strange House's door, Undella Town's street
            // and bay, Route 13 at its south and its north ends, Lacunosa Town's square, and the great cave
            ("u115_reversal_slopes_day", "u116_reversal_slopes_night", "Unova", 656, 297, Direction.Up),
            ("u117_strange_house_door_day", "u118_strange_house_door_night", "Unova", 683, 306, Direction.Up),
            ("u119_undella_street_day", "u120_undella_street_night", "Unova", 745, 300, Direction.Right),
            ("u121_undella_beach_day", "u122_undella_beach_night", "Unova", 740, 310, Direction.Down),
            ("u123_undella_bay_day", "u124_undella_bay_night", "Unova", 788, 281, Direction.Right),
            ("u125_route13_south_day", "u126_route13_south_night", "Unova", 750, 280, Direction.Up),
            ("u127_route13_north_day", "u128_route13_north_night", "Unova", 690, 190, Direction.Up),
            ("u129_lacunosa_square_day", "u130_lacunosa_square_night", "Unova", 654, 174, Direction.Up),
            ("u131_reversal_great_cave_day", "u132_reversal_great_cave_night", "ReversalMountain_3", 20, 8, Direction.Right),
            // Plan 22 · U8, part a: Route 12 at both ends, the Village Bridge at its west gate, its middle and its east gate, Route 11 at both
            // ends, Opelucid City's east street, square and gym lane, Route 9 and the mall's front
            ("u133_route12_west_day", "u134_route12_west_night", "Unova", 592, 175, Direction.Right),
            ("u135_route12_east_day", "u136_route12_east_night", "Unova", 620, 172, Direction.Left),
            ("u137_village_bridge_west_day", "u138_village_bridge_west_night", "VillageBridge", 8, 33, Direction.Right),
            ("u139_village_bridge_middle_day", "u140_village_bridge_middle_night", "VillageBridge", 47, 38, Direction.Up),
            ("u141_village_bridge_east_day", "u142_village_bridge_east_night", "VillageBridge", 88, 33, Direction.Left),
            ("u143_route11_west_day", "u144_route11_west_night", "Unova", 460, 175, Direction.Right),
            ("u145_route11_east_day", "u146_route11_east_night", "Unova", 490, 175, Direction.Left),
            ("u147_opelucid_east_street_day", "u148_opelucid_east_street_night", "Unova", 440, 175, Direction.Left),
            ("u149_opelucid_square_day", "u150_opelucid_square_night", "Unova", 418, 168, Direction.Up),
            ("u151_opelucid_gym_lane_day", "u152_opelucid_gym_lane_night", "Unova", 405, 150, Direction.Up),
            ("u153_route9_day", "u154_route9_night", "Unova", 345, 175, Direction.Right),
            ("u155_mall_front_day", "u156_mall_front_night", "Unova", 357, 168, Direction.Up),
        };
        foreach (var (day, _, map, x, y, facing) in places)
        {
            At(map, x, y, facing); Frames(2); Shot(day);
            ShotCrop(day + "_native", 640, 300, 640, 360, 2);
        }
        // Beside them: the street before the player's door, Route 20's grass and the ranch's gate on Route 20
        (string Name, string Map, int X, int Y, Direction Facing)[] more =
        {
            ("u01b_aspertia_home", "Unova", 47, 762, Direction.Down),
            ("u03b_route19_grass", "Unova", 87, 690, Direction.Left),
            ("u05b_route20", "Unova", 157, 656, Direction.Up),
            ("u07b_ranch_gate", "Unova", 142, 634, Direction.Up),
            ("u10_players_house", "AspertiaCity_PlayersHouse", 14, 5, Direction.Left),
            ("u11_aspertia_center", "AspertiaCity_PokemonCenter", 7, 13, Direction.Up),
            ("u12_trainers_school", "AspertiaCity_TrainerSchool", 10, 17, Direction.Up),
            ("u13_aspertia_gym", "AspertiaCity_Gym", 13, 16, Direction.Up),
            ("u14_alders_house", "FloccesyTown_AldersHouse", 6, 8, Direction.Up),
            ("u15_floccesy_center", "FloccesyTown_PokemonCenter", 7, 13, Direction.Up),
            ("u16_aspertia_gate", "AspertiaGate", 5, 5, Direction.Up),
            // Plan 22 · U6: a house of flats on Route 4, Join Avenue, the desert's gate house, Elesa's runway and Nimbasa's Center
            ("u20_route4_house", "Route4_House1_1F", 7, 8, Direction.Up),
            ("u31_join_avenue", "JoinAvenue", 15, 74, Direction.Up),
            ("u32_desert_gate", "DesertResortGate", 5, 8, Direction.Up),
            ("u33_nimbasa_gym", "NimbasaCity_Gym", 15, 57, Direction.Up),
            ("u34_nimbasa_center", "NimbasaCity_PokemonCenter", 7, 13, Direction.Up),
            // Plan 22 · U6, part b: Nimbasa's other rooms, its gate to Route 5, Driftveil's rooms and Clay's mine of ours
            ("u43_big_stadium", "NimbasaCity_BigStadium", 11, 17, Direction.Up),
            ("u44_musical_theater", "MusicalTheater", 14, 18, Direction.Up),
            ("u45_battle_institute", "NimbasaCity_BattleInstitute", 7, 11, Direction.Up),
            ("u46_roller_coaster", "NimbasaCity_RollerCoaster", 14, 43, Direction.Up),
            ("u47_route5_gate", "NimbasaGate_Route5", 8, 5, Direction.Left),
            ("u48_driftveil_center", "DriftveilCity_PokemonCenter", 7, 17, Direction.Up),
            ("u49_plasma_house", "DriftveilCity_PlasmaHouse", 7, 22, Direction.Up),
            ("u50_market", "DriftveilCity_Market", 12, 14, Direction.Up),
            ("u51_gym_lobby", "DriftveilCity_GymLobby", 7, 13, Direction.Up),
            ("u52_clays_mine", "DriftveilCity_Gym", 10, 26, Direction.Up),
            ("u53_ferris_wheel", "NimbasaCity_AmusementPark", 57, 17, Direction.Up),
            // Plan 22 · U6, part c: the gate to Route 16, the Gear Station of ours, a house of Anville, the Relic Passage's
            // halls, the Relic Castle's floor below and its deepest room, and the frigate's deck
            ("u64_route16_gate", "NimbasaGate_Route16", 8, 5, Direction.Right),
            ("u65_gear_station", "GearStation", 11, 10, Direction.Up),
            ("u66_anville_house", "AnvilleTown_House3", 5, 7, Direction.Up),
            ("u67_relic_passage_east", "RelicPassage_East", 55, 50, Direction.Up),
            ("u68_relic_passage", "RelicPassage", 50, 13, Direction.Right),
            ("u69_relic_passage_west", "RelicPassage_West", 30, 23, Direction.Left),
            ("u70_relic_castle_b1f", "RelicCastle_B1F", 15, 12, Direction.Up),
            ("u71_relic_castle_depths", "RelicCastle_Depths", 16, 20, Direction.Up),
            ("u72_plasma_frigate", "PlasmaFrigate", 12, 28, Direction.Up),
            ("u73_relic_castle_ruins", "RelicCastle_Ruins1", 6, 12, Direction.Up),
            ("u74_join_avenue_shops", "JoinAvenue", 9, 28, Direction.Left),
            // plan 22 · U6, part d: a car of the Battle Subway, the World Tournament's hall, Route 5 and the drawbridge's gate
            ("u75_battle_subway_car", "BattleSubwayTrain", 3, 5, Direction.Right),
            ("u76_world_tournament_hall", "PokemonWorldTournament", 11, 12, Direction.Up),
            ("u77_anville_plaza", "AnvilleTown", 20, 56, Direction.Up),
            ("u78_driftveil_market", "DriftveilCity_Market", 9, 14, Direction.Up),
            // plan 22 · U7, part a: the Season Research Lab, Skyla's Gym from its door and its far end, the cargo hall, the Celestial
            // Tower's ground floor and summit, a house of Route 7 and Mistralton's and Lentimas' Pokémon Centers
            ("u89_season_lab", "Route6_SeasonLab", 5, 9, Direction.Up),
            ("u90_skylas_gym_door", "MistraltonCity_Gym", 11, 48, Direction.Up),
            ("u91_skylas_gym_far", "MistraltonCity_Gym", 11, 8, Direction.Up),
            ("u92_cargo_hall", "MistraltonCity_CargoHall", 14, 22, Direction.Up),
            ("u93_celestial_tower_1f", "CelestialTower_1F", 14, 20, Direction.Up),
            ("u94_celestial_tower_summit", "CelestialTower_Summit", 22, 15, Direction.Right),
            ("u95_route7_house", "Route7_House1", 4, 8, Direction.Up),
            ("u96_mistralton_center", "MistraltonCity_PokemonCenter", 7, 18, Direction.Up),
            ("u97_lentimas_center", "LentimasTown_PokemonCenter", 7, 18, Direction.Up),
            // plan 22 · U7, part b: the Strange House's six rooms, Reversal Mountain's lair, passage and east tunnel, and the towns' rooms
            ("u98_strange_hall", "StrangeHouse_Hall", 17, 29, Direction.Up),
            ("u99_strange_library", "StrangeHouse_Library1", 14, 13, Direction.Up),
            ("u100_strange_rooms", "StrangeHouse_Rooms", 24, 18, Direction.Up),
            ("u101_strange_library2", "StrangeHouse_Library2", 14, 13, Direction.Up),
            ("u102_strange_cellar", "StrangeHouse_Cellar", 18, 18, Direction.Up),
            ("u103_strange_attic", "StrangeHouse_Attic", 6, 11, Direction.Up),
            ("u104_reversal_first_floor", "ReversalMountain_1", 26, 10, Direction.Up),
            ("u105_reversal_passage", "ReversalMountain_3", 40, 57, Direction.Up),
            ("u106_reversal_lair", "ReversalMountain_4", 10, 20, Direction.Up),
            ("u107_reversal_east_tunnel", "ReversalMountain_6", 25, 20, Direction.Up),
            ("u108_undella_center", "UndellaTown_PokemonCenter", 7, 18, Direction.Up),
            ("u109_undella_visitors_house", "UndellaTown_House2", 6, 8, Direction.Up),
            ("u110_undella_gate", "UndellaGate", 5, 12, Direction.Up),
            ("u111_route13_house", "Route13_House1", 6, 8, Direction.Up),
            ("u112_lacunosa_center", "LacunosaTown_PokemonCenter", 7, 18, Direction.Up),
            ("u113_storyteller_house", "LacunosaTown_House2", 6, 8, Direction.Up),
            ("u114_lacunosa_shop", "LacunosaTown_House4", 6, 8, Direction.Up),
            // plan 22 · U8, part a: the gate houses of the bridge and Opelucid, the Dragon Gym's door, platforms and strip, Drayden's house, the
            // houses, the Pokémon Center and the mall's two floors
            ("u157_bridge_gate", "VillageBridgeGate_East", 5, 8, Direction.Up),
            ("u158_opelucid_gate", "OpelucidGate_West", 8, 6, Direction.Up),
            ("u159_route11_hut", "Route11_Hut", 5, 6, Direction.Up),
            ("u160_gym_door", "OpelucidCity_Gym", 23, 52, Direction.Up),
            ("u161_gym_first_platform", "OpelucidCity_Gym", 15, 29, Direction.Up),
            ("u162_gym_strip", "OpelucidCity_Gym", 15, 24, Direction.Up),
            ("u163_gym_high_platform", "OpelucidCity_Gym", 4, 17, Direction.Right),
            ("u164_draydens_house", "OpelucidCity_DraydensHouse_1F", 9, 12, Direction.Up),
            ("u165_draydens_house_upstairs", "OpelucidCity_DraydensHouse_2F", 9, 6, Direction.Down),
            ("u166_triple_house", "OpelucidCity_TripleHouse_1F", 9, 12, Direction.Up),
            ("u167_trade_house_upstairs", "OpelucidCity_TradeHouse_2F", 9, 6, Direction.Down),
            ("u168_opelucid_center", "OpelucidCity_PokemonCenter", 7, 18, Direction.Up),
            ("u169_mall_ground", "ShoppingMallNine_1F", 15, 26, Direction.Up),
            ("u170_mall_upper", "ShoppingMallNine_1F", 14, 8, Direction.Up),
            ("u171_mall_back_room", "ShoppingMallNine_2F", 6, 6, Direction.Up),
        };
        foreach (var (name, map, x, y, facing) in more)
        {
            At(map, x, y, facing); Frames(2); Shot(name);
        }

        // The drawbridge's middle, the player stood on its deck five tiles over the sea (plan 22 · U6, part b; a tile is
        // come onto at its ground, so the deck's height is given by hand)
        void OnTheDeck(string name)
        {
            At("DriftveilDrawbridge", 120, 47, Direction.Left);
            game.Player.SetHeight(game.Map.DeckAt(120, 47) ?? game.Map.HeightAt(120, 47));
            Frames(2); Shot(name);
            ShotCrop(name + "_native", 640, 300, 640, 360, 2);
            Console.WriteLine($"{name}: camera {game.Map.Camera}, deck {game.Map.DeckAt(120, 47)}, sea {game.Map.HeightAt(120, 30)}");
        }
        OnTheDeck("u41_drawbridge_deck_day");

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        foreach (var (_, night, map, x, y, facing) in places)
        {
            At(map, x, y, facing); Frames(2); Shot(night);
        }
        OnTheDeck("u42_drawbridge_deck_night");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // The walk the plan asks for, from the north door of Aspertia City's gate (the gate is a room since plan 22 · U5, so a
        // walk on the map of Unova starts past it) down Route 19 to Floccesy Town's Pokémon Center, tile by tile at running
        // pace, every frame timed and held to a sixtieth of a second as the screen holds it
        if (filter.Length == 0 || Environment.GetEnvironmentVariable("SHOTS_TIMING") == "1")
        {
            var unova = MapDatabase.Get("Unova");
            var path = WalkingPath(unova, (52, 702), (105, 694));
            var renderer = game.World;
            At("Unova", 52, 702, Direction.Up); Frames(30);
            renderer.ResetStreamingStats();
            double total = 0, worst = 0;
            int frames = 0, mostChunks = 0;
            (int X, int Y) worstAt = default;
            var watch = new System.Diagnostics.Stopwatch();
            foreach (var (x, y) in path)
            {
                game.Player.SetPosition(x, y, Direction.Up);
                for (int f = 0; f < 8; f++)
                {
                    watch.Restart();
                    FrameClock.Fixed = ++tick / 60.0;
                    engine.Update(1f / 60f);
                    engine.Draw();
                    double ms = watch.Elapsed.TotalMilliseconds;
                    total += ms; frames++;
                    if (ms > worst) { worst = ms; worstAt = (x, y); }
                    while (watch.Elapsed.TotalMilliseconds < 1000.0 / 60.0) Thread.Sleep(1);
                }
                mostChunks = Math.Max(mostChunks, renderer.LoadedChunks);
            }
            Console.WriteLine($"walk from Aspertia City's gate to Floccesy Town's Pokémon Center: {path.Count} tiles, {total / Math.Max(1, frames):F2} ms/frame, "
                + $"worst frame {worst:F1} ms at ({worstAt.X}, {worstAt.Y}), at most {mostChunks} chunks loaded");
            Console.WriteLine($"  streaming: {renderer.Streaming}");
            foreach (var (label, x, y) in new[] { ("aspertia city", 48, 739), ("route 19", 73, 693), ("floccesy town", 113, 678), ("route 20", 157, 656) })
            {
                At("Unova", x, y, Direction.Up);
                Timing("unova " + label);
            }
        }
        // And last the first chapter, in a new game of its own (plan 22 · U5, part a: us01 to us30)
        UnovaScenes();
        engine.NewGameRegion = "Sinnoh";
    }
}
