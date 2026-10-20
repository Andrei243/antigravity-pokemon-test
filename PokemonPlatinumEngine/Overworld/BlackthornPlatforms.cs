using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// The Blackthorn Gym's floating platforms (plan 20 · J11, part c; HeartGold's <c>src/field/gymmick/blackthorn.c</c>):
/// three slabs of rock on a lake of lava, each with three buttons on its floor. Standing on the middle button and
/// pressing A turns the slab a quarter clockwise about the player; the buttons either side of it send the slab its own
/// width along its length one way or the other, the player riding on it. A slab moves only over lava: a step whose
/// leading edge would meet anything else bumps, and the slab goes back to where it began, so it ends its move all the
/// way along or not at all. A turn is refused outright where an edge of the slab already touches something, and
/// stopped part of the way (and swung back) where its corners would sweep into it, by the original's tables of how far
/// each corner gets. Only a slab's floor of three tiles by six can be walked; every other tile of lava refuses a step.
/// The slabs are laid out afresh each time the player comes in (the room's own script runs <c>BlackthornGymInit</c>),
/// so nothing is saved. GPU-free: the engine asks <see cref="ActionAt"/> when A is pressed, plays a
/// <see cref="Motion"/> with <see cref="Update"/>, and the room's pieces are drawn from <see cref="Slabs"/>.
/// </summary>
public sealed class BlackthornPlatforms : GymPuzzle
{
    public const string PuzzleName = "BlackthornPlatforms";

    public override string Name => PuzzleName;

    /// <summary>Where a slab stands: its middle button's tile and its quarter turns clockwise.</summary>
    public readonly record struct Slab(int X, int Z, int Rotation);

    /// <summary>The three slabs as the room begins (<c>InitBlackthornGym</c>): the first by the door, the second in the middle, the third under the Leader's dais.</summary>
    public static readonly Slab[] Layout = { new(13, 75, 0), new(9, 58, 1), new(14, 32, 0) };

    /// <summary>What a button does: turn its slab, or send it along its length (east or west as it lies unturned).</summary>
    public enum Action { None, Rotate, MoveRight, MoveLeft }

    /// <summary>A slab's own measures: the second is a tile narrower and a tile longer than the others (<c>PLATFORM_TYPE_*</c>).</summary>
    public static (int Width, int Height) SizeOf(int slab) => slab == 1 ? (4, 8) : (5, 7);

    /// <summary>The tiles the original's frames take to move a tile (two frames at thirty a second) and to turn a quarter (sixteen).</summary>
    public const float SecondsPerTile = 2f / 30f, SecondsPerQuarter = 16f / 30f;

    private readonly int[] xs = new int[3], zs = new int[3], rots = new int[3];

    public BlackthornPlatforms() => Reset();

    /// <summary>The slabs where they stand now.</summary>
    public IReadOnlyList<Slab> Slabs => new[] { new Slab(xs[0], zs[0], rots[0]), new Slab(xs[1], zs[1], rots[1]), new Slab(xs[2], zs[2], rots[2]) };

    /// <summary>What is moving now, or null.</summary>
    public Motion? Moving { get; private set; }

    /// <summary>Every slab back where the room begins.</summary>
    public void Reset()
    {
        for (int i = 0; i < 3; i++) (xs[i], zs[i], rots[i]) = (Layout[i].X, Layout[i].Z, Layout[i].Rotation);
        Moving = null;
    }

    public override void Apply(Map map, StoryState story) { }

    public override void Arrive(Map map, StoryState story, Random rng) => Reset();

    /// <summary>
    /// A tile of lava refuses a step unless a slab's floor lies over it (<c>GymmickCheckCollision_Blackthorn</c>); while a
    /// slab moves nothing is walked anyway.
    /// </summary>
    public override bool Refuses(Map map, int x, int y, float from, bool afloat) =>
        map.InBounds(x, y) && map.BehaviourAt(x, y) == TileBehavior.Magma && !OnAFloor(x, y);

