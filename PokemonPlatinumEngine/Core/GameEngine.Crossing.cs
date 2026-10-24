using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The crossing's part of the game (plan 18 · W4, part b): a region's rival named by a script's <c>namerival</c>. The
/// rules of the crossing itself are <see cref="Models.Crossing"/>, which <c>TravelTo</c> plays; the journey on screen is
/// plan 18 · W5's.
/// </summary>
public partial class GameEngine
{
    /// <summary>The keyboard for the rival of the region the player is in, on the name they have now; the script waits on it.</summary>
    internal void OpenRivalNaming()
    {
        var region = Here;
        flow.RivalNamingScreen.Open(region.Id, region.RivalFor(PlayerIdentity.Look) ?? PlayerIdentity.DefaultRivalName,
            PlayerIdentity.HasNamedRival(region.Id) ? PlayerIdentity.RivalOf(region) : null);
        flow.State = GameState.RivalNaming;
    }

    /// <summary>The keyboard closed: the region's rival is called what was given (the region's own for nothing), and the script hears 1.</summary>
    private void CloseRivalNaming()
    {
        var screen = flow.RivalNamingScreen;
        PlayerIdentity.SetRival(screen.Outcome, screen.Fallback, screen.Region);
        PlayerIdentity.InRegion(Here);
        flow.ScriptAnswer = 1;
        flow.State = GameState.Overworld;
    }
}
