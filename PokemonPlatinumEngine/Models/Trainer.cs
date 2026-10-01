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

    public string FullTitle => $"{TrainerClass} {Name}";
}