    /// <summary>Whether a tile is one of a slab's floor, as the slabs stand now.</summary>
    public bool OnAFloor(int x, int y)
    {
        for (int i = 0; i < 3; i++)
            foreach (var (fx, fz) in Floor(i))
                if (fx == x && fz == y) return true;
        return false;
    }

    // ------------------------------------------------------------------ a slab's points, as BlackthornPlatform_Init lays them

    /// <summary>A point of a slab laid out unturned round its middle button, turned with it.</summary>
    private static (int X, int Z) Turned(int rot, int x, int z, int px, int pz)
    {
        int dx = px - x, dz = pz - z;
        return (rot % 4) switch
        {
            1 => (x - dz, z + dx),
            2 => (x - dx, z - dz),
            3 => (x + dz, z - dx),
            _ => (px, pz)
        };
    }

    private IEnumerable<(int X, int Z)> Floor(int i) => FloorOf(i, xs[i], zs[i], rots[i]);

    /// <summary>The eighteen tiles of a slab's floor: three wide, from a row before its middle button to four after.</summary>
    public static IEnumerable<(int X, int Z)> FloorOf(int slab, int x, int z, int rot)
    {
        for (int row = 0; row < 6; row++)
            for (int col = 0; col < 3; col++)
                yield return Turned(rot, x, z, x - 1 + col, z - 1 + row);
    }

    /// <summary>The tiles a slab covers, its floor and its rim, unturned: from (x0, z0) to (x1, z1) round its middle button.</summary>
    public static (int X0, int Z0, int X1, int Z1) Extent(int slab) => slab == 1 ? (-2, -2, 1, 5) : (-2, -2, 2, 4);

    private enum Wall { Right, Left, Bottom, Top, RightEdgeOuter }

    /// <summary>The tiles just outside a slab along one side (<c>rightWall</c> and its fellows), turned with it.</summary>
    private static List<(int X, int Z)> WallOf(int slab, int x, int z, int rot, Wall wall)
    {
        var (width, height) = SizeOf(slab);
        var points = new List<(int, int)>();
        bool narrow = slab == 1;
        switch (wall)
        {
            case Wall.Right:
                for (int i = 0; i < height; i++) points.Add((x + (narrow ? 2 : 3), z - 2 + i));
                break;
            case Wall.Left:
                for (int i = 0; i < height; i++) points.Add((x - 3, z - 2 + i));
                break;
            case Wall.Bottom:
                for (int i = 0; i < width; i++) points.Add((x - 2 + i, z + (narrow ? 6 : 5)));
                break;
            case Wall.Top:
                for (int i = 0; i < width; i++) points.Add((x - 2 + i, z - 3));
                break;
            default:
                for (int i = 0; i < width; i++) points.Add((x + 3, z - 2 + i));
                break;
        }
        for (int i = 0; i < points.Count; i++) points[i] = Turned(rot, x, z, points[i].Item1, points[i].Item2);
        return points;
    }

    /// <summary>The tiles a slab's corners sweep through as it turns (<c>rotateCollisionCheck</c>), turned with it.</summary>
    private static List<(int X, int Z)> SweptBy(int slab, int x, int z, int rot)
    {
        var points = new List<(int, int)>();
        bool narrow = slab == 1;
        int columns = narrow ? 3 : 2, rows = narrow ? 8 : 7, start = narrow ? 5 : 4;
        for (int i = 0; i < columns; i++)
            for (int j = 0; j < rows; j++)
                points.Add(Turned(rot, x, z, x - 4 - i, z + start - j));
        return points;
    }

    // If a point a corner sweeps through is in the way, how far round (in 64ths of the quarter) the slab gets first;
    // 64 is the whole way (the original's sMainAreaRotationExtents_* and sLeadingEdgeRotationExtents_*)
    private static readonly int[] MainExtents0 = { 10, 10, 13, 18, 25, 38, 51, 64, 64, 23, 28, 35, 45, 55 };
    private static readonly int[] MainExtents1 = { 8, 9, 10, 14, 19, 26, 37, 51, 64, 17, 19, 23, 28, 35, 46, 55, 64, 64, 64, 30, 35, 41, 49, 57 };
    private static readonly int[] EdgeExtents0 = { 64, 64, 64, 64, 42 };
    private static readonly int[] EdgeExtents1 = { 64, 20, 30, 46 };

