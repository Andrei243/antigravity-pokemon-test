using System;
using System.Numerics;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// Continuous movement over the grid (plan 30 · FM7, stage 2), the rules alone: no drawing, no input, no chance.
/// Where a region's <c>movement</c> record says <see cref="MovementDirections.Free"/>, the player's body is a circle
/// (<see cref="Radius"/>) whose centre is kept in whole 256ths of a tile (<see cref="SubTile"/>), so a run is the same
/// played twice. The circle is resolved against the edges of the tiles, and whether an edge is a wall for this walker is
/// <b>asked of <see cref="FieldMovement.Step"/></b> from the tile the centre is in toward the neighbour
/// (<see cref="Classify"/>): a plain walk is <see cref="Edge.Open"/>, a refusal <see cref="Edge.Solid"/>, and anything
/// else (a hop, a landing, a climb, a jump, a stair, ice, a moving floor, a warp, a door, a deck's end, a boulder to push,
/// a Gym's puzzle) is <see cref="Edge.HandOff"/>, which the player plays as the grid's own step from the tile's centre.
/// So there is one rule of collision, the grid's, and free movement reaches nothing two cardinal steps don't.
/// <para>
/// A step is a tile entered (decision 7), counted per axis once the centre is <see cref="Hysteresis"/> past the edge it
/// came over (<see cref="Counted"/>): by then the body has wholly left the tile behind along that axis, so walking back
/// and forth over one edge counts nothing, and a body pressed against a wall in a tile always counts that tile.
/// </para>
/// </summary>
public static class FreeMovement
{
    /// <summary>A tile, in the fixed-point units of a free position: 256 to a tile, eight to a field texel.</summary>
    public const int SubTile = 256;

    /// <summary>Half a tile: a tile's centre is its number × <see cref="SubTile"/>, its edges half a tile either side.</summary>
    public const int Half = SubTile / 2;

    /// <summary>The body's radius: 0.3 of a tile (plan 30, decided in FM7), 77 256ths.</summary>
    public const int Radius = 77;

    /// <summary>How far past an edge the centre goes before the tile beyond is counted as entered: the body's radius.</summary>
    public const int Hysteresis = Radius;

    /// <summary>The cone within which a push across an edge is a push across it (35° of its normal): a hand-off, not a slide.</summary>
    public const float HandOffCone = 0.8191520f;

    /// <summary>
    /// How far ahead of the centre, along the yaw, talking and pressing reach: three quarters of a tile (the plan's 0.7
    /// fell just short of the diagonal tile at 45°, 0.495 a tile along each axis).
    /// </summary>
    public const float AheadReach = 0.75f;

    /// <summary>The longest stretch moved before the walls are asked again: a quarter of a tile, so nothing is tunnelled through.</summary>
    public const int Substep = SubTile / 4;

    /// <summary>A tilt under this share of full walks slowly (<see cref="Pace.Slow"/>) where the region's speed is analog.</summary>
    public const float SlowTilt = 0.5f;

    /// <summary>A tilt this near full, held <see cref="RunAfterSeconds"/>, runs as the run button does (FM9's choice).</summary>
    public const float FullTilt = 0.95f;

    /// <summary>How long a full tilt is held before it runs.</summary>
    public const float RunAfterSeconds = 1f;

    /// <summary>What an edge between two tiles is to a body moving freely.</summary>
    public enum Edge
    {
        /// <summary>A plain walk on the same level in the same way of travelling: the circle crosses it.</summary>
        Open,
        /// <summary>A step the grid plays (a hop, a landing, a stair, a warp, ice…), from the tile's centre.</summary>
        HandOff,
        /// <summary>A wall, a person, deep water, a cliff, a closed side: the circle stops against it.</summary>
        Solid
    }

    /// <summary>The tile a free position is in (a tile spans from half a tile before its centre to just under half after).</summary>
    public static int TileOf(int sub) => FloorDiv(sub + Half, SubTile);

