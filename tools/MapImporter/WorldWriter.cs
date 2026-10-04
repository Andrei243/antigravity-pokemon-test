using System.Globalization;
using System.Text;
using PokemonPlatinumEngine.Data;

namespace MapImporter;

/// <summary>
/// Turns what was read from the decompilation into the game's own world files (<see cref="WorldChunkFile"/>,
/// <see cref="WorldMatrixFile"/>, <see cref="WorldAreaFile"/>): our units (tiles), our names, and nothing of the
/// original's models beyond a name and a box per prop.
/// </summary>
public sealed class WorldWriter
{
    private readonly DecompMaps decomp;
    private readonly Dictionary<int, TerrainCover[]> cover = new();

    public WorldWriter(DecompMaps decomp) => this.decomp = decomp;

    /// <summary>The look of each tile of a chunk, worked out once.</summary>
    public TerrainCover[] CoverOf(int landId)
    {
        if (!cover.TryGetValue(landId, out var result))
            cover[landId] = result = Cover.Of(decomp.Land(landId), decomp.PropModel);
        return result;
    }

    public WorldChunkFile Chunk(int landId)
    {
        var land = decomp.Land(landId);
        var look = CoverOf(landId);
        var file = new WorldChunkFile { Id = landId };

        for (int z = 0; z < LandData.Tiles; z++)
        {
            var behaviours = new StringBuilder(LandData.Tiles * 2);
            var solid = new StringBuilder(LandData.Tiles);
            var row = new StringBuilder(LandData.Tiles);
            for (int x = 0; x < LandData.Tiles; x++)
            {
                behaviours.Append(land.Behaviour(x, z).ToString("X2", CultureInfo.InvariantCulture));
                solid.Append(land.Solid(x, z) ? '#' : '.');
                row.Append(TerrainCoverCodes.CodeOf(look[z * LandData.Tiles + x]));
            }
            file.Behaviours.Add(behaviours.ToString());
            file.Solid.Add(solid.ToString());
            file.Cover.Add(row.ToString());
        }

        file.Heights = land.Heights.Plates.Select(ToPlate).ToList();
        file.Props = land.Props.Select(ToProp).ToList();
        return file;
    }

    // Rounded once, at the end and in double precision, so 10.1114 is written as 10.1114 and a level slope as 0
    private static float Round(double value) => (float)Math.Round(value, 4) + 0f;

    /// <summary>A length in tiles.</summary>
    private static float Tiles(float units) => Round(units / (double)LandData.TileUnits);

    /// <summary>A position along a chunk, in tiles from its north-west corner (the original measures from the centre).</summary>
    private static float Along(float units) => Round(units / (double)LandData.TileUnits + LandData.Tiles / 2.0);

    /// <summary>A plate in tiles from the chunk's north-west corner: its height there and how it rises east and south.</summary>
    public static HeightPlate ToPlate(HeightPlates.Plate p) => new()
    {
        X = Along(p.MinX),
        Z = Along(p.MinZ),
        Width = Tiles(p.MaxX - p.MinX),
        Depth = Tiles(p.MaxZ - p.MinZ),
        Height = Tiles(p.HeightAt(p.MinX, p.MinZ)),
        SlopeX = Round(-p.Normal.X / (double)p.Normal.Y),
        SlopeZ = Round(-p.Normal.Z / (double)p.Normal.Y)
    };

    private ChunkProp ToProp(LandProp prop)
    {
        var model = decomp.PropModel(prop.ModelId);
        return new ChunkProp
        {
            Model = prop.ModelId,
            Name = model?.Name ?? "",
            X = Along(prop.Position.X),
            Y = Tiles(prop.Position.Y),
            Z = Along(prop.Position.Z),
            BoxX = Along(prop.Position.X + (model?.BoxMin.X ?? 0)),
            BoxZ = Along(prop.Position.Z + (model?.BoxMin.Z ?? 0)),
            Width = Tiles(model?.BoxSize.X ?? 0),
            Depth = Tiles(model?.BoxSize.Z ?? 0),
            Height = Tiles(model?.BoxSize.Y ?? 0)
        };
    }

