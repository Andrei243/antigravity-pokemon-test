using System;
using System.Numerics;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI.Kit;
using Raylib_cs;

namespace PokemonPlatinumEngine.UI;

internal static partial class ModernUi
{
    /// <summary>
    /// What is drawn over the sky while soaring (plan 38 · EX7; style guide, "Soaring", the interface): the place under the
    /// flier named in a pill at the top, the height as five pips at the right, and the hints at the foot. Nothing else.
    /// </summary>
    public static void DrawSoaring(int sw, int sh, SoarScreen screen)
    {
        var flight = screen.Flight!;
        string? named = screen.Under is { } spot ? screen.NameOf(spot) : screen.PlaceUnder is { } place ? SoarScreen.NameOf(place) : null;
        if (named is { } name)
        {
            float w = UiFonts.Measure(name, 40, UiWeight.Black) + 96;
            var pill = new Rectangle(sw / 2f - w / 2f, 48, w, 72);
            UiShapes.Shadow(pill, 36, 14, new Vector2(0, 6), new Color(0, 0, 0, 70));
            UiShapes.Fill(pill, 36, new Color(255, 255, 255, 240));
            UiShapes.Circle(new Vector2(pill.X + 38, pill.Y + 36), 11, Gold);
            UiFonts.DrawCentered(name, pill.X + 60, pill.Y + 36, 40, Ink, UiWeight.Black);
        }

        // The height: five pips at the right, the highest at the top, those up to the flier's height lit
        float px = sw - Margin - 18, top = sh / 2f - 120;
        for (int i = 0; i < 5; i++)
        {
            int level = 5 - i;
            var c = new Vector2(px, top + i * 60);
            bool lit = flight.Height >= level - 0.5f;
            UiShapes.Circle(c, 17, new Color(0, 0, 0, 60));
            UiShapes.Circle(c, 14, lit ? new Color(255, 255, 255, 240) : new Color(255, 255, 255, 70));
        }

        var land = Hint("shared.keys.z", "soar.hint.land");
        Hints(sw - Margin, sh - 84, Hint("shared.keys.leftRight", "soar.hint.turn"), Hint("shared.keys.upDown", "soar.hint.climb"),
            Hint("shared.keys.shift", "soar.hint.faster"), land, Hint("shared.keys.esc", "shared.hints.back"));
    }
}
