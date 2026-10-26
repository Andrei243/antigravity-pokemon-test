using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The first start (plan 16 · T24): on the game's first run (<see cref="GameSettings.FirstRun"/>: there was no
/// <c>settings.json</c> to read), a page of the options a player wants before reading a line comes before the title's
/// notice. Each change is applied at once, so the language or a colour remap shows on the page; CONTINUE writes the
/// options, so the page never comes again, and the title begins. The harness and the tests make their own options,
/// which are no first run, so they never meet it unless they ask (<see cref="Driver.OpenFirstStart"/>).
/// </summary>
public partial class GameEngine
{
    private bool firstStartOffered;

    /// <summary>Opens the page once, on a first run, before anything of the title is shown.</summary>
    private void OfferTheFirstStart()
    {
        if (firstStartOffered || !Settings.FirstRun) return;
        OpenFirstStart();
    }

    private void OpenFirstStart()
    {
        firstStartOffered = true;
        flow.FirstStartScreen.Open();
        flow.State = GameState.FirstStart;
        Log.Info("The first start: the options' page comes before the title.");
    }

    private void UpdateFirstStart(float dt)
    {
        // A change shows at once (the language, a colour remap, the button preset); nothing on the page needs the window
        if (flow.FirstStartScreen.Update(Settings, dt)) ApplySettings(window: false);
        if (flow.FirstStartScreen.TakeContinue()) FinishFirstStart();
    }

    /// <summary>CONTINUE: the options written where they are kept, so the page never comes again, and on to the title.</summary>
    private void FinishFirstStart()
    {
        flow.FirstStartScreen.Close();
        Settings.Save();
        flow.State = GameState.Title;
    }

    internal sealed partial class Driver
    {
        /// <summary>The first start's page (plan 16 · T24).</summary>
        public FirstStartScreen FirstStart => game.flow.FirstStartScreen;

        /// <summary>Opens the first start's page as a first run opens it, for a tool whose options are no first run.</summary>
        public void OpenFirstStart() => game.OpenFirstStart();

        /// <summary>A change made on the page applied as the game applies it.</summary>
        public void ApplyFirstStartChange() => game.ApplySettings(window: false);

        /// <summary>CONTINUE as the page's own button gives it: the options written and the title begun.</summary>
        public void FinishFirstStart() => game.FinishFirstStart();
    }
}
