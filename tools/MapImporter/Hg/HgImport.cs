using System.Globalization;
using System.Text;
using DataImporter;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace MapImporter.Hg;

/// <summary>
/// <c>--game heartgold</c> (plan 20 · J1): HeartGold read into <c>out/johto/</c>: the overworld (Johto and Kanto on
/// one grid, matrix 0) by cover, by behaviour and by height, a picture of every area, the world files with a
/// <c>world.json</c> that opens every area (not with <c>--quick</c>) and <c>report.md</c>. <c>--data</c> writes what
/// <c>Data/world/johto/world.json</c> opens, and beside it Johto's <c>trainers.json</c>, <c>phone.json</c> and
/// <c>habitats.json</c>.
/// </summary>
public static class HgImport
{
    public const string Region = "johto";

    public static string Fetch(string repo) =>
        Sources.Checkout(Path.Combine(repo, "tools", "MapImporter", ".cache"), "pokeheartgold", Sources.HeartGoldRepo, Sources.HeartGoldCommit, Sources.HeartGoldFolders);

    public static int Run(string repo, string? dir, string outDir, bool quick)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        Console.WriteLine("Fetching pret/pokeheartgold…");
        var maps = new HgMaps(dir ?? Fetch(repo));
        var world = new HgWorld(maps);
        Console.WriteLine($"Read {maps.Headers.Count} areas, {maps.MatrixCount} matrices, {maps.LandCount} chunks, {world.Encounters.Count} tables of wild Pokémon, "
            + $"{world.Trainers.Count} trainers and {world.Phone.Count} contacts ({clock.Elapsed.TotalSeconds:F1} s)");

