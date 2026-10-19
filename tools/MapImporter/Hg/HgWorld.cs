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

    public static string KeyOf(string headerId) => headerId.StartsWith("MAP_", StringComparison.Ordinal) ? headerId["MAP_".Length..].ToLowerInvariant() : headerId.ToLowerInvariant();

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

    public WorldAreaFile Area(HgHeader header)
    {
        var events = Maps.Events(header.Events);
        var wild = EncountersOf(header);
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
            if (EncountersOf(header) is not { } wild) continue;
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
        return file;
    }

    /// <summary>The headers whose ground a matrix holds: on the overworld those its cells name, elsewhere those that load it.</summary>
    public IEnumerable<HgHeader> AreasOn(Matrix matrix) => matrix.Headers != null
        ? matrix.Headers.Distinct().Where(Maps.Headers.ContainsKey).Select(id => Maps.Headers[id]).Where(h => h.Index != 0)
        : Maps.Headers.Values.Where(h => h.Matrix == matrix.Id).OrderBy(h => h.Index);
}
