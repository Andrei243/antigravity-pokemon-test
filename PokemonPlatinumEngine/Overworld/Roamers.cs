using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// Roaming Pokémon (plan 06 · R13; <c>src/roaming_pokemon.c</c>, <c>roamer_after_battle.c</c> and the roamers' part
/// of <c>wild_encounters.c</c>), with no drawing or input. Once set loose (Mesprit after its cavern, Cresselia after
/// Fullmoon Island, the three birds after the Hall of Fame: the story's scripts, <c>roamer start</c>), each roams the
/// 29 places of the mainland that have grass, never into the one the player has just left: whenever the player walks
/// into another place each moves on, one time in sixteen anywhere, otherwise to a place nearby; flying, Teleport and
/// coming back to a saved game send each anywhere. Where one is, any Pokémon met in the field is it one time in two
/// (one of them if there are several; not beside a partner, not in a Poké Radar patch), a Repel keeping it away as
/// it would any Pokémon of its level. It keeps the HP and condition each battle leaves it with; knocked out or caught
/// it roams no more, and after any battle with it every roamer there moves on, as they do three times in ten after
/// another wild battle.
/// Johto's two (plan 20 · J7; HeartGold's <c>src/field_roamer.c</c>): Raikou and Entei, set loose at level 40 when they
/// flee the Burned Tower, roam HeartGold's sixteen routes of Johto (<see cref="JohtoRoutes"/>) by the same rules, each
/// moving among its own region's places alone.
/// Hoenn's (plan 21 · H9, part b; Emerald's <c>src/roamer.c</c>): Latias or Latios, whichever the player names on the
/// television's report after Hoenn's Hall of Fame, set loose at level 40 over Emerald's twenty routes
/// (<see cref="HoennRoutes"/>) by the same rules, Platinum's, where Emerald's differ only in the odds of a move.
/// </summary>
public static class Roamers
{
    /// <summary>The original's six slots (<c>ROAMING_SLOT_*</c>, <c>RoamingPokemon_ActivateSlot</c>): each one's species and level.</summary>
    public static readonly (string Species, int Level)[] Slots =
    {
        ("Mesprit", 50), ("Cresselia", 50), ("Darkrai", 40), ("Moltres", 60), ("Zapdos", 60), ("Articuno", 60),
        // HeartGold's ROAMER_RAIKOU and ROAMER_ENTEI (Save_CreateRoamerByID: level 40), after Platinum's six
        ("Raikou", 40), ("Entei", 40),
        // Emerald's eon roamer (plan 21 · H9, part b; ROAMER_SPECIES at level 40), whichever of the two the player names
        // on the television's report after Hoenn's Hall of Fame
        ("Latias", 40), ("Latios", 40)
    };

    /// <summary>The slots that roam Johto (HeartGold's <c>roamer_idx == ROAMER_RAIKOU || ROAMER_ENTEI</c>): the rest roam Sinnoh.</summary>
    public static bool InJohto(string species) => species is "Raikou" or "Entei";

    /// <summary>The slots that roam Hoenn (Emerald's <c>ROAMER_SPECIES</c>, Latias or Latios).</summary>
    public static bool InHoenn(string species) => species is "Latias" or "Latios";

    /// <summary>The slots that roam Sinnoh: Platinum's six.</summary>
    public static bool InSinnoh(string species) => !InJohto(species) && !InHoenn(species);

    /// <summary>
    /// Hoenn's places a roamer goes (Emerald's <c>sRoamerLocations</c>, its first column: Routes 110, 111 and 117 to
    /// 134), by their area keys.
    /// </summary>
    public static readonly string[] HoennRoutes =
    {
        "route_110", "route_111", "route_117", "route_118", "route_119", "route_120", "route_121", "route_122", "route_123",
        "route_124", "route_125", "route_126", "route_127", "route_128", "route_129", "route_130", "route_131", "route_132",
        "route_133", "route_134"
    };

