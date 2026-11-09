using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Facilities;

/// <summary>The later battle places' prize counters (<c>Data/facility-prizes.json</c>, written by hand): each place's stock by its word.</summary>
public sealed class FacilityPrizesFile
{
    [DataRequired] public Dictionary<string, List<FrontierPrizeRecord>> Places { get; set; } = new();
}

/// <summary>
/// What the later battle places hand over for the Frontier's one purse of Battle Points (plan 37's decision 6;
/// <c>Data/facility-prizes.json</c>): a stock to each place, by the word a script names it with (<c>frontier prize maison</c>,
/// Kiloude City's Battle Maison, plan 23). The items are the later games' (evolution items, battle items); the prices ours.
/// </summary>
public static class FacilityPrizes
{
    public const string FileName = "facility-prizes.json";

    /// <summary>The Battle Maison's counter in Kiloude City.</summary>
    public const string Maison = "maison";

    /// <summary>The Battle Tree's counter at the head of Poni Gauntlet (plan 23, the Battle Tree's inside).</summary>
    public const string Tree = "tree";

    private static readonly Lazy<FacilityPrizesFile> file = new(() => GameDataFiles.Load<FacilityPrizesFile>(FileName, strict: true));

    /// <summary>The places that have a counter, by their words.</summary>
    public static IEnumerable<string> Places => file.Value.Places.Keys;

    /// <summary>A place's stock, or nothing for a word no place has.</summary>
    public static IReadOnlyList<FrontierPrizeRecord> Of(string place) =>
        file.Value.Places.TryGetValue(place, out var stock) ? stock : Array.Empty<FrontierPrizeRecord>();
}
