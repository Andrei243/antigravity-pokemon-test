using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>How someone moves about of their own accord.</summary>
public enum PersonMoves
{
    /// <summary>Turns now and then among some ways, never stepping (<c>LOOK_AROUND</c>, <c>LOOK_NORTH_AND_WEST</c>…).</summary>
    Look,
    /// <summary>Turns now and then and takes a step that way, within the box round where they stand (<c>WANDER_AROUND</c>…).</summary>
    Wander,
    /// <summary>Turns now and then to the next way round (<c>ROTATE_CLOCKWISE</c>, <c>ROTATE_COUNTERCLOCKWISE</c>).</summary>
    Rotate,
    /// <summary>Walks a loop of four legs round a corner of the box, step after step (<c>WALK_WEST_SOUTH_EAST_NORTH</c>…).</summary>
    Loop
}

/// <summary>
/// How someone of the map moves when nobody sends them anywhere (plan 02 · S6): the original's movement type and
/// the range round where they first stand (<c>movement_range_x</c>, <c>movement_range_z</c> in its events). No
/// drawing or input.
/// </summary>
public sealed record PersonMovement(PersonMoves Kind, Direction[] Ways, int RangeX, int RangeZ, int HomeX, int HomeY)
{
    /// <summary>The movement type as the area or map file names it (<c>wander_around</c>), for writing it back out.</summary>
    public string Source { get; init; } = "";

    private static readonly Dictionary<string, Direction> Named = new()
    {
        ["north"] = Direction.Up, ["south"] = Direction.Down, ["west"] = Direction.Left, ["east"] = Direction.Right
    };

    /// <summary>
    /// The movement an area file names (its type in lower case without <c>MOVEMENT_TYPE_</c>), for someone first
    /// standing on (<paramref name="x"/>, <paramref name="y"/>) facing <paramref name="facing"/>; null for anyone who
    /// stands still, facing one way (<c>none</c>, <c>look_south</c>) or moving by other rules (a berry's soil, a
    /// follower, a disguise).
    /// </summary>
    public static PersonMovement? Parse(string movement, int rangeX, int rangeZ, int x, int y, Direction facing)
    {
        if (string.IsNullOrEmpty(movement)) return null;
        var words = movement.Split('_');
        Direction[] Ways(IEnumerable<string> names) => names.Where(Named.ContainsKey).Select(n => Named[n]).ToArray();
        PersonMovement Of(PersonMoves kind, Direction[] ways) => new(kind, ways, Math.Max(0, rangeX), Math.Max(0, rangeZ), x, y) { Source = movement };

        switch (words[0])
        {
            case "look" when movement == "look_around":
                return Of(PersonMoves.Look, new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right });
            case "look" when words.Contains("and"):
                return Of(PersonMoves.Look, Ways(words.Skip(1)));
            case "wander" when movement == "wander_around":
                return Of(PersonMoves.Wander, new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right });
            case "wander":
                return Of(PersonMoves.Wander, Ways(words.Skip(1)));
            case "rotate":
                return Of(PersonMoves.Rotate, movement == "rotate_clockwise"
                    ? new[] { Direction.Up, Direction.Right, Direction.Down, Direction.Left }
                    : new[] { Direction.Up, Direction.Left, Direction.Down, Direction.Right });
            case "walk" when movement == "walk_back_and_forth":
                return Of(PersonMoves.Loop, new[] { facing, FieldMovement.Opposite(facing), facing, FieldMovement.Opposite(facing) });
            case "walk" when Ways(words.Skip(1)) is { Length: 4 } legs:
                return Of(PersonMoves.Loop, legs);
        }
        return null;
    }

    /// <summary>Whether a tile is inside the box they keep to (<c>MapObject_IsOutOfRange</c>).</summary>
    public bool InRange(int x, int y) => Math.Abs(x - HomeX) <= RangeX && Math.Abs(y - HomeY) <= RangeZ;
}

/// <summary>
/// The people of the field moving of their own accord (plan 02 · S6; the original's <c>unk_0206450C.c</c>): those who
/// look about turn after a wait of 16, 32, 48 or 64 frames drawn at random (<c>Unk_020EEA88</c>), those who wander
/// take a step the way they turned when it stays inside their box and nothing is in the way, and those who walk a
/// loop step on and on round its four legs, marking time while something blocks them. Nobody moves while a script,
/// a trainer's walk or a menu has the field: the engine simply stops asking. No drawing or input.
/// </summary>
public sealed class Wandering
{
    /// <summary>The original's frames, thirty to a second.</summary>
    public const float Frame = 1f / 30f;

    private static readonly int[] Waits = { 16, 32, 48, 64 };

    private readonly Dictionary<NPC, float> waits = new();
    private readonly Dictionary<NPC, NpcWalk> steps = new();
    private readonly Dictionary<NPC, int> legs = new();

    /// <summary>Whether anyone is in the middle of a step.</summary>
    public bool AnyoneStepping => steps.Count > 0;

