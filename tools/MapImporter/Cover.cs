using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace MapImporter;

/// <summary>
/// Decides what each tile looks like. The tile's behaviour settles it where it can (water, tall grass, snow);
/// elsewhere the name of the texture the original drew there does: the names are the map makers' shorthand
/// (<c>ngrass</c>, <c>nsand</c>, <c>tree01</c>, <c>criff</c>), and a short list of patterns sorts them into our own
/// kinds of ground. A name no pattern knows leaves the tile <see cref="TerrainCover.Unknown"/> and is listed in the
/// importer's report, to be added here when its area is built.
/// </summary>
public static class Cover
{
    // First match wins. A null cover means the texture says nothing about the ground (a shadow, a decal).
    private static readonly (Regex Pattern, TerrainCover? Cover)[] Rules = new (string, TerrainCover?)[]
    {
        ("^dhole", TerrainCover.CaveMouth),                      // the dark in the mouth of a cave ("dungeon hole")
        ("^fenter", TerrainCover.ForestMouth),                   // the dark under the trees where a forest is entered
        ("^dun_jump", TerrainCover.Steps),                       // the run up to a Bicycle's ramp
        ("shadow|kage", null),                                   // shadows painted over the ground
        ("^puddle", null),                                       // the behaviour already says puddle
        // The Battle Frontier's grounds (plan 01 · M10, part b): its causeways over the pools and their parapets, its
        // lawns, the floor before its gate and the emblem set into the plaza, its walls, its planting; and what
        // stands on the plaza with no model of its own (the Exchange Service Corner's booths, the two statues, the
        // model of the Frontier), whose tiles show the ground round them. The gate's canopy says nothing of the ground
        ("^bf_hashi02", TerrainCover.Fence),
        ("^bf_hashi|^flobby_yuka|^bf_symbol", TerrainCover.Paving),
        ("^bf_shiba", TerrainCover.Grass),
        ("^bf_hei|^flobby_kabe", TerrainCover.Fence),
        ("^bf_ueki", TerrainCover.Broadleaf),
        ("^bf_shop|^bf_zou|^bf_mini", TerrainCover.Building),
        ("^bf_yane|^bf_hata", null),
        ("tree3|^bf_tree", TerrainCover.Broadleaf),              // the Battle Zone's forests
        ("tree|^plant\\d|^imped$", TerrainCover.Tree),           // trees, forest fill and the dark floor under it
        ("^wcliff$|^enccliff$", TerrainCover.Rock),              // rock that is walked on
        ("criff|cllif|cliff|peak", TerrainCover.Cliff),
        ("rock|_iwa|meteo", TerrainCover.Boulder),
        ("^sea$|^asasea$|lake", TerrainCover.Water),
        ("^nectgr$", TerrainCover.TallGrass),
        ("hana|_fl_[a-z]\\.", TerrainCover.Flowers),             // nhana, shana, rhana, Floaroma's beds
        ("grass|green|_fl_g$", TerrainCover.Grass),
        ("beach|hamabe", TerrainCover.Sand),
        ("snow|sonw", TerrainCover.Snow),
        ("_ice", TerrainCover.Ice),
        ("^numa_", TerrainCover.Marsh),
        ("^a8_sora_[ah]$", TerrainCover.Walkway),                // the decks of Sunyshore's walkways
        ("^a8_sora_", TerrainCover.Fence),                       // and their rails
        ("bridge", TerrainCover.Bridge),
        ("step|slope", TerrainCover.Steps),
        ("^nsand|^blueglay|^road|^hage$", TerrainCover.Path),
        ("_road|^c\\d+_r\\d|_cy\\d|_base|_g\\d$|_grand$|kado$", TerrainCover.Paving),
        ("^dun08_chip|^colum_", TerrainCover.Paving),            // Spear Pillar's ancient stone floor, and its columns' feet
        ("^c\\d+_lamp|^c\\d+_light|^bf_light", TerrainCover.Lamp),      // the street lamps of the cities
        ("hanger|rale|stop$|lamp|light|^c\\d+_f_|^c\\d+_d_|_pol\\d|_hei_|_gate|^c1_o02$", TerrainCover.Fence)
    }.Select(r => (new Regex(r.Item1, RegexOptions.Compiled), r.Item2)).ToArray();

    /// <summary>The kind of ground a texture name stands for; <paramref name="known"/> is false for a name no rule covers.</summary>
    public static TerrainCover? OfTexture(string? name, out bool known)
    {
        known = true;
        if (string.IsNullOrEmpty(name)) return null;
        foreach (var (pattern, cover) in Rules)
            if (pattern.IsMatch(name)) return cover;
        known = false;
        return null;
    }

    /// <summary>What a behaviour says about the look of its tile, when it says anything.</summary>
    public static TerrainCover? OfBehaviour(TileBehavior behaviour) => behaviour switch
    {
        TileBehavior.TallGrass or TileBehavior.VeryTallGrass => TerrainCover.TallGrass,
        TileBehavior.CaveFloor => TerrainCover.CaveFloor,
        TileBehavior.River or TileBehavior.Waterfall or TileBehavior.Sea or TileBehavior.ShallowWater => TerrainCover.Water,
        TileBehavior.Ice => TerrainCover.Ice,
        TileBehavior.Sand => TerrainCover.Sand,
        TileBehavior.DeepSnow or TileBehavior.DeeperSnow or TileBehavior.DeepestSnow or TileBehavior.ShallowSnow or TileBehavior.ShadedSnow => TerrainCover.Snow,
        TileBehavior.Mud or TileBehavior.DeepMud or TileBehavior.MarshGrass or TileBehavior.DeepMarshGrass => TerrainCover.Marsh,
        // A puddle is a look of its own, whatever ground its texture shows (plan 01 · M7)
        TileBehavior.Puddle or TileBehavior.StillPuddle => TerrainCover.Puddle,
        TileBehavior.Bridge or TileBehavior.BridgeEnd or TileBehavior.BridgeOverCave or TileBehavior.BridgeOverWater or TileBehavior.BridgeOverSnow
            or (>= TileBehavior.BikeBridgeNorthSouth and <= TileBehavior.BikeBridgeEastWestOverSand) => TerrainCover.Bridge,
        _ => null
    };

