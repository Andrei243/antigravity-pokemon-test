using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models.Delve;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Models.Underground;

/// <summary>
/// One of the twelve hideaways under the Underground (plan 38 · EX6): a habitat of two or three types, its grounds, its
/// seed and the tile of the tunnels its way down opens in.
/// </summary>
/// <param name="Floor">The ground of its floor, a <see cref="TileCodes"/> character: cave floor, sand, snow or earth.</param>
/// <param name="Patch">The ground laid in patches over the floor where the seed's noise is high.</param>
/// <param name="Pools">Whether its pieces' pools are water; in a hideaway of no water they are rock.</param>
public sealed record Hideaway(int Number, string Map, string Name, PokemonType[] Types, char Floor, char Patch, bool Pools,
    uint Seed, int DownX, int DownZ);

/// <summary>
/// The hideaways under the Underground (plan 38 · EX6; <c>docs/mechanics/rulings.md</c>, "The field beyond the tables";
/// style guide, "Caves", "Hideaways"). Each is laid by rule from its own seed, the Battle Pyramid's way
/// (<see cref="FloorLayout"/>): three squares by three of eight tiles, each square one of <see cref="Pieces"/>, inside
/// the cave's rock; its ground is the field's own, laid by the seed's noise. It is written as a map file that says it is
/// a cave (<see cref="MapFile.Setting"/>) and made as the maps load (<see cref="MapDatabase"/>), with a way out in its
/// back wall to the tile of the tunnels below its way down, two ways down in each of the six tunnel systems
/// (<see cref="LayWaysDown"/>), shut until Sinnoh's Hall of Fame (<see cref="OpenedBy"/>). Its Pokémon are those of its
/// types, Sinnoh's Pokédex's before the National Pokédex and Generations 1 to 4's with it, never a legendary or a
/// mythical, at the Battle Zone's band, twelve a day chosen by the day's number (<see cref="Table"/>), never a draw of
/// <see cref="Dice"/>. No drawing or input.
/// </summary>
public static class Hideaways
{
    /// <summary>A square's side, the squares to a side, and where the grid's first tile lies (two tiles of rock to the west, the back wall three deep).</summary>
    public const int Square = 8, Squares = 3, Left = 2, Top = 3;
    public const int Width = Left + Square * Squares + 2, Height = Top + Square * Squares + 2;

    /// <summary>The way out, in the back wall over the middle of the north row's middle square, and where whoever comes down stands.</summary>
    public const int OutX = Left + Square + 3, OutY = Top - 1;
    public const int ArriveX = OutX, ArriveY = Top;

    /// <summary>What opens the ways down: Sinnoh's Hall of Fame (the rulings say why not the National Pokédex).</summary>
    public const string OpenedBy = "FLAG_GAME_COMPLETED";

    /// <summary>The Battle Zone's band of levels (<c>levels.txt</c>: its grass is 47 to 55).</summary>
    public const int LowestLevel = 47, HighestLevel = 55;

    /// <summary>The weights of a table's twelve slots: Platinum's for its grass.</summary>
    public static readonly int[] SlotWeights = { 20, 20, 10, 10, 10, 10, 5, 5, 4, 4, 1, 1 };

    /// <summary>
    /// Our own pieces of eight by eight: '.' floor, 'R' rock and '~' a pool. Each is open at columns 3 and 4 of its top
    /// and bottom rows and rows 3 and 4 of its sides, its floor is one piece without its pools, and no pool lies on a
    /// joint (<c>HideawayTests</c> holds all three).
    /// </summary>
    public static IReadOnlyList<string[]> Pieces => new[]
    {
        new[] { "........", "........", "........", "........", "........", "........", "........", "........" },
        new[] { "........", ".RR..RR.", ".RR..RR.", "........", "........", ".RR..RR.", ".RR..RR.", "........" },
        new[] { "........", "........", "..~~~~..", "..~~~~..", "..~~~~..", "..~~~~..", "........", "........" },
        new[] { "~~......", "~~......", "........", "........", "........", "........", "......~~", "......~~" },
        new[] { "RR....RR", "R......R", "........", "...RR...", "...RR...", "........", "R......R", "RR....RR" },
        new[] { "........", ".~~~....", ".~~~.RR.", "......R.", "........", ".RR.....", ".....~~.", ".....~~." },
        new[] { ".R....R.", ".R.RR.R.", "........", "..R..R..", "........", ".RR..RR.", "......R.", "R......R" },
        new[] { "........", "........", "~~~..~~~", "........", "........", "~~~..~~~", "........", "........" }
    };

