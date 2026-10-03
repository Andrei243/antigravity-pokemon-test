using System.Globalization;
using System.Text;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace MapImporter;

/// <summary>
/// Counts what the imported world is made of, and writes the two reports that come out of it:
/// <c>docs/tile-behaviours.md</c> (every behaviour the maps use) and the run's own <c>report.md</c> (textures,
/// props, the areas compared with the hand-made maps, and whatever the importer could not place).
/// </summary>
public sealed class Report
{
    private readonly DecompMaps decomp;
    private readonly WorldWriter world;
    private readonly Renders renders;

    private readonly long[] tiles = new long[256];
    private readonly long[] blocked = new long[256];
    private readonly HashSet<string>[] areasOf = Enumerable.Range(0, 256).Select(_ => new HashSet<string>()).ToArray();
    private readonly Dictionary<int, List<MapHeader>> chunkAreas = new();

    private sealed class TextureUse
    {
        public int Open, Blocked, Chunks;
        public bool Overworld;
    }

    private readonly SortedDictionary<string, TextureUse> textures = new(StringComparer.Ordinal);
    private readonly Dictionary<TerrainCover, long> overworldCover = new();
    private readonly Dictionary<int, int> propUse = new();
    public List<string> Problems { get; } = new();

    public Report(DecompMaps decomp, WorldWriter world, Renders renders)
    {
        this.decomp = decomp;
        this.world = world;
        this.renders = renders;
        Count();
    }

    /// <summary>The areas each chunk lies in: the ones its matrices name, or the ones that load those matrices.</summary>
    public IReadOnlyList<MapHeader> AreasOf(int landId) => chunkAreas.TryGetValue(landId, out var list) ? list : Array.Empty<MapHeader>();

    private void Count()
    {
        var byMatrix = decomp.Headers.Values.ToLookup(h => h.Matrix);
        for (int id = 0; id < decomp.MatrixCount; id++)
        {
            var matrix = decomp.Matrix(id);
            for (int y = 0; y < matrix.Height; y++)
                for (int x = 0; x < matrix.Width; x++)
                {
                    int land = matrix.LandAt(x, y);
                    if (land == Matrix.NoLand) continue;
                    if (land >= decomp.LandCount) { Problems.Add($"Matrix {id} names chunk {land}, which does not exist"); continue; }
                    if (!chunkAreas.TryGetValue(land, out var list)) chunkAreas[land] = list = new();
                    IEnumerable<MapHeader> owners = matrix.Headers != null
                        ? decomp.Headers.TryGetValue(matrix.HeaderAt(x, y)!, out var h) ? new[] { h } : Array.Empty<MapHeader>()
                        : byMatrix[id];
                    foreach (var owner in owners)
                        if (!list.Contains(owner)) list.Add(owner);
                }
        }

        var overworld = decomp.Matrix(0).Land.Where(l => l != Matrix.NoLand).ToHashSet();
        for (int id = 0; id < decomp.LandCount; id++)
        {
            var land = decomp.Land(id);
            var look = world.CoverOf(id);
            var layers = land.ReadTerrain()?.LayersOver(land.Heights);
            var seen = new HashSet<string>();
            for (int z = 0; z < LandData.Tiles; z++)
                for (int x = 0; x < LandData.Tiles; x++)
                {
                    int i = z * LandData.Tiles + x;
                    byte b = land.Behaviour(x, z);
                    bool solid = land.Solid(x, z);
                    tiles[b]++;
                    if (solid) blocked[b]++;
                    foreach (var area in AreasOf(id)) areasOf[b].Add(area.Key);
                    if (overworld.Contains(id)) overworldCover[look[i]] = overworldCover.GetValueOrDefault(look[i]) + 1;

                    foreach (string? name in new[] { layers?.Ground[i], layers?.Above[i] })
                    {
                        if (name == null) continue;
                        if (!textures.TryGetValue(name, out var use)) textures[name] = use = new TextureUse();
                        if (solid) use.Blocked++; else use.Open++;
                        if (seen.Add(name)) use.Chunks++;
                        use.Overworld |= overworld.Contains(id);
                    }
                }
            foreach (var prop in land.Props) propUse[prop.ModelId] = propUse.GetValueOrDefault(prop.ModelId) + 1;
        }

        for (int b = 0; b < 256; b++)
            if (tiles[b] > 0 && !TileBehaviors.IsKnown((byte)b))
                Problems.Add($"Behaviour 0x{b:X2} ({decomp.BehaviourNames.ElementAtOrDefault(b)}) is used by {tiles[b]} tiles but has no name in TileBehavior");

        // Every warp must lead to a warp that exists
        foreach (var header in decomp.Headers.Values)
            foreach (var warp in decomp.Events(header.Events).Warps)
            {
                if (warp.DestHeaderId == DecompMaps.DynamicHeader) continue;   // a lift: a script decides where it goes
                if (!decomp.Headers.TryGetValue(warp.DestHeaderId, out var target))
                    Problems.Add($"{header.Key}: a warp leads to {warp.DestHeaderId}, which is not an area");
                else if (warp.DestWarpId >= decomp.Events(target.Events).Warps.Count && warp.DestWarpId < 0xFF)
                    Problems.Add($"{header.Key}: a warp leads to warp {warp.DestWarpId} of {target.Key}, which has {decomp.Events(target.Events).Warps.Count}");
            }
    }

