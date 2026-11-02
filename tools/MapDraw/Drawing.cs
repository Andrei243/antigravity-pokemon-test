using System.Text;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace MapDraw;

/// <summary>
/// A region drawn by hand (plan 23 · Z1; the user's decision of 2026-11-02: Kalos has no decompilation and no dump, so
/// its places are drawn in their shape on the four-direction grid). <c>layout.json</c> places each drawing, a text file
/// of a character to a tile (<see cref="Legend"/>), at a tile of the region, and names the areas by rectangles; the
/// drawings are the source, and the chunks, the matrix and the habitats the game loads are written from them.
/// </summary>
public sealed class Layout
{
    /// <summary>The region's id, as <c>Data/regions</c> names it.</summary>
    public string Region { get; set; } = "";

    /// <summary>The overworld's matrix in chunks across and down: the region's whole shape, most of it not drawn yet.</summary>
    public int Width { get; set; }
    public int Height { get; set; }

    public List<Piece> Pieces { get; set; } = new();

    /// <summary>The areas, the first holding a tile winning; in the order of the matrix's area keys.</summary>
    public List<AreaRect> Areas { get; set; } = new();
}

/// <summary>A drawing (<see cref="File"/>, beside <c>layout.json</c>) and the tile of the region its north-west corner lies on.</summary>
public sealed class Piece
{
    public string File { get; set; } = "";
    public int X { get; set; }
    public int Z { get; set; }
}

/// <summary>An area's tiles: a rectangle of tiles of the region.</summary>
public sealed class AreaRect
{
    public string Key { get; set; } = "";
    public int X { get; set; }
    public int Z { get; set; }
    public int Width { get; set; }
    public int Depth { get; set; }

    public bool Holds(int x, int z) => x >= X && x < X + Width && z >= Z && z < Z + Depth;
}

/// <summary>What one character of a drawing makes of its tile: the chunk file's cover code, whether it blocks, its behaviour.</summary>
public readonly record struct Ink(char Cover, bool Solid, TileBehavior Behaviour);

/// <summary>The world files a layout makes (<see cref="Drawing.Build"/>).</summary>
public sealed record DrawnWorld(WorldMatrixFile Matrix, IReadOnlyList<WorldChunkFile> Chunks);

public static class Drawing
{
    public const string LayoutFile = "layout.json";

    /// <summary>
    /// The characters a drawing is made of. Most are the chunk files' own cover codes (<see cref="TerrainCoverCodes"/>);
    /// <c>H</c> is a house's body and <c>D</c> its door, found together by the builder at the door
    /// (<c>WorldMapBuilder.BuildingsAtDoors</c>, as a Game Boy Advance region's houses are), and <c>S</c> a signpost's
    /// tile, which the area file's sign there makes a signpost; <c>K</c> a counter on paving, which whoever stands behind it is
    /// spoken to across.
    /// </summary>
    public static readonly IReadOnlyDictionary<char, Ink> Legend = new Dictionary<char, Ink>
    {
        ['T'] = new('T', true, TileBehavior.None),
        ['O'] = new('O', true, TileBehavior.None),
        ['.'] = new('.', false, TileBehavior.None),
        ['*'] = new('*', false, TileBehavior.None),
        ['w'] = new('w', false, TileBehavior.TallGrass),
        [':'] = new(':', false, TileBehavior.None),
        ['='] = new('=', false, TileBehavior.None),
        [','] = new(',', false, TileBehavior.None),
        ['~'] = new('~', false, TileBehavior.River),
        ['b'] = new('b', false, TileBehavior.None),
        ['s'] = new('s', false, TileBehavior.None),
        ['F'] = new('F', true, TileBehavior.None),
        ['L'] = new('L', true, TileBehavior.None),
        ['R'] = new('R', true, TileBehavior.None),
        ['C'] = new('C', true, TileBehavior.None),
        ['H'] = new('?', true, TileBehavior.None),
        ['D'] = new('B', true, TileBehavior.Door),
        ['S'] = new('.', true, TileBehavior.None),
        ['K'] = new('=', true, TileBehavior.Counter),
    };

    /// <summary>What fills a drawn chunk where no drawing reaches: the woods every place is cut out of.</summary>
    public const char Fill = 'T';

    private const int T = WorldChunkFile.Tiles;

    /// <summary>Reads a layout and its drawings from a folder and makes the world files.</summary>
    public static DrawnWorld Build(string folder)
    {
        var layout = GameDataFiles.Deserialize<Layout>(File.ReadAllText(Path.Combine(folder, LayoutFile)));
        var drawings = layout.Pieces.Select(p => (p, Rows: File.ReadAllLines(Path.Combine(folder, p.File)).Where(r => r.Length > 0).ToArray())).ToList();
        return Build(layout, drawings);
    }

