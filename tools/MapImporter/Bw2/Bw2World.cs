using System.Globalization;
using System.Text;
using System.Text.Json;
using MapImporter.Gba;
using MapImporter.Hg;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace MapImporter.Bw2;

/// <summary>
/// What was read of Black 2 turned into the game's world files (plan 22 · U2), as <see cref="HgWorld"/> turns
/// HeartGold's: the chunks with their classes read as this game's behaviours (<see cref="Bw2Behaviours"/>), their look
/// (<see cref="Bw2Cover"/>), heights and props; the matrices; every zone as an area with its events and its wild
/// Pokémon by season; and the region's trainers. The tiles a warp stands on take the behaviour of the way it is
/// taken (<see cref="WarpTiles"/>), since Black 2 keeps that in its warps and not in its tiles.
/// </summary>
public sealed class Bw2World
{
    public Bw2Maps Maps { get; }
    public List<string> Problems => Maps.Problems;
    public List<TrainerRecord> Trainers { get; }

    /// <summary>What every trainer id of Black 2's takes before it (<c>unova_youngster_petey</c>), as HeartGold's take <c>johto_</c>.</summary>
    public const string TrainerPrefix = "unova_";

    /// <summary>The chunks' numbers, by their folders (<c>generated/map_models.txt</c>).</summary>
    private readonly Dictionary<string, int> chunkIds = new(StringComparer.Ordinal);
    private readonly Dictionary<int, string> itemConstants = new();
    private readonly Dictionary<int, Dictionary<int, int>> balls, hidden;
    private readonly Dictionary<int, string> trainerConstants = new();
    private readonly Dictionary<string, Bw2Events> events = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Bw2Encounters?> encounters = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<(int X, int Z), (TileBehavior Behaviour, bool Open)>> warpTiles = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TerrainCover[]> cover = new(StringComparer.Ordinal);

    public Bw2World(Bw2Maps maps)
    {
        Maps = maps;
        string Text(params string[] parts) => File.ReadAllText(Path.Combine(parts.Prepend(maps.Directory).ToArray()));
        foreach (var (name, id) in Bw2Constants.Read(Text("generated", "map_models.txt")))
            chunkIds[Bw2Matrix.Folder(name)] = id;
        foreach (var (name, id) in Bw2Constants.Read(Text("generated", "items.txt"))) itemConstants.TryAdd(id, name);
        foreach (var (name, id) in Bw2Constants.Read(Text("generated", "trainers.txt"))) trainerConstants.TryAdd(id, name);
        balls = Bw2Events.GlobalScripts(Text("res", "field", "scripts", "global_7000.s"));
        hidden = Bw2Events.GlobalScripts(Text("res", "field", "scripts", "global_8000.s"));
        Trainers = ReadTrainers(Text("generated", "trainer_classes.txt"));
        maps.Matrix(Bw2Maps.Overworld).FillZones();
        foreach (var (constant, _) in maps.Matrices) FindWarpTiles(constant);
    }

    // ---------------------------------------------------------------- names

    /// <summary>
    /// An area's key, the zone's own name (<c>aspertia_city</c>), but where it would meet another region's: Unova's routes
    /// share their numbers with Kanto's (<c>route_19</c>), and a script file and the regions' areas are found by the key
    /// alone, so every route of Unova's, its Victory Road and its League take the region's name (<c>unova_route_19</c>).
    /// </summary>
    public static string KeyOf(string zone)
    {
        string key = (zone.StartsWith("ZONE_", StringComparison.Ordinal) ? zone["ZONE_".Length..] : zone).ToLowerInvariant();
        return Clashes(key) ? "unova_" + key : key;
    }

    public static bool Clashes(string key) =>
        key.StartsWith("route_", StringComparison.Ordinal) || key.StartsWith("victory_road", StringComparison.Ordinal)
        || key.StartsWith("pokemon_league", StringComparison.Ordinal);

    /// <summary>A chunk's number, by its folder; -1 where the list has none.</summary>
    public int ChunkId(string folder) => chunkIds.GetValueOrDefault(folder, -1);

    /// <summary>The name of the area a zone is, in our capitals: <c>PLACE_ASPERTIA_CITY</c> is "Aspertia City".</summary>
    public static string NameOf(Bw2Zone zone)
    {
        string words = zone.Name;
        return string.Join(' ', words.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(w =>
            w is "POKEMON" ? "Pokémon" : w.Length > 1 && w.All(char.IsDigit) ? w : char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));
    }

    private string? ItemName(int id, string where)
    {
        if (!itemConstants.TryGetValue(id, out string? constant))
        {
            Note($"{where}: the item {id} isn't in generated/items.txt");
            return null;
        }
        // The decompilation keeps the original's short spelling of one item, as HeartGold's does
        if (constant == "ITEM_PARLYZ_HEAL") constant = "ITEM_PARALYZE_HEAL";
        return GbaNames.Item(constant) ?? Note($"{where}: {constant} is not in the game's data", constant);
    }

    private string Note(string problem, string value = "")
    {
        if (!Problems.Contains(problem)) Problems.Add(problem);
        return value;
    }

    // ---------------------------------------------------------------- events

    public Bw2Events EventsOf(Bw2Zone zone)
    {
        if (events.TryGetValue(zone.Key, out var known)) return known;
        string path = Path.Combine(Maps.Directory, "res", "field", "events", zone.Key + ".json");
        var read = File.Exists(path) ? Bw2Events.Parse(File.ReadAllText(path)) : Bw2Events.None;
        return events[zone.Key] = read;
    }

