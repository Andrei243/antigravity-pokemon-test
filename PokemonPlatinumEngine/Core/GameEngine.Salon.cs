using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The salon and the dye house (plan 43 · LF3): a script's <c>wardrobe salon</c> opens the chair and
/// <c>wardrobe dye "&lt;boutique&gt;"</c> the counter, the money paid as each change is made and a dye's berry taken
/// from the bag; on leaving, the field and the battle draw the player as the outfit now names them.
/// </summary>
public partial class GameEngine
{
    /// <summary>The salon's chair, or the dye house's counter.</summary>
    internal void OpenSalon(bool dye)
    {
        flow.State = GameState.Salon;
        if (dye)
            flow.SalonScreen.OpenDye(flow.Wardrobe, berry => ItemDatabase.Get(berry) is { } item && flow.Bag.GetQuantity(item) > 0);
        else flow.SalonScreen.OpenSalon(flow.Wardrobe);
    }

    private void UpdateSalon(float dt)
    {
        var screen = flow.SalonScreen;
        screen.Update(ref flow.Money, ShowNotification, dt);
        if (screen.TakeBerry() is { } berry && ItemDatabase.Get(berry) is { } item) flow.Bag.RemoveItem(item);
        if (!screen.IsActive)
        {
            PlayerIdentity.SetOutfit(flow.Wardrobe.Worn);
            foreach (string tried in screen.TakeTried()) CharacterModels.Forget(tried);
            flow.State = GameState.Overworld;
        }
    }

    internal sealed partial class Driver
    {
        /// <summary>The salon's chair and the dye house's counter (plan 43 · LF3).</summary>
        public SalonScreen Salon => game.flow.SalonScreen;

        /// <summary>The chair or the counter opened by a tool, as a script's <c>wardrobe salon</c> or <c>wardrobe dye</c> does.</summary>
        public void OpenSalon(bool dye) => game.OpenSalon(dye);
    }
}
