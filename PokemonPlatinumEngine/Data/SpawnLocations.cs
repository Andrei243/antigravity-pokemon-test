using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// One of the places a trainer flies to and comes back to (plan 02 · S2): a town's Pokémon Center (Twinleaf's is
/// the player's house), the tile of the map of Sinnoh in front of it where Fly and Teleport land, the room whose
/// door sets it as the place to come back to, and whether arriving in the town is what lets one fly there.
/// </summary>
public sealed record SpawnLocation(int Id, string Area, int X, int Y, string Room, bool UnlockedOnArrival = true, string? Flag = null, string Map = "Sinnoh")
{
    /// <summary>
    /// The flag that first arriving in the town sets, which lets one fly there: <c>FLAG_FIRST_ARRIVAL_ETERNA_CITY</c>.
    /// The League's two are set by its Pokémon Centers' own scripts, as the original's are.
    /// </summary>
    public string ArrivalFlag => Flag ?? "FLAG_FIRST_ARRIVAL_" + Area.ToUpperInvariant();
}

/// <summary>
/// The original's spawn locations (<c>sSpawnLocations</c> in <c>src/spawn_locations.c</c>), numbered from 1 as it
/// numbers them: where Fly lands in each town, and the Pokémon Center a trainer last went into, which Teleport
/// takes them back to. Only the numbers and the places are the original's.
/// </summary>
public static class SpawnLocations
{
    /// <summary>
    /// The variable that keeps the last place come back to, by its number: the original's blackout warp
    /// (<c>FieldOverworldState_GetBlackOutWarpId</c>), set on going into a Pokémon Center. Nought is Twinleaf Town.
    /// </summary>
    public const string Variable = "VAR_SPAWN_LOCATION";

    /// <summary>A region's spawn locations as its file lists them (plan 18 · W1), numbered as the file numbers them.</summary>
    public static IReadOnlyList<SpawnLocation> Of(Region region) =>
        (region.File?.Spawns ?? new()).Select(p => new SpawnLocation(p.Id, p.Area, p.X, p.Y, p.Room, p.UnlockedOnArrival, p.Flag, p.Map ?? region.OverworldMap ?? "Sinnoh")).ToList();

    /// <summary>
    /// Sinnoh's twenty, from <c>regions/sinnoh.json</c>: the places the members with no region speak of. Fly's choice
    /// and the place to come back to are region by region (<see cref="In"/>, <see cref="VariableOf"/>, plan 18 · W9).
    /// </summary>
    public static readonly IReadOnlyList<SpawnLocation> All = Of(RegionDatabase.Get(RegionDatabase.Sinnoh)!);

    private static readonly Dictionary<string, IReadOnlyList<SpawnLocation>> byRegion = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// A region's spawn locations, by the region's id (plan 20 · J9: Fly within Johto). Sinnoh's are <see cref="All"/>;
    /// an unknown region has none. Only Fly and a first arrival read another region's: the place to come back to
    /// (<see cref="Variable"/>, Teleport, a white-out) stays Sinnoh's until plan 18 · W9.
    /// </summary>
    public static IReadOnlyList<SpawnLocation> In(string? regionId)
    {
        if (regionId == null || string.Equals(regionId, RegionDatabase.Sinnoh, StringComparison.OrdinalIgnoreCase)) return All;
        lock (byRegion)
        {
            if (!byRegion.TryGetValue(regionId, out var list))
                byRegion[regionId] = list = RegionDatabase.Get(regionId) is { } region ? OfWorld(region) : Array.Empty<SpawnLocation>();
            return list;
        }
    }

    /// <summary>
    /// A region's file's spawns, and after them those its world folder's <c>spawns.json</c> adds (a GBA import writes
    /// one, plan 18 · W7: Kanto's heal locations), the file's own winning where both number one place.
    /// </summary>
    private static IReadOnlyList<SpawnLocation> OfWorld(Region region)
    {
        var list = Of(region).ToList();
        string path = System.IO.Path.Combine(World.Folder, region.Folder, "spawns.json");
        if (System.IO.File.Exists(GameDataFiles.PathOf(path)))
            foreach (var p in GameDataFiles.Load<List<RegionSpawn>>(path))
                if (list.All(s => s.Id != p.Id))
                    list.Add(new SpawnLocation(p.Id, p.Area, p.X, p.Y, p.Room, p.UnlockedOnArrival, p.Flag, p.Map ?? region.OverworldMap ?? "Sinnoh"));
        return list.OrderBy(s => s.Id).ToList();
    }

