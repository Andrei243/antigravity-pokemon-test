using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// The imported world of one region, read from <c>Data/world/&lt;region&gt;/</c>: the index of what is built
/// (<c>world.json</c>), the matrices, chunks and areas <c>tools/MapImporter</c> wrote, and the overlays written by
/// hand. It makes the game's maps from them (<see cref="BuildMaps"/>). Chunks that have no file yet are forest.
/// </summary>
public sealed class World
{
    public const string Folder = "world";
    public const string IndexFile = "world.json";

    public WorldIndexFile Index { get; }

    private readonly string folder;
    private readonly Dictionary<int, WorldMatrixFile> matrices = new();
    private readonly Dictionary<int, WorldChunkFile?> chunks = new();
    private readonly Dictionary<string, WorldAreaFile?> areas = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, WorldOverlayFile?> overlays = new(StringComparer.OrdinalIgnoreCase);

    private World(string folder, WorldIndexFile index)
    {
        this.folder = folder;
        Index = index;
    }

    /// <summary>Every region that has a world folder, in name order.</summary>
    public static List<World> LoadAll()
    {
        var worlds = new List<World>();
        string root = GameDataFiles.PathOf(Folder);
        if (!Directory.Exists(root)) return worlds;
        foreach (string dir in Directory.GetDirectories(root).OrderBy(d => d, StringComparer.Ordinal))
        {
            string relative = Path.Combine(Folder, Path.GetFileName(dir));
            if (File.Exists(GameDataFiles.PathOf(Path.Combine(relative, IndexFile))))
                worlds.Add(new World(relative, GameDataFiles.Load<WorldIndexFile>(Path.Combine(relative, IndexFile))));
        }
        return worlds;
    }

    private T? Optional<T>(string file) where T : class
    {
        string relative = Path.Combine(folder, file);
        return File.Exists(GameDataFiles.PathOf(relative)) ? GameDataFiles.Load<T>(relative) : null;
    }

    public WorldMatrixFile Matrix(int id)
    {
        if (!matrices.TryGetValue(id, out var matrix))
            matrices[id] = matrix = GameDataFiles.Load<WorldMatrixFile>(Path.Combine(folder, "matrices", $"{id:000}.json"));
        return matrix;
    }

    /// <summary>A chunk, or null while its file isn't part of the game yet.</summary>
    public WorldChunkFile? Chunk(int id)
    {
        if (!chunks.TryGetValue(id, out var chunk))
        {
            chunk = Optional<WorldChunkFile>(Path.Combine("chunks", $"{id:000}.json"));
            chunk?.Validate();
            chunks[id] = chunk;
        }
        return chunk;
    }

    public WorldAreaFile? Area(string key)
    {
        if (!areas.TryGetValue(key, out var area)) areas[key] = area = Optional<WorldAreaFile>(Path.Combine("areas", key + ".json"));
        return area;
    }

    public WorldOverlayFile? Overlay(string key)
    {
        if (!overlays.TryGetValue(key, out var overlay)) overlays[key] = overlay = Optional<WorldOverlayFile>(Path.Combine("overlays", key + ".json"));
        return overlay;
    }

    /// <summary>Whether an area is built: its people stand in it and the player can walk there.</summary>
    public bool IsOpen(string area) => Index.Areas.Contains(area, StringComparer.OrdinalIgnoreCase);

    /// <summary>The map an area lies on, or null if the game makes no map of its matrix.</summary>
    public WorldMapEntry? MapOf(string area) =>
        Area(area) is { } file ? Index.Maps.FirstOrDefault(m => m.Matrix == file.Matrix) : null;

    /// <summary>The chunk under a tile of a matrix, with the tile's place in it; null where there is none.</summary>
    public (WorldChunkFile Chunk, int X, int Z)? ChunkAt(int matrixId, int x, int z)
    {
        var matrix = Matrix(matrixId);
        const int t = WorldChunkFile.Tiles;
        if (x < 0 || z < 0 || x >= matrix.Width * t || z >= matrix.Height * t) return null;
        int id = matrix.ChunkAt(x / t, z / t);
        return id != WorldMatrixFile.NoChunk && Chunk(id) is { } chunk ? (chunk, x % t, z % t) : null;
    }

