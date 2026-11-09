using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The camp in the game (plan 10 · F6; docs/mechanics/rulings.md, "The camp"): the start menu's CAMP, offered while the
/// options' FOLLOWER row is on and there is a Pokémon (the team out of their balls is the follower's world, and Platinum's
/// menu stays as it was with the row off), opens <see cref="CampScreen"/>; its item button opens the bag on its berries
/// and the berry chosen is fed to the Pokémon under the cursor, the camp coming back either way.
/// </summary>
public partial class GameEngine
{
    // The bag is open to choose a berry for a Pokémon of the camp
    private bool choosingBerryForCamp;

    /// <summary>
    /// Whether the start menu offers CAMP now: a Pokémon on the team, and the options' FOLLOWER row on or the crafting kit in
    /// the bag (plan 39 · CR6, whose pot is the camp's: so the start menu is as it was until Sinnoh's Hall of Fame).
    /// </summary>
    internal bool HasCamp => (Settings.Follower || HasCraftingKit) && flow.Party.Count > 0;

    /// <summary>Opens the camp with the team: the visit's cure, if a battle came between, is the screen's first line.</summary>
    internal void OpenCamp()
    {
        if (flow.Party.Count == 0) return;
        flow.CampScreen.Open(flow.Party, flow.CampCare, CampKitchenNow());
        flow.State = GameState.Camp;
        // The team's menu sprites, which the clearing shows
        foreach (var member in flow.Party.Members)
            if (!member.IsEgg) PokemonSprites.Request(member.LookName);
    }

    private void UpdateCamp(float dt)
    {
        flow.CampScreen.Update(dt);
        switch (flow.CampScreen.TakeRequest())
        {
            case CampRequest.Feed:
                choosingBerryForCamp = true;
                flow.State = GameState.BagMenu;
                flow.BagScreen.Registered = flow.RegisteredItem;
                flow.BagScreen.OpenToPick("berries");
                return;
            case CampRequest.Close:
                flow.State = GameState.Overworld;
                return;
        }
        if (!flow.CampScreen.IsActive) flow.State = GameState.Overworld;
    }

    // The bag put away: the berry chosen is eaten by whoever stands under the camp's cursor, and the camp is back
    private void BerryChosenForCamp(ItemData? berry)
    {
        choosingBerryForCamp = false;
        if (berry != null) flow.CampScreen.Feed(berry, flow.Bag);
        else flow.CampScreen.NoBerryChosen();
        flow.State = GameState.Camp;
    }
}