    // Emerald's sRoamerLocations' other columns, the places beside each, in HoennRoutes' order
    private static readonly int[][] HoennNearby =
    {
        new[] { 1, 2, 3, 19 },      // 110: 111, 117, 118, 134
        new[] { 0, 2, 3 },          // 111: 110, 117, 118
        new[] { 1, 0, 3 },          // 117: 111, 110, 118
        new[] { 2, 0, 1, 4, 8 },    // 118: 117, 110, 111, 119, 123
        new[] { 3, 5 },             // 119: 118, 120
        new[] { 4, 6 },             // 120: 119, 121
        new[] { 5, 7, 8 },          // 121: 120, 122, 123
        new[] { 6, 8 },             // 122: 121, 123
        new[] { 7, 3 },             // 123: 122, 118
        new[] { 6, 10, 11 },        // 124: 121, 125, 126
        new[] { 9, 12 },            // 125: 124, 127
        new[] { 9, 12 },            // 126: 124, 127
        new[] { 10, 11, 13 },       // 127: 125, 126, 128
        new[] { 12, 14 },           // 128: 127, 129
        new[] { 13, 15 },           // 129: 128, 130
        new[] { 14, 16 },           // 130: 129, 131
        new[] { 15, 17 },           // 131: 130, 132
        new[] { 16, 18 },           // 132: 131, 133
        new[] { 17, 19 },           // 133: 132, 134
        new[] { 18, 0 }             // 134: 133, 110
    };

    /// <summary>Johto's places a roamer goes (HeartGold's <c>sRoamerLocations</c>, <c>ROAMER_LOC_R29</c> to <c>R46</c>), by its area keys.</summary>
    public static readonly string[] JohtoRoutes =
    {
        "route_29", "route_30", "route_31", "route_32", "route_33", "route_34", "route_35", "route_36", "route_37",
        "route_38", "route_39", "route_42", "route_43", "route_44", "route_45", "route_46"
    };

    // HeartGold's sRoamerAdjacencyTable for Johto, in JohtoRoutes' order
    private static readonly int[][] JohtoNearby =
    {
        new[] { 1, 15 },        // 29: 30, 46
        new[] { 0, 2 },         // 30: 29, 31
        new[] { 1, 3, 7 },      // 31: 30, 32, 36
        new[] { 2, 4, 7 },      // 32: 31, 33, 36
        new[] { 3, 5 },         // 33: 32, 34
        new[] { 4, 6 },         // 34: 33, 35
        new[] { 5, 7 },         // 35: 34, 36
        new[] { 2, 3, 8 },      // 36: 31, 32, 37
        new[] { 7, 9, 11 },     // 37: 36, 38, 42
        new[] { 8, 10, 11 },    // 38: 37, 39, 42
        new[] { 9 },            // 39: 38
        new[] { 8, 9, 12, 13 }, // 42: 37, 38, 43, 44
        new[] { 11, 13 },       // 43: 42, 44
        new[] { 11, 12, 15 },   // 44: 42, 43, 46
        new[] { 13, 15 },       // 45: 44, 46
        new[] { 0, 14 }         // 46: 29, 45
    };

    /// <summary>The places a species roams: Johto's for Raikou and Entei, Hoenn's for Latias and Latios, Sinnoh's for the rest.</summary>
    public static string[] RoutesOf(string species) => InJohto(species) ? JohtoRoutes : InHoenn(species) ? HoennRoutes : Routes;

    private static int[][] NearbyOf(string species) => InJohto(species) ? JohtoNearby : InHoenn(species) ? HoennNearby : Nearby;

    /// <summary>The places roamers go, by the original's numbers (<c>RoamingPokemonRoutes</c>, <c>RI_*</c>).</summary>
    public static readonly string[] Routes =
    {
        "route_201", "route_202", "route_203", "route_204_south", "route_204_north", "route_205_south", "route_205_north",
        "route_206", "route_207", "route_208", "route_209", "route_210_south", "route_210_north", "route_211_west",
        "route_211_east", "route_212_north", "route_212_south", "route_213", "route_214", "route_215", "route_216",
        "route_217", "route_218", "route_219", "route_220", "route_221", "route_222", "valley_windworks_outside",
        "fuego_ironworks_outside"
    };

