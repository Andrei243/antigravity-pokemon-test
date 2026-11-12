using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Delve;

/// <summary>
/// A delve's turn after the leader's action (plan 44 · SD7; <c>docs/mechanics/rulings.md</c>, "The delve"): everyone else
/// acts once, the fastest first (<see cref="DelveMon.EffectiveSpeed"/>), ties broken by a draw of the run's each round.
/// The partner and the recruits strike a resident beside them or follow the leader; a resident sleeps until the team comes
/// near, comes by the floor's paths, strikes, and a shy one flees when low. No drawing or input.
/// </summary>
public static class DelveTurns
{
    /// <summary>The word an event of a flinch names (the screen says it in its own words).</summary>
    public const string FlinchWord = "Flinch";

    /// <summary>How near the team must come to wake a sleeping resident.</summary>
    public const int WakeReach = 2;

    /// <summary>How far a resident notices the team and comes after it.</summary>
    public const int SightReach = 8;

    private static readonly Direction[] Ways = { Direction.Up, Direction.Down, Direction.Left, Direction.Right };

    /// <summary>The tile a step that way comes to.</summary>
    public static (int X, int Y) Ahead(int x, int y, Direction way) => way switch
    {
        Direction.Up => (x, y - 1),
        Direction.Down => (x, y + 1),
        Direction.Left => (x - 1, y),
        _ => (x + 1, y)
    };

    /// <summary>The way from one tile to a tile beside it, or null.</summary>
    public static Direction? WayTo(int fromX, int fromY, int toX, int toY) => (toX - fromX, toY - fromY) switch
    {
        (0, -1) => Direction.Up,
        (0, 1) => Direction.Down,
        (-1, 0) => Direction.Left,
        (1, 0) => Direction.Right,
        _ => null
    };

    /// <summary>
    /// Whether a Pokémon may act this turn: asleep, frozen, flinched, a fit of paralysis or confusion's hurt each cost it
    /// the action (and say so). A confusion running out, a thaw and a waking come here.
    /// </summary>
    public static bool MayAct(DelveRun run, DelveMon m)
    {
        if (m.Fainted) return false;
        var p = m.Pokemon;
        if (m.Flinched)
        {
            m.Flinched = false;
            run.Say(DelveEventKind.LostTurn, m.Name, what: FlinchWord);
            return false;
        }
        switch (p.Status)
        {
            case StatusCondition.Sleep:
                if (--p.SleepTurns > 0)
                {
                    run.Say(DelveEventKind.LostTurn, m.Name, what: nameof(StatusCondition.Sleep));
                    return false;
                }
                p.Status = StatusCondition.None;
                run.Say(DelveEventKind.Woke, m.Name);
                return false;
            case StatusCondition.Freeze:
                if (run.Rng.Next(5) != 0)
                {
                    run.Say(DelveEventKind.LostTurn, m.Name, what: nameof(StatusCondition.Freeze));
                    return false;
                }
                p.Status = StatusCondition.None;
                run.Say(DelveEventKind.Cured, m.Name, what: nameof(StatusCondition.Freeze));
                break;
            case StatusCondition.Paralyze:
                if (run.Rng.Next(4) == 0)
                {
                    run.Say(DelveEventKind.LostTurn, m.Name, what: nameof(StatusCondition.Paralyze));
                    return false;
                }
                break;
        }
        if (m.ConfusedTurns > 0)
        {
            if (--m.ConfusedTurns == 0) run.Say(DelveEventKind.Snapped, m.Name);
            else if (run.Rng.Next(3) == 0)
            {
                int hurt = Math.Max(1, p.MaxHP / 8);
                p.CurrentHP = Math.Max(0, p.CurrentHP - hurt);
                run.Say(DelveEventKind.HurtItself, m.Name, amount: hurt);
                if (m.Fainted) DelveMoves.Faint(run, null, m);
                return false;
            }
        }
        return true;
    }

    /// <summary>Everyone but the leader acts once, the fastest first, ties by a draw each.</summary>
    public static void Round(DelveRun run)
    {
        var others = run.Team.Skip(1).Concat(run.Residents).Where(m => !m.Fainted).OrderBy(m => m.Order).ToList();
        var draws = others.ToDictionary(m => m, _ => run.Rng.Next(1 << 16));
        foreach (var m in others.OrderByDescending(m => m.EffectiveSpeed).ThenBy(m => draws[m]).ThenBy(m => m.Order).ToList())
        {
            if (run.Outcome != DelveOutcome.Going) return;
            if (m.Fainted || (!m.OnTeam && !run.Residents.Contains(m)) || run.Asking == m) continue;
            if (m.OnTeam) Teammate(run, m);
            else Resident(run, m);
            run.LeaderFalls();
        }
    }

