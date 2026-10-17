using System.Globalization;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Data;

namespace MapImporter.Gba;

/// <summary>
/// A GBA map's events and wild Pokémon in the world files' shape (plan 18 · W7): what <see cref="GbaWorld.AreaFile"/>
/// adds to an area once the game's tables are read. Every position becomes a tile of the map's matrix, as Sinnoh's
/// are; every constant the name this game's data gives it.
/// </summary>
public static class GbaEvents
{
    /// <summary>The looks of an object that is an item lying in its ball.</summary>
    public const string ItemBallLooks = "item_ball";

    /// <summary>
    /// The area's warps, people and things, signs and hidden items, triggers and wild Pokémon, onto
    /// <paramref name="area"/>. A warp to <c>MAP_DYNAMIC</c> (the way back to wherever the player came from: the
    /// lifts) is kept in its place among the warps, since others count them, and leads nowhere yet.
    /// </summary>
    public static void Fill(WorldAreaFile area, GbaPlaced placed, GbaTables tables, Func<string, GbaMap?> byId, List<string> problems)
    {
        var (map, _, ox, oy) = placed;
        foreach (var w in map.Warps)
        {
            var to = byId(w.DestMap);
            if (to == null && w.DestMap != "MAP_DYNAMIC") problems.Add($"{map.Name}: a warp to {w.DestMap}, which has no map");
            area.Warps.Add(new AreaWarp { X = ox + w.X, Z = oy + w.Y, To = to?.Key ?? "", ToWarp = w.DestWarp });
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < map.Objects.Count; i++)
        {
            var o = map.Objects[i];
            string looks = Strip(o.Graphics, "OBJ_EVENT_GFX_");
            string id = o.LocalId.Length > 0 ? Strip(Regex.Replace(o.LocalId, "^LOCALID_", ""), "") : looks;
            string unique = id;
            for (int n = 2; !ids.Add(unique); n++) unique = $"{id}_{n}";
            var (movement, facing) = Movement(o.Movement);
            var obj = new AreaObject
            {
                Id = unique, Looks = looks, Movement = movement, X = ox + o.X, Z = oy + o.Y, Facing = facing,
                RangeX = o.RangeX, RangeZ = o.RangeY,
                HiddenBy = o.Flag is "" or "0" ? null : o.Flag,
                Script = o.Script is "" or "0x0" ? "" : o.Script
            };
            // FireRed's boulders keep a flag in the trainer's field: only a trainer's type makes a trainer
            if (o.TrainerType.StartsWith("TRAINER_TYPE_", StringComparison.Ordinal) && o.TrainerType != "TRAINER_TYPE_NONE")
            {
                obj.Trainer = Strip(o.TrainerType, "TRAINER_TYPE_");
                obj.Sight = int.TryParse(o.SightOrBerryTree, NumberStyles.Integer, CultureInfo.InvariantCulture, out int sight) ? sight : 0;
                // The trainer its script battles as: what a placed trainer is looked up by
                if (tables.TrainerScripts.TryGetValue(o.Script, out var trainer)) obj.Script = trainer;
                else problems.Add($"{map.Name}: the trainer {unique}'s script {o.Script} names no trainer");
            }
            if (looks == ItemBallLooks)
            {
                if (tables.ItemBalls.TryGetValue(o.Script, out var held))
                {
                    obj.Item = GbaNames.Item(held.Item) ?? held.Item;
                    if (held.Count > 1) obj.Count = held.Count;
                    if (GbaNames.Item(held.Item) == null) problems.Add($"{map.Name}: {held.Item} has no item of the data");
                }
                else problems.Add($"{map.Name}: the item ball {unique}'s script {o.Script} holds nothing the item scripts name");
            }
            area.Objects.Add(obj);
        }

        foreach (var b in map.Bgs)
        {
            if (b.Type == "hidden_item")
                area.Signs.Add(new AreaSign
                {
                    X = ox + b.X, Z = oy + b.Y, Type = AreaSign.HiddenItem, Script = "",
                    Item = GbaNames.Item(b.Item) ?? b.Item, Count = b.Quantity > 1 ? b.Quantity : null, Flag = b.Flag
                });
            else if (b.Type == "sign")
                area.Signs.Add(new AreaSign { X = ox + b.X, Z = oy + b.Y, Type = 0, Script = b.Script });
            // A secret base's spot is plan 21's (Hoenn's secret bases): left out
        }

        foreach (var c in map.Coords.Where(c => c.Type == "trigger"))
            area.Triggers.Add(new AreaTrigger
            {
                X = ox + c.X, Z = oy + c.Y, Script = c.Script, Variable = c.Var is "" or "0" ? "" : c.Var, Value = c.VarValue
            });

        if (tables.Wild.TryGetValue(map.Id, out var wild))
        {
            area.Encounters = map.Key;
            (area.Land, area.LandRate) = Table(wild.Land, WorldAreaFile.LandSlotWeights, map.Name, problems);
            (area.Water, area.WaterRate) = Table(wild.Water, WorldAreaFile.WaterSlotWeights, map.Name, problems);
            (area.RockSmash, area.RockSmashRate) = Table(wild.RockSmash, WorldAreaFile.WaterSlotWeights, map.Name, problems);
            (area.OldRod, area.OldRodRate) = Table(wild.OldRod, WorldAreaFile.WaterSlotWeights, map.Name, problems);
            (area.GoodRod, area.GoodRodRate) = Table(wild.GoodRod, WorldAreaFile.RodSlotWeights, map.Name, problems);
            (area.SuperRod, area.SuperRodRate) = Table(wild.SuperRod, WorldAreaFile.RodSlotWeights, map.Name, problems);
        }

        foreach (var c in map.Connections)
        {
            if (c.Direction == "dive") area.Dive = byId(c.Map)?.Key;
            else if (c.Direction == "emerge") area.Emerge = byId(c.Map)?.Key;
        }
    }

