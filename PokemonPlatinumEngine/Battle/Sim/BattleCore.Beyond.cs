using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Battle.Sim;

// The Leagues' teams beyond (plan 03 · D15, design T; docs/plans/D15-species-of-ours-in-the-world.md): a third team
// for each region's Elite Four and Champion, of species of our own, found the way a Champion's Mega Dynamax team is
// (MegaMaxTeamOf) and ready by question 10's default: the region's Hall of Fame entered and a Pokémon of the team at or
// above the lowest level its batch of ours evolves at.
public sealed partial class BattleCore
{
    /// <summary>What a team beyond the League's id ends in (<c>elite_four_aaron_beyond</c>).</summary>
    public const string BeyondSuffix = TrainerDatabase.BeyondSuffix;

    /// <summary>
    /// The id of the team beyond a League's team: the id fought with <c>_beyond</c> in place of what follows
    /// <c>_rematch</c> or <c>_first</c> (<c>elite_four_aaron_rematch</c>, <c>kanto_champion_rematch_squirtle</c>:
    /// <c>kanto_champion_beyond</c>, one team whatever starter the rival took) or of a rematch's number
    /// (<c>unova_elite_four_shauntal_2</c>), or after it (<c>johto_champion_lance</c>, <c>hoenn_steven</c>). Null for an
    /// id that is a team beyond already, or a Champion's Mega Dynamax team.
    /// </summary>
    public static string? BeyondIdOf(string id)
    {
        if (id.EndsWith(BeyondSuffix, StringComparison.Ordinal) || id.EndsWith(TrainerDatabase.MegaMaxSuffix, StringComparison.Ordinal)) return null;
        foreach (string marker in new[] { TrainerDatabase.RematchSuffix, TrainerDatabase.FirstMarker })
            if (id.IndexOf(marker, StringComparison.Ordinal) is var at and > 0) return id[..at] + BeyondSuffix;
        int end = id.Length;
        while (end > 0 && char.IsAsciiDigit(id[end - 1])) end--;
        if (end < id.Length && end > 1 && id[end - 1] == '_') return id[..(end - 1)] + BeyondSuffix;
        return id + BeyondSuffix;
    }

    /// <summary>
    /// The team beyond the League a trainer fights with in place of the team asked for (plan 03 · D15-T): a record of
    /// <c>our-trainers.json</c> named by <see cref="BeyondIdOf"/>, once its batch's region's Hall of Fame is entered
    /// and a Pokémon of the player's team, no Egg, stands at or above <see cref="ReadyLevel"/>; null for anyone else.
    /// The region is its team's own batch's (<see cref="OursSlots.RegionOf"/> of its first Pokémon), which is the
    /// League's own region.
    /// </summary>
    public static TrainerRecord? BeyondTeamOf(string id, StoryState story, Party party)
    {
        if (BeyondIdOf(id) is not { } beyondId || TrainerDatabase.Get(beyondId) is not { Party.Count: > 0 } beyond) return null;
        return IsBeyondReady(beyond, story, party) ? beyond : null;
    }

    /// <summary>Whether a team beyond is ready to be fought: its region's Hall of Fame and a Pokémon at its batch's lowest level.</summary>
    public static bool IsBeyondReady(TrainerRecord beyond, StoryState story, Party party)
    {
        if (RegionOfBeyond(beyond) is not { } regionId || RegionDatabase.Get(regionId) is not { } region) return false;
        if (!story.Has(region.StoryCompleteFlag)) return false;
        int ready = ReadyLevel(regionId);
        return ready > 0 && party.Members.Any(p => !p.IsEgg && p.Level >= ready);
    }

    /// <summary>The region a team beyond belongs to: its Pokémon's batch's, null for a team of none of ours.</summary>
    public static string? RegionOfBeyond(TrainerRecord beyond) =>
        beyond.Party.Count == 0 || PokemonDatabase.Get(beyond.Party[0].Species) is not { } first ? null : OursSlots.RegionOf(first);

    /// <summary>
    /// The lowest level at which a species of a region's batch of ours that may be met (not a legendary set's end)
    /// is evolved into: Sinnoh's and Kanto's 101, Johto's 104, Hoenn's 140, Unova's 165. Zero for a region with none.
    /// </summary>
    public static int ReadyLevel(string region) => ReadyLevels.Value.TryGetValue(region, out int level) ? level : 0;

    private static readonly Lazy<Dictionary<string, int>> ReadyLevels = new(() =>
        PokemonDatabase.Ours().Where(OursSlots.Placeable)
            .Select(s => (Region: OursSlots.RegionOf(s), Level: OursSlots.EvolutionLevel(s)))
            .Where(s => s.Region != null && s.Level > 0)
            .GroupBy(s => s.Region!, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Min(s => s.Level), StringComparer.Ordinal));
}