    /// <summary>The twelve, in order, each with its tunnel system's way down (two in each of the six; <c>HideawayTests</c> holds the pairs).</summary>
    public static IReadOnlyList<Hideaway> All { get; } = new Hideaway[]
    {
        new(1, "HideawayFernGrotto", "Fern Grotto", new[] { PokemonType.Grass, PokemonType.Bug }, 'c', 'w', true, 0x5EED0101, 64, 116),
        new(2, "HideawayLakeCavern", "Lake Cavern", new[] { PokemonType.Water, PokemonType.Ice }, 'c', '*', true, 0x5EED0202, 230, 69),
        new(3, "HideawayCrystalHall", "Crystal Hall", new[] { PokemonType.Rock, PokemonType.Psychic }, 'c', ',', false, 0x5EED0303, 370, 148),
        new(4, "HideawayIceHall", "Ice Hall", new[] { PokemonType.Ice, PokemonType.Steel }, '^', ',', false, 0x5EED0404, 421, 180),
        new(5, "HideawayEmberHall", "Ember Hall", new[] { PokemonType.Fire, PokemonType.Fighting }, ';', ',', false, 0x5EED0505, 196, 197),
        new(6, "HideawaySandHall", "Sand Hall", new[] { PokemonType.Ground, PokemonType.Rock }, ',', ';', false, 0x5EED0606, 304, 308),
        new(7, "HideawayShadeHollow", "Shade Hollow", new[] { PokemonType.Ghost, PokemonType.Dark }, 'c', 'w', false, 0x5EED0707, 259, 261),
        new(8, "HideawaySparkVein", "Spark Vein", new[] { PokemonType.Electric, PokemonType.Steel }, ';', 'c', false, 0x5EED0808, 271, 276),
        new(9, "HideawaySkyShaft", "Sky Shaft", new[] { PokemonType.Flying, PokemonType.Normal }, 'c', 'w', false, 0x5EED0909, 80, 357),
        new(10, "HideawayMirePool", "Mire Pool", new[] { PokemonType.Poison, PokemonType.Water }, 'c', 'm', true, 0x5EED0A0A, 189, 421),
        new(11, "HideawayDragonsDeep", "Dragon's Deep", new[] { PokemonType.Dragon, PokemonType.Ground }, 'c', ',', true, 0x5EED0B0B, 397, 436),
        new(12, "HideawayEchoDen", "Echo Den", new[] { PokemonType.Normal, PokemonType.Fighting, PokemonType.Psychic }, ';', 'w', false, 0x5EED0C0C, 343, 453),
    };

    /// <summary>Every hideaway's map's name.</summary>
    public static IEnumerable<string> MapNames => All.Select(h => h.Map);

    /// <summary>The hideaway of a map's name, or null.</summary>
    public static Hideaway? OfMap(string? map) => All.FirstOrDefault(h => string.Equals(h.Map, map, StringComparison.OrdinalIgnoreCase));

    /// <summary>The squares of a hideaway, laid from its seed alone: one draw of the pieces' count for each square in turn.</summary>
    public static FloorLayout Lay(Hideaway h) =>
        FloorLayout.Lay(Pieces, Square, Squares, Left, Top, Width, Height, new SeededDraws(h.Seed));

    /// <summary>The ground of a tile of the floor: the patch where the seed's noise is high, never on a square's joint.</summary>
    public static char GroundOf(Hideaway h, int x, int y)
    {
        if (OnAJoint(x, y)) return h.Floor;
        return Noise(h.Seed, x, y) > 0.58f ? h.Patch : h.Floor;
    }

    /// <summary>Whether a tile is one of the two at the middle of a square's side, where squares join.</summary>
    public static bool OnAJoint(int x, int y)
    {
        int lx = (x - Left) % Square, ly = (y - Top) % Square;
        if (x < Left || y < Top) return false;
        bool midX = lx is 3 or 4, midY = ly is 3 or 4;
        return (midX && ly is 0 or Square - 1) || (midY && lx is 0 or Square - 1);
    }