    /// <summary>
    /// A table as the area file keeps it: its slots in their order, each with a range where its levels differ, and a
    /// weight where it isn't the one the slot's place gives in Platinum's table of the same kind.
    /// </summary>
    public static (List<AreaEncounter>? Slots, int? Rate) Table(GbaWildTable? table, int[] usual, string mapName, List<string> problems)
    {
        if (table == null || table.Slots.Count == 0) return (null, null);
        var slots = new List<AreaEncounter>();
        for (int i = 0; i < table.Slots.Count; i++)
        {
            var s = table.Slots[i];
            string? species = GbaNames.Species(s.Species);
            if (species == null) problems.Add($"{mapName}: {s.Species} is no species of the data");
            slots.Add(new AreaEncounter
            {
                Species = species ?? s.Species,
                Level = s.MinLevel,
                MaxLevel = s.MaxLevel != s.MinLevel ? s.MaxLevel : null,
                Weight = i < usual.Length && usual[i] == s.Weight ? null : s.Weight
            });
        }
        return (slots, table.Rate);
    }

    /// <summary>
    /// A GBA movement as an area file names Platinum's (<see cref="PokemonPlatinumEngine.Overworld.PersonMovement.Parse"/>):
    /// up, down, left and right become north, south, west and east, a face a look (<c>MOVEMENT_TYPE_FACE_DOWN_AND_UP</c>
    /// is <c>look_south_and_north</c>); and the way they face to begin with, in the area file's numbers (0 north, 1
    /// south, 2 west, 3 east).
    /// </summary>
    public static (string Movement, int Facing) Movement(string type)
    {
        var words = Strip(type, "MOVEMENT_TYPE_").Split('_', StringSplitOptions.RemoveEmptyEntries).Select(w => w switch
        {
            "up" => "north", "down" => "south", "left" => "west", "right" => "east", "face" => "look", _ => w
        }).ToList();
        string movement = string.Join('_', words);
        int facing = words.Skip(1).FirstOrDefault(w => w is "north" or "south" or "west" or "east") switch
        {
            "north" => 0, "west" => 2, "east" => 3, _ => 1
        };
        return (movement, facing);
    }

    private static string Strip(string constant, string prefix) =>
        (constant.StartsWith(prefix, StringComparison.Ordinal) ? constant[prefix.Length..] : constant).ToLowerInvariant();

