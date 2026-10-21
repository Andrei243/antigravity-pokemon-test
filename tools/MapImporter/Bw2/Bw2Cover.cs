using System.Numerics;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace MapImporter.Bw2;

/// <summary>
/// Cover's third list (plan 22 · U1): Black 2's names for its textures, in <see cref="Cover"/>'s order (what says
/// nothing of the ground first, then what stands, then the ground), looked at before Platinum's, whose names the
/// same studio's habits often share (<c>gake</c> a cliff, <c>ki</c> a tree, <c>michi</c> a path). These are the first
/// rows. U1 judged them from the names alone; since plan 22 · U2 (the user's decision of 2026-10-20) the reader reads
/// each chunk glTF's binary buffer as plain numbers, the positions of its triangles and nothing else, only to find which
/// texture's name lies over each tile (<see cref="Bw2Geometry"/>), so a tile's look is its behaviour's where that says
/// one and its texture's otherwise (<see cref="OfChunk"/>), and only the <see cref="TerrainCover"/> is kept. The report
/// lists the textures no rule sorts, by the tiles they lie over: the session that opens an area adds to these rows.
/// </summary>
public static class Bw2Cover
{
    // First match wins; a null cover says nothing about the ground (a shadow, the battle's own backdrop)
    private static readonly (Regex Pattern, TerrainCover? Cover)[] Rules = new (string, TerrainCover?)[]
    {
        ("kage|shadow|battle$", null),                          // shadows over the ground, the backdrop baked for battles
        // (plan 22 · U2: from here on judged from where each texture lies, the report's list by tiles)
        ("yogore|_air$|hibi|shita\\d|_obj\\d|_etc\\d|_dgs", null),   // stains, the air over a roof, cracks, what lies under, odds and ends
        ("^gake_michi", TerrainCover.Path),                     // the path along the foot of a cliff
        ("^taki|^sea_zanami|^sea_simi|^shore|^ike\\d|^mizu|kawa|gawa|^sea_|nagare|asase|_zan\\d|mizo|suimon|foun", TerrainCover.Water),   // falls, surf, ponds, rivers, shallows, the sea, ditches, sluices, fountains
        ("marsh", TerrainCover.Marsh),
        ("gake|^ga_s|dansa|gak\\d|futa|wall", TerrainCover.Cliff),   // cliffs, the sea's cliffs, their caps (futa) and steps of rock, walls of rock
        ("^\\d*ki\\d|_ki\\d|^mori|plant", TerrainCover.Tree),    // the trees of every kind (ki02, ki03, the battle's 54ki, C12_ki02), woods, plants
        ("^iwa|_iwa|ishi|isi\\d", TerrainCover.Boulder),          // rocks and stones
        ("hana|kadan|_fl\\d", TerrainCover.Flowers),             // flowers and flower beds
        ("nagakusa", TerrainCover.TallGrass),                   // the long grass
        ("kusa|grass|grs\\d", TerrainCover.Grass),               // the lawn
        ("yuki|yuk\\d", TerrainCover.Snow),
        ("sabaku", TerrainCover.Sand),
        ("doukutu|_dou$", TerrainCover.CaveFloor),
        ("hashi|hasi|sanbashi|dekki|wd\\d", TerrainCover.Bridge),     // bridges, piers, decks of wood
        ("kaidan|stair|_dan\\d*$", TerrainCover.Steps),          // stairs and steps
        ("saku|fence|tesuri|tesu\\d|hata|_fe\\d|stake|paipu|kabe|gate|post|bench|isu", TerrainCover.Fence),   // fences, railings, flags, stakes, pipes, low walls, posts, benches and seats
        ("lamp|hikari|ight|poal", TerrainCover.Lamp),           // lamps and poles
        ("tent|crane", TerrainCover.Building),                  // a market's awnings, a harbour's cranes
        ("_ana$|_ana\\d", TerrainCover.CaveMouth),               // a hole into the ground
        ("michi|miti|mic\\d|^kemono|hage|dirt|jimen|ochiba|saien|haizai|kiwa", TerrainCover.Path),   // paths, tracks, bare earth, fallen leaves, a vegetable plot, rubble, a bank
        ("yuka|_gai|gai\\d|dodai|tile|hway|strip|park|cafe|_yu\\d|saka|_cp\\d|mrkt", TerrainCover.Paving),   // floors, streets, plinths, tiles, roads, a park's, a café's, slopes, a market's
    }.Select(r => (new Regex(r.Item1, RegexOptions.Compiled | RegexOptions.IgnoreCase), r.Item2)).ToArray();

