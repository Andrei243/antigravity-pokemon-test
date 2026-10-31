using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

internal static partial class ModernUi
{
    /// <summary>The run timer's corner (plan 13 · V10): its size, and its top where it stands alone and where it stands under a counting panel.</summary>
    public const float TimerCornerWidth = 340, TimerCornerHeight = 144, TimerCornerTop = 32, TimerCornerLow = 32 + 96 + 16;

    /// <summary>
    /// The run timer's corner (style guide, "Field menus and notices", "The run timer"): a panel at the top right, 32
    /// from the edges, with RUN TIME over the real time in Black 36, the game's own time under it, and the last split the
    /// category counts with the real time it was taken at. A finished run says FINAL TIME in Gold and holds its time.
    /// </summary>
    public static void RunTimerCorner(int sw, float top, string real, string game, (string Name, string Time)? last, bool finished)
    {
        var r = new Rectangle(sw - 32 - TimerCornerWidth, top, TimerCornerWidth, TimerCornerHeight);
        Panel(r, 24);
        float x = r.X + 24, right = r.X + r.Width - 24;
        if (finished) UiFonts.Draw(Text.Of("timer.final"), x, r.Y + 16, 20, Gold, UiWeight.Black);
        else Label(Text.Of("timer.run"), x, r.Y + 16);
        UiFonts.Draw(real, x, r.Y + 36, 36, finished ? Gold : Ink, UiWeight.Black);
        UiFonts.Draw(Text.Of("timer.game", ("time", game)), x, r.Y + 84, 20, Muted, UiWeight.Bold);
        if (last is not { } split) return;
        // The split's name at the left and its time at the right; a long name is set smaller until both fit
        float timeW = UiFonts.Measure(split.Time, 20, UiWeight.Black);
        UiFonts.Draw(split.Time, right - timeW, r.Y + 110, 20, Ink, UiWeight.Black);
        float room = right - timeW - 12 - x;
        float size = 20;
        while (size > 14 && UiFonts.Measure(split.Name, size, UiWeight.Bold) > room) size -= 1;
        UiFonts.Draw(split.Name, x, r.Y + 110 + (20 - size) / 2f, size, Ink, UiWeight.Bold);
    }
}
