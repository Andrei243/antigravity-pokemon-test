using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PokemonPlatinumEngine.Data;

namespace OpenWorldBake;

// The files a person writes to describe an open region's land (plan 32 · OW2, "The world described, and baked";
// docs/data-files.md, "Open worlds"; their schemas in docs/schemas/, written by SchemaTests). Each is read strictly:
// a key no field has, a value of the wrong type or a field marked [DataRequired] left out stops the bake, naming the
// file, the JSON path and the key. Every length is in metres; x runs east, z south and y up, as OpenPos's do. A
// point on the ground is [x, z], a point in the air [x, y, z].

/// <summary><c>world.json</c>: the region's extent, its cover's rules, its areas and the maps it makes.</summary>
public sealed class OpenWorldFile
{
    /// <summary>The region's id, as its region file and the save name it.</summary>
    [DataRequired] public string Region { get; set; } = "";

    /// <summary>A chunk's side: 32, as every open map's (written so that a file never says otherwise without being refused).</summary>
    public double ChunkSize { get; set; } = 32;

    /// <summary>The chunks the region bakes: [west, north, east, south], each a chunk's index as OpenMap.ChunkOf counts it (32 m columns from nought), the last two included.</summary>
    [DataRequired] public int[] Chunks { get; set; } = Array.Empty<int>();

    /// <summary>The height where no grid of control points lies.</summary>
    public double BaseHeight { get; set; }

    /// <summary>
    /// Whether the region is ridden (plan 34 · PD2): its navigation mesh then gains the mount's links, a hop, a high
    /// jump, a climb up a climbable face and a glide off a height, which only a mounted body may take. False unless it says,
    /// so a region with no mount bakes the mesh it always had.
    /// </summary>
    public bool Mount { get; set; }

    /// <summary>The cover's rules, where the region wants other heights than the defaults.</summary>
    public CoverRulesFile? Cover { get; set; }

    /// <summary>The areas: what a place's name, music and weather are given by (OW5 reads the rest; the baker draws them with --plan).</summary>
    public List<OpenAreaFile> Areas { get; set; } = new();

    /// <summary>The open maps the region makes, each of some of its areas.</summary>
    public List<OpenMapEntryFile> Maps { get; set; } = new();
}

/// <summary>The heights the cover's rules go by.</summary>
public sealed class CoverRulesFile
{
    /// <summary>Above this height open grass is highland.</summary>
    public double? Highland { get; set; }

    /// <summary>Above this height open ground is snow.</summary>
    public double? Snow { get; set; }

    /// <summary>How far over the water its shore is sand (0.6 m unless it says).</summary>
    public double? Shore { get; set; }
}

/// <summary>An area: a polygon on the ground between two heights, so a cave under a hill is an area of its own.</summary>
public sealed class OpenAreaFile
{
    [DataRequired] public string Key { get; set; } = "";
    public string? Name { get; set; }
    [DataRequired] public double[][] Polygon { get; set; } = Array.Empty<double[]>();
    public double MinY { get; set; } = -1000;
    public double MaxY { get; set; } = 10000;
    public string? Music { get; set; }
    public string? Weather { get; set; }
    /// <summary>A light of the place's own (plan 33 · GL6), the game's alone: the baker passes it by.</summary>
    public string? Light { get; set; }
    public string? LightWhile { get; set; }
}

/// <summary>An open map of the region: its name (which no grid map may have) and its areas.</summary>
public sealed class OpenMapEntryFile
{
    [DataRequired] public string Name { get; set; } = "";
    public List<string> Areas { get; set; } = new();
}

/// <summary><c>terrain/*.json</c>, read in the order of their names: grids of control points, brushes and paint.</summary>
public sealed class TerrainFile
{
    /// <summary>A grid of control points; a column takes its height from the first file's grid that holds it.</summary>
    public ControlGridFile? Grid { get; set; }

    /// <summary>Laid on the heights in the order written, file after file.</summary>
    public List<BrushFile> Brushes { get; set; } = new();

    /// <summary>A cover painted over the rules (the first paint that holds a vertex wins).</summary>
    public List<PaintFile> Paint { get; set; } = new();
}

/// <summary>Control points: a row of heights for each row of points, north to south, each west to east, read between them by Catmull-Rom.</summary>
public sealed class ControlGridFile
{
    /// <summary>The north-west point's place, [x, z].</summary>
    [DataRequired] public double[] Origin { get; set; } = Array.Empty<double>();

