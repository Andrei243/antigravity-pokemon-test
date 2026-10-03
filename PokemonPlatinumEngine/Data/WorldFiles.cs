using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// What a tile of the imported world looks like: what its ground is made of, or what stands on it. The tile's
/// <see cref="TileBehavior"/> says what it does; this says what to draw there.
/// </summary>
public enum TerrainCover
{
    Unknown,
    Grass,
    Flowers,
    TallGrass,
    /// <summary>Bare earth, a sandy track, gravel.</summary>
    Path,
    /// <summary>The made ground of towns and cities: flagstones, tiles, asphalt.</summary>
    Paving,
    Sand,
    /// <summary>Bare rock underfoot.</summary>
    Rock,
    CaveFloor,
    Snow,
    Ice,
    Marsh,
    Water,
    /// <summary>The planks of a bridge or a pier.</summary>
    Bridge,
    Steps,
    Tree,
    /// <summary>A rock face.</summary>
    Cliff,
    /// <summary>A rock standing on the ground or in water.</summary>
    Boulder,
    /// <summary>A fence, a railing, a lamp, a low wall.</summary>
    Fence,
    /// <summary>Under one of the chunk's props: a building, usually.</summary>
    Building
}

/// <summary>The one-character codes of <see cref="TerrainCover"/> in chunk files.</summary>
public static class TerrainCoverCodes
{
    private static readonly (TerrainCover Cover, char Code)[] Table =
    {
        (TerrainCover.Unknown, '?'), (TerrainCover.Grass, '.'), (TerrainCover.Flowers, '*'), (TerrainCover.TallGrass, 'w'),
        (TerrainCover.Path, ':'), (TerrainCover.Paving, '='), (TerrainCover.Sand, ','), (TerrainCover.Rock, 'r'),
        (TerrainCover.CaveFloor, 'c'), (TerrainCover.Snow, '^'), (TerrainCover.Ice, 'i'), (TerrainCover.Marsh, 'm'),
        (TerrainCover.Water, '~'), (TerrainCover.Bridge, 'b'), (TerrainCover.Steps, 's'), (TerrainCover.Tree, 'T'),
        (TerrainCover.Cliff, 'C'), (TerrainCover.Boulder, 'R'), (TerrainCover.Fence, 'F'), (TerrainCover.Building, 'B')
    };

    public static char CodeOf(TerrainCover cover) => Table.First(e => e.Cover == cover).Code;

    public static TerrainCover Parse(char code)
    {
        foreach (var (cover, c) in Table)
            if (c == code) return cover;
        throw new InvalidDataException($"Unknown terrain cover code '{code}'.");
    }

    public static IEnumerable<TerrainCover> All => Table.Select(e => e.Cover);
}

/// <summary>
/// One 32×32-tile chunk of the imported world (<c>chunks/NNN.json</c>). Rows run north to south, tiles west to
/// east. <c>tools/MapImporter</c> writes these files; <c>docs/data-files.md</c> describes them.
/// </summary>
public sealed class WorldChunkFile
{
    public const int Tiles = 32;

    public int Id { get; set; }

    /// <summary>What each tile does: two hexadecimal digits per tile, a <see cref="TileBehavior"/> value.</summary>
    public List<string> Behaviours { get; set; } = new();

    /// <summary>Blocked tiles: '#' is solid, '.' is open.</summary>
    public List<string> Solid { get; set; } = new();

    /// <summary>What each tile looks like: one <see cref="TerrainCoverCodes"/> character per tile.</summary>
    public List<string> Cover { get; set; } = new();

    /// <summary>The ground's height, as rectangles of flat or sloping ground; see <see cref="HeightPlate"/>.</summary>
    public List<HeightPlate> Heights { get; set; } = new();

    public List<ChunkProp> Props { get; set; } = new();

