using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Models.Frontier;

/// <summary>
/// A floor of the Battle Pyramid (plan 14 · B10; <c>docs/mechanics/rulings.md</c>, "The Battle Pyramid"; style guide,
/// "Caves", "The Battle Pyramid's dark"), made from a seed alone, so the same seed gives the same floor every time.
/// Emerald's floor is 32 by 32 tiles cut into a grid of four by four squares of eight, each square one of a set of
/// layouts (<c>src/battle_pyramid.c</c>, <c>GetPyramidFloorLayoutOffsets</c>); ours is laid the same way from pieces of
/// our own (<see cref="Pieces"/>), every one open at the middle two tiles of each side, so that any square joins its
/// neighbours. How many trainers and finds a floor has is Emerald's template table by the floor
/// (<c>sPyramidFloorTemplates</c>, <c>sPyramidFloorTemplateOptions</c>). The way in and the stairs up lie in two different
/// squares; trainers stand where they never close the way between them. No drawing or input.
/// </summary>
public sealed class PyramidFloor
{
    /// <summary>The map every floor is laid on (<c>Data/maps/BattlePyramidFloor.json</c> is the floor before any challenge).</summary>
    public const string MapName = "BattlePyramidFloor";

    /// <summary>A square's side, the squares to a side, and where the floor's first tile lies on the map (a wall round it, the back wall three tiles deep).</summary>
    public const int Square = 8, Squares = 4, Left = 1, Top = 3;
    public const int Width = Square * Squares + 2, Height = Square * Squares + Top + 1;

    /// <summary>
    /// Our own pieces of eight by eight: '.' floor and 'R' rock. Each is open at columns 3 and 4 of its top and bottom
    /// rows and rows 3 and 4 of its sides, and all its floor is one piece (<c>BattlePyramidTests</c> holds both).
    /// </summary>
    public static IReadOnlyList<string[]> Pieces => new[]
    {
        new[] { "........", "........", "........", "........", "........", "........", "........", "........" },
        new[] { "........", ".RR..RR.", ".RR..RR.", "........", "........", ".RR..RR.", ".RR..RR.", "........" },
        new[] { "RRR..RRR", "R......R", "R......R", "........", "........", "R......R", "R......R", "RRR..RRR" },
        new[] { "........", "........", "..RRRR..", "..RRRR..", "..RRRR..", "..RRRR..", "........", "........" },
        new[] { ".R......", ".R..RRR.", ".R......", "........", "........", "......R.", ".RRR..R.", "......R." },
        new[] { "........", ".RR..RR.", ".R....R.", "........", "........", ".R....R.", ".RR..RR.", "........" },
        new[] { ".....R..", ".RR..R..", ".R....R.", ".R.RR...", "...RR.R.", ".R....R.", ".RR...R.", "R......R" },
        new[] { "R.......", ".R......", "..R.....", "........", "........", ".....R..", "......R.", ".......R" }
    };

    /// <summary>Emerald's templates (<c>sPyramidFloorTemplates</c>): the finds and the trainers of a floor.</summary>
    public static readonly (int Items, int Trainers)[] Templates =
    {
        (7, 3), (6, 3), (5, 3), (4, 4), (4, 4), (3, 5), (3, 5), (2, 4), (4, 5), (3, 6), (2, 3), (4, 5), (3, 7), (2, 4), (3, 6), (3, 8)
    };

    /// <summary>Which template a floor takes (<c>sPyramidFloorTemplateOptions</c>, <c>GetPyramidFloorTemplateId</c>): a draw of a hundred against the floor's row of limits.</summary>
    public static readonly (int Below, int Template)[][] TemplateOptions =
    {
        new[] { (40, 0), (70, 1), (90, 2), (100, 3) },
        new[] { (35, 1), (55, 2), (75, 3), (90, 4), (100, 10) },
        new[] { (35, 2), (55, 3), (75, 4), (90, 5), (100, 11) },
        new[] { (35, 3), (55, 4), (75, 5), (90, 6), (100, 12) },
        new[] { (35, 4), (55, 5), (75, 6), (90, 7), (100, 13) },
        new[] { (35, 5), (55, 6), (75, 7), (90, 8), (100, 14) },
        new[] { (35, 6), (55, 7), (75, 8), (90, 9), (100, 15) }
    };