    // The places near each (sNearbyRoutes): the routes beside it and those of the towns beside it. Route 205 North's
    // third is the original's 9, Route 208, though Eterna City's own routes are 206 and 211 West
    private static readonly int[][] Nearby =
    {
        new[] { 1, 23 },            // 201: 202, 219
        new[] { 0, 2, 3, 22, 23 },  // 202: 201, 203, 204 South, 218, 219
        new[] { 1, 3, 8, 22 },      // 203: 202, 204 South, 207, 218
        new[] { 1, 2, 4, 22 },      // 204 South: 202, 203, 204 North, 218
        new[] { 3, 5 },             // 204 North: 204 South, 205 South
        new[] { 4, 6, 27, 28 },     // 205 South: 204 North, 205 North, the Windworks, the Ironworks
        new[] { 5, 7, 9 },          // 205 North: 205 South, 206, 208
        new[] { 6, 8, 13 },         // 206: 205 North, 207, 211 West
        new[] { 2, 7, 9 },          // 207: 203, 206, 208
        new[] { 8, 10, 15 },        // 208: 207, 209, 212 North
        new[] { 9, 11, 15 },        // 209: 208, 210 South, 212 North
        new[] { 10, 12, 19 },       // 210 South: 209, 210 North, 215
        new[] { 11, 14 },           // 210 North: 210 South, 211 East
        new[] { 6, 7, 14, 20 },     // 211 West: 205 North, 206, 211 East, 216
        new[] { 12, 13, 20 },       // 211 East: 210 North, 211 West, 216
        new[] { 9, 10, 16 },        // 212 North: 208, 209, 212 South
        new[] { 15, 17 },           // 212 South: 212 North, 213
        new[] { 16, 18, 26 },       // 213: 212 South, 214, 222
        new[] { 17, 19, 26 },       // 214: 213, 215, 222
        new[] { 11, 18 },           // 215: 210 South, 214
        new[] { 13, 14, 21 },       // 216: 211 West, 211 East, 217
        new[] { 20 },               // 217: 216
        new[] { 1, 2, 3 },          // 218: 202, 203, 204 South
        new[] { 0, 1, 24 },         // 219: 201, 202, 220
        new[] { 23, 25 },           // 220: 219, 221
        new[] { 24 },               // 221: 220
        new[] { 17, 18 },           // 222: 213, 214
        new[] { 5 },                // the Windworks: 205 South
        new[] { 5 }                 // the Ironworks: 205 South
    };

    /// <summary>The story's variable that tells what became of a roamer (<c>VAR_ROAMING_&lt;SPECIES&gt;_STATE</c>).</summary>
    public static string StateVariable(string species) => $"VAR_ROAMING_{species.ToUpperInvariant()}_STATE";

    /// <summary>What the variable says (<c>ROAMER_STATE_*</c>).</summary>
    public const int Roaming = 0, Captured = 1, Defeated = 2, Reset = 3;

    /// <summary>The slot of a species, or null for one that doesn't roam.</summary>
    public static int? SlotOf(string species)
    {
        int slot = Array.FindIndex(Slots, s => string.Equals(s.Species, species, StringComparison.OrdinalIgnoreCase));
        return slot >= 0 ? slot : null;
    }

    /// <summary>The place a roamer is, by its key; null for one that isn't roaming.</summary>
    public static string? PlaceOf(Roamer roamer)
    {
        if (!roamer.Active) return null;
        var routes = RoutesOf(roamer.Species);
        return routes[Math.Clamp(roamer.Route, 0, routes.Length - 1)];
    }

    /// <summary>
    /// <c>RoamingPokemon_ActivateSlot</c>: the slot's Pokémon is made (random IVs, at full HP, with no condition) and
    /// set loose somewhere at random.
    /// </summary>
    public static void SetLoose(SpecialEncounters state, int slot, Random rng)
    {
        var (species, level) = Slots[slot];
        var roamer = state.Roamers[slot];
        roamer.Species = species;
        roamer.Level = level;
        var made = new Pokemon(PokemonDatabase.Get(species)!, level, rng);
        roamer.Pokemon = SavedPokemonData.FromPokemon(made);
        roamer.Active = true;
        MoveRandom(state, slot, rng);
    }

    /// <summary><c>MoveRoamerRandom</c>: anywhere but where it is and where the player has just been.</summary>
    public static void MoveRandom(SpecialEncounters state, int slot, Random rng)
    {
        var roamer = state.Roamers[slot];
        var routes = RoutesOf(roamer.Species);
        string current = routes[Math.Clamp(roamer.Route, 0, routes.Length - 1)];
        while (true)
        {
            int next = rng.Next(routes.Length);
            if (routes[next] == state.PreviousPlace || routes[next] == current) continue;
            roamer.Route = next;
            return;
        }
    }

    /// <summary><c>MoveRoamerNearby</c>: to a place near it that isn't where the player has just been; one with a single neighbour that the player has just left sends it anywhere.</summary>
    public static void MoveNearby(SpecialEncounters state, int slot, Random rng)
    {
        var roamer = state.Roamers[slot];
        var routes = RoutesOf(roamer.Species);
        var near = NearbyOf(roamer.Species)[Math.Clamp(roamer.Route, 0, routes.Length - 1)];
        if (near.Length == 1)
        {
            if (routes[near[0]] == state.PreviousPlace) MoveRandom(state, slot, rng);
            else roamer.Route = near[0];
            return;
        }
        while (true)
        {
            int next = near[rng.Next(near.Length)];
            if (routes[next] == state.PreviousPlace) continue;
            roamer.Route = next;
            return;
        }
    }

