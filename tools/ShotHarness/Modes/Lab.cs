partial class Harness
{
    public void LabMode()
    {
        var lab = BuildTerrainLab();
        var hiker = game.Player;
        void Stand(string name, int x, int y, Direction facing, TravelMode travel = TravelMode.OnFoot, float? height = null)
        {
            game.Map = lab;
            hiker.SetPosition(x, y, facing);
            hiker.SetMode(travel);
            if (height is { } h) hiker.SetHeight(h);
            game.State = GameState.Overworld;
            Frames(3);
            Shot(name);
        }

        Stand("lab01_mountain_stairs", 9, 15, Direction.Up);
        ShotCrop("lab01b_stairs_native", 600, 180, 640, 360, 2);
        Stand("lab02_plateau", 19, 7, Direction.Down);
        Stand("lab03_waterfall", 20, 15, Direction.Up);
        ShotCrop("lab03b_waterfall_native", 760, 100, 640, 360, 2);
        Stand("lab04_rock_climb", 13, 15, Direction.Up);
        Stand("lab05_bridge", 23, 17, Direction.Right, height: 2f);
        ShotCrop("lab05b_bridge_native", 640, 280, 640, 360, 2);
        Stand("lab06_under_the_bridge", 23, 21, Direction.Up, TravelMode.Surfing);
        Stand("lab07_gorge_stairs", 13, 18, Direction.Right);
        Stand("lab08_boardwalk", 23, 24, Direction.Right, height: 0f);
        Stand("lab09_ice", 6, 26, Direction.Down);
        Stand("lab10_snow", 15, 25, Direction.Right);
        ShotCrop("lab10b_snow_native", 640, 300, 640, 360, 2);
        Stand("lab11_marsh", 30, 24, Direction.Down);
        Stand("lab12_beach", 40, 26, Direction.Down);
        Stand("lab13_surfing", 40, 30, Direction.Left, TravelMode.Surfing);
        ShotCrop("lab13b_surfing_native", 640, 300, 640, 360, 2);
        Stand("lab14_ledges", 40, 16, Direction.Down);
        ShotCrop("lab14b_ledges_native", 640, 260, 640, 360, 2);
        Stand("lab15_obstacles", 29, 22, Direction.Up);
        ShotCrop("lab15b_obstacles_native", 640, 260, 640, 360, 2);

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        Stand("lab20_mountain_night", 9, 15, Direction.Up);
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        hiker.SetPosition(20, 15, Direction.Up);
        hiker.SetMode(TravelMode.OnFoot);
        Timing("terrain lab");
        hiker.SetMode(TravelMode.OnFoot);
    }

    // ---------------------------------------------------------------- terrain lab

    // A map made here to show every terrain feature (plan 01 · M3) in one place, since the areas open so far are
    // nearly flat: a mountain with stairs, a rock face to climb and a river falling off it, a gorge with a bridge
    // over the river and a boardwalk across it lower down, a sheet of ice, snow of every depth, a marsh, a beach,
    // and a lawn fenced by ledges that face south, west and east.
    Map BuildTerrainLab()
    {
        const int W = 48, H = 34;
        var m = new Map(W, H) { Name = "TerrainLab", DisplayName = "Terrain Lab", Trees = TreeStyle.Pine };
        void Fill(int x0, int y0, int x1, int y1, TileType type, bool solid = false)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    m.SetGroundTile(x, y, type, solid);
        }
        void Raise(int x0, int y0, int x1, int y1, float height, float slopeX = 0f, float slopeZ = 0f)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    m.SetHeight(x, y, height, slopeX, slopeZ);
        }
        void Does(int x0, int y0, int x1, int y1, TileBehavior behaviour)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    m.SetBehaviour(x, y, behaviour);
        }

        Raise(0, 0, W - 1, H - 1, 0f);
        Fill(0, 0, W - 1, 0, TileType.Tree, true);
        Fill(0, H - 1, W - 1, H - 1, TileType.Tree, true);
        Fill(0, 0, 0, H - 1, TileType.Tree, true);
        Fill(W - 1, 0, W - 1, H - 1, TileType.Tree, true);

        // The mountain: a plateau three tiles up across the north, lawn and pines on top, bare rock along its rim
        Raise(1, 1, W - 2, 10, 3f);
        Fill(1, 9, W - 2, 10, TileType.Rock);
        Fill(3, 2, 5, 3, TileType.Tree, true);
        Fill(12, 2, 13, 2, TileType.Tree, true);
        Fill(28, 2, 31, 3, TileType.Tree, true);
        Fill(36, 4, 42, 7, TileType.TallGrass);
        Fill(16, 5, 19, 6, TileType.FlowerGrass);

        // Notches cut into the rim, with a spur of rock either side of each: stairs, a face to climb, and the river's fall
        void Notch(int x0, int x1)
        {
            Fill(x0 - 1, 11, x0 - 1, 13, TileType.Rock, true);
            Fill(x1 + 1, 11, x1 + 1, 13, TileType.Rock, true);
            Raise(x0 - 1, 11, x0 - 1, 13, 3f);
            Raise(x1 + 1, 11, x1 + 1, 13, 3f);
            for (int y = 11; y <= 13; y++) Raise(x0, y, x1, y, 2.5f - (y - 11), 0f, -1f);
        }
        Notch(8, 9);
        Fill(8, 11, 9, 13, TileType.Stairs);
        Notch(14, 15);
        Fill(14, 11, 15, 13, TileType.Rock, true);
        Does(14, 11, 15, 13, TileBehavior.RockClimbNorthSouth);

        // The river: along the plateau, down the fall, and on south through the lowland, half a tile under its banks
        Fill(22, 1, 24, 10, TileType.Water);
        Raise(22, 1, 24, 10, 2.5f);
        Notch(22, 24);
        Fill(22, 11, 24, 13, TileType.Water);
        Does(22, 11, 24, 13, TileBehavior.Waterfall);
        for (int y = 11; y <= 13; y++) Raise(22, y, 24, y, 2f - (y - 11), 0f, -1f);
        Fill(22, 14, 24, H - 2, TileType.Water);
        Raise(22, 14, 24, H - 2, -0.5f);

        // The gorge: a terrace of rock two tiles up on each bank, stairs up to each from the far side, a bridge between
        Fill(16, 16, 21, 19, TileType.Rock);
        Raise(16, 16, 21, 19, 2f);
        Fill(25, 16, 30, 19, TileType.Rock);
        Raise(25, 16, 30, 19, 2f);
        Fill(22, 17, 24, 18, TileType.Planks);
        Does(22, 17, 24, 18, TileBehavior.BridgeOverWater);
        for (int x = 22; x <= 24; x++)
            for (int y = 17; y <= 18; y++)
                m.SetDeck(x, y, 2f);
        Fill(14, 17, 15, 18, TileType.Stairs);
        Raise(14, 17, 14, 18, 0.5f, 1f);
        Raise(15, 17, 15, 18, 1.5f, 1f);
        Fill(31, 17, 32, 18, TileType.Stairs);
        Raise(31, 17, 31, 18, 1.5f, -1f);
        Raise(32, 17, 32, 18, 0.5f, -1f);
        m.NPCs.Add(new NPC { Name = "Hiker", NpcType = "Gentleman", GridX = 18, GridY = 17, Facing = Direction.Down });

        // A boardwalk across the river lower down, level with its banks
        Fill(21, 24, 25, 25, TileType.Planks);
        Does(22, 24, 24, 25, TileBehavior.BridgeOverWater);
        for (int x = 22; x <= 24; x++)
            for (int y = 24; y <= 25; y++)
                m.SetDeck(x, y, 0f);
        Fill(10, 20, 21, 20, TileType.Path);
        Fill(25, 20, 34, 20, TileType.Path);

        // A sheet of ice with two rocks on it
        Fill(3, 22, 10, 29, TileType.Ice);
        m.AddProp(PropType.Boulder, 6, 24);
        m.AddProp(PropType.Boulder, 8, 27);

        // Snow, deeper band by band toward the east
        Fill(12, 22, 19, 29, TileType.Snow);
        Does(14, 22, 15, 29, TileBehavior.DeepSnow);
        Does(16, 22, 17, 29, TileBehavior.DeeperSnow);
        Does(18, 22, 19, 29, TileBehavior.DeepestSnow);

        // A marsh, its grass and a patch of deep mud
        Fill(27, 22, 33, 28, TileType.Marsh);
        Fill(28, 23, 29, 25, TileType.TallGrass);
        Does(28, 23, 29, 25, TileBehavior.MarshGrass);
        Does(31, 25, 32, 27, TileBehavior.DeepMud);

        // A beach running down into a bay, with two puddles on it
        Fill(35, 24, 46, 32, TileType.Sand);
        Fill(37, 28, 46, 32, TileType.Water);
        Raise(37, 28, 46, 32, -0.5f);
        m.SetBehaviour(36, 25, TileBehavior.Puddle);
        m.SetBehaviour(41, 25, TileBehavior.Puddle);
        m.AddProp(PropType.Boulder, 43, 30);

        // A lawn with tall grass, fenced by ledges: hopped out of southward, westward and eastward, entered from the north
        Fill(38, 14, 42, 17, TileType.TallGrass);
        Fill(37, 14, 37, 17, TileType.LedgeLeft);
        Fill(43, 14, 43, 17, TileType.LedgeRight);
        Fill(38, 18, 42, 18, TileType.LedgeDown);
        m.Signboards[(35, 15)] = "Terrain Lab\nEvery kind of ground in one place.";
        m.SetGroundTile(35, 15, TileType.Signpost, isSolid: true);

        // The obstacles that field moves clear, in a row along the path: objects of the map (plan 02 · S2)
        m.AddObstacle(PropType.CutTree, 28, 20);
        m.AddObstacle(PropType.CrackedRock, 30, 20);
        m.AddObstacle(PropType.StrengthBoulder, 32, 20);
        return m;
    }
}