    private static int FloorDiv(int a, int b) => a >= 0 ? a / b : -((-a + b - 1) / b);

    /// <summary>The centre of a tile, in free units.</summary>
    public static int CentreOf(int tile) => tile * SubTile;

    /// <summary>
    /// What the edge from (<paramref name="x"/>, <paramref name="y"/>) toward <paramref name="way"/> is to a walker:
    /// <see cref="FieldMovement.Step"/> is asked, and its answer read. <paramref name="step"/> is that answer (its height
    /// is the height the walker stands at once over the edge). <paramref name="pushes"/> says a boulder in the way would
    /// be pushed (Strength in force): it is then a hand-off, as a block of ice always is.
    /// </summary>
    public static Edge Classify(Map map, int x, int y, Direction way, Walker walker, bool pushes, out FieldStep step)
    {
        step = FieldMovement.Step(map, x, y, way, walker);
        var (dx, dy) = FieldMovement.Delta(way);
        int nx = x + dx, ny = y + dy;
        if (!step.Moves)
        {
            if (step.Obstacle == Obstacle.Person && map.InBounds(nx, ny)
                && map.NpcIn(nx, ny, map.SurfaceAt(nx, ny, walker.Height).Height) is { } thing
                && (thing.IsIceBlock || (pushes && thing.Obstacle == PropType.StrengthBoulder)
                    // A charging ride (Tauros) runs into a cracked rock from the tile's centre, as on the grid, so the
                    // grid's bump notes what it ran into and the engine's charge breaks it (plan 23, Alola free)
                    || (walker.Mode == TravelMode.Riding && RidePager.Charges(walker.Steed) && thing.Obstacle == PropType.CrackedRock)))
                return Edge.HandOff;
            // Skates refused (tall grass, stairs): the grid takes the step on foot
            return step.Obstacle == Obstacle.Skates ? Edge.HandOff : Edge.Solid;
        }
        if (step.Kind != StepKind.Walk || step.Mode != walker.Mode || (step.X, step.Y) != (nx, ny)) return Edge.HandOff;
        if (!Plain(map, x, y) || !Plain(map, nx, ny)) return Edge.HandOff;
        // A door, a mat, a hole's warp: walked into from the tile's front, by the grid, and taken as its step ends
        if (map.GetWarpAt(nx, ny) != null) return Edge.HandOff;
        // Onto a deck or off it, up a step of the ground: the grid's step eases the height
        if (MathF.Abs(step.Height - walker.Height) > 0.01f) return Edge.HandOff;
        return Edge.Open;
    }

    /// <summary>
    /// Whether a tile is walked freely on and over: nothing of a Gym's puzzle on its map, no rule more than walking
    /// (<see cref="FieldMovement.RefusesDiagonal"/>: ledges, ice, moving floors, stairs, ramps, rails, doors, warp panels,
    /// holes) and no slope. Anywhere else the body is the grid's (<see cref="Edge.HandOff"/>), as a diagonal is refused.
    /// </summary>
    public static bool Plain(Map map, int x, int y) =>
        map.InBounds(x, y) && map.Puzzle == null && !FieldMovement.RefusesDiagonal(map.BehaviourAt(x, y)) && map.SlopeAt(x, y) == (0f, 0f);

