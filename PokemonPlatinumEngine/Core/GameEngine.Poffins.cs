using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Poffins in the game (plan 06 · R14c): the Poffin House's pot, berry after berry (<see cref="PoffinCookingScreen"/>,
/// the bag choosing each berry as the original's berry selection does), and the Poffin Case from the bag or the item
/// button (<see cref="PoffinCaseScreen"/>). The rules are <see cref="PoffinPot"/>'s and <see cref="Poffins"/>'.
/// </summary>
public partial class GameEngine
{
    /// <summary>The flag that shows a Pokémon's condition in its summary (the original's <c>SystemFlag_CheckContestHallVisited</c>).</summary>
    public const string ContestHallVisitedFlag = "FLAG_CONTEST_HALL_VISITED";

    // The bag is open to choose a berry for the pot; the case goes back to the bag it was opened from
    private bool choosingBerryToCook, caseFromBag;

    /// <summary>
    /// The cooking begins (<c>poffin cook</c>, the original's <c>OpenPoffinCooking</c>): the bag on its berries, to choose
    /// one; the script waits until the player stops cooking.
    /// </summary>
    private void StartCooking()
    {
        choosingBerryToCook = true;
        scriptItem = null;
        flow.State = GameState.BagMenu;
        flow.BagScreen.Registered = flow.RegisteredItem;
        flow.BagScreen.OpenToPick("berries");
    }

    // PoffinBerrySelection_RunBagApp: the berry chosen goes from the bag into the pot; backing out of the bag ends the
    // cooking
    private void BerryChosenToCook(ItemData? berry)
    {
        choosingBerryToCook = false;
        if (berry?.Berry == null || !flow.Bag.RemoveItem(berry, 1))
        {
            flow.State = GameState.Overworld;
            return;
        }
        flow.PoffinCookingScreen.Begin(berry, PlayerIdentity.Name, fieldRandom, flow.PoffinCase, flow.Bag);
        flow.State = GameState.PoffinCooking;
    }

    private void UpdateCooking(float dt)
    {
        flow.PoffinCookingScreen.Update(dt);
        // A Poffin cooked alone adds to the Trainer Card's score (TRAINER_SCORE_EVENT_UNK_12, ov83_0223BCEC)
        if (flow.PoffinCookingScreen.TakeCooked()) flow.TrainerScore = TrainerScore.Add(flow.TrainerScore, TrainerScore.CookedPoffin);
        if (flow.PoffinCookingScreen.IsActive) return;
        if (flow.PoffinCookingScreen.WantsBerry) StartCooking();
        else flow.State = GameState.Overworld;
    }

    /// <summary>Opens the Poffin Case, from the bag (which it goes back to) or from the item button (back to the field).</summary>
    private void OpenPoffinCase(bool fromBag)
    {
        caseFromBag = fromBag;
        flow.State = GameState.PoffinCase;
        flow.PoffinCaseScreen.Open(flow.PoffinCase, flow.Party);
    }

    private void UpdatePoffinCase(float dt)
    {
        flow.PoffinCaseScreen.Update(dt);
        if (flow.PoffinCaseScreen.IsActive) return;
        if (caseFromBag)
        {
            flow.State = GameState.BagMenu;
            flow.BagScreen.Resume();
        }
        else flow.State = GameState.Overworld;
    }
}
