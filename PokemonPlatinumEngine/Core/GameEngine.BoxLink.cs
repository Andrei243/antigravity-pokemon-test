using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>Boxes anywhere and a sorted bag (plan 12 · Q13): the start menu's BOXES entry and the bag's orders, behind one options row.</summary>
public partial class GameEngine
{
    /// <summary>Whether the options' BOXES ANYWHERE row is on under the rules in force.</summary>
    internal bool BoxLinkOn => FieldHelp.On(Settings.BoxesAnywhere, Ruleset.Current.Preset);

    /// <summary>
    /// Whether the PC can be reached from where the player stands: not in a room whose map says no (<see cref="Map.NoBoxLink"/>),
    /// nor in the middle of a Safari Game or the Bug-Catching Contest, whose rules hold the team as it is.
    /// </summary>
    internal bool CanReachBoxes => !flow.Map.NoBoxLink && !flow.Safari.Active && !InBugContest;

    /// <summary>The start menu's BOXES: the PC's box screen over the field, as a script's PC opens it, or a refusal with the menu left up.</summary>
    private void OpenBoxesFromMenu()
    {
        if (!CanReachBoxes)
        {
            AudioManager.PlaySound("error");
            ShowNotification(Text.Of("start.noBoxLink"));
            return;
        }
        flow.State = GameState.PCStorage;
        flow.PCScreen.Open(flow.Boxes);
        flow.PCScreen.ShowCondition = flow.Story.Has(ContestHallVisitedFlag);
        flow.StartMenu.Hide();
    }
}
