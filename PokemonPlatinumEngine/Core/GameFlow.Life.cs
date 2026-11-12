using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The player's life (plan 43): the card they design (LF1), and where the phrase picker goes back to when the designer
/// asked for the greeting.
/// </summary>
public sealed partial class GameFlow
{
    /// <summary>The Trainer Card as the player designed it (plan 43 · LF1); null for a card never designed.</summary>
    public CardDesign? CardDesign { get; set; }

    /// <summary>The card designer (plan 43 · LF1).</summary>
    public CardDesignScreen CardDesignScreen { get; } = new();

    /// <summary>The salon's chair and the dye house's counter (plan 43 · LF3).</summary>
    public SalonScreen SalonScreen { get; } = new();

    /// <summary>The player's photo book and its screen (plan 43 · LF4).</summary>
    public PhotoBook PhotoBook { get; } = new();
    public PhotoBookScreen PhotoBookScreen { get; } = new();

    /// <summary>
    /// Where the phrase picker goes back to (plan 43 · LF1): the field, as a script's <c>phrase</c> has it, or the card
    /// designer, whose greeting it is, which hears no answer.
    /// </summary>
    public GameState PhrasesReturn { get; set; } = GameState.Overworld;
}
