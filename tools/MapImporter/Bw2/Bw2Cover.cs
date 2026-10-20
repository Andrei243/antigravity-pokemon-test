using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace MapImporter.Bw2;

/// <summary>
/// Cover's third list (plan 22 · U1): Black 2's names for its textures, in <see cref="Cover"/>'s order (what says
/// nothing of the ground first, then what stands, then the ground), looked at before Platinum's, whose names the
/// same studio's habits often share (<c>gake</c> a cliff, <c>ki</c> a tree, <c>michi</c> a path). These are the first
/// rows, judged from the names and their counts on the overworld alone: the session that opens an area corrects and
/// adds to them from the report's list. Where a texture lies is in the glTF's binary buffer, which the reader never
/// reads (the plan's rule), so a tile's look comes from its behaviour (<see cref="OfTile"/>) and a texture's from its
/// name only, counted by the chunks that use it.
/// </summary>
public static class Bw2Cover
{
    // First match wins; a null cover says nothing about the ground (a shadow, the battle's own backdrop)
    private static readonly (Regex Pattern, TerrainCover? Cover)[] Rules = new (string, TerrainCover?)[]
    {
        ("kage|shadow|battle$", null),                          // shadows over the ground, the backdrop baked for battles
        ("^gake_michi", TerrainCover.Path),                     // the path along the foot of a cliff
        ("^taki|^sea_zanami|^sea_simi|^shore|^ike\\d|^mizu|^kawa|^sea_|nagare", TerrainCover.Water),   // falls, surf, ponds, rivers, the sea
        ("gake|^ga_s|dansa|gak\\d", TerrainCover.Cliff),         // cliffs, the sea's cliffs, their caps (futa) and steps of rock
        ("^\\d*ki\\d|^mori|^plant", TerrainCover.Tree),          // the trees of every kind (ki02, ki03, the battle's 54ki), woods
        ("^iwa|_iwa", TerrainCover.Boulder),
        ("hana", TerrainCover.Flowers),
        ("^kusa", TerrainCover.TallGrass),
        ("grass|grs\\d", TerrainCover.Grass),
        ("yuki", TerrainCover.Snow),
        ("sabaku", TerrainCover.Sand),
        ("doukutu", TerrainCover.CaveFloor),
        ("hashi|hasi|sanbashi|dekki", TerrainCover.Bridge),     // bridges, piers, decks
        ("kaidan", TerrainCover.Steps),
        ("saku|fence|tesuri|hata", TerrainCover.Fence),         // fences, railings, flags
        ("lamp|hikari|ight", TerrainCover.Lamp),
        ("michi|^kemono|hage", TerrainCover.Path),              // paths, the beasts' tracks, bare ground
        ("yuka|_gai|dodai", TerrainCover.Paving),               // floors, streets, plinths
    }.Select(r => (new Regex(r.Item1, RegexOptions.Compiled), r.Item2)).ToArray();

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

    /// <summary>A tile's look from what the reader has of it: its behaviour, or <see cref="TerrainCover.Unknown"/>.</summary>
    public static TerrainCover OfTile(TileBehavior behaviour) => OfBehaviour(behaviour) ?? TerrainCover.Unknown;
}