    /// <summary>The distance between two points, across and down.</summary>
    [DataRequired] public double Spacing { get; set; }

    [DataRequired] public double[][] Heights { get; set; } = Array.Empty<double[]>();
}

/// <summary>A brush: <c>"kind"</c> says which, and its fields are that kind's.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(DomeBrushFile), "dome")]
[JsonDerivedType(typeof(RidgeBrushFile), "ridge")]
[JsonDerivedType(typeof(PlateauBrushFile), "plateau")]
[JsonDerivedType(typeof(CraterBrushFile), "crater")]
[JsonDerivedType(typeof(ChannelBrushFile), "channel")]
public abstract class BrushFile
{
    /// <summary>A name for the bake's messages.</summary>
    public string? Name { get; set; }
}

/// <summary>A round hill: raised by <see cref="Rise"/> at its middle, to nought at its radius.</summary>
public sealed class DomeBrushFile : BrushFile
{
    [DataRequired] public double[] At { get; set; } = Array.Empty<double>();
    [DataRequired] public double Radius { get; set; }
    [DataRequired] public double Rise { get; set; }
}

/// <summary>A ridge along a line: raised by <see cref="Rise"/> on the line, to nought at half its width either side.</summary>
public sealed class RidgeBrushFile : BrushFile
{
    [DataRequired] public double[][] Points { get; set; } = Array.Empty<double[]>();
    [DataRequired] public double Width { get; set; }
    [DataRequired] public double Rise { get; set; }
}

/// <summary>A plateau: level at <see cref="Height"/> inside its polygon, eased into the land over its falloff outside.</summary>
public sealed class PlateauBrushFile : BrushFile
{
    [DataRequired] public double[][] Polygon { get; set; } = Array.Empty<double[]>();
    [DataRequired] public double Height { get; set; }
    public double Falloff { get; set; } = 8;
}

/// <summary>A crater: a bowl down by <see cref="Depth"/> at its middle, a rim raised by <see cref="Rim"/> at its radius.</summary>
public sealed class CraterBrushFile : BrushFile
{
    [DataRequired] public double[] At { get; set; } = Array.Empty<double>();
    [DataRequired] public double Radius { get; set; }
    [DataRequired] public double Depth { get; set; }
    public double Rim { get; set; }
}

/// <summary>A river's bed: lowered by <see cref="Depth"/> on its line, to nought at half its width either side.</summary>
public sealed class ChannelBrushFile : BrushFile
{
    [DataRequired] public double[][] Points { get; set; } = Array.Empty<double[]>();
    [DataRequired] public double Width { get; set; }
    [DataRequired] public double Depth { get; set; }
}

/// <summary>A cover painted inside a polygon, or a circle (<see cref="At"/> and <see cref="Radius"/>), between two heights.</summary>
public sealed class PaintFile
{
    [DataRequired] public Cover Cover { get; set; }
    public double[][]? Polygon { get; set; }
    public double[]? At { get; set; }
    public double Radius { get; set; }
    public double MinY { get; set; } = -1000;
    public double MaxY { get; set; } = 10000;
}

/// <summary><c>carve/*.json</c>: what is cut out of the land (air) and what is added to it (rock).</summary>
public sealed class CarveFile
{
    public List<ShapeFile> Carves { get; set; } = new();
    public List<ShapeFile> Additions { get; set; } = new();
}

/// <summary>A signed-distance shape: <c>"kind"</c> says which.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(CapsulesShapeFile), "capsules")]
[JsonDerivedType(typeof(EllipsoidShapeFile), "ellipsoid")]
[JsonDerivedType(typeof(BoxShapeFile), "box")]
[JsonDerivedType(typeof(PillarShapeFile), "pillar")]
public abstract class ShapeFile
{
    public string? Name { get; set; }
}

/// <summary>A chain of capsules along points in the air (a tunnel, a rock bridge), with a flat floor if it says.</summary>
public sealed class CapsulesShapeFile : ShapeFile
{
    [DataRequired] public double[][] Points { get; set; } = Array.Empty<double[]>();
    [DataRequired] public double Radius { get; set; }
    public double? Floor { get; set; }
}

