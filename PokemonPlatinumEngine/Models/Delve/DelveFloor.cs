using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Models.Delve;

/// <summary>A tile of a delve's floor.</summary>
public enum DelveTile
{
    Floor,
    Wall,
    /// <summary>Water only a Water type crosses.</summary>
    Pool
}

/// <summary>A snare hidden in a floor (ours).</summary>
public enum SnareKind
{
    /// <summary>Sends whoever steps on it to sleep for three turns.</summary>
    Sleep,
    /// <summary>Poisons whoever steps on it.</summary>
    Poison,
    /// <summary>Drops the team to the next floor (never on the last).</summary>
    Drop
}

/// <summary>A resident as the floor places it: its species, its tile, whether it sleeps until the team comes near, and whether it flees when low.</summary>
public sealed record ResidentSpawn(string Species, int X, int Y, bool Asleep, bool Shy);

/// <summary>
/// A floor of a hollow (plan 44 · SD7; <c>docs/mechanics/rulings.md</c>, "The delve"), made from its seed alone: four
/// squares by four of eight tiles laid by <see cref="FloorLayout"/> from our own pieces (<see cref="Pieces"/>), the way in
/// and the stairs in two different squares (<see cref="FloorLayout.ChooseWays"/>), then its residents, finds and snares on
/// open ground the way in reaches. A piece's 'P' cells are pools in a sea cave and rock elsewhere; they never stand on the
/// floor's own open ground, so no pool cuts the way from the way in to the stairs. No drawing or input.
/// </summary>
public sealed class DelveFloor
{
    /// <summary>The map every floor is drawn on (SD8).</summary>
    public const string MapName = "DelveFloor";

    /// <summary>A square's side, the squares to a side, and where the floor's first tile lies on the map (a wall round it).</summary>
    public const int Square = 8, Squares = 4, Left = 1, Top = 3;
    public const int Width = Square * Squares + 2, Height = Square * Squares + Top + 1;

    /// <summary>How far round the way in no resident is put.</summary>
    public const int ClearOfTheWayIn = 4;

    /// <summary>
    /// Our own pieces of eight by eight: '.' floor, 'R' wall, 'P' a pool in a sea cave and wall elsewhere. Each is open at
    /// columns 3 and 4 of its top and bottom rows and rows 3 and 4 of its sides, and all its floor is one piece
    /// (<c>DelveTests</c> holds both).
    /// </summary>
    public static IReadOnlyList<string[]> Pieces => new[]
    {
        new[] { "........", "........", "........", "........", "........", "........", "........", "........" },
        new[] { "........", ".R....R.", "..R..R..", "........", "........", "..R..R..", ".R....R.", "........" },
        new[] { "........", "........", "..PPPP..", "..PPPP..", "..PPPP..", "..PPPP..", "........", "........" },
        new[] { "RR....RR", "RR....RR", "........", "........", "........", "........", "RR....RR", "RR....RR" },
        new[] { "..R.....", ".R.R.RR.", ".R......", "........", "........", "......R.", ".RR.R.R.", ".....R.." },
        new[] { "R......R", ".RR..RR.", ".R....R.", "........", "........", ".R....R.", ".RR..RR.", "R......R" },
        new[] { "PP....PP", "P......P", "........", "........", "........", "........", "P......P", "PP....PP" }
    };

    public DelveHollow Hollow { get; }

    /// <summary>The floor's number, from nought.</summary>
    public int Index { get; }
    public uint Seed { get; }
    public FloorLayout Grid { get; }
    public (int X, int Y) Entrance { get; }
    public (int X, int Y) Stairs { get; }
    public List<ResidentSpawn> Residents { get; } = new();
    public List<(int X, int Y, string Item)> Finds { get; } = new();
    public List<(int X, int Y, SnareKind Kind)> Snares { get; } = new();

    /// <summary>Whether this is the hollow's last floor, whose stairs end the run.</summary>
    public bool IsLast => Index >= Hollow.Floors - 1;