    /// <summary>The kind of ground a Black 2 texture's name stands for; <paramref name="known"/> false for a name neither list covers.</summary>
    public static TerrainCover? OfTexture(string? name, out bool known)
    {
        known = true;
        if (string.IsNullOrEmpty(name)) return null;
        foreach (var (pattern, cover) in Rules)
            if (pattern.IsMatch(name)) return cover;
        return Cover.OfTexture(name, out known);
    }

    /// <summary>What a behaviour says about the look of its tile: Platinum's rules, and Black 2's own values.</summary>
    public static TerrainCover? OfBehaviour(TileBehavior behaviour) => Cover.OfBehaviour(behaviour) ?? behaviour switch
    {
        TileBehavior.DarkGrass or TileBehavior.VeryTallDarkGrass or TileBehavior.GrottoGrass => TerrainCover.TallGrass,
        TileBehavior.DeepSand or TileBehavior.Quicksand => TerrainCover.Sand,
        TileBehavior.ChargedFloor => TerrainCover.CaveFloor,
        TileBehavior.SurfEdge or TileBehavior.CurrentEast or TileBehavior.CurrentWest or TileBehavior.CurrentNorth
            or TileBehavior.CurrentSouth => TerrainCover.Water,
        TileBehavior.Catwalk => TerrainCover.Bridge,
        _ => null
    };

    /// <summary>A tile's look from its behaviour alone, or <see cref="TerrainCover.Unknown"/>: what a chunk with no model comes to.</summary>
    public static TerrainCover OfTile(TileBehavior behaviour) => OfBehaviour(behaviour) ?? TerrainCover.Unknown;

    // What stands rather than lies: a blocked tile shows it, an open one looks past it to its ground
    private static bool Stands(TerrainCover cover) =>
        cover is TerrainCover.Tree or TerrainCover.Broadleaf or TerrainCover.Cliff or TerrainCover.Boulder or TerrainCover.Fence or TerrainCover.Lamp
            or TerrainCover.Building;

    /// <summary>Whether a prop's box is a building's: wide, deep and tall enough, and not a sheet the size of the chunk (a sky).</summary>
    public static bool IsBuilding(Vector3 size) =>
        size.X / LandData.TileUnits >= 2.5f && size.Z / LandData.TileUnits >= 2f && size.Y >= 20f
        && size.X / LandData.TileUnits < LandData.Tiles - 1 && size.Z / LandData.TileUnits < LandData.Tiles - 1;

