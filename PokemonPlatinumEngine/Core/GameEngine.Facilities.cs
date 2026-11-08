using PokemonPlatinumEngine.Models.Facilities;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The later games' battle places in the game (plan 37 · FC2): where a script's <c>facility</c> meets the engine. A
/// place's challenge (<see cref="StreakChallenge"/>) lives between the desk and the battle room as the venues' round does,
/// so a game closed in the middle of one finds none, and the next one begun at the desk ends the streak; a challenge put
/// down is the records' own and is saved with them. Its battles are the Frontier's (<c>StartFrontierBattle</c>): copies,
/// nothing won or lost but the streak, and the bag's key items in force as in any battle (<c>BattleEngine</c> reads the
/// Key Stone, the Z-Ring and the rest from the bag it is handed), its board the Frontier's records screen
/// (<see cref="Models.Frontier.FrontierBoard.ForFacility"/>, <c>FrontierAct("facilityboard")</c>).
/// </summary>
public partial class GameEngine
{
    private sealed partial class FieldHost
    {
        public StreakChallenge? Facility { get => game.flow.Facility; set => game.flow.Facility = value; }
    }
}
