using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// Someone travelling with the player (plan 02 · S6; the original's <c>MOVEMENT_TYPE_FOLLOW_PLAYER</c> and its
/// partner trainer, <c>VAR_PARTNER_TRAINER_ID</c>): Cheryl through Eterna Forest, later Mira, Riley, Marley and Buck.
/// They walk a step behind, onto each tile the player has just left, at the player's own pace, and never stand
/// in the player's way: walking back into them swaps the two of them round. The battles it changes are the
/// engine's (<see cref="TrainerId"/> battles beside the player). No drawing or input.
/// </summary>
public sealed class Follower
{
    private readonly Queue<(int X, int Y)> trail = new();
    private NpcWalk? step;

    /// <summary>The person of the map who walks behind the player.</summary>
    public NPC Who { get; }

    /// <summary>The trainer of Platinum's data who battles beside the player while they travel together.</summary>
    public string TrainerId { get; }

    public Follower(NPC who, string trainerId)
    {
        Who = who;
        TrainerId = trainerId;
    }

    /// <summary>Whether they are on their way somewhere.</summary>
    public bool Moving => step != null || trail.Count > 0;

    /// <summary>The player has set off from a tile: the follower goes there next.</summary>
    public void PlayerLeft(int x, int y)
    {
        if ((x, y) == (Who.GridX, Who.GridY)) return;
        trail.Enqueue((x, y));
        // Someone who falls far behind (a long slide, a ride) is brought up to the last tiles but two
        while (trail.Count > 2)
        {
            var skipped = trail.Dequeue();
            step?.Finish();
            step = null;
            (Who.GridX, Who.GridY) = skipped;
            Who.StepOffsetX = Who.StepOffsetY = 0f;
        }
    }

    /// <summary>
    /// A frame: the step under way goes on at <paramref name="tilesPerSecond"/>, and the next one begins toward the
    /// tile the player left, straight along each axis in turn.
    /// </summary>
    public void Update(float dt, float tilesPerSecond)
    {
        if (step != null)
        {
            step.Update(dt);
            if (!step.IsDone && (Who.StepOffsetX != 0f || Who.StepOffsetY != 0f)) return;
            if (!step.IsDone && trail.Count == 0) return;
            step.Finish();
            step = null;
        }
        if (trail.Count == 0) return;

        var (tx, ty) = trail.Dequeue();
        var path = new List<Direction>();
        for (int x = Who.GridX; x != tx; x += Math.Sign(tx - x)) path.Add(tx > x ? Direction.Right : Direction.Left);
        for (int y = Who.GridY; y != ty; y += Math.Sign(ty - y)) path.Add(ty > y ? Direction.Down : Direction.Up);
        if (path.Count == 0) return;
        step = new NpcWalk(Who, path, Math.Max(NpcWalk.WalkPace, tilesPerSecond));
    }

    /// <summary>Puts them on the tile behind the player at once, facing the way the player faces: a door gone through, a save loaded.</summary>
    public void Behind(int playerX, int playerY, Direction facing)
    {
        trail.Clear();
        step?.Finish();
        step = null;
        var (dx, dy) = FieldMovement.Delta(facing);
        (Who.GridX, Who.GridY) = (playerX - dx, playerY - dy);
        Who.Facing = facing;
        Who.StepOffsetX = Who.StepOffsetY = 0f;
        Who.WalkBlend = 0f;
    }

    /// <summary>Ends a step under way where it was going: a script takes them over.</summary>
    public void Settle()
    {
        trail.Clear();
        step?.Finish();
        step = null;
    }
}
