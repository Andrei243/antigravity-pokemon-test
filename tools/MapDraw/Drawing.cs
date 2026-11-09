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

    /// <summary>Maps of their own beside the overworld (plan 23 · Z3: a cave), each drawn on a grid of its own and written as its own matrix.</summary>
    public List<MapDrawing> Maps { get; set; } = new();

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
public sealed record DrawnWorld(WorldMatrixFile Matrix, IReadOnlyList<WorldChunkFile> Chunks, IReadOnlyList<WorldMatrixFile>? Maps = null, IReadOnlyDictionary<string, string>? Cells = null,
    IReadOnlyDictionary<int, (int X, int Y)>? Islands = null);

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
        // A cave (plan 23 · Z3): its floor, its rock (blocked), and a cave's mouth on the open ground outside
        ['c'] = new('c', false, TileBehavior.CaveFloor),
        ['r'] = new('r', true, TileBehavior.None),
        ['M'] = new('M', false, TileBehavior.EntranceNorth),
        ['m'] = new('c', false, TileBehavior.EntranceSouth),
        // Plan 23 · Z4: snow underfoot, a puddle, a cave's way out taken walking north (a second mouth, Victory Road's),
        // and a cave's mouth in the open taken walking south
        ['^'] = new('^', false, TileBehavior.ShallowSnow),
        ['p'] = new('p', false, TileBehavior.Puddle),
        ['u'] = new('c', false, TileBehavior.EntranceNorth),
        ['n'] = new('M', false, TileBehavior.EntranceSouth),
        // Plan 23, Kalos's Mamoswine ride: a drift of snow too deep to wade, crossed only on a ridden Pokémon's back
        ['%'] = new('^', false, TileBehavior.SnowDrift),
        // Plan 23, Kalos's Waterfall: a waterfall, taken up or down with the field move (Couriway Town's falls)
        ['|'] = new('~', false, TileBehavior.Waterfall),
        // Plan 23, Alola's rough ground: rocks and rubble only Mudsdale's Gallop crosses, open rock underfoot
        ['x'] = new('r', false, TileBehavior.RoughGround),
        // Plan 23, Alola's strong currents: water that carries a surfer on its way (west, east, north, south), which only a
        // Sharpedo's Jet stems; a gap in a reef (R) with one holds an islet shut against a Lapras
        ['<'] = new('~', false, TileBehavior.CurrentWest),
        ['>'] = new('~', false, TileBehavior.CurrentEast),
        ['A'] = new('~', false, TileBehavior.CurrentNorth),
        ['V'] = new('~', false, TileBehavior.CurrentSouth),
    };

    /// <summary>What fills a drawn chunk where no drawing reaches: the woods every place is cut out of.</summary>
    public const char Fill = 'T';

    private const int T = WorldChunkFile.Tiles;

    /// <summary>Reads a layout and its drawings from a folder and makes the world files.</summary>
    public static DrawnWorld Build(string folder)
    {
        var layout = GameDataFiles.Deserialize<Layout>(File.ReadAllText(Path.Combine(folder, LayoutFile)));
        var drawings = layout.Pieces.Select(p => (p, Rows: File.ReadAllLines(Path.Combine(folder, p.File)).Where(r => r.Length > 0).ToArray())).ToList();
        var maps = layout.Maps.Select(m => (IReadOnlyList<(Piece Piece, string[] Rows)>)m.Pieces.Select(p => (p, Rows: File.ReadAllLines(Path.Combine(folder, p.File)).Where(r => r.Length > 0).ToArray())).ToList()).ToList();
        return Build(layout, drawings, maps);
    }

    /// <summary>The overworld and, from each <see cref="MapDrawing"/> of the layout, a map of its own with its own matrix (plan 23 · Z3: the caves).</summary>
    public static DrawnWorld Build(Layout layout, IReadOnlyList<(Piece Piece, string[] Rows)> drawings, IReadOnlyList<IReadOnlyList<(Piece Piece, string[] Rows)>>? mapDrawings = null)
    {
        var keys = layout.Areas.Select(a => a.Key).ToList();
        var (matrix, chunks) = BuildMatrix(0, layout.Width, layout.Height, drawings, keys, (x, z) => layout.Areas.FirstOrDefault(a => a.Holds(x, z))?.Key, Fill, 0);
        var extra = new List<WorldMatrixFile>();
        for (int i = 0; i < layout.Maps.Count; i++)
        {
            var m = layout.Maps[i];
            // A map of several areas (plan 23 · Z5: an island of Alola beside Melemele) names them by rectangles as the
            // overworld does; a cave is one area, its own
            var mapKeys = m.Areas.Count > 0 ? m.Areas.Select(a => a.Key).ToList() : new List<string> { m.Area };
            Func<int, int, string?> areaOf = m.Areas.Count > 0 ? (x, z) => m.Areas.FirstOrDefault(a => a.Holds(x, z))?.Key : (x, z) => m.Area;
            var (mx, mc) = BuildMatrix(m.Matrix, m.Width, m.Height, mapDrawings![i], mapKeys, areaOf, m.Fill, chunks.Count);
            chunks.AddRange(mc);
            extra.Add(mx);
        }
        var islands = new Dictionary<int, (int X, int Y)>();
        foreach (var m in layout.Maps.Where(m => m.Island.Length > 0))
        {
            var parts = m.Island.Split(',');
            islands[m.Matrix] = (int.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture), int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture));
        }
        return new DrawnWorld(matrix, chunks, extra, layout.Maps.Where(m => m.Cell.Length > 0).ToDictionary(m => m.Area, m => m.Cell), islands);
    }

    private static (WorldMatrixFile Matrix, List<WorldChunkFile> Chunks) BuildMatrix(int matrixId, int layoutWidth, int layoutHeight, IReadOnlyList<(Piece Piece, string[] Rows)> drawings,
        List<string> keys, Func<int, int, string?> AreaOf, char fill, int firstChunk)
    {
        var layout = (Width: layoutWidth, Height: layoutHeight);
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

                var chunk = new WorldChunkFile { Id = firstChunk + chunks.Count };
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
                        var ink = Legend[tiles[gx, gz] ?? fill];
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

        int cellWidth = Math.Max(2, (firstChunk + chunks.Count).ToString(System.Globalization.CultureInfo.InvariantCulture).Length);
        var matrix = new WorldMatrixFile
        {
            Id = matrixId, Width = layout.Width, Height = layout.Height,
            Chunks = Enumerable.Range(0, layout.Height).Select(y => WorldMatrixFile.Row(Enumerable.Range(0, layout.Width).Select(x => cells[y, x]), cellWidth)).ToList(),
            Areas = Enumerable.Range(0, layout.Height).Select(y => WorldMatrixFile.Row(Enumerable.Range(0, layout.Width).Select(x => cellAreas[y, x]), cellWidth)).ToList(),
            AreaKeys = keys
        };
        return (matrix, chunks);
    }

    /// <summary>
    /// Where the region's wild Pokémon live (the Pokédex's area page): a character to each chunk of the overworld, and every
    /// area file of the folder with wild Pokémon, shown on the chunks it covers.
    /// </summary>
    public static WorldHabitatsFile Habitats(DrawnWorld world, IEnumerable<WorldAreaFile> areas)
    {
        // The overworld at 0,0 and each island at its own cell (plan 23, wave 91: Alola's islands on one grid)
        var grids = new List<(WorldMatrixFile Matrix, int X, int Y)> { (world.Matrix, 0, 0) };
        if (world.Islands != null && world.Maps != null)
            foreach (var m in world.Maps)
                if (world.Islands.TryGetValue(m.Id, out var at)) grids.Add((m, at.X, at.Y));
        int gridWidth = grids.Max(g => g.X + g.Matrix.Width), gridHeight = grids.Max(g => g.Y + g.Matrix.Height);
        var looks = new char[gridHeight, gridWidth];
        for (int y = 0; y < gridHeight; y++)
            for (int x = 0; x < gridWidth; x++) looks[y, x] = ' ';
        foreach (var (matrix, ox, oy) in grids)
            for (int y = 0; y < matrix.Height; y++)
                for (int x = 0; x < matrix.Width; x++)
                {
                    int id = matrix.ChunkAt(x, y);
                    if (id == WorldMatrixFile.NoChunk) continue;
                    if (looks[oy + y, ox + x] != ' ') throw new InvalidDataException($"Two islands have a chunk at {ox + x},{oy + y} of the habitats' grid.");
                    var chunk = world.Chunks[id];
                    int water = chunk.Cover.Sum(r => r.Count(c => c == '~'));
                    int town = chunk.Cover.Sum(r => r.Count(c => c is '=' or '?' or 'B'));
                    looks[oy + y, ox + x] = water * 2 > T * T ? '~' : town * 8 > T * T ? 'T' : '.';
                }
        var map = new List<string>();
        for (int y = 0; y < gridHeight; y++)
            map.Add(new string(Enumerable.Range(0, gridWidth).Select(x => looks[y, x]).ToArray()));
        var habitats = new WorldHabitatsFile { Map = map };
        foreach (var area in areas.OrderBy(a => a.Key, StringComparer.Ordinal))
        {
            if (area.Land == null && area.Water == null) continue;
            var cells = new List<string>();
            foreach (var (matrix, ox, oy) in grids)
                for (int y = 0; y < matrix.Height; y++)
                    for (int x = 0; x < matrix.Width; x++)
                        if (matrix.AreaAt(x, y) == area.Key) cells.Add($"{ox + x},{oy + y}");
            if (cells.Count == 0 && world.Cells != null && world.Cells.TryGetValue(area.Key, out var mouth)) cells.Add(mouth);
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

/// <summary>
/// A map of its own drawn beside the overworld (<see cref="Layout.Maps"/>): a cave or any place whose world file lists
/// it as a map (<c>world.json</c>'s <c>maps</c>), on a grid of <see cref="Width"/> by <see cref="Height"/> chunks written as
/// matrix <see cref="Matrix"/>, all one area. Where no drawing reaches is <see cref="Fill"/>, rock by default.
/// </summary>
public sealed class MapDrawing
{
    public int Matrix { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>The map's one area key, which its area file names.</summary>
    public string Area { get; set; } = "";

    public char Fill { get; set; } = 'r';

    /// <summary>The cell of the overworld its mouth lies in, <c>"x,y"</c>: where the Pokédex's area page shows the cave's Pokémon.</summary>
    public string Cell { get; set; } = "";

    /// <summary>
    /// For an island beside the overworld (plan 23, wave 91: Alola's Akala, Ula'ula and Poni), the cell of the region's map
    /// its north-west chunk is shown at, <c>"x,y"</c>, as the Town Map's <c>islands</c> lays it: its chunks and areas are on
    /// the habitats' grid there, which grows to hold them.
    /// </summary>
    public string Island { get; set; } = "";

    public List<Piece> Pieces { get; set; } = new();

    /// <summary>
    /// The map's areas by rectangles of its own tiles, the first holding a tile winning (plan 23 · Z5: an island of
    /// several places); none for a map that is all one area, <see cref="Area"/>.
    /// </summary>
    public List<AreaRect> Areas { get; set; } = new();
}