    public Bw2Encounters? EncountersOf(Bw2Zone zone)
    {
        if (zone.Encounters is not { } constant) return null;
        if (encounters.TryGetValue(constant, out var known)) return known;
        string name = (constant.StartsWith("ENCOUNTERS_", StringComparison.Ordinal) ? constant["ENCOUNTERS_".Length..] : constant).ToLowerInvariant();
        string path = Path.Combine(Maps.Directory, "res", "field", "encounters", name + ".json");
        Bw2Encounters? read = null;
        if (File.Exists(path)) read = Bw2Encounters.Parse(File.ReadAllText(path));
        else Note($"{zone.Constant} names {constant}, which has no file");
        return encounters[constant] = read;
    }

    /// <summary>The terrain grid's tile at a tile of a matrix; null off its chunks.</summary>
    private Bw2Tile? TileAt(Bw2Matrix matrix, int x, int z)
    {
        if (x < 0 || z < 0) return null;
        if (matrix.ModelAt(x / LandData.Tiles, z / LandData.Tiles) is not { } model) return null;
        if (Maps.Terrain(Bw2Matrix.Folder(model)) is not [{ } land, ..]) return null;
        int lx = x % LandData.Tiles, lz = z % LandData.Tiles;
        return lx < land.Width && lz < land.Height ? land.At(lx, lz) : null;
    }

    /// <summary>
    /// The behaviour and openness of every tile a warp of a matrix stands on (plan 22 · U2). Black 2 says how a warp is
    /// taken in the warp itself (<c>transition</c>), not in its tile, which is plain: a door is a blocked tile of a
    /// building's front, a gate's way out a blocked tile at the end of a path, a room's mat an open tile against its
    /// wall, a staircase an open tile with one way off it. Each becomes the behaviour that is taken the way the tile is
    /// open: a blocked tile walked into from the south is a door outdoors, an opening walked into from the north is one
    /// entered walking south, a mat with the wall south of it is left walking south, and the rest by the one side open.
    /// </summary>
    private void FindWarpTiles(string constant)
    {
        var matrix = Maps.Matrix(constant);
        foreach (var zone in AreasOn(constant))
        {
            bool outdoors = constant == Bw2Maps.Overworld || zone.Area != null && Maps.AreaOutdoors.GetValueOrDefault(zone.Area);
            foreach (var warp in EventsOf(zone).Warps)
                for (int dx = 0; dx < warp.Width; dx++)
                    for (int dz = 0; dz < warp.Height; dz++)
                    {
                        int x = warp.X + dx, z = warp.Z + dz;
                        if (TileAt(matrix, x, z) is not { } here || matrix.ModelAt(x / LandData.Tiles, z / LandData.Tiles) is not { } model) continue;
                        bool Open(int tx, int tz) => TileAt(matrix, tx, tz) is { Blocked: false };
                        bool n = Open(x, z - 1), s = Open(x, z + 1), w = Open(x - 1, z), e = Open(x + 1, z);
                        (TileBehavior, bool)? way =
                            here.Blocked && s && outdoors ? (TileBehavior.Door, false)
                            : here.Blocked && n ? (TileBehavior.EntranceSouth, true)
                            : here.Blocked && s ? (TileBehavior.ExitNorth, true)
                            : !here.Blocked && !s && n ? (TileBehavior.ExitSouth, true)
                            : !here.Blocked && !n && s ? (TileBehavior.ExitNorth, true)
                            : !here.Blocked && w && !e ? (TileBehavior.EntranceEast, true)
                            : !here.Blocked && e && !w ? (TileBehavior.EntranceWest, true)
                            : here.Blocked && w ? (TileBehavior.EntranceEast, true)
                            : here.Blocked && e ? (TileBehavior.EntranceWest, true)
                            : null;
                        if (way is not { } found) continue;
                        string folder = Bw2Matrix.Folder(model);
                        if (!warpTiles.TryGetValue(folder, out var tiles)) warpTiles[folder] = tiles = new();
                        tiles[(x % LandData.Tiles, z % LandData.Tiles)] = found;
                    }
        }
    }

    // ---------------------------------------------------------------- world files

    /// <summary>The look of each tile of a chunk, worked out once (32 × 32, padded where the chunk's grid is smaller).</summary>
    public TerrainCover[] CoverOf(string folder)
    {
        if (cover.TryGetValue(folder, out var known)) return known;
        var result = new TerrainCover[LandData.Tiles * LandData.Tiles];
        if (Maps.Terrain(folder) is [{ } land, ..])
        {
            var look = Bw2Cover.OfChunk(Maps, folder);
            for (int z = 0; z < Math.Min(LandData.Tiles, land.Height); z++)
                for (int x = 0; x < Math.Min(LandData.Tiles, land.Width); x++)
                    result[z * LandData.Tiles + x] = look.Length > 0 ? look[z * land.Width + x] : Bw2Cover.OfTile(Bw2Behaviours.Of(land.At(x, z).Class));
        }
        return cover[folder] = result;
    }

