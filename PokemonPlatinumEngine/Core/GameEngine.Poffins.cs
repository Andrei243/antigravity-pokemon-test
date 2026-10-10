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

    /// <summary>The Poffin Case's Poffins (saved).</summary>
    private readonly PoffinCase poffinCase = new();
    private readonly PoffinCookingScreen cookingScreen = new();
    private readonly PoffinCaseScreen poffinCaseScreen = new();

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
        currentState = GameState.BagMenu;
        bagScreen.Registered = registeredItem;
        bagScreen.OpenToPick("berries");
    }

    // PoffinBerrySelection_RunBagApp: the berry chosen goes from the bag into the pot; backing out of the bag ends the
    // cooking
    private void BerryChosenToCook(ItemData? berry)
    {
        choosingBerryToCook = false;
        if (berry?.Berry == null || !playerInventory.RemoveItem(berry, 1))
        {
            currentState = GameState.Overworld;
            return;
        }
        cookingScreen.Begin(berry, PlayerIdentity.Name, fieldRandom, poffinCase, playerInventory);
        currentState = GameState.PoffinCooking;
    }

    private void UpdateCooking(float dt)
    {
        cookingScreen.Update(dt);
        // A Poffin cooked alone adds to the Trainer Card's score (TRAINER_SCORE_EVENT_UNK_12, ov83_0223BCEC)
        if (cookingScreen.TakeCooked()) trainerScore = TrainerScore.Add(trainerScore, TrainerScore.CookedPoffin);
        if (cookingScreen.IsActive) return;
        if (cookingScreen.WantsBerry) StartCooking();
        else currentState = GameState.Overworld;
    }

    /// <summary>Opens the Poffin Case, from the bag (which it goes back to) or from the item button (back to the field).</summary>
    private void OpenPoffinCase(bool fromBag)
    {
        caseFromBag = fromBag;
        currentState = GameState.PoffinCase;
        poffinCaseScreen.Open(poffinCase, playerParty);
    }

    private void UpdatePoffinCase(float dt)
    {
        poffinCaseScreen.Update(dt);
        if (poffinCaseScreen.IsActive) return;
        if (caseFromBag)
        {
            currentState = GameState.BagMenu;
            bagScreen.Resume();
        }
        else currentState = GameState.Overworld;
    }
}
