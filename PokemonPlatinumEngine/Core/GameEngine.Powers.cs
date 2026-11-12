using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The powers in hand (plan 36 · TR7, TR8; <c>docs/mechanics/rulings.md</c>, "Powers"): the Power Watch, a key item a
/// training hall's teacher gives with the first powers taught, opens <see cref="PowersScreen"/> from the bag or the item
/// button (<see cref="GameState.Powers"/>); its charge fills as the player walks (<c>OnStep</c>); the powers in force stand
/// as chips over the field and are told on the toast as they wear off (<c>GameEngine.Cooking.cs</c>). The rules are
/// GPU-free in <c>Models/Powers.cs</c>.
/// </summary>
public partial class GameEngine
{
    /// <summary>The watch's name, as the data has it.</summary>
    public const string PowerWatchItem = "Power Watch";

    /// <summary>The watch's field use (<c>items.json</c>'s <c>fieldUse</c>).</summary>
    internal const string PowerWatchUse = "PowerWatch";

    private bool powersFromBag;

    /// <summary>Whether the bag holds the Power Watch.</summary>
    internal bool HoldsPowerWatch => ItemDatabase.Get(PowerWatchItem) is { } watch && flow.Bag.GetQuantity(watch) > 0;

    /// <summary>Opens the watch's panel, from the bag or the item button.</summary>
    private void OpenPowers(bool fromBag)
    {
        powersFromBag = fromBag;
        flow.State = GameState.Powers;
        flow.PowersScreen.Open(flow.Powers, flow.Encounters.DailyNumber, flow.Run.NoPowers);
    }

    private void UpdatePowers(float dt)
    {
        flow.PowersScreen.Update(dt);
        if (flow.PowersScreen.IsActive) return;
        if (powersFromPhone)
        {
            powersFromPhone = false;
            OpenPhone(PhoneCard.Powers);
        }
        else if (powersFromBag)
        {
            flow.State = GameState.BagMenu;
            flow.BagScreen.Resume();
        }
        else flow.State = GameState.Overworld;
    }

    /// <summary>The chips of the powers in force over the field, while it is the player's: no script, menu or text over it.</summary>
    private void DrawPowerChips(GameState shown)
    {
        if (shown != GameState.Overworld || ScriptRunning || flow.StartMenu.IsActive || screenshotWanted) return;
        var inForce = flow.Powers.InForce;
        if (inForce.Count > 0) ModernUi.PowerChips(inForce);
    }
}
