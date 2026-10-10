using Raylib_cs;
using PokemonPlatinumEngine.Models.PoketchApps;

namespace PokemonPlatinumEngine.UI;

/// <summary>The Pokétch's Pokemon apps (plan 06 · R14b; style guide, "The Pokétch"): to be drawn.</summary>
internal static partial class ModernUi
{
    private static void PoketchFriendshipChecker(Rectangle screen, FriendshipCheckerApp app, PoketchContext context) => LcdText(screen, "FriendshipChecker", PoketchAppState.Columns / 2f, 16);

    private static void PoketchDayCareChecker(Rectangle screen, DayCareCheckerApp app, PoketchContext context) => LcdText(screen, "DayCareChecker", PoketchAppState.Columns / 2f, 16);

    private static void PoketchPokemonHistory(Rectangle screen, PokemonHistoryApp app, PoketchContext context) => LcdText(screen, "PokemonHistory", PoketchAppState.Columns / 2f, 16);

    private static void PoketchMoveTester(Rectangle screen, MoveTesterApp app, PoketchContext context) => LcdText(screen, "MoveTester", PoketchAppState.Columns / 2f, 16);

    private static void PoketchMatchupChecker(Rectangle screen, MatchupCheckerApp app, PoketchContext context) => LcdText(screen, "MatchupChecker", PoketchAppState.Columns / 2f, 16);

}
