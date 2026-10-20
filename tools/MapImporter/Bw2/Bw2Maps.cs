using System.Buffers.Binary;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MapImporter.Bw2;

/// <summary>
/// The decompilation's lists of constants (<c>generated/*.txt</c>): a name to a line, numbered from the last
/// <c>= N</c> on, with <c>#</c> lines left out.
/// </summary>
public static class Bw2Constants
{
    public static Dictionary<string, int> Read(string text)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        int next = 0;
        foreach (string raw in text.Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            int eq = line.IndexOf('=');
            string name = (eq >= 0 ? line[..eq] : line).Trim();
            if (eq >= 0) next = int.Parse(line[(eq + 1)..].Trim(), CultureInfo.InvariantCulture);
            result.TryAdd(name, next++);
        }
        return result;
    }
}

/// <summary>A zone's header (<c>res/field/zones/&lt;zone&gt;.json</c>): the part of a matrix a place is, and what it is like.</summary>
public sealed record Bw2Zone(string Key, int Id, int MapType, string Matrix, string? Area, string? Place, string? Parent,
    string? Music, string? Encounters, int Weather, int Camera, int BattleBackground, bool Cycling, bool EscapeRope,
    bool FlyFrom, (int X, int Y, int Z)? Fly)
{
    /// <summary>The zone's constant, <c>ZONE_ASPERTIA_CITY</c>.</summary>
    public string Constant => "ZONE_" + Key.ToUpperInvariant();

    /// <summary>A name to print: the place's words, <c>ASPERTIA CITY</c>, or the zone's.</summary>
    public string Name => (Place is { } p && p.StartsWith("PLACE_", StringComparison.Ordinal) ? p["PLACE_".Length..] : Key.ToUpperInvariant()).Replace('_', ' ');

    /// <summary>Reads a header. The music of a zone whose song changes with the season is its spring's.</summary>
    public static Bw2Zone Parse(string key, int id, string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        string? Str(string name) => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        int Int(string name) => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;
        bool Bool(string name) => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;
        string? music = Str("bgm");
        if (music == null && root.TryGetProperty("bgm", out var seasons) && seasons.ValueKind == JsonValueKind.Object)
            music = seasons.EnumerateObject().Select(p => p.Value.GetString()).FirstOrDefault(s => s != null);
        (int, int, int)? fly = root.TryGetProperty("fly", out var f) && f.ValueKind == JsonValueKind.Object
            ? (f.GetProperty("x").GetInt32(), f.GetProperty("y").GetInt32(), f.GetProperty("z").GetInt32()) : null;
        return new Bw2Zone(key, id, Int("map_type"), Str("matrix") ?? "", Str("area"), Str("place"), Str("parent"), music,
            Str("encounters"), Int("weather"), Int("camera"), Int("battle_bg"), Bool("cycling"), Bool("escape_rope"), Bool("fly_from"), fly);
    }
}

/// <summary>
/// A matrix (<c>res/field/matrices/&lt;name&gt;.json</c>): rows of chunks by their <c>MAP_MODEL_*</c> names, and for the
/// overworld a grid of the zone each chunk belongs to.
/// </summary>
public sealed class Bw2Matrix
{
    public string Name { get; }
    public int Width { get; }
    public int Height { get; }
    private readonly string?[] models;
    private readonly string?[]? zones;

    /// <summary>How many of the zones' cells were empty under a chunk and were given the nearest zone (<see cref="FillZones"/>).</summary>
    public int ZonesFilled { get; private set; }

    public Bw2Matrix(string name, int width, int height, string?[] models, string?[]? zones)
    {
        (Name, Width, Height, this.models, this.zones) = (name, width, height, models, zones);
    }

    public bool HasZones => zones != null;
    public string? ModelAt(int x, int y) => x < 0 || y < 0 || x >= Width || y >= Height ? null : models[y * Width + x];
    public string? ZoneAt(int x, int y) => zones == null || x < 0 || y < 0 || x >= Width || y >= Height ? null : zones[y * Width + x];

    /// <summary>The chunk's folder under <c>res/field/maps/models</c>: <c>MAP_MODEL_OVERWORLD_2_MAP03_20</c> is <c>overworld_2_map03_20</c>.</summary>
    public static string Folder(string model) => (model.StartsWith("MAP_MODEL_", StringComparison.Ordinal) ? model["MAP_MODEL_".Length..] : model).ToLowerInvariant();

