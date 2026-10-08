partial class Harness
{
    public void WorldMode()
    {
        // The first towns and routes as the imported world has them, with the places where one area runs into the next
        (string Name, string Map, int X, int Y, Direction Facing)[] places =
        {
            ("w01_twinleaf", "Sinnoh", 112, 880, Direction.Down), ("w02_twinleaf_home", "Sinnoh", 116, 886, Direction.Up),
            ("w03_twinleaf_pond", "Sinnoh", 111, 890, Direction.Down), ("w04_twinleaf_to_route201", "Sinnoh", 112, 864, Direction.Up),
            ("w05_route201_briefcase", "Sinnoh", 110, 855, Direction.Right), ("w06_route201_grass", "Sinnoh", 127, 853, Direction.Right),
            ("w07_route201_to_sandgem", "Sinnoh", 160, 845, Direction.Right), ("w08_sandgem", "Sinnoh", 178, 845, Direction.Up),
            ("w09_sandgem_lab", "Sinnoh", 168, 844, Direction.Up), ("w10_sandgem_beach", "Sinnoh", 184, 858, Direction.Down),
            ("w11_route219", "Sinnoh", 183, 868, Direction.Down), ("w12_sandgem_to_route202", "Sinnoh", 186, 832, Direction.Up),
            ("w13_route202", "Sinnoh", 174, 815, Direction.Up), ("w14_route202_north", "Sinnoh", 174, 802, Direction.Up),
            ("w15_lakefront", "Sinnoh", 81, 846, Direction.Up), ("w16_lake", "LakeVerity", 44, 46, Direction.Up),
            ("w17_lake_east", "LakeVerity", 51, 40, Direction.Left),
            // The south-west (plan 01 · M5): Jubilife City, the way east to Oreburgh and its mine, the way north to Floaroma
            ("w30_jubilife_south", "Sinnoh", 175, 792, Direction.Up), ("w31_jubilife_crossroads", "Sinnoh", 175, 760, Direction.Up),
            ("w32_jubilife_tv", "Sinnoh", 164, 757, Direction.Up), ("w33_jubilife_poketch", "Sinnoh", 143, 757, Direction.Up),
            ("w34_jubilife_school", "Sinnoh", 172, 779, Direction.Up), ("w35_jubilife_terminal", "Sinnoh", 150, 781, Direction.Up),
            ("w36_jubilife_north", "Sinnoh", 174, 742, Direction.Up), ("w37_route203_west", "Sinnoh", 202, 757, Direction.Right),
            ("w38_route203_east", "Sinnoh", 240, 752, Direction.Right), ("w39_oreburgh_gate", "OreburghGate1F", 8, 22, Direction.Right),
            ("w40_oreburgh_gate_rocks", "OreburghGate1F", 16, 6, Direction.Right), ("w41_oreburgh_gate_b1f", "OreburghGateB1F", 46, 6, Direction.Left),
            ("w42_oreburgh_west", "Sinnoh", 264, 750, Direction.Right), ("w43_oreburgh_gym", "Sinnoh", 282, 759, Direction.Up),
            ("w44_oreburgh_center", "Sinnoh", 303, 759, Direction.Up), ("w45_oreburgh_yard", "Sinnoh", 302, 782, Direction.Down),
            ("w46_oreburgh_mine_mouth", "Sinnoh", 302, 791, Direction.Down), ("w47_mine_b1f", "OreburghMineB1F", 12, 5, Direction.Down),
            ("w48_mine_b2f", "OreburghMineB2F", 15, 16, Direction.Down), ("w49_mine_b2f_machine", "OreburghMineB2F", 14, 28, Direction.Up),
            ("w50_route204_south", "Sinnoh", 172, 722, Direction.Up), ("w51_ravaged_path", "RavagedPath", 19, 47, Direction.Up),
            ("w52_ravaged_path_water", "RavagedPath", 6, 26, Direction.Right), ("w53_route204_north", "Sinnoh", 178, 690, Direction.Up),
            ("w54_floaroma", "Sinnoh", 178, 661, Direction.Up), ("w55_floaroma_shop", "Sinnoh", 180, 654, Direction.Up),
            ("w56_meadow", "FloaromaMeadow", 30, 30, Direction.Up), ("w57_meadow_house", "FloaromaMeadow", 39, 47, Direction.Up),
            ("w58_route205", "Sinnoh", 210, 632, Direction.Up), ("w59_route205_north", "Sinnoh", 208, 600, Direction.Up),
            ("w60_windworks", "Sinnoh", 243, 657, Direction.Up), ("w61_fuego", "Sinnoh", 169, 590, Direction.Up),
            // The centre (plan 01 · M6): Eterna Forest and Eterna City, the Cycling Road and Routes 206 to 208, Wayward
            // Cave and Mt. Coronet's south, Hearthome City and Amity Square, Route 209, Solaceon Town and its ruins
            ("w90_forest_way_in", "Sinnoh", 206, 583, Direction.Up), ("w91_eterna_forest", "EternaForest", 30, 82, Direction.Up),
            ("w92_eterna_forest_deep", "EternaForest", 60, 68, Direction.Right), ("w93_old_chateau", "EternaForest", 74, 18, Direction.Up),
            ("w94_route205_north", "Sinnoh", 270, 532, Direction.Right), ("w95_eterna", "Sinnoh", 310, 545, Direction.Up),
            ("w96_eterna_statue", "Sinnoh", 327, 530, Direction.Up), ("w97_eterna_gate", "Sinnoh", 304, 566, Direction.Down),
            ("w98_under_cycling_road", "Sinnoh", 299, 613, Direction.Up), ("w99_route206_south", "Sinnoh", 305, 695, Direction.Up),
            ("wa0_wayward_cave", "WaywardCave1F", 42, 51, Direction.Up), ("wa1_route207", "Sinnoh", 325, 720, Direction.Right),
            ("wa2_coronet", "MtCoronet1FSouth", 6, 8, Direction.Right), ("wa3_route208", "Sinnoh", 420, 722, Direction.Right),
            ("wa4_hearthome", "Sinnoh", 475, 700, Direction.Up), ("wa5_hearthome_contest", "Sinnoh", 479, 694, Direction.Up),
            ("wa6_amity_square", "AmitySquare", 11, 48, Direction.Up), ("wa7_amity_square_inside", "AmitySquare", 32, 30, Direction.Up),
            ("wa8_route209", "Sinnoh", 530, 720, Direction.Right), ("wa9_solaceon", "Sinnoh", 570, 660, Direction.Up),
            ("wb0_solaceon_ruins", "SolaceonRuinsRoom1", 5, 9, Direction.Up),
            // The east and the sea (plan 01 · M7): Route 210 and Celestic Town, Route 215 and Veilstone City, Route 214 and
            // the lakes, Route 213 and Pastoria City, the Great Marsh, Route 212, Route 218 and Canalave City, Iron Island
            ("wc0_route210_south", "Sinnoh", 560, 592, Direction.Up), ("wc1_route210_north", "Sinnoh", 527, 524, Direction.Left),
            ("wc2_route210_bridge", "Sinnoh", 502, 530, Direction.Left), ("wc3_celestic", "Sinnoh", 465, 528, Direction.Up),
            ("wc4_route215", "Sinnoh", 624, 591, Direction.Right), ("wc5_veilstone", "Sinnoh", 717, 613, Direction.Up),
            ("wc6_veilstone_warehouses", "Sinnoh", 692, 586, Direction.Up), ("wc7_route214", "Sinnoh", 720, 688, Direction.Up),
            ("wc8_valor_lakefront", "Sinnoh", 708, 780, Direction.Up), ("wc9_lake_valor", "LakeValor", 38, 14, Direction.Down),
            ("wd0_route213", "Sinnoh", 688, 832, Direction.Right), ("wd1_hotel_pavilion", "Sinnoh", 691, 807, Direction.Up),
            ("wd2_pastoria", "Sinnoh", 600, 817, Direction.Up), ("wd3_pastoria_boats", "Sinnoh", 623, 835, Direction.Down),
            ("wd4_great_marsh", "GreatMarsh", 68, 114, Direction.Up), ("wd5_route212_mansion", "Sinnoh", 470, 774, Direction.Up),
            ("wd6_route212_puddles", "Sinnoh", 478, 838, Direction.Down), ("wd7_trophy_garden", "TrophyGarden", 14, 22, Direction.Up),
            ("wd8_route218", "Sinnoh", 93, 752, Direction.Left), ("wd9_canalave", "Sinnoh", 58, 724, Direction.Up),
            ("we0_canalave_bridge", "Sinnoh", 47, 725, Direction.Left), ("we1_iron_island", "Sinnoh", 100, 502, Direction.Right),
            ("we2_iron_island_lift", "IronIslandB1FRight", 11, 20, Direction.Down), ("we3_route221", "Sinnoh", 272, 912, Direction.Up),
            // The north and the end (plan 01 · M8): Route 211 and Mt. Coronet to its summit and Spear Pillar, Routes 216
            // and 217 in the snow, Acuity Lakefront and Lake Acuity, Snowpoint City; Route 222, Sunyshore City, Route 223,
            // Victory Road and the Pokémon League
            ("wf0_route211_west", "Sinnoh", 367, 524, Direction.Up), ("wf1_route211_east", "Sinnoh", 428, 541, Direction.Up),
            ("wf2_coronet_north", "MtCoronet1FNorthRoom1", 22, 27, Direction.Up), ("wf3_coronet_2f", "MtCoronet2F", 13, 61, Direction.Up),
            ("wf4_coronet_summit", "MtCoronetOutsideNorth", 43, 45, Direction.Up), ("wf5_spear_pillar", "SpearPillar", 31, 49, Direction.Up),
            ("wf6_route216", "Sinnoh", 337, 393, Direction.Up), ("wf7_route217", "Sinnoh", 314, 297, Direction.Up),
            ("wf8_acuity_lakefront", "Sinnoh", 323, 236, Direction.Up), ("wf9_lake_acuity", "LakeAcuity", 16, 47, Direction.Up),
            ("wg0_snowpoint", "Sinnoh", 363, 223, Direction.Up), ("wg1_route222", "Sinnoh", 769, 790, Direction.Up),
            ("wg2_sunyshore", "Sinnoh", 856, 783, Direction.Up), ("wg3_route223", "Sinnoh", 844, 683, Direction.Up),
            ("wg4_victory_road", "VictoryRoad1F", 28, 47, Direction.Up), ("wg5_pokemon_league", "Sinnoh", 848, 599, Direction.Up)
        };
        foreach (var (name, map, x, y, facing) in places)
        {
            At(map, x, y, facing); Frames(2); Shot(name);
            if (name is "w01_twinleaf" or "w08_sandgem") ShotCrop(name + "_native", 640, 300, 640, 360, 2);
        }

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        At("Sinnoh", 112, 880, Direction.Down); Frames(2); Shot("w20_twinleaf_night");
        At("Sinnoh", 178, 845, Direction.Up); Frames(2); Shot("w21_sandgem_night");
        At("Sinnoh", 175, 760, Direction.Up); Frames(2); Shot("w70_jubilife_night");
        At("Sinnoh", 282, 759, Direction.Up); Frames(2); Shot("w71_oreburgh_night");
        At("Sinnoh", 302, 782, Direction.Down); Frames(2); Shot("w72_oreburgh_yard_night");
        At("Sinnoh", 178, 661, Direction.Up); Frames(2); Shot("w73_floaroma_night");
        At("Sinnoh", 310, 545, Direction.Up); Frames(2); Shot("w75_eterna_night");
        At("Sinnoh", 475, 700, Direction.Up); Frames(2); Shot("w76_hearthome_night");
        At("Sinnoh", 570, 660, Direction.Up); Frames(2); Shot("w77_solaceon_night");
        At("Sinnoh", 717, 613, Direction.Up); Frames(2); Shot("w78_veilstone_night");
        At("Sinnoh", 59, 731, Direction.Left); Frames(2); Shot("w79_canalave_night");
        At("Sinnoh", 363, 223, Direction.Up); Frames(2); Shot("wg6_snowpoint_night");
        At("Sinnoh", 856, 783, Direction.Up); Frames(2); Shot("wg7_sunyshore_night");
        // A cave's light ignores the clock: the same picture as by day
        At("OreburghGate1F", 8, 22, Direction.Right); Frames(2); Shot("w74_oreburgh_gate_night");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // A cave nobody has lit. None of the south-west's is dark (the original's one is Wayward Cave), so Oreburgh
        // Gate is made dark for the picture: the circle round the player, and the cave lit once Flash has been used
        // there (plan 02 · S2: the flag the move sets; the fieldmoves mode uses it from the party menu)
        {
            var gate = MapDatabase.Get("OreburghGate1F");
            gate.IsDark = true;
            At("OreburghGate1F", 16, 22, Direction.Right); Frames(2); Shot("w80_dark_cave");
            At("OreburghGate1F", 8, 22, Direction.Left); Frames(2); Shot("w81_dark_cave_at_the_mouth");
            var lit = game.Story;
            At("OreburghGate1F", 16, 22, Direction.Right);
            lit.Set(FieldMoveRules.FlashFlag);
            Frames(2); Shot("w82_dark_cave_with_flash");
            lit.Unset(FieldMoveRules.FlashFlag);
            gate.IsDark = false;
            Frames(2);
        }

        if (args.Length > 2)
            Boards(args[2], new[] { "w01_twinleaf", "w02_twinleaf_home", "w06_route201_grass", "w08_sandgem", "w09_sandgem_lab", "w13_route202", "w14_route202_north", "w16_lake", "w20_twinleaf_night" });

        // The walks the plan asks for, tile by tile along the shortest way, every frame timed: from the player's door in
        // Twinleaf Town to the top of Route 202 (M2), and on through Jubilife City and along Route 203 to the mouth of
        // Oreburgh Gate (M5). A chunk that has to be waited for shows up as one long frame.
        if (filter.Length == 0 || Environment.GetEnvironmentVariable("SHOTS_TIMING") == "1")
        {
            TimedWalk("Twinleaf Town to Route 202", (116, 886), (174, 801));
            TimedWalk("Route 202 through Jubilife City to Oreburgh Gate", (174, 801), (245, 749));
            TimedWalk("Jubilife City up Route 204 to the Ravaged Path", (175, 760), (171, 706));
        }

        void TimedWalk(string label, (int X, int Y) from, (int X, int Y) to)
        {
            var sinnoh = MapDatabase.Get("Sinnoh");
            var path = WalkingPath(sinnoh, from, to);
            var renderer = game.World;
            At("Sinnoh", from.X, from.Y, Direction.Up); Frames(30);
            renderer.ResetStreamingStats();
            double total = 0, worst = 0;
            (int X, int Y) worstAt = default;
            int frames = 0, mostChunks = 0;
            var watch = new System.Diagnostics.Stopwatch();
            foreach (var (x, y) in path)
            {
                game.Player.SetPosition(x, y, Direction.Up);
                // Eight tiles a second, the pace of running, with each frame held to a sixtieth of a second as the
                // screen's refresh holds it: the chunks ahead get the time to bake that they get in play
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
            Console.WriteLine($"walk from {label}: {path.Count} tiles, {total / frames:F2} ms/frame, worst frame {worst:F1} ms at ({worstAt.X}, {worstAt.Y}), at most {mostChunks} chunks loaded");
            Console.WriteLine($"  streaming: {renderer.Streaming}");
        }

        foreach (var (label, map, x, y) in new[]
                 {
                     ("twinleaf", "Sinnoh", 112, 881), ("route 201", "Sinnoh", 127, 853), ("sandgem", "Sinnoh", 178, 845), ("route 202", "Sinnoh", 174, 815), ("lake verity", "LakeVerity", 44, 46),
                     ("jubilife crossroads", "Sinnoh", 175, 760), ("jubilife school", "Sinnoh", 172, 779), ("route 203", "Sinnoh", 220, 755), ("oreburgh gate", "OreburghGate1F", 16, 22),
                     ("oreburgh city", "Sinnoh", 282, 759), ("oreburgh's mine yard", "Sinnoh", 302, 782), ("oreburgh mine", "OreburghMineB2F", 15, 16), ("route 204", "Sinnoh", 172, 722),
                     ("ravaged path", "RavagedPath", 19, 47), ("floaroma town", "Sinnoh", 178, 661), ("floaroma meadow", "FloaromaMeadow", 30, 30), ("route 205", "Sinnoh", 210, 632),
                     ("valley windworks", "Sinnoh", 243, 657)
                 })
        {
            At(map, x, y, Direction.Up);
            Timing(label);
        }

        // The Distortion World and the north's last stand-ins (plan 01 · M8, part 2)
        DistortionMode();
    }

    // The shortest way on foot between two tiles (ledges are walked round, as they must be going north)
    List<(int X, int Y)> WalkingPath(Map map, (int X, int Y) from, (int X, int Y) to)
    {
        var came = new Dictionary<(int, int), (int, int)> { [from] = from };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue(from);
        while (queue.Count > 0 && !came.ContainsKey(to))
        {
            var (x, y) = queue.Dequeue();
            foreach (var next in new[] { (x, y - 1), (x + 1, y), (x - 1, y), (x, y + 1) })
            {
                if (came.ContainsKey(next) || !(map.IsWalkable(next.Item1, next.Item2) || next == to)) continue;
                came[next] = (x, y);
                queue.Enqueue(next);
            }
        }
        if (!came.ContainsKey(to)) throw new InvalidOperationException($"No way on foot from {from} to {to}");
        var path = new List<(int X, int Y)>();
        for (var at = to; at != from; at = came[at]) path.Add(at);
        path.Reverse();
        return path;
    }
}