    private static string N(long n) => n.ToString("N0", CultureInfo.InvariantCulture);

    private string Where(int behaviour)
    {
        var names = areasOf[behaviour].Select(key => decomp.Headers.Values.First(h => h.Key == key)).OrderBy(h => h.Index).Select(renders.Name).Distinct().ToList();
        if (names.Count > 12) return $"{names.Count} places";
        return names.Count <= 4 ? string.Join(", ", names) : string.Join(", ", names.Take(3)) + $" and {names.Count - 3} more";
    }

    // ---------------------------------------------------------------- docs/tile-behaviours.md

    public string TileBehaviours()
    {
        var text = new StringBuilder();
        text.Append("""
            # Tile behaviours

            Every tile of the imported world carries two things from Platinum's own map data: a **behaviour** (a number from 0 to 255 saying what the tile does) and a **blocked** flag. This page lists every behaviour Sinnoh's maps use, with the name the game gives it (`TileBehavior` in `Overworld/TileBehavior.cs`), what it means, and where it occurs.

            `tools/MapImporter` writes this page: the counts come from all 666 chunks of the decompilation, the meanings from `TileBehaviors.Meaning`. To change a meaning, change it there and run the importer.

            ## How a tile is read

            - A chunk is 32 by 32 tiles; each tile is 16 bits. The low byte is the behaviour, the top bit is the blocked flag, and the seven bits between are never set.
            - **Blocked** tiles can't be walked onto. Most are plain (`None`): the tile data doesn't say whether a blocked tile is a tree, a fence or a cliff. Doors, ledges and rock-climb walls are blocked tiles whose behaviour lets a walker through in its own way.
            - **Height** is separate: each chunk lists rectangles of flat or sloping ground ("plates"). A walker can't step onto a tile whose ground is 1.25 tiles or more above or below where they stand, which is what makes a cliff edge a wall without any blocked tile. Under a bridge two plates overlap, and a walker stays on the one nearest their own height.
            - What a tile **looks like** (lawn, path, flowers, tree, rock face) is in neither: the importer reads it from the names of the textures the original drew there. See "World files" in `docs/data-files.md`.

            ## The values Sinnoh uses

            | Value | Name | What it is | Wild Pokémon | Surfable | Tiles | Blocked | Where |
            |---|---|---|---|---|---:|---:|---|

            """);

        for (int b = 0; b < 256; b++)
        {
            if (tiles[b] == 0) continue;
            var behaviour = (TileBehavior)b;
            string name = TileBehaviors.IsKnown((byte)b) ? behaviour.ToString() : "(unnamed)";
            text.Append(CultureInfo.InvariantCulture, $"| `0x{b:X2}` | `{name}` | {TileBehaviors.Meaning(behaviour)} | {(TileBehaviors.HasEncounters(behaviour) ? "yes" : "")} | {(TileBehaviors.IsSurfable(behaviour) ? "yes" : "")} | {N(tiles[b])} | {N(blocked[b])} | {(b == 0 ? "everywhere" : Where(b))} |\n");
        }

        int used = tiles.Count(t => t > 0);
        text.Append(CultureInfo.InvariantCulture, $"""

            {used} values are in use, on {N(tiles.Sum())} tiles; {N(blocked.Sum())} of those tiles are blocked.

            ## What the list shows

            - **Ledges** exist in three directions only: south (by far the most common), east and west. No map has a ledge jumped northward.
            - **Water** is three behaviours: `Sea` (which also covers lakes and ponds), `River` and `Waterfall`. A few water tiles are blocked: rocks standing in the sea.
            - **Wild Pokémon** appear on tall grass, very tall grass, cave floors, the Old Chateau's floors, marsh grass and water. `MountainFloor` has none of its own.
            - **Doors** are always blocked: a walker bumps into the door and the warp there takes them inside. The mats and openings (`Exit…`, `Entrance…`, `Stairs…`) are open tiles that take their warp when walked off in the right direction.
            - **Bridges** tell a walker which level they are on; the ground's height comes from the plates.
            - **Not understood yet**: `Unknown3C` to `Unknown3F`, `Unknown60`, `Unknown88`, `Unknown8E` and `Unknown8F`, {N(new[] { 0x3C, 0x3D, 0x3E, 0x3F, 0x60, 0x88, 0x8E, 0x8F }.Sum(b => tiles[b]))} tiles in all. The decompilation has no name for them either. Work out each from the place it occurs when that place is built.

            ## Sources

            The numbers and their order are Platinum's (`include/constants/field/map_tile_behaviors.h` in pret/pokeplatinum, where the names are the decompilation's); which behaviours have wild Pokémon or can be surfed is the table at the top of `src/map_tile_behavior.c`; the blocked bit and the 1.25-tile step are in `src/terrain_collision_manager.c`. The names and descriptions here are our own.

            """);
        return text.ToString().ReplaceLineEndings("\n");
    }

