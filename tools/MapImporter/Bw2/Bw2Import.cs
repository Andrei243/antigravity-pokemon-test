using System.Globalization;
using System.Text;
using DataImporter;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace MapImporter.Bw2;

/// <summary>
/// <c>--game black2</c> (plan 22 · U1): Black 2 read into <c>out/unova/</c>: the overworld (matrix 0) by cover, by
/// behaviour and by height, a picture of every zone (not with <c>--quick</c>) and <c>report.md</c> with the classes,
/// textures and props it counted; and Black 2's section of <c>docs/tile-behaviours.md</c>. No world file is written
/// yet: <c>--data</c> and the areas it opens are U2's.
/// </summary>
public static class Bw2Import
{
    public const string Region = "unova";
    public const string SectionTitle = "## Black 2's behaviours";

    public static string Fetch(string repo) =>
        Sources.Checkout(Path.Combine(repo, "tools", "MapImporter", ".cache"), "pokebw2", Sources.Bw2Repo, Sources.Bw2Commit, Sources.Bw2Folders);

    /// <summary>Every tile of a matrix laid out, with the class, behaviour, blocked flag, look, height (in tiles) and zone of each.</summary>
    public sealed record TileGrid(int Width, int Height, int[] Class, byte[] Behaviour, bool[] Solid, TerrainCover[] Cover, float[] Ground, string?[] Zone);

    public static int Run(string repo, string? dir, string outDir, bool quick)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        Console.WriteLine("Fetching fuddlesworth/pokebw2…");
        var maps = new Bw2Maps(dir ?? Fetch(repo));
        var over = maps.Matrix(Bw2Maps.Overworld);
        int filled = over.FillZones();
        Console.WriteLine($"Read {maps.Zones.Count} zones, {maps.Matrices.Count} matrices and {maps.Chunks.Count} chunks; "
            + $"the overworld is {over.Width} × {over.Height} chunks, {filled} of its zone cells filled from a neighbour ({clock.Elapsed.TotalSeconds:F1} s)");

        Directory.CreateDirectory(outDir);
        var unsorted = new Dictionary<string, int>(StringComparer.Ordinal);
        var tiles = Tiles(maps, over, unsorted);
        Mosaic(maps, over, tiles, 4, byCover: true).Save(Path.Combine(outDir, "unova.png"));
        Mosaic(maps, over, tiles, 4, byCover: false).Save(Path.Combine(outDir, "unova_behaviours.png"));
        HeightPicture(tiles, 2).Save(Path.Combine(outDir, "unova_heights.png"));
        int pictures = 3;
        if (!quick)
        {
            Directory.CreateDirectory(Path.Combine(outDir, "areas"));
            foreach (var (name, (_, matrix)) in maps.Matrices)
            {
                var zones = maps.ZonesOn(name).ToList();
                if (zones.Count == 0) continue;
                var t = name == Bw2Maps.Overworld ? tiles : Tiles(maps, matrix);
                foreach (var zone in zones)
                    if (AreaPicture(maps, matrix, t, zone, 8) is { } picture)
                    {
                        picture.Save(Path.Combine(outDir, "areas", zone.Key + ".png"));
                        pictures++;
                    }
            }
        }
        Console.WriteLine($"Drew {pictures} pictures ({clock.Elapsed.TotalSeconds:F1} s)");

        var census = Census.Take(maps);
        File.WriteAllText(Path.Combine(outDir, "report.md"), Report(maps, over, tiles, census, unsorted));
        string doc = Path.Combine(repo, "docs", "tile-behaviours.md");
        File.WriteAllText(doc, DocSections.Splice(File.Exists(doc) ? File.ReadAllText(doc) : "", SectionTitle, Section(census)));