    public static DrawnWorld Build(Layout layout, IReadOnlyList<(Piece Piece, string[] Rows)> drawings)
    {
        int width = layout.Width * T, height = layout.Height * T;
        var tiles = new char?[width, height];
        foreach (var (piece, rows) in drawings)
            for (int z = 0; z < rows.Length; z++)
                for (int x = 0; x < rows[z].Length; x++)
                {
                    char c = rows[z][x];
                    if (c == ' ') continue;
                    if (!Legend.ContainsKey(c)) throw new InvalidDataException($"{piece.File}: row {z} has '{c}' at {x}, which the legend doesn't know.");
                    int gx = piece.X + x, gz = piece.Z + z;
                    if (gx < 0 || gz < 0 || gx >= width || gz >= height) throw new InvalidDataException($"{piece.File}: tile {x},{z} lies outside the region.");
                    tiles[gx, gz] = c;
                }

        var keys = layout.Areas.Select(a => a.Key).ToList();
        string? AreaOf(int x, int z) => layout.Areas.FirstOrDefault(a => a.Holds(x, z))?.Key;

        var chunks = new List<WorldChunkFile>();
        var cells = new string[layout.Height, layout.Width];
        var cellAreas = new string[layout.Height, layout.Width];
        for (int cy = 0; cy < layout.Height; cy++)
            for (int cx = 0; cx < layout.Width; cx++)
            {
                bool drawn = false;
                for (int z = 0; z < T && !drawn; z++)
                    for (int x = 0; x < T && !drawn; x++)
                        drawn = tiles[cx * T + x, cy * T + z] != null;
                if (!drawn) { cells[cy, cx] = "-"; cellAreas[cy, cx] = "-"; continue; }

                // The chunk's area is the one most of its tiles belong to; the others are named tile by tile
                var counts = new Dictionary<string, int>();
                for (int z = 0; z < T; z++)
                    for (int x = 0; x < T; x++)
                        if (AreaOf(cx * T + x, cy * T + z) is { } a) counts[a] = counts.GetValueOrDefault(a) + 1;
                string own = counts.Count == 0 ? keys[0] : counts.OrderByDescending(p => p.Value).ThenBy(p => keys.IndexOf(p.Key)).First().Key;

                var chunk = new WorldChunkFile { Id = chunks.Count };
                var areaRows = new List<string>();
                bool mixed = false;
                for (int z = 0; z < T; z++)
                {
                    var behaviours = new StringBuilder();
                    var solid = new StringBuilder();
                    var cover = new StringBuilder();
                    var areas = new StringBuilder();
                    for (int x = 0; x < T; x++)
                    {
                        int gx = cx * T + x, gz = cy * T + z;
                        var ink = Legend[tiles[gx, gz] ?? Fill];
                        behaviours.Append(((byte)ink.Behaviour).ToString("x2"));
                        solid.Append(ink.Solid ? '#' : '.');
                        cover.Append(ink.Cover);
                        string? area = AreaOf(gx, gz);
                        if (area != null && area != own) { areas.Append(WorldChunkFile.AreaCode(keys.IndexOf(area))); mixed = true; }
                        else areas.Append(WorldChunkFile.NoArea);
                    }
                    chunk.Behaviours.Add(behaviours.ToString());
                    chunk.Solid.Add(solid.ToString());
                    chunk.Cover.Add(cover.ToString());
                    areaRows.Add(areas.ToString());
                }
                chunk.Heights.Add(new HeightPlate { X = 0, Z = 0, Width = T, Depth = T, Height = 0 });
                if (mixed) chunk.Areas = areaRows;
                chunk.Validate();
                chunks.Add(chunk);
                cells[cy, cx] = chunk.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
                cellAreas[cy, cx] = keys.IndexOf(own).ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

        int cellWidth = Math.Max(2, chunks.Count.ToString(System.Globalization.CultureInfo.InvariantCulture).Length);
        var matrix = new WorldMatrixFile
        {
            Id = 0, Width = layout.Width, Height = layout.Height,
            Chunks = Enumerable.Range(0, layout.Height).Select(y => WorldMatrixFile.Row(Enumerable.Range(0, layout.Width).Select(x => cells[y, x]), cellWidth)).ToList(),
            Areas = Enumerable.Range(0, layout.Height).Select(y => WorldMatrixFile.Row(Enumerable.Range(0, layout.Width).Select(x => cellAreas[y, x]), cellWidth)).ToList(),
            AreaKeys = keys
        };
        return new DrawnWorld(matrix, chunks);
    }

    /// <summary>
    /// Where the region's wild Pokémon live (the Pokédex's area page): a character to each chunk of the overworld, and every
    /// area file of the folder with wild Pokémon, shown on the chunks it covers.
    /// </summary>
    public static WorldHabitatsFile Habitats(DrawnWorld world, IEnumerable<WorldAreaFile> areas)
    {
        var map = new List<string>();
        for (int y = 0; y < world.Matrix.Height; y++)
        {
            var row = new StringBuilder();
            for (int x = 0; x < world.Matrix.Width; x++)
            {
                int id = world.Matrix.ChunkAt(x, y);
                if (id == WorldMatrixFile.NoChunk) { row.Append(' '); continue; }
                var chunk = world.Chunks[id];
                int water = chunk.Cover.Sum(r => r.Count(c => c == '~'));
                int town = chunk.Cover.Sum(r => r.Count(c => c is '=' or '?' or 'B'));
                row.Append(water * 2 > T * T ? '~' : town * 8 > T * T ? 'T' : '.');
            }
            map.Add(row.ToString());
        }
        var habitats = new WorldHabitatsFile { Map = map };
        foreach (var area in areas.OrderBy(a => a.Key, StringComparer.Ordinal))
        {
            if (area.Land == null && area.Water == null) continue;
            var cells = new List<string>();
            for (int y = 0; y < world.Matrix.Height; y++)
                for (int x = 0; x < world.Matrix.Width; x++)
                    if (world.Matrix.AreaAt(x, y) == area.Key) cells.Add($"{x},{y}");
            var land = area.Land?.Select(e => e.Species).Distinct().ToList();
            habitats.Areas.Add(new HabitatArea
            {
                Key = area.Key, Name = area.Name, Cells = string.Join(' ', cells),
                Morning = land, Day = land, Night = land,
                Surf = area.Water?.Select(e => e.Species).Distinct().ToList()
            });
        }
        return habitats;
    }
}
