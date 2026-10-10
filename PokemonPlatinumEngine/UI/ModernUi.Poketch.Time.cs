using Raylib_cs;
using PokemonPlatinumEngine.Models.PoketchApps;

namespace PokemonPlatinumEngine.UI;

/// <summary>The Pokétch's Time apps (plan 06 · R14b; style guide, "The Pokétch"): to be drawn.</summary>
internal static partial class ModernUi
{
    private static void PoketchAnalogWatch(Rectangle screen, AnalogWatchApp app, PoketchContext context) => LcdText(screen, "AnalogWatch", PoketchAppState.Columns / 2f, 16);

    private static void PoketchCalendar(Rectangle screen, CalendarApp app, PoketchContext context) => LcdText(screen, "Calendar", PoketchAppState.Columns / 2f, 16);

    private static void PoketchLinkSearcher(Rectangle screen, LinkSearcherApp app, PoketchContext context) => LcdText(screen, "LinkSearcher", PoketchAppState.Columns / 2f, 16);

}
