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

    /// <summary>
    /// A world read from any folder that has the layout of <c>Data/world/&lt;region&gt;/</c>, with the index given
    /// instead of read: how tools look at an import the game doesn't ship (the importer's whole output).
    /// </summary>
    public static World Open(string folder, WorldIndexFile index) => new(Path.GetFullPath(folder), index);

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
/// Makes a playable <see cref="Map"/> of a matrix: tiles from each chunk's cover and behaviours, buildings and
/// what else stands about from its models (<see cref="WorldModels"/>), and for the areas that are open their
/// signposts, people, doors and wild Pokémon. The map's tiles are the matrix's own, so on the overworld a
/// position is a position in the whole region.
/// </summary>
public static class WorldMapBuilder
{
    private const int T = WorldChunkFile.Tiles;

    /// <summary>The height of Sinnoh's lowlands in the original's data, in tiles: its sea lies half a tile lower.</summary>
    public const float GroundLevel = 1f;

    public static Map Build(World world, WorldMapEntry entry)
    {
        var matrix = world.Matrix(entry.Matrix);
        var map = new Map(matrix.Width * T, matrix.Height * T)
        {
            Name = entry.Name, DisplayName = world.Index.Region, Trees = entry.Trees, GroundLevel = GroundLevel
        };

        // Until a chunk says otherwise the world is forest nobody can enter, on Sinnoh's usual ground level
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                map.SetGroundTile(x, y, TileType.Tree, isSolid: true);
                map.SetHeight(x, y, GroundLevel);
            }

        var areas = new Dictionary<string, MapArea>(StringComparer.OrdinalIgnoreCase);
        var models = new List<Placed>();
        // Tiles that a model stands on: they get their look from the model, not from the ground's cover
        var under = new bool[map.Width * map.Height];
        // Open tiles whose ground the import couldn't name: they take the ground round them
        var vague = new List<(int X, int Z)>();
        // How many of each area's trees are broad-leaved and how many are pines, and how much of its ground is snow or green
        var trees = new Dictionary<MapArea, (int Broad, int Pine, int Snow, int Green)>();
        for (int cy = 0; cy < matrix.Height; cy++)
            for (int cx = 0; cx < matrix.Width; cx++)
            {
                string key = matrix.AreaAt(cx, cy) ?? entry.Area ?? "";
                if (!areas.TryGetValue(key, out var area)) areas[key] = area = AreaOf(world, key);
                map.SetArea(cx, cy, area);

                int id = matrix.ChunkAt(cx, cy);
                if (id == WorldMatrixFile.NoChunk || world.Chunk(id) is not { } chunk) continue;
                PlaceChunk(map, chunk, cx * T, cy * T, matrix.AltitudeAt(cx, cy) / 2f, area, under, vague);
                foreach (var prop in chunk.Props)
                    models.Add(new Placed(prop, cx * T + prop.BoxX, cy * T + prop.BoxZ, WorldModels.Of(prop.Name), area));

                var count = trees.GetValueOrDefault(area);
                foreach (string row in chunk.Cover)
                    foreach (char code in row)
                    {
                        if (code == TerrainCoverCodes.CodeOf(TerrainCover.Broadleaf)) count.Broad++;
                        else if (code == TerrainCoverCodes.CodeOf(TerrainCover.Tree)) count.Pine++;
                        else if (code == TerrainCoverCodes.CodeOf(TerrainCover.Snow)) count.Snow++;
                        else if (code == TerrainCoverCodes.CodeOf(TerrainCover.Grass) || code == TerrainCoverCodes.CodeOf(TerrainCover.TallGrass)) count.Green++;
                    }
                trees[area] = count;
            }
        foreach (var (area, count) in trees)
        {
            if (count.Broad > count.Pine) area.Trees ??= TreeStyle.Round;
            // Twinleaf Town has patches of snow and is not snow country; Snowpoint City is
            area.Snowbound = count.Snow > count.Green;
        }

        // The tiles that lead somewhere, in every area whose file the game has: a building's way in
        var entrances = new HashSet<(int X, int Z)>();
        foreach (string key in areas.Keys)
            foreach (var warp in world.Area(key)?.Warps ?? new()) entrances.Add((warp.X, warp.Z));