    /// <summary>
    /// Moves a body's centre by (<paramref name="dx"/>, <paramref name="dy"/>) in free units, a
    /// <see cref="Substep"/> at a time: each axis stopped against an edge that is not <see cref="Edge.Open"/>, then the
    /// corner points round the tile pushed out of where the tile across the corner is shut. <paramref name="standing"/> is
    /// the height the walker stands at, and becomes the height of each tile the centre crosses into. Returns where the
    /// centre ends.
    /// </summary>
    public static (int X, int Y) Resolve(Map map, int x, int y, int dx, int dy, Walker walker, bool pushes, ref float standing)
    {
        int count = Math.Max(1, (Math.Max(Math.Abs(dx), Math.Abs(dy)) + Substep - 1) / Substep);
        for (int i = 0; i < count; i++)
        {
            int sx = dx * (i + 1) / count - dx * i / count;
            int sy = dy * (i + 1) / count - dy * i / count;
            x = Along(map, x, y, sx, horizontal: true, walker, pushes, ref standing);
            y = Along(map, y, x, sy, horizontal: false, walker, pushes, ref standing);
            (x, y) = Sides(map, x, y, walker with { Height = standing }, pushes);
            (x, y) = Corners(map, x, y, walker with { Height = standing }, pushes);
        }
        return (x, y);
    }

    // The four sides of the tile the centre is in once both axes have moved: a crossing into another row or column can
    // bring the circle over a side of the new tile that is shut, which the axis that moved never asked about. The centre
    // is pushed back to touch it, so the body never overlaps a tile it can't walk into
    private static (int X, int Y) Sides(Map map, int x, int y, Walker walker, bool pushes)
    {
        int gx = TileOf(x), gy = TileOf(y);
        foreach (var (way, sign, horizontal) in new[] { (Direction.Left, -1, true), (Direction.Right, 1, true), (Direction.Up, -1, false), (Direction.Down, 1, false) })
        {
            int a = horizontal ? x : y;
            int edge = CentreOf(horizontal ? gx : gy) + sign * Half;
            if ((a + sign * Radius - edge) * sign <= 0) continue;
            if (Classify(map, gx, gy, way, walker, pushes, out _) == Edge.Open) continue;
            if (horizontal) x = edge - sign * Radius;
            else y = edge - sign * Radius;
        }
        return (x, y);
    }

    // One axis of a substep: `a` is the coordinate that moves, `b` the other one
    private static int Along(Map map, int a, int b, int move, bool horizontal, Walker walker, bool pushes, ref float standing)
    {
        if (move == 0) return a;
        int sign = Math.Sign(move);
        int tile = TileOf(a), across = TileOf(b);
        int edge = CentreOf(tile) + sign * Half;
        int next = a + move;
        // Not near the edge yet: free
        if (sign > 0 ? next + Radius <= edge : next - Radius >= edge) return next;
        var way = horizontal ? (sign > 0 ? Direction.Right : Direction.Left) : (sign > 0 ? Direction.Down : Direction.Up);
        var (tx, ty) = horizontal ? (tile, across) : (across, tile);
        if (Classify(map, tx, ty, way, walker with { Height = standing }, pushes, out var step) != Edge.Open)
        {
            // Against it: the circle stops touching the edge (and is pushed back to it if something came at it)
            return edge - sign * Radius;
        }
        if (TileOf(next) != tile) standing = step.Height;
        return next;
    }

    // The four corner points of the tile the centre is in: where the circle reaches one and the tile across the corner is
    // shut (both sides open, but the way round to it is no plain walk), the centre is pushed straight out from the point
    private static (int X, int Y) Corners(Map map, int x, int y, Walker walker, bool pushes)
    {
        int gx = TileOf(x), gy = TileOf(y);
        for (int cy = -1; cy <= 1; cy += 2)
            for (int cx = -1; cx <= 1; cx += 2)
            {
                int px = CentreOf(gx) + cx * Half, py = CentreOf(gy) + cy * Half;
                long ddx = x - px, ddy = y - py;
                long d2 = ddx * ddx + ddy * ddy;
                if (d2 >= (long)Radius * Radius) continue;
                if (!CornerShut(map, gx, gy, cx, cy, walker, pushes)) continue;
                if (d2 == 0)
                {
                    x = px - cx * (int)MathF.Round(Radius / MathF.Sqrt(2f));
                    y = py - cy * (int)MathF.Round(Radius / MathF.Sqrt(2f));
                    continue;
                }
                double length = Math.Sqrt(d2);
                x = px + (int)Math.Round(ddx * Radius / length, MidpointRounding.AwayFromZero);
                y = py + (int)Math.Round(ddy * Radius / length, MidpointRounding.AwayFromZero);
            }
        return (x, y);
    }

