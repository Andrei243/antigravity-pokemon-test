partial class Harness
{
    // ---------------------------------------------------------------- the DexNav's rustle (plan 10 · F12)

    // The last of `life` (LIFE_PART=rustle runs it alone): on Route 202 a find put hiding in the grass by the DexNav, its
    // tile parting and its shiver marks up (l70_rustle), the player creeping up on it with the run button held
    // (l71_sneaking_up), and a plain walk within three tiles scaring it out of the grass (l72_rustle_scared). The player
    // keeps to ground that is no tall grass, so no wild Pokémon is met on the way; the find is taken away after.
    void LifeRustle()
    {
        var sinnoh = MapDatabase.Get("Sinnoh");
        var walker = game.Player;
        bool Open(int x, int y) => sinnoh.InBounds(x, y) && sinnoh.IsWalkable(x, y) && !sinnoh.IsTallGrass(x, y);
        // A row of five open tiles running east, and tall grass six east of its first (or a row up)
        (int X, int Y, int GrassX, int GrassY)? spot = null;
        for (int reach = 0; reach < 40 && spot == null; reach++)
            for (int dy = -reach; dy <= reach && spot == null; dy++)
                for (int dx = -reach; dx <= reach && spot == null; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != reach) continue;
                    int x = 174 + dx, y = 815 + dy;
                    if (!Enumerable.Range(0, 5).All(i => Open(x + i, y))) continue;
                    foreach (int gy in new[] { y, y - 1 })
                        if (sinnoh.IsTallGrass(x + 6, gy) && sinnoh.IsWalkable(x + 6, gy) && spot == null) spot = (x, y, x + 6, gy);
                }
        if (spot is not { } s)
        {
            Console.WriteLine("!! rustle: no row of open ground beside tall grass near Route 202");
            return;
        }
        At("Sinnoh", s.X, s.Y, Direction.Right);
        walker.SetMode(TravelMode.OnFoot);
        engine.Steering = (null, false);
        Frames(20);
        if (!game.PlaceDexNavRustle(s.GrassX, s.GrassY))
        {
            Console.WriteLine($"!! rustle: nothing lives at {s.GrassX},{s.GrassY}");
            return;
        }
        Console.WriteLine($"rustle: {game.DexNavRustle!.Entry.SpeciesName} hiding at {s.GrassX},{s.GrassY}, the player at {s.X},{s.Y}");
        var life = game.World.Life;
        // A shot just after the rustle stirs: its shiver marks are whole for 0.18 s
        void AfterAStir()
        {
            int before = life.Count;
            for (int guard = 0; guard < 120 && life.Count <= before; guard++) Frames(1);
            Frames(3);
        }
        Frames(40);
        AfterAStir();
        Shot("l70_rustle");
        ShotCrop("l70b_rustle_native", 920, 300, 640, 360, 2);

        // Creeping up: the run button held, three tiles at the sneak's pace, shot in the middle of the third
        int fromX = walker.GridX;
        engine.Steering = (Direction.Right, true);
        for (int guard = 0; guard < 600 && walker.GridX < fromX + 2; guard++) Frames(1);
        for (int guard = 0; guard < 30 && !walker.IsMoving; guard++) Frames(1);
        Frames(4);
        Console.WriteLine($"l71: sneaking {walker.IsSneaking}, at {walker.GridX},{walker.GridY}, the find {(game.DexNavRustle != null ? "still there" : "!! gone")}");
        Shot("l71_sneaking_up");
        for (int guard = 0; guard < 120 && walker.IsMoving; guard++) Frames(1);
        engine.Steering = (null, false);
        Frames(4);
        Console.WriteLine($"  after the creep: at {walker.GridX},{walker.GridY}, {Math.Abs(walker.GridX - s.GrassX)} tiles off, the find {(game.DexNavRustle != null ? "still there" : "!! gone")}");

        // A plain walk one tile on, within three: it bursts out of the grass
        fromX = walker.GridX;
        engine.Steering = (Direction.Right, false);
        for (int guard = 0; guard < 120 && walker.GridX == fromX; guard++) Frames(1);
        engine.Steering = (null, false);
        for (int guard = 0; guard < 60 && walker.IsMoving; guard++) Frames(1);
        Frames(4);
        Console.WriteLine($"l72: the find {(game.DexNavRustle == null ? "scared off" : "!! still there")}, the chain {game.DexNav.Chain}");
        Shot("l72_rustle_scared");
        ShotCrop("l72b_rustle_scared_native", 760, 300, 640, 360, 2);
        game.ForgetDexNavRustle();
        Frames(4);
    }
}