    /// <summary>The template a floor (0 to 6) takes for a draw of a hundred.</summary>
    public static int TemplateFor(int floor, int rand)
    {
        foreach (var (below, template) in TemplateOptions[Math.Clamp(floor, 0, 6)])
            if (rand < below) return template;
        return 0;
    }

    /// <summary>How far round the way in no trainer is put, so none sees the player as they arrive.</summary>
    public const int ClearOfTheWayIn = 3;

    /// <summary>How far a floor's trainers see along the way they face.</summary>
    public const int Sight = 3;

    public int Seed { get; }
    public int Floor { get; }
    public int Template { get; }

    /// <summary>The piece of each of the sixteen squares, row by row.</summary>
    public int[] Layout { get; }
    public int EntranceSquare { get; }
    public int ExitSquare { get; }

    /// <summary>Where the player comes onto the floor, and the stairs up, in the map's tiles.</summary>
    public (int X, int Y) Entrance { get; }
    public (int X, int Y) Exit { get; }

    /// <summary>The floor's trainers, where they stand and the way they look, and its finds, hidden in the floor.</summary>
    public List<(int X, int Y, Direction Facing)> Trainers { get; } = new();
    public List<(int X, int Y)> Finds { get; } = new();

    private readonly bool[] open = new bool[Width * Height];

    /// <summary>Whether a tile of the map is the floor's open ground.</summary>
    public bool IsOpen(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height && open[y * Width + x];

    private PyramidFloor(int seed, int floor, Random rng)
    {
        Seed = seed;
        Floor = floor;
        Template = TemplateFor(floor, rng.Next(100));
        Layout = Enumerable.Range(0, Squares * Squares).Select(_ => rng.Next(Pieces.Count)).ToArray();
        for (int s = 0; s < Layout.Length; s++)
        {
            var piece = Pieces[Layout[s]];
            int sx = Left + s % Squares * Square, sy = Top + s / Squares * Square;
            for (int y = 0; y < Square; y++)
                for (int x = 0; x < Square; x++)
                    open[(sy + y) * Width + sx + x] = piece[y][x] == '.';
        }
        EntranceSquare = rng.Next(Squares * Squares);
        do ExitSquare = rng.Next(Squares * Squares); while (ExitSquare == EntranceSquare);
        Entrance = MiddleOf(EntranceSquare);
        Exit = MiddleOf(ExitSquare);

        // The trainers, each where the way from the entrance to the stairs stays open with them standing
        var (_, trainers) = Templates[Template];
        var (items, _) = Templates[Template];
        var reach = Reachable(Entrance, new HashSet<(int, int)>());
        var taken = new HashSet<(int, int)> { Entrance, Exit };
        var spots = reach.Where(t => !taken.Contains(t)).OrderBy(t => t.Y).ThenBy(t => t.X).ToList();
        for (int n = 0, guard = 0; n < trainers && guard < 400; guard++)
        {
            var at = spots[rng.Next(spots.Count)];
            if (taken.Contains(at) || Math.Max(Math.Abs(at.X - Entrance.X), Math.Abs(at.Y - Entrance.Y)) <= ClearOfTheWayIn) continue;
            var blocked = new HashSet<(int, int)>(Trainers.Select(t => (t.X, t.Y))) { at };
            if (!Reachable(Entrance, blocked).Contains(Exit)) continue;
            Trainers.Add((at.X, at.Y, (Direction)rng.Next(4)));
            taken.Add(at);
            n++;
        }
        for (int n = 0, guard = 0; n < items && guard < 400; guard++)
        {
            var at = spots[rng.Next(spots.Count)];
            if (!taken.Add(at)) continue;
            Finds.Add(at);
            n++;
        }
    }

    /// <summary>The floor a seed makes, for a floor of the round (0 to 6).</summary>
    public static PyramidFloor Generate(int seed, int floor) => new(seed, floor, new Random(seed));

    // The open tile nearest a square's middle
    private (int X, int Y) MiddleOf(int square)
    {
        int sx = Left + square % Squares * Square, sy = Top + square / Squares * Square;
        (int X, int Y) best = (sx + 3, sy + 3);
        int bestD = int.MaxValue;
        for (int y = sy; y < sy + Square; y++)
            for (int x = sx; x < sx + Square; x++)
            {
                if (!IsOpen(x, y)) continue;
                int d = Math.Abs(2 * x - (2 * sx + Square - 1)) + Math.Abs(2 * y - (2 * sy + Square - 1));
                if (d < bestD) { bestD = d; best = (x, y); }
            }
        return best;
    }

    /// <summary>Every open tile walked to from a tile, by the four ways, past none of the tiles named.</summary>
    public HashSet<(int X, int Y)> Reachable((int X, int Y) from, IReadOnlySet<(int, int)> blocked)
    {
        var seen = new HashSet<(int X, int Y)> { from };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            foreach (var (dx, dy) in new[] { (0, -1), (0, 1), (-1, 0), (1, 0) })
            {
                var next = (x + dx, y + dy);
                if (!IsOpen(next.Item1, next.Item2) || blocked.Contains(next) || !seen.Add(next)) continue;
                queue.Enqueue(next);
            }
        }
        return seen;
    }