    public TileBehavior BehaviourAt(int matrixId, int x, int z) =>
        ChunkAt(matrixId, x, z) is { } at ? at.Chunk.BehaviourAt(at.X, at.Z) : TileBehavior.None;

    public List<Map> BuildMaps() => Index.Maps.Select(entry => WorldMapBuilder.Build(this, entry)).ToList();
}

/// <summary>
/// Makes a playable <see cref="Map"/> of a matrix: tiles from each chunk's cover and behaviours, buildings from
/// its props, and for the areas that are open their signposts, people, doors and wild Pokémon. The map's tiles
/// are the matrix's own, so on the overworld a position is a position in the whole region.
/// </summary>
public static class WorldMapBuilder
{
    private const int T = WorldChunkFile.Tiles;

    public static Map Build(World world, WorldMapEntry entry)
    {
        var matrix = world.Matrix(entry.Matrix);
        var map = new Map(matrix.Width * T, matrix.Height * T) { Name = entry.Name, DisplayName = world.Index.Region, Trees = entry.Trees };

        // Until a chunk says otherwise the world is forest nobody can enter
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                map.SetGroundTile(x, y, TileType.Tree, isSolid: true);

        var areas = new Dictionary<string, MapArea>(StringComparer.OrdinalIgnoreCase);
        for (int cy = 0; cy < matrix.Height; cy++)
            for (int cx = 0; cx < matrix.Width; cx++)
            {
                string key = matrix.AreaAt(cx, cy) ?? entry.Area ?? "";
                if (!areas.TryGetValue(key, out var area)) areas[key] = area = AreaOf(world, key);
                map.SetArea(cx, cy, area);

                int id = matrix.ChunkAt(cx, cy);
                if (id != WorldMatrixFile.NoChunk && world.Chunk(id) is { } chunk)
                    PlaceChunk(map, chunk, cx * T, cy * T, area, world.Overlay(key));
            }

        foreach (var (key, area) in areas)
            if (area.Open) PlaceEvents(world, map, key, area);
        return map;
    }

    private static MapArea AreaOf(World world, string key)
    {
        var file = world.Area(key);
        var overlay = world.Overlay(key);
        var area = new MapArea
        {
            Key = key,
            DisplayName = file?.Name ?? "",
            BgmTrack = overlay?.BgmTrack ?? "",
            Open = file != null && world.IsOpen(key),
            Trees = overlay?.Trees,
            Architecture = overlay?.Architecture,
            Arena = overlay?.BattleArena
        };
        if (overlay?.EvolutionSites != null) area.EvolutionSites.AddRange(overlay.EvolutionSites);

        if (file?.Land != null)
        {
            for (int slot = 0; slot < file.Land.Count; slot++)
            {
                var wild = file.Land[slot];
                area.WildEncounters.Add(new WildEncounterEntry
                {
                    SpeciesName = wild.Species,
                    MinLevel = wild.Level,
                    MaxLevel = wild.Level,
                    Weight = slot < WorldAreaFile.LandSlotWeights.Length ? WorldAreaFile.LandSlotWeights[slot] : 1
                });
            }
        }
        return area;
    }

    // ---------------------------------------------------------------- tiles

