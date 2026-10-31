using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>Auto-battle's button and its sign at work (plan 12 · Q12; style guide, "Battle panels", "AUTO").</summary>
internal static partial class ModernUi
{
    /// <summary>Where the AUTO button stands: hung on the message box's top edge at its right end, beside FIGHT.</summary>
    public static readonly Rectangle AutoRect = new(1084 - 176, 828, 176, 52);

    /// <summary>AUTO's own colour, a teal none of the four commands uses.</summary>
    public static readonly Color AutoTeal = new(40, 160, 168, 255);

    /// <summary>The AUTO button: teal and white while it waits to be chosen, Gold with Ink while it runs.</summary>
    public static void AutoButton(bool lit, bool selected)
    {
        string label = Text.Of("battleMenu.auto");
        if (!lit)
        {
            Button(AutoRect, 26, AutoTeal, label, 26, selected);
            return;
        }
        UiShapes.Shadow(AutoRect, 26, 22f, System.Numerics.Vector2.Zero, Gold with { A = 150 });
        UiShapes.Shape(AutoRect, 26, Lighter(Gold, 0.18f), Darker(Gold, 0.1f), Darker(Gold, 0.35f), 3f);
        float w = UiFonts.Measure(label, 26, UiWeight.Black);
        UiFonts.DrawCentered(label, AutoRect.X + (AutoRect.Width - w) / 2f, AutoRect.Y + AutoRect.Height / 2f, 26, Ink, UiWeight.Black);
    }

    /// <summary>AUTO at work: the button lit over whatever the battle shows, and the hint that stops it at the top right.</summary>
    public static void AutoAtWork(int sw)
    {
        AutoButton(lit: true, selected: false);
        Hints(sw - 64, 44, Hint("shared.keys.esc", "battleMenu.autoStop"));
    }
}
