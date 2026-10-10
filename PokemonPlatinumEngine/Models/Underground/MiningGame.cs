using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Underground;

/// <summary>Something set in the wall: which of the wall's objects it is, and the square of its north-west corner.</summary>
public sealed record WallObject(MiningObjectRecord Object, int Id, int X, int Y)
{
    /// <summary>Whether a square of the wall is part of it (<c>Mining_AreCoordinatesWithinObjectShape</c>).</summary>
    public bool Covers(int x, int y)
    {
        int dx = x - X, dy = y - Y;
        if (dx < 0 || dy < 0 || dx >= Object.Width || dy >= Object.Height) return false;
        return Object.Shape == null || Object.Shape[dy][dx] != 'o';
    }

    public bool IsRock => Id >= UndergroundDatabase.TreasureEnd;
}

/// <summary>What a blow came to: whether it rang on dark rock, whether it bared part of something, and whether it landed on the wall at all.</summary>
public readonly record struct MiningBlow(bool Landed, bool HitRock, bool FoundItem);

/// <summary>Something dug out whole: the thing, and for a Sphere its size.</summary>
public readonly record struct MiningFind(int Id, int SphereSize);

/// <summary>
/// The wall's game (<c>src/underground/mining.c</c>, plan 06 · R16a): a wall of thirteen squares by ten under two to
/// six layers of dirt, with two to four things set in it and dark rock between them. The hammer and the pick each
/// take dirt off in their own pattern and bring the wall nearer to falling in; whatever lies wholly bare when it
/// does, or once everything is bare, is the player's. Every draw is from the generator the game is given, in the
/// original's order. No drawing or input.
/// </summary>
public sealed class MiningGame
{
    public const int Width = 13, Height = 10;

    /// <summary><c>INITIAL_WALL_INTEGRITY</c>: the wall stands 196 points of blows, four for the pick and eight for the hammer.</summary>
    public const int InitialIntegrity = 196;
    public const int PickDamage = 4, HammerDamage = 8;
    public const int MaxItems = 4, MaxObjects = 8;

    private readonly Rand32 rand;
    private readonly int[,] dirt = new int[Height, Width];
    private readonly int[,] grid = new int[Height, Width];
    private readonly List<WallObject> objects = new();
    private readonly bool[] bare;

    /// <summary>What is set in the wall: the things to be found first, then the dark rocks.</summary>
    public IReadOnlyList<WallObject> Objects => objects;

    /// <summary>How many of <see cref="Objects"/> are things to be found (<c>itemCount</c>).</summary>
    public int ItemCount { get; }

    public int Integrity { get; private set; } = InitialIntegrity;

    /// <summary>The pick is in hand as the game begins (<c>pickaxeSelected</c>).</summary>
    public bool PickSelected { get; set; } = true;

    public bool Collapsed => Integrity == 0 && !AllDug;
    public bool AllDug { get; private set; }
    public bool Over => AllDug || Integrity == 0;

    /// <summary>How many layers of dirt lie on a square: 0 is bare.</summary>
    public int DirtAt(int x, int y) => dirt[y, x];

    /// <summary>The place in <see cref="Objects"/> of what lies under a square, or -1.</summary>
    public int ObjectAt(int x, int y) => grid[y, x] - 1;

    /// <summary>Whether a thing lies wholly bare: it shines, and is the player's when the game ends.</summary>
    public bool IsBare(int item) => item >= 0 && item < bare.Length && bare[item];

    /// <param name="oddId">Whether the trainer's ID number is odd, which a thing's odds hang on.</param>
    /// <param name="national">Whether the National Pokédex has been obtained, which they hang on too.</param>
    /// <param name="firstTime">The first wall ever dug at holds three things and no dark rock.</param>
    /// <param name="neverMined">Whether a thing may still be found: a plate is found once.</param>
    public MiningGame(Rand32 rand, bool oddId, bool national, bool firstTime, Func<int, bool> neverMined)
    {
        this.rand = rand;
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++) dirt[y, x] = 2;

