using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Overworld;

public class NPC
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Townsperson";
    public string NpcType { get; set; } = "Trainer"; // "Rowan", "Rival", "Nurse", "Clerk", "Youngster", "Lass", "StarterBriefcase"
    public int GridX { get; set; }
    public int GridY { get; set; }
    public Direction Facing { get; set; } = Direction.Down;
    public List<string> DialogLines { get; set; } = new();

    // Trainer specific
    public bool IsTrainer { get; set; } = false;
    public Trainer? TrainerData { get; set; }
    public bool HasSpottedPlayer { get; set; } = false;
    public float ExclamationTimer { get; set; } = 0f;
    public bool HasBattled { get; set; } = false;

    // Walking (a trainer stepping up to the player): the grid position is where the current step ends
    public float StepOffsetX { get; set; }
    public float StepOffsetY { get; set; }
    public float WalkCycle { get; set; }
    public float WalkBlend { get; set; }

    /// <summary>Where the character is drawn, in tiles (differs from the grid position mid-step).</summary>
    public float DrawX => GridX + StepOffsetX;
    public float DrawY => GridY + StepOffsetY;

    private (int X, int Y, Direction Facing)? post;

    // Special Interactions
    public bool IsStarterBriefcase { get; set; } = false;
    public bool IsHealingNurse { get; set; } = false;
    public bool IsPokeMartClerk { get; set; } = false;
    public bool IsPCTerminal { get; set; } = false;

    /// <summary>Takes the player to the next region (a ferry sailor, say) once this region's story is finished.</summary>
    public bool IsTransportAttendant { get; set; } = false;

    /// <summary>Remembers where the trainer was standing, before they walk up to the player.</summary>
    public void LeavePost() => post ??= (GridX, GridY, Facing);

    /// <summary>
    /// A beaten trainer stays where the battle happened and doesn't challenge again. One who won goes back to
    /// their post with a healed team, so the player can return for a rematch.
    /// </summary>
    public void FinishBattle(bool playerWon)
    {
        if (playerWon)
        {
            HasBattled = true;
            return;
        }

        TrainerData?.Party.HealAll();
        if (post.HasValue)
        {
            (GridX, GridY, Facing) = post.Value;
        }
    }

    public void FaceTowards(int playerX, int playerY)
    {
        if (playerX < GridX) Facing = Direction.Left;
        else if (playerX > GridX) Facing = Direction.Right;
        else if (playerY < GridY) Facing = Direction.Up;
        else if (playerY > GridY) Facing = Direction.Down;
    }
}
