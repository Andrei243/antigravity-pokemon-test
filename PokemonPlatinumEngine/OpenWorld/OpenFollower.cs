using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>
/// Whoever walks behind the player in an open map (plan 32 · OW6; the grid's <c>Follower</c>: a Pokémon of the team
/// out of its ball, or a partner who battles beside the player): a breadcrumb trail of the player's own feet, a crumb
/// every tick the feet moved, and the follower walked along it <see cref="Gap"/> behind them, at most
/// <see cref="FastestStep"/> a tick, so they catch up with a running player and fall behind no further. The follower
/// is only ever where the player's feet have been, or between two crumbs a tick apart, so they never cut a corner
/// through a wall the player walked round. A place the player was put down at (a marker, a caught fall, a door come
/// out of), more than <see cref="Jump"/> from the last crumb, starts the trail afresh with the follower on the player.
/// Whole millimetres; GPU-free; ticks only.
/// </summary>
public sealed class OpenFollower
{
    /// <summary>How far behind the player along the trail the follower keeps, in millimetres.</summary>
    public const int Gap = 1100;

    /// <summary>The most the follower goes in a tick, in millimetres (6 m/s: a running player is caught up with).</summary>
    public const int FastestStep = 50;

    /// <summary>A step of the player's feet longer than this, in millimetres, is no step: the player was put down somewhere.</summary>
    public const int Jump = 2000;

    /// <summary>Who follows: the key of the person of the grid's follower, or the Pokémon's look.</summary>
    public string Who { get; }

    /// <summary>The trainer of the data who battles beside the player; null for a Pokémon.</summary>
    public string? TrainerId { get; }

    public OpenPos Pos { get; private set; }
    public int Yaw { get; private set; }

    // The crumbs ahead of the follower, the first the next it walks to; the last is where the player's feet were last
    private readonly List<OpenPos> crumbs = new();

    public OpenFollower(string who, OpenPos player, int yaw, string? trainerId = null)
    {
        Who = who;
        TrainerId = trainerId;
        Start(player, yaw);
    }

    /// <summary>The crumbs ahead of the follower (a test's look at the trail).</summary>
    public IReadOnlyList<OpenPos> Trail => crumbs;

    /// <summary>The trail begun afresh: the follower on the player's feet.</summary>
    public void Start(OpenPos player, int yaw)
    {
        crumbs.Clear();
        crumbs.Add(player);
        Pos = player;
        Yaw = OpenYaw.Normalize(yaw);
    }

    /// <summary>After the player's tick: the crumb of their feet, and the follower walked along the trail to its gap behind them.</summary>
    public void Step(OpenPos player, int playerYaw)
    {
        var last = crumbs[^1];
        if (player.DistanceSquared(last) > (long)Jump * Jump)
        {
            Start(player, playerYaw);
            return;
        }
        if (player != last) crumbs.Add(player);

        // How far the trail runs from the follower to the player's feet
        double left = Length(Pos, crumbs[0]);
        for (int i = 1; i < crumbs.Count; i++) left += Length(crumbs[i - 1], crumbs[i]);
        double go = Math.Min(left - Gap, FastestStep);
        if (go <= 0) return;
        var from = Pos;
        while (go > 0)
        {
            var next = crumbs[0];
            double piece = Length(Pos, next);
            if (piece <= go)
            {
                Pos = next;
                go -= piece;
                if (crumbs.Count == 1) break;
                crumbs.RemoveAt(0);
                continue;
            }
            double s = go / piece;
            Pos = new OpenPos(Pos.X + Round((next.X - Pos.X) * s), Pos.Y + Round((next.Y - Pos.Y) * s), Pos.Z + Round((next.Z - Pos.Z) * s));
            go = 0;
        }
        if (Pos.X != from.X || Pos.Z != from.Z) Yaw = OpenTriangles.YawOf(Pos.X - from.X, Pos.Z - from.Z);
    }

    private static int Round(double v) => (int)Math.Round(v, MidpointRounding.AwayFromZero);

    private static double Length(OpenPos a, OpenPos b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y, dz = b.Z - a.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}