    // ---------------------------------------------------------------- the run's report

    public string Run(IEnumerable<(string Imported, string HandMade)> comparisons)
    {
        var text = new StringBuilder();
        var overworld = decomp.Matrix(0);
        text.Append(CultureInfo.InvariantCulture, $"""
            # Map import report

            Read from pret/pokeplatinum at `{DataImporter.Sources.DecompCommit}`.

            - {decomp.Headers.Count} areas, {decomp.MatrixCount} matrices, {decomp.LandCount} chunks, {N(decomp.LandCount * 1024L)} tiles
            - The overworld is {overworld.Width} by {overworld.Height} chunks: {overworld.Land.Count(l => l != Matrix.NoLand)} cells filled from {overworld.Land.Where(l => l != Matrix.NoLand).Distinct().Count()} different chunks, {overworld.Headers!.Distinct().Count()} areas
            - {N(propUse.Values.Sum())} props of {propUse.Count} models; {N(Enumerable.Range(0, decomp.LandCount).Sum(i => (long)decomp.Land(i).Heights.Plates.Count))} height plates
            - {N(decomp.Headers.Values.Select(h => h.Events).Distinct().Sum(e => (long)decomp.Events(e).Objects.Count))} people and objects, {N(decomp.Headers.Values.Select(h => h.Events).Distinct().Sum(e => (long)decomp.Events(e).Warps.Count))} warps, {N(decomp.Headers.Values.Select(h => h.Events).Distinct().Sum(e => (long)decomp.Events(e).Signs.Count))} signs, {N(decomp.Headers.Values.Select(h => h.Events).Distinct().Sum(e => (long)decomp.Events(e).Triggers.Count))} triggers


            """);

        text.Append("## What the overworld's tiles look like\n\n| Cover | Tiles | Share |\n|---|---:|---:|\n");
        long total = overworldCover.Values.Sum();
        foreach (var (cover, count) in overworldCover.OrderByDescending(kv => kv.Value))
            text.Append(CultureInfo.InvariantCulture, $"| {cover} | {N(count)} | {100.0 * count / total:F1}% |\n");

        text.Append(OpenAreas());

        if (comparisons.Any())
        {
            text.Append("\n## Compared with the hand-made maps that are left\n\n| Area | Tiles (imported, hand-made) | Tall grass | Ledges | Water | Buildings | Warps | People | Signs |\n|---|---|---|---|---|---|---|---|---|\n");
            foreach (var (imported, handMade) in comparisons) text.Append(Compare(imported, handMade));
        }

        var unknown = textures.Where(kv => { Cover.OfTexture(kv.Key, out bool known); return !known; }).ToList();
        text.Append(CultureInfo.InvariantCulture, $"\n## Textures\n\n{textures.Count} texture names lie over tiles; {textures.Count - unknown.Count} are sorted into a kind of ground by `Cover.cs`. Those still to sort, on the overworld first:\n\n| Texture | Open tiles | Blocked tiles | Chunks | Overworld |\n|---|---:|---:|---:|---|\n");
        foreach (var (name, use) in unknown.OrderByDescending(kv => kv.Value.Overworld).ThenByDescending(kv => kv.Value.Open + kv.Value.Blocked))
            text.Append(CultureInfo.InvariantCulture, $"| `{name}` | {N(use.Open)} | {N(use.Blocked)} | {use.Chunks} | {(use.Overworld ? "yes" : "")} |\n");

        text.Append("\nSorted so far:\n\n| Texture | Cover | Open tiles | Blocked tiles | Chunks |\n|---|---|---:|---:|---:|\n");
        foreach (var (name, use) in textures.Where(kv => !unknown.Any(u => u.Key == kv.Key)).OrderByDescending(kv => kv.Value.Open + kv.Value.Blocked))
            text.Append(CultureInfo.InvariantCulture, $"| `{name}` | {Cover.OfTexture(name, out _)?.ToString() ?? "(a shadow or a decal)"} | {N(use.Open)} | {N(use.Blocked)} | {use.Chunks} |\n");

        text.Append("\n## Props\n\nThe models placed on chunks, most used first. \"Building\" marks those large enough to be drawn as one.\n\n| Model | Name | Uses | Size in tiles (w × d × h) | Building |\n|---|---|---:|---|---|\n");
        foreach (var (model, uses) in propUse.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key))
        {
            var info = decomp.PropModel(model);
            string size = info == null ? "" : string.Create(CultureInfo.InvariantCulture, $"{info.BoxSize.X / 16:F1} × {info.BoxSize.Z / 16:F1} × {info.BoxSize.Y / 16:F1}");
            text.Append(CultureInfo.InvariantCulture, $"| {model} | `{info?.Name}` | {uses} | {size} | {(info != null && Cover.IsBuilding(info) ? "yes" : "")} |\n");
        }

