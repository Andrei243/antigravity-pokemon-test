partial class Harness
{
    // The field's rules drawn as layers (plan 16 · T18; not part of `all`): the terrain lab under each layer, Route 202's
    // trainers' sight, Jubilife City's chunks while it streams and a cave's heights in Oreburgh Gate. Each shot prints
    // what its layers drew, by layer, and a few of their labels.
    public void LayersMode()
    {
        void Tell(string name)
        {
            var drawn = game.World.LastLayers;
            if (drawn == null) { Console.WriteLine($"  {name}: no layer drawn"); return; }
            Console.WriteLine($"  {name}: {DebugLayers.Describe(game.World.Layers)} on {game.Map.Name} at {game.Player.GridX},{game.Player.GridY}: "
                + $"{drawn.Quads.Count} tiles, {drawn.Lines.Count} lines, {drawn.Labels.Count} labels");
            foreach (var group in drawn.Quads.GroupBy(q => q.Layer))
                Console.WriteLine($"    {group.Key}: {group.Count()} tiles");
            foreach (var label in drawn.Labels.Take(12))
                Console.WriteLine($"    {label.Layer} at {label.X - 0.5f},{label.Z - 0.5f}: {label.Text}");
        }

        // The terrain lab under each layer by itself, then all of them at once
        var lab = BuildTerrainLab();
        var hiker = game.Player;
        foreach (var (name, layer) in DebugLayers.Names.Append(("all", DebugLayer.All)))
        {
            game.World.Layers = layer;
            game.Map = lab;
            hiker.SetPosition(20, 17, Direction.Up);
            hiker.SetMode(TravelMode.OnFoot);
            game.State = GameState.Overworld;
            Frames(3);
            Shot($"lay_lab_{name}");
            Tell($"lay_lab_{name}");
        }

        // Route 202's trainers, and where each looks
        game.World.Layers = DebugLayer.Sight;
        At("Sinnoh", 174, 815, Direction.Up);
        Frames(2);
        Shot("lay_route202_sight");
        Tell("lay_route202_sight");
        foreach (var npc in game.Map.NPCs.Near(174, 815, 24))
            if (npc.IsTrainer && npc.TrainerData != null)
                Console.WriteLine($"    {npc.TrainerData.Id} at {npc.GridX},{npc.GridY} facing {npc.Facing}, sight {npc.TrainerData.SightRange}, battled {npc.HasBattled}");

        // Jubilife City's chunks while it streams: every scene let go, then the city come to and shot at once (what the
        // frame waited for is red, what is still being made ready yellow), and again a second on
        game.World.Layers = DebugLayer.Chunks;
        game.World.ForgetAll();
        game.World.ResetStreamingStats();
        At("Sinnoh", 175, 760, Direction.Down);
        Shot("lay_jubilife_chunks");
        Tell("lay_jubilife_chunks");
        Console.WriteLine($"    streaming: {game.World.Streaming}");
        Frames(60);
        Shot("lay_jubilife_chunks_ready");
        Tell("lay_jubilife_chunks_ready");

        // A cave's heights: Oreburgh Gate's first floor from the open tile nearest its middle, the walked height on each
        // tile and the drawn one beside it where they differ
        game.World.Layers = DebugLayer.Height;
        var gate = MapDatabase.Get("OreburghGate1F");
        var (gx, gy) = MiddleOf(gate);
        At(gate.Name, gx, gy, Direction.Down);
        Frames(2);
        Shot("lay_cave_heights");
        Tell("lay_cave_heights");
        game.World.Layers = DebugLayer.Solid | DebugLayer.Warps | DebugLayer.Encounters;
        Frames(2);
        Shot("lay_cave_solid_warps");
        Tell("lay_cave_solid_warps");

        // Back to what SHOTS_LAYERS asked for
        game.World.Layers = ShotLayers;
        Frames(2);
    }

    // The open tile of a map nearest the middle of its open ground
    static (int X, int Y) MiddleOf(Map map)
    {
        var open = new List<(int X, int Y)>();
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                if (map.IsWalkable(x, y)) open.Add((x, y));
        if (open.Count == 0) throw new InvalidOperationException($"{map.Name} has no open ground");
        double cx = open.Average(t => t.X), cy = open.Average(t => t.Y);
        return open.OrderBy(t => (t.X - cx) * (t.X - cx) + (t.Y - cy) * (t.Y - cy)).First();
    }
}