    /// <summary>Whether a slab may stand over a tile: lava, which is never the room's wall (<c>BlackthornGymmick_CheckWallCollision</c>).</summary>
    private static bool Lava(Map map, int x, int z) => map.InBounds(x, z) && map.BehaviourAt(x, z) == TileBehavior.Magma;

    // ------------------------------------------------------------------ the buttons

    /// <summary>The button under the player and its slab (<c>BlackthornGymmick_GetButtonAction</c>): the middle one turns, the others send it along.</summary>
    public (Action Action, int Slab) ActionAt(int x, int z)
    {
        for (int i = 0; i < 3; i++)
        {
            if (x == xs[i] && z == zs[i]) return (Action.Rotate, i);
            if (Turned(rots[i], xs[i], zs[i], xs[i] + 1, zs[i]) == (x, z)) return (Action.MoveRight, i);
            if (Turned(rots[i], xs[i], zs[i], xs[i] - 1, zs[i]) == (x, z)) return (Action.MoveLeft, i);
        }
        return (Action.None, -1);
    }

    /// <summary>
    /// Presses the button under the player: works out the whole of what the slab does, at once, as the rules decide it,
    /// and starts it playing (<see cref="Update"/>). False where the player stands on no button or something moves already.
    /// </summary>
    public bool Press(Map map, int x, int z)
    {
        if (Moving != null) return false;
        var (action, i) = ActionAt(x, z);
        if (action == Action.None) return false;
        Moving = action == Action.Rotate ? Turn(map, i) : Slide(map, i, action);
        return true;
    }

    /// <summary>
    /// A slab sent along (<c>Task_BlackthornGymmick_MovePlatform</c>): its own width in tiles, its leading edge checked
    /// before each tile; at the first that meets anything but lava it bumps and goes back the tiles it went.
    /// </summary>
    private Motion Slide(Map map, int i, Action action)
    {
        int way = ((action == Action.MoveRight ? 0 : 2) + rots[i]) % 4;
        var (dx, dz) = way switch { 0 => (1, 0), 1 => (0, 1), 2 => (-1, 0), _ => (0, -1) };
        var (width, _) = SizeOf(i);
        var edge = WallOf(i, xs[i], zs[i], rots[i], action == Action.MoveRight ? Wall.Right : Wall.Left);
        int gone = 0;
        bool bumped = false;
        while (gone < width)
        {
            bool clear = true;
            foreach (var (px, pz) in edge)
                if (!Lava(map, px + dx * gone, pz + dz * gone)) { clear = false; break; }
            if (!clear) { bumped = true; break; }
            gone++;
        }
        return new Motion(i, action, dx, dz, bumped ? gone : width, bumped, 0);
    }

    /// <summary>
    /// A slab turned about its middle button (<c>ov04_02255CBC</c>): refused where its bottom, top or left side, or its
    /// right side but the last tile, touches anything but lava; otherwise as far round as its corners get before they
    /// would meet something, and back again if that is short of the whole quarter.
    /// </summary>
    private Motion Turn(Map map, int i)
    {
        var (width, height) = SizeOf(i);
        bool Clear(List<(int X, int Z)> points, int count)
        {
            for (int k = 0; k < count; k++)
                if (!Lava(map, points[k].X, points[k].Z)) return false;
            return true;
        }
        int x = xs[i], z = zs[i], rot = rots[i];
        if (!Clear(WallOf(i, x, z, rot, Wall.Bottom), width) || !Clear(WallOf(i, x, z, rot, Wall.Top), width)
            || !Clear(WallOf(i, x, z, rot, Wall.Left), height) || !Clear(WallOf(i, x, z, rot, Wall.Right), width - 1))
            return new Motion(i, Action.Rotate, 0, 0, 0, true, 0);
        int Furthest(List<(int X, int Z)> points, int[] extents)
        {
            for (int k = 0; k < points.Count; k++)
                if (!Lava(map, points[k].X, points[k].Z) && extents[k] < 64) return extents[k];
            return 64;
        }
        int main = Furthest(SweptBy(i, x, z, rot), i == 1 ? MainExtents1 : MainExtents0);
        int edge = Furthest(WallOf(i, x, z, rot, Wall.RightEdgeOuter), i == 1 ? EdgeExtents1 : EdgeExtents0);
        int reach = Math.Min(main, edge);
        return new Motion(i, Action.Rotate, 0, 0, 0, reach < 64, reach);
    }

