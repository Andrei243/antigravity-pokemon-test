using System.Globalization;
using PokemonPlatinumEngine.OpenWorld;

namespace OpenWorldBake;

/// <summary>Where a region's descriptions are: the game's <c>Data/world3d/&lt;region&gt;</c>, or the baker's own test worlds.</summary>
public static class RegionFolders
{
    /// <summary>The repository's folder (the one with PokemonPlatinum.sln), found upward from here or from the tool, or null.</summary>
    public static string? Repository()
    {
        foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, "PokemonPlatinum.sln"))) return dir.FullName;
        return null;
    }

    /// <summary>A region's description folder: <c>PokemonPlatinumEngine/Data/world3d/&lt;region&gt;</c>, else <c>tools/OpenWorldBake/worlds/&lt;region&gt;</c>.</summary>
    public static string? Find(string region)
    {
        if (Repository() is not { } root) return null;
        foreach (string candidate in new[]
                 {
                     Path.Combine(root, "PokemonPlatinumEngine", "Data", "world3d", region),
                     Path.Combine(root, "tools", "OpenWorldBake", "worlds", region)
                 })
            if (File.Exists(Path.Combine(candidate, "world.json"))) return candidate;
        return null;
    }
}

/// <summary>
/// The baker's command for a region (plan 32 · OW2):
/// <c>&lt;region&gt; [--world &lt;folder&gt;] [--out &lt;baked folder&gt;] [--chunk x,z] [--check] [--seams]
/// [--plan &lt;out dir&gt; [area]] [--section &lt;out dir&gt; [area] x1,z1 x2,z2 ...]</c>. It prints counts, never timings.
/// </summary>
public static class RegionCommand
{
    public static int Run(string[] args)
    {
        string region = args[0];
        string? folder = null, outFolder = null, planDir = null, planArea = null, sectionDir = null, sectionArea = null;
        (int X, int Z)? onlyChunk = null;
        bool check = false, seams = false, swim = false;
        string? walkDir = null, navDir = null, navArea = null;
        var navPaths = new List<((double X, double? Y, double Z) From, (double X, double? Y, double Z) To)>();
        var walkPoints = new List<(double X, double Z, bool Run)>();
        var cuts = new List<((double, double) A, (double, double) B)>();
        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--world": folder = args[++i]; break;
                case "--out": outFolder = args[++i]; break;
                case "--check": check = true; break;
                case "--seams": seams = true; break;
                case "--chunk":
                    var c = args[++i].Split(',');
                    onlyChunk = (int.Parse(c[0], CultureInfo.InvariantCulture), int.Parse(c[1], CultureInfo.InvariantCulture));
                    break;
                case "--plan":
                    planDir = args[++i];
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--")) planArea = args[++i];
                    break;
                case "--section":
                    sectionDir = args[++i];
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--") && !args[i + 1].Contains(',')) sectionArea = args[++i];
                    while (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                    {
                        var a = Point(args[++i]);
                        if (i + 1 >= args.Length || args[i + 1].StartsWith("--")) { Console.Error.WriteLine("--section: a cut needs two points"); return 2; }
                        cuts.Add((a, Point(args[++i])));
                    }
                    break;
                case "--walk":
                    walkDir = args[++i];
                    while (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                    {
                        string p = args[++i];
                        bool running = p.EndsWith('!');
                        var (x, z) = Point(running ? p[..^1] : p);
                        walkPoints.Add((x, z, running));
                    }
                    if (walkPoints.Count < 2) { Console.Error.WriteLine("--walk: a start and at least one stop, as x,z (a stop ending in ! is run to)"); return 2; }
                    break;
                case "--swim": swim = true; break;
                case "--nav":
                    navDir = args[++i];
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--") && (!args[i + 1].Contains(',') || args[i + 1].StartsWith("box:"))) navArea = args[++i];
                    while (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                    {
                        var a = Place3(args[++i]);
                        if (i + 1 >= args.Length || args[i + 1].StartsWith("--")) { Console.Error.WriteLine("--nav: a path needs two places"); return 2; }
                        navPaths.Add((a, Place3(args[++i])));
                    }
                    break;
                default: Console.Error.WriteLine($"unknown argument {args[i]}"); return 2;
            }
        }
        folder ??= RegionFolders.Find(region);
        if (folder == null) { Console.Error.WriteLine($"no region {region}: no world.json in PokemonPlatinumEngine/Data/world3d/{region} or tools/OpenWorldBake/worlds/{region}"); return 2; }
        string baked = outFolder ?? Path.Combine(folder, "baked");

        RegionDescription description;
        try { description = RegionDescription.Read(folder); }
        catch (InvalidDataException e) { Console.Error.WriteLine(e.Message); return 1; }
        if (description.World.Region != region && args[0] != description.World.Region)
            Console.WriteLine($"(the folder's world.json names the region {description.World.Region})");

        Func<int, int, bool>? only = onlyChunk is { } oc ? (x, z) => x == oc.X && z == oc.Z : null;
        if (onlyChunk is { } wanted && !RegionBake.ChunksOf(description.World).Contains(wanted))
        {
            Console.Error.WriteLine($"chunk {wanted.X},{wanted.Z} is not the region's"); return 2;
        }

        if (navDir != null && only != null) { Console.Error.WriteLine("--nav: the navigation mesh is the whole region's, not a chunk's"); return 2; }
        var navReport = new NavReport();
        var files = RegionBake.Files(description, only, navReport);
        if (check)
        {
            var differences = RegionBake.Differences(baked, files, partial: only != null);
            foreach (string line in differences) Console.WriteLine(line);
            Console.WriteLine(differences.Count == 0
                ? $"{files.Count} files baked again from {folder}: the same to the byte as {baked}"
                : $"{differences.Count} of {files.Count} files differ: run the baker without --check");
            return differences.Count == 0 ? 0 : 1;
        }

        RegionBake.Write(baked, files, partial: only != null);
        var land = description.Land();
        var chunks = files.Where(f => f.Key.EndsWith(".chunk")).Select(f => PokemonPlatinumEngine.OpenWorld.BakedChunk.Read(new MemoryStream(f.Value))).ToList();
        for (int level = 0; level < RegionBake.Levels; level++)
        {
            var at = chunks.Where(c => c.Level == level).ToList();
            if (at.Count == 0) continue;
            Console.WriteLine($"level {level} ({(level == 0 ? $"{RegionBake.Cell} m cells" : $"thinned to {RegionBake.Thinned[level - 1].MaxError} m")}): {at.Count} chunks, "
                + $"{at.Sum(c => c.VertexCount)} vertices, {at.Sum(c => c.GroundTriangles)} ground triangles, {at.Sum(c => c.ApronTriangles)} in aprons and {at.Sum(c => c.SkirtTriangles)} in skirts; "
                + $"the busiest chunk {at.Max(c => c.TriangleCount)} triangles; {at.Sum(c => files[PokemonPlatinumEngine.OpenWorld.BakedChunk.FileName(c.ChunkX, c.ChunkZ, c.Level)].Length):N0} bytes");
        }
        Console.WriteLine($"water over {chunks.Where(c => c.Level == 0).Sum(c => c.Water.Count(w => w != PokemonPlatinumEngine.OpenWorld.BakedChunk.NoWater))} cells of 2 m; wrote {files.Count} files under {baked}");
        if (files.TryGetValue(OpenNavMesh.FileName, out var navBytes))
            Console.WriteLine($"navigation ({OpenNavMesh.FileName}, {navBytes.Length:N0} bytes): {navReport}");

        var wantedChunks = RegionBake.ChunksOf(description.World).Where(c => only == null || only(c.X, c.Z)).ToList();
        Dictionary<(int, int), ChunkMesh[]>? meshes = null;
        Dictionary<(int, int), ChunkMesh[]> Meshes() => meshes ??= wantedChunks.ToDictionary(c => c, c => RegionBake.Meshes(land, description.World, c.X, c.Z));

        if (seams) Seams(Meshes());
        if (planDir != null)
        {
            var box = BoxOf(description, planArea);
            if (box == null) { Console.Error.WriteLine($"no area {planArea}"); return 2; }
            var all = Meshes();
            var finest = RegionPictures.Surface(all.Values.Select(m => m[0]));
            string name = planArea ?? description.World.Region;
            // About a thousand pixels across, from 2 to 16 a metre
            int perMetre = Math.Clamp((int)(1024 / (box.Value.X1 - box.Value.X0)), 2, 16);
            Pictures.Save(RegionPictures.Plan(description, land, finest, box.Value, perMetre: perMetre), Path.Combine(planDir, $"plan_{name}.png"));
            Console.WriteLine($"wrote {Path.Combine(planDir, $"plan_{name}.png")} ({perMetre} pixels a metre)");
            // ...and with its places laid over it (plan 32 · OW5), where the region has any
            if (Directory.Exists(Path.Combine(description.Folder, PokemonPlatinumEngine.OpenWorld.OpenPlaceFiles.Folder)))
            {
                var placed = RegionPictures.Plan(description, land, finest, box.Value, perMetre: perMetre);
                var placesKey = PlacePictures.Over(placed, PokemonPlatinumEngine.OpenWorld.OpenPlaceFiles.LoadRegion(description.Folder, withLand: false), box.Value, perMetre);
                string placesPath = Path.Combine(planDir, $"plan_{name}_places.png");
                Pictures.Save(placed, placesPath);
                Console.WriteLine($"wrote {placesPath}; its key:");
                foreach (string line in placesKey) Console.WriteLine("    " + line);
            }
            for (int level = 1; level < RegionBake.Levels; level++)
            {
                string path = Path.Combine(planDir, $"plan_{name}_level{level}.png");
                Pictures.Save(RegionPictures.Plan(description, land, finest, box.Value, all.Values.Select(m => m[level]).ToArray(), perMetre), path);
                Console.WriteLine($"wrote {path}");
            }
            Console.WriteLine("  key: the top of level 0 shaded by its slope and coloured by its cover; violet where a walkable floor lies under the top; blue where water stands; brown the roads' lines; dark lines a level's triangles; the areas' outlines:");
            foreach (string line in RegionPictures.AreaKey(description)) Console.WriteLine("    " + line);
        }
        if (sectionDir != null)
        {
            if (cuts.Count == 0)
            {
                var box = BoxOf(description, sectionArea);
                if (box == null) { Console.Error.WriteLine($"no area {sectionArea}"); return 2; }
                var (x0, z0, x1, z1) = box.Value;
                cuts.Add((((x0 + x1) / 2 - (x1 - x0) / 2, (z0 + z1) / 2), ((x0 + x1) / 2 + (x1 - x0) / 2, (z0 + z1) / 2)));
                cuts.Add((((x0 + x1) / 2, z0), ((x0 + x1) / 2, z1)));
            }
            var all = Meshes().Values.SelectMany(m => m).ToList();
            foreach (var (a, b) in cuts)
            {
                // From under the lowest surface along the cut to over its highest (a cave's floor included, whatever the area's heights)
                double lo = double.MaxValue, hi = double.MinValue;
                for (int s = 0; s <= 200; s++)
                {
                    double t = s / 200.0, x = a.Item1 + (b.Item1 - a.Item1) * t, z = a.Item2 + (b.Item2 - a.Item2) * t;
                    double h = land.Height(x, z);
                    lo = Math.Min(lo, h); hi = Math.Max(hi, h);
                    foreach (var c in land.Carves.Concat(land.Additions))
                        if (x >= c.Min.X && x <= c.Max.X && z >= c.Min.Z && z <= c.Max.Z) { lo = Math.Min(lo, c.Min.Y); hi = Math.Max(hi, c.Max.Y); }
                }
                lo = Math.Floor(lo - 3);
                hi = Math.Ceiling(hi + 4);
                string name = $"section_{a.Item1:0}_{a.Item2:0}_{b.Item1:0}_{b.Item2:0}".Replace('-', 'm');
                string path = Path.Combine(sectionDir, name + ".png");
                Pictures.Save(RegionPictures.Section(land, all, a, b, lo, hi), path);
                Console.WriteLine($"wrote {path} ({lo:0} m to {hi:0} m)");
            }
            Console.WriteLine("  key: rock grey, topsoil green, air pale with a line every 5 m; water's surface blue; where each level's surface crosses the cut, level 0 black, level 1 orange a pixel lower, level 2 magenta two lower");
        }
        if (walkDir != null) Walk(description, land, Meshes(), walkDir, walkPoints, swim);
        if (navDir != null)
        {
            var box = BoxOf(description, navArea);
            if (box == null) { Console.Error.WriteLine($"no area {navArea}"); return 2; }
            Nav(description, land, Meshes(), OpenNavMesh.Read(new MemoryStream(files[OpenNavMesh.FileName])), navDir, navArea, box.Value, navPaths, swim);
        }
        return 0;
    }

    /// <summary>
    /// <c>--nav</c> (plan 32 · OW4): the navigation mesh over the region's plan (or an area's), and the paths between the
    /// pairs of places given (each <c>x,z</c> on the highest ground there, or <c>x,y,z</c> on the ground nearest y), each
    /// printed with its length, corners and what it crosses.
    /// </summary>
    static void Nav(RegionDescription description, TerrainWorld land, Dictionary<(int, int), ChunkMesh[]> meshes, OpenNavMesh mesh, string dir, string? area,
        (double X0, double Z0, double X1, double Z1) box, List<((double X, double? Y, double Z) From, (double X, double? Y, double Z) To)> pairs, bool swim)
    {
        var ground = CapsuleRoute.GroundOf(description, land);
        OpenPos? At((double X, double? Y, double Z) p) =>
            ground.Footing(p.X, p.Z, (p.Y ?? 1000) + 0.5, p.Y is { } y ? y - 2 : -1000) is { } f ? OpenPos.FromMetres(p.X, f.Y, p.Z) : null;
        var allowed = OpenNavMesh.Allowed(swim ? PokemonPlatinumEngine.OpenWorld.MovementGrants.Swim : PokemonPlatinumEngine.OpenWorld.MovementGrants.None);
        var paths = new List<NavPath>();
        foreach (var (from, to) in pairs)
        {
            if (At(from) is not { } a || At(to) is not { } b) { Console.WriteLine($"  no ground at {from} or {to}"); continue; }
            var path = NavPath.Find(mesh, a, b, allowed);
            if (path == null) { Console.WriteLine($"  no way from {a} to {b}"); continue; }
            paths.Add(path);
            Console.WriteLine($"  from {a} to {b}: {path.Length:0.0} m, {path.Polygons.Count} polygons ({path.Visited} closed), crossing {path.Uses}: "
                + string.Join(" ", path.Corners.Select(c => FormattableString.Invariant($"({c.MetresX:0.0},{c.MetresY:0.0},{c.MetresZ:0.0})"))));
        }
        int perMetre = Math.Clamp((int)(1024 / (box.X1 - box.X0)), 2, 16);
        var finest = RegionPictures.Surface(meshes.Values.Select(m => m[0]));
        var plan = RegionPictures.Plan(description, land, finest, box, perMetre: perMetre);
        NavPictures.Over(plan, mesh, ground, box, perMetre, paths);
        string path0 = Path.Combine(dir, $"nav_{(area ?? description.World.Region).Replace(':', '_').Replace(',', '_').Replace('-', 'm')}.png");
        Pictures.Save(plan, path0);
        Console.WriteLine($"wrote {path0} ({perMetre} pixels a metre)");
        Console.WriteLine("  key: polygons outlined dark on the top, violet under other ground; swim polygons washed cyan; shared edges green; a step off the mesh yellow, a drop red (to a dot where it lands); paths white, their corners dotted");
    }

    static (double X, double? Y, double Z) Place3(string text)
    {
        var parts = text.Split(',').Select(p => double.Parse(p, CultureInfo.InvariantCulture)).ToArray();
        return parts.Length == 3 ? (parts[0], parts[1], parts[2]) : (parts[0], null, parts[1]);
    }

    /// <summary>
    /// <c>--walk</c> (plan 32 · OW3): the game's capsule walked from the first point through the rest on level 0's
    /// ground, what happened printed, and the walk drawn over the region's plan and as a profile.
    /// </summary>
    static void Walk(RegionDescription description, TerrainWorld land, Dictionary<(int, int), ChunkMesh[]> meshes, string dir,
        List<(double X, double Z, bool Run)> points, bool swim)
    {
        var ground = CapsuleRoute.GroundOf(description, land);
        var (sx, sz, _) = points[0];
        var foot = ground.Footing(sx, sz, 1000, -1000);
        if (foot == null) { Console.Error.WriteLine($"--walk: no ground at {sx},{sz}"); return; }
        var start = OpenPos.FromMetres(sx, foot.Value.Y, sz);
        var stops = points.Skip(1).Select((p, i) => new RouteStop(p.X, p.Z, $"stop {i + 1} ({p.X:0.#}, {p.Z:0.#})", p.Run)).ToList();
        var walk = CapsuleRoute.Walk(ground, start, stops, swim ? PokemonPlatinumEngine.OpenWorld.MovementGrants.Swim : PokemonPlatinumEngine.OpenWorld.MovementGrants.None);
        Console.WriteLine($"walked from {start}: {walk.Samples.Count} ticks, ended {walk.End}; {(walk.Finished ? "finished" : walk.Stuck ? "stuck" : "out of time")}; "
            + $"{walk.Reached.Count} stops reached, {walk.GivenUp.Count} given up; {walk.PushTicks} ticks pushed off walls, {walk.SlideTicks} sliding, {walk.SwimTicks} swimming, "
            + $"{walk.HardLandings} landings; feet from {walk.LowestY:0.000} m to {walk.HighestY:0.000} m");
        foreach (string e in walk.Events) Console.WriteLine("  " + e);
        var box = BoxOf(description, null)!.Value;
        int perMetre = Math.Clamp((int)(1024 / (box.X1 - box.X0)), 2, 16);
        var finest = RegionPictures.Surface(meshes.Values.Select(m => m[0]));
        var plan = RegionPictures.Plan(description, land, finest, box, perMetre: perMetre);
        WalkPictures.Over(plan, walk, stops, box, perMetre);
        string planPath = Path.Combine(dir, "walk_plan.png"), profilePath = Path.Combine(dir, "walk_profile.png");
        Pictures.Save(plan, planPath);
        Pictures.Save(WalkPictures.Profile(walk, ground), profilePath);
        Console.WriteLine($"wrote {planPath} and {profilePath}");
        Console.WriteLine("  key: the feet white standing, orange sliding, red falling, cyan swimming; a stop reached ringed yellow, given up magenta; the profile's grey every surface under the feet (the top darkest), a line every 5 m");
    }

    static (double, double) Point(string text)
    {
        var parts = text.Split(',');
        return (double.Parse(parts[0], CultureInfo.InvariantCulture), double.Parse(parts[1], CultureInfo.InvariantCulture));
    }

    /// <summary>An area's box on the ground, or the region's whole extent for none; null for an area it doesn't have.</summary>
    static (double X0, double Z0, double X1, double Z1)? BoxOf(RegionDescription description, string? area)
    {
        var w = description.World;
        if (area == null) return (w.Chunks[0] * 32.0, w.Chunks[1] * 32.0, (w.Chunks[2] + 1) * 32.0, (w.Chunks[3] + 1) * 32.0);
        if (area.StartsWith("box:"))
        {
            // A rectangle of the ground by hand: box:x0,z0,x1,z1
            var c = area[4..].Split(',').Select(p => double.Parse(p, CultureInfo.InvariantCulture)).ToArray();
            return c.Length == 4 && c[2] > c[0] && c[3] > c[1] ? (c[0], c[1], c[2], c[3]) : null;
        }
        var found = w.Areas.FirstOrDefault(a => a.Key == area);
        if (found == null) return null;
        return Flat.Bounds(found.Polygon.Select(p => new P2(p[0], p[1])));
    }

    /// <summary>
    /// Where two levels meet: how far a chunk's border stands off its neighbour's surface at every other level, the
    /// worst of each pair. Thinned levels keep the finest's border rows, so every pair should be nought.
    /// </summary>
    static void Seams(Dictionary<(int, int), ChunkMesh[]> meshes)
    {
        var worst = new double[RegionBake.Levels, RegionBake.Levels];
        var surfaces = new Dictionary<(int, int, int), CollisionMesh>();
        CollisionMesh SurfaceOf(int x, int z, int level) =>
            surfaces.TryGetValue((x, z, level), out var m) ? m : surfaces[(x, z, level)] = RegionPictures.Surface(new[] { meshes[(x, z)][level] }, apron: true);
        int measured = 0;
        foreach (var ((x, z), levels) in meshes)
            foreach (var chunk in levels)
            {
                // The vertices of the ground's border edges that lie in a neighbour's columns (the row shared east and south)
                var edges = new Dictionary<long, int>();
                for (int t = 0; t < chunk.GroundTriangles; t++)
                    for (int e = 0; e < 3; e++)
                    {
                        int a = chunk.Indices[t * 3 + e], b = chunk.Indices[t * 3 + (e + 1) % 3];
                        long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                        edges[key] = edges.GetValueOrDefault(key) + 1;
                    }
                var border = edges.Where(e => e.Value == 1).SelectMany(e => new[] { (int)(e.Key >> 32), (int)(e.Key & 0xFFFFFFFF) }).Distinct();
                foreach (int v in border)
                {
                    var p = chunk.Vertex(v);
                    int nx = (int)Math.Floor(p.X / 32), nz = (int)Math.Floor(p.Z / 32);
                    if (nx == x && nz == z || !meshes.ContainsKey((nx, nz))) continue;
                    for (int other = 0; other < RegionBake.Levels; other++)
                    {
                        var column = SurfaceOf(nx, nz, other).Column(p.X, p.Z);
                        if (column.Count == 0) continue;
                        double gap = column.Min(c => Math.Abs(c.Y - p.Y));
                        worst[chunk.Lod, other] = Math.Max(worst[chunk.Lod, other], gap);
                        measured++;
                    }
                }
            }
        Console.WriteLine($"seams: {measured} border vertices measured against their neighbours' levels");
        for (int a = 0; a < RegionBake.Levels; a++)
            for (int b = 0; b < RegionBake.Levels; b++)
                Console.WriteLine($"  a level-{a} border against level {b}: at most {worst[a, b]:0.000} m");
    }
}