    /// <summary>A matrix file's constant: <c>castelia_city</c> is <c>MAP_MATRIX_CASTELIA_CITY</c>.</summary>
    public static string Constant(string fileName) => "MAP_MATRIX_" + fileName.ToUpperInvariant();

    public static Bw2Matrix Parse(string name, string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        string?[][] Grid(string property) => root.TryGetProperty(property, out var rows) && rows.ValueKind == JsonValueKind.Array
            ? rows.EnumerateArray().Select(r => r.EnumerateArray().Select(c => c.ValueKind == JsonValueKind.String ? c.GetString() : null).ToArray()).ToArray()
            : Array.Empty<string?[]>();
        var maps = Grid("maps");
        var zoneRows = root.TryGetProperty("zones", out _) ? Grid("zones") : null;
        int height = maps.Length, width = maps.Length == 0 ? 0 : maps.Max(r => r.Length);
        var flat = new string?[width * height];
        string?[]? zones = zoneRows == null ? null : new string?[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                flat[y * width + x] = x < maps[y].Length ? maps[y][x] : null;
                if (zones != null && y < zoneRows!.Length && x < zoneRows[y].Length) zones[y * width + x] = zoneRows[y][x];
            }
        return new Bw2Matrix(name, width, height, flat, zones);
    }

    /// <summary>
    /// Gives each chunk whose zone cell is empty the zone of the nearest chunk that has one (the original leaves the
    /// cells of some joins between places empty; the game asks the zone by position, and the nearest is what a step
    /// there would read). Nearest is counted in steps across chunks, then by the order of the four ways.
    /// </summary>
    public int FillZones()
    {
        if (zones == null) return 0;
        var queue = new Queue<int>();
        for (int i = 0; i < zones.Length; i++)
            if (zones[i] != null && models[i] != null) queue.Enqueue(i);
        int filled = 0;
        while (queue.Count > 0)
        {
            int i = queue.Dequeue();
            int x = i % Width, y = i / Width;
            foreach (var (dx, dy) in new[] { (0, -1), (1, 0), (0, 1), (-1, 0) })
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= Width || ny >= Height) continue;
                int n = ny * Width + nx;
                if (models[n] == null || zones[n] != null) continue;
                zones[n] = zones[i];
                filled++;
                queue.Enqueue(n);
            }
        }
        ZonesFilled += filled;
        return filled;
    }

    public IEnumerable<string> Models => models.Where(m => m != null)!;
}

/// <summary>
/// The height tables the terrain grids index (<c>MAP_HEIGHT_SLOPE_TABLE</c>, normals of three fx16 each, and
/// <c>MAP_HEIGHT_OFFSET_TABLE</c>, fx32), read from the C file that holds them, and the original's sum on them.
/// </summary>
public sealed class Bw2Heights
{
    public short[] Slopes { get; }
    public int[] Offsets { get; }

    public Bw2Heights(short[] slopes, int[] offsets) => (Slopes, Offsets) = (slopes, offsets);

    public static Bw2Heights Parse(string source) =>
        new(Table(source, "MAP_HEIGHT_SLOPE_TABLE").Select(v => (short)v).ToArray(), Table(source, "MAP_HEIGHT_OFFSET_TABLE").Select(v => (int)v).ToArray());

