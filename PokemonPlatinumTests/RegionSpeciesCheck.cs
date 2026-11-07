using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 40 · GE1: the shape every region's species check shares (plan 21 · H12c's for Hoenn, plan 03 · D12a's for
/// Sinnoh). A region's own Pokédex is held to <see cref="SpeciesSources.Of(string)"/>, and what nothing gives is held to
/// a list of what is owed, each entry naming who owes it: a way lost (a species no longer given and not owed) and a way
/// given (a species owed that something gives now) both fail, so the list stays honest. The same for the items and places
/// the region's evolutions ask for. Plans 23, 33 and 34 call these in their regions' last sessions.
/// </summary>
public static class RegionSpeciesCheck
{
    /// <summary>What is wrong with a region's list of what it owes, or nothing: lost, given, or owed and not of the Pokédex.</summary>
    public static List<string> Problems(SpeciesSources sources, IReadOnlyList<string> dex, IReadOnlyDictionary<string, string> owed)
    {
        var problems = new List<string>();
        var missing = dex.Where(s => !sources.Has(s)).ToList();
        var lost = missing.Where(s => !owed.ContainsKey(s)).ToList();
        if (lost.Count > 0) problems.Add($"Nothing gives: {string.Join(", ", lost.Select(s => $"[\"{s}\"] = \"…\""))}");
        var given = owed.Keys.Where(sources.Has).Select(s => $"{s} ({sources.Ways[s]})").ToList();
        if (given.Count > 0) problems.Add($"Owed but now given: {string.Join(", ", given)}");
        var stray = owed.Keys.Where(s => !dex.Contains(s)).ToList();
        if (stray.Count > 0) problems.Add($"Owed but not of the Pokédex: {string.Join(", ", stray)}");
        var nobody = owed.Where(o => string.IsNullOrWhiteSpace(o.Value)).Select(o => o.Key).ToList();
        if (nobody.Count > 0) problems.Add($"Owed by nobody: {string.Join(", ", nobody)}");
        return problems;
    }

    /// <summary>
    /// The items and places the evolutions within a region's Pokédex ask for that nothing in the region gives (a trade's
    /// is the Linking Cord), each once, with the first evolution that asks; as <c>SinnohSpeciesTests</c> asks of Sinnoh, a
    /// second way to the same species with no item beside one that wants an item (Feebas's Prism Scale) is passed over.
    /// </summary>
    public static Dictionary<string, string> Needs(SpeciesSources sources, IReadOnlyList<string> dex)
    {
        var inDex = dex.ToHashSet(StringComparer.Ordinal);
        var needs = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string name in dex)
        {
            var species = PokemonDatabase.Get(name);
            foreach (var e in species?.Evolutions ?? new())
            {
                if (e.Region != null || e.FromForm != null || !inDex.Contains(e.TargetSpecies)) continue;
                if (e.Item != null && species!.Evolutions!.Any(o => o != e && o.TargetSpecies == e.TargetSpecies && o.Item == null)) continue;
                string why = $"{name} into {e.TargetSpecies}";
                if (e.Item != null && !sources.Items.ContainsKey(e.Item)) needs.TryAdd(e.Item, why);
                if (e.Location != null && !sources.Sites.Contains(e.Location)) needs.TryAdd(e.Location, why);
                if (e.Method is EvolutionMethod.Trade or EvolutionMethod.TradeHoldingItem or EvolutionMethod.TradeWithSpecies
                    && !sources.Items.ContainsKey(Evolution.LinkingCord))
                    needs.TryAdd(Evolution.LinkingCord, why);
            }
        }
        return needs;
    }

    /// <summary>What is wrong with a region's list of the items and places it owes, or nothing.</summary>
    public static List<string> NeedProblems(SpeciesSources sources, IReadOnlyList<string> dex, IReadOnlyDictionary<string, string> owed)
    {
        var needs = Needs(sources, dex);
        var problems = new List<string>();
        var lost = needs.Where(n => !owed.ContainsKey(n.Key)).Select(n => $"[\"{n.Key}\"] = \"…\" ({n.Value})").ToList();
        if (lost.Count > 0) problems.Add($"Asked for and given by nothing: {string.Join(", ", lost)}");
        var given = owed.Keys.Where(k => !needs.ContainsKey(k)).ToList();
        if (given.Count > 0) problems.Add($"Owed but now given or asked for by nobody: {string.Join(", ", given)}");
        return problems;
    }
}