    /// <summary>
    /// The look of every tile of a chunk, row by row (plan 22 · U2): what its glTF's triangles lay over each tile's
    /// middle (<see cref="Bw2Geometry.LayersOver"/>, the ground on the walking surface of the chunk's terrain grids and
    /// what rises above it) sorted by the textures' names, in <see cref="Cover.Of"/>'s order: a blocked tile under a
    /// building's box is the building's, a behaviour that says how the tile looks says it, a blocked tile shows what
    /// stands on it and an open one its ground. <paramref name="unsorted"/>, when given, counts the tiles whose
    /// texture no rule sorts, by the texture's name.
    /// </summary>
    public static TerrainCover[] OfChunk(Bw2Maps maps, string chunk, Dictionary<string, int>? unsorted = null)
    {
        var layers = maps.Terrain(chunk);
        if (layers is not [{ } land, ..]) return Array.Empty<TerrainCover>();
        int w = land.Width, h = land.Height;
        var result = new TerrainCover[w * h];
        var triangles = maps.Geometry(chunk);
        float? Surface(int x, int z, float height)
        {
            float? best = null;
            foreach (var level in layers)
            {
                if (level == null || x >= level.Width || z >= level.Height) continue;
                float u = level.At(x, z).Height * LandData.TileUnits;
                if (best == null || MathF.Abs(u - height) < MathF.Abs(best.Value - height)) best = u;
            }
            return best;
        }
        var over = triangles.Count > 0 ? Bw2Geometry.LayersOver(triangles, w, h, Surface) : null;

        // Tiles under a building's box (a room has none: its props are its furniture, numbered as the rooms' are)
        var buildings = new bool[w * h];
        if (!maps.IsInterior(chunk) && maps.Model(chunk) is { } model)
            foreach (var prop in model.Props)
            {
                if (maps.PropBox(prop.Resource) is not { } box || !IsBuilding(box.Max - box.Min)) continue;
                var (x0, z0, x1, z1) = Footprint(prop, box, w, h);
                for (int z = Math.Max(0, (int)MathF.Floor(z0)); z < h && z + 0.5f < z1; z++)
                    for (int x = Math.Max(0, (int)MathF.Floor(x0)); x < w && x + 0.5f < x1; x++)
                        if (x + 0.5f > x0 && z + 0.5f > z0) buildings[z * w + x] = true;
            }

        TerrainCover? Sort(string? name)
        {
            var cover = OfTexture(name, out bool known);
            if (!known && name != null && unsorted != null) unsorted[name] = unsorted.GetValueOrDefault(name) + 1;
            return cover;
        }

        for (int z = 0; z < h; z++)
            for (int x = 0; x < w; x++)
            {
                int i = z * w + x;
                var tile = land.At(x, z);
                bool solid = tile.Blocked;
                if (solid && buildings[i]) { result[i] = TerrainCover.Building; continue; }
                var ground = Sort(over?.Ground[i]);
                var above = Sort(over?.Above[i]);
                if (OfBehaviour(Bw2Behaviours.Of(tile.Class)) is { } fromBehaviour) { result[i] = fromBehaviour; continue; }
                if (!solid && above == TerrainCover.Flowers && (ground == null || ground == TerrainCover.Grass)) { result[i] = TerrainCover.Flowers; continue; }
                TerrainCover? pick = solid
                    ? (above is { } a && Stands(a) ? a : ground ?? above)
                    : (ground is { } g && !Stands(g) ? g : above is { } b && !Stands(b) ? b : null);
                result[i] = pick ?? TerrainCover.Unknown;
            }
        return result;
    }

    /// <summary>
    /// The rectangle a prop's box takes on its chunk, in tiles from the chunk's north-west corner: the box turned by
    /// the prop's quarter turns about its origin, then moved to its place.
    /// </summary>
    public static (float X0, float Z0, float X1, float Z1) Footprint(Bw2Prop prop, (Vector3 Min, Vector3 Max) box, int width, int depth)
    {
        int quarter = (int)MathF.Round(prop.Turn / 90f) & 3;
        var (minX, maxX, minZ, maxZ) = quarter switch
        {
            // A turn about the up axis by 90° takes x to -z and z to x (glTF's right-handed turn, seen from above)
            1 => (box.Min.Z, box.Max.Z, -box.Max.X, -box.Min.X),
            2 => (-box.Max.X, -box.Min.X, -box.Max.Z, -box.Min.Z),
            3 => (-box.Max.Z, -box.Min.Z, box.Min.X, box.Max.X),
            _ => (box.Min.X, box.Max.X, box.Min.Z, box.Max.Z)
        };
        float halfX = width * LandData.TileUnits / 2f, halfZ = depth * LandData.TileUnits / 2f;
        return ((prop.Position.X + minX + halfX) / LandData.TileUnits, (prop.Position.Z + minZ + halfZ) / LandData.TileUnits,
            (prop.Position.X + maxX + halfX) / LandData.TileUnits, (prop.Position.Z + maxZ + halfZ) / LandData.TileUnits);
    }
}
