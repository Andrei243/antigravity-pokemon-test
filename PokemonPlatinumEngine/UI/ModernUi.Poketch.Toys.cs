using Raylib_cs;
using PokemonPlatinumEngine.Models.PoketchApps;

namespace PokemonPlatinumEngine.UI;

/// <summary>The Pokétch's Toys apps (plan 06 · R14b; style guide, "The Pokétch"): to be drawn.</summary>
internal static partial class ModernUi
{
    private static void PoketchCalculator(Rectangle screen, CalculatorApp app, PoketchContext context) => LcdText(screen, "Calculator", PoketchAppState.Columns / 2f, 16);

    private static void PoketchMemoPad(Rectangle screen, MemoPadApp app, PoketchContext context) => LcdText(screen, "MemoPad", PoketchAppState.Columns / 2f, 16);

    private static void PoketchCounter(Rectangle screen, CounterApp app, PoketchContext context) => LcdText(screen, "Counter", PoketchAppState.Columns / 2f, 16);

    private static void PoketchCoinToss(Rectangle screen, CoinTossApp app, PoketchContext context) => LcdText(screen, "CoinToss", PoketchAppState.Columns / 2f, 16);

    private static void PoketchRoulette(Rectangle screen, RouletteApp app, PoketchContext context) => LcdText(screen, "Roulette", PoketchAppState.Columns / 2f, 16);

    private static void PoketchDotArt(Rectangle screen, DotArtApp app, PoketchContext context) => LcdText(screen, "DotArt", PoketchAppState.Columns / 2f, 16);

    private static void PoketchColorChanger(Rectangle screen, ColorChangerApp app, PoketchContext context) => LcdText(screen, "ColorChanger", PoketchAppState.Columns / 2f, 16);

    private static void PoketchKitchenTimer(Rectangle screen, KitchenTimerApp app, PoketchContext context) => LcdText(screen, "KitchenTimer", PoketchAppState.Columns / 2f, 16);

}
