using System.Globalization;
using System.Text;
using MapImporter.Gba;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace MapImporter.Hg;

/// <summary>
/// What was read of HeartGold turned into the game's world files (plan 20 · J1), as <see cref="WorldWriter"/> turns
/// Sinnoh's: the chunks with their behaviours read as this game's (<see cref="HgBehaviours"/>), their look
/// (<see cref="HgCover"/>), heights and props; the matrices; and every area with its events and its wild Pokémon.
/// A room's props are models of <c>bm_room.narc</c>, everything else's of <c>bm_field.narc</c> (the loader that picks
/// the set, the area data's, is still assembly in the decompilation; the interiors' map type is what tells them apart,
/// and the props' names agree: a house outdoors, a bed in a room).
/// </summary>
public sealed class HgWorld
{
    public HgMaps Maps { get; }
    public byte[] Behaviours { get; }
    public List<HgEncounters> Encounters { get; }
    public List<HgHeadbutt> Headbutt { get; }
    public List<HgSafariArea> Safari { get; }
    public List<TrainerRecord> Trainers { get; }
    public List<PhoneContact> Phone { get; }
    public List<string> Problems { get; } = new();

    /// <summary>
    /// What every trainer id of HeartGold's takes before it (<c>johto_leader_falkner</c>), as FireRed's take
    /// <c>kanto_</c> (plan 19 · K2): <c>TrainerDatabase</c> reads every region's file into one table, and ids must be
    /// unique across them. HeartGold's Kanto trainers are in Johto's file with the same prefix (plan 20).
    /// </summary>
    public const string TrainerPrefix = "johto_";

    private readonly Dictionary<int, TerrainCover[]> cover = new();
    private readonly Dictionary<int, bool> roomChunk = new();

    public HgWorld(HgMaps maps)
    {
        Maps = maps;
        Behaviours = HgBehaviours.Table(maps.BehaviourNames);
        foreach (var (value, name) in maps.BehaviourNames)
            if (!HgBehaviours.ByName.ContainsKey(name)) Problems.Add($"The behaviour {name} (0x{value:X2}) is not in HgBehaviours: it keeps its number");
        Encounters = HgssEncounters.Parse(System.IO.File.ReadAllText(maps.PathOf("files", "fielddata", "encountdata", "gs_enc_data.json")));
        Headbutt = HgssEncounters.ParseHeadbutt(System.IO.File.ReadAllText(maps.PathOf("files", "arc", "headbutt.json")));
        Safari = HgssEncounters.ParseSafari(System.IO.File.ReadAllText(maps.PathOf("files", "arc", "safari_enc.json")));
        Trainers = HgssTrainers.Parse(System.IO.File.ReadAllText(maps.PathOf("files", "poketool", "trainer", "trainers.json")),
            System.IO.File.ReadAllText(maps.PathOf("include", "constants", "trainers.h")), Problems, TrainerPrefix,
            System.IO.File.ReadAllText(maps.PathOf("include", "constants", "trainer_class.h")),
            System.IO.File.ReadAllText(maps.PathOf("src", "trainer_data.c")),
            System.IO.File.ReadAllText(maps.PathOf("asm", "overlay_12_battle_command.s")));
        HiddenItems = ReadHiddenItems(maps);
        Phone = Phonebook.Parse(System.IO.File.ReadAllText(maps.PathOf("files", "tel", "pmtel_book.json")), Problems, TrainerPrefix);
        // A contact who is a trainer battles again with the teams HeartGold numbers after their first (plan 20 · J3:
        // TRAINER_YOUNGSTER_JOEY_2 to _5), which the phone's rematches bring in turn as plan 06 · R12's row of levels
        var byId = Trainers.ToDictionary(t => t.Id, StringComparer.Ordinal);
        foreach (var contact in Phone)
        {
            if (contact.Trainer is not { } id || !byId.TryGetValue(id, out var first)) continue;
            var row = new List<string?>();
            for (int n = 2; byId.ContainsKey($"{id}_{n}"); n++) row.Add($"{id}_{n}");
            if (row.Count > 0) first.Rematches = row;
        }

        // Which chunks are rooms: those of a matrix whose areas are interiors
        foreach (var header in maps.Headers.Values)
        {
            if (header.Matrix == 0) continue;
            var matrix = maps.Matrix(header.Matrix);
            foreach (int land in matrix.Land)
                if (land != Matrix.NoLand) roomChunk[land] = roomChunk.GetValueOrDefault(land) || header.MapType == "INTERIOR" || RoomsByTheirModels.Contains(header.Key);
        }
    }