        foreach (string problem in maps.Problems.Take(20)) Console.WriteLine("  " + problem);
        Console.WriteLine($"{maps.Problems.Count} problems; {census.UnknownTextures(overworld: true).Count} textures on the overworld and "
            + $"{census.UnknownTextures(overworld: false).Count} elsewhere no rule sorts; {census.Classes.Keys.Count(c => !Bw2Behaviours.Table.ContainsKey(c))} classes in no row; "
            + $"report in {Path.Combine(outDir, "report.md")} ({clock.Elapsed.TotalSeconds:F1} s)");
        return 0;
    }

    // ---------------------------------------------------------------- tiles

    public static TileGrid Tiles(Bw2Maps maps, Bw2Matrix matrix, Dictionary<string, int>? unsorted = null)
    {
        int w = matrix.Width * LandData.Tiles, h = matrix.Height * LandData.Tiles;
        var grid = new TileGrid(w, h, new int[w * h], new byte[w * h], new bool[w * h], new TerrainCover[w * h], new float[w * h], new string?[w * h]);
        Array.Fill(grid.Ground, float.NaN);
        string? only = matrix.HasZones ? null : maps.ZonesOn(matrix.Name).FirstOrDefault()?.Constant;
        for (int cy = 0; cy < matrix.Height; cy++)
            for (int cx = 0; cx < matrix.Width; cx++)
            {
                if (matrix.ModelAt(cx, cy) is not { } model) continue;
                if (maps.Terrain(Bw2Matrix.Folder(model)) is not [{ } land, ..]) continue;
                string zone = (matrix.HasZones ? matrix.ZoneAt(cx, cy) : only) ?? "";
                var look = Bw2Cover.OfChunk(maps, Bw2Matrix.Folder(model), unsorted);
                for (int z = 0; z < Math.Min(LandData.Tiles, land.Height); z++)
                    for (int x = 0; x < Math.Min(LandData.Tiles, land.Width); x++)
                    {
                        int i = (cy * LandData.Tiles + z) * w + cx * LandData.Tiles + x;
                        var tile = land.At(x, z);
                        var behaviour = Bw2Behaviours.Of(tile.Class);
                        grid.Class[i] = tile.Class;
                        grid.Behaviour[i] = (byte)behaviour;
                        grid.Solid[i] = tile.Blocked;
                        grid.Cover[i] = look.Length > 0 ? look[z * land.Width + x] : Bw2Cover.OfTile(behaviour);
                        grid.Ground[i] = tile.Height;
                        grid.Zone[i] = zone;
                    }
            }
        return grid;
    }

    private static Rgb Colour(Bw2Maps maps, TileGrid t, int i, bool byCover)
    {
        if (t.Zone[i] == null) return Renders.Nothing;
        var setting = maps.SettingOf(t.Zone[i] is { Length: > 0 } z ? maps.Zones.GetValueOrDefault(z) : null);
        // The look of a tile no behaviour tells is the plain ground's (where a texture lies isn't read: Bw2Cover)
        if (byCover && t.Cover[i] != TerrainCover.Unknown) return Renders.CoverColour(t.Cover[i], t.Solid[i]);
        return Renders.Colour(t.Behaviour[i], t.Solid[i], setting);
    }

    /// <summary>A matrix, <paramref name="scale"/> pixels a tile, each zone outlined and named.</summary>
    public static Picture Mosaic(Bw2Maps maps, Bw2Matrix matrix, TileGrid t, int scale, bool byCover)
    {
        var picture = new Picture(t.Width * scale, t.Height * scale, Renders.Nothing);
        for (int y = 0; y < t.Height; y++)
            for (int x = 0; x < t.Width; x++)
                picture.Fill(x * scale, y * scale, scale, scale, Colour(maps, t, y * t.Width + x, byCover));
        var named = new HashSet<string>(StringComparer.Ordinal);
        var ink = Rgb.Of(24, 22, 40);
        for (int cy = 0; cy < matrix.Height; cy++)
            for (int cx = 0; cx < matrix.Width; cx++)
            {
                string? id = matrix.ZoneAt(cx, cy);
                if (id == null || matrix.ModelAt(cx, cy) == null) continue;
                int px = cx * LandData.Tiles * scale, py = cy * LandData.Tiles * scale, size = LandData.Tiles * scale;
                bool Same(int x, int y) => matrix.ZoneAt(x, y) == id && matrix.ModelAt(x, y) != null;
                if (!Same(cx - 1, cy)) picture.Fill(px, py, 2, size, ink);
                if (!Same(cx + 1, cy)) picture.Fill(px + size - 2, py, 2, size, ink);
                if (!Same(cx, cy - 1)) picture.Fill(px, py, size, 2, ink);
                if (!Same(cx, cy + 1)) picture.Fill(px, py + size - 2, size, 2, ink);
                if (named.Add(id)) picture.Text(px + 4, py + 4, maps.Zones.TryGetValue(id, out var zone) ? zone.Name : id, Rgb.Of(255, 255, 255), 2, ink);
            }
        return picture;
    }

    public static Picture HeightPicture(TileGrid t, int scale)
    {
        var heights = t.Ground.Where(float.IsFinite).ToList();
        float low = heights.Count > 0 ? heights.Min() : 0, high = heights.Count > 0 ? heights.Max() : 1;
        var picture = new Picture(t.Width * scale, t.Height * scale, Renders.Nothing);
        for (int y = 0; y < t.Height; y++)
            for (int x = 0; x < t.Width; x++)
            {
                int i = y * t.Width + x;
                if (t.Zone[i] == null || !float.IsFinite(t.Ground[i])) continue;
                float v = Math.Clamp((t.Ground[i] - low) / Math.Max(1f, high - low), 0f, 1f);
                picture.Fill(x * scale, y * scale, scale, scale, Rgb.Of(40, 60, 90).Mix(Rgb.Of(250, 240, 210), v));
            }
        return picture;
    }

    /// <summary>A zone: the chunks that are its own bright, its neighbours in the box round them dim, its props marked.</summary>
    public static Picture? AreaPicture(Bw2Maps maps, Bw2Matrix matrix, TileGrid t, Bw2Zone zone, int scale)
    {
        int x0 = int.MaxValue, y0 = int.MaxValue, x1 = -1, y1 = -1;
        bool Mine(int cx, int cy) => matrix.ModelAt(cx, cy) != null && (!matrix.HasZones || matrix.ZoneAt(cx, cy) == zone.Constant);
        for (int cy = 0; cy < matrix.Height; cy++)
            for (int cx = 0; cx < matrix.Width; cx++)
                if (Mine(cx, cy)) (x0, y0, x1, y1) = (Math.Min(x0, cx), Math.Min(y0, cy), Math.Max(x1, cx), Math.Max(y1, cy));
        if (x1 < 0) return null;
        int ox = x0 * LandData.Tiles, oy = y0 * LandData.Tiles, w = (x1 - x0 + 1) * LandData.Tiles, h = (y1 - y0 + 1) * LandData.Tiles;
        var picture = new Picture(w * scale, h * scale, Renders.Nothing);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = (oy + y) * t.Width + ox + x;
                var c = Colour(maps, t, i, byCover: true);
                picture.Fill(x * scale, y * scale, scale, scale, Mine((ox + x) / LandData.Tiles, (oy + y) / LandData.Tiles) ? c : c.Darker(0.6f));
            }
        // The props, a dot each where their node stands
        for (int cy = y0; cy <= y1; cy++)
            for (int cx = x0; cx <= x1; cx++)
            {
                if (!Mine(cx, cy) || matrix.ModelAt(cx, cy) is not { } model || maps.Model(Bw2Matrix.Folder(model)) is not { } m) continue;
                float half = LandData.Tiles * LandData.TileUnits / 2f;
                foreach (var prop in m.Props)
                {
                    int tx = (int)MathF.Floor((prop.Position.X + half) / LandData.TileUnits), tz = (int)MathF.Floor((prop.Position.Z + half) / LandData.TileUnits);
                    if (tx < 0 || tz < 0 || tx >= LandData.Tiles || tz >= LandData.Tiles) continue;
                    int px = ((cx - x0) * LandData.Tiles + tx) * scale, py = ((cy - y0) * LandData.Tiles + tz) * scale;
                    picture.Fill(px + scale / 4, py + scale / 4, scale / 2, scale / 2, Rgb.Of(196, 104, 84));
                }
            }
        picture.Text(4, 4, zone.Name, Rgb.Of(255, 255, 255), 2, Rgb.Of(24, 22, 40));
        return picture;
    }

    // ---------------------------------------------------------------- counts

    /// <summary>What every chunk holds, counted once for the report and the docs' section.</summary>
    public sealed class Census
    {
        /// <summary>Each class: its tiles, those blocked, and the chunks it is on.</summary>
        public Dictionary<int, (long Tiles, long Blocked, int Chunks)> Classes { get; } = new();
        /// <summary>Each texture name: the chunks of the overworld and of everywhere else whose glTF names it.</summary>
        public Dictionary<string, (int Overworld, int Elsewhere)> Textures { get; } = new(StringComparer.Ordinal);
        /// <summary>Each prop resource placed: how many times, and on how many chunks.</summary>
        public Dictionary<int, int> Props { get; } = new();
        public int ChunksRead, Layers, TwoLayers, RawPlanes, Models, NoTerrain;
        public List<string> OddSizes { get; } = new();
        public List<string> Unnamed { get; } = new();
        public HashSet<string> Named { get; } = new(StringComparer.Ordinal);

        public List<(string Name, int Count)> UnknownTextures(bool overworld) => Textures
            .Where(kv => (overworld ? kv.Value.Overworld : kv.Value.Elsewhere) > 0 && !Known(kv.Key))
            .Select(kv => (kv.Key, overworld ? kv.Value.Overworld : kv.Value.Elsewhere))
            .OrderByDescending(x => x.Item2).ThenBy(x => x.Key, StringComparer.Ordinal).ToList();

        private static bool Known(string name) { Bw2Cover.OfTexture(name, out bool known); return known; }

        public static Census Take(Bw2Maps maps)
        {
            var census = new Census();
            var overChunks = new HashSet<string>(maps.Matrix(Bw2Maps.Overworld).Models.Select(Bw2Matrix.Folder), StringComparer.Ordinal);
            foreach (var (_, matrix) in maps.Matrices.Values)
                foreach (string model in matrix.Models)
                {
                    string folder = Bw2Matrix.Folder(model);
                    if (!maps.Chunks.Contains(folder)) census.Unnamed.Add($"{matrix.Name} names {model}, which has no folder");
                    census.Named.Add(folder);
                }
            foreach (string chunk in maps.Chunks)
            {
                census.ChunksRead++;
                var layers = maps.Terrain(chunk);
                if (layers.Length == 0) census.NoTerrain++;
                if (layers.Length > 1) census.TwoLayers++;
                foreach (var layer in layers)
                {
                    if (layer == null) continue;
                    census.Layers++;
                    if (layer.RawPlanes) census.RawPlanes++;
                    if (layer.Width != LandData.Tiles || layer.Height != LandData.Tiles) census.OddSizes.Add($"{chunk} ({layer.Width} × {layer.Height})");
                    var seen = new HashSet<int>();
                    foreach (var tile in layer.Tiles)
                    {
                        var c = census.Classes.GetValueOrDefault(tile.Class);
                        census.Classes[tile.Class] = (c.Tiles + 1, c.Blocked + (tile.Blocked ? 1 : 0), c.Chunks + (seen.Add(tile.Class) ? 1 : 0));
                    }
                }
                if (maps.Model(chunk) is not { } model) continue;
                census.Models++;
                bool overworld = overChunks.Contains(chunk);
                foreach (string texture in model.Textures)
                {
                    var t = census.Textures.GetValueOrDefault(texture);
                    census.Textures[texture] = overworld ? (t.Overworld + 1, t.Elsewhere) : (t.Overworld, t.Elsewhere + 1);
                }
                foreach (var prop in model.Props) census.Props[prop.Resource] = census.Props.GetValueOrDefault(prop.Resource) + 1;
            }
            return census;
        }
    }

    // ---------------------------------------------------------------- report

    public static string Report(Bw2Maps maps, Bw2Matrix over, TileGrid tiles, Census census, IReadOnlyDictionary<string, int>? unsortedTiles = null)
    {
        var text = new StringBuilder();
        int overZones = maps.ZonesOn(Bw2Maps.Overworld).Count();
        int overCells = over.Models.Count();
        long overTiles = tiles.Zone.LongCount(z => z != null);
        long sorted = Enumerable.Range(0, tiles.Zone.Length).LongCount(i => tiles.Zone[i] != null && tiles.Cover[i] != TerrainCover.Unknown);
        int placed = census.Props.Values.Sum();
        text.Append(CultureInfo.InvariantCulture, $"""
            # Unova (Black 2): the import

            *Written by `tools/MapImporter --game black2` (plan 22 · U1), from fuddlesworth/pokebw2 at `{Sources.Bw2Commit[..8]}` (GPL v3.0).*

            {maps.Zones.Count} zones ({overZones} on the overworld), {maps.Matrices.Count} matrices, {census.ChunksRead} chunks ({census.Named.Count} named by a matrix; the rest the seasons' and other variants), {census.Layers} terrain grids read ({census.TwoLayers} chunks with two layers, {census.RawPlanes} grids with their planes written out in full), {census.Models} glTF files read for their JSON chunk.
            The overworld (`{Bw2Maps.Overworld}`) is {over.Width} × {over.Height} chunks, {overCells} of them with a chunk; {over.ZonesFilled} of those had no zone in the matrix's grid and were given their nearest neighbour's.
            {placed:N0} props placed, of {census.Props.Count} resources ({census.Props.Keys.Count(maps.PropNames.ContainsKey)} with a name by their folder).

            **Where a texture lies** (plan 22 · U2, the user's ruling of 2026-10-20): each chunk's `model.glb` is read past its JSON chunk into its binary one, as plain numbers, only to find which texture lies over the middle of each tile (`Bw2/Bw2Geometry.cs`), and nothing of it is kept but a tile's `TerrainCover`. The rules of `Bw2/Bw2Cover.cs` sort {Percent(sorted, overTiles)} of the overworld's {overTiles:N0} tiles; the rest are drawn as plain or blocked ground.

            ## Problems

            """);
        if (unsortedTiles is { Count: > 0 })
        {
            int problemsAt = text.ToString().IndexOf("## Problems", StringComparison.Ordinal);
            var list = new StringBuilder();
            list.Append(CultureInfo.InvariantCulture, $"""
                ## Textures no rule sorts, by the overworld's tiles they lie over

                {unsortedTiles.Count} names, over {unsortedTiles.Values.Sum():N0} tiles. A region session adds their patterns to `Bw2/Bw2Cover.cs` for the areas it opens.

                | Texture | Tiles |
                |---|---:|

                """);
            foreach (var (name, count) in unsortedTiles.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).Take(120))
                list.Append(CultureInfo.InvariantCulture, $"| `{name}` | {count:N0} |\n");
            list.Append('\n');
            text.Insert(problemsAt, list.ToString());
        }
        var problems = maps.Problems.Concat(census.Unnamed).Concat(census.OddSizes.Select(c => $"{c}: a grid that isn't 32 × 32; its first 32 × 32 is drawn")).ToList();
        if (census.NoTerrain > 0) problems.Add($"{census.NoTerrain} chunk folders have no terrain grid");
        if (problems.Count == 0) text.AppendLine("None.");
        foreach (string problem in problems) text.AppendLine("- " + problem);

        text.Append("""

            ## Tile classes

            Every class on every chunk's grids (each layer counted), with what the reader makes of it. A class in no row of `Bw2/Bw2Behaviours.cs` becomes `None`: the session that opens a place using it gives it its row.

            | Class | What | Becomes | Tiles | Blocked | Chunks |
            |---|---|---|---:|---:|---:|

            """);
        foreach (var (cls, (count, blocked, chunks)) in census.Classes.OrderBy(kv => kv.Key))
        {
            var becomes = Bw2Behaviours.Of(cls, out bool known);
            string what = known ? Bw2Behaviours.Table[cls].What : "*not in the table*";
            text.Append(CultureInfo.InvariantCulture, $"| `0x{cls:X2}` | {what} | `{becomes}` (`0x{(int)becomes:X2}`) | {count:N0} | {blocked:N0} | {chunks:N0} |\n");
        }

        foreach (bool overworld in new[] { true, false })
        {
            var unknown = census.UnknownTextures(overworld);
            text.Append(CultureInfo.InvariantCulture, $"""

                ## Textures no rule sorts, {(overworld ? "on the overworld" : "in the other matrices and variants")}

                {unknown.Count} names of {census.Textures.Count(kv => (overworld ? kv.Value.Overworld : kv.Value.Elsewhere) > 0)}; the count is of chunks whose glTF names the texture. A region session adds their patterns to `Bw2/Bw2Cover.cs` for the areas it opens.

                | Texture | Chunks |
                |---|---:|

                """);
            foreach (var (name, count) in unknown.Take(150))
                text.Append(CultureInfo.InvariantCulture, $"| `{name}` | {count:N0} |\n");
        }

        text.Append("""

            ## Props

            The resources placed most often, by the name of their folder under `res/field/props`.

            | Resource | Name | Placed |
            |---:|---|---:|

            """);
        foreach (var (id, count) in census.Props.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).Take(60))
            text.Append(CultureInfo.InvariantCulture, $"| {id} | {(maps.PropNames.TryGetValue(id, out var n) ? $"`{n}`" : "*no folder*")} | {count:N0} |\n");

        text.Append("""

            ## Matrices

            | Matrix | Number | Size | Zones |
            |---|---:|---|---|

            """);
        foreach (var (name, (id, matrix)) in maps.Matrices)
        {
            var zones = maps.ZonesOn(name).Select(z => z.Key).ToList();
            text.Append(CultureInfo.InvariantCulture,
                $"| `{name}` | {(id < 0 ? "" : id.ToString(CultureInfo.InvariantCulture))} | {matrix.Width} × {matrix.Height} | {string.Join(", ", zones.Take(8))}{(zones.Count > 8 ? $" and {zones.Count - 8} more" : "")} |\n");
        }
        return text.ToString().ReplaceLineEndings("\n");
    }

    private static string Percent(long part, long whole) => whole == 0 ? "0%" : (100.0 * part / whole).ToString("F1", CultureInfo.InvariantCulture) + "%";

    // ---------------------------------------------------------------- docs/tile-behaviours.md

    /// <summary>Black 2's section of <c>docs/tile-behaviours.md</c>: each class, what it becomes, its counts; and Black 2's own values.</summary>
    public static string Section(Census census)
    {
        var text = new StringBuilder();
        text.Append(SectionTitle).Append("""


            *Written by `tools/MapImporter --game black2` (plan 22 · U1); the Sinnoh run and the GBA's keep it as it is.*

            A tile of a Black 2 chunk carries a 32-bit type: its low sixteen bits are its class, which the field code asks about class by class (`src/ov036/map_attr.c`), and the lowest of its high sixteen blocks the tile. The reader (`tools/MapImporter/Bw2/Bw2Behaviours.cs`) turns each class the code names into Platinum's value where Platinum has one that means the same, the GBA's where the GBA has one, and a value of Black 2's own from `0xB0` where neither has; a class the code asks about only for its battle background, or not at all, becomes `None` and waits for the session that opens a place using it. Height is separate: each tile names a plane (a slope and an offset in the original's two tables, or two planes for a tile cut along a diagonal), and the reader takes its height at the tile's middle.

            | Class | What | Becomes | Value | Tiles | Blocked |
            |---|---|---|---|---:|---:|

            """);
        foreach (var (cls, (count, blocked, _)) in census.Classes.OrderBy(kv => kv.Key))
        {
            var becomes = Bw2Behaviours.Of(cls, out bool known);
            string what = known ? Bw2Behaviours.Table[cls].What : "Not understood yet";
            text.Append(CultureInfo.InvariantCulture,
                $"| `0x{cls:X2}` | {what}. | `{becomes}`{(known ? "" : " (not in the table)")} | `0x{(int)becomes:X2}` | {count:N0} | {blocked:N0} |\n");
        }
        text.Append("""

            The values of Black 2's own, and what the game does with each today:

            | Value | Name | What it is | In the game |
            |---|---|---|---|

            """);
        foreach (var behaviour in OwnValues)
        {
            var (support, note) = TileBehaviors.InGame(behaviour);
            string inGame = support switch { BehaviourSupport.Ruled => "**Rule.** " + note, BehaviourSupport.Waiting => "*Not yet.* " + note, _ => note };
            text.Append(CultureInfo.InvariantCulture, $"| `0x{(int)behaviour:X2}` | `{behaviour}` | {TileBehaviors.Meaning(behaviour)} | {inGame} |\n");
        }
        return text.ToString().ReplaceLineEndings("\n");
    }

    /// <summary>The values the table makes that neither Platinum's maps nor the GBA's use: Black 2's own.</summary>
    public static IEnumerable<TileBehavior> OwnValues => Bw2Behaviours.Table.Values.Select(r => r.Becomes)
        .Where(b => (int)b is >= 0xB0 and <= 0xBF).Distinct().OrderBy(b => (int)b);
}
