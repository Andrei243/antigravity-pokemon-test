partial class Harness
{
    // ---------------------------------------------------------------- terrain and nature (plan 04 · G4)
    public void TerrainMode()
    {
        // Water: the pond, the lake from its shore and from its east bank, with native-resolution close-ups
        GoTo("TwinleafTown", 9, 14, Direction.Left); Frames(2); Shot("t1_pond");
        ShotCrop("t1b_pond_native", 330, 500, 560, 400, 2);
        GoTo("LakeVerity", 14, 10, Direction.Up); Frames(2); Shot("t2_lake");
        GoTo("LakeVerity", 24, 5, Direction.Left); Frames(2); Shot("t2b_lake_east");
        ShotCrop("t2c_lake_native", 560, 300, 560, 400, 2);

        // Tall grass with someone standing in it, the ledge, a boulder
        GoTo("Route201", 22, 15, Direction.Down); Frames(2); Shot("t3_in_grass");
        ShotCrop("t3b_grass_native", 680, 340, 560, 400, 2);
        GoTo("Route201", 22, 10, Direction.Down); Frames(2); Shot("t4_ledge");
        ShotCrop("t4b_ledge_native", 560, 560, 560, 300, 2);

        // Trees: Twinleaf's pines and Route 201's round trees
        GoTo("TwinleafTown", 3, 3, Direction.Left); Frames(2); Shot("t5_pines");
        ShotCrop("t5b_pines_native", 0, 300, 560, 400, 2);
        GoTo("Route201", 32, 3, Direction.Right); Frames(2); Shot("t6_round_trees");
        ShotCrop("t6b_round_native", 1300, 200, 560, 400, 2);
        GoTo("TwinleafTown", 6, 9, Direction.Up); Frames(2); Shot("t7_flowers");
        ShotCrop("t7b_flowers_native", 620, 300, 560, 400, 2);

        // The other ground kinds, on a sample map: sand round a pool, dirt, snow and a cave floor
        var sample = new Map(26, 16) { Name = "TerrainSample", DisplayName = "Terrain sample" };
        for (int x = 0; x < 26; x++) { sample.SetGroundTile(x, 0, TileType.Tree, true); sample.SetGroundTile(x, 15, TileType.Tree, true); }
        for (int y = 0; y < 16; y++) { sample.SetGroundTile(0, y, TileType.Tree, true); sample.SetGroundTile(25, y, TileType.Tree, true); }
        void Patch(int x0, int y0, int w, int h, TileType t) { for (int y = y0; y < y0 + h; y++) for (int x = x0; x < x0 + w; x++) sample.SetGroundTile(x, y, t); }
        Patch(2, 2, 10, 6, TileType.Sand);
        Patch(5, 4, 4, 2, TileType.Water);
        Patch(14, 2, 9, 5, TileType.Dirt);
        Patch(2, 9, 10, 5, TileType.Snow);
        Patch(14, 9, 9, 5, TileType.CaveFloor);
        Patch(12, 2, 1, 12, TileType.Path);
        game.Map = sample;
        game.Player.SetPosition(12, 6, Direction.Down);
        game.State = GameState.Overworld;
        Frames(2); Shot("t8_ground_kinds_north");
        game.Player.SetPosition(12, 10, Direction.Down);
        Frames(2); Shot("t8b_ground_kinds_south");
        ShotCrop("t8c_ground_native", 100, 300, 760, 420, 2);

        // Water after dark
        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        GoTo("LakeVerity", 14, 10, Direction.Up); Frames(2); Shot("t9_lake_night");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        GoTo("Route201", 22, 10, Direction.Down);
        Timing("route 201");
        GoTo("LakeVerity", 14, 10, Direction.Up);
        Timing("lake verity");

        // Battles: boulders round the meadow, and a lake behind the opponent on maps that have one
        var lakeBattle = StartBattle("Bidoof", 4, null, "LakeVerity");
        ToMainMenu(lakeBattle);
        Shot("t10_battle_lakeside");
        Timing("battle by the lake");
        var routeBattle = StartBattle("Starly", 4, null, "Route201");
        ToMainMenu(routeBattle);
        Shot("t11_battle_route");
        game.State = GameState.Overworld;

        if (args.Length > 2) Boards(args[2], new[] { "t1_pond", "t2_lake", "t2b_lake_east", "t3_in_grass", "t4_ledge", "t5_pines", "t6_round_trees", "t7_flowers", "t9_lake_night", "t10_battle_lakeside", "t11_battle_route" });
    }
}
