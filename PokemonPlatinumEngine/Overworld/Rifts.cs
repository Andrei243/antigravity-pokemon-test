using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// A region's rift table (plan 38 · EX5; <c>Data/rifts.json</c>, ours, written by hand and read strictly): the species of
/// other regions met in the tall grass and on a rod within a rift's reach, their levels as the region's own game writes them,
/// and the items its finds are drawn from.
/// </summary>
public sealed class RiftTable
{
    public List<string> Grass { get; set; } = new();
    public List<string> Water { get; set; } = new();
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; }
    public List<string> Finds { get; set; } = new();
}

/// <summary>The rift tables of every region, by the region's id (<c>Data/rifts.json</c>).</summary>
public sealed class RiftsFile
{
    public const string FileName = "rifts.json";

    public Dictionary<string, RiftTable> Regions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>An hour's rift: the region, the place it opens in, its centre tile and its three finds' tiles and items.</summary>
public sealed record Rift(string Region, string Area, int X, int Y, IReadOnlyList<(int X, int Y, string Item)> Finds)
{
    /// <summary>Whether a tile is within the rift's reach (<see cref="Rifts.Reach"/> tiles either way of its centre).</summary>
    public bool Holds(int x, int y) => Math.Abs(x - X) <= Rifts.Reach && Math.Abs(y - Y) <= Rifts.Reach;
}

/// <summary>
/// Rifts in the field (plan 38 · EX5; <c>docs/mechanics/rulings.md</c>, "The field beyond the tables"; after Legends: Arceus's
/// space-time distortions, the content ours): a tear that stands for an hour of the clock in a place of a region with its Hall
/// of Fame entered. Not the rift to Hisui (plan 23) nor Spear Pillar's (<see cref="PropType.Rift"/>): only the picture
/// <see cref="SceneEffect.Rift"/> is shared. Whether an hour has one, its place and its centre are worked out from the day's
/// number, the hour and the region by <see cref="SplitMix"/>, never drawn: one hour in <see cref="OneHourIn"/>, at a place
/// that can have an outbreak (and not one of the day's), its centre a tile of the place's tall grass. Within
/// <see cref="Reach"/> tiles of it the grass's slots 0 to 5 and the rods' slots are the rift's table, and three finds lie
/// in its ground, hidden items under daily flags of their own. GPU-free; no input or drawing.
/// </summary>
public static class Rifts
{
    /// <summary>How far from its centre (in tiles, either way) a rift reaches.</summary>
    public const int Reach = 12;

    /// <summary>One hour in this many has a rift.</summary>
    public const int OneHourIn = 3;

    /// <summary>The grass's slots a rift takes in its reach (weights 20, 20, 10, 10, 10, 10: eighty in a hundred).</summary>
    public const int GrassSlots = 6;

    /// <summary>How many finds lie in a rift's ground.</summary>
    public const int FindCount = 3;

    /// <summary>The daily flags of the finds, one each: a find taken stays taken for the day, in every rift of it.</summary>
    public static string FindFlag(int n) => $"{FindFlagStem}{n + 1}";

    private const string FindFlagStem = "FLAG_DAILY_RIFT_FIND_";

    /// <summary>Whether a flag is one of the finds' (what a rift laid, and takes away again).</summary>
    public static bool IsFindFlag(string flag) => flag.StartsWith(FindFlagStem, StringComparison.Ordinal);

    private static readonly Lazy<RiftsFile> file = new(() => System.IO.File.Exists(GameDataFiles.PathOf(RiftsFile.FileName))
        ? GameDataFiles.Load<RiftsFile>(RiftsFile.FileName, strict: true) : new RiftsFile());

    /// <summary>The rift tables as read.</summary>
    public static RiftsFile File => file.Value;

    /// <summary>A region's rift table; null where it has none, and then it has no rifts.</summary>
    public static RiftTable? TableOf(string region) => File.Regions.TryGetValue(region, out var t) && t.Grass.Count > 0 ? t : null;

    /// <summary>Whether a region has rifts now: its Hall of Fame is entered and it has a table.</summary>
    public static bool Open(Region? region, Story.StoryState story) => Outbreaks.Open(region, story) && TableOf(region!.Id) != null;

    // The tall grass of a place on a map, row by row, worked out once
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Map, Dictionary<string, List<(int X, int Y)>>> grass = new();

