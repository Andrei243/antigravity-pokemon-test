partial class Harness
{
    // Not part of `all` but the end of `world`: the Battle Zone's towns and routes (plan 01 · M10, part a) and Route 224
    // behind Victory Road's back rooms, by day, the three towns after dark, Route 228 in its sandstorm and Route 227
    // under its ash (`wj00` to `wj55`); then the rest of the post-game (part b, `wj60` on): Stark Mountain and its
    // rooms, Snowpoint Temple's floors, the two islands and their woods, the Seabreak Path, Flower Paradise and Route
    // 224 with the path revealed, the Battle Frontier's grounds, the Ribbon Syndicate's door kept and opened and its
    // hall, and the mountain, the Frontier and the islands after dark. Given places after the mode's name (`battlezone Sinnoh,650,430 Sinnoh,802,474,night`), it
    // shoots those instead, each as `wjx_<map>_<x>_<y>`: how a spot is looked at while its area is being built.
    public void BattleZoneMode()
    {
        // A place with air of its own (Stark Mountain, Snowpoint Temple) eases into its light over a second, as snow
        // country does: a shot of it waits for that, where the player would have walked in
        void Settle() => Frames(game.Map.AreaAt(game.Player.GridX, game.Player.GridY)?.Climate is AreaClimate.Volcanic or AreaClimate.Frozen ? 70 : 2);

        if (mode == "battlezone" && args.Length > 2)
        {
            foreach (string spot in args.Skip(2))
            {
                string[] parts = spot.Split(',');
                bool night = parts.Length > 3 && parts[3] == "night";
                if (night) { engine.Settings.TimeOfDay = TimeOfDay.Night; engine.ApplySettings(window: false); }
                At(parts[0], int.Parse(parts[1]), int.Parse(parts[2]), Direction.Up);
                if (parts.Length > 3 && parts[3] == "surf") game.Player.SetMode(TravelMode.Surfing);
                Settle(); Shot($"wjx_{parts[0]}_{parts[1]}_{parts[2]}{(night ? "_night" : "")}");
                // What stands where the player was put: a spot that looks wrong is first a question of what the map holds there
                Console.WriteLine($"  {game.Map.Name} ({game.Player.GridX},{game.Player.GridY}): {game.Map.GetGroundTile(game.Player.GridX, game.Player.GridY)}"
                    + $"{(game.Map.IsSolid(game.Player.GridX, game.Player.GridY) ? ", blocked" : "")}, height {game.Map.HeightAt(game.Player.GridX, game.Player.GridY):0.##}"
                    + $", {game.Map.AreaAt(game.Player.GridX, game.Player.GridY)?.Key ?? "no area"}, state {game.State}");
                game.Player.SetMode(TravelMode.OnFoot);
                if (night) { engine.Settings.TimeOfDay = TimeOfDay.Day; engine.ApplySettings(window: false); }
            }
            return;
        }

        (string Name, string Map, int X, int Y, Direction Facing)[] places =
        {
            // Victory Road's back rooms and Route 224
            ("wj00_victory_road_room_1", "VictoryRoad1FRoom1", 6, 20, Direction.Right), ("wj01_victory_road_room_2", "VictoryRoad1FRoom2", 8, 47, Direction.Up),
            ("wj02_victory_road_room_3", "VictoryRoad1FRoom3", 20, 16, Direction.Right), ("wj03_route224_cave", "Sinnoh", 879, 562, Direction.Down),
            ("wj04_route224_grass", "Sinnoh", 885, 535, Direction.Up), ("wj05_route224_shore", "Sinnoh", 908, 524, Direction.Up),
            ("wj06_route224_stone", "Sinnoh", 908, 494, Direction.Up),
            // The Fight Area
            ("wj10_fight_area", "Sinnoh", 647, 431, Direction.Up), ("wj11_fight_area_quay", "Sinnoh", 621, 435, Direction.Left),
            ("wj12_fight_area_gate", "Sinnoh", 654, 424, Direction.Up), ("wj13_fight_area_houses", "Sinnoh", 652, 441, Direction.Up),
            ("wj14_fight_area_mart", "Sinnoh", 660, 431, Direction.Up), ("wj15_fight_area_containers", "Sinnoh", 631, 437, Direction.Down),
            // Route 225, the Survival Area, Routes 226 and 227
            ("wj20_route225_south", "Sinnoh", 630, 409, Direction.Up), ("wj21_route225_house", "Sinnoh", 618, 370, Direction.Up),
            ("wj22_route225_north", "Sinnoh", 628, 326, Direction.Up), ("wj23_survival_area", "Sinnoh", 659, 340, Direction.Up),
            ("wj24_survival_battleground", "Sinnoh", 652, 341, Direction.Up), ("wj25_survival_north", "Sinnoh", 656, 328, Direction.Up),
            ("wj26_route226_west", "Sinnoh", 690, 336, Direction.Right), ("wj27_route226_house", "Sinnoh", 739, 341, Direction.Up),
            ("wj28_route226_east", "Sinnoh", 760, 330, Direction.Right), ("wj29_route227_south", "Sinnoh", 755, 311, Direction.Up),
            ("wj2a_route227_house", "Sinnoh", 741, 304, Direction.Up), ("wj2b_route227_north", "Sinnoh", 758, 278, Direction.Up),
            // Route 228 in its sandstorm, Routes 229 and 230, the Resort Area
            ("wj30_route228_gate", "Sinnoh", 778, 330, Direction.Right), ("wj31_route228_slopes", "Sinnoh", 784, 346, Direction.Up),
            ("wj32_route228_house", "Sinnoh", 789, 358, Direction.Up), ("wj33_route228_south", "Sinnoh", 786, 403, Direction.Up),
            ("wj34_route229", "Sinnoh", 812, 432, Direction.Up), ("wj35_route229_pond", "Sinnoh", 824, 430, Direction.Up),
            ("wj36_route230_shore", "Sinnoh", 685, 422, Direction.Up), ("wj37_route230_island", "Sinnoh", 726, 428, Direction.Up),
            ("wj40_resort_area", "Sinnoh", 802, 474, Direction.Up), ("wj41_resort_ribbon_syndicate", "Sinnoh", 824, 455, Direction.Up),
            ("wj42_resort_villa", "Sinnoh", 822, 471, Direction.Up), ("wj43_resort_pool", "Sinnoh", 816, 457, Direction.Up),
            ("wj44_resort_pond", "Sinnoh", 810, 462, Direction.Down)
        };
        // The rest of the post-game (plan 01 · M10, part b)
        (string Name, string Map, int X, int Y, Direction Facing)[] later =
        {
            // Stark Mountain: its foot with the lakes of lava, its mouth, the crater (looked at from the rock beside it,
            // where nobody walks: from the mouth only its foot is in sight), and the three rooms inside
            ("wj60_stark_mountain_foot", "Sinnoh", 750, 248, Direction.Up), ("wj61_stark_mountain_mouth", "Sinnoh", 752, 235, Direction.Up),
            ("wj62_stark_mountain_crater", "Sinnoh", 757, 222, Direction.Up), ("wj63_stark_mountain_room_1", "StarkMountainRoom1", 20, 27, Direction.Up),
            ("wj64_stark_mountain_room_2", "StarkMountainRoom2", 42, 84, Direction.Up), ("wj65_stark_mountain_room_2_trainers", "StarkMountainRoom2", 48, 55, Direction.Up),
            ("wj66_stark_mountain_room_3", "StarkMountainRoom3", 7, 15, Direction.Up),
            // Snowpoint Temple: the hall with its statue, and the floors of ice under it
            ("wj70_snowpoint_temple_hall", "SnowpointTemple1F", 8, 11, Direction.Up), ("wj71_snowpoint_temple_b1f", "SnowpointTempleB1F", 9, 9, Direction.Up),
            ("wj72_snowpoint_temple_b3f", "SnowpointTempleB3F", 9, 9, Direction.Up), ("wj73_snowpoint_temple_b5f", "SnowpointTempleB5F", 11, 15, Direction.Up),
            // The islands, each with its pier and its wood
            ("wj80_fullmoon_island", "Sinnoh", 39, 277, Direction.Right), ("wj81_fullmoon_island_wood", "FullmoonIslandForest", 16, 19, Direction.Up),
            ("wj82_newmoon_island", "Sinnoh", 152, 277, Direction.Left), ("wj83_newmoon_island_wood", "NewmoonIslandForest", 16, 19, Direction.Up),
            // The Seabreak Path and Flower Paradise at its end
            ("wj84_seabreak_path", "Sinnoh", 911, 350, Direction.Up), ("wj85_flower_paradise", "Sinnoh", 911, 208, Direction.Up),
            // The Battle Frontier: in from the gate, the plaza, the Tower, and the four halls from the passages before their doors
            ("wj90_battle_frontier_gate", "BattleFrontier", 48, 50, Direction.Up), ("wj91_battle_frontier_plaza", "BattleFrontier", 48, 40, Direction.Up),
            ("wj92_battle_tower", "BattleFrontier", 48, 21, Direction.Up), ("wj93_battle_hall", "BattleFrontier", 22, 35, Direction.Left),
            ("wj94_battle_castle", "BattleFrontier", 35, 63, Direction.Left), ("wj95_battle_arcade", "BattleFrontier", 62, 63, Direction.Right),
            ("wj96_battle_factory", "BattleFrontier", 75, 35, Direction.Right), ("wj97_battle_frontier_booths", "BattleFrontier", 48, 43, Direction.Left)
        };
        foreach (var (name, map, x, y, facing) in places.Concat(later))
        {
            At(map, x, y, facing); Settle(); Shot(name);
        }

        // Route 224's north shore once the story has revealed the Seabreak Path (the chunks swapped), and put back
        game.Story.SetVar("VAR_HIDDEN_LOCATION_SEABREAK_PATH", 4136);
        At("Sinnoh", 908, 494, Direction.Up); Frames(4); Shot("wj86_route224_path_revealed");
        At("Sinnoh", 911, 482, Direction.Up); Frames(4); Shot("wj87_seabreak_path_foot");
        Console.WriteLine($"  the Seabreak Path revealed: ({game.Player.GridX},{game.Player.GridY}) is {game.Map.GetGroundTile(game.Player.GridX, game.Player.GridY)}"
            + $"{(game.Map.IsSolid(game.Player.GridX, game.Player.GridY) ? ", blocked" : "")}, swapped {game.Map.SwappedPlaces.Count(p => p.Swapped)} of {game.Map.SwappedPlaces.Count}");
        game.Story.SetVar("VAR_HIDDEN_LOCATION_SEABREAK_PATH", 0);
        Frames(2);

        // The Ribbon Syndicate's ground floor, through its own arrival script: shown out with no ribbons, let in with ten
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void ReadOn()
        {
            for (int i = 0; i < 20 && (Box().IsActive || game.State == GameState.Dialogue); i++)
            {
                if (engine.Choice.IsOpen) { Frames(14); engine.Choice.Move(1); engine.Choice.Confirm(); Frames(4); continue; }
                Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); Box().Advance(); Frames(2);
            }
        }
        void Arrive()
        {
            At("ResortAreaRibbonSyndicate1F", 15, 15, Direction.Up);
            game.ArriveOnMap();
            Until(() => Box().IsActive, "the word of the hostess");
            if (Box().IsActive) { Box().FinishLine(); Frames(2); }
        }
        var lead = game.Party.Members[0];
        var worn = lead.Ribbons.ToList();
        Arrive(); Shot("wj98_ribbon_syndicate_refused");
        ReadOn();
        Until(() => game.Map.Name == "Sinnoh" && game.State == GameState.Overworld, "the player shown out");
        Frames(40); Shot("wj99_ribbon_syndicate_shown_out");
        Console.WriteLine($"  the Ribbon Syndicate with {RibbonRules.CountPartyRibbons(game.Party)} ribbons: shown out to {game.Map.Name} ({game.Player.GridX},{game.Player.GridY}), a member {game.Story.Has("FLAG_JOINED_RIBBON_SYNDICATE")}");
        foreach (var ribbon in RibbonDatabase.All.Take(10)) RibbonRules.Give(lead, ribbon.Id);
        Arrive(); Shot("wj9a_ribbon_syndicate_welcome");
        ReadOn();
        Until(() => game.Map.Everyone.Any(n => n.Key == "beauty" && n.GridX == 16) && game.State == GameState.Overworld, "the hostess aside");
        Frames(20); Shot("wj9b_ribbon_syndicate_let_in");
        var hostess = game.Map.Everyone.First(n => n.Key == "beauty");
        Console.WriteLine($"  the Ribbon Syndicate with {RibbonRules.CountPartyRibbons(game.Party)} ribbons: in {game.Map.Name}, a member {game.Story.Has("FLAG_JOINED_RIBBON_SYNDICATE")}, the hostess at ({hostess.GridX},{hostess.GridY})");
        At("ResortAreaRibbonSyndicate1F", 15, 9, Direction.Up); Frames(2); Shot("wj9c_ribbon_syndicate_hall");
        At("ResortAreaRibbonSyndicate1F", 23, 14, Direction.Up); Frames(2); Shot("wj9d_ribbon_syndicate_display");
        At("ResortAreaRibbonSyndicate1F", 5, 11, Direction.Down); Frames(2); Shot("wj9e_ribbon_syndicate_parlour");
        At("ResortAreaRibbonSyndicate1F", 15, 6, Direction.Up); game.Interact();
        Until(() => engine.Choice.IsOpen, "the offer of the cashier"); Frames(14); Shot("wj9f_ribbon_syndicate_cashier");
        ReadOn();
        // As it was: the mode leaves no ribbons and no membership behind for whatever is shot after it
        foreach (string ribbon in lead.Ribbons.Except(worn).ToList()) lead.Ribbons.Remove(ribbon);
        game.Story.Unset("FLAG_JOINED_RIBBON_SYNDICATE");
        game.Story.SetVar("VAR_RESORT_AREA_RIBBON_SYNDICATE_1F_STATE", 0);

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        At("Sinnoh", 647, 431, Direction.Up); Frames(2); Shot("wj50_fight_area_night");
        At("Sinnoh", 621, 435, Direction.Left); Frames(2); Shot("wj51_fight_area_quay_night");
        At("Sinnoh", 659, 340, Direction.Up); Frames(2); Shot("wj52_survival_area_night");
        At("Sinnoh", 802, 474, Direction.Up); Frames(2); Shot("wj53_resort_area_night");
        At("Sinnoh", 824, 455, Direction.Up); Frames(2); Shot("wj54_resort_ribbon_syndicate_night");
        At("Sinnoh", 784, 346, Direction.Up); Frames(2); Shot("wj55_route228_night");
        // Part b after dark: the mountain's lava and its crater, the Frontier's lamps and windows, an island, the meadow
        At("Sinnoh", 750, 248, Direction.Up); Settle(); Shot("wja0_stark_mountain_foot_night");
        At("Sinnoh", 752, 235, Direction.Up); Settle(); Shot("wja1_stark_mountain_mouth_night");
        At("Sinnoh", 757, 222, Direction.Up); Settle(); Shot("wja2_stark_mountain_crater_night");
        At("BattleFrontier", 48, 44, Direction.Up); Frames(2); Shot("wja3_battle_frontier_plaza_night");
        At("BattleFrontier", 48, 22, Direction.Up); Frames(2); Shot("wja4_battle_tower_night");
        At("BattleFrontier", 35, 63, Direction.Left); Frames(2); Shot("wja5_battle_castle_night");
        At("BattleFrontier", 62, 63, Direction.Right); Frames(2); Shot("wja6_battle_arcade_night");
        At("Sinnoh", 39, 277, Direction.Right); Frames(2); Shot("wja7_fullmoon_island_night");
        At("Sinnoh", 911, 208, Direction.Up); Frames(2); Shot("wja8_flower_paradise_night");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
    }
}
