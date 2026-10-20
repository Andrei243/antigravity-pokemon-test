using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Data;

namespace MapImporter.Hg;

/// <summary>
/// Cover's second list (plan 20 · J1): HeartGold's names for its textures, looked at before Platinum's
/// (<see cref="Cover.OfTexture"/>), which many of HGSS's names share since the same team named them. It holds only
/// what the overworld's picture needed; a region session adds what an area it opens asks for, from the report's list.
/// </summary>
public static class HgCover
{
    // First match wins; a null cover says nothing about the ground (a shadow, a decal)
    // Judged from the names and from where each lies on the overworld's picture by behaviour (the report's counts),
    // never from a picture of the original: a region session corrects any it finds wrong for the area it opens
    private static readonly (Regex Pattern, TerrainCover? Cover)[] Rules = new (string, TerrainCover?)[]
    {
        ("^ymb_hole", TerrainCover.CaveMouth),                    // the dark in a cave's mouth
        // The mouths of Mt. Mortar, the Ice Path and the Dragon's Den in the north's cliffs (plan 20 · J10)
        ("^cavehole|^icehole", TerrainCover.CaveMouth),
        ("^cavekn", TerrainCover.CaveMouth),                      // Tohjo Falls' two mouths on Route 27 (plan 20 · J10, part b)
        ("^etc_|^h_kage|^a18_shadow", null),                     // odds and ends drawn over the ground, shadows
        // The Safari Zone's areas (plan 20 · J8, part b), judged by `--unknown`'s counts of open and blocked tiles:
        // the Meadow's and the Forest's ground, the Peak's and the Mountain's tracks and faces, the Rocky Beach's sand
        // and rocks, the Wetland's banks, the Swamp's mud, the Wasteland's broken rock
        ("^ms02_|^ms04_0[123]|^ms09_|^ms07_", TerrainCover.Grass),
        ("^ms08_", TerrainCover.Marsh),
        ("^ms05_0[145]|^ms10_0[25]|^ms11_", TerrainCover.Path),
        ("^ms06_0[367]", TerrainCover.Sand),
        ("^ms10_01", TerrainCover.Rock),
        ("^ms05_|^ms06_|^ms04_|^ms10_", TerrainCover.Cliff),
        ("^msg_\\d", TerrainCover.Building),                      // the zone's gate, drawn into the ground behind its door
        ("^overpass|_br$|_br\\d", TerrainCover.Bridge),
        ("^sea_|^pond_|^river|_wf\\d", TerrainCover.Water),       // the sea, ponds and their edges, rivers, waterfalls
        ("^flower", TerrainCover.Flowers),
        ("^wall\\d|^wallkn|^cl_bg|^r_climb", TerrainCover.Cliff), // the walls of rock between the areas, and their faces
        // Route 22's open grass by the League's gate (plan 20 · J10, part b): every tile of it walked, none blocked
        ("^tok_fe01", TerrainCover.Grass),
        ("_fe\\d|fence|^senro|_board$", TerrainCover.Fence),      // each town's fences, the railway's line, signboards
        ("_rd\\d|^ko_land|^ko_h\\d|^d22_|^area18_", TerrainCover.Path),
        ("_yuka|_fuch|^ar_|_teien$|^kk_|_shouka$", TerrainCover.Paving),   // floors, kerbs, the ruins' stone, gardens
        ("^fieldkk|_gbb\\d", TerrainCover.Grass)
    }.Select(r => (new Regex(r.Item1, RegexOptions.Compiled), r.Item2)).ToArray();

    /// <summary>The kind of ground a HeartGold texture stands for; <paramref name="known"/> false for a name neither list covers.</summary>
    public static TerrainCover? OfTexture(string? name, out bool known)
    {
        known = true;
        if (string.IsNullOrEmpty(name)) return null;
        foreach (var (pattern, cover) in Rules)
            if (pattern.IsMatch(name)) return cover;
        return Cover.OfTexture(name, out known);
    }

    /// <summary>The look of every tile of one of HeartGold's chunks, by <see cref="Cover.Of(LandData, Func{int, ModelInfo?}, Func{string?, TerrainCover?}, Func{byte, PokemonPlatinumEngine.Overworld.TileBehavior}?)"/>'s rules.</summary>
    public static TerrainCover[] Of(LandData land, Func<int, ModelInfo?> model, byte[] behaviours) =>
        Cover.Of(land, model, name => OfTexture(name, out _), b => (PokemonPlatinumEngine.Overworld.TileBehavior)behaviours[b]);
}
