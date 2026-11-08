using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>
/// A trainer of an open map catching the player's eye (plan 32 · OW6, the grid's <c>TrainerApproach</c> on a cone):
/// the "!" over them for <see cref="ExclaimTicks"/>, then a walk up the navigation mesh to a talking distance
/// (<see cref="TalkDistance"/>) of where the player stands, the two face to face, and a moment before the challenge.
/// The player stands still meanwhile (the engine's). The rules of the grid's are kept: one trainer at a time, or two
/// who see the player at once together, and nobody who is beaten. The trainer's capsule is walked by the field
/// (<see cref="OpenField.StepTheRest"/>); this only watches it and says what comes next. GPU-free; ticks only.
/// </summary>
public sealed class OpenApproach
{
    /// <summary>The "!" before the walk, and the moment face to face after it, in ticks (the grid's 0.7 s and 0.2 s).</summary>
    public const int ExclaimTicks = 84, TurnTicks = 24;

    /// <summary>How near the player on the ground the trainer's feet come, in millimetres: two bodies' width and a little.</summary>
    public const int TalkDistance = 1100;

    public OpenPerson Trainer { get; }

    /// <summary>Whether the player turns to face this trainer once they stand face to face: the first of two who came together.</summary>
    public bool TurnPlayer { get; }

    /// <summary>The "!" is over the trainer's head.</summary>
    public bool Exclaiming => phase == 0;

    /// <summary>The trainer stands face to face with the player, and has said nothing yet.</summary>
    public bool IsDone => phase == 3;

    /// <summary>Whether the trainer's walk was given up short of the player (they speak from where they stand).</summary>
    public bool Stopped { get; private set; }

    private int phase, ticks;

    public OpenApproach(OpenPerson trainer, bool turnPlayer = true)
    {
        Trainer = trainer;
        TurnPlayer = turnPlayer;
        trainer.Held = true;
        trainer.Walk = null;
    }

    /// <summary>
    /// The first trainer of the map within <see cref="OpenSight.Longest"/> of the player who is there, waiting
    /// (<paramref name="waiting"/>: not beaten), held by nothing, and whose cone sees the player; one other than
    /// <paramref name="except"/> (and not the same trainer of the data) when given. In the map's order.
    /// </summary>
    public static OpenPerson? FindSpotter(OpenMap map, OpenPos player, Func<OpenPerson, bool> waiting, OpenPerson? except = null)
    {
        foreach (var p in map.PeopleNear(player, OpenSight.Longest))
        {
            if (p == except || !p.Present || p.Held || p.TrainerId == null || p.Sight is not { } sight) continue;
            if (except != null && string.Equals(p.TrainerId, except.TrainerId, StringComparison.OrdinalIgnoreCase)) continue;
            if (!waiting(p) || !sight.Sees(map.Ground, p.Pos, p.Yaw, player)) continue;
            return p;
        }
        return null;
    }

    /// <summary>After a tick of the field: the "!" counted, the walk up begun and watched, the two turned face to face, and the moment counted.</summary>
    public void Watch(OpenMap map, OpenPlayer player)
    {
        if (IsDone) return;
        ticks++;
        switch (phase)
        {
            case 0:
                if (ticks < ExclaimTicks) return;
                if (Near(player))
                {
                    FaceToFace(player);
                    return;
                }
                Trainer.Walk = OpenPathWalk.Over(map, Trainer.Pos, player.Pos, OpenWalkKind.WalkUp, stopShort: TalkDistance, straight: true);
                phase = 1;
                return;
            case 1:
                if (Near(player) || Trainer.Walk is not { Done: false })
                {
                    Stopped = !Near(player);
                    Trainer.Walk = null;
                    FaceToFace(player);
                }
                return;
            case 2:
                if (ticks >= TurnTicks) phase = 3;
                return;
        }
    }

    private bool Near(OpenPlayer player) => Trainer.Pos.GroundDistanceSquared(player.Pos) <= (long)TalkDistance * TalkDistance;

    private void FaceToFace(OpenPlayer player)
    {
        var t = Trainer.Pos;
        var p = player.Pos;
        if (t.X != p.X || t.Z != p.Z)
        {
            Trainer.Body.Face(OpenTriangles.YawOf(p.X - t.X, p.Z - t.Z));
            if (TurnPlayer) player.Body.Face(OpenTriangles.YawOf(t.X - p.X, t.Z - p.Z));
        }
        phase = 2;
        ticks = 0;
    }
}
