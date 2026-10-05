using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// Someone walking where a script sends them, a tile at a time (plan 02 · S1). Like the original's scripted
/// movement it asks nothing of what is in the way: the script knows the place. The person's grid position is
/// where the step in progress ends, and the offset slides them there.
/// </summary>
public sealed class NpcWalk
{
    /// <summary>Tiles a second, walking (a trainer's pace as they step up) and hurrying.</summary>
    public const float WalkPace = TrainerApproach.StepsPerSecond, RunPace = 8f;

    private const float SettleTime = 0.12f;

    private readonly Queue<Direction> steps;
    private readonly float pace;
    private bool stepping;
    private float progress, settling = SettleTime;
    private int dx, dy;

    public NPC Who { get; }

    /// <summary>True once they stand where the walk ends, legs still.</summary>
    public bool IsDone { get; private set; }

    public NpcWalk(NPC who, IEnumerable<Direction> path, bool fast)
    {
        Who = who;
        steps = new Queue<Direction>(path);
        pace = fast ? RunPace : WalkPace;
    }

    public void Update(float dt)
    {
        if (IsDone) return;
        if (!stepping && steps.Count > 0) Begin();

        Who.WalkBlend += ((stepping ? 1f : 0f) - Who.WalkBlend) * Math.Min(1f, dt * 12f);
        if (!stepping)
        {
            // The last step is taken: the legs come to rest, and then the walk is over
            settling -= dt;
            if (settling <= 0f)
            {
                Who.WalkBlend = 0f;
                IsDone = true;
            }
            return;
        }

        progress += pace * dt;
        Who.WalkCycle += pace * dt * 0.5f;
        while (stepping && progress >= 1f)
        {
            progress -= 1f;
            Begin();
        }
        if (stepping)
        {
            Who.StepOffsetX = -dx * (1f - progress);
            Who.StepOffsetY = -dy * (1f - progress);
        }
    }

    /// <summary>Puts them at the end of the walk at once.</summary>
    public void Finish()
    {
        while (steps.Count > 0) Begin();
        Who.StepOffsetX = Who.StepOffsetY = 0f;
        Who.WalkBlend = 0f;
        stepping = false;
        IsDone = true;
    }

    private void Begin()
    {
        Who.StepOffsetX = Who.StepOffsetY = 0f;
        stepping = steps.Count > 0;
        if (!stepping) return;

        var direction = steps.Dequeue();
        (dx, dy) = FieldMovement.Delta(direction);
        Who.Facing = direction;
        Who.GridX += dx;
        Who.GridY += dy;
        Who.StepOffsetX = -dx;
        Who.StepOffsetY = -dy;
    }
}

/// <summary>
/// The player walking where a script sends them. The steps are the player's own (<see cref="Player.Advance"/>),
/// so stairs, ledges and bridges are taken as the keys would take them; no wild Pokémon appears and no door is
/// gone through on the way. A step that can't be taken is given up after a moment, and the rest of the walk with
/// it, so a script can't leave the game waiting at a wall.
/// </summary>
public sealed class PlayerWalk
{
    public const float GiveUpAfter = 0.75f;

    private readonly Queue<Direction> steps;
    private readonly bool fast;
    private float stuck;

    public bool IsDone => steps.Count == 0;

    /// <summary>True when the walk ended at something in the way.</summary>
    public bool Blocked { get; private set; }

    public PlayerWalk(IEnumerable<Direction> path, bool fast)
    {
        steps = new Queue<Direction>(path);
        this.fast = fast;
    }

    /// <param name="onArrive">Called at the end of each step (what a step leaves behind: a print, dust).</param>
    public void Update(float dt, Player player, Map map, Action? onArrive = null)
    {
        if (IsDone)
        {
            player.StandStill(dt);
            return;
        }

        bool wasMoving = player.IsMoving;
        player.Advance(dt, map, steps.Peek(), fast, null, null, () =>
        {
            onArrive?.Invoke();
            return false;
        });

        if (wasMoving && !player.IsMoving)
        {
            steps.Dequeue();
            stuck = 0f;
        }
        else if (!wasMoving && !player.IsMoving && player.Facing == steps.Peek())
        {
            stuck += dt;
            if (stuck < GiveUpAfter) return;
            steps.Clear();
            Blocked = true;
        }
    }
}
