using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.Core;

// Captions and holding to confirm (plan 12 · Q9; style guide, "Field menus and notices", "Captions" and "Holding to confirm").
public partial class GameEngine
{
    /// <summary>The CAPTIONS and HOLD TO CONFIRM rows as the options have them; captions turned off are put away at once.</summary>
    private void UseCaptionsAndHold()
    {
        Accessibility.Captions = Settings.Captions;
        if (!Settings.Captions) Accessibility.CaptionsShown.Clear();
        ConfirmHold.Asked = Settings.HoldToConfirm;
    }

    /// <summary>The captions standing, in their column at the left; nothing with the row off, nor in a picture the screenshot key takes.</summary>
    private void DrawCaptions()
    {
        if (!Accessibility.Captions || screenshotWanted) return;
        var lines = Accessibility.CaptionsShown.Lines;
        if (lines.Count > 0) ModernUi.Captions(lines);
    }
}