    // ---------------------------------------------------------------- the region's own files

    /// <summary>
    /// The heal locations as the region's <c>spawns</c> (<see cref="RegionSpawn"/>, W1's shape): each on its tile of its
    /// map's matrix, numbered from 1 in the original's order, with the room it wakes in (the map the matrix of its
    /// <c>respawn_map</c> is made into). Fly's flags are the region plan's.
    /// </summary>
    public static List<RegionSpawn> Spawns(GbaWorld world, GbaTables tables, Func<GbaMap, string> mapNameOf)
    {
        var spawns = new List<RegionSpawn>();
        foreach (var heal in tables.HealLocations)
        {
            var placed = world.Matrices.SelectMany(m => m.Maps).FirstOrDefault(p => p.Map.Id == heal.Map);
            if (placed == null) continue;
            var room = world.Source.ById.GetValueOrDefault(heal.RespawnMap);
            spawns.Add(new RegionSpawn
            {
                Id = spawns.Count + 1, Area = placed.Map.Key, X = placed.X + heal.X, Y = placed.Y + heal.Y,
                Room = room != null ? mapNameOf(room) : ""
            });
        }
        return spawns;
    }

    /// <summary>
    /// The region's <c>habitats.json</c> (as Sinnoh's: <see cref="WorldHabitatsFile"/>): a character for each chunk of
    /// the overworld and each area with wild Pokémon, its land table at every time of day (the GBA keeps no times), its
    /// water and its rods. An area of the overworld is shown on its own chunks, one elsewhere on none yet.
    /// </summary>
    public static WorldHabitatsFile Habitats(GbaWorld world, IReadOnlyDictionary<string, WorldAreaFile> areas)
    {
        var file = new WorldHabitatsFile();
        var over = world.Matrices[0];
        var cells = new Dictionary<int, List<string>>();
        for (int cy = 0; cy < over.ChunksHigh; cy++)
        {
            var row = new System.Text.StringBuilder();
            for (int cx = 0; cx < over.ChunksWide; cx++)
            {
                int owner = GbaWorld.Owner(over, cx, cy);
                if (owner < 0) { row.Append(' '); continue; }
                int water = 0, all = 0;
                for (int z = 0; z < GbaMatrix.T; z++)
                    for (int x = 0; x < GbaMatrix.T; x++)
                    {
                        int i = (cy * GbaMatrix.T + z) * over.Width + cx * GbaMatrix.T + x;
                        if (over.Area[i] < 0) continue;
                        all++;
                        if (over.Cover[i] == TerrainCover.Water) water++;
                    }
                string type = over.Maps[owner].Map.MapType;
                row.Append(type is "MAP_TYPE_TOWN" or "MAP_TYPE_CITY" ? 'T' : water * 2 > all ? '~' : '.');
                if (!cells.TryGetValue(owner, out var list)) cells[owner] = list = new();
                list.Add($"{cx},{cy}");
            }
            file.Map.Add(row.ToString().TrimEnd());
        }
        foreach (var matrix in world.Matrices)
            for (int index = 0; index < matrix.Maps.Count; index++)
            {
                var map = matrix.Maps[index].Map;
                if (!areas.TryGetValue(map.Key, out var area)) continue;
                static List<string>? Species(List<AreaEncounter>? slots) => slots is { Count: > 0 } ? slots.Select(s => s.Species).Distinct().ToList() : null;
                var land = Species(area.Land);
                var habitat = new HabitatArea
                {
                    Key = map.Key, Name = area.Name,
                    Cells = matrix.Id == 0 && cells.TryGetValue(index, out var own) ? string.Join(' ', own) : "",
                    Morning = land, Day = land, Night = land,
                    Surf = Species(area.Water), OldRod = Species(area.OldRod), GoodRod = Species(area.GoodRod), SuperRod = Species(area.SuperRod)
                };
                if (land == null && habitat.Surf == null && habitat.OldRod == null && habitat.GoodRod == null && habitat.SuperRod == null) continue;
                file.Areas.Add(habitat);
            }
        return file;
    }
}
