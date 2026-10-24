namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The Pokéathlon in the game (plan 15 · E6): the dome's clerk is <see cref="AthlonDeskScript"/>, whose
/// <c>athlon open</c> opens <see cref="UI.AthlonScreen"/> (<see cref="GameState.Athlon"/>) on the team and the records
/// (<c>GameFlow.Athlon</c>, saved as <c>SaveData.Athlon</c>). Nobody stands at the counter yet: plan 20 · J13 opens the
/// dome's rooms behind Route 35's gate house and puts the clerk there with this script.
/// </summary>
public partial class GameEngine
{
    /// <summary>The dome's counter: the course asked for, run and spoken of.</summary>
    public const string AthlonDeskScript = "common.AthlonDesk";
}