        // Mining_GenerateGameLayout
        var table = UndergroundDatabase.Objects;
        int Weight(MiningObjectRecord o) => national ? (oddId ? o.OddNational : o.EvenNational) : (oddId ? o.Odd : o.Even);
        int firstRock = Enumerable.Range(0, table.Count).First(i => table[i].Id.StartsWith("ROCK_", StringComparison.Ordinal));
        int total = table.Take(firstRock).Sum(Weight), rocks = table.Count - firstRock;
        ItemCount = rand.Next(MaxItems - 1) + 2;
        if (firstTime) ItemCount = 3;
        var plates = new int[MaxItems];
        while (objects.Count < ItemCount)
        {
            // Mining_PickItem: the first thing the draw falls within, by the weights in the table's order
            int counter = rand.Next(total), index = 0;
            for (; index < firstRock; index++)
            {
                counter -= Weight(table[index]);
                if (counter < 0) break;
            }
            int id = UndergroundDatabase.TreasureId(table[index].Id);
            if (!neverMined(id)) continue;
            // No wall holds the same plate twice (the plate is noted before the place is tried, as in the original)
            plates[objects.Count] = UndergroundDatabase.IsPlate(id) ? id : 0;
            if (UndergroundDatabase.IsPlate(id) && plates.Take(objects.Count).Contains(id)) continue;
            int x = rand.Next(Width), y = rand.Next(Height);
            TryPlace(table[index], id, x, y);
        }
        if (!firstTime)
            for (int attempt = 0; attempt < 100; attempt++)
            {
                int index = rand.Next(rocks) + firstRock;
                int x = rand.Next(Width), y = rand.Next(Height);
                TryPlace(table[index], UndergroundDatabase.TreasureEnd + int.Parse(table[index].Id["ROCK_".Length..]) - 1, x, y);
            }
        bare = new bool[ItemCount];
        RandomizeDirt();
    }

    /// <summary><c>Mining_TryPlaceObject</c>: inside the wall, on squares nothing else covers, and only while one of the eight places is free.</summary>
    private bool TryPlace(MiningObjectRecord o, int id, int x, int y)
    {
        if (objects.Count >= MaxObjects || x + o.Width > Width || y + o.Height > Height) return false;
        var placed = new WallObject(o, id, x, y);
        for (int cx = x; cx < x + o.Width; cx++)
            for (int cy = y; cy < y + o.Height; cy++)
                if (placed.Covers(cx, cy) && grid[cy, cx] != 0) return false;
        objects.Add(placed);
        for (int cx = x; cx < x + o.Width; cx++)
            for (int cy = y; cy < y + o.Height; cy++)
                if (placed.Covers(cx, cy)) grid[cy, cx] = objects.Count;
        return true;
    }

    private static readonly int[,] Layer4 =
    {
        { 0, 0, 4, 4, 4, 4, 0, 0 },
        { 0, 4, 4, 4, 4, 4, 4, 0 },
        { 4, 4, 4, 4, 4, 4, 0, 0 },
        { 4, 4, 4, 4, 4, 4, 0, 0 },
        { 4, 4, 4, 4, 4, 4, 0, 0 },
        { 4, 4, 4, 4, 4, 4, 0, 0 },
        { 0, 4, 4, 4, 4, 4, 4, 0 },
        { 0, 0, 4, 4, 4, 4, 0, 0 }
    };

    private static readonly int[,] Layer6 =
    {
        { 0, 6, 6, 6, 0 },
        { 6, 6, 6, 6, 6 },
        { 6, 6, 6, 6, 6 },
        { 6, 6, 6, 6, 6 },
        { 0, 6, 6, 6, 0 }
    };

    /// <summary>
    /// <c>Mining_RandomizeDirtLayers</c>: ten patches of four layers, then up to fifteen of six laid only where four
    /// lie already. The original's two slips are kept: the first patches' rows are offset by the smaller patch's
    /// size, and the test for the six-layer patches reads the corner of the four-layer patch's shape.
    /// </summary>
    private void RandomizeDirt()
    {
        const int four = 8, six = 5;
        for (int i = 0; i < 10; i++)
        {
            int startX = rand.Next(Width + four) - four, startY = rand.Next(Height + four) - six;
            for (int y = startY; y < startY + four; y++)
            {
                if (y >= Height || y < 0) continue;
                for (int x = startX; x < startX + four; x++)
                {
                    if (x >= Width || x < 0 || Layer4[y - startY, x - startX] == 0) continue;
                    dirt[y, x] = Layer4[y - startY, x - startX];
                }
            }
        }
        for (int i = 0; i < 15; i++)
        {
            int startX = rand.Next(Width + six) - six, startY = rand.Next(Height + six) - six;
            bool canAdd = true;
            for (int y = startY; y < startY + six && canAdd; y++)
            {
                if (y >= Height || y < 0) continue;
                for (int x = startX; x < startX + six; x++)
                {
                    if (x >= Width || x < 0 || Layer4[y - startY, x - startX] == 0) continue;
                    if (dirt[y, x] < 4)
                    {
                        canAdd = false;
                        break;
                    }
                }
            }
            if (!canAdd) continue;
            for (int y = startY; y < startY + six; y++)
            {
                if (y >= Height || y < 0) continue;
                for (int x = startX; x < startX + six; x++)
                {
                    if (x >= Width || x < 0 || Layer6[y - startY, x - startX] == 0) continue;
                    dirt[y, x] = Layer6[y - startY, x - startX];
                }
            }
        }
    }

    private void Chip(int x, int y, int layers)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return;
        dirt[y, x] = Math.Max(0, dirt[y, x] - layers);
    }

    /// <summary>
    /// A blow at a square with the tool in hand (<c>Mining_RemoveDirt</c> and the wall's damage in
    /// <c>Mining_MainGameLoop</c>): two layers off the square itself; then, unless the square is bare dark rock, the
    /// pick takes one off the four beside it, and the hammer two off those and one off the four corners. The wall
    /// loses four points to the pick and eight to the hammer, whatever was hit.
    /// </summary>
    public MiningBlow Strike(int x, int y)
    {
        if (Over || x < 0 || x >= Width || y < 0 || y >= Height) return default;
        Chip(x, y, 2);
        int under = ObjectAt(x, y);
        bool rock = under >= 0 && objects[under].IsRock && dirt[y, x] == 0;
        bool found = under >= 0 && !objects[under].IsRock && dirt[y, x] == 0;
        if (!rock)
        {
            if (!PickSelected)
                foreach (var (dx, dy) in new[] { (1, 1), (-1, -1), (-1, 1), (1, -1) }) Chip(x + dx, y + dy, 1);
            foreach (var (dx, dy) in new[] { (0, 1), (0, -1), (-1, 0), (1, 0) }) Chip(x + dx, y + dy, PickSelected ? 1 : 2);
        }
        int damage = PickSelected ? PickDamage : HammerDamage;
        Integrity = Integrity > damage ? Integrity - damage : 0;

        // Mining_AreAllItemsDugUp: a thing is the player's once no dirt lies on any of its squares
        bool all = true;
        for (int i = 0; i < ItemCount; i++)
        {
            bool clear = true;
            for (int cy = 0; cy < Height && clear; cy++)
                for (int cx = 0; cx < Width; cx++)
                    if (grid[cy, cx] == i + 1 && dirt[cy, cx] != 0)
                    {
                        clear = false;
                        break;
                    }
            if (clear) bare[i] = true;
            else all = false;
        }
        AllDug = all;
        return new MiningBlow(true, rock, found);
    }

    /// <summary>How far the crack has run along the top of the wall, from nothing to all of it.</summary>
    public float Crack => 1f - Integrity / (float)InitialIntegrity;

    /// <summary>
    /// What was dug out whole, in the wall's order, each Sphere with its size drawn
    /// (<c>Mining_GenerateSizeOfMinedSphere</c>): one for a Prism or Pale Sphere and one to four for the others, ten
    /// more for a large one, one for every fifth of the wall still standing, and five more unless it fell.
    /// </summary>
    public List<MiningFind> Finds()
    {
        var finds = new List<MiningFind>();
        for (int i = 0; i < ItemCount; i++)
        {
            if (!bare[i]) continue;
            int id = objects[i].Id, size = 0;
            if (UndergroundDatabase.IsSphere(id))
            {
                var kind = UndergroundDatabase.SphereOf(id);
                size = rand.Next(kind is SphereKind.Prism or SphereKind.Pale ? 1 : 4) + 1;
                if (UndergroundDatabase.IsLargeSphere(id)) size += 10;
                size += Integrity / (InitialIntegrity / 5);
                if (Integrity != 0) size += 5;
            }
            finds.Add(new MiningFind(id, size));
        }
        return finds;
    }

    /// <summary>Whether a thing dug up counts as a fossil in the records: the fossils, the Old Amber and, by the original's slip, the Rare Bone.</summary>
    public static bool CountsAsFossil(int id)
    {
        string key = UndergroundDatabase.Treasure(id)?.Id ?? "";
        return key.EndsWith("_FOSSIL", StringComparison.Ordinal) || key is "TREASURE_OLD_AMBER" or "TREASURE_RARE_BONE";
    }
}