    /// <summary>
    /// Whether the tile across a corner of (<paramref name="gx"/>, <paramref name="gy"/>) is shut to a body there: the two
    /// tiles beside the corner are both open from it (else their edges already stop the circle) and the way on from either
    /// to the corner's tile is not. FM2's rule for a diagonal step, at the corner's point.
    /// </summary>
    public static bool CornerShut(Map map, int gx, int gy, int cx, int cy, Walker walker, bool pushes)
    {
        var horizontal = cx > 0 ? Direction.Right : Direction.Left;
        var vertical = cy > 0 ? Direction.Down : Direction.Up;
        if (Classify(map, gx, gy, horizontal, walker, pushes, out var side) != Edge.Open) return false;
        if (Classify(map, gx, gy, vertical, walker, pushes, out var up) != Edge.Open) return false;
        if (Classify(map, side.X, side.Y, vertical, walker with { Height = side.Height }, pushes, out _) != Edge.Open) return true;
        return Classify(map, up.X, up.Y, horizontal, walker with { Height = up.Height }, pushes, out _) != Edge.Open;
    }

    /// <summary>
    /// The tile counted as the one stood on, along one axis (decision 7 with its hysteresis): it becomes the tile the
    /// centre is in once the centre is <see cref="Hysteresis"/> past the edge it came over; two tiles away or more, at
    /// once.
    /// </summary>
    public static int Counted(int counted, int sub)
    {
        int now = TileOf(sub);
        if (now == counted) return counted;
        if (Math.Abs(now - counted) >= 2) return now;
        int sign = Math.Sign(now - counted);
        int past = (sub - (CentreOf(counted) + sign * Half)) * sign;
        return past >= Hysteresis ? now : counted;
    }

    /// <summary>The yaw a way of moving gives (0 toward the camera, +Z; π/2 to the right), as <see cref="Player.YawOf"/> measures.</summary>
    public static float YawOf(Vector2 way) => MathF.Atan2(way.X, way.Y);

    /// <summary>The way a yaw looks, as a unit vector on the map (x right, y down).</summary>
    public static Vector2 WayOf(float yaw) => new(MathF.Sin(yaw), MathF.Cos(yaw));

    /// <summary>The cardinal nearest a way of moving: what <see cref="Player.Facing"/> is while moving freely (a tie is the vertical).</summary>
    public static Direction NearestCardinal(Vector2 way) =>
        MathF.Abs(way.X) > MathF.Abs(way.Y) ? (way.X < 0 ? Direction.Left : Direction.Right) : (way.Y < 0 ? Direction.Up : Direction.Down);

    /// <summary>The tile <see cref="AheadReach"/> ahead of a free centre along a yaw: what talking and pressing ask first.</summary>
    public static (int X, int Y) AheadTile(int x, int y, float yaw)
    {
        var way = WayOf(yaw);
        return (TileOf(x + (int)MathF.Round(way.X * AheadReach * SubTile)), TileOf(y + (int)MathF.Round(way.Y * AheadReach * SubTile)));
    }

    /// <summary>
    /// The pace a free walker goes at (plan 30 · FM9): where the region's speed is analog, a tilt under half walks slowly;
    /// otherwise the grid's own pace for the ground and the way of travelling (<see cref="FieldMovement.PaceOn"/>), which
    /// is a run where the walker runs.
    /// </summary>
    public static Pace PaceFor(float tilt, bool analog, TileBehavior underfoot, Walker walker)
    {
        var pace = FieldMovement.PaceOn(underfoot, walker);
        if (analog && tilt < SlowTilt && walker.Mode == TravelMode.OnFoot && pace is Pace.Walk or Pace.Fast) return Pace.Slow;
        return pace;
    }
}