    public TileBehavior BehaviourAt(int x, int z) =>
        (TileBehavior)byte.Parse(Behaviours[z].AsSpan(x * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    public bool SolidAt(int x, int z) => Solid[z][x] == '#';

    public TerrainCover CoverAt(int x, int z) => TerrainCoverCodes.Parse(Cover[z][x]);

    /// <summary>
    /// The height of the ground at a point (in tiles from the chunk's north-west corner) for a walker who is at
    /// height <paramref name="from"/>: where plates overlap, as under a bridge, the nearest one. Null where the
    /// chunk has no ground.
    /// </summary>
    public float? HeightAt(float x, float z, float from = 0f)
    {
        float? best = null;
        foreach (var plate in Heights)
        {
            if (!plate.Contains(x, z)) continue;
            float h = plate.HeightAt(x, z);
            if (best == null || MathF.Abs(h - from) < MathF.Abs(best.Value - from)) best = h;
        }
        return best;
    }

    /// <summary>Checks the grids' sizes and codes, so a damaged file stops loading with its fault named.</summary>
    public void Validate()
    {
        void Rows(List<string> rows, string layer, int width)
        {
            if (rows.Count != Tiles || rows.Any(r => r.Length != width))
                throw new InvalidDataException($"Chunk {Id}: {layer} must be {Tiles} rows of {width} characters.");
        }
        Rows(Behaviours, "behaviours", Tiles * 2);
        Rows(Solid, "solid", Tiles);
        Rows(Cover, "cover", Tiles);
        for (int z = 0; z < Tiles; z++)
            for (int x = 0; x < Tiles; x++)
            {
                if (!byte.TryParse(Behaviours[z].AsSpan(x * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
                    throw new InvalidDataException($"Chunk {Id}: behaviours row {z} has '{Behaviours[z].Substring(x * 2, 2)}' at tile {x}.");
                if (Solid[z][x] != '#' && Solid[z][x] != '.')
                    throw new InvalidDataException($"Chunk {Id}: solid row {z} has '{Solid[z][x]}' at tile {x}; use '#' or '.'.");
                TerrainCoverCodes.Parse(Cover[z][x]);
            }
    }
}

/// <summary>
/// A rectangle of ground and the plane it lies in, in tiles from the chunk's north-west corner: the ground is
/// <see cref="Height"/> tiles high at the rectangle's north-west corner and rises by <see cref="SlopeX"/> per tile
/// eastward and <see cref="SlopeZ"/> per tile southward. Flat ground has no slopes; stairs and ramps have one.
/// </summary>
[JsonConverter(typeof(OneLine<HeightPlate>))]
public sealed class HeightPlate
{
    public float X { get; set; }
    public float Z { get; set; }
    public float Width { get; set; }
    public float Depth { get; set; }
    public float Height { get; set; }
    public float SlopeX { get; set; }
    public float SlopeZ { get; set; }

    public bool Contains(float x, float z) => x >= X && x <= X + Width && z >= Z && z <= Z + Depth;

    public float HeightAt(float x, float z) => Height + (x - X) * SlopeX + (z - Z) * SlopeZ;
}

/// <summary>
/// Something standing on a chunk: a building, a signboard, a piece of furniture. <see cref="Model"/> is the
/// original's id for it and <see cref="Name"/> its short name, which is how the game decides what to build there;
/// the box is the space it takes, in tiles (heights in tiles too), from the chunk's north-west corner.
/// </summary>
[JsonConverter(typeof(OneLine<ChunkProp>))]
public sealed class ChunkProp
{
    public int Model { get; set; }
    public string Name { get; set; } = "";

    /// <summary>The prop's own origin, usually the middle of its base.</summary>
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    /// <summary>The north-west corner of the space it takes, and that space's size.</summary>
    public float BoxX { get; set; }
    public float BoxZ { get; set; }
    public float Width { get; set; }
    public float Depth { get; set; }
    public float Height { get; set; }
}

/// <summary>
/// A grid of chunks (<c>matrices/NNN.json</c>): the overworld is matrix 0; a cave floor or the inside of a
/// building is a small matrix of its own.
/// </summary>
public sealed class WorldMatrixFile
{
    public const int NoChunk = -1;

    public int Id { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>Chunk ids, one row of the grid per string, separated by spaces; <c>-</c> where there is none.</summary>
    public List<string> Chunks { get; set; } = new();

    /// <summary>The area each chunk belongs to (an index into <see cref="AreaKeys"/>), in the same layout; left
    /// out when the whole matrix belongs to the one area that uses it.</summary>
    public List<string>? Areas { get; set; }

    /// <summary>The areas the grid refers to.</summary>
    public List<string>? AreaKeys { get; set; }

    /// <summary>How high each chunk sits, in half tiles, in the same layout; left out when all sit at zero.</summary>
    public List<string>? Altitudes { get; set; }

    public int ChunkAt(int x, int y) => Cell(Chunks, x, y) is "-" ? NoChunk : int.Parse(Cell(Chunks, x, y), CultureInfo.InvariantCulture);

    public string? AreaAt(int x, int y) =>
        Areas == null || AreaKeys == null || Cell(Areas, x, y) is "-" ? null : AreaKeys[int.Parse(Cell(Areas, x, y), CultureInfo.InvariantCulture)];

    public int AltitudeAt(int x, int y) => Altitudes == null ? 0 : int.Parse(Cell(Altitudes, x, y), CultureInfo.InvariantCulture);

    private string Cell(List<string> rows, int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) throw new ArgumentOutOfRangeException(nameof(x), $"Matrix {Id} has no cell {x},{y}.");
        var cells = rows[y].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (cells.Length != Width) throw new InvalidDataException($"Matrix {Id}: row {y} has {cells.Length} cells, expected {Width}.");
        return cells[x];
    }

    /// <summary>One row of a grid as it is stored: cells padded to one width, so the rows line up.</summary>
    public static string Row(IEnumerable<string> cells, int cellWidth)
    {
        var row = new StringBuilder();
        foreach (string cell in cells)
        {
            if (row.Length > 0) row.Append(' ');
            row.Append(cell.PadLeft(cellWidth));
        }
        return row.ToString();
    }
}

/// <summary>
/// One area (<c>areas/&lt;key&gt;.json</c>): a town, a route, a cave floor, a room. It says which matrix it lies on,
/// what it is called and how it behaves, and lists what is placed on it. Positions are tiles of its matrix (for
/// the overworld: tiles of the whole region).
/// </summary>
public sealed class WorldAreaFile
{
    /// <summary>The area's key: <c>twinleaf_town</c>.</summary>
    public string Key { get; set; } = "";

    /// <summary>The original's number for the area; warps and saved positions in the original's data use it.</summary>
    public int Index { get; set; }

    /// <summary>The name a player sees.</summary>
    public string Name { get; set; } = "";

    public int Matrix { get; set; }

    /// <summary>Town, Outdoors, Cave, Indoors, PokemonCenter or Underground.</summary>
    public string Kind { get; set; } = "";

    /// <summary>The style of the sign that names the area on arrival: City, Town, Route, Cave, Forest, Water, Park, Lake, Indoors or None.</summary>
    public string Sign { get; set; } = "";

    public string Weather { get; set; } = "";
    public string Camera { get; set; } = "";
    public string BattleBackground { get; set; } = "";

    /// <summary>The roles of the area's music by day and by night, as the original names them (a checklist for plan 05).</summary>
    public string DayMusic { get; set; } = "";
    public string NightMusic { get; set; } = "";

    /// <summary>The name of the area's table of wild Pokémon; left out when it has none.</summary>
    public string? Encounters { get; set; }

    public bool Bike { get; set; }
    public bool Running { get; set; }
    public bool EscapeRope { get; set; }
    public bool Fly { get; set; }

    public List<AreaWarp> Warps { get; set; } = new();
    public List<AreaObject> Objects { get; set; } = new();
    public List<AreaSign> Signs { get; set; } = new();
    public List<AreaTrigger> Triggers { get; set; } = new();
}

/// <summary>A tile that leads elsewhere: to the warp numbered <see cref="ToWarp"/> of the area <see cref="To"/>.</summary>
[JsonConverter(typeof(OneLine<AreaWarp>))]
public sealed class AreaWarp
{
    public int X { get; set; }
    public int Z { get; set; }
    public string To { get; set; } = "";
    public int ToWarp { get; set; }
}

/// <summary>A person or a thing standing on the area: what it looks like, how it moves and which script it runs.</summary>
[JsonConverter(typeof(OneLine<AreaObject>))]
public sealed class AreaObject
{
    public string Id { get; set; } = "";
    public string Looks { get; set; } = "";
    public string Movement { get; set; } = "";
    public int X { get; set; }
    public int Z { get; set; }
    public int Facing { get; set; }
    public int RangeX { get; set; }
    public int RangeZ { get; set; }

    /// <summary>For a trainer: how they watch for the player; left out for everyone else.</summary>
    public string? Trainer { get; set; }

    /// <summary>The flag that hides the object while set; left out when it is always there.</summary>
    public string? HiddenBy { get; set; }

    public string Script { get; set; } = "";
}

/// <summary>Something read or found by facing a tile: a signboard, a hidden item.</summary>
[JsonConverter(typeof(OneLine<AreaSign>))]
public sealed class AreaSign
{
    public int X { get; set; }
    public int Z { get; set; }
    public int Type { get; set; }
    public string Script { get; set; } = "";
}

/// <summary>A rectangle that starts a script when stepped on while a story variable has a value.</summary>
[JsonConverter(typeof(OneLine<AreaTrigger>))]
public sealed class AreaTrigger
{
    public int X { get; set; }
    public int Z { get; set; }
    public int Width { get; set; } = 1;
    public int Depth { get; set; } = 1;
    public string Script { get; set; } = "";
    public string Variable { get; set; } = "";
    public string Value { get; set; } = "";
}

/// <summary>
/// Writes a small record on one line, so a chunk's hundred height plates take a hundred lines instead of nine
/// hundred. Reading is the ordinary reading.
/// </summary>
public sealed class OneLine<T> : JsonConverter<T> where T : class, new()
{
    private static readonly JsonSerializerOptions Inner = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public override T Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var value = new T();
        foreach (var property in typeof(T).GetProperties().Where(p => p.CanWrite))
        {
            if (!doc.RootElement.TryGetProperty(JsonNamingPolicy.CamelCase.ConvertName(property.Name), out var element)) continue;
            property.SetValue(value, element.Deserialize(property.PropertyType, Inner));
        }
        return value;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        var text = new StringBuilder("{ ");
        bool first = true;
        foreach (var property in typeof(T).GetProperties().Where(p => p.CanRead && p.CanWrite))
        {
            object? field = property.GetValue(value);
            if (field == null) continue;
            if (!first) text.Append(", ");
            first = false;
            text.Append('"').Append(JsonNamingPolicy.CamelCase.ConvertName(property.Name)).Append("\": ");
            text.Append(JsonSerializer.Serialize(field, property.PropertyType, Inner));
        }
        text.Append(" }");
        // The writer puts raw values straight after one another, so each record brings its own line break
        if (writer.Options.Indented) text.Insert(0, "\n" + new string(' ', writer.CurrentDepth * 2));
        writer.WriteRawValue(text.ToString(), skipInputValidation: true);
    }
}
