using System.Globalization;
using System.Text;
using DataImporter;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace MapImporter.Gba;

/// <summary>
/// The <c>--game</c> command (plan 18 · W6): one Game Boy Advance game read into <c>out/&lt;region&gt;/</c>: a mosaic
/// of its overworld by cover, by behaviour and by height, a picture of every map, its world files and
/// <c>report.md</c>; and the GBA's section of <c>docs/tile-behaviours.md</c>, from both games.
/// </summary>
public static class GbaImport
{
    public static string RegionOf(string game) => GbaGame.Of(game).Region;

    public static string Fetch(string repo, GbaGame game)
    {
        string cache = Path.Combine(repo, "tools", "MapImporter", ".cache");
        return game == GbaGame.FireRed
            ? Sources.Checkout(cache, "pokefirered", Sources.FireRedRepo, Sources.FireRedCommit, Sources.GbaFolders)
            : Sources.Checkout(cache, "pokeemerald", Sources.EmeraldRepo, Sources.EmeraldCommit, Sources.GbaFolders);
    }

    public static int Run(string repo, string gameName, string? dir, string outDir, bool quick)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var game = GbaGame.Of(gameName);
        Console.WriteLine($"Fetching {(game == GbaGame.FireRed ? "pret/pokefirered" : "pret/pokeemerald")}…");
        var maps = new GbaMaps(dir ?? Fetch(repo, game), game);
        var world = new GbaWorld(maps);
        Console.WriteLine($"Read {maps.Maps.Count} maps and {maps.Layouts.Count} layouts into {world.Matrices.Count} matrices ({clock.Elapsed.TotalSeconds:F1} s)");

        Directory.CreateDirectory(outDir);
        var overworld = world.Matrices[0];
        Mosaic(overworld, 4, byCover: true).Save(Path.Combine(outDir, game.Region + ".png"));
        Mosaic(overworld, 4, byCover: false).Save(Path.Combine(outDir, game.Region + "_behaviours.png"));
        HeightPicture(overworld, 2).Save(Path.Combine(outDir, game.Region + "_heights.png"));
        int pictures = 3;
        if (!quick)
        {
            foreach (var matrix in world.Matrices)
                foreach (var placed in matrix.Maps)
                {
                    MapPicture(matrix, placed, 8).Save(Path.Combine(outDir, "maps", placed.Map.Name + ".png"));
                    pictures++;
                }
            int files = WriteWorld(world, Path.Combine(outDir, "world"));
            Console.WriteLine($"Wrote {files} world files ({clock.Elapsed.TotalSeconds:F1} s)");
        }
        Console.WriteLine($"Drew {pictures} pictures ({clock.Elapsed.TotalSeconds:F1} s)");
        File.WriteAllText(Path.Combine(outDir, "report.md"), Report(world));

        // The docs' section counts both games, so the other is read too (fetched once, like this one)
        var both = new List<(GbaGame, GbaWorld)>();
        foreach (var each in new[] { GbaGame.FireRed, GbaGame.Emerald })
            both.Add((each, each == game ? world : new GbaWorld(new GbaMaps(Fetch(repo, each), each))));
        string doc = Path.Combine(repo, "docs", "tile-behaviours.md");
        File.WriteAllText(doc, Splice(File.Exists(doc) ? File.ReadAllText(doc) : "", Section(both)));