        var parts = PlaceModels(world, entry.Matrix, map, models, under, entrances);
        foreach (var (x, z) in vague) under[z * map.Width + x] = true;
        foreach (var (x, z) in vague) map.SetGroundTile(x, z, GroundLike(map, x, z, under), map.IsSolid(x, z));

        // A town's fences and walls are built like its houses, where its overlay doesn't say
        foreach (var town in parts.Where(p => p.Of.Model?.Town != null && p.Of.Model.Kind is BuildingKind.House or BuildingKind.Apartments)
                     .GroupBy(p => p.Of.Area))
            town.Key.Architecture ??= town.GroupBy(p => p.Of.Model!.Town!.Value).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).First().Key;

        RocksInWater(map);
        foreach (var (key, area) in areas)
            if (area.Open) PlaceEvents(world, map, key, area);

        // Last, because a door knows where it leads only once the warps are in place
        map.PlacedBuildings = parts.Select(part => ToBuilding(map, part, entrances)).ToList();
        return map;
    }

    /// <summary>
    /// A rock standing in the sea is a blocked tile whose behaviour says nothing of water, so by itself it would
    /// be a boulder on a square of dirt. Wherever such a boulder touches water (or another that does), the tile
    /// is water too: the sea runs round the rock, and the rock keeps its tile blocked.
    /// </summary>
    private static void RocksInWater(Map map)
    {
        var rocks = new HashSet<(int X, int Y)>();
        foreach (var prop in map.Props)
            if (prop.Type == PropType.Boulder && map.GetGroundTile(prop.X, prop.Y) == TileType.Dirt) rocks.Add((prop.X, prop.Y));

        // Standing in the water, not on the bank above it
        bool AtLevel(int ax, int ay, int bx, int by) => MathF.Abs(map.HeightAt(ax, ay) - map.HeightAt(bx, by)) < 0.26f;

        var wet = new Queue<(int X, int Y)>();
        foreach (var (x, y) in rocks)
            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                if (map.InBounds(x + dx, y + dy) && map.GetGroundTile(x + dx, y + dy) == TileType.Water && AtLevel(x, y, x + dx, y + dy))
                {
                    wet.Enqueue((x, y));
                    break;
                }

        while (wet.Count > 0)
        {
            var (x, y) = wet.Dequeue();
            if (!rocks.Remove((x, y))) continue;
            map.SetGroundTile(x, y, TileType.Water, isSolid: true);
            map.SetBehaviour(x, y, TileBehavior.Sea);
            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                if (rocks.Contains((x + dx, y + dy)) && AtLevel(x, y, x + dx, y + dy)) wet.Enqueue((x + dx, y + dy));
        }
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
            Weather = Weathers.Of(file?.Weather),
            Trees = overlay?.Trees,
            Architecture = overlay?.Architecture,
            Arena = overlay?.BattleArena
        };
        if (overlay?.EvolutionSites != null) area.EvolutionSites.AddRange(overlay.EvolutionSites);

        static void Fill(List<WildEncounterEntry> table, List<AreaEncounter>? slots, int[] weights)
        {
            for (int slot = 0; slot < (slots?.Count ?? 0); slot++)
            {
                var wild = slots![slot];
                table.Add(new WildEncounterEntry
                {
                    SpeciesName = wild.Species,
                    MinLevel = wild.Level,
                    MaxLevel = wild.MaxLevel ?? wild.Level,
                    Weight = slot < weights.Length ? weights[slot] : 1
                });
            }
        }
        Fill(area.WildEncounters, file?.Land, WorldAreaFile.LandSlotWeights);
        Fill(area.WaterEncounters, file?.Water, WorldAreaFile.WaterSlotWeights);
        area.LandRate = file?.LandRate ?? 0;
        area.WaterRate = file?.WaterRate ?? 0;
        return area;
    }

    // ---------------------------------------------------------------- tiles

    /// <summary>
    /// What the game draws for a tile of the world: its type, whether it blocks, and what stands on it. What the
    /// tile does stays its behaviour (<see cref="Map.BehaviourAt"/>): a ledge is hopped, open water waits for
    /// Surf without being blocked, and a door is shut until a warp opens it.
    /// </summary>
    public static (TileType Type, bool Solid, PropType? Prop) Look(TerrainCover cover, TileBehavior behaviour, bool solid)
    {
        switch (behaviour)
        {
            case TileBehavior.LedgeSouth:
                return (TileType.LedgeDown, false, null);
            case TileBehavior.LedgeWest:
                return (TileType.LedgeLeft, false, null);
            case TileBehavior.LedgeEast:
                return (TileType.LedgeRight, false, null);
            // The corner piece at the south end of a ledge that faces west or east: the same ridge, and a wall
            case TileBehavior.LedgeCornerSouthWest:
                return (TileType.LedgeLeft, true, null);
            case TileBehavior.LedgeCornerSouthEast:
                return (TileType.LedgeRight, true, null);
            case TileBehavior.Door:
                return (TileType.Door, true, null);   // opened when a warp is put on it
            case TileBehavior.River or TileBehavior.Sea or TileBehavior.Waterfall:
                // A blocked tile of water is a rock standing in it
                return (TileType.Water, solid, solid && cover == TerrainCover.Boulder ? PropType.Boulder : null);
            case TileBehavior.Ice:
                return (TileType.Ice, solid, null);
        }

        return cover switch
        {
            TerrainCover.Grass => (TileType.Grass, solid, null),
            TerrainCover.Flowers => (TileType.FlowerGrass, solid, null),
            TerrainCover.TallGrass => (TileType.TallGrass, solid, null),
            TerrainCover.Path => (TileType.Path, solid, null),
            TerrainCover.Paving => (TileType.Paving, solid, null),
            TerrainCover.Bridge => (TileType.Planks, solid, null),
            TerrainCover.Walkway => (TileType.Walkway, solid, null),
            TerrainCover.Steps => (TileType.Stairs, solid, null),
            TerrainCover.Sand => (TileType.Sand, solid, null),
            TerrainCover.Rock => (TileType.Rock, solid, null),
            TerrainCover.Marsh => (TileType.Marsh, solid, null),
            TerrainCover.CaveFloor => (TileType.CaveFloor, solid, null),
            TerrainCover.Snow => (TileType.Snow, solid, null),
            TerrainCover.Ice => (TileType.Ice, solid, null),
            TerrainCover.Water => (TileType.Water, solid, null),
            // A rock face is bare rock nobody walks on; the drop beside it comes from the heights
            TerrainCover.Cliff => (TileType.Rock, true, null),
            TerrainCover.Boulder => (TileType.Dirt, true, PropType.Boulder),
            TerrainCover.Fence => (TileType.Grass, true, PropType.Fence),
            // A street lamp; the ground at its foot is whatever lies round it (PlaceModels)
            TerrainCover.Lamp => (TileType.Path, true, PropType.LampPost),
            TerrainCover.Building => (TileType.Wall, true, null),
            // Trees of either kind (the area's style says which), and whatever blocks without saying what it is;
            // open ground nobody named is lawn until its neighbours say otherwise
            _ => solid ? (TileType.Tree, true, null) : (TileType.Grass, false, null)
        };
    }

    /// <summary>Behaviours whose plates lie one over the other: the deck of a bridge, and the ground or water under it.</summary>
    private static bool HasDeck(TileBehavior behaviour) => behaviour is TileBehavior.Bridge or TileBehavior.BridgeEnd
        or TileBehavior.BridgeOverCave or TileBehavior.BridgeOverWater or TileBehavior.BridgeOverSnow
        || FieldMovement.BikePlankRunsNorthSouth(behaviour) != null;

    /// <param name="altitude">How high the matrix sets the whole chunk, in tiles.</param>
    /// <param name="under">Marks the tiles whose look is settled later, by the model that stands on them.</param>
    /// <param name="vague">Gathers the open tiles of ground nobody named.</param>
    private static void PlaceChunk(Map map, WorldChunkFile chunk, int ox, int oy, float altitude, MapArea area, bool[] under, List<(int X, int Z)> vague)
    {
        for (int z = 0; z < T; z++)
            for (int x = 0; x < T; x++)
            {
                var cover = chunk.CoverAt(x, z);
                var behaviour = chunk.BehaviourAt(x, z);
                bool solid = chunk.SolidAt(x, z);
                map.SetBehaviour(ox + x, oy + z, behaviour);
                PlaceHeight(map, chunk, x, z, ox, oy, altitude, behaviour);

                var (type, blocks, prop) = Look(cover, behaviour, solid);
                // An area that isn't built yet is scenery: seen from its neighbours, entered by nobody
                map.SetGroundTile(ox + x, oy + z, type, blocks || !area.Open);
                if (cover is TerrainCover.Building or TerrainCover.Lamp && solid) under[(oy + z) * map.Width + ox + x] = true;
                if (cover == TerrainCover.Unknown && !solid && type == TileType.Grass) vague.Add((ox + x, oy + z));
                if (prop is { } standing && (blocks || !area.Open)) map.Props.Add(new Prop { Type = standing, X = ox + x, Y = oy + z });
            }
    }

    /// <summary>
    /// A tile's ground from the chunk's plates: the plate under its middle gives its height and slope. Where two
    /// lie one over the other at a bridge, the lower is the ground and the upper the deck.
    /// </summary>
    private static void PlaceHeight(Map map, WorldChunkFile chunk, int x, int z, int ox, int oy, float altitude, TileBehavior behaviour)
    {
        float mx = x + 0.5f, mz = z + 0.5f;
        HeightPlate? ground = null, upper = null;
        foreach (var plate in chunk.Heights)
        {
            if (!plate.Contains(mx, mz)) continue;
            float h = plate.HeightAt(mx, mz);
            if (ground == null || h < ground.HeightAt(mx, mz)) ground = plate;
            if (upper == null || h > upper.HeightAt(mx, mz)) upper = plate;
        }
        if (ground == null) return;

        // Where plates overlap without a bridge the walker's own is the upper one (a terrace over a hidden floor)
        bool deck = HasDeck(behaviour) && upper != ground && upper!.HeightAt(mx, mz) - ground.HeightAt(mx, mz) >= 0.75f;
        var stand = deck ? ground : upper!;
        map.SetHeight(ox + x, oy + z, altitude + stand.HeightAt(mx, mz), stand.SlopeX, stand.SlopeZ);
        if (deck) map.SetDeck(ox + x, oy + z, altitude + upper!.HeightAt(mx, mz));
    }

    // ---------------------------------------------------------------- models: buildings and what stands about

    /// <summary>
    /// A prop as large as a building, and not a sheet the size of the whole chunk (a lake's water): what a model
    /// the catalogue doesn't know is taken for.
    /// </summary>
    public static bool IsBuilding(ChunkProp prop) =>
        prop.Width >= 2.5f && prop.Depth >= 2f && prop.Height >= 1.25f && !(prop.Width > T - 1 && prop.Depth > T - 1);

    /// <summary>One of the original's models where it stands on the map: its box starts at (X, Z), in tiles.</summary>
    private sealed record Placed(ChunkProp Prop, float X, float Z, WorldModel? Model, MapArea Area)
    {
        public float X1 => X + Prop.Width;
        public float Z1 => Z + Prop.Depth;

        /// <summary>Whether a tile's middle lies in the box.</summary>
        public bool Covers(int tx, int tz) => tx + 0.5f > X && tx + 0.5f < X1 && tz + 0.5f > Z && tz + 0.5f < Z1;

        public bool IsBuilding => Model != null ? Model.Role == ModelRole.Building : WorldMapBuilder.IsBuilding(Prop);
        public bool Stands => IsBuilding || Model?.Role == ModelRole.Scenery;
    }

    /// <summary>One block of a building: a rectangle of the tiles its model blocks, and for the main block its porch.</summary>
    private sealed record Part(Placed Of, int X0, int Z0, int X1, int Z1, bool Annex)
    {
        /// <summary>The blocked tiles (their x) of an entrance on the row before the front wall.</summary>
        public List<int> Porch { get; } = new();
    }

    /// <summary>Towns whose yards are fenced in wood; elsewhere what a model leaves standing about is a low wall.</summary>
    private static bool FencesInWood(Architecture? town) =>
        town is Architecture.Timber or Architecture.Plaster or Architecture.Clapboard or Architecture.Cottage or Architecture.Farm
            or Architecture.Marsh or Architecture.Resort or Architecture.HalfTimber;

    /// <summary>
    /// Cuts a set of tiles into rectangles, the largest first: a house is one, a museum with a wing two. Ties go
    /// to the one further north, then west, then the wider, so the same tiles always give the same blocks.
    /// </summary>
    public static List<(int X0, int Z0, int X1, int Z1)> Rectangles(IEnumerable<(int X, int Z)> tiles)
    {
        var left = new HashSet<(int X, int Z)>(tiles);
        var result = new List<(int X0, int Z0, int X1, int Z1)>();
        while (left.Count > 0)
        {
            (int X0, int Z0, int X1, int Z1) best = default;
            int bestArea = 0;
            foreach (var (x0, z0) in left)
            {
                int reach = int.MaxValue;
                for (int z = z0; left.Contains((x0, z)); z++)
                {
                    int x = x0;
                    while (x < reach && left.Contains((x + 1, z))) x++;
                    reach = x;
                    int area = (reach - x0 + 1) * (z - z0 + 1);
                    bool better = area > bestArea
                        || area == bestArea && (z0 < best.Z0 || z0 == best.Z0 && (x0 < best.X0 || x0 == best.X0 && reach > best.X1));
                    if (!better) continue;
                    best = (x0, z0, reach, z);
                    bestArea = area;
                }
            }
            result.Add(best);
            for (int z = best.Z0; z <= best.Z1; z++)
                for (int x = best.X0; x <= best.X1; x++)
                    left.Remove((x, z));
        }
        return result;
    }

    /// <summary>
    /// The ground a tile under a model takes: that of the nearest tile round it which no model stands on, so a
    /// lamp in a paved street stands on paving and a ship in the harbour on water.
    /// </summary>
    private static TileType GroundLike(Map map, int x, int z, bool[] under)
    {
        for (int reach = 1; reach <= 4; reach++)
            foreach (var (dx, dz) in new[] { (0, reach), (-reach, 0), (reach, 0), (0, -reach), (-reach, reach), (reach, reach), (-reach, -reach), (reach, -reach) })
            {
                int nx = x + dx, nz = z + dz;
                if (!map.InBounds(nx, nz) || under[nz * map.Width + nx]) continue;
                switch (map.GetGroundTile(nx, nz))
                {
                    case TileType.Path or TileType.Paving or TileType.Sand or TileType.Snow or TileType.Dirt or TileType.Rock or TileType.Water
                        or TileType.Planks or TileType.Ice or TileType.Marsh or TileType.CaveFloor:
                        return map.GetGroundTile(nx, nz);
                    case TileType.Grass or TileType.FlowerGrass or TileType.TallGrass or TileType.Tree:
                        return TileType.Grass;
                }
            }
        return TileType.Grass;
    }

    /// <summary>
    /// Gives every model its place on the map. A building's model takes the tiles the world blocks under its
    /// box, cut into rectangles: each rectangle two tiles or more each way is a block of the building (roof,
    /// with a row of wall along its front, and its doors). Thin pieces on the row before the main block's
    /// front, beside a way in, are its porch; any others are a fence or a low wall. Something that only stands
    /// there becomes a prop over its box. Returns the buildings' blocks.
    /// </summary>
    private static List<Part> PlaceModels(World world, int matrixId, Map map, List<Placed> models, bool[] under, HashSet<(int X, int Z)> entrances)
    {
        bool Blocked(int x, int z) => world.ChunkAt(matrixId, x, z) is { } at && at.Chunk.SolidAt(at.X, at.Z);

        // Every blocked tile under a model's box is that model's; where boxes overlap (a Pokémon Center inside
        // the box of a museum's forecourt) it is the smaller model's
        var owner = new Dictionary<(int X, int Z), Placed>();
        foreach (var model in models.Where(m => m.Stands).OrderBy(m => m.Prop.Width * m.Prop.Depth))
            for (int z = (int)MathF.Floor(model.Z); z <= (int)MathF.Floor(model.Z1); z++)
                for (int x = (int)MathF.Floor(model.X); x <= (int)MathF.Floor(model.X1); x++)
                    if (map.InBounds(x, z) && model.Covers(x, z) && Blocked(x, z) && !owner.ContainsKey((x, z))) owner[(x, z)] = model;

        foreach (var (x, z) in owner.Keys) under[z * map.Width + x] = true;
        // What the ground's cover guessed at such a tile (a boulder, a fence) gives way to the model
        map.Props.RemoveAll(prop => prop.Type is PropType.Boulder or PropType.Fence && owner.ContainsKey((prop.X, prop.Y)));

        bool WayIn(int x, int z) => map.BehaviourAt(x, z) == TileBehavior.Door || entrances.Contains((x, z));

        var parts = new List<Part>();
        var inPart = new HashSet<(int X, int Z)>();
        var fenced = new List<(int X, int Z, bool Wood)>();
        foreach (var group in owner.GroupBy(kv => kv.Value, ReferenceEqualityComparer.Instance).Select(g => ((Placed)g.Key!, g.Select(kv => kv.Key).ToList())))
        {
            var (model, tiles) = group;
            if (!model.IsBuilding) continue;

            var blocks = new List<(int X0, int Z0, int X1, int Z1)>();
            var thin = new HashSet<(int X, int Z)>();
            foreach (var r in Rectangles(tiles))
            {
                if (r.X1 > r.X0 && r.Z1 > r.Z0) { blocks.Add(r); continue; }
                for (int z = r.Z0; z <= r.Z1; z++)
                    for (int x = r.X0; x <= r.X1; x++)
                        thin.Add((x, z));
            }
            if (blocks.Count == 0)
            {
                // Nothing two tiles each way (a house whose middle the world leaves open): the whole of it is one block
                var (bx0, bz0, bx1, bz1) = (tiles.Min(t => t.X), tiles.Min(t => t.Z), tiles.Max(t => t.X), tiles.Max(t => t.Z));
                if (bx1 == bx0 || bz1 == bz0) continue;
                blocks.Add((bx0, bz0, bx1, bz1));
                thin.Clear();
            }

            // The main block is the one with the way in (in its front row, or on the row before it), else the largest
            bool Entered((int X0, int Z0, int X1, int Z1) r) =>
                Enumerable.Range(r.X0, r.X1 - r.X0 + 1).Any(x => WayIn(x, r.Z1) || WayIn(x, r.Z1 + 1) && !blocks.Any(o => o != r && x >= o.X0 && x <= o.X1 && r.Z1 + 1 >= o.Z0 && r.Z1 + 1 <= o.Z1));
            var main = blocks.Any(Entered) ? blocks.First(Entered) : blocks[0];
            foreach (var r in blocks)
            {
                var part = new Part(model, r.X0, r.Z0, r.X1, r.Z1, Annex: r != main);
                parts.Add(part);
                for (int z = r.Z0; z <= r.Z1; z++)
                    for (int x = r.X0; x <= r.X1; x++)
                    {
                        if (!map.InBounds(x, z)) continue;
                        inPart.Add((x, z));
                        var type = map.BehaviourAt(x, z) == TileBehavior.Door ? TileType.Door : z == r.Z1 ? TileType.Wall : TileType.RoofRed;
                        map.SetGroundTile(x, z, type, isSolid: true);
                    }
                if (r != main) continue;

                // Its porch: on the row before the front, the thin pieces that touch a way in, or hold one
                int row = r.Z1 + 1;
                bool OfPorch(int x) => x >= r.X0 && x <= r.X1 && (thin.Contains((x, row)) || WayIn(x, row));
                for (int x = r.X0; x <= r.X1; x++)
                {
                    if (!WayIn(x, row)) continue;
                    for (int dir = -1; dir <= 1; dir += 2)
                        for (int px = x; OfPorch(px); px += dir)
                            if (thin.Remove((px, row)))
                            {
                                part.Porch.Add(px);
                                inPart.Add((px, row));
                                map.SetGroundTile(px, row, map.BehaviourAt(px, row) == TileBehavior.Door ? TileType.Door : TileType.Wall, isSolid: true);
                            }
                }
                part.Porch.Sort();
            }
            bool wood = FencesInWood(model.Model?.Town);
            foreach (var (x, z) in thin) fenced.Add((x, z, wood));
        }

        // Everything else a model stands on shows the ground round it; a ship lies in the water
        for (int z = 0; z < map.Height; z++)
            for (int x = 0; x < map.Width; x++)
            {
                if (!under[z * map.Width + x] || inPart.Contains((x, z)) || map.GetGroundTile(x, z) == TileType.Door) continue;
                bool afloat = owner.TryGetValue((x, z), out var on) && on.Model is { Role: ModelRole.Scenery, Prop: PropType.Boat };
                map.SetGroundTile(x, z, afloat ? TileType.Water : GroundLike(map, x, z, under), isSolid: true);
            }
        foreach (var (x, z, wood) in fenced) map.Props.Add(new Prop { Type = wood ? PropType.Fence : PropType.LowWall, X = x, Y = z });

        foreach (var model in models)
        {
            if (model.Model is not { Role: ModelRole.Scenery } scenery) continue;
            // The tiles whose middle lies in the box; a thing smaller than a tile stands on the one under its middle
            int x0 = (int)MathF.Floor(model.X - 0.5f) + 1, x1 = (int)MathF.Ceiling(model.X1 - 0.5f) - 1;
            int z0 = (int)MathF.Floor(model.Z - 0.5f) + 1, z1 = (int)MathF.Ceiling(model.Z1 - 0.5f) - 1;
            if (x1 < x0) x0 = x1 = (int)MathF.Floor((model.X + model.X1) / 2f);
            if (z1 < z0) z0 = z1 = (int)MathF.Floor((model.Z + model.Z1) / 2f);
            if (!map.InBounds(x0, z0)) continue;
            map.Props.Add(new Prop
            {
                Type = scenery.Prop, X = x0, Y = z0, Width = x1 - x0 + 1, Depth = z1 - z0 + 1,
                Height = model.Prop.Height, Model = model.Prop.Name
            });
        }
        return parts;
    }

    /// <summary>The building a block of a model is: what kind, how it is built, and where its doors are.</summary>
    private static BuildingInfo ToBuilding(Map map, Part part, HashSet<(int X, int Z)> entrances)
    {
        var model = part.Of.Model;
        var kind = model?.Kind ?? BuildingKind.House;
        var info = new BuildingInfo
        {
            X0 = part.X0, Y0 = part.Z0, X1 = part.X1, Y1 = part.Z1,
            Kind = kind, RoofTile = TileType.RoofRed,
            Model = part.Of.Prop.Name,
            Town = model?.Town,
            Storeys = part.Annex ? 1 : model?.Storeys ?? 0,
            Sign = part.Annex ? null : model?.Sign,
            Theme = kind == BuildingKind.Gym ? WorldModels.GymTheme(part.Of.Area.Key) : null,
            Annex = part.Annex,
            Height = part.Annex ? 0f : part.Of.Prop.Height
        };
        if (part.Annex) return info;

        info.Porch.AddRange(part.Porch);
        int row = part.Z1 + 1;
        for (int x = part.X0; x <= part.X1; x++)
        {
            // In the wall itself; or on the row before it: in the porch's front, or open ground between its sides
            if (map.GetGroundTile(x, part.Z1) == TileType.Door) info.Doors.Add((x, map.GetWarpAt(x, part.Z1)?.TargetMap));
            else if (map.InBounds(x, row) && (map.GetGroundTile(x, row) == TileType.Door && part.Porch.Contains(x)
                         || entrances.Contains((x, row)) && !MapStructures.IsBuildingTile(map, x, row)))
            {
                info.Doors.Add((x, map.GetWarpAt(x, row)?.TargetMap));
                if (!part.Porch.Contains(x)) info.PorchIsOpen = true;
            }
        }

        // A gate house on a road that runs east and west is entered through its ends
        for (int z = part.Z0; z <= part.Z1; z++)
            if (entrances.Contains((part.X0 - 1, z)) || entrances.Contains((part.X1 + 1, z)) || map.BehaviourAt(part.X0, z) == TileBehavior.Door || map.BehaviourAt(part.X1, z) == TileBehavior.Door)
                info.SideDoors.Add(z);
        return info;
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

    /// <summary>The obstacle an object of the original is, by the name of its looks: what Cut, Rock Smash and Strength clear.</summary>
    public static PropType? ObstacleFor(string looks) => looks switch
    {
        "cut_tree" => PropType.CutTree,
        "rock_smash" => PropType.CrackedRock,
        "strength_boulder" => PropType.StrengthBoulder,
        _ => null
    };

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
            if (ObstacleFor(o.Looks) is { } obstacle)
            {
                map.AddProp(obstacle, o.X, o.Z);
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
