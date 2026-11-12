using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Delve;

/// <summary>
/// A plain way of playing a run (plan 44 · SD7), for the tests and the harness: say yes to every recruit, eat or heal when
/// low, strike whatever stands beside the leader with its best move that reaches or the plain strike, and otherwise walk to
/// the stairs by the shortest way. Its choice is the run's state alone, so a run played by it is the same for a seed.
/// No drawing or input.
/// </summary>
public static class DelvePolicy
{
    private static readonly Direction[] Ways = { Direction.Up, Direction.Down, Direction.Left, Direction.Right };

    public static DelveAction Next(DelveRun run)
    {
        if (run.Asking != null) return DelveAction.Answer(true);
        var leader = run.Leader;
        var p = leader.Pokemon;
        if (p.CurrentHP * 3 < p.MaxHP)
            foreach (var item in new[] { "Sitrus Berry", "Super Potion", "Potion", "Oran Berry" })
                if (run.WhyNot(DelveAction.Use(item)) == null) return DelveAction.Use(item);
        if (run.Hunger <= 20)
            foreach (var item in run.Bag.Where(DelveItems.IsFood).Distinct().OrderBy(i => i).ToList())
                if (run.WhyNot(DelveAction.Use(item)) == null) return DelveAction.Use(item);
        if (p.Status != StatusCondition.None)
            foreach (var item in run.Bag.Where(i => DelveItems.Cures(i) != null).Distinct().OrderBy(i => i).ToList())
                if (run.WhyNot(DelveAction.Use(item)) == null && DelveItems.Cures(item) == p.Status) return DelveAction.Use(item);

        foreach (var way in Ways)
        {
            var (x, y) = DelveTurns.Ahead(leader.X, leader.Y, way);
            if (run.At(x, y) is not { OnTeam: false }) continue;
            int best = -1, power = 0;
            for (int i = 0; i < p.Moves.Count; i++)
            {
                var data = p.Moves[i].Data;
                if (DelveMoves.WhyNot(leader, i) != null || data.Category == MoveCategory.Status) continue;
                if (DelveMoves.ReachOf(data) is not (DelveReach.Ahead or DelveReach.Round or DelveReach.Room)) continue;
                if (data.Power > power) (best, power) = (i, data.Power);
            }
            return best >= 0 && power > DelveMoves.StrikePower ? DelveAction.UseMove(best, way) : DelveAction.Strike(way);
        }

        // A resident awake and close is met, not walked round: one that comes after the team steps into whichever way
        // round is free, and the leader would go to and fro for ever (one that flees, low on HP, is let go)
        var chaser = run.Residents
            .Where(r => !r.Fainted && !r.Dormant && !(r.Shy && r.Pokemon.CurrentHP * 4 < r.Pokemon.MaxHP) && leader.Reach(r.X, r.Y) <= ChaseReach)
            .OrderBy(r => leader.Reach(r.X, r.Y)).ThenBy(r => r.Order).FirstOrDefault();
        if (chaser != null && DelveTurns.StepToward(run, leader, chaser.X, chaser.Y) is { } toward)
            return DelveAction.Step(toward);

        // To the stairs, the shortest way past nobody, or past residents where none is free or the free one is the longer
        // by more than a step (and strike the one in the way): a resident that keeps stepping into whichever way round is
        // free would otherwise send the leader to and fro for ever
        var free = PathStep(run, true);
        var any = PathStep(run, false);
        var step = free is { } f && (any is not { } a || f.Length <= a.Length + DetourSteps) ? f.Way : any?.Way;
        if (step is { } go)
        {
            var (x, y) = DelveTurns.Ahead(leader.X, leader.Y, go);
            return run.At(x, y) is { OnTeam: false } ? DelveAction.Strike(go) : DelveAction.Step(go);
        }
        return DelveAction.Wait;
    }

    /// <summary>How many steps longer the way round a resident may be before the leader goes through it instead.</summary>
    public const int DetourSteps = 1;

    /// <summary>How near a resident awake must be for the leader to go and meet it.</summary>
    public const int ChaseReach = 4;

    private static (Direction Way, int Length)? PathStep(DelveRun run, bool pastNobody)
    {
        var leader = run.Leader;
        var start = (leader.X, leader.Y);
        var goal = run.Floor.Stairs;
        var first = new Dictionary<(int, int), Direction> { [start] = Direction.Down };
        var steps = new Dictionary<(int, int), int> { [start] = 0 };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var at = queue.Dequeue();
            foreach (var way in Ways)
            {
                var next = DelveTurns.Ahead(at.X, at.Y, way);
                if (first.ContainsKey(next) || !run.Floor.Passable(next.X, next.Y, leader.Swims)) continue;
                if (pastNobody && run.At(next.X, next.Y) is { OnTeam: false }) continue;
                first[next] = at == start ? way : first[at];
                steps[next] = steps[at] + 1;
                if (next == goal) return (first[next], steps[next]);
                queue.Enqueue(next);
            }
        }
        return null;
    }
}
