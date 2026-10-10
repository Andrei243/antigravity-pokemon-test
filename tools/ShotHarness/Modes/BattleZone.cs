partial class Harness
{
    // Not part of `all` but the end of `world`: the Battle Zone's towns and routes (plan 01 · M10, part a) and Route 224
    // behind Victory Road's back rooms, by day, the three towns after dark, Route 228 in its sandstorm and Route 227
    // under its ash (`wj*`). Given places after the mode's name (`battlezone Sinnoh,650,430 Sinnoh,802,474,night`), it
    // shoots those instead, each as `wjx_<map>_<x>_<y>`: how a spot is looked at while its area is being built.
    public void BattleZoneMode()
    {
        if (mode == "battlezone" && args.Length > 2)
        {
            foreach (string spot in args.Skip(2))
            {
                string[] parts = spot.Split(',');
                bool night = parts.Length > 3 && parts[3] == "night";
                if (night) { engine.Settings.TimeOfDay = TimeOfDay.Night; engine.ApplySettings(window: false); }
                At(parts[0], int.Parse(parts[1]), int.Parse(parts[2]), Direction.Up);
                if (parts.Length > 3 && parts[3] == "surf") game.Player.SetMode(TravelMode.Surfing);
                Frames(2); Shot($"wjx_{parts[0]}_{parts[1]}_{parts[2]}{(night ? "_night" : "")}");
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
        foreach (var (name, map, x, y, facing) in places)
        {
            At(map, x, y, facing); Frames(2); Shot(name);
        }

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        At("Sinnoh", 647, 431, Direction.Up); Frames(2); Shot("wj50_fight_area_night");
        At("Sinnoh", 621, 435, Direction.Left); Frames(2); Shot("wj51_fight_area_quay_night");
        At("Sinnoh", 659, 340, Direction.Up); Frames(2); Shot("wj52_survival_area_night");
        At("Sinnoh", 802, 474, Direction.Up); Frames(2); Shot("wj53_resort_area_night");
        At("Sinnoh", 824, 455, Direction.Up); Frames(2); Shot("wj54_resort_ribbon_syndicate_night");
        At("Sinnoh", 784, 346, Direction.Up); Frames(2); Shot("wj55_route228_night");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
    }
}
