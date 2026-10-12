using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>How a step of a script's walk is taken (plan 11 · C11).</summary>
public enum ScriptStepKind
{
    /// <summary>A step onto the next tile, turning to face it.</summary>
    Walk,

    /// <summary>A step on the spot, turning to face the way (the original's <c>WalkOnSpot</c>).</summary>
    OnSpot,

    /// <summary>A step onto the next tile, facing as before (the original's <c>LockDir</c>): a step back when the way is behind.</summary>
    Back
}

/// <summary>One step of a script's walk: which way, and how it is taken (<c>walk self onspot down back up</c>).</summary>
public readonly record struct ScriptStep(Direction Way, ScriptStepKind Kind = ScriptStepKind.Walk)
{
    /// <summary>Plain steps, each onto the next tile and facing it.</summary>
    public static IEnumerable<ScriptStep> Walking(IEnumerable<Direction> path) => path.Select(d => new ScriptStep(d));

    /// <summary>Whether the step moves them off their tile.</summary>
    public bool Moves => Kind != ScriptStepKind.OnSpot;

    /// <summary>Whether the step turns them to face its way.</summary>
    public bool Turns => Kind != ScriptStepKind.Back;
}

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

    private readonly Queue<ScriptStep> steps;
    private readonly float pace;
    private bool stepping;
    private float progress, settling = SettleTime;
    private int dx, dy;

    public NPC Who { get; }

    /// <summary>True once they stand where the walk ends, legs still.</summary>
    public bool IsDone { get; private set; }

    public NpcWalk(NPC who, IEnumerable<Direction> path, bool fast) : this(who, ScriptStep.Walking(path), fast ? RunPace : WalkPace)
    {
    }

    /// <summary>A script's walk, whose steps may be on the spot or keep the facing (plan 11 · C11).</summary>
    public NpcWalk(NPC who, IEnumerable<ScriptStep> path, bool fast) : this(who, path, fast ? RunPace : WalkPace)
    {
    }

    /// <summary>A walk at a pace of its own, in tiles a second: someone keeping up with the player.</summary>
    public NpcWalk(NPC who, IEnumerable<Direction> path, float pace) : this(who, ScriptStep.Walking(path), pace)
    {
    }

    private NpcWalk(NPC who, IEnumerable<ScriptStep> path, float pace)
    {
        Who = who;
        steps = new Queue<ScriptStep>(path);
        this.pace = pace;
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

        var step = steps.Dequeue();
        // A step on the spot turns them and walks the legs where they stand; a step back keeps their facing
        (dx, dy) = step.Moves ? FieldMovement.Delta(step.Way) : (0, 0);
        if (step.Turns) Who.Facing = step.Way;
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

    private readonly Queue<ScriptStep> steps;
    private readonly bool fast;
    private float stuck;

    // The step at the head of the queue has begun: a step on the spot or a step keeping the facing, which the
    // player's own steering doesn't take
    private bool begun;

    public bool IsDone => steps.Count == 0;

    /// <summary>True when the walk ended at something in the way.</summary>
    public bool Blocked { get; private set; }

    public PlayerWalk(IEnumerable<Direction> path, bool fast) : this(ScriptStep.Walking(path), fast)
    {
    }

    /// <summary>A script's walk, whose steps may be on the spot or keep the facing (plan 11 · C11).</summary>
    public PlayerWalk(IEnumerable<ScriptStep> path, bool fast)
    {
        steps = new Queue<ScriptStep>(path);
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

        Func<bool> arrived = () =>
        {
            onArrive?.Invoke();
            return false;
        };
        var step = steps.Peek();
        switch (step.Kind)
        {
            case ScriptStepKind.OnSpot:
                // The step before ends first; then the player turns and walks a step's time where they stand
                if (!begun)
                {
                    if (player.IsMoving || !player.StepOnSpot(step.Way, fast))
                    {
                        player.Advance(dt, map, null, fast, null, null, arrived);
                        return;
                    }
                    begun = true;
                }
                player.Advance(dt, map, null, fast, null, null, arrived);
                if (!player.IsSteppingOnSpot) Next();
                return;

            case ScriptStepKind.Back:
                if (!begun)
                {
                    if (player.IsMoving || player.IsSteppingOnSpot)
                    {
                        player.Advance(dt, map, null, fast, null, null, arrived);
                        return;
                    }
                    if (!player.StepKeepingFacing(map, step.Way))
                    {
                        // Something is in the way: given up after a moment, as a walk into a wall is
                        player.StandStill(dt);
                        stuck += dt;
                        if (stuck < GiveUpAfter) return;
                        steps.Clear();
                        Blocked = true;
                        return;
                    }
                    begun = true;
                }
                player.Advance(dt, map, null, fast, null, null, arrived);
                if (!player.IsMoving) Next();
                return;
        }

        bool wasMoving = player.IsMoving;
        player.Advance(dt, map, step.Way, fast, null, null, arrived);

        if (wasMoving && !player.IsMoving)
        {
            Next();
        }
        else if (!wasMoving && !player.IsMoving && player.Facing == step.Way)
        {
            stuck += dt;
            if (stuck < GiveUpAfter) return;
            steps.Clear();
            Blocked = true;
        }
    }

    private void Next()
    {
        steps.Dequeue();
        stuck = 0f;
        begun = false;
    }
}
