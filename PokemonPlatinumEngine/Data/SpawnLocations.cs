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
public sealed record SpawnLocation(int Id, string Area, int X, int Y, string Room, bool UnlockedOnArrival = true)
{
    /// <summary>The flag that first arriving in the town sets, which lets one fly there: <c>FLAG_FIRST_ARRIVAL_ETERNA_CITY</c>.</summary>
    public string ArrivalFlag => "FLAG_FIRST_ARRIVAL_" + Area.ToUpperInvariant();
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

    public static readonly IReadOnlyList<SpawnLocation> All = new SpawnLocation[]
    {
        new(1, "twinleaf_town", 116, 886, "PlayerHouse"),
        new(2, "sandgem_town", 177, 843, "PokemonCenter"),
        new(3, "floaroma_town", 176, 667, "FloaromaPokemonCenter"),
        new(4, "solaceon_town", 566, 657, "SolaceonPokemonCenter"),
        new(5, "celestic_town", 472, 539, "CelesticPokemonCenter"),
        new(6, "jubilife_city", 180, 777, "JubilifePokemonCenter"),
        new(7, "canalave_city", 58, 723, "CanalavePokemonCenter"),
        new(8, "oreburgh_city", 303, 757, "OreburghPokemonCenter"),
        new(9, "eterna_city", 305, 531, "EternaPokemonCenter"),
        new(10, "hearthome_city", 465, 698, "HearthomePokemonCenter"),
        new(11, "pastoria_city", 600, 816, "PastoriaPokemonCenter"),
        new(12, "veilstone_city", 717, 612, "VeilstonePokemonCenter"),
        new(13, "sunyshore_city", 860, 785, "SunyshorePokemonCenter"),
        new(14, "snowpoint_city", 379, 234, "SnowpointPokemonCenter"),
        new(15, "pokemon_league", 842, 599, "PokemonLeagueSouthPokemonCenter", UnlockedOnArrival: false),
        new(16, "fight_area", 647, 430, "FightAreaPokemonCenter"),
        new(17, "survival_area", 659, 339, "SurvivalAreaPokemonCenter"),
        new(18, "resort_area", 802, 473, "ResortAreaPokemonCenter"),
        new(19, "route_221", 306, 910, "PalParkLobby", UnlockedOnArrival: false),
        new(20, "pokemon_league", 847, 560, "PokemonLeagueNorthPokemonCenter", UnlockedOnArrival: false)
    };

    public static SpawnLocation? Get(int id) => id >= 1 && id <= All.Count ? All[id - 1] : null;

    /// <summary>The town whose first arrival is noted on coming into an area, if it is one (<c>TryUnlockFlyLocationByMap</c>).</summary>
    public static SpawnLocation? ArrivedIn(string areaKey) => All.FirstOrDefault(s => s.Area == areaKey && s.UnlockedOnArrival);

    /// <summary>The place a room makes the one to come back to, on going into it (<c>GetMapBlackOutWarpId</c>).</summary>
    public static SpawnLocation? OfRoom(string mapName) => All.FirstOrDefault(s => s.Room == mapName && s.Id != 19);

    /// <summary>Where Teleport takes the player: the town of the Pokémon Center they last went into, Twinleaf Town before any.</summary>
    public static SpawnLocation Respawn(StoryState story) => Get(story.Var(Variable)) ?? All[0];

    /// <summary>
    /// The towns Fly can go to: those first arrived in, among the areas that are open, in the original's order.
    /// </summary>
    public static IEnumerable<SpawnLocation> FlyDestinations(StoryState story, Func<string, bool> isOpen) =>
        All.Where(s => s.UnlockedOnArrival && story.Has(s.ArrivalFlag) && isOpen(s.Area));
}
