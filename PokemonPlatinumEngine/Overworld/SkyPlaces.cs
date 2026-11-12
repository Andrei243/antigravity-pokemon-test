using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Delve;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>What a place of the day in the sky is (plan 38 · EX7, part b): an islet at sea, a grove, a hollow, a peak.</summary>
public enum SkyPlaceKind { Islet, Grove, Hollow, Peak }

/// <summary>A place of the day: its number in its region's list, its cell of the region map and its kind.</summary>
public sealed record SkyPlace(string Region, int Index, int X, int Y, SkyPlaceKind Kind);

/// <summary>A find lying on a place of the day: the item, its tile and the daily flag that takes it away once picked up.</summary>
public sealed record SkyFind(string Item, int X, int Y, string Flag);

/// <summary>The hand-written tables of the places of the day (<c>Data/skyplaces.json</c>, ours).</summary>
public sealed class SkyPlacesFile
{
    public const string FileName = "skyplaces.json";

    /// <summary>What may lie on a place's ground, drawn by the place's seed.</summary>
    public List<string> Finds { get; set; } = new();

    /// <summary>Each region's tables: its levels and its species by kind.</summary>
    public Dictionary<string, SkyPlacesRegion> Regions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>A region's tables: the levels its places' Pokémon are met at (its own game's post-game band) and the species of each kind.</summary>
public sealed class SkyPlacesRegion
{
    public int[] Levels { get; set; } = Array.Empty<int>();
    public Dictionary<string, List<string>> Kinds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// The places of the day (plan 38 · EX7, part b; the rulings' "Soaring"; style guide, "Soaring" and "Caves" and "Water"):
/// each region's sky spots (its <c>townmap.json</c>'s <c>sky</c> list, cells over sea and mountain), two or three lit a
/// day by a fixed rotation of the date (never a draw), each a beacon of its own colour while soaring. Landing makes the
/// place from the day's seed and its kind's pieces of ours (laid by <see cref="FloorLayout"/> as the hideaways and the
/// Pyramid are) onto one map per region (<see cref="MapNameOf"/>); its table is species of other regions by the kind,
/// never a legendary or one of the region's own, at the region's post-game band, with two or three finds, gone for the day
/// once picked up, and a beacon tile that lifts the player back into the sky. Everything is worked out from the date, the
/// region and the place by <see cref="SplitMix"/>; nothing is drawn from <see cref="Dice"/>. No drawing or input.
/// </summary>
public static class SkyPlaces
{
    /// <summary>The floor's size: three squares by three of eight tiles, two tiles of the kind's edge round them.</summary>
    public const int Square = 8, Squares = 3, Edge = 2;
    public const int Width = Edge * 2 + Square * Squares, Height = Edge * 2 + Square * Squares;

    /// <summary>The first day the rotation counts from.</summary>
    private static readonly DateTime Epoch = new(2000, 1, 1);

    private static SkyPlacesFile? data;

    /// <summary>The tables, read once.</summary>
    public static SkyPlacesFile Data => data ??= GameDataFiles.Load<SkyPlacesFile>(SkyPlacesFile.FileName, strict: true);

    /// <summary>The day's number the rotation turns by.</summary>
    public static int DayOf(DateTime date) => (int)(date.Date - Epoch).TotalDays;

    /// <summary>The map a region's place of the day is laid on: one to a region, as every map belongs to one region.</summary>
    public static string MapNameOf(string region) => $"SkyPlace{char.ToUpperInvariant(region[0])}{region[1..].ToLowerInvariant()}";

    /// <summary>The region a place-of-the-day map belongs to, or null for another map.</summary>
    public static string? RegionOfMap(string? map) =>
        map != null && map.StartsWith("SkyPlace", StringComparison.Ordinal) && map.Length > 8 ? map[8..] : null;

    /// <summary>A region's places: its town map's sky list.</summary>
    public static IReadOnlyList<SkyPlace> Of(RegionMap map) => map.SkyPlaces;

    /// <summary>How many places are lit on a day: two on even days, three on odd, never more than there are.</summary>
    public static int LitOn(int day, int count) => Math.Min(count, 2 + (day & 1));

    /// <summary>
    /// The places lit on a date: a fixed rotation (a first place moved on by one a day, the rest spread evenly after it),
    /// so a date gives the same places every time and the next day others.
    /// </summary>
    public static IReadOnlyList<SkyPlace> Lit(RegionMap map, DateTime date)
    {
        var all = Of(map);
        if (all.Count == 0) return Array.Empty<SkyPlace>();
        int day = DayOf(date), n = all.Count, lit = LitOn(day, n);
        int start = (day % n + n) % n, stride = Math.Max(1, n / lit);
        return Enumerable.Range(0, lit).Select(i => all[(start + i * stride) % n]).Distinct().ToList();
    }

    /// <summary>A place's seed on a date: its region, its number and the day.</summary>
    public static uint SeedOf(SkyPlace place, DateTime date) => SplitMix.Mix(SplitMix.Fnv(place.Region.ToLowerInvariant()), (uint)place.Index, (uint)DayOf(date));

    /// <summary>
    /// Our own pieces of eight by eight for each kind: '.' open ground, '#' what stands in the way (rock, a tree, a crag) and
    /// '~' water. Each is open at columns 3 and 4 of its top and bottom rows and rows 3 and 4 of its sides, so any square
    /// joins its neighbours (<c>SkyPlaceTests</c> holds it).
    /// </summary>
    public static IReadOnlyList<string[]> PiecesOf(SkyPlaceKind kind) => kind switch
    {
        SkyPlaceKind.Islet => new[]
        {
            new[] { "........", "........", "........", "........", "........", "........", "........", "........" },
            new[] { "~~....~~", "~......~", "........", "........", "........", "........", "~......~", "~~....~~" },
            new[] { "........", ".~~..~~.", ".~~..~~.", "........", "........", ".~~..~~.", ".~~..~~.", "........" },
            new[] { "~~~..~~~", "~~....~~", "~......~", "........", "........", "~......~", "~~....~~", "~~~..~~~" },
            new[] { "........", "..#.....", "........", "........", "........", ".....#..", "........", "........" },
        },
        SkyPlaceKind.Grove => new[]
        {
            new[] { "........", "........", "........", "........", "........", "........", "........", "........" },
            new[] { "##....##", "#......#", "........", "........", "........", "........", "#......#", "##....##" },
            new[] { "........", ".##..##.", ".##..##.", "........", "........", ".##..##.", ".##..##.", "........" },
            new[] { "#.#..#.#", "........", "#......#", "........", "........", "#......#", "........", "#.#..#.#" },
            new[] { "........", "...~~...", "..~~~~..", "........", "........", "..~~~~..", "...~~...", "........" },
        },
        SkyPlaceKind.Hollow => new[]
        {
            new[] { "........", "........", "........", "........", "........", "........", "........", "........" },
            new[] { "###..###", "##....##", "#......#", "........", "........", "#......#", "##....##", "###..###" },
            new[] { "........", ".#....#.", "..#..#..", "........", "........", "..#..#..", ".#....#.", "........" },
            new[] { "........", ".~~..~~.", ".~~..~~.", "........", "........", "........", "........", "........" },
            new[] { "#......#", "........", "...##...", "........", "........", "...##...", "........", "#......#" },
        },
        _ => new[]
        {
            new[] { "........", "........", "........", "........", "........", "........", "........", "........" },
            new[] { "###..###", "##....##", "#......#", "........", "........", "#......#", "##....##", "###..###" },
            new[] { "........", "..#..#..", ".##..##.", "........", "........", ".##..##.", "..#..#..", "........" },
            new[] { "#......#", "#......#", "........", "........", "........", "........", "#......#", "#......#" },
            new[] { "........", ".#....#.", "........", "........", "........", "........", ".#....#.", "........" },
        }
    };

    /// <summary>A place's floor on a date, laid from its seed alone: one draw of its kind's pieces' count for each square.</summary>
    public static FloorLayout Lay(SkyPlace place, DateTime date) =>
        FloorLayout.Lay(PiecesOf(place.Kind), Square, Squares, Edge, Edge, Width, Height, new SeededDraws(SeedOf(place, date)));

    /// <summary>The beacon's tile on the floor, where the player lands and is lifted back into the sky: the middle square's middle.</summary>
    public static (int X, int Y) BeaconOf(FloorLayout floor) => floor.MiddleOf(Squares * Squares / 2);

    /// <summary>The kind's tiles: the ground, its patch of tall grass, what stands in the way, and what lies round the floor.</summary>
    public static (char Ground, char Patch, char Block, char Round, bool Cave) TilesOf(SkyPlaceKind kind) => kind switch
    {
        SkyPlaceKind.Islet => (',', 'w', 'R', '~', false),
        SkyPlaceKind.Grove => ('.', 'w', 'T', 'T', false),
        SkyPlaceKind.Hollow => ('c', ';', 'R', 'X', true),
        _ => (';', '^', 'R', 'R', false)
    };

    /// <summary>A region's tables, its name in any case (a region map names its region in lower case).</summary>
    public static SkyPlacesRegion? TablesOf(string region) =>
        Data.Regions.FirstOrDefault(r => string.Equals(r.Key, region, StringComparison.OrdinalIgnoreCase)).Value;

    /// <summary>The species a kind of place has in a region: its row of the tables, none of the region's own nor a legendary.</summary>
    public static IReadOnlyList<string> SpeciesOf(string region, SkyPlaceKind kind)
    {
        if (TablesOf(region) is not { } tables || !tables.Kinds.TryGetValue(kind.ToString(), out var list)) return Array.Empty<string>();
        var own = SpeciesSources.DexOf(region).ToHashSet(StringComparer.Ordinal);
        return list.Where(s => PokemonDatabase.Get(s) is { Legendary: false, Mythical: false } && !own.Contains(s)).ToList();
    }

    /// <summary>The slots' weights: the hideaways' twelve.</summary>
    public static IReadOnlyList<int> SlotWeights => Models.Underground.Hideaways.SlotWeights;

    /// <summary>A place's table on a date: twelve slots of its kind's species in the order its seed gives, at the region's band.</summary>
    public static IReadOnlyList<WildEncounterEntry> Table(SkyPlace place, DateTime date)
    {
        var pool = SpeciesOf(place.Region, place.Kind);
        var rows = new List<WildEncounterEntry>();
        if (pool.Count == 0 || TablesOf(place.Region) is not { } tables || tables.Levels.Length < 2) return rows;
        var draws = new SplitMix(SplitMix.Mix(SeedOf(place, date), 7u, 7u));
        var left = new List<string>();
        int low = tables.Levels[0], high = tables.Levels[1];
        for (int slot = 0; slot < SlotWeights.Count; slot++)
        {
            if (left.Count == 0) left.AddRange(pool);
            int i = draws.Int(left.Count);
            string species = left[i];
            left.RemoveAt(i);
            int min = low + slot * Math.Max(0, high - low - 2) / (SlotWeights.Count - 1);
            rows.Add(new WildEncounterEntry { SpeciesName = species, MinLevel = min, MaxLevel = Math.Min(high, min + 2), Weight = SlotWeights[slot] });
        }
        return rows;
    }

    /// <summary>The daily flag of a find, cleared by a new day as every <c>FLAG_DAILY_</c> flag is.</summary>
    public static string FindFlag(SkyPlace place, int find) => $"FLAG_DAILY_SKY_{place.Region.ToUpperInvariant()}_{place.Index}_FIND_{find}";

    /// <summary>
    /// A place's finds on a date: two or three items of the list on open tiles of its floor, none on the beacon's tile,
    /// worked out from its seed.
    /// </summary>
    public static IReadOnlyList<SkyFind> FindsOf(SkyPlace place, DateTime date)
    {
        var floor = Lay(place, date);
        var beacon = BeaconOf(floor);
        var draws = new SplitMix(SplitMix.Mix(SeedOf(place, date), 11u, 3u));
        var open = new List<(int X, int Y)>();
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                if (floor.Inside(x, y) && floor.IsOpen(x, y) && (x, y) != beacon && Math.Abs(x - beacon.X) + Math.Abs(y - beacon.Y) > 2) open.Add((x, y));
        var finds = new List<SkyFind>();
        if (Data.Finds.Count == 0 || open.Count == 0) return finds;
        int count = 2 + draws.Int(2);
        for (int i = 0; i < count && open.Count > 0; i++)
        {
            int at = draws.Int(open.Count);
            var (fx, fy) = open[at];
            open.RemoveAt(at);
            finds.Add(new SkyFind(Data.Finds[draws.Int(Data.Finds.Count)], fx, fy, FindFlag(place, i)));
        }
        return finds;
    }

    /// <summary>
    /// A place of the day as a map file: the floor of its kind, its edge round it, its beacon's tile, its Pokémon of the day
    /// (seen whatever the options say) and its finds as item balls behind their daily flags.
    /// </summary>
    public static MapFile ToFile(SkyPlace place, DateTime date, string displayName)
    {
        var floor = Lay(place, date);
        var (ground, patch, block, round, cave) = TilesOf(place.Kind);
        var pieces = PiecesOf(place.Kind);
        var rows = new List<string>();
        var solid = new List<string>();
        uint seed = SeedOf(place, date);
        for (int y = 0; y < Height; y++)
        {
            var g = new char[Width];
            var s = new char[Width];
            for (int x = 0; x < Width; x++)
            {
                char g1 = round, s1 = round == '~' ? '.' : '#';
                if (floor.Inside(x, y))
                {
                    if (floor.IsOpen(x, y)) (g1, s1) = (SplitMix.Mix(seed, (uint)(x / 3), (uint)(y / 3)) % 3 == 0 ? patch : ground, '.');
                    else
                    {
                        char piece = PieceAt(floor, pieces, x, y);
                        (g1, s1) = piece == '~' ? ('~', '.') : (block, '#');
                    }
                }
                g[x] = g1;
                s[x] = s1;
            }
            rows.Add(new string(g));
            solid.Add(new string(s));
        }
        var (bx, by) = BeaconOf(floor);
        // The beacon's tile is plain ground
        var beaconRow = rows[by].ToCharArray();
        beaconRow[bx] = ground;
        rows[by] = new string(beaconRow);
        var npcs = FindsOf(place, date).Select((f, i) => new MapFile.NpcRecord
        {
            Name = $"sky_find_{i}", NpcType = "ItemBall", X = f.X, Y = f.Y, Item = f.Item, Count = 1, HiddenBy = f.Flag
        }).ToList();
        return new MapFile
        {
            Name = MapNameOf(place.Region),
            DisplayName = displayName,
            BgmTrack = cave ? "sinnoh/cave" : "sinnoh/route_209",
            BattleArena = place.Kind switch
            {
                SkyPlaceKind.Islet => BattleArena.Sand,
                SkyPlaceKind.Grove => BattleArena.Forest,
                SkyPlaceKind.Hollow => BattleArena.Cave,
                _ => BattleArena.Grass
            },
            Setting = cave ? MapSetting.Cave : null,
            WildVisible = true,
            Width = Width,
            Height = Height,
            Ground = rows,
            Solid = solid,
            Npcs = npcs,
            WildEncounters = Table(place, date).ToList()
        };
    }

    private static char PieceAt(FloorLayout grid, IReadOnlyList<string[]> pieces, int x, int y)
    {
        int sx = (x - grid.Left) / grid.Square, sy = (y - grid.Top) / grid.Square;
        var piece = pieces[grid.Layout[sy * grid.Squares + sx]];
        return piece[(y - grid.Top) % grid.Square][(x - grid.Left) % grid.Square];
    }

    // A Random over SplitMix, so FloorLayout's draws are the seed's alone
    private sealed class SeededDraws : Random
    {
        private readonly SplitMix mix;
        public SeededDraws(uint seed) : base(0) => mix = new SplitMix(seed);
        public override int Next(int maxValue) => mix.Int(maxValue);
        public override int Next() => (int)(mix.Next() >> 1);
        public override int Next(int minValue, int maxValue) => minValue + mix.Int(maxValue - minValue);
        public override double NextDouble() => mix.Float();
        protected override double Sample() => mix.Float();
    }
}