    public WorldChunkFile Chunk(string folder)
    {
        const int T = LandData.Tiles;
        var file = new WorldChunkFile { Id = ChunkId(folder) };
        var layers = Maps.Terrain(folder);
        var land = layers.Length > 0 ? layers[0] : null;
        var look = CoverOf(folder);
        // A season's chunk stands where its spring's does, under the same warps
        var overrides = warpTiles.GetValueOrDefault(folder) ?? (BaseOf(folder) is { } spring ? warpTiles.GetValueOrDefault(spring) : null);
        var behaviour = new TileBehavior[T * T];
        var solid = new bool[T * T];
        var height = new float[T * T];
        var slopeX = new float[T * T];
        var slopeZ = new float[T * T];
        var there = new bool[T * T];
        for (int z = 0; z < T; z++)
        {
            var behaviours = new StringBuilder(T * 2);
            var blocked = new StringBuilder(T);
            var row = new StringBuilder(T);
            for (int x = 0; x < T; x++)
            {
                int i = z * T + x;
                if (land != null && x < land.Width && z < land.Height)
                {
                    var tile = land.At(x, z);
                    there[i] = true;
                    behaviour[i] = Bw2Behaviours.Of(tile.Class);
                    solid[i] = tile.Blocked;
                    (height[i], slopeX[i], slopeZ[i]) = (tile.Height, tile.SlopeX, tile.SlopeZ);
                    if (overrides != null && overrides.TryGetValue((x, z), out var way))
                    {
                        behaviour[i] = way.Behaviour;
                        if (way.Open) solid[i] = false;
                    }
                }
                else solid[i] = true;
                behaviours.Append(((byte)behaviour[i]).ToString("X2", CultureInfo.InvariantCulture));
                blocked.Append(solid[i] ? '#' : '.');
                row.Append(TerrainCoverCodes.CodeOf(look[i]));
            }
            file.Behaviours.Add(behaviours.ToString());
            file.Solid.Add(blocked.ToString());
            file.Cover.Add(row.ToString());
        }
        file.Heights = Plates(there, height, slopeX, slopeZ);
        // A second grid (a bridge's deck over a road) is a plate a tile where it stands well clear of the first
        for (int layer = 1; layer < layers.Length; layer++)
        {
            if (layers[layer] is not { } upper) continue;
            for (int z = 0; z < Math.Min(T, upper.Height); z++)
                for (int x = 0; x < Math.Min(T, upper.Width); x++)
                {
                    var tile = upper.At(x, z);
                    if (tile.Blocked || !there[z * T + x] || MathF.Abs(tile.Height - height[z * T + x]) < 0.5f) continue;
                    file.Heights.Add(new HeightPlate { X = x, Z = z, Width = 1, Depth = 1, Height = Round(tile.Height - (tile.SlopeX + tile.SlopeZ) / 2f), SlopeX = Round(tile.SlopeX), SlopeZ = Round(tile.SlopeZ) });
                }
        }
        bool interior = Maps.IsInterior(folder);
        if (Maps.Model(folder) is { } model)
            file.Props = model.Props.Select(p =>
            {
                var box = Maps.PropBox(p.Resource, interior);
                var (x0, z0, x1, z1) = box is { } b ? Bw2Cover.Footprint(p, b, T, T) : (p.Position.X / LandData.TileUnits + T / 2f, p.Position.Z / LandData.TileUnits + T / 2f, p.Position.X / LandData.TileUnits + T / 2f, p.Position.Z / LandData.TileUnits + T / 2f);
                return new ChunkProp
                {
                    Model = p.Resource,
                    Name = Maps.PropName(p.Resource, interior),
                    X = Round(p.Position.X / LandData.TileUnits + T / 2f), Y = Round(p.Position.Y / LandData.TileUnits), Z = Round(p.Position.Z / LandData.TileUnits + T / 2f),
                    BoxX = Round(x0), BoxZ = Round(z0), Width = Round(x1 - x0), Depth = Round(z1 - z0),
                    Height = box is { } c ? Round((c.Max.Y - c.Min.Y) / LandData.TileUnits) : 0f
                };
            }).ToList();
        // Castelia City's towers, which Black 2 draws into the street's own model with no prop of their own (plan 22 · U5,
        // part c): a prop of ours for each block of them, as tall as the tallest of its tiles
        foreach (var (x0, z0, x1, z1, tall) in Bw2Cover.Towers(Maps, folder, overrides?.Keys))
            file.Props.Add(new ChunkProp
            {
                Model = -1, Name = Bw2Cover.TowerProp,
                X = Round((x0 + x1 + 1) / 2f), Y = 0f, Z = Round((z0 + z1 + 1) / 2f),
                BoxX = x0, BoxZ = z0, Width = x1 - x0 + 1, Depth = z1 - z0 + 1, Height = Round(tall)
            });
        return file;
    }

    private static float Round(double value) => (float)Math.Round(value, 4) + 0f;

    /// <summary>
    /// The chunk's ground as plates: a flat plate per run of tiles at one height, merged down the rows where they agree;
    /// a sloping tile a plate of its own, its height given at its north-west corner.
    /// </summary>
    public static List<HeightPlate> Plates(bool[] there, float[] height, float[] slopeX, float[] slopeZ)
    {
        const int T = LandData.Tiles;
        var plates = new List<HeightPlate>();
        var open = new Dictionary<(int X, int W, float H), HeightPlate>();
        for (int z = 0; z < T; z++)
        {
            var row = new Dictionary<(int X, int W, float H), HeightPlate>();
            int x = 0;
            while (x < T)
            {
                int i = z * T + x;
                if (!there[i]) { x++; continue; }
                if (MathF.Abs(slopeX[i]) > 1e-4f || MathF.Abs(slopeZ[i]) > 1e-4f)
                {
                    plates.Add(new HeightPlate { X = x, Z = z, Width = 1, Depth = 1, Height = Round(height[i] - (slopeX[i] + slopeZ[i]) / 2f), SlopeX = Round(slopeX[i]), SlopeZ = Round(slopeZ[i]) });
                    x++;
                    continue;
                }
                float h = Round(height[i]);
                int start = x;
                while (x < T)
                {
                    int j = z * T + x;
                    if (!there[j] || MathF.Abs(slopeX[j]) > 1e-4f || MathF.Abs(slopeZ[j]) > 1e-4f || Round(height[j]) != h) break;
                    x++;
                }
                var key = (start, x - start, h);
                if (open.TryGetValue(key, out var above)) above.Depth++;
                else plates.Add(above = new HeightPlate { X = start, Z = z, Width = x - start, Depth = 1, Height = h });
                row[key] = above;
            }
            open = row;
        }
        return plates;
    }