    /// <summary>The tiles of a place's tall grass on a map, row by row; empty where the map holds none of it.</summary>
    public static IReadOnlyList<(int X, int Y)> GrassOf(Map map, string area)
    {
        var byArea = grass.GetValue(map, m =>
        {
            var all = new Dictionary<string, List<(int X, int Y)>>(StringComparer.Ordinal);
            for (int y = 0; y < m.Height; y++)
                for (int x = 0; x < m.Width; x++)
                    if (m.BehaviourAt(x, y) is TileBehavior.TallGrass or TileBehavior.VeryTallGrass && m.AreaAt(x, y)?.Key is { } key)
                    {
                        if (!all.TryGetValue(key, out var list)) all[key] = list = new();
                        list.Add((x, y));
                    }
            return all;
        });
        return byArea.TryGetValue(area, out var tiles) ? tiles : Array.Empty<(int, int)>();
    }

    /// <summary>
    /// The rift of an hour in a region, or null where the hour has none. The same day and hour always give the same; the
    /// next hour another, or none.
    /// </summary>
    /// <param name="region">The region's id.</param>
    /// <param name="daily">The day's number.</param>
    /// <param name="hour">The hour of the clock, 0 to 23.</param>
    /// <param name="map">The region's overworld, whose tall grass the centre is chosen from.</param>
    /// <param name="outbreaks">The day's outbreaks, whose places a rift keeps off.</param>
    public static Rift? Of(string region, uint daily, int hour, Map map, IReadOnlyList<Outbreak>? outbreaks)
    {
        if (TableOf(region) is not { } table) return null;
        uint seed = SplitMix.Fnv(region);
        if (SplitMix.Mix(daily, seed, 0x3100u + (uint)hour) % OneHourIn != 0) return null;
        var places = Outbreaks.PlacesOf(region).Select(p => p.Area)
            .Where(a => Outbreaks.At(outbreaks, a) == null && GrassOf(map, a).Count > 0).ToList();
        if (places.Count == 0) return null;
        string area = places[(int)(SplitMix.Mix(daily, seed, 0x3200u + (uint)hour) % (uint)places.Count)];
        var tiles = GrassOf(map, area);
        var centre = tiles[(int)(SplitMix.Mix(daily, seed, 0x3300u + (uint)hour) % (uint)tiles.Count)];
        // Three finds on walkable tiles round the centre, each a different one, the items from the table
        var finds = new List<(int X, int Y, string Item)>();
        var round = new List<(int X, int Y)>();
        for (int dy = -4; dy <= 4; dy++)
            for (int dx = -4; dx <= 4; dx++)
            {
                int x = centre.X + dx, y = centre.Y + dy;
                if ((dx != 0 || dy != 0) && map.InBounds(x, y) && map.IsWalkable(x, y) && map.AreaAt(x, y)?.Key == area) round.Add((x, y));
            }
        for (uint n = 0; finds.Count < FindCount && round.Count > 0 && table.Finds.Count > 0 && n < 32; n++)
        {
            var at = round[(int)(SplitMix.Mix(daily, seed, 0x3400u + (uint)hour * 64u + n) % (uint)round.Count)];
            round.Remove(at);
            string item = table.Finds[(int)(SplitMix.Mix(daily, seed, 0x3500u + (uint)hour * 64u + n) % (uint)table.Finds.Count)];
            finds.Add((at.X, at.Y, item));
        }
        return new Rift(region, area, centre.X, centre.Y, finds);
    }

    /// <summary>
    /// Lays a rift's table over a place's grass: its species in the first <see cref="GrassSlots"/> slots, in its table's
    /// order and round again, each at the table's levels, marked so a meeting has one shiny roll more.
    /// </summary>
    public static void LayGrass(List<WildEncounterEntry> slots, RiftTable table)
    {
        if (slots.Count != 12 || table.Grass.Count == 0) return;
        for (int i = 0; i < GrassSlots; i++) Mark(slots[i], table.Grass[i % table.Grass.Count], table);
    }

    /// <summary>A rod's table within a rift's reach: the rift's water species in every slot, round again, at its levels.</summary>
    public static List<WildEncounterEntry> Water(IReadOnlyList<WildEncounterEntry> rod, RiftTable table) =>
        rod.Select((slot, i) =>
        {
            var e = new WildEncounterEntry { Weight = slot.Weight };
            Mark(e, table.Water.Count > 0 ? table.Water[i % table.Water.Count] : slot.SpeciesName, table);
            return e;
        }).ToList();

    private static void Mark(WildEncounterEntry slot, string species, RiftTable table)
    {
        slot.SpeciesName = species;
        slot.Rift = true;
        slot.Outbreak = false;
        slot.Moves = null;
        if (table.MinLevel > 0)
        {
            slot.MinLevel = table.MinLevel;
            slot.MaxLevel = Math.Max(table.MinLevel, table.MaxLevel);
        }
    }

    /// <summary>The shiny rolls more a Pokémon met out of a rift has.</summary>
    public const int ShinyRolls = 1;
}