    public WorldMatrixFile Matrix(Matrix matrix)
    {
        var file = new WorldMatrixFile { Id = matrix.Id, Width = matrix.Width, Height = matrix.Height };
        var keys = matrix.Headers?.Distinct().Select(KeyOf).ToList();
        int chunkWidth = matrix.Land.Select(l => l == MapImporter.Matrix.NoLand ? 1 : l.ToString(CultureInfo.InvariantCulture).Length).DefaultIfEmpty(1).Max();
        int areaWidth = keys == null ? 1 : (keys.Count - 1).ToString(CultureInfo.InvariantCulture).Length;

        for (int y = 0; y < matrix.Height; y++)
        {
            var cells = Enumerable.Range(0, matrix.Width).ToList();
            file.Chunks.Add(WorldMatrixFile.Row(cells.Select(x => matrix.LandAt(x, y) is var l and not MapImporter.Matrix.NoLand ? l.ToString(CultureInfo.InvariantCulture) : "-"), chunkWidth));
            if (keys != null)
                (file.Areas ??= new()).Add(WorldMatrixFile.Row(cells.Select(x => keys.IndexOf(KeyOf(matrix.HeaderAt(x, y)!)).ToString(CultureInfo.InvariantCulture)), areaWidth));
            if (matrix.Altitudes != null)
                (file.Altitudes ??= new()).Add(WorldMatrixFile.Row(cells.Select(x => matrix.AltitudeAt(x, y).ToString(CultureInfo.InvariantCulture)), 2));
        }
        file.AreaKeys = keys;
        return file;
    }

    public WorldAreaFile Area(MapHeader header, string name)
    {
        var events = decomp.Events(header.Events);
        var land = Land(header, out int? landRate);
        var water = Water(header, out int? waterRate);
        return new WorldAreaFile
        {
            Key = header.Key,
            Index = header.Index,
            Name = name,
            Matrix = header.Matrix,
            Kind = header.MapType switch { "TOWN_CITY" => "Town", "POKECENTER" => "PokemonCenter", var t => Pascal(t) },
            Sign = Pascal(header.LabelWindow),
            Weather = Pascal(header.Weather),
            Camera = Pascal(header.Camera),
            BattleBackground = Pascal(header.BattleBackground),
            DayMusic = Trim(header.DayMusic, "SEQ_", "_sseq"),
            NightMusic = Trim(header.NightMusic, "SEQ_", "_sseq"),
            Encounters = header.Encounters == null ? null : Trim(header.Encounters, "encounters_", ""),
            Bike = header.Bike,
            Running = header.Running,
            EscapeRope = header.EscapeRope,
            Fly = header.Fly,
            Land = land,
            LandRate = land == null ? null : landRate,
            Water = water,
            WaterRate = water == null ? null : waterRate,
            Warps = events.Warps.Select(w => new AreaWarp { X = w.X, Z = w.Z, To = KeyOf(w.DestHeaderId), ToWarp = w.DestWarpId }).ToList(),
            Objects = events.Objects.Select(o => new AreaObject
            {
                Id = Trim(o.Id, "LOCALID_", "").ToLowerInvariant(),
                Looks = Trim(o.GraphicsId, "OBJ_EVENT_GFX_", "").ToLowerInvariant(),
                Movement = Trim(o.MovementType, "MOVEMENT_TYPE_", "").ToLowerInvariant(),
                X = o.X,
                Z = o.Z,
                Facing = o.InitialDir,
                RangeX = o.MovementRangeX,
                RangeZ = o.MovementRangeZ,
                Trainer = o.TrainerType is "TRAINER_TYPE_NONE" or "" ? null : Trim(o.TrainerType, "TRAINER_TYPE_", "").ToLowerInvariant(),
                HiddenBy = o.HiddenFlag is "0" or "" ? null : o.HiddenFlag,
                Script = o.Script
            }).ToList(),
            Signs = events.Signs.Select(s => new AreaSign { X = s.X, Z = s.Z, Type = s.Type, Script = s.Script }).ToList(),
            Triggers = events.Triggers.Select(t => new AreaTrigger
            {
                X = t.X, Z = t.Z, Width = Math.Max(1, t.Width), Depth = Math.Max(1, t.Length), Script = t.Script, Variable = t.Var, Value = t.Value
            }).ToList()
        };
    }

    /// <summary>Problems met while writing: a species the game's data doesn't have, say.</summary>
    public List<string> Problems { get; } = new();

    private Dictionary<string, string>? speciesNames;

