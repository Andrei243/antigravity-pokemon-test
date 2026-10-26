using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// The species of our own met in the wild (plan 03 · D15, design B; <c>docs/plans/D15-species-of-ours-in-the-world.md</c>):
/// an overlay's <c>ours</c> (<see cref="OverlayOurs"/>) puts them in the rarest slots of an area's grass (10 and 11), its
/// water (4) and its Super Rod (4), at their own levels, never mapped by the region's levels, once the region's Hall of
/// Fame is entered (for Sinnoh, Kanto's: its team comes back by the harbour at 100 and more). What may stand in such a
/// slot, and which region each species belongs to, is decided here, for the engine, the content check and the tests
/// alike. No drawing or input.
/// </summary>
public static class OursSlots
{
    /// <summary>The flag the area's slots of ours wait for: the region's own Hall of Fame, and for Sinnoh Kanto's.</summary>
    public static string GateFlag(Region region) =>
        region.Id == RegionDatabase.Sinnoh && RegionDatabase.Get(RegionDatabase.Kanto) is { } kanto
            ? kanto.StoryCompleteFlag
            : region.StoryCompleteFlag;

    /// <summary>Whether the slots of ours are met in a region now.</summary>
    public static bool Open(Region? region, Story.StoryState story) => region != null && story.Has(GateFlag(region));

    /// <summary>
    /// A species of ours that may live in the wild: none of the games' and none of the legendary or mythical sets (catch
    /// rate 3), whose ends their own conditions give.
    /// </summary>
    public static bool Placeable(PokemonSpecies species) =>
        species.IsOurs && !species.Legendary && !species.Mythical && species.CatchRate > 3;

    /// <summary>The species that evolves into one of ours, a game's or one of ours; null for none.</summary>
    public static PokemonSpecies? ForebearOf(PokemonSpecies species) =>
        PokemonDatabase.Everyone().FirstOrDefault(f => f.Evolutions?.Any(e => e.TargetSpecies == species.Name) == true);

    /// <summary>The level at which a species of ours is evolved into: its forebear's evolution's level, 0 when none says.</summary>
    public static int EvolutionLevel(PokemonSpecies species) =>
        ForebearOf(species)?.Evolutions?.Where(e => e.TargetSpecies == species.Name).Select(e => e.Level).DefaultIfEmpty(0).Max() ?? 0;

    /// <summary>The generations' regions, the first generation's first.</summary>
    private static readonly string[] ByGeneration =
    {
        RegionDatabase.Kanto, RegionDatabase.Johto, RegionDatabase.Hoenn, RegionDatabase.Sinnoh, RegionDatabase.Unova,
        RegionDatabase.Kalos, RegionDatabase.Alola, RegionDatabase.Galar, RegionDatabase.Paldea
    };

    /// <summary>
    /// The region a species of ours belongs to, its batch's (plan 03 · D14): the region of its forebear's generation,
    /// or, for one grown from a regional form, the region of the block of a thousand it is numbered in (Alola 4000,
    /// Galar 5000, Hisui 6000, Paldea 7000). Null for one of the games' species, or one with no forebear.
    /// </summary>
    public static string? RegionOf(PokemonSpecies species)
    {
        if (!species.IsOurs) return null;
        switch (species.DexNumber / 1000)
        {
            case 4: return RegionDatabase.Alola;
            case 5: return RegionDatabase.Galar;
            case 6: return "Hisui";
            case 7: return RegionDatabase.Paldea;
        }
        // Up the line to a species of the games (a set of ours may evolve from one of ours)
        var seen = new HashSet<string>();
        var at = species;
        while (at.IsOurs && seen.Add(at.Name))
        {
            if (ForebearOf(at) is not { } before) return null;
            at = before;
        }
        return at.Generation is >= 1 and <= 9 ? ByGeneration[at.Generation - 1] : null;
    }

    /// <summary>A slot of ours written into a copy of a table's slot: its species and its levels, unmapped.</summary>
    public static void Put(WildEncounterEntry slot, OursSlot ours)
    {
        slot.SpeciesName = ours.Species;
        slot.MinLevel = ours.MinLevel;
        slot.MaxLevel = ours.MaxLevel;
        slot.Unmapped = true;
    }
}
