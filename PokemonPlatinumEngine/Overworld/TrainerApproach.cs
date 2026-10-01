using System;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// A trainer catching the player's eye, as in Platinum: a "!" pops up over the trainer, who then walks up to the
/// player; the player turns to face them and the challenge follows. The player can't move while it plays.
/// </summary>
public sealed class TrainerApproach
{
    public const float ExclaimTime = 0.7f;
    public const float StepsPerSecond = 4.5f; // the player's walking pace
    public const float TurnTime = 0.2f;

    public NPC Trainer { get; }

    /// <summary>The trainer stands in front of the player, face to face.</summary>
    public bool IsDone { get; private set; }

    private readonly int dx, dy;
    private int stepsLeft;
    private bool stepping;
    private float stepProgress;
    private float turnTimer = TurnTime;

    public TrainerApproach(NPC trainer, Player player)
    {
        Trainer = trainer;
        (dx, dy) = Step(trainer.Facing);
        stepsLeft = Math.Abs(player.GridX - trainer.GridX) + Math.Abs(player.GridY - trainer.GridY) - 1;

        trainer.LeavePost();
        trainer.HasSpottedPlayer = true;
        trainer.ExclamationTimer = ExclaimTime;
    }

    /// <summary>The first trainer still waiting for a battle who is looking at the tile, or null.</summary>
    public static NPC? FindSpotter(Map map, int x, int y) =>
        map.NPCs.FirstOrDefault(n => n.IsTrainer && !n.HasBattled && n.TrainerData != null && CanSee(map, n, x, y));

    /// <summary>
    /// Trainers look straight ahead for as many tiles as their sight range, and not past anything they
    /// couldn't walk through (walls, trees, ledges, other people).
    /// </summary>
    public static bool CanSee(Map map, NPC npc, int x, int y)
    {
        var (sx, sy) = Step(npc.Facing);
        int range = npc.TrainerData?.SightRange ?? 0;
        for (int d = 1; d <= range; d++)
        {
            int tx = npc.GridX + sx * d, ty = npc.GridY + sy * d;
            if (tx == x && ty == y) return true;
            if (!map.IsWalkable(tx, ty)) return false;
        }
        return false;
    }

    public void Update(float dt, Player player)
    {
        if (IsDone) return;
        player.StandStill(dt);

        // The "!" comes first; the trainer stays put until it is gone
        if (Trainer.HasSpottedPlayer)
        {
            Trainer.ExclamationTimer -= dt;
            if (Trainer.ExclamationTimer > 0f) return;
            Trainer.ExclamationTimer = 0f;
            Trainer.HasSpottedPlayer = false;
            BeginStep(player);
            return;
        }

        Trainer.WalkBlend += ((stepping ? 1f : 0f) - Trainer.WalkBlend) * Math.Min(1f, dt * 12f);

        if (stepping)
        {
            stepProgress += StepsPerSecond * dt;
            Trainer.WalkCycle += StepsPerSecond * dt * 0.5f;
            if (stepProgress >= 1f)
            {
                BeginStep(player);
            }
            else
            {
                Trainer.StepOffsetX = -dx * (1f - stepProgress);
                Trainer.StepOffsetY = -dy * (1f - stepProgress);
            }
            return;
        }

        // Face to face for a moment before the trainer speaks
        turnTimer -= dt;
        if (turnTimer <= 0f)
        {
            Trainer.WalkBlend = 0f;
            IsDone = true;
        }
    }

    /// <summary>Starts the next step toward the player, or stops next to them and has the player turn around.</summary>
    private void BeginStep(Player player)
    {
        Trainer.StepOffsetX = Trainer.StepOffsetY = 0f;
        stepProgress = 0f;
        stepping = stepsLeft > 0;

        if (stepping)
        {
            // The grid position is where the step ends; the offset slides the character there
            stepsLeft--;
            Trainer.GridX += dx;
            Trainer.GridY += dy;
            Trainer.StepOffsetX = -dx;
            Trainer.StepOffsetY = -dy;
        }
        else
        {
            player.Facing = Opposite(Trainer.Facing);
        }
    }

    private static (int X, int Y) Step(Direction facing) => facing switch
    {
        Direction.Up => (0, -1),
        Direction.Down => (0, 1),
        Direction.Left => (-1, 0),
        _ => (1, 0)
    };

    private static Direction Opposite(Direction facing) => facing switch
    {
        Direction.Up => Direction.Down,
        Direction.Down => Direction.Up,
        Direction.Left => Direction.Right,
        _ => Direction.Left
    };
}