    /// <summary>
    /// The floor as a hand-made map's file (plan 14 · B10): the cave's floor and its rocks inside a hall's walls, the stairs
    /// up a trigger of the floor's script (<c>Stairs</c>), the finds hidden under flags that last only while the player
    /// is on the floor, the wild Pokémon handed in. Its trainers are people the challenge stands on it as the map is made.
    /// </summary>
    public MapFile ToFile(IReadOnlyList<string> finds, IReadOnlyList<WildEncounterEntry> wild)
    {
        var ground = new List<string>();
        var solid = new List<string>();
        for (int y = 0; y < Height; y++)
        {
            var g = new char[Width];
            var s = new char[Width];
            for (int x = 0; x < Width; x++)
            {
                bool inside = x >= Left && x < Left + Square * Squares && y >= Top && y < Top + Square * Squares;
                if (!inside) { g[x] = '#'; s[x] = '#'; continue; }
                if ((x, y) == Exit) { g[x] = 's'; s[x] = '.'; continue; }
                g[x] = IsOpen(x, y) ? 'c' : 'R';
                s[x] = IsOpen(x, y) ? '.' : '#';
            }
            ground.Add(new string(g));
            solid.Add(new string(s));
        }
        return new MapFile
        {
            Name = MapName,
            DisplayName = "Battle Pyramid",
            BgmTrack = "sinnoh/turnback_cave",
            Interior = InteriorStyle.Gym,
            BattleArena = BattleArena.Cave,
            Width = Width,
            Height = Height,
            Ground = ground,
            Solid = solid,
            Triggers = new List<MapFile.TriggerRecord> { new() { X = Exit.X, Y = Exit.Y, Script = "Stairs" } },
            HiddenItems = Finds.Select((f, i) => new MapFile.HiddenItemRecord { X = f.X, Y = f.Y, Item = finds[i % Math.Max(1, finds.Count)], Flag = FindFlag(i) }).ToList(),
            WildEncounters = wild.ToList()
        };
    }

    /// <summary>The flag a find of the floor is taken under: one that lasts only while the player stays on the floor.</summary>
    public static string FindFlag(int i) => $"FLAG_MAP_LOCAL_PYRAMID_FIND_{i + 1}";
}
