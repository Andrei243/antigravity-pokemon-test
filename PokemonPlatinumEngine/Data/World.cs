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

    /// <summary>
    /// Platinum's weather calendar, each place's weather for each day of a leap year (<see cref="WorldCalendarFile"/>);
    /// empty when the folder has none.
    /// </summary>
    public IReadOnlyDictionary<string, FieldWeather[]> Calendar => calendar ??= LoadCalendar();
    private Dictionary<string, FieldWeather[]>? calendar;

    private Dictionary<string, FieldWeather[]> LoadCalendar()
    {
        var days = new Dictionary<string, FieldWeather[]>(StringComparer.OrdinalIgnoreCase);
        if (Optional<WorldCalendarFile>(WorldCalendarFile.FileName) is not { } file) return days;
        for (int p = 0; p < file.Places.Count; p++)
            days[file.Places[p]] = file.Days.Select(day => Weathers.Of(day[p])).ToArray();
        return days;
    }

    /// <summary>
    /// The region's tables of wild Pokémon that belong to no one area (plan 06 · R13, <see cref="WorldEncountersFile"/>):
    /// the honey trees', the Great Marsh's and the Trophy Garden's dailies and Feebas's tiles. Empty when the folder has none.
    /// </summary>
    public WorldEncountersFile Encounters => encounters ??= Optional<WorldEncountersFile>(WorldEncountersFile.FileName) ?? new();
    private WorldEncountersFile? encounters;

    public WorldOverlayFile? Overlay(string key)
    {
        if (!overlays.TryGetValue(key, out var overlay)) overlays[key] = overlay = Optional<WorldOverlayFile>(Path.Combine("overlays", key + ".json"));
        return overlay;
    }

    /// <summary>Whether an area is built: its people stand in it and the player can walk there.</summary>
    public bool IsOpen(string area) => Index.Areas.Contains(area, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The map an area lies on, or null if the game makes no map of its matrix. A matrix the original uses for
    /// several places (the Solaceon Ruins' rooms) is a map for each, told apart by the area it is made for.
    /// </summary>
    public WorldMapEntry? MapOf(string area) =>
        Area(area) is { } file
            ? Index.Maps.FirstOrDefault(m => m.Matrix == file.Matrix && string.Equals(m.Area, area, StringComparison.OrdinalIgnoreCase))
              ?? Index.Maps.FirstOrDefault(m => m.Matrix == file.Matrix && m.Area == null)
            : null;

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

    public List<Map> BuildMaps()
    {
        var maps = Index.Maps.Select(entry => WorldMapBuilder.Build(this, entry)).ToList();
        // What the story reveals later is built like the rest, and the map keeps a copy of it to put back
        foreach (var hidden in Index.Hidden)
            maps.FirstOrDefault(m => m.Name == hidden.Map)?.AddHiddenPlace(hidden.Var, hidden.Value, hidden.ChunkX, hidden.ChunkY, hidden.ChunksWide, hidden.ChunksHigh);
        // Turnback Cave's doors are aimed as the player comes into each room: they learn here where each door leads
        foreach (string area in TurnbackCave.Everywhere)
            for (int door = 0; door < TurnbackCave.Doors.Count; door++)
                if (WorldMapBuilder.WayInto(this, area, door) is { } way) TurnbackCave.Learn(area, door, way);
        return maps;
    }
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
        bool cave = entry.Setting == MapSetting.Cave, nothing = entry.Setting == MapSetting.Void;
        var map = new Map(matrix.Width * T, matrix.Height * T)
        {
            Name = entry.Name, DisplayName = world.Index.Region, Trees = entry.Trees, GroundLevel = GroundLevel, Setting = entry.Setting
        };
        // A place that is a matrix of its own is looked at with its own camera, and may be dark
        if (entry.Area != null && world.Area(entry.Area) is { } own)
        {
            map.Camera = CameraOf(own.Camera);
            map.IsDark = own.Weather == "DarkFlash";
        }

        // Until a chunk says otherwise the world is forest nobody can enter, on Sinnoh's usual ground level; a
        // cave is rock, and the Distortion World nothing
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                map.SetGroundTile(x, y, cave ? TileType.CaveWall : nothing ? TileType.Void : TileType.Tree, isSolid: true);
                map.SetHeight(x, y, GroundLevel);
            }

        var areas = new Dictionary<string, MapArea>(StringComparer.OrdinalIgnoreCase);
        var models = new List<Placed>();
        // Tiles that a model stands on: they get their look from the model, not from the ground's cover
        var under = new bool[map.Width * map.Height];
        // Open tiles whose ground the import couldn't name: they take the ground round them
        var vague = new List<(int X, int Z)>();
        // Blocked tiles the original draws nothing on: forest among trees, rock anywhere else
        var blank = new List<(int X, int Z)>();
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
                PlaceChunk(map, chunk, cx * T, cy * T, matrix.AltitudeAt(cx, cy) / 2f, area, under, vague, blank);
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

        // A way in under a bridge (Wayward Cave's, under the Cycling Road): the original gives its tile the deck's
        // height, and it is walked into from the ground under the deck. It is the ground's, with the deck over it
        foreach (var (x, z) in entrances)
        {
            if (!map.InBounds(x, z) || map.DeckAt(x, z) != null) continue;
            var (ax, az, _) = Arrival(map.BehaviourAt(x, z), x, z);
            if (!map.InBounds(ax, az) || map.DeckAt(ax, az) is not { } over) continue;
            float here = map.HeightAt(x, z), below = map.HeightAt(ax, az);
            if (MathF.Abs(here - over) < 0.5f && here - below >= FieldMovement.StepLimit)
            {
                map.SetDeck(x, z, here);
                map.SetHeight(x, z, below);
            }
        }

        // What a tile of unnamed ground will be isn't known yet, so nothing takes its own ground from one
        var unnamed = new bool[map.Width * map.Height];
        foreach (var (x, z) in vague) unnamed[z * map.Width + x] = true;

        var parts = PlaceModels(world, entry.Matrix, map, models, under, unnamed, entrances);
        if (cave)
        {
            CaveRock(map, vague, entrances);
        }
        else if (nothing)
        {
            VoidIslands(map);
        }
        else
        {
            // The original paints its dark of a cave's mouth in a few places that lead into nothing (the west end of
            // Route 205's bridge): open ground there with no way in on it or beside it is the ground round it
            foreach (var (x, z) in OpenMouthsLeadingNowhere(map, entrances)) vague.Add((x, z));
            foreach (var (x, z) in vague) under[z * map.Width + x] = true;
            foreach (var (x, z) in vague) map.SetGroundTile(x, z, GroundLike(map, x, z, under), map.IsSolid(x, z));
            BareRock(map, blank);
            StreetLamps(map, under);
            // The mouth of a cave is no rock: it stays at the level of the ground, a dark hollow under the rock round it
            RaiseRock(map, (x, z) => map.GetGroundTile(x, z) == TileType.Rock && map.IsSolid(x, z));
        }

        // A town's fences and walls are built like its houses, where its overlay doesn't say
        foreach (var town in parts.Where(p => p.Of.Model?.Town != null && p.Of.Model.Kind is BuildingKind.House or BuildingKind.Apartments)
                     .GroupBy(p => p.Of.Area))
            town.Key.Architecture ??= town.GroupBy(p => p.Of.Model!.Town!.Value).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).First().Key;

        RocksInWater(map);
        foreach (var (key, area) in areas)
            if (area.Open) PlaceEvents(world, map, key, area);
        // In the Distortion World a way somewhere is a slab set into the island
        if (nothing)
            foreach (var warp in map.Warps)
                if (map.InBounds(warp.SourceX, warp.SourceY) && map.GetGroundTile(warp.SourceX, warp.SourceY) == TileType.DistortionGround)
                    map.SetGroundTile(warp.SourceX, warp.SourceY, TileType.DistortionSlab, map.IsSolid(warp.SourceX, warp.SourceY));

        // Last, because a door knows where it leads only once the warps are in place
        map.PlacedBuildings = parts.Select(part => ToBuilding(map, part, entrances)).ToList();
        return map;
    }

    /// <summary>The field's camera for one of the original's camera names; anything this game has no own camera for is the default.</summary>
    public static FieldCamera CameraOf(string? name) => name switch
    {
        "Cave" => FieldCamera.Cave,
        "ZoomedIn" => FieldCamera.ZoomedIn,
        // The north face's camera is the overworld's to within a hundredth of a degree (CAMERA_TYPE_MT_CORONET_EXT_NORTH)
        "MtCoronetExtSouth" => FieldCamera.CoronetSouth,
        _ => FieldCamera.Default
    };

    /// <summary>How far a cave's rock stands above the floor before it (seen from the south), in tiles.</summary>
    public const float CaveWallRise = 2f;

    /// <summary>
    /// How far the rock stands up right in front of a floor (the row south of it): low, so that it hides no more
    /// than the feet of whoever walks there (at the cave camera's 63° a rise of this much covers under four
    /// tenths of a tile behind it). It is the least that is still a step with a face: anything lower the field
    /// would join to the floor with a slope (<c>Relief.WeldLimit</c>), and the floor would rise to meet the rock.
    /// </summary>
    public const float CaveLipRise = 0.75f;

    /// <summary>
    /// Finishes a cave's rock (plan 01 · M5; style guide, "Caves"). Open ground the import couldn't name is floor
    /// where it can be reached from one of the cave's ways in, and whatever can't be reached is rock: the space
    /// beyond the walls, which the original leaves open and empty. Then every tile of rock is raised above the
    /// floor nearest to it: <see cref="CaveWallRise"/>, or only <see cref="CaveLipRise"/> where floor lies
    /// right behind it, so a wall is tall where its face shows and low where it would hide the way.
    /// </summary>
    private static void CaveRock(Map map, List<(int X, int Z)> vague, HashSet<(int X, int Z)> entrances)
    {
        int w = map.Width, h = map.Height;
        bool IsRock(int x, int z) => map.GetGroundTile(x, z) == TileType.CaveWall;

        // What can be reached from the cave's ways in, over anything that isn't blocked (water is ridden, a ledge
        // hopped, a cracked rock smashed: none of them is a blocked tile), and over a Bicycle's ramp, which is: it is
        // jumped the way it faces, to one tile past it in low gear and three in top (Victory Road's second floor)
        var reached = new bool[w * h];
        var open = new Queue<(int X, int Z)>();
        void Reach(int x, int z)
        {
            if (!map.InBounds(x, z) || reached[z * w + x] || map.IsSolid(x, z)) return;
            reached[z * w + x] = true;
            open.Enqueue((x, z));
        }
        // A rock face is a blocked tile that Rock Climb takes one up or down along its grain (Mt. Coronet's upper
        // floors lie past them): the floor at its far end is reached all the same, as the field's rule reaches it
        void Step(int x, int z, int dx, int dz)
        {
            int nx = x + dx, nz = z + dz;
            if (map.InBounds(nx, nz) && map.BehaviourAt(nx, nz) is var face
                && (face == TileBehavior.RockClimbNorthSouth && dz != 0 || face == TileBehavior.RockClimbEastWest && dx != 0))
                while (map.InBounds(nx, nz) && map.BehaviourAt(nx, nz) == face) { nx += dx; nz += dz; }
            Reach(nx, nz);
        }
        foreach (var (x, z) in entrances)
        {
            // A way in may itself be a blocked tile (a door): the cave starts beside it
            Reach(x, z);
            Reach(x + 1, z); Reach(x - 1, z); Reach(x, z + 1); Reach(x, z - 1);
        }
        while (open.Count > 0)
        {
            var (x, z) = open.Dequeue();
            Step(x, z, 1, 0); Step(x, z, -1, 0); Step(x, z, 0, 1); Step(x, z, 0, -1);
            foreach (var way in new[] { Direction.Left, Direction.Right })
            {
                var (dx, dz) = FieldMovement.Delta(way);
                if (!map.InBounds(x + dx, z + dz) || FieldMovement.RampDirection(map.BehaviourAt(x + dx, z + dz)) != way) continue;
                Reach(x + 2 * dx, z + 2 * dz);
                Reach(x + 4 * dx, z + 4 * dz);
            }
        }
        foreach (var (x, z) in vague)
            if (reached[z * w + x]) map.SetGroundTile(x, z, TileType.CaveFloor, isSolid: false);
        // The rest is rock: the space past the walls, and any pocket of floor the walls close in
        for (int z = 0; z < h; z++)
            for (int x = 0; x < w; x++)
                if (!map.IsSolid(x, z) && !reached[z * w + x]) map.SetGroundTile(x, z, TileType.CaveWall, isSolid: true);
        OpenWaysOut(map, entrances);
        RaiseRock(map, IsRock);
    }

    /// <summary>
    /// A cave's ways out, seen from inside: the original leaves the cave by a mat at the floor's edge (<c>Exit…</c>),
    /// walked off into the rock, and paints the rock past it as the dark of a passage. That tile is the mouth's dark
    /// here too, at the mat's height, so it stays a hollow under the rock round it instead of a wall like the rest.
    /// </summary>
    private static void OpenWaysOut(Map map, HashSet<(int X, int Z)> entrances)
    {
        foreach (var (x, z) in entrances)
        {
            if (!map.InBounds(x, z) || map.IsSolid(x, z)) continue;
            Direction? way = map.BehaviourAt(x, z) switch
            {
                TileBehavior.ExitNorth => Direction.Up,
                TileBehavior.ExitSouth => Direction.Down,
                TileBehavior.ExitWest => Direction.Left,
                TileBehavior.ExitEast => Direction.Right,
                _ => null
            };
            if (way is not { } out_) continue;
            var (dx, dz) = FieldMovement.Delta(out_);
            int bx = x + dx, bz = z + dz;
            if (!map.InBounds(bx, bz) || map.GetGroundTile(bx, bz) != TileType.CaveWall) continue;
            map.SetGroundTile(bx, bz, TileType.CaveMouth, isSolid: true);
            map.SetHeight(bx, bz, map.HeightAt(x, z));
        }
    }

    /// <summary>How far below the islands of the Distortion World the drop under them is drawn, in tiles: the depth of their undersides.</summary>
    public const float VoidDrop = 3f;

    /// <summary>
    /// How far the Distortion World's rock stands above the island it rims: the least that is still a step with a
    /// face, so a path between two rims stays in view.
    /// </summary>
    public const float VoidRimRise = 0.75f;

    /// <summary>
    /// Finishes the Distortion World's islands (plan 01 · M8; style guide, "The Distortion World"). Water that reaches
    /// the map's edge is the sea far under the islands: it lies at the drop's depth and fills the nothing round
    /// them, so the islands stand out of it. Everything else that is nothing lies <see cref="VoidDrop"/> under the
    /// nearest island, so each island's edge shows its underside; and the rock that rims an island stands
    /// <see cref="VoidRimRise"/> above it.
    /// </summary>
    private static void VoidIslands(Map map)
    {
        int w = map.Width, h = map.Height;
        bool Nothing(int x, int z) => map.GetGroundTile(x, z) == TileType.Void;
        bool Water(int x, int z) => map.GetGroundTile(x, z) == TileType.Water;

        // The sea: water joined to the map's edge, and the nothing joined to it
        var sea = new bool[w * h];
        var flood = new Queue<(int X, int Z)>();
        for (int z = 0; z < h; z++)
            for (int x = 0; x < w; x++)
                if ((x == 0 || z == 0 || x == w - 1 || z == h - 1) && Water(x, z))
                {
                    sea[z * w + x] = true;
                    flood.Enqueue((x, z));
                }
        while (flood.Count > 0)
        {
            var (x, z) = flood.Dequeue();
            foreach (var (dx, dz) in new[] { (0, 1), (1, 0), (-1, 0), (0, -1) })
            {
                int nx = x + dx, nz = z + dz;
                if (!map.InBounds(nx, nz) || sea[nz * w + nx] || !(Water(nx, nz) || Nothing(nx, nz))) continue;
                sea[nz * w + nx] = true;
                flood.Enqueue((nx, nz));
            }
        }
        for (int i = 0; i < sea.Length; i++)
        {
            if (!sea[i]) continue;
            int x = i % w, z = i / w;
            // Nobody surfs on the sea down there; a gap over it is still jumped
            if (Nothing(x, z) && FieldMovement.LongJumpDirection(map.BehaviourAt(x, z)) == null) map.SetBehaviour(x, z, TileBehavior.Sea);
            map.SetGroundTile(x, z, TileType.Water, isSolid: true);
        }

        // The height of the nearest island, for each tile of the drop and of the sea
        bool Below(int x, int z) => sea[z * w + x] || Nothing(x, z);
        var near = new float[w * h];
        var seen = new bool[w * h];
        var front = new Queue<(int X, int Z)>();
        for (int z = 0; z < h; z++)
            for (int x = 0; x < w; x++)
            {
                if (Below(x, z)) continue;
                near[z * w + x] = map.HeightAt(x, z);
                seen[z * w + x] = true;
                front.Enqueue((x, z));
            }
        if (front.Count == 0) return;
        while (front.Count > 0)
        {
            var (x, z) = front.Dequeue();
            foreach (var (dx, dz) in new[] { (0, 1), (1, 0), (-1, 0), (0, -1) })
            {
                int nx = x + dx, nz = z + dz;
                if (!map.InBounds(nx, nz) || seen[nz * w + nx]) continue;
                seen[nz * w + nx] = true;
                near[nz * w + nx] = near[z * w + x];
                front.Enqueue((nx, nz));
            }
        }
        // The sea is one level, under the lowest island that stands in it
        float seaLevel = float.MaxValue;
        for (int i = 0; i < sea.Length; i++)
            if (sea[i]) seaLevel = MathF.Min(seaLevel, MathF.Floor(near[i]) - VoidDrop);
        for (int z = 0; z < h; z++)
            for (int x = 0; x < w; x++)
            {
                if (sea[z * w + x]) map.SetHeight(x, z, seaLevel);
                else if (Below(x, z)) map.SetHeight(x, z, MathF.Floor(near[z * w + x]) - VoidDrop);
                else if (map.GetGroundTile(x, z) == TileType.Rock && map.IsSolid(x, z)) map.SetHeight(x, z, map.HeightAt(x, z) + VoidRimRise);
            }
    }

    /// <summary>
    /// What a tile is in the Distortion World: what the original doesn't say anything of is nothing where it blocks
    /// and the islands' stone where it is walked on, and a gap jumped across is nothing too. Rock, water, grass and
    /// trees are what they are.
    /// </summary>
    public static (TileType Type, bool Solid, PropType? Prop) InTheVoid(TerrainCover cover, TileBehavior behaviour, bool solid, TileType type, bool blocks, PropType? prop)
    {
        if (FieldMovement.LongJumpDirection(behaviour) != null) return (TileType.Void, true, null);
        // A ledge is a drop from one island down onto the next (B5F's), hopped whatever the original's blocked flag says
        if (FieldMovement.LedgeDirection(behaviour) != null) return (type, blocks, prop);
        if (cover == TerrainCover.Unknown) return solid ? (TileType.Void, true, null) : (TileType.DistortionGround, false, null);
        // The original's marks on its floors (behaviour 0x08) are the stone like the rest: the ways somewhere get slabs
        if (type == TileType.CaveFloor) return (TileType.DistortionGround, blocks, null);
        return (type, blocks, prop);
    }

    /// <summary>
    /// A blocked tile with nothing drawn on it in the original is forest where trees stand within
    /// <see cref="BlankReach"/> tiles of it, and rock anywhere else: the bare ground round Mt. Coronet's faces and
    /// past Spear Pillar's way in was pine forest because whatever blocks without saying what it is was taken for a tree.
    /// </summary>
    private static void BareRock(Map map, List<(int X, int Z)> blank)
    {
        var isBlank = new HashSet<(int X, int Z)>(blank);
        bool TreeNear(int x, int z)
        {
            for (int dz = -BlankReach; dz <= BlankReach; dz++)
                for (int dx = -BlankReach; dx <= BlankReach; dx++)
                {
                    int nx = x + dx, nz = z + dz;
                    if (map.InBounds(nx, nz) && !isBlank.Contains((nx, nz)) && map.GetGroundTile(nx, nz) == TileType.Tree) return true;
                }
            return false;
        }
        // Decided on the map as it was, so the first tiles turned to rock don't decide their neighbours
        var bare = blank.Where(t => !TreeNear(t.X, t.Z)).ToList();
        foreach (var (x, z) in bare) map.SetGroundTile(x, z, TileType.Rock, isSolid: true);
    }

    /// <summary>How far a blocked tile nobody named looks for trees before it is taken for forest.</summary>
    public const int BlankReach = 2;

    /// <summary>
    /// Stands rock up above the ground (style guide, "Caves" and "Rock outdoors"): each tile of rock on the
    /// height of the nearest ground that isn't rock, plus <see cref="CaveWallRise"/>, or only
    /// <see cref="CaveLipRise"/> where open ground lies right behind it. A cave's walls are such rock, and so are
    /// the mountainsides of the open country: the world's own heights say nothing of rock nobody walks on, which
    /// by them would lie as flat as the road beside it.
    /// </summary>
    private static void RaiseRock(Map map, Func<int, int, bool> IsRock)
    {
        int w = map.Width, h = map.Height;

        // Each tile of rock stands on the height of the nearest ground that isn't rock
        var near = new float[w * h];
        var seen = new bool[w * h];
        var front = new Queue<(int X, int Z)>();
        for (int z = 0; z < h; z++)
            for (int x = 0; x < w; x++)
            {
                if (IsRock(x, z)) continue;
                // Water is drawn level with its banks (Relief), so rock beside it stands on the bank's height
                near[z * w + x] = map.IsDeepWater(x, z) ? MathF.Ceiling(map.HeightAt(x, z) - 0.01f) : map.HeightAt(x, z);
                seen[z * w + x] = true;
                front.Enqueue((x, z));
            }
        if (front.Count == 0) return;
        while (front.Count > 0)
        {
            var (x, z) = front.Dequeue();
            foreach (var (dx, dz) in new[] { (0, 1), (1, 0), (-1, 0), (0, -1) })
            {
                int nx = x + dx, nz = z + dz;
                if (!map.InBounds(nx, nz) || seen[nz * w + nx]) continue;
                seen[nz * w + nx] = true;
                near[nz * w + nx] = near[z * w + x];
                front.Enqueue((nx, nz));
            }
        }

        for (int z = 0; z < h; z++)
            for (int x = 0; x < w; x++)
            {
                if (!IsRock(x, z)) continue;
                bool floorBehind = map.InBounds(x, z - 1) && !IsRock(x, z - 1);
                map.SetHeight(x, z, near[z * w + x] + (floorBehind ? CaveLipRise : CaveWallRise));
            }
    }

    /// <summary>
    /// A city's street lamps. The import knows a lamp by its texture, and Jubilife's stand on a texture it takes
    /// for a fence: one tile of "fence" by itself, with no fence beside it, in a paved street. Such a tile is a
    /// lamp standing on the paving round it; a post alone in a meadow stays a post.
    /// </summary>
    private static void StreetLamps(Map map, bool[] under)
    {
        var fenced = new HashSet<(int X, int Y)>();
        foreach (var prop in map.Props)
            if (prop.Type is PropType.Fence or PropType.LowWall) fenced.Add((prop.X, prop.Y));

        var lamps = new List<Prop>();
        foreach (var prop in map.Props)
        {
            if (prop.Type != PropType.Fence || prop.Width != 1 || prop.Depth != 1) continue;
            var (x, y) = (prop.X, prop.Y);
            if (fenced.Contains((x + 1, y)) || fenced.Contains((x - 1, y)) || fenced.Contains((x, y + 1)) || fenced.Contains((x, y - 1))) continue;
            under[y * map.Width + x] = true;
            if (GroundLike(map, x, y, under) != TileType.Paving) continue;
            lamps.Add(prop);
        }
        foreach (var post in lamps)
        {
            map.Props.Remove(post);
            map.Props.Add(new Prop { Type = PropType.LampPost, X = post.X, Y = post.Y });
            map.SetGroundTile(post.X, post.Y, TileType.Paving, isSolid: true);
        }
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
            Calendar = file?.Weather is { } named && world.Calendar.TryGetValue(named, out var days) ? days : null,
            Trees = overlay?.Trees,
            Architecture = overlay?.Architecture,
            Arena = overlay?.BattleArena,
            BattleBackground = file?.BattleBackground ?? "",
            Kind = file?.Kind ?? "",
            BikeAllowed = file?.Bike ?? false,
            EscapeRopeAllowed = file?.EscapeRope ?? false,
            FlyAllowed = file?.Fly ?? false,
            EastSea = file?.EastSea ?? false,
            UnownTable = file?.UnownTable ?? 0
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
        // What the time of day, a swarm and the Poké Radar put in the land's slots (plan 06 · R13)
        area.DaySlots.AddRange(file?.Day ?? new());
        area.NightSlots.AddRange(file?.Night ?? new());
        area.SwarmSlots.AddRange(file?.Swarm ?? new());
        area.RadarSlots.AddRange(file?.Radar ?? new());
        area.WaterRate = file?.WaterRate ?? 0;
        // The rods' tables: the Old Rod's slots bite as often as the water's, the other two's by their own weights
        Fill(area.RodEncounters[(int)FishingRod.Old], file?.OldRod, WorldAreaFile.WaterSlotWeights);
        Fill(area.RodEncounters[(int)FishingRod.Good], file?.GoodRod, WorldAreaFile.RodSlotWeights);
        Fill(area.RodEncounters[(int)FishingRod.Super], file?.SuperRod, WorldAreaFile.RodSlotWeights);
        area.RodRates[(int)FishingRod.Old] = file?.OldRodRate ?? 0;
        area.RodRates[(int)FishingRod.Good] = file?.GoodRodRate ?? 0;
        area.RodRates[(int)FishingRod.Super] = file?.SuperRodRate ?? 0;
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
            TerrainCover.Puddle => (TileType.Puddle, solid, null),
            TerrainCover.CaveFloor => (TileType.CaveFloor, solid, null),
            // The dark of a cave's mouth: the hole in the rock where it blocks, the way in where it is open
            TerrainCover.CaveMouth => (TileType.CaveMouth, solid, null),
            // The dark under the trees where a forest is entered, the same way round
            TerrainCover.ForestMouth => (TileType.ForestMouth, solid, null),
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

    /// <summary>
    /// What a tile is in a cave: whatever blocks the way without being water or something built is the cave's own
    /// rock (what the open country takes for a tree, a fence, a rock face), and a rock that stands on the floor
    /// stands on the cave's floor.
    /// </summary>
    public static (TileType Type, PropType? Prop) InACave(TileType type, bool solid, PropType? prop) => type switch
    {
        TileType.Tree or TileType.Rock when solid => (TileType.CaveWall, null),
        TileType.Grass when solid => (TileType.CaveWall, null),
        // Seen from inside, a way out is the floor that leads to it and the rock round it
        TileType.CaveMouth => (solid ? TileType.CaveWall : TileType.CaveFloor, null),
        TileType.Dirt when prop == PropType.Boulder => (TileType.CaveFloor, prop),
        _ => (type, prop)
    };

    /// <summary>Behaviours whose plates lie one over the other: the deck of a bridge, and the ground or water under it.</summary>
    private static bool HasDeck(TileBehavior behaviour) => behaviour is TileBehavior.Bridge or TileBehavior.BridgeEnd
        or TileBehavior.BridgeOverCave or TileBehavior.BridgeOverWater or TileBehavior.BridgeOverSnow
        || FieldMovement.BikePlankRunsNorthSouth(behaviour) != null;

    /// <param name="altitude">How high the matrix sets the whole chunk, in tiles.</param>
    /// <param name="under">Marks the tiles whose look is settled later, by the model that stands on them.</param>
    /// <param name="vague">Gathers the open tiles of ground nobody named.</param>
    /// <param name="blank">Gathers the blocked tiles the original draws nothing on.</param>
    private static void PlaceChunk(Map map, WorldChunkFile chunk, int ox, int oy, float altitude, MapArea area, bool[] under,
        List<(int X, int Z)> vague, List<(int X, int Z)> blank)
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
                if (map.IsCave) (type, prop) = InACave(type, blocks, prop);
                else if (map.IsVoid) (type, blocks, prop) = InTheVoid(cover, behaviour, solid, type, blocks, prop);
                // An area that isn't built yet is scenery: seen from its neighbours, entered by nobody
                map.SetGroundTile(ox + x, oy + z, type, blocks || !area.Open);
                if (cover is TerrainCover.Building or TerrainCover.Lamp && solid) under[(oy + z) * map.Width + ox + x] = true;
                if (cover == TerrainCover.Unknown && !solid && type == TileType.Grass) vague.Add((ox + x, oy + z));
                if (cover == TerrainCover.Unknown && solid && type == TileType.Tree) blank.Add((ox + x, oy + z));
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

    /// <summary>Open tiles of a cave's mouth with no way in on them or beside them.</summary>
    private static List<(int X, int Z)> OpenMouthsLeadingNowhere(Map map, HashSet<(int X, int Z)> entrances)
    {
        var found = new List<(int X, int Z)>();
        for (int z = 0; z < map.Height; z++)
            for (int x = 0; x < map.Width; x++)
            {
                if (map.GetGroundTile(x, z) != TileType.CaveMouth || map.IsSolid(x, z)) continue;
                bool leads = false;
                for (int dz = -1; dz <= 1 && !leads; dz++)
                    for (int dx = -1; dx <= 1 && !leads; dx++)
                        leads = entrances.Contains((x + dx, z + dz));
                if (!leads) found.Add((x, z));
            }
        return found;
    }

    /// <summary>
    /// The ground a tile under a model takes: that of the nearest tile round it which no model stands on, so a
    /// lamp in a paved street stands on paving and a ship in the harbour on water.
    /// </summary>
    private static TileType GroundLike(Map map, int x, int z, bool[] under, bool[]? unnamed = null)
    {
        for (int reach = 1; reach <= 4; reach++)
        {
            // Made or bare ground at this distance wins over lawn at the same distance: a heap of coal between
            // a paved yard and a row of trees lies on the paving
            bool lawn = false;
            foreach (var (dx, dz) in new[] { (0, reach), (-reach, 0), (reach, 0), (0, -reach), (-reach, reach), (reach, reach), (-reach, -reach), (reach, -reach) })
            {
                int nx = x + dx, nz = z + dz;
                if (!map.InBounds(nx, nz) || under[nz * map.Width + nx] || unnamed?[nz * map.Width + nx] == true) continue;
                switch (map.GetGroundTile(nx, nz))
                {
                    case TileType.Path or TileType.Paving or TileType.Sand or TileType.Snow or TileType.Dirt or TileType.Rock or TileType.Water
                        or TileType.Planks or TileType.Ice or TileType.Marsh or TileType.CaveFloor:
                        return map.GetGroundTile(nx, nz);
                    case TileType.Grass or TileType.FlowerGrass or TileType.TallGrass or TileType.Tree:
                        lawn = true;
                        break;
                }
            }
            if (lawn) return TileType.Grass;
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
    private static List<Part> PlaceModels(World world, int matrixId, Map map, List<Placed> models, bool[] under, bool[] unnamed, HashSet<(int X, int Z)> entrances)
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
        var conveyed = new List<(int X, int Z)>();
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
            // A yard's thin pieces carry its conveyors; anyone else's are a fence or a low wall
            if (model.Model?.Thin == PropType.Conveyor)
            {
                conveyed.AddRange(thin);
                continue;
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
                map.SetGroundTile(x, z, afloat ? TileType.Water : GroundLike(map, x, z, under, unnamed), isSolid: true);
            }
        foreach (var (x, z, wood) in fenced) map.Props.Add(new Prop { Type = wood ? PropType.Fence : PropType.LowWall, X = x, Y = z });
        map.Props.AddRange(Conveyors(conveyed, (x, z) => !map.InBounds(x, z) || Blocked(x, z)));

        foreach (var model in models)
        {
            if (model.Model is not { Role: ModelRole.Scenery } scenery) continue;
            // The tiles whose middle lies in the box; a thing smaller than a tile stands on the one under its middle
            int x0 = (int)MathF.Floor(model.X - 0.5f) + 1, x1 = (int)MathF.Ceiling(model.X1 - 0.5f) - 1;
            int z0 = (int)MathF.Floor(model.Z - 0.5f) + 1, z1 = (int)MathF.Ceiling(model.Z1 - 0.5f) - 1;
            if (x1 < x0) x0 = x1 = (int)MathF.Floor((model.X + model.X1) / 2f);
            if (z1 < z0) z0 = z1 = (int)MathF.Floor((model.Z + model.Z1) / 2f);
            // Boats moored side by side whose boxes overlap (Pastoria's two) each keep their own side of the line
            // halfway between them, or their decks would run together into one slab
            if (scenery.Prop == PropType.Boat)
                foreach (var other in models)
                {
                    if (ReferenceEquals(other, model) || other.Model?.Prop != PropType.Boat
                        || other.X >= model.X1 || model.X >= other.X1 || other.Z >= model.Z1 || model.Z >= other.Z1) continue;
                    float mx = (model.X + model.X1) / 2f, mz = (model.Z + model.Z1) / 2f, ox = (other.X + other.X1) / 2f, oz = (other.Z + other.Z1) / 2f;
                    // A tile is this boat's when its middle lies on this boat's side of the line
                    if (MathF.Abs(mz - oz) >= MathF.Abs(mx - ox))
                    {
                        float line = (mz + oz) / 2f;
                        if (mz < oz) z1 = Math.Min(z1, (int)MathF.Ceiling(line - 0.5f) - 1);
                        else z0 = Math.Max(z0, (int)MathF.Ceiling(line - 0.5f));
                    }
                    else
                    {
                        float line = (mx + ox) / 2f;
                        if (mx < ox) x1 = Math.Min(x1, (int)MathF.Ceiling(line - 0.5f) - 1);
                        else x0 = Math.Max(x0, (int)MathF.Ceiling(line - 0.5f));
                    }
                }
            // A stack of rock in the sea stands on the rock the world blocks: Sunyshore's box lies two rows south of
            // its blocked tiles, over water a swimmer crosses. It takes the blocked tiles joined to those under its box
            if (scenery.Prop == PropType.SeaStack) (x0, z0, x1, z1) = BlockedRound(x0, z0, x1, z1, Blocked);
            if (!map.InBounds(x0, z0)) continue;
            map.Props.Add(new Prop
            {
                Type = scenery.Prop, X = x0, Y = z0, Width = x1 - x0 + 1, Depth = z1 - z0 + 1,
                Height = model.Prop.Height, Model = model.Prop.Name
            });
        }
        return parts;
    }

    /// <summary>
    /// The rectangle of the blocked tiles joined to those inside a rectangle, reaching no more than
    /// <see cref="BlockedReach"/> tiles past it; the rectangle itself when none of it is blocked.
    /// </summary>
    public static (int X0, int Z0, int X1, int Z1) BlockedRound(int x0, int z0, int x1, int z1, Func<int, int, bool> blocked)
    {
        var seen = new HashSet<(int X, int Z)>();
        var open = new Queue<(int X, int Z)>();
        for (int z = z0; z <= z1; z++)
            for (int x = x0; x <= x1; x++)
                if (blocked(x, z) && seen.Add((x, z))) open.Enqueue((x, z));
        if (seen.Count == 0) return (x0, z0, x1, z1);
        while (open.Count > 0)
        {
            var (x, z) = open.Dequeue();
            foreach (var (nx, nz) in new[] { (x + 1, z), (x - 1, z), (x, z + 1), (x, z - 1) })
                if (nx >= x0 - BlockedReach && nx <= x1 + BlockedReach && nz >= z0 - BlockedReach && nz <= z1 + BlockedReach
                    && blocked(nx, nz) && seen.Add((nx, nz)))
                    open.Enqueue((nx, nz));
        }
        return (seen.Min(t => t.X), seen.Min(t => t.Z), seen.Max(t => t.X), seen.Max(t => t.Z));
    }

    /// <summary>How far past its box a sea stack's blocked rock is looked for, in tiles.</summary>
    public const int BlockedReach = 3;

    /// <summary>How far a conveyor reaches from a gantry, in tiles, toward what feeds it or what it fills.</summary>
    public const int ConveyorReach = 8;

    /// <summary>
    /// Lays a yard's conveyors from the thin pieces its model blocks (plan 01 · M5; style guide, "The mine").
    /// A straight run of three pieces or more carries a belt along itself. Two pieces that face each other
    /// across one open tile are the legs of a gantry, and the belt passes between them, across the pair: it
    /// runs on either way over the open ground until something blocked takes it up (a shed, a heap of coal, a
    /// run that carries it on), if that is within <see cref="ConveyorReach"/>. Whatever is left is a pier
    /// standing alone. Returns the belts (<see cref="PropType.Conveyor"/>, one tile wide and as long as they
    /// run) and what carries them (<see cref="PropType.Gantry"/>).
    /// </summary>
    public static List<Prop> Conveyors(IEnumerable<(int X, int Z)> thin, Func<int, int, bool> blocked)
    {
        var pieces = new HashSet<(int X, int Z)>(thin);
        var props = new List<Prop>();
        if (pieces.Count == 0) return props;

        int Run(int x, int z, int dx, int dz)
        {
            int n = 1;
            for (int i = 1; pieces.Contains((x + dx * i, z + dz * i)); i++) n++;
            for (int i = 1; pieces.Contains((x - dx * i, z - dz * i)); i++) n++;
            return n;
        }

        // Gantries first: a pair east and west of an open tile (the commoner), then north and south of one
        var legs = new HashSet<(int X, int Z)>();
        var through = new List<(int X, int Z, bool NorthSouth)>();
        var ordered = pieces.OrderBy(p => p.Z).ThenBy(p => p.X).ToList();
        foreach (var (x, z) in ordered)
            if (pieces.Contains((x + 2, z)) && !blocked(x + 1, z))
            {
                legs.Add((x, z));
                legs.Add((x + 2, z));
                through.Add((x + 1, z, true));
            }
        foreach (var (x, z) in ordered)
            if (!legs.Contains((x, z)) && pieces.Contains((x, z + 2)) && !legs.Contains((x, z + 2)) && !blocked(x, z + 1))
            {
                legs.Add((x, z));
                legs.Add((x, z + 2));
                through.Add((x, z + 1, false));
            }

        // The belts, tile by tile: true where one runs north and south. Of what is left, a straight run of three
        // or more carries one along itself
        var belt = new Dictionary<(int X, int Z), bool>();
        var carriers = new HashSet<(int X, int Z)>();
        pieces.ExceptWith(legs);
        foreach (var (x, z) in pieces)
        {
            int ns = Run(x, z, 0, 1), ew = Run(x, z, 1, 0);
            if (Math.Max(ns, ew) < 3) continue;
            carriers.Add((x, z));
            belt[(x, z)] = ns >= ew;
        }

        // One gantry for each row of such pairs, and the belt through it, as far as it finds something to meet
        foreach (var group in through.GroupBy(t => (t.NorthSouth, Line: t.NorthSouth ? t.X : t.Z)))
        {
            bool ns = group.Key.NorthSouth;
            var along = group.Select(t => ns ? t.Z : t.X).OrderBy(v => v).ToList();
            for (int i = 0; i < along.Count;)
            {
                int j = i;
                while (j + 1 < along.Count && along[j + 1] == along[j] + 1) j++;
                int from = along[i], to = along[j], line = group.Key.Line;
                props.Add(ns
                    ? new Prop { Type = PropType.Gantry, X = line - 1, Y = from, Width = 3, Depth = to - from + 1 }
                    : new Prop { Type = PropType.Gantry, X = from, Y = line - 1, Width = to - from + 1, Depth = 3 });
                for (int v = from; v <= to; v++) belt[ns ? (line, v) : (v, line)] = ns;

                foreach (int step in new[] { -1, 1 })
                {
                    var reach = new List<(int X, int Z)>();
                    bool met = false;
                    for (int n = 1; n <= ConveyorReach + 1; n++)
                    {
                        int v = (step < 0 ? from : to) + step * n;
                        var tile = ns ? (line, v) : (v, line);
                        if (blocked(tile.Item1, tile.Item2) || belt.ContainsKey(tile)) { met = true; break; }
                        reach.Add(tile);
                    }
                    if (met && reach.Count <= ConveyorReach)
                        foreach (var tile in reach) belt[tile] = ns;
                }
                i = j + 1;
            }
        }

        foreach (var (x, z) in pieces.Where(p => !carriers.Contains(p)).OrderBy(p => p.Z).ThenBy(p => p.X))
            props.Add(new Prop { Type = PropType.Gantry, X = x, Y = z });

        // The belts as runs
        foreach (var column in belt.Where(b => b.Value).GroupBy(b => b.Key.X).OrderBy(g => g.Key))
        {
            var rows = column.Select(b => b.Key.Z).OrderBy(v => v).ToList();
            for (int i = 0; i < rows.Count;)
            {
                int j = i;
                while (j + 1 < rows.Count && rows[j + 1] == rows[j] + 1) j++;
                props.Add(new Prop { Type = PropType.Conveyor, X = column.Key, Y = rows[i], Width = 1, Depth = rows[j] - rows[i] + 1 });
                i = j + 1;
            }
        }
        foreach (var row in belt.Where(b => !b.Value).GroupBy(b => b.Key.Z).OrderBy(g => g.Key))
        {
            var columns = row.Select(b => b.Key.X).OrderBy(v => v).ToList();
            for (int i = 0; i < columns.Count;)
            {
                int j = i;
                while (j + 1 < columns.Count && columns[j + 1] == columns[j] + 1) j++;
                props.Add(new Prop { Type = PropType.Conveyor, X = columns[i], Y = row.Key, Width = columns[j] - columns[i] + 1, Depth = 1 });
                i = j + 1;
            }
        }
        return props;
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
        "var_0" or "lass" or "school_kid_f" or "twin" or "picnicker" or "little_girl" or "battle_girl" or "ace_trainer_f" or "cyclist_f" => "Lass",
        "youngster" or "school_kid_m" or "guitarist" or "bug_catcher" or "camper" or "little_boy" or "kid_with_nds" or "cyclist_m"
            or "ace_trainer_m" => "Youngster",
        "pokemon_breeder_f" or "beauty" or "lady" or "aroma_lady" or "parasol_lady" or "socialite" or "middle_aged_woman" or "old_woman"
            or "pokefan_f" => "Lady",
        "collector" or "gentleman" or "old_man" or "rich_boy" or "scientist_m" or "middle_aged_man" or "expert_m" or "hiker" or "worker"
            or "pokefan_m" => "Gentleman",
        // The centre of Sinnoh's people (plan 01 · M6), as the nearest of the characters there are
        "fisherman" or "pokemon_breeder_m" or "black_belt" or "ruin_maniac" or "artist" or "rancher" or "gym_guide" => "Gentleman",
        "jogger" or "ninja_boy" or "psychic" => "Youngster",
        "cowgirl" or "idol" or "receptionist" => "Lass",
        "cashier_m" or "cashier_f" or "clerk" or "waiter" => "Clerk",
        "clown" => "Clown",
        "looker" => "Looker",
        "cyrus" => "Cyrus",
        "roark" => "Roark",
        "grunt_m" or "grunt_f" => "Grunt",
        "briefcase" => "StarterBriefcase",
        _ => "Trainer"
    };

    /// <summary>
    /// The species (or form) one of the original's objects is, by the name of its looks, where it is a Pokémon
    /// standing in the field (plan 10 · F1): the mine's Machop, the Windworks' Drifloon, the lake guardians. Null for
    /// anyone and anything else. A trainer is never a Pokémon, whatever looks they wear (Route 209's Poké Kid).
    /// </summary>
    public static string? SpeciesFor(string looks) => looks switch
    {
        "machop" => "Machop",
        "drifloon" => "Drifloon",
        "pachirisu" => "Pachirisu",
        "buneary" => "Buneary",
        "happiny" => "Happiny",
        "pikachu" => "Pikachu",
        "clefairy" => "Clefairy",
        "croagunk" => "Croagunk",
        "psyduck" => "Psyduck",
        "starly" => "Starly",
        "uxie" => "Uxie",
        "mesprit" => "Mesprit",
        "azelf" => "Azelf",
        "giratina_altered" => "Giratina",
        "giratina_origin" => "Giratina-Origin",
        _ => null
    };

    private static bool IsSignpost(string looks) => looks is "map_signpost" or "arrow_signpost" or "signboard" or "trainer_tips_signpost" or "gym_signpost";

    /// <summary>
    /// A flag of the original's that lasts only while the player is in its area (<c>FLAG_MAP_LOCAL_...</c>, cleared
    /// on leaving: <see cref="Story.StoryState.ClearLocal"/>) made the area's own, since one map of the world holds
    /// many areas that number their local flags alike: <c>FLAG_MAP_LOCAL_HIDE_OBSTACLE_1_ETERNA_CITY</c>. Any other
    /// flag is left as it is.
    /// </summary>
    public static string LocalFlag(string flag, string areaKey) =>
        flag.StartsWith(Story.StoryState.LocalFlagPrefix, StringComparison.Ordinal) ? flag + "_" + areaKey.ToUpperInvariant() : flag;

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
            if (overlay?.HeldBack?.Contains(o.Id) == true) continue;
            // A copy of a neighbouring area's object, there so it shows across the border: the neighbour places it
            if (o.HiddenBy != null && o.HiddenBy.StartsWith("MAP_HEADER_", StringComparison.Ordinal)) continue;
            // The two halves of a route list each other's people and signs without saying so: whoever stands on
            // the other half, where that half has the same thing on the same tile, is the other half's to place
            if (map.InBounds(o.X, o.Z) && map.AreaAt(o.X, o.Z) is { Open: true } there && there != area
                && world.Area(there.Key)?.Objects.Any(twin => twin.X == o.X && twin.Z == o.Z && twin.Looks == o.Looks) == true) continue;

            if (IsSignpost(o.Looks) && o.HiddenBy == null)
            {
                map.SetGroundTile(o.X, o.Z, TileType.Signpost, isSolid: true);
                map.Signboards[(o.X, o.Z)] = overlay?.Signs?.GetValueOrDefault(o.Id) ?? area.DisplayName;
                if (overlay?.SignScripts?.GetValueOrDefault(o.Id) is { Length: > 0 } read) map.SignScripts[(o.X, o.Z)] = read;
                continue;
            }
            if (o.Looks == "mailbox")
            {
                map.AddProp(PropType.Mailbox, o.X, o.Z);
                map.Signboards[(o.X, o.Z)] = overlay?.Signs?.GetValueOrDefault(o.Id) ?? "A mailbox.";
                if (overlay?.SignScripts?.GetValueOrDefault(o.Id) is { Length: > 0 } opened) map.SignScripts[(o.X, o.Z)] = opened;
                continue;
            }
            // An obstacle is an object of the map, as the original's is (plan 02 · S2): cleared by its field move, it
            // stays gone by its own flag, which is local to its area and cleared when the player leaves it
            if (ObstacleFor(o.Looks) is { } obstacle)
            {
                var thing = map.AddObstacle(obstacle, o.X, o.Z);
                thing.HiddenBy = o.HiddenBy is { } flag ? LocalFlag(flag, key) : null;
                thing.Key = o.Id;
                thing.ScriptFile = key;
                continue;
            }
            // An item in its ball lies where the original has it, and stays gone by the original's own flag
            if (o.Item != null && o.HiddenBy != null)
            {
                map.NPCs.Add(new NPC
                {
                    Name = o.Item,
                    NpcType = NPC.ItemBallType,
                    GridX = o.X,
                    GridY = o.Z,
                    Item = o.Item,
                    ItemCount = o.Count ?? 1,
                    HiddenBy = o.HiddenBy,
                    Key = o.Id,
                    ScriptFile = key
                });
                continue;
            }

            // People appear only once the game says who they are
            if (overlay?.People == null || !overlay.People.TryGetValue(o.Id, out var person)) continue;
            // A Pokémon of the original's stands as itself, unless the overlay makes someone else of it (plan 10 · F1)
            string? species = person.NpcType == null && person.Trainer == null ? SpeciesFor(o.Looks) : null;
            var npc = MapFile.BuildNpc(new MapFile.NpcRecord
            {
                Id = person.Id ?? person.Trainer?.Id,
                Name = person.Name,
                NpcType = person.NpcType ?? (species != null ? NPC.PokemonType : CharacterFor(o.Looks)),
                Species = species,
                X = o.X,
                Y = o.Z,
                Facing = FacingOf(o.Facing),
                Dialog = person.Dialog,
                IsStarterBriefcase = person.IsStarterBriefcase,
                Script = person.Script,
                // The flag that hides them is the original's own unless the overlay says otherwise
                HiddenBy = person.HiddenBy is { } hiddenBy ? (hiddenBy.Length > 0 ? hiddenBy : null) : o.HiddenBy,
                ShownBy = person.ShownBy,
                Trainer = person.Trainer,
                Item = person.Item
            }, map.Name);
            // Someone the original stands on a bridge's deck stands there, over whoever walks under it
            if (o.Y is > 0 && map.DeckAt(o.X, o.Z) is { } deck) npc.Level = deck;
            // Scripts call them by their id in the area's file, and look their own scripts up in the area's
            npc.Key = o.Id;
            npc.ScriptFile = key;
            // How they move about of their own accord, within the original's range round where they stand (plan 02 · S6)
            if (!person.Still) npc.Movement = PersonMovement.Parse(o.Movement, o.RangeX, o.RangeZ, o.X, o.Z, npc.Facing);
            // How far a trainer sees is the original's own number
            if (npc.TrainerData != null && o.Sight is { } sight) npc.TrainerData.SightRange = sight;
            // How it thinks, what it carries and its team are Platinum's (plan 06 · R9)
            if (npc.TrainerData != null && TrainerDatabase.Get(o.Script) is { } platinum) TrainerDatabase.Fill(npc.TrainerData, platinum);
            map.NPCs.Add(npc);
        }

        foreach (var record in overlay?.Npcs ?? new())
        {
            var npc = MapFile.BuildNpc(record, map.Name);
            npc.ScriptFile = key;
            map.NPCs.Add(npc);
        }

        // What is hidden in the ground, found by looking at its tile; and what is read there, where the overlay
        // gives the original's script one of ours
        foreach (var s in file.Signs)
        {
            if (!map.InBounds(s.X, s.Z)) continue;
            if (s.Type == AreaSign.HiddenItem && s.Item != null && s.Flag != null)
                map.HiddenItems[(s.X, s.Z)] = new HiddenItem(s.Item, s.Count ?? 1, s.Flag, s.Range ?? 0);
            else if (overlay?.Read?.GetValueOrDefault(s.Script) is { Length: > 0 } read)
                map.TileScripts[(s.X, s.Z)] = read;
        }

        // The original's triggers that have a script of ours: its tiles, its variable and its value; and ours, where
        // the original has none, on the tiles the overlay gives
        foreach (var bound in overlay?.Triggers ?? new())
        {
            if (bound.Trigger is not { } number)
            {
                if (bound.X is not { } x || bound.Z is not { } z)
                    throw new InvalidDataException($"A trigger in the overlay of {key} gives neither the original's number nor its own tiles.");
                map.Triggers.Add(new StepTrigger
                {
                    X = x, Y = z, Width = bound.Width, Depth = bound.Depth, Script = bound.Script, ScriptFile = key,
                    Variable = bound.Variable, Value = bound.Value
                });
                continue;
            }
            if (number < 0 || number >= file.Triggers.Count)
                throw new InvalidDataException($"The overlay of {key} gives a script to trigger {number}, and the area has {file.Triggers.Count}.");
            var t = file.Triggers[number];
            if (!int.TryParse(t.Value, out int value))
                throw new InvalidDataException($"Trigger {number} of {key} waits for the value '{t.Value}', which is no number.");
            map.Triggers.Add(new StepTrigger
            {
                X = t.X, Y = t.Z, Width = t.Width, Depth = t.Depth, Script = bound.Script, ScriptFile = key,
                Variable = t.Variable.Length > 0 ? t.Variable : null, Value = value
            });
        }
        foreach (var prop in overlay?.Props ?? new()) map.AddProp(prop.Type, prop.X, prop.Y, prop.Width, prop.Depth);

        for (int i = 0; i < file.Warps.Count; i++)
        {
            if (overlay?.Locked?.Contains(i) == true) continue;
            var from = file.Warps[i];
            Warp? warp = null;
            if (overlay?.Doors?.FirstOrDefault(d => d.Warp == i) is { } door)
            {
                warp = new Warp { TargetMap = door.Map, TargetX = door.X, TargetY = door.Y, TargetFacing = door.Facing, OpenedBy = door.OpenedBy };
            }
            else if (overlay?.Through?.FirstOrDefault(t => t.Warp == i) is { } through)
            {
                // A gate house passed through: out of its far side, at the warp there that leads into it
                warp = Join(world, through.To, through.ToWarp);
                if (warp != null) warp.CyclistsOnly = through.Bicycle;
            }
            else
            {
                warp = Join(world, from.To, from.ToWarp);
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

        // A way in that leads nowhere yet (a cave's mouth, a gate house, a hall without a door) is closed: one
        // can't walk into an opening and stand in the wall. A door is shut by itself.
        foreach (var from in file.Warps)
            if (map.GetWarpAt(from.X, from.Z) == null && map.GetGroundTile(from.X, from.Z) != TileType.Door) map.SetSolid(from.X, from.Z, true);
    }

    /// <summary>The way into an open area through one of its warps (Turnback Cave's doors, which are aimed as the player comes in).</summary>
    public static Warp? WayInto(World world, string area, int toWarp) => Join(world, area, toWarp);

    /// <summary>A warp onto one of an open area's warps, coming out one step from it; null while the area isn't open or has no map.</summary>
    private static Warp? Join(World world, string area, int toWarp)
    {
        if (!world.IsOpen(area) || world.MapOf(area) is not { } onto || world.Area(area) is not { } target || toWarp >= target.Warps.Count) return null;
        var to = target.Warps[toWarp];
        var (x, z, facing) = Arrival(world.BehaviourAt(target.Matrix, to.X, to.Z), to.X, to.Z);
        return new Warp { TargetMap = onto.Name, TargetX = x, TargetY = z, TargetFacing = facing };
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
