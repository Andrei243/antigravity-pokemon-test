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
        AddFloatingFloors(file, landId);
        return file;
    }

    private Dictionary<int, List<(int X, int Z, DistortionWorld.Floor Floor, int FloorX, int FloorZ)>>? floating;

    /// <summary>
    /// The floors the Distortion World floats over a map's own ground (<see cref="DistortionWorld"/>: B2F's upper
    /// stones) are written into the chunks under them: where the chunk is blocked and the floor is open, the tile is the
    /// floor's, its behaviour and blocked flag, at the floor's height, a plate of its own for each run of such tiles
    /// along a row. The original keeps them apart and walks them as another level of the same map; so does the game.
    /// </summary>
    private void AddFloatingFloors(WorldChunkFile file, int landId)
    {
        if (floating == null)
        {
            floating = new();
            foreach (var (index, floors) in decomp.Distortion?.Floors ?? new Dictionary<int, List<DistortionWorld.Floor>>())
            {
                if (decomp.Headers.Values.FirstOrDefault(h => h.Index == index) is not { } header) continue;
                var matrix = decomp.Matrix(header.Matrix);
                foreach (var floor in floors)
                    for (int z = 0; z < floor.Depth; z++)
                        for (int x = 0; x < floor.Width; x++)
                        {
                            int mx = floor.X + x, mz = floor.Z + z;
                            if (mx < 0 || mz < 0 || mx >= matrix.Width * LandData.Tiles || mz >= matrix.Height * LandData.Tiles) continue;
                            int chunkLand = matrix.LandAt(mx / LandData.Tiles, mz / LandData.Tiles);
                            if (chunkLand == MapImporter.Matrix.NoLand) continue;
                            if (!floating.TryGetValue(chunkLand, out var tiles)) floating[chunkLand] = tiles = new();
                            tiles.Add((mx % LandData.Tiles, mz % LandData.Tiles, floor, x, z));
                        }
            }
        }
        if (!floating.TryGetValue(landId, out var mine)) return;

        var land = decomp.Land(landId);
        var raised = new SortedDictionary<(int Z, int X), int>();
        foreach (var (x, z, floor, fx, fz) in mine)
        {
            if (!land.Solid(x, z) || floor.Solid(fx, fz)) continue;
            var behaviours = file.Behaviours[z].ToCharArray();
            floor.Behaviour(fx, fz).ToString("X2", CultureInfo.InvariantCulture).CopyTo(0, behaviours, x * 2, 2);
            file.Behaviours[z] = new string(behaviours);
            var solid = file.Solid[z].ToCharArray();
            solid[x] = '.';
            file.Solid[z] = new string(solid);
            var cover = file.Cover[z].ToCharArray();
            cover[x] = TerrainCoverCodes.CodeOf(Cover.OfBehaviour((PokemonPlatinumEngine.Overworld.TileBehavior)floor.Behaviour(fx, fz)) ?? TerrainCover.Unknown);
            file.Cover[z] = new string(cover);
            raised[(z, x)] = floor.Height;
        }

        // A plate for each run of the floor's tiles along a row, at the floor's height
        foreach (var row in raised.GroupBy(t => t.Key.Z))
        {
            var tiles = row.OrderBy(t => t.Key.X).ToList();
            for (int i = 0; i < tiles.Count;)
            {
                int j = i;
                while (j + 1 < tiles.Count && tiles[j + 1].Key.X == tiles[j].Key.X + 1 && tiles[j + 1].Value == tiles[i].Value) j++;
                file.Heights.Add(new HeightPlate
                {
                    X = tiles[i].Key.X, Z = row.Key, Width = j - i + 1, Depth = 1, Height = tiles[i].Value, SlopeX = 0, SlopeZ = 0
                });
                i = j + 1;
            }
        }
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

    private Dictionary<int, string>? itemNames;

    /// <summary>The game's name for an item constant, by its number: <c>ITEM_PARLYZ_HEAL</c> is "Paralyze Heal".</summary>
    private string? ItemName(string constant)
    {
        itemNames ??= ItemDatabase.GetAll().GroupBy(i => i.Id).ToDictionary(g => g.Key, g => g.First().Name);
        int id = -1;
        for (int i = 0; i < decomp.ItemIds.Count && id < 0; i++)
            if (decomp.ItemIds[i] == constant) id = i;
        return id >= 0 ? itemNames.GetValueOrDefault(id) : null;
    }

    /// <summary>An item ball's item by the game's name, or null (with the problem noted) when the game has no such item.</summary>
    private (string Item, int Count)? Lying(MapHeader header, AreaEvents.ObjectEvent o)
    {
        if (decomp.VisibleItem(o.Script) is not { } found) return null;
        if (ItemName(found.Item) is { } name) return (name, found.Count);
        string problem = $"{header.Key}: the item ball {o.Id} holds {found.Item}, which is not an item the game knows";
        if (!Problems.Contains(problem)) Problems.Add(problem);
        return null;
    }

    private AreaSign Sign(MapHeader header, AreaEvents.BgEvent s)
    {
        var sign = new AreaSign { X = s.X, Z = s.Z, Type = s.Type, Script = s.Script };
        if (s.Type != AreaSign.HiddenItem || decomp.HiddenItem(s.Script) is not { } hidden) return sign;
        if (ItemName(hidden.Item) is not { } name)
        {
            Problems.Add($"{header.Key}: the hidden item at {s.X},{s.Z} is {hidden.Item}, which is not an item the game knows");
            return sign;
        }
        sign.Item = name;
        sign.Count = hidden.Count > 1 ? hidden.Count : null;
        sign.Flag = hidden.Flag;
        sign.Range = hidden.Range;
        return sign;
    }

    public WorldAreaFile Area(MapHeader header, string name)
    {
        var events = decomp.Events(header.Events);
        // The Distortion World gives its events in its own space, where each floor lies at an offset of its own
        var (ox, _, oz) = decomp.Distortion?.Offsets.GetValueOrDefault(header.Index) ?? default;
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
            Day = land == null ? null : SlotSpecies(header, "day", 2),
            Night = land == null ? null : SlotSpecies(header, "night", 2),
            Swarm = land == null ? null : SlotSpecies(header, "swarms", 2),
            Radar = land == null ? null : SlotSpecies(header, "radar", 4),
            Water = water,
            WaterRate = water == null ? null : waterRate,
            OldRod = Water(header, "old_rod", out int? oldRodRate),
            OldRodRate = oldRodRate,
            GoodRod = Water(header, "good_rod", out int? goodRodRate),
            GoodRodRate = goodRodRate,
            SuperRod = Water(header, "super_rod", out int? superRodRate),
            SuperRodRate = superRodRate,
            EastSea = header.Encounters != null && decomp.Forms(header.Encounters).EastSea ? true : null,
            UnownTable = header.Encounters != null && decomp.Forms(header.Encounters).UnownTable is > 0 and var unown ? unown : null,
            Warps = events.Warps.Select(w => new AreaWarp { X = w.X - ox, Z = w.Z - oz, To = KeyOf(w.DestHeaderId), ToWarp = w.DestWarpId }).ToList(),
            Objects = events.Objects.Select(o => new AreaObject
            {
                Id = Trim(o.Id, "LOCALID_", "").ToLowerInvariant(),
                Looks = Trim(o.GraphicsId, "OBJ_EVENT_GFX_", "").ToLowerInvariant(),
                Movement = Trim(o.MovementType, "MOVEMENT_TYPE_", "").ToLowerInvariant(),
                X = o.X - ox,
                Z = o.Z - oz,
                Y = o.Y > 0 ? o.Y : null,
                Facing = o.InitialDir,
                RangeX = o.MovementRangeX,
                RangeZ = o.MovementRangeZ,
                Trainer = o.TrainerType is "TRAINER_TYPE_NONE" or "" ? null : Trim(o.TrainerType, "TRAINER_TYPE_", "").ToLowerInvariant(),
                // A trainer the data gives no range sees nobody coming: they battle when spoken to
                Sight = o.TrainerType is "TRAINER_TYPE_NONE" or "" || !o.Script.StartsWith("TRAINER_", StringComparison.Ordinal) ? null : o.Data.Count == 0 ? 0 : o.Data[0],
                HiddenBy = o.HiddenFlag is "0" or "" ? null : o.HiddenFlag,
                Item = Lying(header, o)?.Item,
                Count = Lying(header, o) is { Count: > 1 } several ? several.Count : null,
                Script = o.Script
            }).ToList(),
            Signs = events.Signs.Select(s => Sign(header, s)).Select(s => { s.X -= ox; s.Z -= oz; return s; }).ToList(),
            Triggers = events.Triggers.Select(t => new AreaTrigger
            {
                X = t.X - ox, Z = t.Z - oz, Width = Math.Max(1, t.Width), Depth = Math.Max(1, t.Length), Script = t.Script, Variable = t.Var, Value = t.Value
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

    /// <summary>
    /// One of the lists an area's table keeps beside its land slots (plan 06 · R13: the day's and the night's two,
    /// a swarm's two, the Poké Radar's four) by the game's names; null when the area has none, or when a species in
    /// it isn't one the game knows (reported).
    /// </summary>
    private List<string>? SlotSpecies(MapHeader header, string list, int count)
    {
        if (header.Encounters == null) return null;
        var constants = decomp.SlotList(header.Encounters, list);
        if (constants.Count == 0) return null;
        if (constants.Count != count) Problems.Add($"{header.Key}: its {list} list has {constants.Count} species, not {count}");
        var names = new List<string>();
        foreach (string constant in constants)
        {
            if (SpeciesName(constant) is { } name) names.Add(name);
            else
            {
                Problems.Add($"{header.Key}: {constant} in its {list} list is not a species the game knows");
                return null;
            }
        }
        return names;
    }

    /// <summary>
    /// The tables of wild Pokémon that belong to no one area (plan 06 · R13): the honey trees', the Great Marsh's
    /// daily Pokémon, the Trophy Garden's and Feebas's lake, as the original builds <c>encdata_ex</c> from them.
    /// Feebas's tiles are kept in the original's order, from its <c>(z × 32 × the matrix's width) + x</c> to
    /// tiles of the lake's matrix.
    /// </summary>
    public WorldEncountersFile Encounters(Func<MapHeader, string> nameOf)
    {
        List<string> Names(System.Text.Json.JsonElement array, string what)
        {
            var names = new List<string>();
            foreach (var e in array.EnumerateArray())
            {
                string constant = e.GetString() ?? "";
                if (SpeciesName(constant) is { } name) names.Add(name);
                else Problems.Add($"{what}: {constant} is not a species the game knows");
            }
            return names;
        }

        var honey = decomp.EncounterFile("encounters_honey_tree");
        var marsh = decomp.EncounterFile("encounters_great_marsh_lookout");
        var trophy = decomp.EncounterFile("encounters_trophy_garden");
        var coronet = decomp.EncounterFile("encounters_mt_coronet_b1f").GetProperty("elusive_rod_encounter");

        // The lake's header: the only one whose table has an elusive rod (MapHeader_HasFeebasTiles)
        var lake = decomp.Headers.Values.First(h => h.Encounters == "encounters_mt_coronet_b1f");
        int width = decomp.Matrix(lake.Matrix).Width * LandData.Tiles;
        var tiles = coronet.GetProperty("tiles").EnumerateArray().Select(t => t.GetInt32()).ToList();
        int expected = coronet.GetProperty("map_dimensions").EnumerateArray().Sum(d => d.GetInt32());
        if (tiles.Count != expected) Problems.Add($"Feebas's lake lists {tiles.Count} tiles where its maps count {expected}");

        return new WorldEncountersFile
        {
            HoneyTrees = new HoneyTreeTables
            {
                Common = Names(honey.GetProperty("common"), "honey trees"),
                Uncommon = Names(honey.GetProperty("uncommon"), "honey trees"),
                Rare = Names(honey.GetProperty("rare"), "honey trees")
            },
            GreatMarsh = new GreatMarshTables
            {
                Local = Names(marsh.GetProperty("before_national_dex"), "the Great Marsh"),
                National = Names(marsh.GetProperty("after_national_dex"), "the Great Marsh")
            },
            TrophyGarden = Names(trophy.GetProperty("daily_encounters"), "the Trophy Garden"),
            Feebas = new FeebasTiles
            {
                Area = lake.Key,
                Species = SpeciesName(coronet.GetProperty("species").GetString() ?? "") ?? "",
                Tiles = string.Join(' ', tiles.Select(t => $"{t % width},{t / width}"))
            },
            Swarms = PokemonPlatinumEngine.Overworld.Swarms.Areas.Select(key =>
            {
                var header = decomp.Headers.Values.First(h => h.Key == key);
                string? species = header.Encounters == null ? null : decomp.SlotList(header.Encounters, "swarms").FirstOrDefault();
                if (species == null || SpeciesName(species) is not { } name) Problems.Add($"{key}: no species swarms there");
                return new SwarmPlace { Area = key, Name = nameOf(header), Species = species != null ? SpeciesName(species) ?? "" : "" };
            }).ToList()
        };
    }

    private List<AreaEncounter>? Water(MapHeader header, out int? rate) => Water(header, "surf", out rate);

    /// <summary>One of an area's tables of water slots (<c>surf</c>, or a rod's), with its rate; null when it has none.</summary>
    private List<AreaEncounter>? Water(MapHeader header, string table, out int? rate)
    {
        rate = null;
        if (header.Encounters == null) return null;
        var water = new List<AreaEncounter>();
        foreach (var (species, min, max) in decomp.WaterEncounters(header.Encounters, table, out int waterRate))
        {
            rate = waterRate;
            if (SpeciesName(species) is { } name) water.Add(new AreaEncounter { Species = name, Level = min, MaxLevel = max > min ? max : null });
            else Problems.Add($"{header.Key}: wild {species} in the water ({table}) is not a species the game knows");
        }
        if (water.Count == 0) rate = null;
        return water.Count > 0 ? water : null;
    }

    /// <summary>
    /// Where every wild Pokémon of the region lives, for the Pokédex's area page: each area with a table of wild
    /// Pokémon, with the species of its grass at each time of day, of its water and of each rod, and the overworld
    /// chunks it is shown on; and the overworld's look, one character per chunk.
    /// </summary>
    public WorldHabitatsFile Habitats(Func<MapHeader, string> nameOf)
    {
        var overworld = decomp.Matrix(0);
        var file = new WorldHabitatsFile();
        var outdoors = new HashSet<string>(overworld.Headers ?? Array.Empty<string>());

        // The overworld's look: water where most of a chunk is, the towns' own chunks, land elsewhere
        for (int y = 0; y < overworld.Height; y++)
        {
            var row = new StringBuilder(overworld.Width);
            for (int x = 0; x < overworld.Width; x++)
            {
                int land = overworld.LandAt(x, y);
                if (land == MapImporter.Matrix.NoLand) row.Append(' ');
                else if (overworld.HeaderAt(x, y) is { } id && decomp.Headers.TryGetValue(id, out var h) && h.MapType == "TOWN_CITY") row.Append('T');
                else row.Append(CoverOf(land).Count(c => c == TerrainCover.Water) * 2 > LandData.Tiles * LandData.Tiles ? '~' : '.');
            }
            file.Map.Add(row.ToString());
        }

        // Where each area is shown: an outdoor area on its own chunks, a cave or a building on the chunks whose warps
        // lead into it (through any rooms between), so every floor of a cave lights up its entrances
        var cells = new Dictionary<string, SortedSet<(int X, int Y)>>();
        SortedSet<(int X, int Y)> CellsOf(string id) => cells.TryGetValue(id, out var set) ? set : cells[id] = new SortedSet<(int X, int Y)>();
        for (int y = 0; y < overworld.Height; y++)
            for (int x = 0; x < overworld.Width; x++)
                if (overworld.HeaderAt(x, y) is { } id) CellsOf(id).Add((x, y));
        var queue = new Queue<(string Id, (int, int) Cell)>();
        foreach (string id in outdoors)
            if (decomp.Headers.TryGetValue(id, out var h))
                foreach (var warp in decomp.Events(h.Events).Warps)
                    queue.Enqueue((warp.DestHeaderId, (warp.X / LandData.Tiles, warp.Z / LandData.Tiles)));
        while (queue.Count > 0)
        {
            var (id, cell) = queue.Dequeue();
            if (outdoors.Contains(id) || id == DecompMaps.DynamicHeader || !decomp.Headers.TryGetValue(id, out var h)) continue;
            if (!CellsOf(id).Add(cell)) continue;
            foreach (var warp in decomp.Events(h.Events).Warps) queue.Enqueue((warp.DestHeaderId, cell));
        }

        // Places a script takes the player into rather than a warp: Turnback Cave's inner rooms are shown where the
        // rest of the cave is (any area of the same name), and the Great Marsh, entered from a gate in Pastoria City
        // once the fee is paid, where the city is
        foreach (var header in decomp.Headers.Values)
        {
            if (CellsOf(header.Id).Count > 0) continue;
            string name = nameOf(header);
            var same = decomp.Headers.Values.Where(h => h.Id != header.Id && nameOf(h) == name).SelectMany(h => CellsOf(h.Id)).ToList();
            var cellsOf = same.Count > 0 ? same : name == "Great Marsh"
                ? decomp.Headers.Values.Where(h => h.Key == "pastoria_city").SelectMany(h => CellsOf(h.Id)).ToList()
                : new();
            foreach (var cell in cellsOf) CellsOf(header.Id).Add(cell);
        }

        foreach (var header in decomp.Headers.Values.OrderBy(h => h.Index))
        {
            if (header.Encounters == null || decomp.Encounters(header.Encounters) is not { } table) continue;
            List<string>? Species(IEnumerable<string> slots, string way)
            {
                var names = new List<string>();
                foreach (string constant in slots.Where(s => s is not ("" or "SPECIES_NONE")).Distinct())
                {
                    if (SpeciesName(constant) is { } name) names.Add(name);
                    else Problems.Add($"{header.Key}: {constant} ({way}) is not a species the game knows");
                }
                return names.Count > 0 ? names : null;
            }
            var area = new HabitatArea
            {
                Key = header.Key,
                Name = nameOf(header),
                Cells = string.Join(' ', CellsOf(header.Id).Select(c => $"{c.X},{c.Y}")),
                Morning = Species(table.Grass("morning"), "morning"),
                Day = Species(table.Grass("day"), "day"),
                Night = Species(table.Grass("night"), "night"),
                Surf = Species(table.Surf, "surfing"),
                OldRod = Species(table.OldRod, "Old Rod"),
                GoodRod = Species(table.GoodRod, "Good Rod"),
                SuperRod = Species(table.SuperRod, "Super Rod")
            };
            if ((area.Morning ?? area.Day ?? area.Night ?? area.Surf ?? area.OldRod ?? area.GoodRod ?? area.SuperRod) is null) continue;
            file.Areas.Add(area);
        }
        return file;
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

        // The whole region's wild Pokémon, open or not, for the Pokédex, and the calendar of its weather
        File.WriteAllText(Path.Combine(directory, WorldHabitatsFile.FileName), GameDataFiles.Serialize(Habitats(nameOf)));
        File.WriteAllText(Path.Combine(directory, WorldCalendarFile.FileName), GameDataFiles.Serialize(Calendar()));
        File.WriteAllText(Path.Combine(directory, WorldEncountersFile.FileName), GameDataFiles.Serialize(Encounters(nameOf)));

        foreach (string folder in new[] { "matrices", "chunks", "areas" })
        {
            string path = Path.Combine(directory, folder);
            if (!Directory.Exists(path)) continue;
            foreach (string file in Directory.GetFiles(path, "*.json"))
                if (!wanted.Contains(Path.GetFullPath(file))) File.Delete(file);
        }
        return wanted.Count;
    }

    /// <summary>Platinum's weather calendar (<c>sYearlyWeather</c>), as the game reads it.</summary>
    public WorldCalendarFile Calendar() => new() { Places = decomp.Calendar.Places, Days = decomp.Calendar.Days };

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
        File.WriteAllText(Path.Combine(directory, WorldHabitatsFile.FileName), GameDataFiles.Serialize(Habitats(nameOf)));
        File.WriteAllText(Path.Combine(directory, WorldCalendarFile.FileName), GameDataFiles.Serialize(Calendar()));
        File.WriteAllText(Path.Combine(directory, WorldEncountersFile.FileName), GameDataFiles.Serialize(Encounters(nameOf)));
        return files + 3;
    }
}