    // Building: something built that a texture names where no model stands (the Battle Frontier's booths and statues)
    private static bool Stands(TerrainCover cover) =>
        cover is TerrainCover.Tree or TerrainCover.Broadleaf or TerrainCover.Cliff or TerrainCover.Boulder or TerrainCover.Fence or TerrainCover.Lamp
            or TerrainCover.Building;

    /// <summary>The look of every tile of a chunk, row by row.</summary>
    public static TerrainCover[] Of(LandData land, Func<int, ModelInfo?> propModel) => Of(land, propModel, name => OfTexture(name, out _));

    /// <summary>
    /// The same with another game's names for its textures (plan 20 · J1: HeartGold's rules, <c>Hg/HgCover.cs</c>)
    /// and its behaviours read as this game's (<paramref name="behaviour"/>, the tile's byte to a <see cref="TileBehavior"/>).
    /// </summary>
    public static TerrainCover[] Of(LandData land, Func<int, ModelInfo?> propModel, Func<string?, TerrainCover?> texture, Func<byte, TileBehavior>? behaviour = null)
    {
        var result = new TerrainCover[LandData.Tiles * LandData.Tiles];
        var layers = land.ReadTerrain()?.LayersOver(land.Heights);
        var buildings = BuildingTiles(land, propModel);

        for (int z = 0; z < LandData.Tiles; z++)
            for (int x = 0; x < LandData.Tiles; x++)
            {
                int i = z * LandData.Tiles + x;
                bool solid = land.Solid(x, z);
                if (solid && buildings[i]) { result[i] = TerrainCover.Building; continue; }

                var ground = texture(layers?.Ground[i]);
                var above = texture(layers?.Above[i]);
                // A walkway's deck is a bridge by its behaviour; its own look is told by its texture
                if (!solid && (ground == TerrainCover.Walkway || above == TerrainCover.Walkway)) { result[i] = TerrainCover.Walkway; continue; }
                // The mouth of a cave is its dark, whether the tile is the hole in the rock or the way in
                if (ground == TerrainCover.CaveMouth || above == TerrainCover.CaveMouth) { result[i] = TerrainCover.CaveMouth; continue; }
                // And so is the way into a forest
                if (ground == TerrainCover.ForestMouth || above == TerrainCover.ForestMouth) { result[i] = TerrainCover.ForestMouth; continue; }
                if (OfBehaviour(behaviour?.Invoke(land.Behaviour(x, z)) ?? (TileBehavior)land.Behaviour(x, z)) is { } fromBehaviour) { result[i] = fromBehaviour; continue; }
                // Flowers stand over the lawn they grow in (Floaroma Meadow is a lawn with a sheet of flowers
                // over every tile of it): the lawn is the ground, the flowers are what is seen
                if (!solid && above == TerrainCover.Flowers && (ground == null || ground == TerrainCover.Grass)) { result[i] = TerrainCover.Flowers; continue; }
                // A blocked tile shows what stands on it; an open one its ground, whatever hangs over it
                TerrainCover? pick = solid
                    ? (above is { } a && Stands(a) ? a : ground ?? above)
                    : (ground is { } g && !Stands(g) ? g : above is { } b && !Stands(b) ? b : null);
                result[i] = pick ?? TerrainCover.Unknown;
            }
        return result;
    }

    /// <summary>Tiles under the box of a prop as large as a building.</summary>
    public static bool[] BuildingTiles(LandData land, Func<int, ModelInfo?> propModel)
    {
        var tiles = new bool[LandData.Tiles * LandData.Tiles];
        foreach (var prop in land.Props)
        {
            if (propModel(prop.ModelId) is not { } model || !IsBuilding(model)) continue;
            float x0 = prop.TileX + model.BoxMin.X / LandData.TileUnits, z0 = prop.TileZ + model.BoxMin.Z / LandData.TileUnits;
            float x1 = x0 + model.BoxSize.X / LandData.TileUnits, z1 = z0 + model.BoxSize.Z / LandData.TileUnits;
            for (int z = Math.Max(0, (int)MathF.Floor(z0)); z < LandData.Tiles && z + 0.5f < z1; z++)
                for (int x = Math.Max(0, (int)MathF.Floor(x0)); x < LandData.Tiles && x + 0.5f < x1; x++)
                    if (x + 0.5f > x0 && z + 0.5f > z0) tiles[z * LandData.Tiles + x] = true;
        }
        return tiles;
    }

    /// <summary>Wide, deep and tall enough to be a building, and not a sheet the size of the whole chunk.</summary>
    public static bool IsBuilding(ModelInfo model)
    {
        float w = model.BoxSize.X / LandData.TileUnits, d = model.BoxSize.Z / LandData.TileUnits;
        return w >= 2.5f && d >= 2f && model.BoxSize.Y >= 20f && !(w > LandData.Tiles - 1 && d > LandData.Tiles - 1);
    }
}