    /// <summary>The game's name for a species constant: <c>SPECIES_MIME_JR</c> is "Mime Jr.".</summary>
    private string? SpeciesName(string constant)
    {
        static string Plain(string text) => new(text.Replace("♀", "F").Replace("♂", "M").Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
        speciesNames ??= PokemonDatabase.GetAll().GroupBy(s => Plain(s.Name)).ToDictionary(g => g.Key, g => g.First().Name);
        return speciesNames.GetValueOrDefault(Plain(Trim(constant, "SPECIES_", "")));
    }

    private List<AreaEncounter>? Land(MapHeader header, out int? rate)
    {
        rate = null;
        if (header.Encounters == null) return null;
        var land = new List<AreaEncounter>();
        foreach (var (species, level) in decomp.LandEncounters(header.Encounters, out int landRate))
        {
            rate = landRate;
            if (SpeciesName(species) is { } name) land.Add(new AreaEncounter { Species = name, Level = level });
            else Problems.Add($"{header.Key}: wild {species} is not a species the game knows");
        }
        return land.Count > 0 ? land : null;
    }

    private List<AreaEncounter>? Water(MapHeader header, out int? rate)
    {
        rate = null;
        if (header.Encounters == null) return null;
        var water = new List<AreaEncounter>();
        foreach (var (species, min, max) in decomp.WaterEncounters(header.Encounters, out int waterRate))
        {
            rate = waterRate;
            if (SpeciesName(species) is { } name) water.Add(new AreaEncounter { Species = name, Level = min, MaxLevel = max > min ? max : null });
            else Problems.Add($"{header.Key}: wild {species} in the water is not a species the game knows");
        }
        return water.Count > 0 ? water : null;
    }

    public static string KeyOf(string headerId) => Trim(headerId, "MAP_HEADER_", "").ToLowerInvariant();

    private static string Trim(string text, string prefix, string suffix)
    {
        if (prefix.Length > 0 && text.StartsWith(prefix, StringComparison.Ordinal)) text = text[prefix.Length..];
        if (suffix.Length > 0 && text.EndsWith(suffix, StringComparison.Ordinal)) text = text[..^suffix.Length];
        return text;
    }

    /// <summary><c>HEAVY_RAIN</c> becomes <c>HeavyRain</c>.</summary>
    public static string Pascal(string constant) => string.Concat(constant.Split('_', StringSplitOptions.RemoveEmptyEntries)
        .Select(word => char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));

    /// <summary>
    /// Writes what the game needs of the world into its data folder, going by the index there (<c>world.json</c>):
    /// the matrices of its maps, the areas that are open, and every chunk of those areas with the ring of chunks
    /// round them, which are seen from inside. Files of an earlier run that are no longer needed are removed; the
    /// index and the overlays, which are written by hand, are never touched.
    /// </summary>
    public int WriteGameData(string directory, Func<MapHeader, string> nameOf)
    {
        var index = System.Text.Json.JsonSerializer.Deserialize<WorldIndexFile>(File.ReadAllText(Path.Combine(directory, World.IndexFile)), GameDataFiles.Json)!;
        var open = new HashSet<string>(index.Areas, StringComparer.OrdinalIgnoreCase);
        var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Write<T>(string folder, string name, T value)
        {
            string path = Path.Combine(directory, folder, name + ".json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, GameDataFiles.Serialize(value));
            wanted.Add(Path.GetFullPath(path));
        }

        foreach (var entry in index.Maps)
        {
            var matrix = decomp.Matrix(entry.Matrix);
            Write("matrices", entry.Matrix.ToString("000", CultureInfo.InvariantCulture), Matrix(matrix));

            var chunks = new SortedSet<int>();
            for (int y = 0; y < matrix.Height; y++)
                for (int x = 0; x < matrix.Width; x++)
                {
                    string area = matrix.HeaderAt(x, y) is { } id ? KeyOf(id) : entry.Area ?? "";
                    if (!open.Contains(area)) continue;
                    for (int ny = Math.Max(0, y - 1); ny <= Math.Min(matrix.Height - 1, y + 1); ny++)
                        for (int nx = Math.Max(0, x - 1); nx <= Math.Min(matrix.Width - 1, x + 1); nx++)
                            if (matrix.LandAt(nx, ny) is var land and not MapImporter.Matrix.NoLand) chunks.Add(land);
                }
            foreach (int id in chunks) Write("chunks", id.ToString("000", CultureInfo.InvariantCulture), Chunk(id));
        }

        foreach (string key in index.Areas)
        {
            var header = decomp.Headers.Values.FirstOrDefault(h => string.Equals(h.Key, key, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidDataException($"{World.IndexFile} lists the area '{key}', which the decompilation doesn't have");
            Write("areas", header.Key, Area(header, nameOf(header)));
        }

        foreach (string folder in new[] { "matrices", "chunks", "areas" })
        {
            string path = Path.Combine(directory, folder);
            if (!Directory.Exists(path)) continue;
            foreach (string file in Directory.GetFiles(path, "*.json"))
                if (!wanted.Contains(Path.GetFullPath(file))) File.Delete(file);
        }
        return wanted.Count;
    }

    /// <summary>Writes every matrix, chunk and area under <paramref name="directory"/> and returns how many files.</summary>
    public int WriteAll(string directory, Func<MapHeader, string> nameOf)
    {
        int files = 0;
        void Write<T>(string folder, string name, T value)
        {
            string path = Path.Combine(directory, folder, name + ".json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, GameDataFiles.Serialize(value));
            files++;
        }

        for (int id = 0; id < decomp.MatrixCount; id++) Write("matrices", id.ToString("000", CultureInfo.InvariantCulture), Matrix(decomp.Matrix(id)));
        for (int id = 0; id < decomp.LandCount; id++) Write("chunks", id.ToString("000", CultureInfo.InvariantCulture), Chunk(id));
        foreach (var header in decomp.Headers.Values.OrderBy(h => h.Index)) Write("areas", header.Key, Area(header, nameOf(header)));
        return files;
    }
}
