using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core.Autoplay;

/// <summary>What the playthrough bot goes for next, in the order the policy tries them (plan 16 · T7).</summary>
public enum GoalKind
{
    /// <summary>The nurse, when the team is low: spoken to however often.</summary>
    Heal,
    /// <summary>Someone not yet spoken to.</summary>
    Speak,
    /// <summary>An item's ball not yet picked up.</summary>
    Item,
    /// <summary>A tree, a rock or a boulder: looked at once, and its own script asks whether the team can clear it.</summary>
    Obstacle,
    /// <summary>A step trigger whose variable has its value: stepped onto.</summary>
    Trigger,
    /// <summary>A way somewhere not taken yet: stepped onto.</summary>
    Warp,
    /// <summary>The nearest tile never stood on.</summary>
    Frontier
}

/// <summary>
/// A goal: what it is (<see cref="Key"/> names it in the bot's memory), the tile to stand on and, for someone or
/// something spoken to, the way to face there.
/// </summary>
public sealed record Goal(GoalKind Kind, string Key, int StandX, int StandY, Direction Face, int TargetX, int TargetY)
{
    /// <summary>Reached by standing on <see cref="StandX"/>, <see cref="StandY"/> (a trigger, a warp, a new tile) rather than by speaking there.</summary>
    public bool IsStepOnto => Kind is GoalKind.Trigger or GoalKind.Warp or GoalKind.Frontier;
}

/// <summary>A goal and the steps that lead to its tile from where the player stands (none when already there).</summary>
public sealed record AutoplayPlan(Goal Goal, IReadOnlyList<Direction> Steps);

/// <summary>
/// What the bot remembers of a run (plan 16 · T7): the tiles stood on, the goals done, and those that failed. A goal
/// that fails three times is dropped and reported, so the bot can't loop on one.
/// </summary>
public sealed class AutoplayMemory
{
    public const int MostFailures = 3;

    private readonly HashSet<(string Map, int X, int Y)> visited = new();
    private readonly HashSet<string> done = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> failures = new(StringComparer.Ordinal);
    private readonly List<string> dropped = new();

    public int TilesVisited => visited.Count;
    public IReadOnlyCollection<string> Done => done;
    public IReadOnlyList<string> Dropped => dropped;

    /// <summary>Notes a tile stood on; true the first time.</summary>
    public bool Visit(string map, int x, int y) => visited.Add((map, x, y));

    public bool Visited(string map, int x, int y) => visited.Contains((map, x, y));

    public void Complete(string key) => done.Add(key);

    public bool IsOpen(string key) => !done.Contains(key) && !(failures.TryGetValue(key, out int n) && n >= MostFailures);

    /// <summary>Notes a goal that wasn't reached; true when it has now failed too often and is dropped.</summary>
    public bool Fail(string key)
    {
        failures[key] = failures.GetValueOrDefault(key) + 1;
        if (failures[key] != MostFailures) return false;
        dropped.Add(key);
        return true;
    }
}

/// <summary>
/// The playthrough bot's policy (plan 16 · T7), GPU-free: from the map, the player's tile and how they travel, and
/// what the bot remembers, the next goal in order (the nurse when the team is low, everyone not yet spoken to, every
/// item's ball, every obstacle, every trigger whose variable has its value, every way not taken, then the nearest
/// tile never stood on) and the steps to it. Paths are searched with the field's own rules
/// (<see cref="FieldMovement.Step"/>) within <see cref="Reach"/> tiles of the player, so the map of Sinnoh costs what a
/// town does; a warp's tile and an armed trigger's are ends of a path, never passed through. The caller plans again
/// whenever the player stands still on a tile, which is also every time the story's revision has moved.
/// </summary>
public static class Policy
{
    /// <summary>How far either way the search looks.</summary>
    public const int Reach = 40;

    private static readonly Direction[] Ways = { Direction.Up, Direction.Down, Direction.Left, Direction.Right };

    /// <summary>A tile reached by the search: the height and the way of travel there, the tile it was reached from and by which step.</summary>
    private readonly record struct Node(float Height, TravelMode Mode, int FromX, int FromY, Direction By, int Depth);