        foreach (string problem in world.Problems.Take(20)) Console.WriteLine("  " + problem);
        Console.WriteLine($"{world.Problems.Count} problems; report in {Path.Combine(outDir, "report.md")} ({clock.Elapsed.TotalSeconds:F1} s)");
        return 0;
    }

    // ---------------------------------------------------------------- world files

    /// <summary>
    /// Writes the matrices, chunks and areas of a GBA world under <paramref name="directory"/> with a
    /// <c>world.json</c> that makes a map of every matrix (the overworld named after the region) and opens every area,
    /// so a tool can build it as the game builds Sinnoh's. Returns how many files.
    /// </summary>
    public static int WriteWorld(GbaWorld world, string directory)
    {
        int files = 0;
        void Write<T>(string folder, string name, T value)
        {
            string path = Path.Combine(directory, folder, name + ".json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, GameDataFiles.Serialize(value));
            files++;
        }

        var index = new WorldIndexFile { Region = world.Source.Game.RegionName };
        int nextChunk = 0, mapIndex = 0;
        foreach (var matrix in world.Matrices)
        {
            var ids = new int[matrix.ChunksWide, matrix.ChunksHigh];
            for (int cy = 0; cy < matrix.ChunksHigh; cy++)
                for (int cx = 0; cx < matrix.ChunksWide; cx++)
                {
                    if (GbaWorld.Owner(matrix, cx, cy) < 0) { ids[cx, cy] = -1; continue; }
                    ids[cx, cy] = nextChunk;
                    Write("chunks", nextChunk.ToString("000", CultureInfo.InvariantCulture), GbaWorld.Chunk(matrix, cx, cy, nextChunk));
                    nextChunk++;
                }
            Write("matrices", matrix.Id.ToString("000", CultureInfo.InvariantCulture), GbaWorld.MatrixFile(matrix, (cx, cy) => ids[cx, cy]));
            foreach (var placed in matrix.Maps)
            {
                Write("areas", placed.Map.Key, GbaWorld.AreaFile(placed.Map, mapIndex++, matrix.Id));
                index.Areas.Add(placed.Map.Key);
            }
            var first = matrix.Maps[0].Map;
            index.Maps.Add(new WorldMapEntry
            {
                Name = matrix.Id == 0 ? world.Source.Game.RegionName : first.Name,
                Matrix = matrix.Id,
                Area = matrix.Maps.Count == 1 ? first.Key : null,
                Trees = TreeStyle.Round,
                Setting = first.MapType == "MAP_TYPE_UNDERGROUND" ? MapSetting.Cave : MapSetting.Outdoors
            });
        }
        File.WriteAllText(Path.Combine(directory, World.IndexFile), GameDataFiles.Serialize(index));
        return files + 1;
    }

    // ---------------------------------------------------------------- pictures

    private static Rgb TileColour(GbaMatrix m, int i, bool byCover) =>
        m.Area[i] < 0 ? Renders.Nothing
        : byCover ? Renders.CoverColour(m.Cover[i], m.Solid[i])
        : Renders.Colour(m.Behaviour[i], m.Solid[i], m.Maps[m.Area[i]].Map.MapType == "MAP_TYPE_INDOOR" ? Renders.Setting.Indoors
            : m.Maps[m.Area[i]].Map.MapType == "MAP_TYPE_UNDERGROUND" ? Renders.Setting.Cave : Renders.Setting.Outdoors);

    /// <summary>A matrix, <paramref name="scale"/> pixels a tile, with each map's outline and name.</summary>
    public static Picture Mosaic(GbaMatrix m, int scale, bool byCover)
    {
        var picture = new Picture(m.Width * scale, m.Height * scale, Renders.Nothing);
        for (int y = 0; y < m.Height; y++)
            for (int x = 0; x < m.Width; x++)
                picture.Fill(x * scale, y * scale, scale, scale, TileColour(m, y * m.Width + x, byCover));
        var ink = Rgb.Of(24, 22, 40);
        foreach (var placed in m.Maps)
        {
            picture.Frame(placed.X * scale, placed.Y * scale, placed.Layout.Width * scale, placed.Layout.Height * scale, ink);
            picture.Text(placed.X * scale + 3, placed.Y * scale + 3, placed.Map.Name.ToUpperInvariant(), Rgb.Of(255, 255, 255), 2, ink);
        }
        return picture;
    }

    /// <summary>A matrix shaded by height: dark low, light high, a slope striped.</summary>
    public static Picture HeightPicture(GbaMatrix m, int scale)
    {
        var picture = new Picture(m.Width * scale, m.Height * scale, Renders.Nothing);
        for (int y = 0; y < m.Height; y++)
            for (int x = 0; x < m.Width; x++)
            {
                int i = y * m.Width + x;
                if (m.Area[i] < 0) continue;
                float t = Math.Clamp((m.Ground[i] + 2f) / 12f, 0f, 1f);
                var c = Rgb.Of(40, 60, 90).Mix(Rgb.Of(250, 240, 210), t);
                if (m.SlopeX[i] != 0 || m.SlopeZ[i] != 0) c = c.Mix(Rgb.Of(230, 120, 60), 0.5f);
                if (!float.IsNaN(m.Deck[i])) c = Rgb.Of(172, 122, 80);
                picture.Fill(x * scale, y * scale, scale, scale, c);
            }
        return picture;
    }

    /// <summary>One map by behaviour on the left and by cover on the right.</summary>
    public static Picture MapPicture(GbaMatrix m, GbaPlaced placed, int scale)
    {
        int w = placed.Layout.Width, h = placed.Layout.Height;
        var picture = new Picture(w * scale * 2 + scale, h * scale, Renders.Nothing);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = (placed.Y + y) * m.Width + placed.X + x;
                picture.Fill(x * scale, y * scale, scale, scale, TileColour(m, i, byCover: false));
                picture.Fill((w + 1 + x) * scale, y * scale, scale, scale, TileColour(m, i, byCover: true));
            }
        return picture;
    }

    // ---------------------------------------------------------------- report

    public static string Report(GbaWorld world)
    {
        var text = new StringBuilder();
        var game = world.Source.Game;
        long tiles = world.Matrices.Sum(m => (long)m.Area.Count(a => a >= 0));
        long unknown = world.UnknownCover.Values.Sum(t => t.Values.Sum());
        var over = world.Matrices[0];
        long overTiles = over.Area.Count(a => a >= 0);
        long overUnknown = Enumerable.Range(0, over.Area.Length).Count(i => over.Area[i] >= 0 && over.Cover[i] == TerrainCover.Unknown);
        text.Append(CultureInfo.InvariantCulture, $"""
            # {game.RegionName} ({game.Name}): the import

            *Written by `tools/MapImporter --game {game.Name}`.*

            {world.Source.Maps.Count} maps on {world.Source.Layouts.Count} layouts, stitched into {world.Matrices.Count} matrices; {tiles:N0} tiles in all.
            The overworld (matrix 0) is {over.Width} × {over.Height} tiles from {over.Maps[0].Map.Name}, {over.Maps.Count} maps; {Percent(overTiles - overUnknown, overTiles)} of its tiles have a look ({Percent(tiles - unknown, tiles)} of every map's). Sinnoh's overworld is 99.5% sorted.

            ## Problems

            """);
        if (world.Problems.Count == 0) text.AppendLine("None.");
        foreach (string problem in world.Problems) text.AppendLine("- " + problem);

        text.Append("""

            ## Behaviours

            | Behaviour | Value | Becomes | Tiles |
            |---|---|---|---:|

            """);
        var byName = world.Source.BehaviourNames.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.Ordinal);
        foreach (var (name, count) in world.BehaviourUse.OrderByDescending(kv => kv.Value))
        {
            var becomes = GbaBehaviours.Of(name, out bool known);
            string value = byName.TryGetValue(name, out int v) ? $"0x{v:X2}" : "";
            text.Append(CultureInfo.InvariantCulture, $"| `{name}` | {value} | `{becomes}` (0x{(int)becomes:X2}){(known ? "" : ", not in the table")} | {count:N0} |\n");
        }

        text.Append("""

            ## Metatiles with no look yet, by tileset

            Each is a metatile no behaviour and no label sorts. The region's own session writes `GbaCover.Ranges` for them.

            | Tileset | Metatiles | Tiles | The most used (metatile: tiles) |
            |---|---:|---:|---|

            """);
        foreach (var (tileset, counts) in world.UnknownCover.OrderByDescending(kv => kv.Value.Values.Sum()))
            text.Append(CultureInfo.InvariantCulture,
                $"| `{tileset}` | {counts.Count} | {counts.Values.Sum():N0} | {string.Join(", ", counts.OrderByDescending(c => c.Value).Take(8).Select(c => $"0x{c.Key:X3}: {c.Value}"))} |\n");

        text.Append("""

            ## Elevations on the overworld

            | Elevation | Tiles | Height |
            |---:|---:|---|

            """);
        foreach (var group in Enumerable.Range(0, over.Area.Length).Where(i => over.Area[i] >= 0).GroupBy(i => over.Elevation[i]).OrderBy(g => g.Key))
            text.Append(CultureInfo.InvariantCulture, $"| {group.Key} | {group.Count():N0} | {(group.Key switch { 0 => "a slope between the levels beside it", 1 => "the nearest land's level", 15 => "a deck over the ground", _ => GbaWorld.LevelOf(group.Key).ToString("0.0", CultureInfo.InvariantCulture) })} |\n");
        text.Append(CultureInfo.InvariantCulture, $"\n{Enumerable.Range(0, over.Area.Length).Count(i => over.SlopeX[i] != 0 || over.SlopeZ[i] != 0):N0} tiles slope; {Enumerable.Range(0, over.Area.Length).Count(i => !float.IsNaN(over.Deck[i])):N0} carry a deck.\n");

        text.Append("""

            ## Matrices

            | Matrix | Size (chunks) | Maps |
            |---:|---|---|

            """);
        foreach (var m in world.Matrices)
            text.Append(CultureInfo.InvariantCulture, $"| {m.Id} | {m.ChunksWide} × {m.ChunksHigh} | {string.Join(", ", m.Maps.Take(12).Select(p => p.Map.Name))}{(m.Maps.Count > 12 ? $" and {m.Maps.Count - 12} more" : "")} |\n");
        return text.ToString();
    }

    private static string Percent(long part, long whole) => whole == 0 ? "0%" : (100.0 * part / whole).ToString("F1", CultureInfo.InvariantCulture) + "%";

    // ---------------------------------------------------------------- docs/tile-behaviours.md

    public const string SectionTitle = "## The GBA's behaviours";

    /// <summary>The GBA's section of <c>docs/tile-behaviours.md</c>: each <c>MB_*</c> of either game, the value it becomes and its count on each game's maps.</summary>
    public static string Section(IReadOnlyList<(GbaGame Game, GbaWorld World)> games)
    {
        var text = new StringBuilder();
        text.Append(SectionTitle).Append("""


            *Written by `tools/MapImporter --game firered` or `--game emerald` (plan 18 · W6), which read both games; the Sinnoh run keeps it as it is.*

            The Game Boy Advance's maps give a tile a behaviour (`MB_*`, numbered differently in FireRed and Emerald) and, apart from it, two bits of collision and four of elevation. The reader (`tools/MapImporter/Gba/GbaBehaviours.cs`) turns each behaviour into one of this game's values by its name: Platinum's own value where Platinum has one that means the same, a value of the GBA's own among the ones Platinum leaves unused at the top of its byte (`0xED` to `0xFD`) where it has none, and `None` for a behaviour that only says how a tile is read or drawn (a sign, a cupboard), which is an event's or the art's business. Collision bits set make the tile blocked. Elevation becomes height: a level is 1.4 tiles above elevation 3, elevation 0 (any level) slopes between the levels beside it, the water's elevation 1 takes the nearest land's level and a bridge's 15 is a deck over the ground.

            | Behaviour | FireRed | Emerald | Becomes | Value | FireRed tiles | Emerald tiles |
            |---|---|---|---|---|---:|---:|

            """);
        var names = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var (_, world) in games) names.UnionWith(world.Source.BehaviourNames.Values);
        int unused = names.Count(name => !GbaBehaviours.Table.ContainsKey(name) && games.All(g => g.World.BehaviourUse.GetValueOrDefault(name) == 0));
        foreach (string name in names)
        {
            if (!GbaBehaviours.Table.ContainsKey(name) && games.All(g => g.World.BehaviourUse.GetValueOrDefault(name) == 0)) continue;
            var becomes = GbaBehaviours.Of(name, out bool known);
            string Value(GbaGame g) => games.First(x => x.Game == g).World.Source.BehaviourNames.FirstOrDefault(kv => kv.Value == name) is { Value: not null } kv
                ? $"0x{kv.Key:X2}" : "";
            string Count(GbaGame g) => games.First(x => x.Game == g).World.BehaviourUse.GetValueOrDefault(name) is var c and > 0 ? c.ToString("N0", CultureInfo.InvariantCulture) : "";
            text.Append(CultureInfo.InvariantCulture,
                $"| `{name}` | {Value(GbaGame.FireRed)} | {Value(GbaGame.Emerald)} | `{becomes}`{(known ? "" : " (not in the table)")} | `0x{(int)becomes:X2}` | {Count(GbaGame.FireRed)} | {Count(GbaGame.Emerald)} |\n");
        }
        text.Append(CultureInfo.InvariantCulture, $"""

            {unused} more names are defined by one game or the other and used on neither's maps (`MB_UNUSED_*` among them); they become `None`.

            The values of the GBA's own, and what the game does with each today:
            """);
        text.Append("""


            | Value | Name | What it is | In the game |
            |---|---|---|---|

            """);
        foreach (var behaviour in GbaBehaviours.Table.Values.Distinct().Where(b => (int)b >= 0xED || b is TileBehavior.LedgeNorth or >= TileBehavior.BlockNorth and <= TileBehavior.BlockSouthAndWest).OrderBy(b => (int)b))
        {
            var (support, note) = TileBehaviors.InGame(behaviour);
            string inGame = support switch { BehaviourSupport.Ruled => "**Rule.** " + note, BehaviourSupport.Waiting => "*Not yet.* " + note, _ => note };
            text.Append(CultureInfo.InvariantCulture, $"| `0x{(int)behaviour:X2}` | `{behaviour}` | {TileBehaviors.Meaning(behaviour)} | {inGame} |\n");
        }
        return text.ToString();
    }

    /// <summary>A doc with its GBA section put in or replaced: everything from the section's title to the end is the GBA's.</summary>
    public static string Splice(string doc, string section)
    {
        int at = doc.IndexOf(SectionTitle, StringComparison.Ordinal);
        string before = (at >= 0 ? doc[..at] : doc).TrimEnd('\r', '\n', ' ');
        return before + "\n\n" + section;
    }

    /// <summary>The GBA's section of a doc as it stands, or nothing: what the Sinnoh run keeps.</summary>
    public static string SectionOf(string doc)
    {
        int at = doc.IndexOf(SectionTitle, StringComparison.Ordinal);
        return at >= 0 ? doc[at..] : "";
    }
}
