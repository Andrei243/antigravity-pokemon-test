using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>What a place of Unova's special tables is on a tile (plan 38 · EX11).</summary>
public enum SpecialKind
{
    /// <summary>Tall grass that shakes: meets the area's shaking-grass table, always.</summary>
    Shaking,
    /// <summary>Water that ripples: met on foot-surf from the rippling water table and on a rod from the rippling rod table.</summary>
    Rippling,
    /// <summary>A dust cloud on a cave's floor: an item (a Gem) or a Pokémon of the area's shaking-grass table.</summary>
    Dust,
    /// <summary>A bridge's shadow: a flying Pokémon of the area's shaking-grass table, met under it.</summary>
    Shadow
}

/// <summary>What a dust cloud gave: an item of Black 2's list, or the Pokémon to meet.</summary>
public sealed record DustFind(string? Item, WildEncounterEntry? Pokemon);

/// <summary>
/// Unova's shaking grass, rippling water, dust clouds and bridge shadows (plan 38 · EX11; <c>docs/mechanics/rulings.md</c>,
/// "Unova's special tiles"): Black 2's special tables are imported for every area (<see cref="MapArea.ShakingGrassEncounters"/>
/// and its kin) and these are the tiles that meet them. A tile is a patch for a day when <see cref="SplitMix"/> over the
/// day's number, the area's key and the tile says so (one candidate tile in <see cref="Density"/>), so the same day always has
/// the same patches, the next day others, and nothing is drawn from <see cref="Dice"/> or the field's generator to find them.
/// Which kind an area's patches are is the area's own: a bridge (its key says so) has shadows, a cave dust clouds, any other
/// place with a shaking-grass table shaking grass on its tall grass, and water with a rippling table ripples. Only Unova's
/// areas have the tables, so every other region has none. GPU-free; no input or drawing.
/// </summary>
public static class UnovaSpecialGrass
{
    /// <summary>One candidate tile in this many is a patch today.</summary>
    public const int Density = 36;

    /// <summary>A dust cloud's share of items, in a hundred; the rest are Pokémon of the area's table.</summary>
    public const int DustItemShare = 50;

    /// <summary>How many Gems a cloud gives and the Gems it may give, Black 2's: the elemental ones.</summary>
    public static readonly string[] Gems =
    {
        "Fire Gem", "Water Gem", "Electric Gem", "Grass Gem", "Ice Gem", "Fighting Gem", "Poison Gem", "Ground Gem", "Flying Gem",
        "Psychic Gem", "Bug Gem", "Rock Gem", "Ghost Gem", "Dragon Gem", "Dark Gem", "Steel Gem", "Normal Gem"
    };

    /// <summary>The seasons' and the places' kind of a patch an area has, or null where it has no special table.</summary>
    public static SpecialKind? LandKindOf(MapArea area) =>
        area.ShakingGrassEncounters.Count == 0 ? null
        : area.Key.Contains("bridge", StringComparison.Ordinal) ? SpecialKind.Shadow
        : area.IsCave ? SpecialKind.Dust
        : SpecialKind.Shaking;

    /// <summary>Whether this tile of the area is a patch of the kind today: the day's number, the area and the tile decide, nothing else.</summary>
    public static bool Patch(SpecialKind kind, string areaKey, uint day, int x, int y) =>
        SplitMix.Mix(SplitMix.Hash(x * 65536 + y, day) ^ SplitMix.Fnv(areaKey), (uint)kind + 0x51u, 0xE11u) % Density == 0;

    /// <summary>The kind of patch at a tile today and whether it applies to someone on the water or on foot; null where there is none.</summary>
    public static SpecialKind? KindAt(Map map, int x, int y, uint day, bool water)
    {
        if (map.AreaAt(x, y) is not { } area) return null;
        if (water)
        {
            if (area.RipplingWaterEncounters.Count == 0 && area.RipplingRodEncounters.Count == 0) return null;
            return TileBehaviors.IsSurfable(map.BehaviourAt(x, y)) && Patch(SpecialKind.Rippling, area.Key, day, x, y) ? SpecialKind.Rippling : null;
        }
        if (LandKindOf(area) is not { } kind) return null;
        bool candidate = kind switch
        {
            SpecialKind.Shaking => map.IsTallGrass(x, y),
            _ => map.IsWalkable(x, y) && !TileBehaviors.IsSurfable(map.BehaviourAt(x, y)) && !map.IsTallGrass(x, y)
        };
        return candidate && Patch(kind, area.Key, day, x, y) ? kind : null;
    }

    /// <summary>The table and its rate a patch of this kind at the tile meets; empty where the area has none for it.</summary>
    public static (IReadOnlyList<WildEncounterEntry> Table, int Rate) TableOf(MapArea area, SpecialKind kind, bool rod = false) => kind switch
    {
        SpecialKind.Rippling when rod => (area.RipplingRodEncounters, area.RipplingRodRate),
        SpecialKind.Rippling => (area.RipplingWaterEncounters, area.RipplingWaterRate),
        _ => (area.ShakingGrassEncounters, area.ShakingGrassRate)
    };

    /// <summary>
    /// What a dust cloud at a tile gives today, worked out from the day, the area and the tile alone: a Gem or one of the area's
    /// shaking-grass slots (by weight) at its own level. The Pokémon is a plain entry the caller meets as any other.
    /// </summary>
    public static DustFind DustAt(MapArea area, uint day, int x, int y)
    {
        var mix = new SplitMix(SplitMix.Mix(SplitMix.Hash(x * 65536 + y, day) ^ SplitMix.Fnv(area.Key), 0xD057u, 0x6E3u));
        var table = area.ShakingGrassEncounters;
        if (table.Count == 0 || mix.Int(100) < DustItemShare)
            return new DustFind(Gems[mix.Int(Gems.Length)], null);
        int total = 0;
        foreach (var e in table) total += Math.Max(1, e.Weight);
        int pick = mix.Int(total);
        foreach (var e in table)
        {
            pick -= Math.Max(1, e.Weight);
            if (pick >= 0) continue;
            int level = e.MinLevel >= e.MaxLevel ? e.MinLevel : e.MinLevel + mix.Int(e.MaxLevel - e.MinLevel + 1);
            return new DustFind(null, new WildEncounterEntry { SpeciesName = e.SpeciesName, MinLevel = level, MaxLevel = level, Weight = e.Weight, FormName = e.FormName });
        }
        return new DustFind(Gems[0], null);
    }

    /// <summary>The kind's name for a toast or a shot line.</summary>
    public static string Word(SpecialKind kind) => kind.ToString().ToLowerInvariant();
}