    public static SpawnLocation? Get(int id) => id >= 1 && id <= All.Count ? All[id - 1] : null;

    /// <summary>A spawn location of a region by its number there.</summary>
    public static SpawnLocation? Get(string? regionId, int id) => In(regionId).FirstOrDefault(s => s.Id == id);

    /// <summary>The town whose first arrival is noted on coming into an area, if it is one (<c>TryUnlockFlyLocationByMap</c>).</summary>
    public static SpawnLocation? ArrivedIn(string areaKey) => All.FirstOrDefault(s => s.Area == areaKey && s.UnlockedOnArrival);

    /// <summary>The same in a region of its own (plan 20 · J9).</summary>
    public static SpawnLocation? ArrivedIn(string? regionId, string areaKey) => In(regionId).FirstOrDefault(s => s.Area == areaKey && s.UnlockedOnArrival);

    /// <summary>The place a room makes the one to come back to, on going into it (<c>GetMapBlackOutWarpId</c>).</summary>
    public static SpawnLocation? OfRoom(string mapName) => All.FirstOrDefault(s => s.Room == mapName && s.Id != PalParkLobby);

    /// <summary>Where Teleport takes the player: the town of the Pokémon Center they last went into, Twinleaf Town before any.</summary>
    public static SpawnLocation Respawn(StoryState story) => Get(story.Var(Variable)) ?? All[0];

    /// <summary>
    /// The variable that keeps a region's place to come back to (plan 18 · W9): Sinnoh's is the original's
    /// <see cref="Variable"/>, every other region's its own (<c>VAR_SPAWN_LOCATION_KANTO</c>), so a flight to
    /// another region and back leaves each region's last Pokémon Center as it was.
    /// </summary>
    public static string VariableOf(string? regionId) =>
        regionId == null || string.Equals(regionId, RegionDatabase.Sinnoh, StringComparison.OrdinalIgnoreCase)
            ? Variable : Variable + "_" + regionId.ToUpperInvariant();

    /// <summary>The place a room of any region makes the one to come back to there, on going into it; null for any other room.</summary>
    public static SpawnLocation? OfRoomAnywhere(string mapName) =>
        RegionDatabase.RegionOfMap(mapName) is { } region && !string.Equals(region.Id, RegionDatabase.Sinnoh, StringComparison.OrdinalIgnoreCase)
            ? In(region.Id).FirstOrDefault(s => s.Room == mapName)
            : OfRoom(mapName);

    /// <summary>
    /// Where Teleport takes the player in a region (plan 18 · W9): the town of its Pokémon Center they last went into,
    /// by the region's own variable, else its first spawn (Sinnoh's Twinleaf Town, Kanto's Pallet Town). Null for a
    /// region with no spawns.
    /// </summary>
    public static SpawnLocation? Respawn(StoryState story, string? regionId)
    {
        var list = In(regionId);
        if (list.Count == 0) return null;
        return Get(regionId, story.Var(VariableOf(regionId))) ?? list[0];
    }

    /// <summary>
    /// The places Fly can go to: those the region's map has open (<see cref="FlyFlag"/>), among the areas that are
    /// open, in the original's order.
    /// </summary>
    public static IEnumerable<SpawnLocation> FlyDestinations(StoryState story, Func<string, bool> isOpen) =>
        All.Where(s => story.Has(FlyFlag(s)) && isOpen(s.Area));

    /// <summary>
    /// The flag that opens a place to Fly: the one the region's map names for it where it names one (the original's
    /// <c>sTownMapFlyLocationUnlockFlags</c> asks Pal Park's gate, not Route 221's arrival), else its first arrival's.
    /// </summary>
    public static string FlyFlag(SpawnLocation spawn) =>
        RegionMap.ForMap(spawn.Map)?.FlySpots.FirstOrDefault(f => f.Spawn == spawn.Id)?.Flag ?? spawn.ArrivalFlag;

    /// <summary>
    /// Pal Park's lobby: no Pokémon Center, so never the place to come back to after a white-out or by Teleport (the
    /// original's <c>isWarpPos</c> 0), but a place Fly lands before once its gate has been come to: the Town Map
    /// draws it at its own cell and <c>GetSpawnIdByMapAndCoords</c> finds this entry for it (plan 01 · M12).
    /// </summary>
    public const int PalParkLobby = 19;
}