/// <summary>An ellipsoid (a cavern, a boulder), with a flat floor if it says.</summary>
public sealed class EllipsoidShapeFile : ShapeFile
{
    [DataRequired] public double[] Centre { get; set; } = Array.Empty<double>();
    [DataRequired] public double[] Radii { get; set; } = Array.Empty<double>();
    public double? Floor { get; set; }
}

/// <summary>A box between two corners, its edges rounded by <see cref="Round"/>.</summary>
public sealed class BoxShapeFile : ShapeFile
{
    [DataRequired] public double[] Min { get; set; } = Array.Empty<double>();
    [DataRequired] public double[] Max { get; set; } = Array.Empty<double>();
    public double Round { get; set; }
}

/// <summary>An upright cylinder (a sea stack, a shaft) from its bottom to its top.</summary>
public sealed class PillarShapeFile : ShapeFile
{
    [DataRequired] public double[] At { get; set; } = Array.Empty<double>();
    [DataRequired] public double Radius { get; set; }
    [DataRequired] public double Bottom { get; set; }
    [DataRequired] public double Top { get; set; }
}

/// <summary><c>water.json</c>: the sea, lakes and rivers as surfaces at heights.</summary>
public sealed class WaterFile
{
    public SeaFile? Sea { get; set; }
    public List<LakeFile> Lakes { get; set; } = new();
    public List<RiverFile> Rivers { get; set; } = new();
}

/// <summary>The sea at its level, everywhere or inside its polygon.</summary>
public sealed class SeaFile
{
    [DataRequired] public double Level { get; set; }
    public double[][]? Polygon { get; set; }
}

/// <summary>A lake at its level inside its polygon.</summary>
public sealed class LakeFile
{
    [DataRequired] public string Name { get; set; } = "";
    [DataRequired] public double[][] Polygon { get; set; } = Array.Empty<double[]>();
    [DataRequired] public double Level { get; set; }
}

/// <summary>A river along points in the air, [x, y, z], y its surface there, as wide as it says.</summary>
public sealed class RiverFile
{
    [DataRequired] public string Name { get; set; } = "";
    [DataRequired] public double[][] Points { get; set; } = Array.Empty<double[]>();
    [DataRequired] public double Width { get; set; }
}

/// <summary><c>roads.json</c>: roads graded along their splines.</summary>
public sealed class RoadsFile
{
    public List<RoadFile> Roads { get; set; } = new();
}

/// <summary>
/// A road: its points [x, z] (graded at the land's height there) or [x, y, z] (at y), its width, its shoulders (where
/// it is eased back into the land) and its surface.
/// </summary>
public sealed class RoadFile
{
    [DataRequired] public string Name { get; set; } = "";
    [DataRequired] public double[][] Points { get; set; } = Array.Empty<double[]>();
    [DataRequired] public double Width { get; set; }
    public double Shoulder { get; set; } = 3;
    public Cover Surface { get; set; } = Cover.Path;
}

/// <summary>
/// A region's description as read from its folder (plan 32 · OW2): every file strictly, then held to what the format
/// can't say (a point's length, a grid's rows), and turned into the land the baker meshes. A finding names the file.
/// </summary>
public sealed class RegionDescription
{
    public required string Folder { get; init; }
    public required OpenWorldFile World { get; init; }
    public List<(string File, TerrainFile Terrain)> Terrain { get; } = new();
    public List<(string File, CarveFile Carve)> Carves { get; } = new();
    public WaterFile? Water { get; set; }
    public RoadsFile? Roads { get; set; }

    /// <summary>A hash of every description's text (FNV-1a, carriage returns passed over, files in the order of their names).</summary>
    public required string Source { get; init; }

    static readonly JsonSerializerOptions Options = new(GameDataFiles.Strict) { AllowOutOfOrderMetadataProperties = true };

    /// <summary>The files a description folder holds, as paths relative to it, in the order they are read.</summary>
    public static List<string> FilesIn(string folder)
    {
        var files = new List<string> { "world.json" };
        foreach (string sub in new[] { "terrain", "carve" })
            if (Directory.Exists(Path.Combine(folder, sub)))
                files.AddRange(Directory.GetFiles(Path.Combine(folder, sub), "*.json").Select(f => sub + "/" + Path.GetFileName(f)).OrderBy(f => f, StringComparer.Ordinal));
        foreach (string single in new[] { "water.json", "roads.json" })
            if (File.Exists(Path.Combine(folder, single))) files.Add(single);
        return files;
    }

