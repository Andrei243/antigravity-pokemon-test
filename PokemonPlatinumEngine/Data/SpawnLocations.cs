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
public sealed record SpawnLocation(int Id, string Area, int X, int Y, string Room, bool UnlockedOnArrival = true, string? Flag = null)
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
        (region.File?.Spawns ?? new()).Select(p => new SpawnLocation(p.Id, p.Area, p.X, p.Y, p.Room, p.UnlockedOnArrival, p.Flag)).ToList();

    /// <summary>Sinnoh's twenty, from <c>regions/sinnoh.json</c>: the places the rest of this class speaks of until Fly is region by region (plan 18 · W9).</summary>
    public static readonly IReadOnlyList<SpawnLocation> All = Of(RegionDatabase.Get(RegionDatabase.Sinnoh)!);

    public static SpawnLocation? Get(int id) => id >= 1 && id <= All.Count ? All[id - 1] : null;

    /// <summary>The town whose first arrival is noted on coming into an area, if it is one (<c>TryUnlockFlyLocationByMap</c>).</summary>
    public static SpawnLocation? ArrivedIn(string areaKey) => All.FirstOrDefault(s => s.Area == areaKey && s.UnlockedOnArrival);

    /// <summary>The place a room makes the one to come back to, on going into it (<c>GetMapBlackOutWarpId</c>).</summary>
    public static SpawnLocation? OfRoom(string mapName) => All.FirstOrDefault(s => s.Room == mapName && s.Id != PalParkLobby);

    /// <summary>Where Teleport takes the player: the town of the Pokémon Center they last went into, Twinleaf Town before any.</summary>
    public static SpawnLocation Respawn(StoryState story) => Get(story.Var(Variable)) ?? All[0];

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
        RegionMap.Sinnoh?.FlySpots.FirstOrDefault(f => f.Spawn == spawn.Id)?.Flag ?? spawn.ArrivalFlag;

    /// <summary>
    /// Pal Park's lobby: no Pokémon Center, so never the place to come back to after a white-out or by Teleport (the
    /// original's <c>isWarpPos</c> 0), but a place Fly lands before once its gate has been come to: the Town Map
    /// draws it at its own cell and <c>GetSpawnIdByMapAndCoords</c> finds this entry for it (plan 01 · M12).
    /// </summary>
    public const int PalParkLobby = 19;
}