    /// <summary>
    /// Plays what is moving on: the slab and the player on it go along together. As it ends the slab stands where the
    /// rules put it (a turn is about the middle button, where the player stands) and <see cref="Moving"/> is null.
    /// </summary>
    public void Update(float dt)
    {
        if (Moving is not { } m) return;
        m.Time += dt;
        if (m.Time < m.Length) return;
        if (!m.Bumped)
        {
            if (m.Action == Action.Rotate) rots[m.Slab] = (rots[m.Slab] + 1) % 4;
            else
            {
                xs[m.Slab] += m.DX * m.Tiles;
                zs[m.Slab] += m.DZ * m.Tiles;
            }
        }
        Moving = null;
    }

    /// <summary>
    /// A slab on its way: which, what was pressed, its way along and how many tiles it went before it bumped (or all of
    /// them), or for a turn how far round it gets in 64ths of a quarter (0 where it was refused outright).
    /// </summary>
    public sealed class Motion
    {
        public Motion(int slab, Action action, int dx, int dz, int tiles, bool bumped, int reach)
        {
            Slab = slab;
            Action = action;
            DX = dx;
            DZ = dz;
            Tiles = tiles;
            Bumped = bumped;
            Reach = reach;
        }

        public int Slab { get; }
        public Action Action { get; }
        public int DX { get; }
        public int DZ { get; }
        public int Tiles { get; }
        public bool Bumped { get; }
        public int Reach { get; }
        public float Time { get; internal set; }

        /// <summary>Whether the slab goes nowhere at all: a turn refused, or a slide that met something at once.</summary>
        public bool Refused => Action == Action.Rotate ? Bumped && Reach == 0 : Bumped && Tiles == 0;

        /// <summary>How long it takes: there and, after a bump, back again.</summary>
        public float Length => Action == Action.Rotate
            ? (Refused ? 0.2f : Bumped ? 2f * SecondsPerQuarter * Reach / 64f : SecondsPerQuarter)
            : (Refused ? 0.2f : (Bumped ? 2f : 1f) * Tiles * SecondsPerTile);

        /// <summary>How far along it is now, in tiles from where it began (a slide), there and back after a bump.</summary>
        public float Along
        {
            get
            {
                if (Action == Action.Rotate || Refused) return 0f;
                float t = Math.Clamp(Time / SecondsPerTile, 0f, Bumped ? 2f * Tiles : Tiles);
                return t <= Tiles ? t : 2f * Tiles - t;
            }
        }

        /// <summary>How far round it is now, in quarters (a turn), there and back after a bump.</summary>
        public float Round
        {
            get
            {
                if (Action != Action.Rotate || Refused) return 0f;
                float most = Bumped ? Reach / 64f : 1f;
                float t = Math.Clamp(Time / SecondsPerQuarter, 0f, Bumped ? 2f * most : most);
                return t <= most ? t : 2f * most - t;
            }
        }

        /// <summary>Whether the bump is heard now: the moment the slab meets what stops it (or at once, refused).</summary>
        public bool BumpsAt(float before, float after)
        {
            if (!Bumped) return false;
            float at = Refused ? 0f : Action == Action.Rotate ? SecondsPerQuarter * Reach / 64f : Tiles * SecondsPerTile;
            return before <= at && after > at || before == 0f && at == 0f && after > 0f;
        }
    }
}