    private DelveFloor(DelveHollow hollow, int index, uint seed)
    {
        Hollow = hollow;
        Index = index;
        Seed = seed;
        var rng = new DelveRandom(seed);
        Grid = FloorLayout.Lay(Pieces, Square, Squares, Left, Top, Width, Height, rng);
        var (way, on) = Grid.ChooseWays(rng);
        Entrance = Grid.MiddleOf(way);
        Stairs = Grid.MiddleOf(on);

        // Everything else stands on open ground the way in reaches, in a fixed order, never on the way in or the stairs
        var reach = Grid.Reachable(Entrance, new HashSet<(int, int)>());
        var spots = reach.OrderBy(t => t.Y).ThenBy(t => t.X).ToList();
        var taken = new HashSet<(int, int)> { Entrance, Stairs };
        for (int n = 0, guard = 0; n < hollow.ResidentsPerFloor && guard < 400; guard++)
        {
            var at = spots[rng.Next(spots.Count)];
            if (Math.Max(Math.Abs(at.X - Entrance.X), Math.Abs(at.Y - Entrance.Y)) <= ClearOfTheWayIn || !taken.Add(at)) continue;
            var species = hollow.Residents[rng.Next(hollow.Residents.Count)];
            Residents.Add(new ResidentSpawn(species, at.X, at.Y, Asleep: rng.Next(3) == 0, Shy: rng.Next(4) == 0));
            n++;
        }
        for (int n = 0, guard = 0; n < hollow.FindsPerFloor && guard < 400; guard++)
        {
            var at = spots[rng.Next(spots.Count)];
            if (!taken.Add(at)) continue;
            Finds.Add((at.X, at.Y, hollow.Finds[rng.Next(hollow.Finds.Count)]));
            n++;
        }
        int kinds = IsLast ? 2 : 3;
        for (int n = 0, guard = 0; n < hollow.SnaresPerFloor && guard < 400; guard++)
        {
            var at = spots[rng.Next(spots.Count)];
            if (!taken.Add(at)) continue;
            Snares.Add((at.X, at.Y, (SnareKind)rng.Next(kinds)));
            n++;
        }
    }

    /// <summary>The floor a seed makes, for a floor of the hollow (from nought).</summary>
    public static DelveFloor Generate(DelveHollow hollow, int index, uint seed) => new(hollow, index, seed);

    /// <summary>What a tile of the map is.</summary>
    public DelveTile TileAt(int x, int y)
    {
        if (Grid.IsOpen(x, y)) return DelveTile.Floor;
        if (!Grid.Inside(x, y) || Hollow.Theme != DelveTheme.SeaCave) return DelveTile.Wall;
        int s = (y - Top) / Square * Squares + (x - Left) / Square;
        return Pieces[Grid.Layout[s]][(y - Top) % Square][(x - Left) % Square] == 'P' ? DelveTile.Pool : DelveTile.Wall;
    }

    /// <summary>Whether a Pokémon may stand on a tile: floor, or a pool for a Water type.</summary>
    public bool Passable(int x, int y, bool swims) => TileAt(x, y) switch
    {
        DelveTile.Floor => true,
        DelveTile.Pool => swims,
        _ => false
    };

    /// <summary>
    /// The floor as a hand-made map's file (SD8 draws it): the cave's floor, the hollow's walls, its pools, the stairs
    /// (which the run's own rules take, so no script stands on them). Its residents and finds are the run's, laid on it
    /// as it is drawn.
    /// </summary>
    public MapFile ToFile()
    {
        char wall = Hollow.Theme switch { DelveTheme.Wood => 'T', DelveTheme.Tower => '#', _ => 'R' };
        var ground = new List<string>();
        var solid = new List<string>();
        for (int y = 0; y < Height; y++)
        {
            var g = new char[Width];
            var s = new char[Width];
            for (int x = 0; x < Width; x++)
            {
                if ((x, y) == Stairs) { g[x] = 's'; s[x] = '.'; continue; }
                var tile = TileAt(x, y);
                g[x] = tile switch { DelveTile.Floor => 'c', DelveTile.Pool => '~', _ => Grid.Inside(x, y) ? wall : '#' };
                s[x] = tile == DelveTile.Floor ? '.' : '#';
            }
            ground.Add(new string(g));
            solid.Add(new string(s));
        }
        return new MapFile
        {
            Name = MapName,
            DisplayName = Hollow.Key,
            Interior = InteriorStyle.Gym,
            BattleArena = BattleArena.Cave,
            Width = Width,
            Height = Height,
            Ground = ground,
            BgmTrack = Hollow.Song ?? "sinnoh/cave",
            Solid = solid
        };
    }
}
