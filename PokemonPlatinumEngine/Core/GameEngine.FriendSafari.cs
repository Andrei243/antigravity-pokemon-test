using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Kalos's Friend Safari in the game (plan 23, Kalos finished; <see cref="FriendSafari"/>): the grounds' grass is laid
/// with the table of the grounds the guide sent the player to, as the player comes onto them (a door, a warp, a save
/// loaded there). The map's own file keeps no table, since which three species live there is the Trainer's own.
/// </summary>
public partial class GameEngine
{
    /// <summary>The Friend Safari's gate in Kiloude City and its grounds.</summary>
    public const string FriendSafariGate = "KiloudeCity_FriendSafari", FriendSafariMap = "KiloudeCity_FriendSafariGrounds";

    /// <summary>Lays the grounds' grass with the chosen grounds' three species.</summary>
    private void LayFriendSafari()
    {
        var grounds = FriendSafari.Chosen(PlayerIdentity.Number, flow.Story.Var(FriendSafari.ChosenVar));
        flow.Map.WildEncounters.Clear();
        flow.Map.WildEncounters.AddRange(FriendSafari.Table(grounds));
    }
}