    // Smooth noise over cells of four tiles, the corners decided by the seed and the place alone
    private static float Noise(uint seed, int x, int y)
    {
        float Corner(int cx, int cy) => SplitMix.Mix(seed, (uint)cx, (uint)cy) / (float)uint.MaxValue;
        int cx = x / 4, cy = y / 4;
        float fx = (x % 4) / 4f, fy = (y % 4) / 4f;
        float top = Corner(cx, cy) * (1 - fx) + Corner(cx + 1, cy) * fx;
        float bottom = Corner(cx, cy + 1) * (1 - fx) + Corner(cx + 1, cy + 1) * fx;
        return top * (1 - fy) + bottom * fy;
    }

    /// <summary>
    /// A hideaway as a map file: the cave's rock (<c>X</c>) round the floor, the floor's grounds, its pools, the way out in
    /// the back wall, its Pokémon of a day, seen whatever the options say.
    /// </summary>
    public static MapFile ToFile(Hideaway h, int day = 0, bool national = false)
    {
        var grid = Lay(h);
        var ground = new List<string>();
        var solid = new List<string>();
        for (int y = 0; y < Height; y++)
        {
            var g = new char[Width];
            var s = new char[Width];
            for (int x = 0; x < Width; x++)
            {
                char g1 = 'X', s1 = '#';
                if (x == OutX && y == OutY) (g1, s1) = ('M', '.');
                else if (grid.Inside(x, y))
                {
                    if (grid.IsOpen(x, y)) (g1, s1) = (GroundOf(h, x, y), '.');
                    else if (PieceAt(grid, x, y) == '~' && h.Pools) (g1, s1) = ('~', '.');
                }
                g[x] = g1;
                s[x] = s1;
            }
            ground.Add(new string(g));
            solid.Add(new string(s));
        }
        return new MapFile
        {
            Name = h.Map,
            DisplayName = h.Name,
            BgmTrack = "sinnoh/cave",
            BattleArena = BattleArena.Cave,
            Setting = MapSetting.Cave,
            WildVisible = true,
            Width = Width,
            Height = Height,
            Ground = ground,
            Solid = solid,
            Warps = new List<Warp>
            {
                new() { SourceX = OutX, SourceY = OutY, TargetMap = UndergroundMapName, TargetX = h.DownX, TargetY = h.DownZ + 1, TargetFacing = Direction.Down }
            },
            WildEncounters = Table(h, day, national).ToList()
        };
    }

    /// <summary>The Underground's map, which the ways down are laid in.</summary>
    public const string UndergroundMapName = "Underground";

    private static char PieceAt(FloorLayout grid, int x, int y)
    {
        int sx = (x - grid.Left) / grid.Square, sy = (y - grid.Top) / grid.Square;
        var piece = grid.Pieces[grid.Layout[sy * grid.Squares + sx]];
        return piece[(y - grid.Top) % grid.Square][(x - grid.Left) % grid.Square];
    }

    /// <summary>
    /// The ways down laid in the tunnels (made as the maps load): each a cave's mouth in the wall at the floor's height
    /// with a warp into its hideaway, shut by <see cref="OpenedBy"/> (<see cref="Map.ApplyDoors"/>, and
    /// <see cref="ShowWaysDown"/> as the tunnels are come to).
    /// </summary>
    public static void LayWaysDown(Map underground)
    {
        foreach (var h in All)
        {
            if (!underground.InBounds(h.DownX, h.DownZ)) continue;
            underground.SetGroundTile(h.DownX, h.DownZ, TileType.CaveMouth, isSolid: false);
            underground.SetHeight(h.DownX, h.DownZ, underground.HeightAt(h.DownX, h.DownZ + 1));
            underground.Warps.Add(new Warp
            {
                SourceX = h.DownX, SourceY = h.DownZ, TargetMap = h.Map, TargetX = ArriveX, TargetY = ArriveY,
                TargetFacing = Direction.Down, OpenedBy = OpenedBy
            });
        }
    }

    /// <summary>The ways down as the story has them: a cave's mouth once Sinnoh's Hall of Fame is set, the tunnels' rock before.</summary>
    /// <returns>The tiles whose look changed.</returns>
    public static List<(int X, int Z)> ShowWaysDown(Map underground, bool open)
    {
        var changed = new List<(int, int)>();
        foreach (var h in All)
        {
            if (!underground.InBounds(h.DownX, h.DownZ)) continue;
            var want = open ? TileType.CaveMouth : TileType.CaveWall;
            if (underground.GetGroundTile(h.DownX, h.DownZ) == want) continue;
            float floor = underground.HeightAt(h.DownX, h.DownZ + 1);
            underground.SetGroundTile(h.DownX, h.DownZ, want, isSolid: !open);
            underground.SetHeight(h.DownX, h.DownZ, open ? floor : floor + WorldMapBuilder.CaveWallRise);
            changed.Add((h.DownX, h.DownZ));
        }
        return changed;
    }