    /// <summary>
    /// The tile the game draws and walks for a tile of the world: its type, whether it blocks, and what stands on
    /// it. A ledge keeps its jump; water blocks until there is Surf; rock faces and sea rocks are boulders until
    /// cliffs are built (plan 01 · M3).
    /// </summary>
    public static (TileType Type, bool Solid, PropType? Prop) Look(TerrainCover cover, TileBehavior behaviour, bool solid)
    {
        switch (behaviour)
        {
            case TileBehavior.LedgeSouth:
                return (TileType.LedgeDown, false, null);
            case TileBehavior.Door:
                return (TileType.Door, true, null);   // opened when a warp is put on it
            case TileBehavior.River or TileBehavior.Sea or TileBehavior.Waterfall:
                return (TileType.Water, true, cover == TerrainCover.Boulder ? PropType.Boulder : null);
        }

        return cover switch
        {
            TerrainCover.Grass => (TileType.Grass, solid, null),
            TerrainCover.Flowers => (TileType.FlowerGrass, solid, null),
            TerrainCover.TallGrass => (TileType.TallGrass, solid, null),
            TerrainCover.Path or TerrainCover.Paving or TerrainCover.Bridge or TerrainCover.Steps => (TileType.Path, solid, null),
            TerrainCover.Sand => (TileType.Sand, solid, null),
            TerrainCover.Rock or TerrainCover.Marsh => (TileType.Dirt, solid, null),
            TerrainCover.CaveFloor => (TileType.CaveFloor, solid, null),
            TerrainCover.Snow or TerrainCover.Ice => (TileType.Snow, solid, null),
            TerrainCover.Water => (TileType.Water, solid, null),
            TerrainCover.Cliff or TerrainCover.Boulder => (TileType.Dirt, true, PropType.Boulder),
            TerrainCover.Fence => (TileType.Grass, true, PropType.Fence),
            TerrainCover.Building => (TileType.Wall, true, null),
            // Trees, and whatever blocks without saying what it is; open ground nobody named is lawn
            _ => solid ? (TileType.Tree, true, null) : (TileType.Grass, false, null)
        };
    }

    private static void PlaceChunk(Map map, WorldChunkFile chunk, int ox, int oy, MapArea area, WorldOverlayFile? overlay)
    {
        var building = new bool[T * T];
        for (int z = 0; z < T; z++)
            for (int x = 0; x < T; x++)
            {
                var cover = chunk.CoverAt(x, z);
                bool solid = chunk.SolidAt(x, z);
                if (cover == TerrainCover.Building && solid)
                {
                    building[z * T + x] = true;
                    continue;
                }

                var (type, blocks, prop) = Look(cover, chunk.BehaviourAt(x, z), solid);
                // An area that isn't built yet is scenery: seen from its neighbours, entered by nobody
                map.SetGroundTile(ox + x, oy + z, type, blocks || !area.Open);
                if (prop is { } standing) map.Props.Add(new Prop { Type = standing, X = ox + x, Y = oy + z });
            }

        PlaceBuildings(map, chunk, building, ox, oy, overlay?.Roof ?? TileType.RoofRed);
    }

    /// <summary>A prop as large as a building, and not a sheet the size of the whole chunk (a lake's water).</summary>
    public static bool IsBuilding(ChunkProp prop) =>
        prop.Width >= 2.5f && prop.Depth >= 2f && prop.Height >= 1.25f && !(prop.Width > T - 1 && prop.Depth > T - 1);

    /// <summary>What a building is, by the original's short name for its model; null for an ordinary house.</summary>
    public static BuildingKind? KindOf(string propName) => propName switch
    {
        "pc" => BuildingKind.PokemonCenter,
        "fs" => BuildingKind.PokeMart,
        "t2_s01" => BuildingKind.Lab,
        "c1_school" => BuildingKind.School,
        "c1_s01" => BuildingKind.Office,
        "c1_s02" => BuildingKind.TvStation,
        "c1_s03" => BuildingKind.Terminal,
        _ when propName.StartsWith("c1_b", StringComparison.Ordinal) => BuildingKind.Apartments,
        _ => null
    };