        Directory.CreateDirectory(outDir);
        var over = maps.Matrix(0);
        var tiles = Tiles(world, over);
        Mosaic(world, over, tiles, 4, byCover: true).Save(Path.Combine(outDir, "johto.png"));
        Mosaic(world, over, tiles, 4, byCover: false).Save(Path.Combine(outDir, "johto_behaviours.png"));
        HeightPicture(over, tiles, 2).Save(Path.Combine(outDir, "johto_heights.png"));
        int pictures = 3;
        if (!quick)
        {
            foreach (var matrixId in maps.MatrixIds)
            {
                var matrix = maps.Matrix(matrixId);
                var t = matrixId == 0 ? tiles : Tiles(world, matrix);
                foreach (var header in world.AreasOn(matrix))
                {
                    if (AreaPicture(world, matrix, t, header, 8) is { } picture)
                    {
                        picture.Save(Path.Combine(outDir, "areas", header.Key + ".png"));
                        pictures++;
                    }
                }
            }
            int files = WriteWorld(world, Path.Combine(outDir, "world"), open: null);
            Console.WriteLine($"Wrote {files} world files ({clock.Elapsed.TotalSeconds:F1} s)");
        }
        Console.WriteLine($"Drew {pictures} pictures ({clock.Elapsed.TotalSeconds:F1} s)");
        File.WriteAllText(Path.Combine(outDir, "report.md"), Report(world, tiles));
        foreach (string problem in world.Problems.Take(20)) Console.WriteLine("  " + problem);
        Console.WriteLine($"{world.Problems.Count} problems; report in {Path.Combine(outDir, "report.md")} ({clock.Elapsed.TotalSeconds:F1} s)");
        return 0;
    }

    /// <summary>
    /// <c>--game heartgold --data</c>: the files <c>Data/world/johto/</c> holds, by its own <c>world.json</c>. Its open
    /// areas, their matrices and the chunks in view of them (none while it opens none), and always the region's
    /// <c>trainers.json</c>, <c>phone.json</c> and <c>habitats.json</c>.
    /// </summary>
    public static int Data(string repo, string? dir, string? dataDir)
    {
        string data = dataDir ?? Path.Combine(repo, "PokemonPlatinumEngine", "Data", World.Folder, Region);
        string indexFile = Path.Combine(data, World.IndexFile);
        if (!File.Exists(indexFile))
        {
            Console.WriteLine($"{indexFile} isn't there: a region's world.json is written by hand, with the areas it opens, before --data writes their files.");
            return 1;
        }
        var index = GameDataFiles.Deserialize<WorldIndexFile>(File.ReadAllText(indexFile));
        var world = new HgWorld(new HgMaps(dir ?? Fetch(repo)));
        int files = WriteWorld(world, data, new HashSet<string>(index.Areas, StringComparer.Ordinal));
        foreach (string problem in world.Problems.Take(20)) Console.WriteLine("  " + problem);
        Console.WriteLine($"Wrote {files} files of {index.Areas.Count} open areas into {data}");
        return 0;
    }

    /// <summary>
    /// The world files under <paramref name="directory"/>. With no <paramref name="open"/>, every matrix, chunk and
    /// area and a <c>world.json</c> that makes a map of each matrix and opens every area (for a tool to build); with
    /// one, only what it opens and the chunks round it, and no <c>world.json</c>. The region's three tables either way.
    /// </summary>
    public static int WriteWorld(HgWorld world, string directory, ISet<string>? open)
    {
        int files = 0;
        void Write<T>(string folder, string name, T value)
        {
            string path = Path.Combine(directory, folder, name + ".json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string text = GameDataFiles.Serialize(value);
            if (File.Exists(path) && File.ReadAllText(path) == text) return;
            File.WriteAllText(path, text);
            files++;
        }
        var maps = world.Maps;
        var index = new WorldIndexFile { Region = "Johto" };
        var wanted = new HashSet<int>();
        foreach (int id in maps.MatrixIds)
        {
            var matrix = maps.Matrix(id);
            var areas = world.AreasOn(matrix).ToList();
            if (areas.Count == 0) continue;
            bool any = open == null || areas.Any(a => open.Contains(a.Key));
            if (!any) continue;
            for (int cy = 0; cy < matrix.Height; cy++)
                for (int cx = 0; cx < matrix.Width; cx++)
                {
                    int land = matrix.LandAt(cx, cy);
                    if (land == Matrix.NoLand) continue;
                    bool inView = open == null;
                    for (int y = cy - 1; y <= cy + 1 && !inView; y++)
                        for (int x = cx - 1; x <= cx + 1 && !inView; x++)
                            if (x >= 0 && y >= 0 && x < matrix.Width && y < matrix.Height)
                            {
                                string key = HgWorld.KeyOf(matrix.HeaderAt(x, y) ?? areas[0].Id);
                                inView = open!.Contains(key) && matrix.LandAt(x, y) != Matrix.NoLand;
                            }
                    if (inView) wanted.Add(land);
                }
            Write("matrices", id.ToString("000", CultureInfo.InvariantCulture), world.MatrixFile(matrix));
            foreach (var header in areas)
            {
                if (open != null && !open.Contains(header.Key)) continue;
                Write("areas", header.Key, world.Area(header));
                index.Areas.Add(header.Key);
            }
            var first = areas[0];
            index.Maps.Add(new WorldMapEntry
            {
                Name = id == 0 ? "Johto" : WorldWriter.Pascal(first.Key.ToUpperInvariant()),
                Matrix = id,
                Area = matrix.Headers == null ? first.Key : null,
                Trees = TreeStyle.Round,
                Setting = first.MapType switch { "INTERIOR" => MapSetting.Indoors, "CAVE" => MapSetting.Cave, _ => MapSetting.Outdoors }
            });
        }
        foreach (int land in wanted.OrderBy(l => l)) Write("chunks", land.ToString("000", CultureInfo.InvariantCulture), world.Chunk(land));
        Write("", "trainers", world.Trainers);
        Write("", "phone", world.Phone);
        Write("", Path.GetFileNameWithoutExtension(WorldHabitatsFile.FileName), world.Habitats());
        if (open == null)
        {
            File.WriteAllText(Path.Combine(directory, World.IndexFile), GameDataFiles.Serialize(index));
            files++;
        }
        return files;
    }

    // ---------------------------------------------------------------- pictures

    /// <summary>A matrix's tiles, row by row: behaviour (this game's), blocked, look, height, and its header's index (-1 where there is no chunk).</summary>
    public sealed record TileGrid(int Width, int Height, byte[] Behaviour, bool[] Solid, TerrainCover[] Cover, float[] Ground, string?[] Header);

    public static TileGrid Tiles(HgWorld world, Matrix matrix)
    {
        int w = matrix.Width * LandData.Tiles, h = matrix.Height * LandData.Tiles;
        var grid = new TileGrid(w, h, new byte[w * h], new bool[w * h], new TerrainCover[w * h], new float[w * h], new string?[w * h]);
        Array.Fill(grid.Ground, float.NaN);
        for (int cy = 0; cy < matrix.Height; cy++)
            for (int cx = 0; cx < matrix.Width; cx++)
            {
                int landId = matrix.LandAt(cx, cy);
                if (landId == Matrix.NoLand || landId >= world.Maps.LandCount) continue;
                var land = world.Maps.Land(landId);
                var look = world.CoverOf(landId);
                string header = matrix.HeaderAt(cx, cy) ?? "";
                float altitude = matrix.AltitudeAt(cx, cy) / 2f;
                for (int z = 0; z < LandData.Tiles; z++)
                    for (int x = 0; x < LandData.Tiles; x++)
                    {
                        int i = (cy * LandData.Tiles + z) * w + cx * LandData.Tiles + x;
                        grid.Behaviour[i] = world.Behaviour(land, x, z);
                        grid.Solid[i] = land.Solid(x, z);
                        grid.Cover[i] = look[z * LandData.Tiles + x];
                        grid.Header[i] = header;
                        float ux = (x + 0.5f - LandData.Tiles / 2f) * LandData.TileUnits, uz = (z + 0.5f - LandData.Tiles / 2f) * LandData.TileUnits;
                        var heights = land.Heights.HeightsAt(ux, uz).Where(float.IsFinite).ToList();
                        grid.Ground[i] = (heights.Count > 0 ? heights[0] / LandData.TileUnits : 0f) + altitude;
                    }
            }
        return grid;
    }

    private static Renders.Setting SettingOf(HgWorld world, string? header) =>
        header != null && world.Maps.Headers.TryGetValue(header, out var h)
            ? h.MapType switch { "INTERIOR" => Renders.Setting.Indoors, "CAVE" => Renders.Setting.Cave, _ => Renders.Setting.Outdoors }
            : Renders.Setting.Outdoors;

    private static Rgb Colour(HgWorld world, TileGrid t, int i, bool byCover) =>
        t.Header[i] == null ? Renders.Nothing
        : byCover ? Renders.CoverColour(t.Cover[i], t.Solid[i])
        : t.Behaviour[i] switch
        {
            // HeartGold's own, which Platinum's key doesn't colour: the Headbutt trees (most of the overworld's
            // woods), whirlpools, magma and the ladders
            HgBehaviours.Headbutt => Rgb.Of(34, 88, 48),
            HgBehaviours.Whirlpool => Rgb.Of(30, 60, 160),
            HgBehaviours.Magma => Rgb.Of(230, 90, 30),
            HgBehaviours.LadderNorth or HgBehaviours.LadderSouth or HgBehaviours.LadderDown => Rgb.Of(200, 150, 60),
            _ => Renders.Colour(t.Behaviour[i], t.Solid[i], SettingOf(world, t.Header[i]))
        };

    /// <summary>The overworld, <paramref name="scale"/> pixels a tile, each area outlined and named, Kanto's in another ink.</summary>
    public static Picture Mosaic(HgWorld world, Matrix matrix, TileGrid t, int scale, bool byCover)
    {
        var picture = new Picture(t.Width * scale, t.Height * scale, Renders.Nothing);
        for (int y = 0; y < t.Height; y++)
            for (int x = 0; x < t.Width; x++)
                picture.Fill(x * scale, y * scale, scale, scale, Colour(world, t, y * t.Width + x, byCover));
        // Each area's chunks: a frame round each, and the name over the first
        var named = new HashSet<string>(StringComparer.Ordinal);
        for (int cy = 0; cy < matrix.Height; cy++)
            for (int cx = 0; cx < matrix.Width; cx++)
            {
                string? id = matrix.HeaderAt(cx, cy);
                if (id == null || matrix.LandAt(cx, cy) == Matrix.NoLand || !world.Maps.Headers.TryGetValue(id, out var header) || header.Index == 0) continue;
                int px = cx * LandData.Tiles * scale, py = cy * LandData.Tiles * scale, size = LandData.Tiles * scale;
                var ink = header.Kanto ? Rgb.Of(120, 30, 40) : Rgb.Of(24, 22, 40);
                bool Same(int x, int y) => x >= 0 && y >= 0 && x < matrix.Width && y < matrix.Height && matrix.HeaderAt(x, y) == id && matrix.LandAt(x, y) != Matrix.NoLand;
                if (!Same(cx - 1, cy)) picture.Fill(px, py, 2, size, ink);
                if (!Same(cx + 1, cy)) picture.Fill(px + size - 2, py, 2, size, ink);
                if (!Same(cx, cy - 1)) picture.Fill(px, py, size, 2, ink);
                if (!Same(cx, cy + 1)) picture.Fill(px, py + size - 2, size, 2, ink);
                if (named.Add(id)) picture.Text(px + 4, py + 4, header.Name.ToUpperInvariant(), Rgb.Of(255, 255, 255), 2, ink);
            }
        return picture;
    }

    /// <summary>The overworld shaded by height: dark low, light high.</summary>
    public static Picture HeightPicture(Matrix matrix, TileGrid t, int scale)
    {
        var heights = t.Ground.Where(g => !float.IsNaN(g)).ToList();
        float low = heights.Count > 0 ? heights.Min() : 0, high = heights.Count > 0 ? heights.Max() : 1;
        var picture = new Picture(t.Width * scale, t.Height * scale, Renders.Nothing);
        for (int y = 0; y < t.Height; y++)
            for (int x = 0; x < t.Width; x++)
            {
                int i = y * t.Width + x;
                if (t.Header[i] == null || float.IsNaN(t.Ground[i])) continue;
                float v = Math.Clamp((t.Ground[i] - low) / Math.Max(1f, high - low), 0f, 1f);
                picture.Fill(x * scale, y * scale, scale, scale, Rgb.Of(40, 60, 90).Mix(Rgb.Of(250, 240, 210), v));
            }
        return picture;
    }

    /// <summary>One area close up by cover, with its warps (blue), people (red), signs (yellow) and triggers (orange outlines).</summary>
    public static Picture? AreaPicture(HgWorld world, Matrix matrix, TileGrid t, HgHeader header, int scale)
    {
        int x0 = int.MaxValue, y0 = int.MaxValue, x1 = -1, y1 = -1;
        for (int cy = 0; cy < matrix.Height; cy++)
            for (int cx = 0; cx < matrix.Width; cx++)
                if (matrix.LandAt(cx, cy) != Matrix.NoLand && (matrix.Headers == null || matrix.HeaderAt(cx, cy) == header.Id))
                    (x0, y0, x1, y1) = (Math.Min(x0, cx), Math.Min(y0, cy), Math.Max(x1, cx), Math.Max(y1, cy));
        if (x1 < 0) return null;
        int ox = x0 * LandData.Tiles, oy = y0 * LandData.Tiles, w = (x1 - x0 + 1) * LandData.Tiles, h = (y1 - y0 + 1) * LandData.Tiles;
        var picture = new Picture(w * scale, h * scale, Renders.Nothing);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = (oy + y) * t.Width + ox + x;
                bool mine = matrix.Headers == null || t.Header[i] == header.Id;
                var c = Colour(world, t, i, byCover: true);
                picture.Fill(x * scale, y * scale, scale, scale, mine ? c : c.Darker(0.6f));
            }
        var events = world.Maps.Events(header.Events);
        void Mark(int x, int z, Rgb c)
        {
            if (x < ox || z < oy || x >= ox + w || z >= oy + h) return;
            picture.Fill((x - ox) * scale + 1, (z - oy) * scale + 1, scale - 2, scale - 2, c);
        }
        foreach (var trigger in events.Triggers)
            picture.Frame((trigger.X - ox) * scale, (trigger.Z - oy) * scale, Math.Max(1, trigger.Width) * scale, Math.Max(1, trigger.Length) * scale, Rgb.Of(250, 150, 40), 2);
        foreach (var warp in events.Warps) Mark(warp.X, warp.Z, Rgb.Of(60, 120, 250));
        foreach (var sign in events.Signs) Mark(sign.X, sign.Z, Rgb.Of(250, 220, 60));
        foreach (var o in events.Objects) Mark(o.X, o.Z, o.Script.StartsWith("TRAINER_", StringComparison.Ordinal) ? Rgb.Of(200, 30, 200) : Rgb.Of(230, 50, 50));
        picture.Text(4, 4, header.Name.ToUpperInvariant(), Rgb.Of(255, 255, 255), 2, Rgb.Of(24, 22, 40));
        return picture;
    }

    // ---------------------------------------------------------------- report

    public static string Report(HgWorld world, TileGrid over)
    {
        var maps = world.Maps;
        var text = new StringBuilder();
        long overTiles = Enumerable.Range(0, over.Header.Length).Count(i => over.Header[i] != null);
        long overUnknown = Enumerable.Range(0, over.Header.Length).Count(i => over.Header[i] != null && over.Cover[i] == TerrainCover.Unknown);
        int johto = maps.Headers.Values.Count(h => !h.Kanto && h.Index > 0), kanto = maps.Headers.Values.Count(h => h.Kanto);
        var overMatrix = maps.Matrix(0);
        text.Append(CultureInfo.InvariantCulture, $"""
            # Johto (HeartGold): the import

            *Written by `tools/MapImporter --game heartgold` (plan 20 · J1), from pret/pokeheartgold at `{Sources.HeartGoldCommit[..8]}`.*

            {maps.Headers.Count} map headers ({johto} of Johto and {kanto} of Kanto by `regionNo`, the rest the original's dummies), {maps.MatrixCount} matrices, {maps.LandCount} chunks; {world.Encounters.Count} tables of wild Pokémon, {world.Headbutt.Count(h => h.Trees.Count > 0)} areas with Headbutt trees, {world.Safari.Count} Safari Zone areas, {world.Trainers.Count} trainers, {world.Phone.Count} phone contacts.
            The overworld (matrix 0, `{overMatrix.Name}`) is {overMatrix.Width} × {overMatrix.Height} chunks, Johto and Kanto on one grid; {Percent(overTiles - overUnknown, overTiles)} of its {overTiles:N0} tiles have a look (Sinnoh's overworld is 99.5% sorted).

            ## Problems

            """);
        if (world.Problems.Count == 0) text.AppendLine("None.");
        foreach (string problem in world.Problems) text.AppendLine("- " + problem);

        // The textures no rule sorts: the overworld's first, then everywhere else's
        var overLands = new HashSet<int>(overMatrix.Land.Where(l => l != Matrix.NoLand));
        var unknownOver = new Dictionary<string, int>(StringComparer.Ordinal);
        var unknownElse = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int land = 0; land < maps.LandCount; land++)
        {
            var layers = maps.Land(land).ReadTerrain()?.LayersOver(maps.Land(land).Heights);
            if (layers == null) continue;
            var look = world.CoverOf(land);
            for (int i = 0; i < look.Length; i++)
            {
                if (look[i] != TerrainCover.Unknown) continue;
                foreach (string? name in new[] { layers.Ground[i], layers.Above[i] }.Distinct())
                {
                    if (name == null) continue;
                    HgCover.OfTexture(name, out bool known);
                    if (known) continue;
                    var into = overLands.Contains(land) ? unknownOver : unknownElse;
                    into[name] = into.GetValueOrDefault(name) + 1;
                }
            }
        }
        foreach (var (title, table) in new[] { ("on the overworld", unknownOver), ("in the other matrices", unknownElse) })
        {
            text.Append(CultureInfo.InvariantCulture, $"""

                ## Textures no rule sorts, {title}

                {table.Count} names; a region session adds their patterns to `Hg/HgCover.cs` (or Platinum's `Cover.cs`, where both games share a name) for the areas it opens.

                | Texture | Tiles |
                |---|---:|

                """);
            foreach (var (name, count) in table.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).Take(120))
                text.Append(CultureInfo.InvariantCulture, $"| `{name}` | {count:N0} |\n");
        }

        // Behaviours: what each of HeartGold's becomes, with its count on every chunk
        var use = new Dictionary<byte, int>();
        for (int land = 0; land < maps.LandCount; land++)
        {
            var data = maps.Land(land);
            for (int z = 0; z < LandData.Tiles; z++)
                for (int x = 0; x < LandData.Tiles; x++) use[data.Behaviour(x, z)] = use.GetValueOrDefault(data.Behaviour(x, z)) + 1;
        }
        text.Append("""

            ## Behaviours

            HeartGold's value, its name in `metatile_behavior.h` (a bare number where it has none), and the value it is written as (`Hg/HgBehaviours.cs`).

            | Value | HeartGold's name | Becomes | Tiles |
            |---|---|---|---:|

            """);
        foreach (var (value, count) in use.OrderBy(kv => kv.Key))
        {
            string name = maps.BehaviourNames.TryGetValue(value, out var n) ? n : "";
            byte becomes = world.Behaviours[value];
            string label = Enum.IsDefined((TileBehavior)becomes) ? ((TileBehavior)becomes).ToString() : "HeartGold's own";
            text.Append(CultureInfo.InvariantCulture, $"| 0x{value:X2} | {(name.Length > 0 ? "`" + name + "`" : "")} | 0x{becomes:X2} `{label}`{(becomes != value ? " (moved)" : "")} | {count:N0} |\n");
        }

        // The building models outdoors: their names and boxes, and whether the catalogue knows them
        var models = new Dictionary<string, (int Count, ModelInfo Info)>(StringComparer.Ordinal);
        foreach (int land in overLands)
            foreach (var prop in maps.Land(land).Props)
                if (world.PropModel(land, prop.ModelId) is { } info)
                    models[info.Name] = (models.GetValueOrDefault(info.Name).Count + 1, info);
        text.Append(CultureInfo.InvariantCulture, $"""

            ## Models on the overworld

            {models.Count} models of `bm_field.narc` ({maps.ModelCount(room: false)} in all; `bm_room.narc` has {maps.ModelCount(room: true)}). None is in `Data/WorldModels.cs`'s catalogue yet but where Platinum's has the same name; J2 starts `Data/WorldModels.Johto.cs`.

            | Model | Size (tiles) | Placed | In the catalogue |
            |---|---|---:|---|

            """);
        foreach (var (name, (count, info)) in models.OrderByDescending(kv => kv.Value.Count).ThenBy(kv => kv.Key, StringComparer.Ordinal))
            text.Append(CultureInfo.InvariantCulture,
                $"| `{name}` | {info.BoxSize.X / LandData.TileUnits:0.#} × {info.BoxSize.Z / LandData.TileUnits:0.#} × {info.BoxSize.Y / LandData.TileUnits:0.#} | {count} | {(WorldModels.Of(name) != null ? "yes (Platinum's)" : "")} |\n");

        text.Append(CultureInfo.InvariantCulture, $"""

            ## Chunks

            {Enumerable.Range(0, maps.LandCount).Count(l => maps.Land(l).HgssRecords > 0)} of the {maps.LandCount} chunks carry records in HeartGold's own section of the land data ({Enumerable.Range(0, maps.LandCount).Sum(l => maps.Land(l).HgssRecords)} in all, eight bytes each); the rest is Platinum's format.

            ## Matrices

            | Matrix | Name | Size (chunks) | Areas |
            |---:|---|---|---|

            """);
        foreach (int id in maps.MatrixIds)
        {
            var m = maps.Matrix(id);
            var areas = world.AreasOn(m).Select(h => h.Key).ToList();
            text.Append(CultureInfo.InvariantCulture, $"| {id} | `{m.Name}` | {m.Width} × {m.Height} | {string.Join(", ", areas.Take(10))}{(areas.Count > 10 ? $" and {areas.Count - 10} more" : "")} |\n");
        }
        return text.ToString();
    }

    private static string Percent(long part, long whole) => whole == 0 ? "0%" : (100.0 * part / whole).ToString("F1", CultureInfo.InvariantCulture) + "%";
}