        text.Append(CultureInfo.InvariantCulture, $"\n## Problems\n\n{(Problems.Count == 0 ? "None." : string.Join("\n", Problems.Select(p => "- " + p)))}\n");
        return text.ToString().ReplaceLineEndings("\n");
    }

    /// <summary>
    /// The areas the game has open (its <c>world.json</c>), with how much of what the original has in each is in
    /// the game: people appear once an overlay says who they are, a warp once its other side exists.
    /// </summary>
    private static string OpenAreas()
    {
        var text = new StringBuilder("\n## Open in the game\n\n| Area | Map | People (in the game, in the original) | Warps | Signs | Wild Pokémon | Doors still locked |\n|---|---|---|---|---|---|---|\n");
        foreach (var world in World.LoadAll())
        {
            foreach (string key in world.Index.Areas)
            {
                if (world.Area(key) is not { } area || world.MapOf(key) is not { } entry) continue;
                var map = MapDatabase.Get(entry.Name);
                bool Here(int x, int y) => string.Equals(map.AreaAt(x, y)?.Key, key, StringComparison.OrdinalIgnoreCase);

                // Signposts and mailboxes are objects in the original; here they are signs
                int signs = map.Signboards.Keys.Count(at => Here(at.X, at.Y));
                int originalSigns = area.Objects.Count(o => o.Looks.Contains("sign", StringComparison.Ordinal) || o.Looks == "mailbox");
                var species = area.Land?.Select(l => l.Species).Distinct().ToList() ?? new();
                text.Append(CultureInfo.InvariantCulture,
                    $"| {area.Name} (`{key}`) | {entry.Name} | {map.NPCs.Count(n => Here(n.GridX, n.GridY))}, {area.Objects.Count - originalSigns} | {map.Warps.Count(w => Here(w.SourceX, w.SourceY))}, {area.Warps.Count} | {signs}, {originalSigns + area.Signs.Count} | {(species.Count > 0 ? string.Join(", ", species) : "")} | {world.Overlay(key)?.Locked?.Count ?? 0} |\n");
            }
        }
        return text.ToString();
    }

    private string Compare(string importedKey, string handMadeName)
    {
        var header = decomp.Headers.Values.First(h => h.Key == importedKey);
        var matrix = decomp.Matrix(header.Matrix);
        var map = MapDatabase.Get(handMadeName);

        int tall = 0, ledges = 0, water = 0, buildings = 0, width = 0, height = 0;
        int minX = int.MaxValue, minZ = int.MaxValue, maxX = -1, maxZ = -1;
        for (int cy = 0; cy < matrix.Height; cy++)
            for (int cx = 0; cx < matrix.Width; cx++)
            {
                int landId = matrix.LandAt(cx, cy);
                if (landId == Matrix.NoLand || (matrix.Headers != null && matrix.HeaderAt(cx, cy) != header.Id)) continue;
                var land = decomp.Land(landId);
                buildings += land.Props.Count(p => decomp.PropModel(p.ModelId) is { } m && Cover.IsBuilding(m));
                for (int z = 0; z < LandData.Tiles; z++)
                    for (int x = 0; x < LandData.Tiles; x++)
                    {
                        var b = (TileBehavior)land.Behaviour(x, z);
                        if (b == TileBehavior.TallGrass) tall++;
                        if (b is TileBehavior.LedgeSouth or TileBehavior.LedgeEast or TileBehavior.LedgeWest) ledges++;
                        if (TileBehaviors.IsSurfable(b)) water++;
                        if (land.Solid(x, z) && b == TileBehavior.None) continue;
                        // The walkable part's extent says how big the place is; the chunks round it up to 32
                        minX = Math.Min(minX, cx * LandData.Tiles + x); maxX = Math.Max(maxX, cx * LandData.Tiles + x);
                        minZ = Math.Min(minZ, cy * LandData.Tiles + z); maxZ = Math.Max(maxZ, cy * LandData.Tiles + z);
                    }
            }
        if (maxX >= 0) { width = maxX - minX + 1; height = maxZ - minZ + 1; }

        int mapTall = 0, mapLedges = 0, mapWater = 0;
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                var tile = map.GetGroundTile(x, y);
                if (tile == TileType.TallGrass) mapTall++;
                if (tile == TileType.LedgeDown) mapLedges++;
                if (tile == TileType.Water) mapWater++;
            }

        var events = decomp.Events(header.Events);
        return string.Create(CultureInfo.InvariantCulture,
            $"| {renders.Name(header)} | {width} × {height}, {map.Width} × {map.Height} | {tall}, {mapTall} | {ledges}, {mapLedges} | {water}, {mapWater} | {buildings}, {MapStructures.FindBuildings(map).Count} | {events.Warps.Count}, {map.Warps.Count} | {events.Objects.Count}, {map.NPCs.Count} | {events.Signs.Count}, {map.Signboards.Count} |\n");
    }
}
