using System.Collections.Generic;

namespace PokemonPlatinumEngine.Models;

public class Trainer
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TrainerClass { get; set; } = "Trainer"; // e.g. "Youngster", "Lass", "Rival", "Champion"
    public Party Party { get; init; } = new();
    public int PrizeMoney { get; set; } = 300;
    public string DialogueBefore { get; set; } = string.Empty;
    public string DialogueAfter { get; set; } = string.Empty;
    public bool IsDefeated { get; set; } = false;
    public int SightRange { get; set; } = 3; // Tiles of vision for line of sight challenge

    /// <summary>Battles two Pokémon at a time (twins, couples), if the player has two that can fight.</summary>
    public bool DoubleBattle { get; set; }

    /// <summary>How the trainer thinks in battle (plan 06 · R9): Platinum's flags from the trainer's data.</summary>
    public Battle.Sim.Ai.AiFlags Ai { get; set; } = Battle.Sim.Ai.AiFlags.Basic;

    /// <summary>The items the trainer can use in battle, by name (Platinum's: up to four, Potions to Full Restores).</summary>
    public List<string> Items { get; set; } = new();

    /// <summary>
    /// The team, prize money and mind were taken from Platinum's data (<c>trainers.json</c>), because the map wrote
    /// only the trainer's id: a map written back out leaves them to the data again (<c>MapFile.FromMap</c>).
    /// </summary>
    public bool FromPlatinum { get; set; }

    public string FullTitle => $"{TrainerClass} {Name}";
}