    private static void Teammate(DelveRun run, DelveMon m)
    {
        if (!run.Team.Contains(m) || !MayAct(run, m)) return;
        var foe = Beside(run, m, foe: true);
        if (foe != null)
        {
            Attack(run, m, foe);
            return;
        }
        // Follow the leader by the floor's paths, to a tile beside the leader
        if (Math.Abs(m.X - run.Leader.X) + Math.Abs(m.Y - run.Leader.Y) <= 1) return;
        if (StepToward(run, m, run.Leader.X, run.Leader.Y) is { } way) MoveTo(run, m, way);
    }

    private static void Resident(DelveRun run, DelveMon m)
    {
        var near = run.Team.Where(t => !t.Fainted).OrderBy(t => m.Reach(t.X, t.Y)).ThenBy(t => t.Order).FirstOrDefault();
        if (near == null) return;
        if (m.Dormant)
        {
            if (m.Reach(near.X, near.Y) > WakeReach) return;
            m.Dormant = false;
            run.Say(DelveEventKind.Noticed, m.Name, near.Name);
            return;
        }
        if (!MayAct(run, m)) return;
        var p = m.Pokemon;
        if (m.Shy && p.CurrentHP * 4 < p.MaxHP)
        {
            if (StepAway(run, m, near) is { } away)
            {
                MoveTo(run, m, away);
                run.Say(DelveEventKind.Fled, m.Name);
            }
            return;
        }
        var foe = Beside(run, m, foe: false);
        if (foe != null)
        {
            Attack(run, m, foe);
            return;
        }
        if (m.Reach(near.X, near.Y) > SightReach) return;
        if (StepToward(run, m, near.X, near.Y) is { } way) MoveTo(run, m, way);
    }

    /// <summary>The first of the other side beside a Pokémon by the four ways, in the ways' order.</summary>
    private static DelveMon? Beside(DelveRun run, DelveMon m, bool foe)
    {
        foreach (var way in Ways)
        {
            var (x, y) = Ahead(m.X, m.Y, way);
            if (run.At(x, y) is { } there && there.OnTeam != m.OnTeam && (foe ? !there.OnTeam : there.OnTeam)) return there;
        }
        return null;
    }

    /// <summary>A strike at one beside: one of its own damaging moves that reaches, one time in two, or the plain strike.</summary>
    private static void Attack(DelveRun run, DelveMon m, DelveMon target)
    {
        var way = WayTo(m.X, m.Y, target.X, target.Y)!.Value;
        m.Facing = way;
        var moves = Enumerable.Range(0, m.Pokemon.Moves.Count)
            .Where(i => DelveMoves.WhyNot(m, i) == null && m.Pokemon.Moves[i].Data.Category != MoveCategory.Status
                        && DelveMoves.ReachOf(m.Pokemon.Moves[i].Data) is DelveReach.Ahead or DelveReach.Round)
            .ToList();
        if (moves.Count > 0 && run.Rng.Next(2) == 0) DelveMoves.Use(run, m, moves[run.Rng.Next(moves.Count)], way);
        else DelveMoves.Strike(run, m, way);
    }

    private static void MoveTo(DelveRun run, DelveMon m, Direction way)
    {
        var (x, y) = Ahead(m.X, m.Y, way);
        m.Facing = way;
        m.X = x;
        m.Y = y;
    }

    /// <summary>The first step of a shortest way to a tile beside a goal (by the four ways, in their order), past nobody.</summary>
    public static Direction? StepToward(DelveRun run, DelveMon m, int goalX, int goalY)
    {
        var start = (m.X, m.Y);
        var first = new Dictionary<(int, int), Direction> { [start] = Direction.Down };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var at = queue.Dequeue();
            foreach (var way in Ways)
            {
                var next = Ahead(at.X, at.Y, way);
                if (first.ContainsKey(next) || !run.Floor.Passable(next.X, next.Y, m.Swims)) continue;
                if (next == (goalX, goalY)) return at == start ? null : first[at];
                if (run.At(next.X, next.Y) != null) continue;
                first[next] = at == start ? way : first[at];
                if (Math.Abs(next.X - goalX) + Math.Abs(next.Y - goalY) == 1) return first[next];
                queue.Enqueue(next);
            }
        }
        return null;
    }

    /// <summary>A step that takes a Pokémon furthest from another, or null where none does.</summary>
    private static Direction? StepAway(DelveRun run, DelveMon m, DelveMon from)
    {
        Direction? best = null;
        int bestD = Math.Abs(m.X - from.X) + Math.Abs(m.Y - from.Y);
        foreach (var way in Ways)
        {
            var (x, y) = Ahead(m.X, m.Y, way);
            if (!run.Floor.Passable(x, y, m.Swims) || run.At(x, y) != null) continue;
            int d = Math.Abs(x - from.X) + Math.Abs(y - from.Y);
            if (d > bestD) (best, bestD) = (way, d);
        }
        return best;
    }
}