    public static string KeyOf(Map map, NPC npc) => $"{map.Name}:{npc.Key ?? npc.Id}";
    public static string WarpKey(Map map, int x, int y) => $"warp:{map.Name}:{x},{y}";
    public static string TriggerKey(Map map, StepTrigger t) => $"trigger:{map.Name}:{t.X},{t.Y}:{t.Variable}={t.Value}";
    public static string FrontierKey(Map map, int x, int y) => $"tile:{map.Name}:{x},{y}";

    /// <summary>
    /// The next goal and the steps to it, or null when nothing within reach is left to do.
    /// </summary>
    /// <param name="armed">Whether a trigger's variable has its value (the story's); none is armed when null.</param>
    /// <param name="heal">The team is low: the nearest nurse comes first, done or not.</param>
    public static AutoplayPlan? Next(Map map, int x, int y, Walker walker, AutoplayMemory memory, Func<StepTrigger, bool>? armed = null,
        bool heal = false, int reach = Reach)
    {
        var triggers = map.Triggers.Where(t => t.X + t.Width > x - reach && t.X <= x + reach && t.Y + t.Depth > y - reach && t.Y <= y + reach
            && (armed?.Invoke(t) ?? false)).ToList();
        var reached = Search(map, x, y, walker, reach, triggers);

        // Ends of a path: a warp's tile and an armed trigger's
        static StepTrigger? TriggerAt(List<StepTrigger> list, int tx, int ty) => list.FirstOrDefault(t => t.Covers(tx, ty));

        // The nurse first when the team is low, then the people and things of the map within reach
        var people = map.NPCs.Near(x, y, reach);
        if (heal && Nearest(map, reached, people.Where(n => n.IsHealingNurse), GoalKind.Heal, _ => true) is { } nurse)
            return nurse;
        // The area the player stands in is done first, everything in its order, then the same within reach: on the map
        // of Sinnoh the people of the next route would otherwise draw the bot on past every door of a town
        var here = map.IsStreamed ? map.AreaAt(x, y) : null;
        foreach (bool local in here != null ? new[] { true, false } : new[] { false })
        {
            bool Near(int tx, int ty) => !local || map.AreaAt(tx, ty) == here;
            foreach (var (kind, which) in new (GoalKind, Func<NPC, bool>)[]
            {
                (GoalKind.Speak, n => !n.IsThing),
                (GoalKind.Item, n => n.IsItemBall),
                (GoalKind.Obstacle, n => n.IsObstacle),
            })
            {
                if (Nearest(map, reached, people.Where(n => which(n) && Near(n.GridX, n.GridY)), kind, n => memory.IsOpen(KeyOf(map, n))) is { } plan) return plan;
            }

            // Triggers, then warps, then the frontier: each the nearest tile of its kind
            if (NearestTile(GoalKind.Trigger, (tx, ty) => Near(tx, ty) && TriggerAt(triggers, tx, ty) is { } t && memory.IsOpen(TriggerKey(map, t)),
                    (tx, ty) => TriggerKey(map, TriggerAt(triggers, tx, ty)!)) is { } trigger) return trigger;
            if (NearestTile(GoalKind.Warp, (tx, ty) => Near(tx, ty) && (tx, ty) != (x, y) && map.GetWarpAt(tx, ty) != null && memory.IsOpen(WarpKey(map, tx, ty)),
                    (tx, ty) => WarpKey(map, tx, ty)) is { } warp) return warp;
            if (NearestTile(GoalKind.Frontier, (tx, ty) => Near(tx, ty) && !memory.Visited(map.Name, tx, ty) && map.GetWarpAt(tx, ty) == null
                    && TriggerAt(triggers, tx, ty) == null && memory.IsOpen(FrontierKey(map, tx, ty)), (tx, ty) => FrontierKey(map, tx, ty)) is { } tile) return tile;
        }
        return null;

        AutoplayPlan? NearestTile(GoalKind kind, Func<int, int, bool> wanted, Func<int, int, string> key)
        {
            (int X, int Y)? best = null;
            Node bestNode = default;
            foreach (var (tile, node) in reached)
            {
                if ((best == null || node.Depth < bestNode.Depth) && wanted(tile.X, tile.Y))
                {
                    best = tile;
                    bestNode = node;
                }
            }
            if (best is not { } at) return null;
            return new AutoplayPlan(new Goal(kind, key(at.X, at.Y), at.X, at.Y, bestNode.By, at.X, at.Y), PathTo(reached, x, y, at.X, at.Y));
        }
    }