    /// <summary>
    /// What each hidden item holds, by the label its sign's script has (<c>std_hiddenitem_r29_r30_t21_nugget</c>; plan 20
    /// · J2): <c>include/constants/hidden_items.h</c> numbers the labels, and the row of <c>sHiddenItemParam</c>
    /// (<c>src/data/fieldmap/hidden_items.h</c>) whose last field is that number gives the item and how many. Its flag is
    /// ours, named after the label (<c>FLAG_HIDDEN_ITEM_R29_R30_T21_NUGGET</c>), as FireRed's are.
    /// </summary>
    public Dictionary<string, (string Item, int Count, string Flag)> HiddenItems { get; }

    private Dictionary<string, (string Item, int Count, string Flag)> ReadHiddenItems(HgMaps maps)
    {
        var found = new Dictionary<string, (string, int, string)>(StringComparer.Ordinal);
        string constants = maps.PathOf("include", "constants", "hidden_items.h"), table = maps.PathOf("src", "data", "fieldmap", "hidden_items.h");
        if (!System.IO.File.Exists(constants) || !System.IO.File.Exists(table)) return found;
        var rows = new Dictionary<int, (string Item, int Count)>();
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(System.IO.File.ReadAllText(table),
                     @"\{\s*(ITEM_\w+)\s*,\s*(\d+)\s*,\s*\d+\s*,\s*\d+\s*,\s*(\d+)\s*\}"))
            rows.TryAdd(int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture), (m.Groups[1].Value, int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)));
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(System.IO.File.ReadAllText(constants),
                     @"^#define\s+HIDDENITEM_(\w+)\s+(\d+)", System.Text.RegularExpressions.RegexOptions.Multiline))
        {
            if (!rows.TryGetValue(int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture), out var row)) continue;
            string constant = row.Item == "ITEM_PARLYZ_HEAL" ? "ITEM_PARALYZE_HEAL" : row.Item;
            string item = GbaNames.Item(constant) ?? Note($"the hidden item {m.Groups[1].Value} holds {row.Item}, which the game's data lacks", constant);
            found["std_hiddenitem_" + m.Groups[1].Value.ToLowerInvariant()] = (item, row.Count, "FLAG_HIDDEN_ITEM_" + m.Groups[1].Value);
        }
        return found;
    }

    /// <summary>
    /// Areas whose header calls them a cave but whose models are the rooms' (plan 20 · J8): Olivine's lighthouse, whose
    /// floors read from <c>bm_field.narc</c> come out as Ecruteak's Mart and New Bark's houses, and from
    /// <c>bm_room.narc</c> as its own plants, ladders and lift door. The area data that picks the set is still not read;
    /// the light room at the top (another bank of area data) reads right from the field's (its sky, its lift's door).
    /// </summary>
    private static readonly HashSet<string> RoomsByTheirModels = new(StringComparer.Ordinal)
    {
        "olivine_lighthouse_1f", "olivine_lighthouse_2f", "olivine_lighthouse_3f", "olivine_lighthouse_4f", "olivine_lighthouse_5f"
    };

    public bool IsRoom(int landId) => roomChunk.GetValueOrDefault(landId);

    public ModelInfo? PropModel(int landId, int model) => Maps.Model(model, IsRoom(landId));

    /// <summary>The look of each tile of a chunk, worked out once.</summary>
    public TerrainCover[] CoverOf(int landId)
    {
        if (!cover.TryGetValue(landId, out var result))
            cover[landId] = result = HgCover.Of(Maps.Land(landId), m => PropModel(landId, m), Behaviours);
        return result;
    }

    /// <summary>The behaviour of a chunk's tile as this game's.</summary>
    public byte Behaviour(LandData land, int x, int z) => Behaviours[land.Behaviour(x, z)];

    // ---------------------------------------------------------------- world files

    public WorldChunkFile Chunk(int landId)
    {
        var land = Maps.Land(landId);
        var look = CoverOf(landId);
        var file = new WorldChunkFile { Id = landId };
        for (int z = 0; z < LandData.Tiles; z++)
        {
            var behaviours = new StringBuilder(LandData.Tiles * 2);
            var solid = new StringBuilder(LandData.Tiles);
            var row = new StringBuilder(LandData.Tiles);
            for (int x = 0; x < LandData.Tiles; x++)
            {
                behaviours.Append(Behaviour(land, x, z).ToString("X2", CultureInfo.InvariantCulture));
                solid.Append(land.Solid(x, z) ? '#' : '.');
                row.Append(TerrainCoverCodes.CodeOf(look[z * LandData.Tiles + x]));
            }
            file.Behaviours.Add(behaviours.ToString());
            file.Solid.Add(solid.ToString());
            file.Cover.Add(row.ToString());
        }
        // A plate standing on its edge (a normal with no height) says nothing of the ground walked on; a few of HeartGold's chunks have one
        file.Heights = land.Heights.Plates.Where(p => p.Normal.Y != 0).Select(WorldWriter.ToPlate).ToList();
        file.Props = land.Props.Select(p =>
        {
            var model = PropModel(landId, p.ModelId);
            return new ChunkProp
            {
                Model = p.ModelId,
                Name = model?.Name ?? "",
                X = Along(p.Position.X), Y = Tiles(p.Position.Y), Z = Along(p.Position.Z),
                BoxX = Along(p.Position.X + (model?.BoxMin.X ?? 0)), BoxZ = Along(p.Position.Z + (model?.BoxMin.Z ?? 0)),
                Width = Tiles(model?.BoxSize.X ?? 0), Depth = Tiles(model?.BoxSize.Z ?? 0), Height = Tiles(model?.BoxSize.Y ?? 0)
            };
        }).ToList();
        return file;
    }

    private static float Round(double value) => (float)Math.Round(value, 4) + 0f;
    private static float Tiles(float units) => Round(units / (double)LandData.TileUnits);
    private static float Along(float units) => Round(units / (double)LandData.TileUnits + LandData.Tiles / 2.0);

    public WorldMatrixFile MatrixFile(Matrix matrix)
    {
        var file = new WorldMatrixFile { Id = matrix.Id, Width = matrix.Width, Height = matrix.Height };
        var keys = matrix.Headers?.Distinct().Select(KeyOf).ToList();
        int chunkWidth = matrix.Land.Select(l => l == Matrix.NoLand ? 1 : l.ToString(CultureInfo.InvariantCulture).Length).DefaultIfEmpty(1).Max();
        int areaWidth = keys == null ? 1 : (keys.Count - 1).ToString(CultureInfo.InvariantCulture).Length;
        for (int y = 0; y < matrix.Height; y++)
        {
            var cells = Enumerable.Range(0, matrix.Width).ToList();
            file.Chunks.Add(WorldMatrixFile.Row(cells.Select(x => matrix.LandAt(x, y) is var l and not Matrix.NoLand ? l.ToString(CultureInfo.InvariantCulture) : "-"), chunkWidth));
            if (keys != null)
                (file.Areas ??= new()).Add(WorldMatrixFile.Row(cells.Select(x => keys.IndexOf(KeyOf(matrix.HeaderAt(x, y)!)).ToString(CultureInfo.InvariantCulture)), areaWidth));
            if (matrix.Altitudes != null)
                (file.Altitudes ??= new()).Add(WorldMatrixFile.Row(cells.Select(x => matrix.AltitudeAt(x, y).ToString(CultureInfo.InvariantCulture)), 2));
        }
        file.AreaKeys = keys;
        return file;
    }

    public static string KeyOf(string headerId)
    {
        string key = headerId.StartsWith("MAP_", StringComparison.Ordinal) ? headerId["MAP_".Length..].ToLowerInvariant() : headerId.ToLowerInvariant();
        return Clashes(key) ? "johto_" + key : key;
    }

    /// <summary>
    /// A key of HeartGold's that another region's world has already (plan 20 · J10, part b): every game has a Victory
    /// Road, and Platinum's keys are <c>victory_road_1f</c> and the rest; FireRed's Route 22 and Hall of Fame are
    /// <c>route_22</c> and <c>pokemon_league_hall_of_fame</c>. Such a key takes the region's prefix, as FireRed's
    /// Victory Road takes <c>kanto_</c>, since a script file and the regions' areas are found by the key alone. The
    /// rest of HeartGold's Kanto shares FireRed's keys too (<c>route_1</c>, <c>route_25</c>): the session that opens
    /// it adds them here: Route 6, between Vermilion and Saffron, since plan 20 · J15, part c; Routes 7 and 8, west to
    /// Celadon and east to Lavender, since part d; Route 5 north to Cerulean, Routes 24 and 25 beyond it and the Sea
    /// Cottage on the cape, since part e; Route 2 between Viridian and Pewter with its house and Viridian Forest, Route 3
    /// east of Pewter and Route 4 west of Cerulean, either side of Mt. Moon, since part f; Route 1 south of Viridian, Cinnabar
    /// Island, Route 20 east of it and the Seafoam Islands' floors, since part g; Route 19 south of Fuchsia and Routes 15,
    /// 14, 13 and 12 between it and Lavender, since part h; Route 11 east of Vermilion and Routes 16 to 18 with Route 16's
    /// house, the Cycling Road between Celadon and Fuchsia, since part i; Routes 9 and 10 east of Cerulean and Rock Tunnel
    /// between Route 10's halves, since part j.
    /// </summary>
    public static bool Clashes(string key) =>
        key.StartsWith("victory_road", StringComparison.Ordinal)
        || key is "route_22" or "pokemon_league_hall_of_fame" or "route_6" or "route_7" or "route_8"
            or "route_5" or "route_24" or "route_25" or "route_25_sea_cottage"
            or "route_4" or "route_3" or "route_2" or "route_2_house" or "viridian_forest"
            or "route_1" or "route_20" or "cinnabar_island" or "seafoam_islands_1f" or "seafoam_islands_b1f"
            or "seafoam_islands_b2f" or "seafoam_islands_b3f" or "seafoam_islands_b4f"
            or "route_19" or "route_15" or "route_14" or "route_13" or "route_12"
            or "route_11" or "route_16" or "route_16_house" or "route_17" or "route_18"
            or "route_9" or "route_10" or "rock_tunnel_1f" or "rock_tunnel_b1f";

    /// <summary>The area's table of wild Pokémon, by its header's bank (<c>R29</c>); null for none.</summary>
    public HgEncounters? EncountersOf(HgHeader header) => header.Encounters == null ? null : Encounters.FirstOrDefault(e => e.Map == header.Encounters);

    /// <summary>A table's slots in one version at one time of day, by the data's names; null for a table with no rate.</summary>
    public List<AreaEncounter>? Slots(HgTable table, string time, string where, bool range)
    {
        if (table.Rate == 0 || table.Slots.Count == 0) return null;
        return table.Slots.Select(s =>
        {
            string constant = s.Species(heartGold: true, time);
            string species = GbaNames.Species(constant) ?? Note($"{where}: {constant} is not in the game's data", constant);
            return new AreaEncounter
            {
                Species = species, Level = s.MinLevel.HeartGold,
                MaxLevel = range ? s.MaxLevel.HeartGold : null
            };
        }).ToList();
    }

    /// <summary>A swarm's species by the data's name, HeartGold's; null where the table names none.</summary>
    private string? SwarmSpecies(Versioned<string>? swarm, string where) =>
        swarm is { HeartGold: { Length: > 0 } constant } && constant != "SPECIES_NONE"
            ? GbaNames.Species(constant) ?? Note($"{where}: {constant} is not in the game's data", constant)
            : null;

    /// <summary>
    /// Where HeartGold's swarms come to (<c>sSwarmMapLUT</c>, <c>unk_02097F6C.c</c>), in its order, and how each is met:
    /// the day's number (<c>Roamers_GetRand</c> modulo 20) picks one. Nine of the twenty are Kanto's.
    /// </summary>
    public static readonly (string Id, string? Way)[] SwarmPlaces =
    {
        ("MAP_ROUTE_1", null), ("MAP_ROUTE_3", null), ("MAP_ROUTE_9", null), ("MAP_ROUTE_12", "fish"), ("MAP_ROUTE_13", null),
        ("MAP_ROUTE_19", "surf"), ("MAP_ROUTE_32", "fish"), ("MAP_ROUTE_25", null), ("MAP_ROUTE_27", "surf"), ("MAP_ROUTE_34", null),
        ("MAP_ROUTE_35", null), ("MAP_ROUTE_38", null), ("MAP_ROUTE_44", "fish"), ("MAP_ROUTE_45", null), ("MAP_ROUTE_47", null),
        ("MAP_MOUNT_MORTAR_1F_ENTRANCE", null), ("MAP_DARK_CAVE_ROUTE_31_SIDE", null), ("MAP_VIRIDIAN_FOREST", null),
        ("MAP_VERMILION", "surf"), ("MAP_VIOLET", "fish")
    };

    /// <summary>Johto's tables that belong to no one area: the places swarms come to, each with its name and the species that swarms there in its way.</summary>
    public WorldEncountersFile SwarmTables()
    {
        var file = new WorldEncountersFile();
        foreach (var (id, way) in SwarmPlaces)
        {
            if (!Maps.Headers.TryGetValue(id, out var header)) { Note($"swarms: {id} is no map", id); continue; }
            var wild = EncountersOf(header);
            string? species = SwarmSpecies(way switch { "surf" => wild?.SurfSwarm, "fish" => wild?.FishSwarm, _ => wild?.LandSwarm }, header.Key);
            if (species == null) Note($"swarms: nothing swarms on {header.Key}", id);
            file.Swarms.Add(new SwarmPlace { Area = header.Key, Name = header.Name, Species = species ?? "", Way = way });
        }
        return file;
    }

    /// <summary>A sound's two species by the data's names, HeartGold's; null where the table has none.</summary>
    private List<string>? Sounds(List<Versioned<string>> slots, string where)
    {
        var names = slots.Where(s => s.HeartGold is { Length: > 0 } && s.HeartGold != "SPECIES_NONE")
            .Select(s => GbaNames.Species(s.HeartGold) ?? Note($"{where}: {s.HeartGold} is not in the game's data", s.HeartGold)).ToList();
        return names.Count == 2 ? names : null;
    }

    private string Note(string problem, string value)
    {
        if (!Problems.Contains(problem)) Problems.Add(problem);
        return value;
    }

    /// <summary>The Safari Zone's own header, whose matrix holds the zone (<c>D47R0102</c>).</summary>
    public const string SafariExterior = "MAP_SAFARI_ZONE_ENTRANCE_EXTERIOR";

    /// <summary>
    /// The Safari Zone's area a header stands for (plan 20 · J8, part b): <c>MAP_SAFARI_ZONE_01</c> to <c>_12</c> are its
    /// twelve areas, in <c>safari_enc.json</c>'s order (the Plains first); <c>_13</c> and <c>_14</c> name none. -1 for
    /// any other header.
    /// </summary>
    public int SafariAreaOf(HgHeader header) =>
        header.Key.StartsWith("safari_zone_", StringComparison.Ordinal) && int.TryParse(header.Key["safari_zone_".Length..], NumberStyles.None, CultureInfo.InvariantCulture, out int n)
        && n >= 1 && n <= Safari.Count ? n - 1 : -1;

    /// <summary>
    /// One of the Safari Zone's areas as the zone holds it (plan 20 · J8, part b): HeartGold's header of each area is a
    /// dummy whose matrix (197 and on) holds its chunk alone, and the zone itself is the exterior's header with the
    /// area's chunk in its hole (<see cref="HgMaps.SafariCells"/>). So the area is written as the exterior is, its name,
    /// music, camera, ground and rates, on the exterior's matrix, with the area's own tables of <c>safari_enc.json</c>
    /// (<c>FieldSystem_GenerateSafariEncounter</c>): ten slots met as often as one another (<c>LCRandom() % 10</c>),
    /// each at its level (the slot's <c>maxLevel</c>), the grass's species by the time of day as the rest of Johto's
    /// (the morning's the slot's own), the water's and the rods' the day's. The slots that objects bring are left out
    /// (the rulings).
    /// </summary>
    private WorldAreaFile SafariArea(HgHeader header, int area)
    {
        var exterior = Maps.Headers[SafariExterior];
        var file = Area(exterior);
        var tables = Safari[area];
        List<AreaEncounter>? Slots(string way, string time)
        {
            if (!tables.Ways.TryGetValue(way, out var slots) || slots.Count == 0) return null;
            return slots.Select(s =>
            {
                string constant = s.Species(heartGold: true, time);
                return new AreaEncounter
                {
                    Species = GbaNames.Species(constant) ?? Note($"{header.Key}: {constant} is not in the game's data", constant),
                    Level = s.MaxLevel.HeartGold,
                    Weight = 1
                };
            }).ToList();
        }
        var land = Slots("land", "morn");
        if (land != null)
        {
            var day = Slots("land", "day")!;
            var night = Slots("land", "nite")!;
            for (int i = 0; i < land.Count; i++)
            {
                if (day[i].Species != land[i].Species) land[i].Day = day[i].Species;
                if (night[i].Species != land[i].Species) land[i].Night = night[i].Species;
            }
        }
        var wild = EncountersOf(exterior);
        file.Key = header.Key;
        file.Index = header.Index;
        file.Matrix = HgMaps.SafariMatrix;
        file.Encounters = null;
        file.Land = land;
        file.LandRate = land == null ? null : wild?.Land.Rate;
        file.Water = Slots("surf", "day");
        file.WaterRate = file.Water == null ? null : wild?.Surf.Rate;
        file.OldRod = Slots("oldrod", "day");
        file.OldRodRate = file.OldRod == null ? null : wild?.OldRod.Rate;
        file.GoodRod = Slots("goodrod", "day");
        file.GoodRodRate = file.GoodRod == null ? null : wild?.GoodRod.Rate;
        file.SuperRod = Slots("superrod", "day");
        file.SuperRodRate = file.SuperRod == null ? null : wild?.SuperRod.Rate;
        file.RockSmash = null;
        file.RockSmashRate = null;
        file.HoennSounds = null;
        file.SinnohSounds = null;
        // The exterior's events are its two ways out at the way in, which stays the exterior's: the areas have none
        file.Warps = new();
        file.Objects = new();
        file.Signs = new();
        file.Triggers = new();
        return file;
    }

    public WorldAreaFile Area(HgHeader header)
    {
        if (SafariAreaOf(header) is var safari and >= 0) return SafariArea(header, safari);
        var events = Maps.Events(header.Events);
        // The exterior's own table is a dummy (Rattata in every slot): the zone's Pokémon are its areas', by place
        var wild = header.Id == SafariExterior ? null : EncountersOf(header);
        // HeartGold gives every slot of the grass a species for the morning, the day and the night (plan 20 · J2): the
        // morning's is the slot's own, and the day's and the night's are kept beside it where they differ from it
        var land = wild == null ? null : Slots(wild.Land, "morn", header.Key, range: false);
        if (land != null)
        {
            var day = Slots(wild!.Land, "day", header.Key, range: false)!;
            var night = Slots(wild.Land, "nite", header.Key, range: false)!;
            for (int i = 0; i < land.Count; i++)
            {
                if (day[i].Species != land[i].Species) land[i].Day = day[i].Species;
                if (night[i].Species != land[i].Species) land[i].Night = night[i].Species;
            }
        }
        return new WorldAreaFile
        {
            Key = header.Key,
            Index = header.Index,
            Name = header.Name,
            Matrix = header.Matrix,
            Kind = header.MapType switch { "CITY_TOWN" => "Town", "INTERIOR" => "Indoors", var t => WorldWriter.Pascal(t) },
            Sign = header.MapType switch { "CITY_TOWN" => "Town", "ROUTE" => "Route", "CAVE" => "Cave", _ => "None" },
            Weather = header.Weather.ToString(CultureInfo.InvariantCulture),
            Camera = header.Camera.ToString(CultureInfo.InvariantCulture),
            BattleBackground = WorldWriter.Pascal(header.BattleBackground),
            DayMusic = header.DayMusic.StartsWith("SEQ_", StringComparison.Ordinal) ? header.DayMusic["SEQ_".Length..] : header.DayMusic,
            NightMusic = header.NightMusic.StartsWith("SEQ_", StringComparison.Ordinal) ? header.NightMusic["SEQ_".Length..] : header.NightMusic,
            Encounters = header.Encounters,
            Bike = header.Bike,
            Running = true,
            EscapeRope = header.EscapeRope,
            Fly = header.Fly,
            Land = land,
            LandRate = land == null ? null : wild!.Land.Rate,
            Water = wild == null ? null : Slots(wild.Surf, "day", header.Key, range: true),
            WaterRate = wild is { Surf.Rate: > 0 } ? wild.Surf.Rate : null,
            OldRod = wild == null ? null : Slots(wild.OldRod, "day", header.Key, range: true),
            OldRodRate = wild is { OldRod.Rate: > 0 } ? wild.OldRod.Rate : null,
            GoodRod = wild == null ? null : Slots(wild.GoodRod, "day", header.Key, range: true),
            GoodRodRate = wild is { GoodRod.Rate: > 0 } ? wild.GoodRod.Rate : null,
            SuperRod = wild == null ? null : Slots(wild.SuperRod, "day", header.Key, range: true),
            SuperRodRate = wild is { SuperRod.Rate: > 0 } ? wild.SuperRod.Rate : null,
            RockSmash = wild == null ? null : Slots(wild.RockSmash, "day", header.Key, range: true),
            RockSmashRate = wild is { RockSmash.Rate: > 0 } ? wild.RockSmash.Rate : null,
            // The swarm's species in each way (plan 20 · J13, part c): the grass takes its one in slots 0 and 1, the water in
            // the first slot and the rods in theirs (EncSlots_Update_LandSwarm, _SurfingSwarm, _FishingSwarm)
            Swarm = land == null || SwarmSpecies(wild?.LandSwarm, header.Key) is not { } landSwarm ? null : new List<string> { landSwarm, landSwarm },
            SurfSwarm = SwarmSpecies(wild?.SurfSwarm, header.Key),
            FishSwarm = SwarmSpecies(wild?.FishSwarm, header.Key),
            // The radio's Hoenn and Sinnoh Sounds (plan 20 · J3), HeartGold's two species of each
            HoennSounds = land == null ? null : Sounds(wild!.Hoenn, header.Key),
            SinnohSounds = land == null ? null : Sounds(wild!.Sinnoh, header.Key),
            Warps = events.Warps.Select(w => new AreaWarp { X = w.X, Z = w.Z, To = KeyOf(w.DestHeaderId), ToWarp = w.DestWarpId }).ToList(),
            Objects = events.Objects.Select(o =>
            {
                string? ball = HgEvents.BallItem(o.Script, header.Short);
                string? item = ball == null ? null : GbaNames.Item(ball == "ITEM_PARLYZ_HEAL" ? "ITEM_PARALYZE_HEAL" : ball) ?? Note($"{header.Key}: the item ball {o.Id} holds {ball}, which the game's data lacks", null!);
                bool trainer = o.Script.StartsWith("TRAINER_", StringComparison.Ordinal);
                return new AreaObject
                {
                    Id = o.Id.StartsWith("obj_", StringComparison.Ordinal) ? o.Id["obj_".Length..].ToLowerInvariant() : o.Id.ToLowerInvariant(),
                    Looks = o.GraphicsId.StartsWith("SPRITE_", StringComparison.Ordinal) ? o.GraphicsId["SPRITE_".Length..].ToLowerInvariant() : o.GraphicsId.ToLowerInvariant(),
                    Movement = o.MovementType,
                    X = o.X, Z = o.Z, Y = o.Y > 0 ? o.Y : null,
                    Facing = o.InitialDir, RangeX = o.MovementRangeX, RangeZ = o.MovementRangeZ,
                    Trainer = trainer ? (o.TrainerType == "" ? "talks" : "sees") : null,
                    Sight = trainer ? (o.TrainerType == "" ? 0 : o.Data[0]) : null,
                    HiddenBy = o.HiddenFlag.Length == 0 ? null : o.HiddenFlag,
                    Item = item,
                    Script = o.Script
                };
            }).ToList(),
            Signs = events.Signs.Select(s =>
            {
                var sign = new AreaSign { X = s.X, Z = s.Z, Type = s.Type, Script = s.Script };
                if (s.Type == AreaSign.HiddenItem && HiddenItems.TryGetValue(s.Script, out var hidden))
                    (sign.Item, sign.Count, sign.Flag) = (hidden.Item, hidden.Count > 1 ? hidden.Count : null, hidden.Flag);
                return sign;
            }).ToList(),
            Triggers = events.Triggers.Select(t => new AreaTrigger
            {
                X = t.X, Z = t.Z, Width = Math.Max(1, t.Width), Depth = Math.Max(1, t.Length), Script = t.Script, Variable = t.Var, Value = t.Value
            }).ToList()
        };
    }

    /// <summary>
    /// Where Johto's wild Pokémon live (<c>habitats.json</c>), for the Pokédex's area page: every area of the
    /// region with a table, each time of day's grass, the water and the rods, in HeartGold's version; and the
    /// overworld's chunks as Platinum's file draws them (<c>T</c> a town, <c>~</c> water, <c>.</c> land). Kanto's
    /// areas, which the overworld holds too, are left out (plan 20: Johto's files hold Johto).
    /// </summary>
    public WorldHabitatsFile Habitats()
    {
        var file = new WorldHabitatsFile();
        var over = Maps.Matrix(0);
        var cells = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        for (int cy = 0; cy < over.Height; cy++)
        {
            var row = new StringBuilder();
            for (int cx = 0; cx < over.Width; cx++)
            {
                int landId = over.LandAt(cx, cy);
                string? id = over.HeaderAt(cx, cy);
                if (landId == Matrix.NoLand || id == null || !Maps.Headers.TryGetValue(id, out var header) || header.Index == 0) { row.Append(' '); continue; }
                var look = CoverOf(landId);
                int water = look.Count(c => c == TerrainCover.Water);
                row.Append(header.MapType == "CITY_TOWN" ? 'T' : water * 2 > look.Length ? '~' : '.');
                if (!cells.TryGetValue(header.Key, out var list)) cells[header.Key] = list = new();
                list.Add($"{cx},{cy}");
            }
            file.Map.Add(row.ToString().TrimEnd());
        }
        static List<string>? Distinct(List<AreaEncounter>? slots) => slots is { Count: > 0 } ? slots.Select(s => s.Species).Distinct().ToList() : null;
        foreach (var header in Maps.Headers.Values.Where(h => !h.Kanto).OrderBy(h => h.Index))
        {
            if (header.Id == SafariExterior || EncountersOf(header) is not { } wild) continue;
            var habitat = new HabitatArea
            {
                Key = header.Key,
                Name = header.Name,
                Cells = cells.TryGetValue(header.Key, out var own) ? string.Join(' ', own) : "",
                Morning = Distinct(Slots(wild.Land, "morn", header.Key, false)),
                Day = Distinct(Slots(wild.Land, "day", header.Key, false)),
                Night = Distinct(Slots(wild.Land, "nite", header.Key, false)),
                Surf = Distinct(Slots(wild.Surf, "day", header.Key, true)),
                OldRod = Distinct(Slots(wild.OldRod, "day", header.Key, true)),
                GoodRod = Distinct(Slots(wild.GoodRod, "day", header.Key, true)),
                SuperRod = Distinct(Slots(wild.SuperRod, "day", header.Key, true))
            };
            if (habitat.Morning == null && habitat.Surf == null && habitat.OldRod == null && habitat.GoodRod == null && habitat.SuperRod == null) continue;
            file.Areas.Add(habitat);
        }
        // The Safari Zone's twelve areas (plan 20 · J8, part b), shown on the gate's chunks
        foreach (int area in HgMaps.SafariAreas)
        {
            var header = Maps.Headers[$"MAP_SAFARI_ZONE_{area + 1:00}"];
            var safari = SafariArea(header, area);
            file.Areas.Add(new HabitatArea
            {
                Key = header.Key,
                Name = safari.Name,
                Cells = cells.TryGetValue("safari_zone_gate", out var gate) ? string.Join(' ', gate) : "",
                Morning = Distinct(safari.Land),
                Day = safari.Land?.Select(s => s.Day ?? s.Species).Distinct().ToList(),
                Night = safari.Land?.Select(s => s.Night ?? s.Species).Distinct().ToList(),
                Surf = Distinct(safari.Water),
                OldRod = Distinct(safari.OldRod),
                GoodRod = Distinct(safari.GoodRod),
                SuperRod = Distinct(safari.SuperRod)
            });
        }
        return file;
    }

    /// <summary>The headers whose ground a matrix holds: on the overworld those its cells name, elsewhere those that load it.</summary>
    public IEnumerable<HgHeader> AreasOn(Matrix matrix) => matrix.Headers != null
        ? matrix.Headers.Distinct().Where(Maps.Headers.ContainsKey).Select(id => Maps.Headers[id]).Where(h => h.Index != 0)
        // The Safari Zone's areas are its matrix's (plan 20 · J8, part b), not their dummy matrices' of one chunk each
        : Maps.Headers.Values.Where(h => h.Matrix == matrix.Id && SafariAreaOf(h) < 0).OrderBy(h => h.Index);
}
