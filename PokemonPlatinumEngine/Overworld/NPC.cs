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
    public int VisionDistance { get; set; } = 3;
    public bool HasSpottedPlayer { get; set; } = false;
    public float ExclamationTimer { get; set; } = 0f;
    public bool HasBattled { get; set; } = false;

    // Special Interactions
    public bool IsStarterBriefcase { get; set; } = false;
    public bool IsHealingNurse { get; set; } = false;
    public bool IsPokeMartClerk { get; set; } = false;
    public bool IsPCTerminal { get; set; } = false;

    public void FaceTowards(int playerX, int playerY)
    {
        if (playerX < GridX) Facing = Direction.Left;
        else if (playerX > GridX) Facing = Direction.Right;
        else if (playerY < GridY) Facing = Direction.Up;
        else if (playerY > GridY) Facing = Direction.Down;
    }
}
