using Raylib_cs;
using PokemonPlatinumEngine.Models.PoketchApps;

namespace PokemonPlatinumEngine.UI;

/// <summary>The Pokétch's Map apps (plan 06 · R14b; style guide, "The Pokétch"): to be drawn.</summary>
internal static partial class ModernUi
{
    private static void PoketchDowsingMachine(Rectangle screen, DowsingMachineApp app, PoketchContext context) => LcdText(screen, "DowsingMachine", PoketchAppState.Columns / 2f, 16);

    private static void PoketchBerrySearcher(Rectangle screen, BerrySearcherApp app, PoketchContext context) => LcdText(screen, "BerrySearcher", PoketchAppState.Columns / 2f, 16);

    private static void PoketchMarkingMap(Rectangle screen, MarkingMapApp app, PoketchContext context) => LcdText(screen, "MarkingMap", PoketchAppState.Columns / 2f, 16);

    private static void PoketchTrainerCounter(Rectangle screen, TrainerCounterApp app, PoketchContext context) => LcdText(screen, "TrainerCounter", PoketchAppState.Columns / 2f, 16);

}
