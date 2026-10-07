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
/// </summary>
public static class Roamers
{
    /// <summary>The original's six slots (<c>ROAMING_SLOT_*</c>, <c>RoamingPokemon_ActivateSlot</c>): each one's species and level.</summary>
    public static readonly (string Species, int Level)[] Slots =
    {
        ("Mesprit", 50), ("Cresselia", 50), ("Darkrai", 40), ("Moltres", 60), ("Zapdos", 60), ("Articuno", 60)
    };

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
    public static string? PlaceOf(Roamer roamer) => roamer.Active ? Routes[Math.Clamp(roamer.Route, 0, Routes.Length - 1)] : null;

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
        string current = Routes[Math.Clamp(roamer.Route, 0, Routes.Length - 1)];
        while (true)
        {
            int next = rng.Next(Routes.Length);
            if (Routes[next] == state.PreviousPlace || Routes[next] == current) continue;
            roamer.Route = next;
            return;
        }
    }

    /// <summary><c>MoveRoamerNearby</c>: to a place near it that isn't where the player has just been; one with a single neighbour that the player has just left sends it anywhere.</summary>
    public static void MoveNearby(SpecialEncounters state, int slot, Random rng)
    {
        var roamer = state.Roamers[slot];
        var near = Nearby[Math.Clamp(roamer.Route, 0, Routes.Length - 1)];
        if (near.Length == 1)
        {
            if (Routes[near[0]] == state.PreviousPlace) MoveRandom(state, slot, rng);
            else roamer.Route = near[0];
            return;
        }
        while (true)
        {
            int next = near[rng.Next(near.Length)];
            if (Routes[next] == state.PreviousPlace) continue;
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