    /// <summary>
    /// The player has walked into another place (<c>FieldSystem_InitFlagsOnMapChange</c>): it becomes where they are
    /// (<c>RoamingPokemon_UpdatePlayerRecentRoutes</c>, only while any roams), and every roamer moves on, anywhere one
    /// time in sixteen, nearby otherwise (<c>RoamingPokemon_MoveAllLocations</c>).
    /// </summary>
    public static void PlayerWalkedInto(SpecialEncounters state, string place, Random rng)
    {
        NotePlace(state, place);
        for (int slot = 0; slot < state.Roamers.Count; slot++)
        {
            if (!state.Roamers[slot].Active) continue;
            if (rng.Next(16) == 0) MoveRandom(state, slot, rng);
            else MoveNearby(state, slot, rng);
        }
    }

    /// <summary>A warp has taken the player into another place (<c>FieldSystem_InitFlagsWarp</c>): it is noted, and nobody moves.</summary>
    public static void NotePlace(SpecialEncounters state, string place)
    {
        if (state.Roamers.Any(r => r.Active)) state.Arrive(place);
    }

    /// <summary><c>RoamingPokemon_RandomizeAllLocations</c>: Fly, Teleport, or a saved game come back to: every roamer anywhere.</summary>
    public static void Scatter(SpecialEncounters state, Random rng)
    {
        for (int slot = 0; slot < state.Roamers.Count; slot++)
            if (state.Roamers[slot].Active) MoveRandom(state, slot, rng);
    }

    /// <summary>
    /// <c>TryEncounterRoamer</c>: the roamer met at a place where a Pokémon is about to be, one time in two when any is
    /// there (one of them drawn when several are). Null for none.
    /// </summary>
    public static int? MeetHere(SpecialEncounters state, string? place, Random rng)
    {
        if (place == null) return null;
        var here = Enumerable.Range(0, state.Roamers.Count).Where(s => PlaceOf(state.Roamers[s]) == place).ToList();
        if (here.Count == 0 || rng.Next(2) == 0) return null;
        return here.Count > 1 ? here[rng.Next(here.Count)] : here[0];
    }

    /// <summary>The roamer of a slot as a Pokémon to battle (<c>AddRoamerToEnemyParty</c>): as it was made, with the HP and condition it has now; no item.</summary>
    public static Pokemon ToBattle(Roamer roamer) =>
        roamer.Pokemon?.ToPokemon() ?? new Pokemon(PokemonDatabase.Get(roamer.Species)!, roamer.Level);

    /// <summary>
    /// <c>RoamerAfterBattle_UpdateRoamers</c>, after a wild battle the player didn't lose: a roamer battled keeps its HP
    /// and condition, or roams no more once knocked out or caught (the story's variable says which), and every roamer
    /// where the player stands moves on; after any other wild battle they do so three times in ten.
    /// </summary>
    public static void AfterBattle(SpecialEncounters state, Pokemon foe, bool won, bool caught, string? place, StoryState story, Random rng)
    {
        int slot = state.Roamers.FindIndex(r => r.Active && r.Species == foe.Species.Name);
        if (slot >= 0)
        {
            var roamer = state.Roamers[slot];
            if (won && foe.CurrentHP == 0)
            {
                Gone(roamer);
                story.SetVar(StateVariable(foe.Species.Name), Defeated);
            }
            else if (caught)
            {
                Gone(roamer);
                story.SetVar(StateVariable(foe.Species.Name), Captured);
            }
            else if (roamer.Pokemon != null)
            {
                roamer.Pokemon.CurrentHP = foe.CurrentHP;
                roamer.Pokemon.Status = (int)foe.Status;
            }
            MoveOff(state, place, rng);
        }
        else if (rng.Next(100) < 30) MoveOff(state, place, rng);
    }

    // SpecialEncounter_ZeroRoamerData
    private static void Gone(Roamer roamer)
    {
        roamer.Active = false;
        roamer.Route = 0;
        roamer.Pokemon = null;
    }

    /// <summary><c>MoveRoamersOffMap</c>: every roamer where the player stands goes anywhere else.</summary>
    private static void MoveOff(SpecialEncounters state, string? place, Random rng)
    {
        for (int slot = 0; slot < state.Roamers.Count; slot++)
            if (state.Roamers[slot].Active && PlaceOf(state.Roamers[slot]) == place) MoveRandom(state, slot, rng);
    }
}