    /// <summary>Whether a tile of the tunnels is a way down: rock to the tunnels' own rules (the day's spread, a trap), open or not.</summary>
    public static bool IsWayDown(int x, int z) => All.Any(h => h.DownX == x && h.DownZ == z);

    // ------------------------------------------------------------------ the Pokémon

    /// <summary>A species' types as the modern rules have them, whatever rules are in force (its data's, never the swapped fields).</summary>
    public static IReadOnlyList<PokemonType> TypesOf(PokemonSpecies s) =>
        s.Modern?.Types is { Count: > 0 } modern ? modern : s.SecondaryType is { } second ? new[] { s.PrimaryType, second } : new[] { s.PrimaryType };

    /// <summary>Whether a species would have evolved by the band's levels (by a level under it), or is a baby that grows by friendship.</summary>
    private static bool GrowsBeforeTheBand(PokemonSpecies s) =>
        (s.Evolutions ?? new()).Any(e => e.Method.ToString().StartsWith("Friendship", StringComparison.Ordinal)
            || e.Method.ToString().StartsWith("Level", StringComparison.Ordinal) && e.Level > 0 && e.Level < LowestLevel);

    /// <summary>
    /// The species a hideaway may hold: of one of its types, of Generations 1 to 4 (Sinnoh's Pokédex alone before the
    /// National Pokédex), never a legendary or a mythical, nor one that would have grown before the band; in National order.
    /// </summary>
    public static IReadOnlyList<string> Pool(Hideaway h, bool national) =>
        PokemonDatabase.GetAll()
            .Where(s => s.DexNumber is >= 1 and <= 493 && !s.Legendary && !s.Mythical)
            .Where(s => national || s.SinnohNumber is > 0)
            .Where(s => TypesOf(s).Any(t => h.Types.Contains(t)))
            .Where(s => !GrowsBeforeTheBand(s))
            .OrderBy(s => s.DexNumber).Select(s => s.Name).ToList();

    /// <summary>
    /// A day's twelve slots: species of the pool chosen by the hideaway's seed and the day's number (no two alike while the
    /// pool has twelve), with Platinum's grass weights and levels across the band.
    /// </summary>
    public static IReadOnlyList<WildEncounterEntry> Table(Hideaway h, int day, bool national)
    {
        var pool = Pool(h, national).ToList();
        var rows = new List<WildEncounterEntry>();
        if (pool.Count == 0) return rows;
        var draws = new SplitMix(SplitMix.Mix(h.Seed, (uint)day, national ? 1u : 0u));
        var left = new List<string>(pool);
        for (int slot = 0; slot < SlotWeights.Length; slot++)
        {
            if (left.Count == 0) left.AddRange(pool);
            int i = draws.Int(left.Count);
            string species = left[i];
            left.RemoveAt(i);
            int min = LowestLevel + slot * 6 / (SlotWeights.Length - 1);
            rows.Add(new WildEncounterEntry { SpeciesName = species, MinLevel = min, MaxLevel = Math.Min(HighestLevel, min + 2), Weight = SlotWeights[slot] });
        }
        return rows;
    }

    /// <summary>A hideaway's slots laid for the day on its map, as the player comes in (the map keeps the last day's until then).</summary>
    public static void Stock(Map map, Hideaway h, int day, bool national)
    {
        map.WildEncounters.Clear();
        map.WildEncounters.AddRange(Table(h, day, national));
    }

    /// <summary>The cell of the region's map over a tile of the tunnels (<see cref="UndergroundField.Below"/> the other way round), for the Pokédex's area page.</summary>
    public static (int X, int Y) CellOver(int x, int z)
    {
        const int t = 32;
        int cx = (x / t - 1) * 2 + 1 + (x % t >= 16 ? 1 : 0);
        int cy = (z / t - 3) * 2 + 6 + (z % t >= 16 ? 1 : 0);
        return (Math.Max(0, cx), Math.Max(0, cy));
    }

    /// <summary>A <see cref="Random"/> whose every draw is SplitMix's from a seed, so a hideaway is laid the same on every machine and every .NET.</summary>
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