    /// <summary>
    /// A frame of the field. <paramref name="player"/> and <paramref name="heading"/> are the tile the player stands
    /// on and the one their step ends on, which nobody steps onto; <paramref name="still"/> are people who mustn't
    /// move now (a trainer who has seen the player, someone a script walks).
    /// </summary>
    public void Update(float dt, Map map, (int X, int Y) player, (int X, int Y) heading, Random rng, Func<NPC, bool>? still = null)
    {
        foreach (var npc in map.NPCs.ToList())
        {
            if (npc.Movement is not { } movement || npc.ReadyForRematch || npc.HasSpottedPlayer || still?.Invoke(npc) == true) continue;

            if (steps.TryGetValue(npc, out var walk))
            {
                walk.Update(dt);
                if (!walk.IsDone) continue;
                steps.Remove(npc);
            }

            // A loop walks on without waiting, unless it is marking time; everyone else waits a while between one
            // turn and the next
            if (movement.Kind == PersonMoves.Loop)
            {
                if (waits.TryGetValue(npc, out float hold))
                {
                    hold -= dt;
                    if (hold > 0f)
                    {
                        waits[npc] = hold;
                        continue;
                    }
                    waits.Remove(npc);
                }
            }
            else
            {
                if (!waits.TryGetValue(npc, out float wait)) wait = Waits[rng.Next(Waits.Length)] * Frame;
                wait -= dt;
                if (wait > 0f)
                {
                    waits[npc] = wait;
                    continue;
                }
                waits[npc] = Waits[rng.Next(Waits.Length)] * Frame;
            }

            switch (movement.Kind)
            {
                case PersonMoves.Look:
                    npc.Facing = movement.Ways[rng.Next(movement.Ways.Length)];
                    break;
                case PersonMoves.Rotate:
                    int at = Array.IndexOf(movement.Ways, npc.Facing);
                    npc.Facing = movement.Ways[(at + 1) % movement.Ways.Length];
                    break;
                case PersonMoves.Wander:
                    var way = movement.Ways[rng.Next(movement.Ways.Length)];
                    npc.Facing = way;
                    if (CanStep(map, npc, movement, way, player, heading)) Take(npc, way);
                    break;
                case PersonMoves.Loop:
                    Loop(map, npc, movement, player, heading);
                    break;
            }
        }
    }

    /// <summary>
    /// A loop's next step (the original's <c>sub_02064EEC</c>): out along the first leg and the second to the edge of
    /// the box, back along the third to where they began on its axis, and along the fourth home, where it starts
    /// again. A step out of the box ends the leg; anything else in the way has them mark time.
    /// </summary>
    private void Loop(Map map, NPC npc, PersonMovement movement, (int X, int Y) player, (int X, int Y) heading)
    {
        int leg = legs.GetValueOrDefault(npc);
        if (leg == 2 && OnHomeAxis(npc, movement, movement.Ways[2])) leg = 3;
        if (leg == 3 && npc.GridX == movement.HomeX && npc.GridY == movement.HomeY) leg = 0;

        var way = movement.Ways[leg];
        var (dx, dy) = FieldMovement.Delta(way);
        if (!movement.InRange(npc.GridX + dx, npc.GridY + dy))
        {
            leg = (leg + 1) % 4;
            way = movement.Ways[leg];
        }
        legs[npc] = leg;
        npc.Facing = way;
        // Anything in the way: mark time for the length of a step before trying again
        if (CanStep(map, npc, movement, way, player, heading)) Take(npc, way);
        else waits[npc] = StepFrames * Frame;
    }

    /// <summary>A step at walking pace, in the original's frames.</summary>
    private const int StepFrames = 16;

    private static bool OnHomeAxis(NPC npc, PersonMovement movement, Direction way) =>
        way is Direction.Left or Direction.Right ? npc.GridX == movement.HomeX : npc.GridY == movement.HomeY;

    /// <summary>
    /// Whether a step a way can be taken (<c>sub_02063EBC</c>): it stays in the box, onto ground someone on foot
    /// could walk to from where they stand, with nobody on it, and not where the player is or is going.
    /// </summary>
    public static bool CanStep(Map map, NPC npc, PersonMovement movement, Direction way, (int X, int Y) player, (int X, int Y) heading)
    {
        if (npc.Level != null) return false;
        var (dx, dy) = FieldMovement.Delta(way);
        int nx = npc.GridX + dx, ny = npc.GridY + dy;
        if (!movement.InRange(nx, ny)) return false;
        if ((nx, ny) == player || (nx, ny) == heading) return false;
        if (map.GetWarpAt(nx, ny) != null || map.NpcIn(nx, ny, map.HeightAt(nx, ny)) != null) return false;
        var step = FieldMovement.Step(map, npc.GridX, npc.GridY, way, new Walker(Height: map.HeightAt(npc.GridX, npc.GridY)));
        return step.Kind == StepKind.Walk && step.X == nx && step.Y == ny;
    }

    private void Take(NPC npc, Direction way)
    {
        var walk = new NpcWalk(npc, new[] { way }, fast: false);
        steps[npc] = walk;
    }

    /// <summary>
    /// Only the steps already begun go on, to their ends: a script, a trainer's walk or a menu has the field, and
    /// nobody starts anything new meanwhile.
    /// </summary>
    public void Continue(float dt)
    {
        foreach (var (npc, walk) in steps.ToList())
        {
            walk.Update(dt);
            if (walk.IsDone) steps.Remove(npc);
        }
    }

    /// <summary>Ends any step someone is in the middle of at once, where it was going (a script or a trainer takes them over).</summary>
    public void Settle(NPC npc)
    {
        if (steps.Remove(npc, out var walk)) walk.Finish();
    }

    /// <summary>Ends every step at once and forgets every wait: a map left, a save loaded.</summary>
    public void Clear()
    {
        foreach (var walk in steps.Values) walk.Finish();
        steps.Clear();
        waits.Clear();
        legs.Clear();
    }

    /// <summary>
    /// Everyone who moves of their own accord back where they first stood, facing as they did: the map is come to
    /// again, as the original lays its objects out afresh from its events.
    /// </summary>
    public static void SendHome(Map map)
    {
        foreach (var npc in map.Everyone)
        {
            if (npc.Movement is not { } movement) continue;
            npc.GridX = movement.HomeX;
            npc.GridY = movement.HomeY;
            npc.StepOffsetX = npc.StepOffsetY = 0f;
            npc.WalkBlend = 0f;
        }
    }
}