    private static long[] Table(string source, string name)
    {
        int at = source.IndexOf(name + "[]", StringComparison.Ordinal);
        if (at < 0) throw new InvalidDataException($"{name} isn't in the height source");
        int open = source.IndexOf('{', at), close = source.IndexOf("};", open, StringComparison.Ordinal);
        return Regex.Matches(source[(open + 1)..close], @"-?0x[0-9a-fA-F]+|-?\d+")
            .Select(m =>
            {
                string v = m.Value;
                bool negative = v.StartsWith('-');
                if (negative) v = v[1..];
                long n = v.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? long.Parse(v[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture) : long.Parse(v, CultureInfo.InvariantCulture);
                return negative ? -n : n;
            }).ToArray();
    }

    /// <summary>The normal of slope <paramref name="slope"/> as the original sets it: the third value turned round.</summary>
    public (int X, int Y, int Z) Normal(int slope) =>
        slope * 3 + 2 < Slopes.Length ? (Slopes[slope * 3], Slopes[slope * 3 + 1], -Slopes[slope * 3 + 2]) : (0, 4096, 0);

    public int Offset(int index) => index >= 0 && index < Offsets.Length ? Offsets[index] : 0;

    /// <summary>
    /// The height, in the original's units (sixteen to a tile), of the plane with this normal (fx16) and offset (fx32)
    /// at a point <paramref name="x"/>, <paramref name="z"/> units from the chunk's middle: <c>-(offset + n.x·x + n.z·z) / n.y</c>,
    /// the original's <c>FieldChunkAccessor_GetTerrainCore</c>.
    /// </summary>
    public static float HeightOn((int X, int Y, int Z) normal, int offset, float x, float z) =>
        normal.Y == 0 ? 0f : -(offset + normal.X * x + normal.Z * z) / normal.Y;
}

/// <summary>A tile of a terrain grid: its class, its flags and the height of the ground at its middle, in tiles.</summary>
public readonly record struct Bw2Tile(int Class, int Flags, float Height)
{
    public bool Blocked => (Flags & Bw2Behaviours.BlockedFlag) != 0 || Class is 0xFF or 0xFFFF;
}

/// <summary>
/// A chunk's terrain grid (<c>terrain_N.csv</c>): a row a tile with its shape, slope, offset, type and diagonal, and
/// for a tile cut in two the two planes in <c>terrain_N_splits.csv</c>. A few chunks (the Join Avenue's, the resort's)
/// write their normals and offsets out in full instead (<see cref="RawPlanes"/>).
/// </summary>
public sealed class Bw2Terrain
{
    public int Width { get; }
    public int Height { get; }
    public Bw2Tile[] Tiles { get; }
    /// <summary>True for a grid that writes its planes out in full rather than by the tables' indices.</summary>
    public bool RawPlanes { get; }

    public Bw2Terrain(int width, int height, Bw2Tile[] tiles, bool rawPlanes) => (Width, Height, Tiles, RawPlanes) = (width, height, tiles, rawPlanes);

    public Bw2Tile At(int x, int z) => Tiles[z * Width + x];

    public static Bw2Terrain Read(string csv, string? splitsCsv, Bw2Heights heights)
    {
        var rows = Rows(csv, out var header);
        var splits = new Dictionary<int, (int Slope0, int Slope1, int Offset0, int Offset1)>();
        if (splitsCsv != null)
            foreach (var s in Rows(splitsCsv, out var sh))
                splits[Get(s, sh, "index")] = (Get(s, sh, "slope0"), Get(s, sh, "slope1"), Get(s, sh, "offset0"), Get(s, sh, "offset1"));
        bool raw = header.ContainsKey("normal0_y");
        int width = rows.Count == 0 ? 0 : rows.Max(r => Get(r, header, "x")) + 1;
        int height = rows.Count == 0 ? 0 : rows.Max(r => Get(r, header, "z")) + 1;
        var tiles = new Bw2Tile[width * height];
        float half = width * LandData.TileUnits / 2f, halfZ = height * LandData.TileUnits / 2f;
        foreach (var r in rows)
        {
            int x = Get(r, header, "x"), z = Get(r, header, "z");
            long type = long.Parse(r[header["tile_type"]], CultureInfo.InvariantCulture);
            float ux = (x + 0.5f) * LandData.TileUnits - half, uz = (z + 0.5f) * LandData.TileUnits - halfZ;
            float units;
            if (raw)
            {
                // Two planes written out, each its normal and offset; at the tile's middle, on the diagonal, the mean
                var n0 = (Get(r, header, "normal0_x"), Get(r, header, "normal0_y"), Get(r, header, "normal0_z"));
                var n1 = (Get(r, header, "normal1_x"), Get(r, header, "normal1_y"), Get(r, header, "normal1_z"));
                units = (Bw2Heights.HeightOn(n0, Get(r, header, "offset0"), ux, uz) + Bw2Heights.HeightOn(n1, Get(r, header, "offset1"), ux, uz)) / 2f;
            }
            else
            {
                int shape = Get(r, header, "shape"), slope = Get(r, header, "slope"), offset = Get(r, header, "offset");
                if (shape == 0) units = Bw2Heights.HeightOn(heights.Normal(slope), heights.Offset(offset), ux, uz);
                else if (shape == 2 && splits.TryGetValue(offset, out var s))
                    // The middle lies on the cut, where the two planes meet: their mean
                    units = (Bw2Heights.HeightOn(heights.Normal(s.Slope0), heights.Offset(s.Offset0), ux, uz)
                        + Bw2Heights.HeightOn(heights.Normal(s.Slope1), heights.Offset(s.Offset1), ux, uz)) / 2f;
                else units = 0f;
            }
            tiles[z * width + x] = new Bw2Tile(Bw2Behaviours.ClassOf(type), Bw2Behaviours.FlagsOf(type), units / LandData.TileUnits);
        }
        return new Bw2Terrain(width, height, tiles, raw);
    }

