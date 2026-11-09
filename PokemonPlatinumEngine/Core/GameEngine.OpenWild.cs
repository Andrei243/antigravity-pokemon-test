using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The wild Pokémon of an open map as the game meets them (plan 32 · OW7): every one is seen (whatever the options'
/// WILD POKéMON row says, as the originals have it), standing in its zone by <see cref="OpenWildField"/>'s rules; one
/// the player walks into, or that runs into the player, is met at once through the grid's own <c>MeetWildPokemon</c>,
/// made as a step in the grass would make it (<see cref="MakeWild"/>: the field's own chance, the region's levels by
/// <c>LevelHere</c>, the lead's Synchronize and Cute Charm, a back strike from behind), on the ground under the player
/// (<see cref="OpenBattleGround"/>). The battle comes back to the open map, and the Pokémon met is gone from it, its zone
/// waiting a while before it brings another out. Nothing of a region of today reaches any of it: no open map is loaded.
/// </summary>
public partial class GameEngine
{
    // The zone of the wild Pokémon whose battle is under way, met in an open map; null otherwise
    private OpenZone? openWildMet;

    /// <summary>
    /// After a tick of the stick in an open map: the wild Pokémon the player touches, if any, met at once. True when a
    /// battle begins (the frame's walking ends there).
    /// </summary>
    private bool MeetOpenWild(OpenField field)
    {
        if (!flow.Party.HasUsablePokemon) return false;
        if (field.Wild.Touching(field.Player.Pos) is not { } seen) return false;
        field.Wild.Met(seen);
        openWildMet = seen.Zone;
        // The row of the zone's table met, at the level it came out with: the Pokémon made as the grass makes one
        string name = seen.Species;
        var species = PokemonDatabase.Get(name);
        string? form = null;
        if (species == null && PokemonDatabase.SpeciesOfForm(name) is { } ofForm)
        {
            form = name;
            species = ofForm;
        }
        var lead = WildLead.Of(flow.Party);
        var entry = new WildEncounterEntry
        {
            SpeciesName = species?.Name ?? name,
            FormName = form,
            MinLevel = seen.Level,
            MaxLevel = seen.Level,
            Nature = WildEncounterRules.NatureFor(lead, fieldRandom),
            Gender = WildEncounterRules.GenderFor(species, lead, fieldRandom)
        };
        var wild = MakeWild(entry, null);
        flow.FieldEncounter = true;
        // Walked into from behind: a back strike under the later rules (plan 10 · F5's, at metre scale)
        wildSurprised = OpenWildField.FromBehind(field.Player.Yaw, seen.Yaw);
        string place = field.Map.AreaAt(field.Player.Pos)?.Key ?? field.Map.Name;
        MeetWildPokemon(wild, BattleKind.Normal, nuzlockeCatch: Nuzlocke.Meet(flow.Run, flow.RunState, place, wild, flow.Pokedex));
        return true;
    }

    /// <summary>
    /// How a battle with a wild Pokémon met in an open map ended: its zone waits the longer before the next when it was
    /// caught or felled, the shorter otherwise, and nothing senses or meets the player for a moment after.
    /// </summary>
    private void OpenWildAfterBattle(BattleEngine? fought)
    {
        if (openWildMet is not { } zone) return;
        openWildMet = null;
        if (flow.Open is not { } field || fought == null) return;
        bool gone = fought.Result is BattleResult.EnemyCaught or BattleResult.PlayerVictory;
        field.Wild.Ended(zone, gone ? OpenWildEnd.CaughtOrFelled : OpenWildEnd.Otherwise);
    }

    /// <summary>The ground a battle begun where the player stands in an open map is fought on (the cover under the capsule).</summary>
    private Battle.Sim.BattleTerrain OpenTerrainHere(OpenField field) =>
        OpenBattleGround.Under(field.Map, field.Player.Pos, field.Player.Body.Stance == OpenStance.Swimming);

    internal sealed partial class Driver
    {
        /// <summary>The open map's wild Pokémon (plan 32 · OW7), or null outside an open map.</summary>
        public OpenWildField? OpenWild => game.flow.Open?.Wild;

        /// <summary>The zone of the wild Pokémon whose battle is under way, met in an open map, or null.</summary>
        public OpenZone? OpenWildMet => game.openWildMet;
    }
}