    /// <summary>
    /// Each connected block of building tiles becomes roof with a row of wall along its front, which is how the
    /// game's maps describe a building (<see cref="MapStructures.FindBuildings"/>); doors keep their place.
    /// </summary>
    private static void PlaceBuildings(Map map, WorldChunkFile chunk, bool[] building, int ox, int oy, TileType roof)
    {
        var seen = new bool[T * T];
        for (int start = 0; start < building.Length; start++)
        {
            if (!building[start] || seen[start]) continue;

            var tiles = new List<(int X, int Z)>();
            var stack = new Stack<(int X, int Z)>();
            stack.Push((start % T, start / T));
            seen[start] = true;
            int x0 = T, x1 = -1, z0 = T, z1 = -1;
            while (stack.Count > 0)
            {
                var (x, z) = stack.Pop();
                tiles.Add((x, z));
                x0 = Math.Min(x0, x); x1 = Math.Max(x1, x);
                z0 = Math.Min(z0, z); z1 = Math.Max(z1, z);
                foreach (var (nx, nz) in new[] { (x + 1, z), (x - 1, z), (x, z + 1), (x, z - 1) })
                {
                    if (nx < 0 || nz < 0 || nx >= T || nz >= T || !building[nz * T + nx] || seen[nz * T + nx]) continue;
                    seen[nz * T + nx] = true;
                    stack.Push((nx, nz));
                }
            }

            foreach (var (x, z) in tiles)
            {
                var type = chunk.BehaviourAt(x, z) == TileBehavior.Door ? TileType.Door : z == z1 ? TileType.Wall : roof;
                map.SetGroundTile(ox + x, oy + z, type, isSolid: true);
            }

            float midX = (x0 + x1 + 1) / 2f, midZ = (z0 + z1 + 1) / 2f;
            var prop = chunk.Props.FirstOrDefault(p => IsBuilding(p)
                && midX >= p.BoxX && midX <= p.BoxX + p.Width && midZ >= p.BoxZ && midZ <= p.BoxZ + p.Depth);
            if (prop != null && KindOf(prop.Name) is { } kind) map.BuildingKinds[(ox + x0, oy + z1)] = kind;
        }
    }

    // ---------------------------------------------------------------- people, signs, doors

    /// <summary>Which of our characters plays one of the original's people, by the name of their looks.</summary>
    public static string CharacterFor(string looks) => looks switch
    {
        "barry" => "Rival",
        "prof_rowan" => "Rowan",
        "mom" => "Mom",
        "nurse" or "pokecenter_nurse" => "Nurse",
        "var_0" or "lass" or "school_kid_f" or "twin" or "picnicker" or "little_girl" => "Lass",
        "youngster" or "school_kid_m" or "guitarist" or "bug_catcher" or "camper" or "little_boy" => "Youngster",
        "pokemon_breeder_f" or "beauty" or "lady" or "aroma_lady" or "parasol_lady" or "socialite" or "middle_aged_woman" or "old_woman" => "Lady",
        "collector" or "gentleman" or "old_man" or "rich_boy" or "scientist_m" or "middle_aged_man" => "Gentleman",
        "cashier_m" or "cashier_f" or "clerk" or "waiter" => "Clerk",
        "clown" => "Clown",
        "briefcase" => "StarterBriefcase",
        _ => "Trainer"
    };

    private static bool IsSignpost(string looks) => looks is "map_signpost" or "arrow_signpost" or "signboard" or "trainer_tips_signpost";

    private static Direction FacingOf(int facing) => facing switch
    {
        0 => Direction.Up,
        2 => Direction.Left,
        3 => Direction.Right,
        _ => Direction.Down
    };