    private static List<string[]> Rows(string csv, out Dictionary<string, int> header)
    {
        var lines = csv.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        header = new Dictionary<string, int>(StringComparer.Ordinal);
        if (lines.Count == 0) return new List<string[]>();
        var names = lines[0].Split(',');
        for (int i = 0; i < names.Length; i++) header[names[i].Trim()] = i;
        return lines.Skip(1).Select(l => l.Split(',')).ToList();
    }

    private static int Get(string[] row, Dictionary<string, int> header, string name) =>
        header.TryGetValue(name, out int i) && i < row.Length ? int.Parse(row[i], CultureInfo.InvariantCulture) : 0;
}

/// <summary>A field prop placed on a chunk: its place in the chunk (units from its middle), its turn about the up axis in degrees, and its resource.</summary>
public sealed record Bw2Prop(int Index, int Resource, Vector3 Position, float Turn);

/// <summary>
/// What the reader takes from a chunk's <c>model.glb</c>: the glTF's JSON chunk alone, never its binary one. The names
/// of the textures its materials use (each image's file name, without the folder or the extension) and the props: the
/// empty nodes named <c>bw2_prop_NNNN_id_XXXXX</c>, with their translation and rotation.
/// </summary>
public sealed record Bw2Model(IReadOnlyList<string> Textures, IReadOnlyList<Bw2Prop> Props)
{
    private static readonly Regex PropName = new(@"^bw2_prop_(\d+)_id_(\d+)$", RegexOptions.Compiled);

    /// <summary>The JSON chunk of a binary glTF, as text: the header and the first chunk's header are read, nothing after it.</summary>
    public static string JsonChunk(ReadOnlySpan<byte> glb)
    {
        if (glb.Length < 20 || BinaryPrimitives.ReadUInt32LittleEndian(glb) != 0x46546C67) throw new InvalidDataException("not a binary glTF");
        int length = (int)BinaryPrimitives.ReadUInt32LittleEndian(glb[12..]);
        if (BinaryPrimitives.ReadUInt32LittleEndian(glb[16..]) != 0x4E4F534A || 20 + length > glb.Length) throw new InvalidDataException("the glTF's first chunk isn't its JSON");
        return Encoding.UTF8.GetString(glb.Slice(20, length));
    }

    public static Bw2Model Read(ReadOnlySpan<byte> glb) => FromJson(JsonChunk(glb));

    /// <summary>Reads a file's header and JSON chunk and stops there: the binary chunk after them is never read.</summary>
    public static Bw2Model ReadFile(string path)
    {
        using var stream = File.OpenRead(path);
        var head = new byte[20];
        stream.ReadExactly(head);
        int length = (int)BinaryPrimitives.ReadUInt32LittleEndian(head.AsSpan(12));
        if (length < 0 || 20L + length > stream.Length) throw new InvalidDataException("the glTF's first chunk runs past its end");
        var glb = new byte[20 + length];
        head.CopyTo(glb, 0);
        stream.ReadExactly(glb, 20, length);
        return Read(glb);
    }

    public static Bw2Model FromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var textures = new List<string>();
        if (root.TryGetProperty("images", out var images))
            foreach (var image in images.EnumerateArray())
                if (image.TryGetProperty("uri", out var uri) && uri.GetString() is { Length: > 0 } u)
                {
                    string name = Path.GetFileNameWithoutExtension(u.Replace('\\', '/').Split('/')[^1]);
                    if (!textures.Contains(name)) textures.Add(name);
                }
        var props = new List<Bw2Prop>();
        if (root.TryGetProperty("nodes", out var nodes))
            foreach (var node in nodes.EnumerateArray())
            {
                if (!node.TryGetProperty("name", out var n) || PropName.Match(n.GetString() ?? "") is not { Success: true } m) continue;
                var t = node.TryGetProperty("translation", out var tr) ? tr.EnumerateArray().Select(v => v.GetSingle()).ToArray() : new float[3];
                var q = node.TryGetProperty("rotation", out var ro) ? ro.EnumerateArray().Select(v => v.GetSingle()).ToArray() : new[] { 0f, 0f, 0f, 1f };
                // The turn about the up axis of a rotation that has no other: 2·atan2(y, w)
                float turn = 2f * MathF.Atan2(q[1], q[3]) * 180f / MathF.PI;
                turn = (turn % 360f + 360f) % 360f;
                props.Add(new Bw2Prop(int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
                    new Vector3(t[0], t[1], t[2]), MathF.Round(turn, 2)));
            }
        return new Bw2Model(textures, props);
    }
}