    /// <summary>Every tile a walker can reach from a tile within <paramref name="reach"/> tiles, by the field's own rules (a warp's tile ends a path).</summary>
    public static IReadOnlyCollection<(int X, int Y)> Reachable(Map map, int x, int y, Walker walker, int reach = Reach) =>
        Search(map, x, y, walker, reach, new List<StepTrigger>()).Keys;

    private static Dictionary<(int X, int Y), Node> Search(Map map, int x, int y, Walker walker, int reach, List<StepTrigger> triggers)
    {
        var reached = new Dictionary<(int X, int Y), Node> { [(x, y)] = new Node(walker.Height, walker.Mode, x, y, Direction.Down, 0) };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue((x, y));
        while (queue.Count > 0)
        {
            var here = queue.Dequeue();
            var node = reached[here];
            // A path ends on a warp or an armed trigger: going on would take the warp or start the scene
            if (here != (x, y) && (map.GetWarpAt(here.X, here.Y) != null || triggers.Any(t => t.Covers(here.X, here.Y)))) continue;
            foreach (var way in Ways)
            {
                var step = FieldMovement.Step(map, here.X, here.Y, way, walker with { Height = node.Height, Mode = node.Mode, Running = false, Climbing = false });
                if (!step.Moves) continue;
                if (Math.Abs(step.X - x) > reach || Math.Abs(step.Y - y) > reach) continue;
                if (reached.ContainsKey((step.X, step.Y))) continue;
                reached[(step.X, step.Y)] = new Node(step.Height, step.Mode, here.X, here.Y, way, node.Depth + 1);
                queue.Enqueue((step.X, step.Y));
            }
        }
        return reached;
    }

    // The nearest of these people or things that is open, with the tile beside them (or across a counter) to speak from
    private static AutoplayPlan? Nearest(Map map, Dictionary<(int X, int Y), Node> reached, IEnumerable<NPC> who, GoalKind kind, Func<NPC, bool> open)
    {
        AutoplayPlan? best = null;
        int bestDepth = int.MaxValue;
        var start = reached.First(r => r.Value.Depth == 0).Key;
        foreach (var npc in who)
        {
            if (!open(npc)) continue;
            foreach (var way in Ways)
            {
                var (dx, dy) = FieldMovement.Delta(way);
                // Beside them, facing them; or two tiles off with a counter between
                foreach (int far in new[] { 1, 2 })
                {
                    int sx = npc.GridX - dx * far, sy = npc.GridY - dy * far;
                    if (far == 2 && !map.IsCounter(npc.GridX - dx, npc.GridY - dy)) continue;
                    if (!reached.TryGetValue((sx, sy), out var node) || node.Depth >= bestDepth) continue;
                    if ((sx, sy) != start && map.GetWarpAt(sx, sy) != null) continue;
                    bestDepth = node.Depth;
                    string key = kind == GoalKind.Heal ? "heal:" + KeyOf(map, npc) : KeyOf(map, npc);
                    best = new AutoplayPlan(new Goal(kind, key, sx, sy, way, npc.GridX, npc.GridY), PathTo(reached, start.X, start.Y, sx, sy));
                }
            }
        }
        return best;
    }

    private static List<Direction> PathTo(Dictionary<(int X, int Y), Node> reached, int fromX, int fromY, int toX, int toY)
    {
        var steps = new List<Direction>();
        var at = (X: toX, Y: toY);
        while (at != (fromX, fromY))
        {
            var node = reached[at];
            steps.Add(node.By);
            at = (node.FromX, node.FromY);
        }
        steps.Reverse();
        return steps;
    }
}