    public static RegionDescription Read(string folder)
    {
        if (!File.Exists(Path.Combine(folder, "world.json"))) throw new InvalidDataException($"{folder}: no world.json");
        var texts = FilesIn(folder).Select(f => (File: f, Text: File.ReadAllText(Path.Combine(folder, f)))).ToList();
        var world = Parse<OpenWorldFile>(texts[0].Text, "world.json");
        var description = new RegionDescription { Folder = folder, World = world, Source = Hash(texts) };
        foreach (var (file, text) in texts.Skip(1))
        {
            if (file.StartsWith("terrain/", StringComparison.Ordinal)) description.Terrain.Add((file, Parse<TerrainFile>(text, file)));
            else if (file.StartsWith("carve/", StringComparison.Ordinal)) description.Carves.Add((file, Parse<CarveFile>(text, file)));
            else if (file == "water.json") description.Water = Parse<WaterFile>(text, file);
            else if (file == "roads.json") description.Roads = Parse<RoadsFile>(text, file);
        }
        description.Validate();
        return description;
    }

    static T Parse<T>(string text, string file)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(text, Options) ?? throw new InvalidDataException($"{file}: the file is empty");
        }
        catch (JsonException e)
        {
            throw new InvalidDataException(GameDataFiles.Finding(file, e), e);
        }
    }

    static string Hash(IEnumerable<(string File, string Text)> texts)
    {
        ulong h = 14695981039346656037UL;
        void Add(string s)
        {
            foreach (byte b in Encoding.UTF8.GetBytes(s))
            {
                if (b == (byte)'\r') continue;
                h ^= b;
                h *= 1099511628211UL;
            }
        }
        foreach (var (file, text) in texts) { Add(file); Add("\n"); Add(text); Add("\n"); }
        return h.ToString("x16");
    }

    // ---------------------------------------------------------------- what the format can't say

    void Validate()
    {
        var w = World;
        if (string.IsNullOrWhiteSpace(w.Region)) Fail("world.json", "$.region", "a region has an id");
        if (w.ChunkSize != 32) Fail("world.json", "$.chunkSize", $"an open map's chunks are 32 m, not {w.ChunkSize}");
        if (w.Chunks.Length != 4 || w.Chunks[2] < w.Chunks[0] || w.Chunks[3] < w.Chunks[1])
            Fail("world.json", "$.chunks", "is [west, north, east, south], east no less than west and south no less than north");
        var keys = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < w.Areas.Count; i++)
        {
            if (!keys.Add(w.Areas[i].Key)) Fail("world.json", $"$.areas[{i}].key", $"two areas are keyed {w.Areas[i].Key}");
            Polygon(w.Areas[i].Polygon, "world.json", $"$.areas[{i}].polygon");
        }
        for (int m = 0; m < w.Maps.Count; m++)
            for (int a = 0; a < w.Maps[m].Areas.Count; a++)
                if (!keys.Contains(w.Maps[m].Areas[a])) Fail("world.json", $"$.maps[{m}].areas[{a}]", $"no area is keyed {w.Maps[m].Areas[a]}");

        foreach (var (file, t) in Terrain)
        {
            if (t.Grid is { } g)
            {
                Point(g.Origin, 2, file, "$.grid.origin");
                if (g.Spacing <= 0) Fail(file, "$.grid.spacing", "the points' spacing is more than nought");
                if (g.Heights.Length < 2 || g.Heights.Any(r => r.Length != g.Heights[0].Length) || g.Heights[0].Length < 2)
                    Fail(file, "$.grid.heights", "two or more rows of two or more heights, every row as long");
            }
            for (int i = 0; i < t.Brushes.Count; i++)
            {
                string at = $"$.brushes[{i}]";
                switch (t.Brushes[i])
                {
                    case DomeBrushFile d: Point(d.At, 2, file, at + ".at"); Positive(d.Radius, file, at + ".radius"); break;
                    case RidgeBrushFile r: Line(r.Points, 2, file, at + ".points"); Positive(r.Width, file, at + ".width"); break;
                    case PlateauBrushFile p: Polygon(p.Polygon, file, at + ".polygon"); if (p.Falloff < 0) Fail(file, at + ".falloff", "is no less than nought"); break;
                    case CraterBrushFile c: Point(c.At, 2, file, at + ".at"); Positive(c.Radius, file, at + ".radius"); break;
                    case ChannelBrushFile c: Line(c.Points, 2, file, at + ".points"); Positive(c.Width, file, at + ".width"); break;
                }
            }
            for (int i = 0; i < t.Paint.Count; i++)
            {
                var p = t.Paint[i];
                if ((p.Polygon == null) == (p.At == null)) Fail(file, $"$.paint[{i}]", "a paint has a polygon or a circle (at and radius), not both");
                if (p.Polygon != null) Polygon(p.Polygon, file, $"$.paint[{i}].polygon");
                if (p.At != null) { Point(p.At, 2, file, $"$.paint[{i}].at"); Positive(p.Radius, file, $"$.paint[{i}].radius"); }
            }
        }
        foreach (var (file, c) in Carves)
            foreach (var (list, name) in new[] { (c.Carves, "carves"), (c.Additions, "additions") })
                for (int i = 0; i < list.Count; i++)
                {
                    string at = $"$.{name}[{i}]";
                    switch (list[i])
                    {
                        case CapsulesShapeFile s: Line(s.Points, 3, file, at + ".points", allowOne: true); Positive(s.Radius, file, at + ".radius"); break;
                        case EllipsoidShapeFile s: Point(s.Centre, 3, file, at + ".centre"); Point(s.Radii, 3, file, at + ".radii"); if (s.Radii.Any(r => r <= 0)) Fail(file, at + ".radii", "each radius is more than nought"); break;
                        case BoxShapeFile s: Point(s.Min, 3, file, at + ".min"); Point(s.Max, 3, file, at + ".max"); if (s.Min.Zip(s.Max).Any(p => p.Second <= p.First)) Fail(file, at, "max is more than min on every axis"); break;
                        case PillarShapeFile s: Point(s.At, 2, file, at + ".at"); Positive(s.Radius, file, at + ".radius"); if (s.Top <= s.Bottom) Fail(file, at, "the top is over the bottom"); break;
                    }
                }
        if (Water is { } water)
        {
            if (water.Sea?.Polygon is { } sea) Polygon(sea, "water.json", "$.sea.polygon");
            for (int i = 0; i < water.Lakes.Count; i++) Polygon(water.Lakes[i].Polygon, "water.json", $"$.lakes[{i}].polygon");
            for (int i = 0; i < water.Rivers.Count; i++) { Line(water.Rivers[i].Points, 3, "water.json", $"$.rivers[{i}].points"); Positive(water.Rivers[i].Width, "water.json", $"$.rivers[{i}].width"); }
        }
        if (Roads is { } roads)
            for (int i = 0; i < roads.Roads.Count; i++)
            {
                var r = roads.Roads[i];
                if (r.Points.Length < 2) Fail("roads.json", $"$.roads[{i}].points", "a road has two points or more");
                for (int p = 0; p < r.Points.Length; p++)
                    if (r.Points[p].Length is not (2 or 3)) Fail("roads.json", $"$.roads[{i}].points[{p}]", "a road's point is [x, z] or [x, y, z]");
                Positive(r.Width, "roads.json", $"$.roads[{i}].width");
                if (r.Shoulder < 0) Fail("roads.json", $"$.roads[{i}].shoulder", "is no less than nought");
            }
    }

    static void Fail(string file, string path, string message) => throw new InvalidDataException($"{file}: {path}: {message}");
    static void Positive(double value, string file, string path) { if (!(value > 0)) Fail(file, path, "is more than nought"); }
    static void Point(double[] p, int length, string file, string path) { if (p.Length != length) Fail(file, path, length == 2 ? "a point on the ground is [x, z]" : "a point in the air is [x, y, z]"); }
    static void Line(double[][] points, int length, string file, string path, bool allowOne = false)
    {
        if (points.Length < (allowOne ? 1 : 2)) Fail(file, path, allowOne ? "a line has a point or more" : "a line has two points or more");
        for (int i = 0; i < points.Length; i++) Point(points[i], length, file, $"{path}[{i}]");
    }
    static void Polygon(double[][] points, string file, string path)
    {
        if (points.Length < 3) Fail(file, path, "a polygon has three points or more");
        for (int i = 0; i < points.Length; i++) Point(points[i], 2, file, $"{path}[{i}]");
    }

    // ---------------------------------------------------------------- the land

    static P2 Ground(double[] p) => new(p[0], p[^1]);
    static List<P2> Ground(double[][] points) => points.Select(Ground).ToList();
    static D3 Air(double[] p) => new(p[0], p[1], p[2]);

    /// <summary>The land the description makes, in the region's own chunk numbers (local chunk 0 is <c>chunks[0]</c>, <c>chunks[1]</c>).</summary>
    public TerrainWorld Land()
    {
        var w = World;
        var land = new TerrainWorld
        {
            Name = w.Region,
            ChunksX = w.Chunks[2] - w.Chunks[0] + 1,
            ChunksZ = w.Chunks[3] - w.Chunks[1] + 1,
            MinX = w.Chunks[0] * 32.0,
            MinZ = w.Chunks[1] * 32.0,
            BaseHeight = w.BaseHeight,
            HighlandAbove = w.Cover?.Highland ?? double.PositiveInfinity,
            SnowAbove = w.Cover?.Snow ?? double.PositiveInfinity,
            Shore = w.Cover?.Shore ?? 0.6
        };
        foreach (var (_, t) in Terrain)
        {
            if (t.Grid is { } g)
            {
                var heights = g.Heights;
                land.Grids.Add(new ControlGrid(g.Origin[0], g.Origin[1], g.Spacing, heights[0].Length, heights.Length, (i, j, _, _) => heights[j][i]));
            }
            foreach (var brush in t.Brushes)
                land.Brushes.Add(brush switch
                {
                    DomeBrushFile d => new Dome(d.At[0], d.At[1], d.Radius, d.Rise),
                    RidgeBrushFile r => new Ridge(Ground(r.Points), r.Width, r.Rise),
                    PlateauBrushFile p => new Plateau(Ground(p.Polygon), p.Height, p.Falloff),
                    CraterBrushFile c => new Crater(c.At[0], c.At[1], c.Radius, c.Depth, c.Rim),
                    ChannelBrushFile c => new Channel(Ground(c.Points), c.Width, c.Depth),
                    _ => throw new InvalidDataException($"a brush of kind {brush.GetType().Name}")
                });
            foreach (var p in t.Paint)
                land.Paint.Add(new Paint(p.Cover, p.Polygon is { } poly ? Ground(poly) : null, p.At is { } at ? Ground(at) : null, p.Radius, p.MinY, p.MaxY));
        }
        // The roads are graded at the land's height under them, brushes and all, before any road is laid
        if (Roads is { } roads)
            foreach (var r in roads.Roads)
                land.GradedRoads.Add(new GradedRoad(r.Name, r.Points.Select(Ground).ToList(), r.Points.Select(p => p.Length == 3 ? p[1] : (double?)null).ToList(),
                    r.Width, r.Shoulder, r.Surface, p => land.BaseAt(p.X, p.Z)));
        foreach (var (_, c) in Carves)
        {
            land.Carves.AddRange(c.Carves.Select(Shape));
            land.Additions.AddRange(c.Additions.Select(Shape));
        }
        if (Water is { } water)
        {
            if (water.Sea is { } sea) land.Water.Add(new Sea("sea", sea.Level, sea.Polygon is { } poly ? Ground(poly) : null));
            foreach (var lake in water.Lakes) land.Water.Add(new Lake(lake.Name, Ground(lake.Polygon), lake.Level));
            foreach (var river in water.Rivers) land.Water.Add(new River(river.Name, Ground(river.Points), river.Points.Select(p => p[1]).ToList(), river.Width));
        }
        return land;
    }

    static ICarve Shape(ShapeFile shape) => shape switch
    {
        CapsulesShapeFile s => new Tunnel(s.Points.Select(Air).ToArray(), s.Radius, s.Floor ?? Tunnel.NoFloor),
        EllipsoidShapeFile s => new Cavern(Air(s.Centre), Air(s.Radii), s.Floor ?? Tunnel.NoFloor),
        BoxShapeFile s => new Block(Air(s.Min), Air(s.Max), s.Round),
        PillarShapeFile s => new Pillar(s.At[0], s.At[1], s.Radius, s.Bottom, s.Top),
        _ => throw new InvalidDataException($"a shape of kind {shape.GetType().Name}")
    };
}