/// <summary>
/// Black 2's maps from a checkout of the decompilation (plan 22 · U1): the zones, the matrices, each chunk's terrain
/// grids and its glTF's JSON chunk, the field props' names by resource, the height tables and the areas' settings.
/// Chunks are read when first asked for.
/// </summary>
public sealed class Bw2Maps
{
    public const string Overworld = "MAP_MATRIX_OVERWORLD";

    public string Directory { get; }
    public Bw2Heights Heights { get; }
    /// <summary>Every zone by its constant, in the order of <c>generated/zones.txt</c>.</summary>
    public IReadOnlyDictionary<string, Bw2Zone> Zones { get; }
    /// <summary>Every matrix by its constant, with its number from <c>generated/map_matrices.txt</c> (-1 where the list has none).</summary>
    public IReadOnlyDictionary<string, (int Id, Bw2Matrix Matrix)> Matrices { get; }
    /// <summary>The chunks' folders under <c>res/field/maps/models</c>.</summary>
    public IReadOnlyList<string> Chunks { get; }
    /// <summary>The field props' names by resource id: the folder a resource's <c>data.json</c> is in, without its <c>_N</c>.</summary>
    public IReadOnlyDictionary<int, string> PropNames { get; }
    /// <summary>Whether each area (<c>AREA_*</c>) is outdoors, from <c>res/field/areas/&lt;n&gt;.json</c>'s <c>exterior</c>.</summary>
    public IReadOnlyDictionary<string, bool> AreaOutdoors { get; }
    public List<string> Problems { get; } = new();

    private readonly Dictionary<string, Bw2Terrain?[]> terrain = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Bw2Model?> models = new(StringComparer.Ordinal);

