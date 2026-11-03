using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Data;

/// <summary>A pack as a save remembers it (plan 16 · T15): its name and the version the game was last played with.</summary>
public sealed class SavedPack
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";

    /// <summary>
    /// The region the save stands in, where it is one this pack adds (plan 16 · T14): without the pack the save's map
    /// is nowhere, which the title says and the game answers by waking the player elsewhere. Null otherwise, so a save
    /// made in the game's own regions keeps exactly what it kept.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? Region { get; set; }
}

/// <summary>
/// A pack a save was played with that isn't in use now: gone or turned off (<see cref="NowVersion"/> null), or another
/// version; <see cref="Region"/> the region of the pack's the save stands in, if it does.
/// </summary>
public sealed record MissingPack(string Name, string Version, string? NowVersion, string? Region = null);

/// <summary>
/// What a save keeps of the packs (plan 16 · T15; GPU-free): the names and versions of the packs in use when it was
/// written, in load order (<c>SaveData.Packs</c>, null for none, so a save made with none is what it always was), and
/// which of them a later start no longer has, which the title tells of before its menu.
/// </summary>
public static class PackRecords
{
    /// <summary>The packs in use now as a save keeps them, in load order; null when none is.</summary>
    public static List<SavedPack>? Of(IReadOnlyList<DataLayer> inUse, Region? here = null) =>
        inUse.Count == 0 ? null : inUse.Where(l => l.Pack != null).Select(l => new SavedPack
        {
            Name = l.Pack!.Name,
            Version = l.Pack.Version,
            Region = here?.Pack != null && string.Equals(here.Pack, l.Pack.Name, StringComparison.OrdinalIgnoreCase) ? here.Id : null
        }).ToList();

    /// <summary>The packs in use now (<see cref="DataLayers.Packs"/>) as a save keeps them, the one whose region the player stands in marked.</summary>
    public static List<SavedPack>? InUse(Region? here = null) => Of(DataLayers.Packs, here);

    /// <summary>The packs a save was played with that aren't in use now at the same version, in the save's order.</summary>
    public static IReadOnlyList<MissingPack> Missing(IReadOnlyList<SavedPack>? saved, IReadOnlyList<DataLayer> inUse)
    {
        if (saved == null || saved.Count == 0) return Array.Empty<MissingPack>();
        var missing = new List<MissingPack>();
        foreach (var pack in saved)
        {
            var now = inUse.FirstOrDefault(l => l.Pack != null && string.Equals(l.Pack.Name, pack.Name, StringComparison.OrdinalIgnoreCase));
            if (now == null) missing.Add(new MissingPack(pack.Name, pack.Version, null, pack.Region));
            else if (!string.Equals(now.Pack!.Version, pack.Version, StringComparison.Ordinal)) missing.Add(new MissingPack(pack.Name, pack.Version, now.Pack.Version, pack.Region));
        }
        return missing;
    }
}