    private static void PlaceEvents(World world, Map map, string key, MapArea area)
    {
        var file = world.Area(key)!;
        var overlay = world.Overlay(key);

        foreach (var o in file.Objects)
        {
            // A copy of a neighbouring area's object, there so it shows across the border: the neighbour places it
            if (o.HiddenBy != null && o.HiddenBy.StartsWith("MAP_HEADER_", StringComparison.Ordinal)) continue;

            if (IsSignpost(o.Looks) && o.HiddenBy == null)
            {
                map.SetGroundTile(o.X, o.Z, TileType.Signpost, isSolid: true);
                map.Signboards[(o.X, o.Z)] = overlay?.Signs?.GetValueOrDefault(o.Id) ?? area.DisplayName;
                continue;
            }
            if (o.Looks == "mailbox")
            {
                map.AddProp(PropType.Mailbox, o.X, o.Z);
                map.Signboards[(o.X, o.Z)] = overlay?.Signs?.GetValueOrDefault(o.Id) ?? "A mailbox.";
                continue;
            }

            // People appear only once the game says who they are
            if (overlay?.People == null || !overlay.People.TryGetValue(o.Id, out var person)) continue;
            map.NPCs.Add(MapFile.BuildNpc(new MapFile.NpcRecord
            {
                Id = person.Id ?? person.Trainer?.Id,
                Name = person.Name,
                NpcType = person.NpcType ?? CharacterFor(o.Looks),
                X = o.X,
                Y = o.Z,
                Facing = FacingOf(o.Facing),
                Dialog = person.Dialog,
                IsStarterBriefcase = person.IsStarterBriefcase,
                Trainer = person.Trainer
            }, map.Name));
        }

        foreach (var npc in overlay?.Npcs ?? new()) map.NPCs.Add(MapFile.BuildNpc(npc, map.Name));
        foreach (var prop in overlay?.Props ?? new()) map.AddProp(prop.Type, prop.X, prop.Y, prop.Width, prop.Depth);

        for (int i = 0; i < file.Warps.Count; i++)
        {
            if (overlay?.Locked?.Contains(i) == true) continue;
            var from = file.Warps[i];
            Warp? warp = null;
            if (overlay?.Doors?.FirstOrDefault(d => d.Warp == i) is { } door)
            {
                warp = new Warp { TargetMap = door.Map, TargetX = door.X, TargetY = door.Y, TargetFacing = door.Facing };
            }
            else if (world.IsOpen(from.To) && world.MapOf(from.To) is { } onto && world.Area(from.To) is { } target && from.ToWarp < target.Warps.Count)
            {
                var to = target.Warps[from.ToWarp];
                var (x, z, facing) = Arrival(world.BehaviourAt(target.Matrix, to.X, to.Z), to.X, to.Z);
                warp = new Warp { TargetMap = onto.Name, TargetX = x, TargetY = z, TargetFacing = facing };
            }
            if (warp == null) continue;

            warp.SourceX = from.X;
            warp.SourceY = from.Z;
            map.Warps.Add(warp);
            // A door is a blocked tile in the world's data; here stepping onto it is what takes the warp
            if (map.GetGroundTile(from.X, from.Z) == TileType.Door) map.SetSolid(from.X, from.Z, false);
        }

        foreach (var exit in overlay?.Exits ?? new())
            map.Warps.Add(new Warp { SourceX = exit.X, SourceY = exit.Z, TargetMap = exit.Map, TargetX = exit.ToX, TargetY = exit.ToY, TargetFacing = exit.Facing });
    }

    /// <summary>
    /// Where someone coming through a warp is put down: one step out of the warp's tile, the way they would be
    /// walking as they come out, so they don't stand on it and take it again.
    /// </summary>
    public static (int X, int Z, Direction Facing) Arrival(TileBehavior warpTile, int x, int z) => warpTile switch
    {
        // Taken walking north: one comes out of it walking south
        TileBehavior.Door or TileBehavior.EntranceNorth or TileBehavior.ExitNorth => (x, z + 1, Direction.Down),
        TileBehavior.EntranceSouth or TileBehavior.ExitSouth => (x, z - 1, Direction.Up),
        TileBehavior.EntranceEast or TileBehavior.ExitEast or TileBehavior.StairsEast => (x - 1, z, Direction.Left),
        TileBehavior.EntranceWest or TileBehavior.ExitWest or TileBehavior.StairsWest => (x + 1, z, Direction.Right),
        _ => (x, z, Direction.Down)
    };
}