    public Bw2Maps(string dir)
    {
        Directory = dir;
        string Text(params string[] parts) => File.ReadAllText(Path.Combine(parts.Prepend(dir).ToArray()));
        Heights = Bw2Heights.Parse(Text("src", "ov036", "field_map_chunk_height.c"));

        var zoneIds = Bw2Constants.Read(Text("generated", "zones.txt"));
        var zones = new Dictionary<string, Bw2Zone>(StringComparer.Ordinal);
        foreach (string file in System.IO.Directory.GetFiles(Path.Combine(dir, "res", "field", "zones"), "*.json").Order(StringComparer.Ordinal))
        {
            string key = Path.GetFileNameWithoutExtension(file);
            if (key.EndsWith(".schema", StringComparison.Ordinal)) continue;
            string constant = "ZONE_" + key.ToUpperInvariant();
            var zone = Bw2Zone.Parse(key, zoneIds.GetValueOrDefault(constant, -1), File.ReadAllText(file));
            if (zone.Id < 0) Problems.Add($"zone {constant} isn't in generated/zones.txt");
            zones[constant] = zone;
        }
        Zones = zones.OrderBy(z => z.Value.Id).ToDictionary(z => z.Key, z => z.Value, StringComparer.Ordinal);

        var matrixIds = Bw2Constants.Read(Text("generated", "map_matrices.txt"));
        var matrices = new Dictionary<string, (int, Bw2Matrix)>(StringComparer.Ordinal);
        foreach (string file in System.IO.Directory.GetFiles(Path.Combine(dir, "res", "field", "matrices"), "*.json").Order(StringComparer.Ordinal))
        {
            string name = Path.GetFileNameWithoutExtension(file);
            if (name.EndsWith(".schema", StringComparison.Ordinal)) continue;
            string constant = Bw2Matrix.Constant(name);
            matrices[constant] = (matrixIds.GetValueOrDefault(constant, -1), Bw2Matrix.Parse(constant, File.ReadAllText(file)));
        }
        Matrices = matrices.OrderBy(m => m.Value.Item1 < 0 ? int.MaxValue : m.Value.Item1).ThenBy(m => m.Key, StringComparer.Ordinal)
            .ToDictionary(m => m.Key, m => m.Value, StringComparer.Ordinal);
        foreach (var zone in Zones.Values)
            if (zone.Matrix.Length > 0 && !Matrices.ContainsKey(zone.Matrix)) Problems.Add($"{zone.Constant} names {zone.Matrix}, which has no file");

        Chunks = System.IO.Directory.GetDirectories(Path.Combine(dir, "res", "field", "maps", "models")).Select(Path.GetFileName).Order(StringComparer.Ordinal).ToList()!;

        // The props' names: the most common folder name of each resource, its _N taken off
        var names = new Dictionary<int, Dictionary<string, int>>();
        string props = Path.Combine(dir, "res", "field", "props");
        if (System.IO.Directory.Exists(props))
            foreach (string file in System.IO.Directory.EnumerateFiles(props, "data.json", SearchOption.AllDirectories))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(file));
                if (!doc.RootElement.TryGetProperty("res_id", out var id) || id.ValueKind != JsonValueKind.Number) continue;
                string name = Regex.Replace(Path.GetFileName(Path.GetDirectoryName(file))!, @"_\d+$", "");
                var counts = names.TryGetValue(id.GetInt32(), out var c) ? c : names[id.GetInt32()] = new Dictionary<string, int>(StringComparer.Ordinal);
                counts[name] = counts.GetValueOrDefault(name) + 1;
            }
        PropNames = names.ToDictionary(kv => kv.Key, kv => kv.Value.OrderByDescending(n => n.Value).ThenBy(n => n.Key, StringComparer.Ordinal).First().Key);

        // The areas' settings, by the areas' constants in their list's order
        var areaIds = Bw2Constants.Read(Text("generated", "areas.txt"));
        var outdoors = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var (area, id) in areaIds)
        {
            string path = Path.Combine(dir, "res", "field", "areas", id.ToString(CultureInfo.InvariantCulture) + ".json");
            if (!File.Exists(path)) continue;
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            outdoors[area] = doc.RootElement.TryGetProperty("exterior", out var e) && e.ValueKind == JsonValueKind.True;
        }
        AreaOutdoors = outdoors;
    }

    public Bw2Matrix Matrix(string constant) => Matrices[constant].Matrix;

    /// <summary>A chunk's terrain grids, by layer (most chunks have one, a few two), or null where it has none.</summary>
    public Bw2Terrain?[] Terrain(string chunk)
    {
        if (terrain.TryGetValue(chunk, out var known)) return known;
        var layers = new List<Bw2Terrain?>();
        for (int layer = 0; ; layer++)
        {
            string csv = Path.Combine(Directory, "res", "field", "maps", "models", chunk, $"terrain_{layer}.csv");
            if (!File.Exists(csv)) break;
            string splits = Path.Combine(Path.GetDirectoryName(csv)!, $"terrain_{layer}_splits.csv");
            try { layers.Add(Bw2Terrain.Read(File.ReadAllText(csv), File.Exists(splits) ? File.ReadAllText(splits) : null, Heights)); }
            catch (Exception e) when (e is FormatException or InvalidDataException or KeyNotFoundException)
            {
                Problems.Add($"{chunk}: terrain_{layer}.csv can't be read ({e.Message})");
                layers.Add(null);
            }
        }
        return terrain[chunk] = layers.ToArray();
    }

    /// <summary>What a chunk's glTF says in its JSON chunk, or null where it has no model.</summary>
    public Bw2Model? Model(string chunk)
    {
        if (models.TryGetValue(chunk, out var known)) return known;
        string path = Path.Combine(Directory, "res", "field", "maps", "models", chunk, "model.glb");
        Bw2Model? model = null;
        if (File.Exists(path))
        {
            try { model = Bw2Model.ReadFile(path); }
            catch (Exception e) when (e is InvalidDataException or JsonException or EndOfStreamException) {Problems.Add($"{chunk}: model.glb can't be read ({e.Message})"); }
        }
        return models[chunk] = model;
    }

    /// <summary>The zones whose header names a matrix, in their order.</summary>
    public IEnumerable<Bw2Zone> ZonesOn(string matrix) => Zones.Values.Where(z => z.Matrix == matrix);

    /// <summary>How a zone's picture is coloured: a cave where an Escape Rope works, its area's setting otherwise.</summary>
    public Renders.Setting SettingOf(Bw2Zone? zone) =>
        zone == null ? Renders.Setting.Outdoors
        : zone.EscapeRope ? Renders.Setting.Cave
        : zone.Matrix == Overworld || (zone.Area != null && AreaOutdoors.GetValueOrDefault(zone.Area)) ? Renders.Setting.Outdoors
        : Renders.Setting.Indoors;
}
