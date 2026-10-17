partial class Harness
{
    // ---------------------------------------------------------------- the imported world (plan 01 · M2)
    public void AreaMode()
    {
        string key = args.Length > 2 ? args[2] : "twinleaf_town";
        if (key.Contains(':')) { ImportedAreaMode(key); return; }
        var (areaMap, ax, ay) = AreaSpot(key);
        At(areaMap.Name, ax, ay, Direction.Down); Frames(2); Shot("area_" + key);
        Console.WriteLine($"{key}: {areaMap.Name} ({ax}, {ay})");
    }

    // ---------------------------------------------------------------- a GBA region's import (plan 18 · W7)

    // `area kanto:pallet_town` reads tools/MapImporter/out/kanto/world (the importer's `--game firered`: run it first),
    // as `cities` reads Sinnoh's: the map the area lies on is built from it, a shot is taken from the area's open middle,
    // and then one inside each room a door of the area leads into, from the tile its door puts the player on.
    void ImportedAreaMode(string spec)
    {
        string region = spec[..spec.IndexOf(':')], key = spec[(spec.IndexOf(':') + 1)..];
        string? repo = null;
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null && repo == null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "PokemonPlatinum.sln"))) repo = dir.FullName;
        string import = Environment.GetEnvironmentVariable("SHOTS_IMPORT") ?? Path.Combine(repo ?? startDir, "tools", "MapImporter", "out", region, "world");
        if (!File.Exists(Path.Combine(import, World.IndexFile)))
        {
            Console.WriteLine($"No import at {import}: run `dotnet run --project tools/MapImporter -- --game <game>` first.");
            return;
        }
        var index = GameDataFiles.Deserialize<WorldIndexFile>(File.ReadAllText(Path.Combine(import, World.IndexFile)));
        var world = World.Open(import, index);
        var built = new Dictionary<string, Map>();
        Map MapNamed(string name) => built.TryGetValue(name, out var m) ? m
            : built[name] = WorldMapBuilder.Build(world, index.Maps.First(e => e.Name == name));
        if (world.MapOf(key) is not { } entry) { Console.WriteLine($"{key}: no area of {region}'s import"); return; }
        var map = MapNamed(entry.Name);

        void Look(string shot, Map on, int x, int y, Direction facing)
        {
            game.Map = on;
            game.Player.SetPosition(Math.Clamp(x, 0, on.Width - 1), Math.Clamp(y, 0, on.Height - 1), facing);
            game.State = GameState.Overworld;
            Frames(3);
            Shot(shot);
        }

        var b = map.AreaBounds(key)!.Value;
        var area = map.FindArea(key);
        var open = new List<(int X, int Y)>();
        for (int y = b.Y; y < b.Y + b.Height; y++)
            for (int x = b.X; x < b.X + b.Width; x++)
                if (map.AreaAt(x, y) == area && map.IsWalkable(x, y)) open.Add((x, y));
        double cx = open.Average(t => t.X), cy = open.Average(t => t.Y);
        var (sx, sy) = open.OrderBy(t => (t.X - cx) * (t.X - cx) + (t.Y - cy) * (t.Y - cy)).First();
        Look($"area_{region}_{key}", map, sx, sy, Direction.Down);
        Console.WriteLine($"{key}: {map.Name} ({sx}, {sy}), {map.PlacedBuildings?.Count ?? 0} buildings found at their doors");
        foreach (var house in map.PlacedBuildings ?? new())
            if (map.AreaAt(house.X0, house.Y1) == area)
                Console.WriteLine($"  {house.Kind} {house.X0},{house.Y0} to {house.X1},{house.Y1}, doors {string.Join(", ", house.Doors.Select(d => $"{d.X} to {d.Target}"))}");

        int n = 0;
        foreach (var warp in map.Warps.Where(w => map.AreaAt(w.SourceX, w.SourceY) == area).OrderBy(w => w.SourceY).ThenBy(w => w.SourceX))
        {
            if (!index.Maps.Any(e => e.Name == warp.TargetMap && e.Setting == MapSetting.Indoors)) continue;
            var room = MapNamed(warp.TargetMap);
            n++;
            // Before the house's door, then in the room on the tile the door puts the player on
            Look($"area_{region}_{key}_door_{n}", map, warp.SourceX, warp.SourceY + 3, Direction.Up);
            Look($"area_{region}_{key}_room_{n}", room, warp.TargetX, warp.TargetY, warp.TargetFacing);
            Console.WriteLine($"  door {n} ({warp.SourceX}, {warp.SourceY}) into {room.Name} ({warp.TargetX}, {warp.TargetY}), {room.Width}×{room.Height}, "
                + $"floor {Enumerable.Range(0, room.Width * room.Height).Count(i => !room.IsSolid(i % room.Width, i / room.Width))} tiles");
        }
    }
}