    /// <summary>The zones whose ground a matrix holds: on the overworld those its cells name, elsewhere those whose header names it.</summary>
    public IEnumerable<Bw2Zone> AreasOn(string matrixConstant)
    {
        var matrix = Maps.Matrix(matrixConstant);
        if (!matrix.HasZones) return Maps.ZonesOn(matrixConstant);
        var seen = new List<Bw2Zone>();
        for (int y = 0; y < matrix.Height; y++)
            for (int x = 0; x < matrix.Width; x++)
                if (matrix.ModelAt(x, y) != null && matrix.ZoneAt(x, y) is { } z && Maps.Zones.TryGetValue(z, out var zone) && !seen.Contains(zone)) seen.Add(zone);
        return seen;
    }

    // ---------------------------------------------------------------- seasons' chunks

    /// <summary>The seasons a chunk folder's name may begin with: the matrix's own chunks are spring's.</summary>
    public static readonly string[] LaterSeasons = { "Summer", "Autumn", "Winter" };

    private static readonly System.Text.RegularExpressions.Regex SeasonalName =
        new(@"^(summer|autumn|winter)(\d\d_\d\d|\d{4}_\d\d_\d\d)$", System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>
    /// What a chunk folder named by season stands in for (plan 22 · U2, part b): its season (<c>Winter</c>) and the
    /// ending of the folder it takes the place of. Black 2 swaps them through a table of its own (<c>map_replace.c</c>,
    /// archive 10, which the decompilation keeps as binary data and doesn't extract), so the swap is read from the
    /// names, which say it: <c>winter05_04</c> is the overworld's <c>overworld_2_map05_04</c> (column 5, row 4), and
    /// <c>winter0701_01_00</c> the cave's <c>…_m_dun0701_01_00</c> (Twist Mountain's). Null for any other folder.
    /// </summary>
    public static (string Season, string Replaces)? SeasonalOf(string folder)
    {
        var m = SeasonalName.Match(folder);
        if (!m.Success) return null;
        string season = char.ToUpperInvariant(m.Groups[1].Value[0]) + m.Groups[1].Value[1..];
        string rest = m.Groups[2].Value;
        return (season, rest.Length == 5 ? "_map" + rest : "_m_dun" + rest);
    }

    private Dictionary<string, string>? seasonalBase;

    /// <summary>The folder each seasonal chunk takes the place of, worked out from the matrices (<see cref="SeasonalChunks"/>).</summary>
    public string? BaseOf(string seasonalFolder)
    {
        if (seasonalBase == null)
        {
            seasonalBase = new(StringComparer.Ordinal);
            foreach (var (constant, _) in Maps.Matrices)
                foreach (var (_, _, _, folder, own) in SeasonalChunks(constant)) seasonalBase.TryAdd(folder, own);
        }
        return seasonalBase.GetValueOrDefault(seasonalFolder);
    }

    /// <summary>
    /// The cells of a matrix another season lays a chunk of its own in: each with its season, the seasonal folder and
    /// the folder it replaces. A seasonal folder whose name matches a cell's chunk is laid there; an overworld chunk's
    /// name gives its column and row, which must be the cell's, or it is a problem of the report's.
    /// </summary>
    public List<(string Season, int X, int Y, string Folder, string Base)> SeasonalChunks(string constant)
    {
        var result = new List<(string, int, int, string, string)>();
        var matrix = Maps.Matrix(constant);
        foreach (string folder in Maps.Chunks)
        {
            if (SeasonalOf(folder) is not { } s) continue;
            for (int y = 0; y < matrix.Height; y++)
                for (int x = 0; x < matrix.Width; x++)
                {
                    if (matrix.ModelAt(x, y) is not { } model) continue;
                    string own = Bw2Matrix.Folder(model);
                    if (SeasonalOf(own) != null || !own.EndsWith(s.Replaces, StringComparison.Ordinal)) continue;
                    if (s.Replaces.StartsWith("_map", StringComparison.Ordinal)
                        && s.Replaces != $"_map{x:00}_{y:00}")
                    {
                        Note($"{folder} stands for {own}, which {constant} has at {x},{y}, not where its name says");
                        continue;
                    }
                    result.Add((s.Season, x, y, folder, own));
                }
        }
        return result;
    }

    public WorldMatrixFile MatrixFile(string constant)
    {
        var (id, matrix) = Maps.Matrices[constant];
        var file = new WorldMatrixFile { Id = id, Width = matrix.Width, Height = matrix.Height };
        var keys = matrix.HasZones ? AreasOn(constant).Select(z => KeyOf(z.Constant)).ToList() : null;
        int chunkWidth = Enumerable.Range(0, matrix.Width * matrix.Height)
            .Select(i => matrix.ModelAt(i % Math.Max(1, matrix.Width), i / Math.Max(1, matrix.Width)) is { } m ? ChunkId(Bw2Matrix.Folder(m)).ToString(CultureInfo.InvariantCulture).Length : 1).DefaultIfEmpty(1).Max();
        int areaWidth = keys == null ? 1 : Math.Max(1, (keys.Count - 1).ToString(CultureInfo.InvariantCulture).Length);
        for (int y = 0; y < matrix.Height; y++)
        {
            var cells = Enumerable.Range(0, matrix.Width).ToList();
            file.Chunks.Add(WorldMatrixFile.Row(cells.Select(x => matrix.ModelAt(x, y) is { } m && ChunkId(Bw2Matrix.Folder(m)) is var c and >= 0 ? c.ToString(CultureInfo.InvariantCulture) : "-"), chunkWidth));
            if (keys != null)
                (file.Areas ??= new()).Add(WorldMatrixFile.Row(cells.Select(x =>
                    matrix.ModelAt(x, y) != null && matrix.ZoneAt(x, y) is { } z ? keys.IndexOf(KeyOf(z)).ToString(CultureInfo.InvariantCulture) : "-"), areaWidth));
        }
        file.AreaKeys = keys;
        var seasonal = SeasonalChunks(constant);
        int seasonWidth = seasonal.Select(s => ChunkId(s.Folder).ToString(CultureInfo.InvariantCulture).Length).Append(chunkWidth).Max();
        foreach (string season in LaterSeasons)
        {
            var mine = seasonal.Where(s => s.Season == season && ChunkId(s.Folder) >= 0).ToList();
            if (mine.Count == 0) continue;
            var grid = new MatrixSeason { Season = season };
            for (int y = 0; y < matrix.Height; y++)
                grid.Chunks.Add(WorldMatrixFile.Row(Enumerable.Range(0, matrix.Width).Select(x =>
                    mine.FirstOrDefault(s => s.X == x && s.Y == y) is { Folder: { } f } ? ChunkId(f).ToString(CultureInfo.InvariantCulture) : "-"), seasonWidth));
            (file.Seasons ??= new()).Add(grid);
        }
        return file;
    }

    /// <summary>
    /// The weather a zone's header names, as the game's area files name it (<see cref="Weathers.Of"/>; plan 22 · U6):
    /// Black 2's 3 is the sandstorm over the Desert Resort's edge, 12 the thicker one of the desert itself, both a
    /// sandstorm here; any other number is written as it is, and reads as clear.
    /// </summary>
    public static string WeatherName(int weather) => weather switch
    {
        3 or 12 => "Sandstorm",
        _ => weather.ToString(CultureInfo.InvariantCulture)
    };

    /// <summary>The battle background a zone names, as the game's area files name them (<c>GameEngine.TerrainAt</c>); empty for one that says nothing more than the ground.</summary>
    public static string BattleBackground(int background) => background switch
    {
        1 => "Plain",
        // Black 2's 2 is the big cities' street (Castelia, Nimbasa, Driftveil and its drawbridge, Route 4's flats) and 3
        // a town's (Aspertia, Floccesy, Virbank): both a city here (plan 22, wave 48), as Platinum's towns are
        2 or 3 => "City",
        4 => "Cave1",
        // a cave of rock and dark (Challengers Cave, the Relic Passage), the woods, the mountains, the shore and the ruins
        // under the sea, read from the zones that name each
        5 => "Cave2",
        6 => "Forest",
        7 => "Mountain",
        8 => "Water",
        9 => "Indoors1",
        _ => ""
    };

    public WorldAreaFile Area(Bw2Zone zone)
    {
        var ev = EventsOf(zone);
        string key = KeyOf(zone.Constant);
        var setting = Maps.SettingOf(zone);
        var matrix = Maps.Matrix(zone.Matrix);
        float GroundAt(int x, int z) => TileAt(matrix, x, z)?.Height ?? 0f;

        var file = new WorldAreaFile
        {
            Key = key,
            Index = zone.Id,
            Name = NameOf(zone),
            Matrix = Maps.Matrices.TryGetValue(zone.Matrix, out var m) ? m.Id : -1,
            Kind = setting switch
            {
                Renders.Setting.Cave => "Cave",
                Renders.Setting.Indoors => "Indoors",
                _ => key.EndsWith("_city", StringComparison.Ordinal) || key.EndsWith("_town", StringComparison.Ordinal) ? "Town" : "Outdoors"
            },
            Sign = setting switch
            {
                Renders.Setting.Cave => "Cave",
                Renders.Setting.Indoors => "None",
                _ => key.EndsWith("_city", StringComparison.Ordinal) ? "City" : key.EndsWith("_town", StringComparison.Ordinal) ? "Town" : key.Contains("route_", StringComparison.Ordinal) ? "Route" : "None"
            },
            Weather = WeatherName(zone.Weather),
            Camera = zone.Camera.ToString(CultureInfo.InvariantCulture),
            BattleBackground = BattleBackground(zone.BattleBackground),
            DayMusic = zone.Music is { } music && music.StartsWith("SEQ_", StringComparison.Ordinal) ? music["SEQ_".Length..] : zone.Music ?? "",
            NightMusic = zone.Music is { } night && night.StartsWith("SEQ_", StringComparison.Ordinal) ? night["SEQ_".Length..] : zone.Music ?? "",
            Encounters = zone.Encounters,
            Bike = zone.Cycling,
            Running = true,
            EscapeRope = zone.EscapeRope,
            Fly = zone.FlyFrom
        };
        Fill(file, zone);

        // The warps, and after them the other tiles of a warp that spans several (a gate's way out three tiles wide):
        // each leads where its first does, and the first keeps its number, which the other side's warps name
        var extra = new List<AreaWarp>();
        foreach (var w in ev.Warps)
        {
            string to = Maps.Zones.ContainsKey(w.Zone) ? KeyOf(w.Zone) : Note($"{key}: a warp leads to {w.Zone}, which has no header", KeyOf(w.Zone));
            file.Warps.Add(new AreaWarp { X = w.X, Z = w.Z, To = to, ToWarp = w.To });
            for (int dx = 0; dx < w.Width; dx++)
                for (int dz = 0; dz < w.Height; dz++)
                    if (dx > 0 || dz > 0) extra.Add(new AreaWarp { X = w.X + dx, Z = w.Z + dz, To = to, ToWarp = w.To });
        }
        file.Warps.AddRange(extra);

        foreach (var n in ev.Npcs)
        {
            var o = new AreaObject
            {
                Id = n.Id.ToString(CultureInfo.InvariantCulture),
                Looks = n.Looks,
                Movement = Bw2Events.Movement(n.Movement),
                X = n.X, Z = n.Z,
                Facing = Bw2Events.Facing(n.Direction),
                RangeX = n.RangeX, RangeZ = n.RangeZ,
                HiddenBy = n.Flag is { } flag ? Bw2Events.Flag(flag) : null,
                Script = n.Script.ToString(CultureInfo.InvariantCulture)
            };
            // Someone the original stands well above the ground under them (on a ledge of the hill, on a deck)
            float above = n.Height - GroundAt(n.X, n.Z);
            if (above >= 0.5f) o.Y = (int)MathF.Round(n.Height);
            if (n.Script >= Bw2Events.ItemBalls && n.Script < Bw2Events.HiddenItems
                && balls.TryGetValue(n.Script - Bw2Events.ItemBalls, out var ball) && ball.TryGetValue(0x800c, out int item))
            {
                o.Item = ItemName(item, $"{key}: the item ball {n.Id}");
                int count = ball.GetValueOrDefault(0x800d, 1);
                o.Count = count > 1 ? count : null;
                // An item ball with no flag of its own would come back for ever: it is given one by its script
                o.HiddenBy ??= Bw2Events.Flag(0x4000 + n.Script);
            }
            else if (TrainerOfScript(n.Script) is { } trainer)
            {
                // A trainer's own script is 3000 + its number; the second of a pair, 5000 + it
                o.Script = trainer;
                o.Trainer = n.Params.Length > 0 && n.Params[0] > 0 ? "sees" : "talks";
                o.Sight = n.Params.Length > 0 ? n.Params[0] : 0;
            }
            file.Objects.Add(o);
        }

        foreach (var b in ev.Signs)
        {
            // (a sign of the other condition outdoors alone: indoors it is a gate house's news board, plan 22 · U6)
            if (b.Condition == Bw2Events.Read || b.Condition == Bw2Events.ReadToo && setting != Renders.Setting.Indoors)
                file.Signs.Add(new AreaSign { X = b.X, Z = b.Z, Type = AreaSign.Read, Script = b.Script.ToString(CultureInfo.InvariantCulture) });
            else if (b.Condition == Bw2Events.Hidden && b.Script >= Bw2Events.HiddenItems && hidden.TryGetValue(b.Script - Bw2Events.HiddenItems, out var h)
                     && h.TryGetValue(0x8000, out int item))
            {
                int count = h.GetValueOrDefault(0x8001, 1);
                file.Signs.Add(new AreaSign
                {
                    X = b.X, Z = b.Z, Type = AreaSign.HiddenItem, Script = b.Script.ToString(CultureInfo.InvariantCulture),
                    Item = ItemName(item, $"{key}: the hidden item {b.Script}"), Count = count > 1 ? count : null,
                    Flag = Bw2Events.Flag(h.GetValueOrDefault(0x8002, 0xb04 + b.Script - Bw2Events.HiddenItems))
                });
            }
        }

        foreach (var t in ev.Triggers)
            file.Triggers.Add(new AreaTrigger
            {
                X = t.X, Z = t.Z, Width = t.Width, Depth = t.Depth, Script = t.Script.ToString(CultureInfo.InvariantCulture),
                Variable = t.Var.Length > 0 ? Bw2Events.Var(t.Var) : "", Value = t.Value.ToString(CultureInfo.InvariantCulture)
            });
        return file;
    }

    /// <summary>The trainer of a script that is a trainer's own (3000 + n, or 5000 + n for the second of a pair); null for any other.</summary>
    public string? TrainerOfScript(int script)
    {
        int n = script >= Bw2Events.SecondOfAPair && script < Bw2Events.SecondOfAPair + 1000 ? script - Bw2Events.SecondOfAPair
            : script >= Bw2Events.Trainers && script < Bw2Events.Trainers + 1000 ? script - Bw2Events.Trainers : -1;
        return n > 0 && trainerConstants.TryGetValue(n, out string? constant) ? constant : null;
    }

    /// <summary>
    /// An area's wild Pokémon (plan 22 · U2): spring's, or the year's, in its own fields, and each later season that
    /// differs in <see cref="WorldAreaFile.Seasons"/>. The grass and the surfed water are the game's tables; the dark
    /// grass, the shaking grass, the rippling water and the rippling fishing spots are Black 2's own fields; the rod is
    /// the Super Rod (Black 2 has one rod).
    /// </summary>
    private void Fill(WorldAreaFile file, Bw2Zone zone)
    {
        if (EncountersOf(zone) is not { BySeason.Count: > 0 } wild) return;
        string where = file.Key;
        AreaSeason Read(Bw2Encounters.Season s)
        {
            int? Rated(string rate, List<AreaEncounter>? slots) =>
                slots == null ? null : rate is "fishing" or "rippling_fishing" ? (s.Rates.GetValueOrDefault(rate) is var p and > 0 ? p : null) : Bw2Encounters.Rate(s.Rates.GetValueOrDefault(rate));
            var season = new AreaSeason { Season = char.ToUpperInvariant(s.Name[0]) + s.Name[1..] };
            season.Land = Bw2Encounters.Slots(s, "grass", false, null, where, Problems);
            season.LandRate = Rated("grass", season.Land);
            season.DarkGrass = Bw2Encounters.Slots(s, "dark_grass", false, null, where, Problems);
            season.DarkGrassRate = Rated("dark_grass", season.DarkGrass);
            season.ShakingGrass = Bw2Encounters.Slots(s, "shaking_grass", false, null, where, Problems);
            season.ShakingGrassRate = Rated("shaking_grass", season.ShakingGrass);
            season.Water = Bw2Encounters.Slots(s, "surf", true, null, where, Problems);
            season.WaterRate = Rated("surf", season.Water);
            season.RipplingWater = Bw2Encounters.Slots(s, "rippling_surf", true, null, where, Problems);
            season.RipplingWaterRate = Rated("rippling_surf", season.RipplingWater);
            season.SuperRod = Bw2Encounters.Slots(s, "fishing", true, Bw2Encounters.RodWeights, where, Problems);
            season.SuperRodRate = Rated("fishing", season.SuperRod);
            season.RipplingRod = Bw2Encounters.Slots(s, "rippling_fishing", true, Bw2Encounters.RodWeights, where, Problems);
            season.RipplingRodRate = Rated("rippling_fishing", season.RipplingRod);
            // A table with no rate is none
            if (season.LandRate == null) season.Land = null;
            if (season.DarkGrassRate == null) season.DarkGrass = null;
            if (season.ShakingGrassRate == null) season.ShakingGrass = null;
            if (season.WaterRate == null) season.Water = null;
            if (season.RipplingWaterRate == null) season.RipplingWater = null;
            if (season.SuperRodRate == null) season.SuperRod = null;
            if (season.RipplingRodRate == null) season.RipplingRod = null;
            return season;
        }
        var seasons = wild.BySeason.Select(Read).ToList();
        var spring = seasons[0];
        (file.Land, file.LandRate) = (spring.Land, spring.LandRate);
        (file.DarkGrass, file.DarkGrassRate) = (spring.DarkGrass, spring.DarkGrassRate);
        (file.ShakingGrass, file.ShakingGrassRate) = (spring.ShakingGrass, spring.ShakingGrassRate);
        (file.Water, file.WaterRate) = (spring.Water, spring.WaterRate);
        (file.RipplingWater, file.RipplingWaterRate) = (spring.RipplingWater, spring.RipplingWaterRate);
        (file.SuperRod, file.SuperRodRate) = (spring.SuperRod, spring.SuperRodRate);
        (file.RipplingRod, file.RipplingRodRate) = (spring.RipplingRod, spring.RipplingRodRate);
        string Same(AreaSeason s) { var copy = Clone(s); copy.Season = ""; return GameDataFiles.Serialize(copy); }
        string springText = Same(spring);
        var later = seasons.Skip(1).Where(s => Same(s) != springText).ToList();
        file.Seasons = later.Count > 0 ? later : null;
    }

    private static AreaSeason Clone(AreaSeason s) => GameDataFiles.Deserialize<AreaSeason>(GameDataFiles.Serialize(s));

    // ---------------------------------------------------------------- trainers

    /// <summary>
    /// Black 2's trainers (<c>res/trainers/data</c>), in the order of <c>generated/trainers.txt</c>, as the game's
    /// trainers.json keeps them: the id the region's prefix and the constant (<c>unova_youngster_petey</c>), the name,
    /// the class in our capitals (a woman's and a man's of one class are one), the routines (Black 2's five names are
    /// Platinum's bits), the items, the team with each Pokémon's personality worked out as HeartGold's
    /// <c>TrainerData_BuildParty</c> works it (the rulings), and the prize money: the last Pokémon's level times four
    /// times the class's rate, twice that for a double battle. A triple battle and a rotation battle are fought as
    /// single ones until plan 14 gives them their formats. The trainers' lines (<c>messages</c>) are never read.
    /// </summary>
    private List<TrainerRecord> ReadTrainers(string classesText)
    {
        var classes = Bw2Constants.Read(classesText);
        var records = new List<TrainerRecord>();
        string dir = Path.Combine(Maps.Directory, "res", "trainers", "data");
        foreach (var (number, constant) in trainerConstants.OrderBy(kv => kv.Key))
        {
            if (constant == "TRAINER_NONE") continue;
            string path = Path.Combine(dir, constant["TRAINER_".Length..].ToLowerInvariant() + ".json");
            if (!File.Exists(path)) continue;
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var t = doc.RootElement;
            if (t.ValueKind != JsonValueKind.Object || !t.TryGetProperty("party", out var party)) continue;
            string Str(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
            int Int(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;
            string trainerClass = Str(t, "class");
            string style = Str(t, "battle_style");
            var record = new TrainerRecord
            {
                Id = TrainerPrefix + constant["TRAINER_".Length..].ToLowerInvariant(),
                Name = Str(t, "name"),
                Class = ClassName(trainerClass),
                DoubleBattle = style == "BTL_STYLE_DOUBLE",
                Ai = AiOf(t)
            };
            if (t.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
                foreach (var item in items.EnumerateArray())
                    if (item.GetString() is { } i && i != "ITEM_NONE") record.Items.Add(GbaNames.Item(i) ?? Note($"{record.Id}: {i} is not in the game's data", i));
            bool woman = trainerClass.EndsWith("_F", StringComparison.Ordinal);
            uint gender = woman ? 0x78u : 0x88u;
            int classNumber = classes.GetValueOrDefault(trainerClass);
            int last = 0;
            foreach (var p in party.EnumerateArray())
            {
                string speciesConstant = Str(p, "species");
                string species = GbaNames.Species(speciesConstant) ?? Note($"{record.Id}: {speciesConstant} is not in the game's data", speciesConstant);
                int level = Int(p, "level"), difficulty = Int(p, "difficulty");
                int dex = PokemonDatabase.Get(species)?.DexNumber ?? 0;
                var member = new TrainerPokemonRecord
                {
                    Species = species, Level = level, IvScale = difficulty,
                    Personality = HgssTrainers.Personality(difficulty, level, dex, number, classNumber, ref gender)
                };
                if (Int(p, "form") is var form and > 0)
                    member.Form = FormName(species, form) ?? Note($"{record.Id}: {species}'s form {form} has no name here", null!);
                if (p.TryGetProperty("item", out var held) && held.GetString() is { } h && h != "ITEM_NONE")
                    member.Item = GbaNames.Item(h) ?? Note($"{record.Id}: {h} is not in the game's data", h);
                if (p.TryGetProperty("moves", out var moves) && moves.ValueKind == JsonValueKind.Array)
                {
                    var list = moves.EnumerateArray().Select(mv => mv.GetString() ?? "").Where(mv => mv.Length > 0 && mv != "MOVE_NONE")
                        .Select(mv => GbaNames.Move(mv) ?? Note($"{record.Id}: {mv} is not in the game's data", mv)).ToList();
                    if (list.Count > 0) member.Moves = list;
                }
                record.Party.Add(member);
                last = level;
            }
            record.PrizeMoney = last * 4 * Int(t, "money") * (record.DoubleBattle ? 2 : 1);
            records.Add(record);
        }
        return records;
    }

    /// <summary>
    /// A form's name by Black 2's number of it in a trainer's team (<c>form</c>; plan 22 · U2, part b): the forms the
    /// region's trainers use, in Generation 5's order. Null for one this table doesn't know, which the report names.
    /// </summary>
    public static string? FormName(string species, int form) => (species, form) switch
    {
        ("Basculin", 1) => "Basculin-Blue-Striped",
        ("Rotom", 1) => "Rotom-Heat",
        ("Rotom", 2) => "Rotom-Wash",
        ("Rotom", 3) => "Rotom-Frost",
        ("Rotom", 4) => "Rotom-Fan",
        ("Rotom", 5) => "Rotom-Mow",
        ("Wormadam", 1) => "Wormadam-Sandy",
        ("Wormadam", 2) => "Wormadam-Trash",
        ("Shellos" or "Gastrodon", 1) => species + "-East",
        _ => null
    };

    /// <summary>
    /// A class constant in our capitals: <c>TRAINER_CLASS_PKMN_RANGER_M</c> is "Pokémon Ranger". A number the
    /// decompilation gives a class only to tell two of one name apart (<c>TRAINER_CLASS_WORKER_44</c>,
    /// <c>TRAINER_CLASS_SCIENTIST_F_46</c>) is not part of its name (plan 22 · U2, part b).
    /// </summary>
    public static string ClassName(string constant)
    {
        string upper = constant.StartsWith("TRAINER_CLASS_", StringComparison.Ordinal) ? constant["TRAINER_CLASS_".Length..] : constant;
        int last = upper.LastIndexOf('_');
        if (last > 0 && upper[(last + 1)..].All(char.IsDigit)) upper = upper[..last];
        if (upper.EndsWith("_M", StringComparison.Ordinal) || upper.EndsWith("_F", StringComparison.Ordinal)) upper = upper[..^2];
        return string.Join(' ', upper.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(w =>
            w == "PKMN" ? "Pokémon" : w.All(char.IsDigit) ? w : char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));
    }

    private static List<string> AiOf(JsonElement t)
    {
        var flags = new List<string>();
        if (!t.TryGetProperty("ai_flags", out var list) || list.ValueKind != JsonValueKind.Array) return flags;
        foreach (var f in list.EnumerateArray())
        {
            AiFlags? flag = f.GetString() switch
            {
                "AI_FLAG_BASIC" => AiFlags.Basic,
                "AI_FLAG_EVAL_ATTACK" => AiFlags.EvalAttack,
                "AI_FLAG_EXPERT" => AiFlags.Expert,
                "AI_FLAG_TAG_STRATEGY" => AiFlags.TagStrategy,
                "AI_FLAG_TARGET_HP" => AiFlags.CheckHp,
                _ => null
            };
            if (flag is { } known && !flags.Contains(known.ToString())) flags.Add(known.ToString());
        }
        return flags;
    }

    // ---------------------------------------------------------------- habitats

    /// <summary>
    /// Where Unova's wild Pokémon live (<c>habitats.json</c>), for the Pokédex's area page: every zone with a table, its
    /// spring's grass and dark grass, water and rod, and the overworld's chunks as the other regions' files draw them.
    /// </summary>
    public WorldHabitatsFile Habitats()
    {
        var file = new WorldHabitatsFile();
        var over = Maps.Matrix(Bw2Maps.Overworld);
        var cells = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        for (int cy = 0; cy < over.Height; cy++)
        {
            var row = new StringBuilder();
            for (int cx = 0; cx < over.Width; cx++)
            {
                if (over.ModelAt(cx, cy) is not { } model || over.ZoneAt(cx, cy) is not { } z || !Maps.Zones.TryGetValue(z, out var zone)) { row.Append(' '); continue; }
                var look = CoverOf(Bw2Matrix.Folder(model));
                int water = look.Count(c => c == TerrainCover.Water);
                string key = KeyOf(zone.Constant);
                row.Append(key.EndsWith("_city", StringComparison.Ordinal) || key.EndsWith("_town", StringComparison.Ordinal) ? 'T' : water * 2 > look.Length ? '~' : '.');
                if (!cells.TryGetValue(key, out var list)) cells[key] = list = new();
                list.Add($"{cx},{cy}");
            }
            file.Map.Add(row.ToString().TrimEnd());
        }
        static List<string>? Distinct(params List<AreaEncounter>?[] tables)
        {
            var all = tables.Where(t => t != null).SelectMany(t => t!).Select(s => s.Species).Distinct().ToList();
            return all.Count > 0 ? all : null;
        }
        var done = new HashSet<string>(StringComparer.Ordinal);
        foreach (var zone in Maps.Zones.Values)
        {
            if (zone.Encounters == null || !done.Add(KeyOf(zone.Constant))) continue;
            var area = new WorldAreaFile { Key = KeyOf(zone.Constant) };
            Fill(area, zone);
            var habitat = new HabitatArea
            {
                Key = area.Key,
                Name = NameOf(zone),
                Cells = cells.TryGetValue(area.Key, out var own) ? string.Join(' ', own) : "",
                Morning = Distinct(area.Land, area.DarkGrass),
                Day = Distinct(area.Land, area.DarkGrass),
                Night = Distinct(area.Land, area.DarkGrass),
                Surf = Distinct(area.Water),
                SuperRod = Distinct(area.SuperRod)
            };
            if (habitat.Morning == null && habitat.Surf == null && habitat.SuperRod == null) continue;
            file.Areas.Add(habitat);
        }
        return file;
    }
}
