using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models.PoketchApps;

/// <summary>
/// The colour changer (<c>color_changer/main.c</c>): a slider of the screen's eight colours
/// (<see cref="Poketch.ScreenColors"/>: green, yellow, orange, red, purple, blue, teal and white), the colour
/// touched becoming the whole Pokétch's at once and kept in the save (<c>CheckColorChanged</c>,
/// <c>Poketch_SetScreenColor</c>); touching the colour it already is does nothing.
/// </summary>
public sealed class ColorChangerApp : PoketchAppState
{
    /// <summary>The colours' names, in the original's order (<c>PoketchScreenColor</c>).</summary>
    public static readonly string[] Names = { "GREEN", "YELLOW", "ORANGE", "RED", "PURPLE", "BLUE", "TEAL", "WHITE" };

    /// <summary>The slider's eight places in blocks, a swatch 4 wide each, a block apart, in the lower half.</summary>
    public static PoketchButton Swatch(int color) => new(color, 3 + color * 5, 22, 4, 6);

    private static readonly PoketchButton[] buttons = Enumerable.Range(0, Poketch.ScreenColors).Select(Swatch).ToArray();

    public override PoketchApp App => PoketchApp.ColorChanger;

    public override IReadOnlyList<PoketchButton> Buttons(PoketchContext context) => buttons;

    public override void Press(int button, PoketchContext context)
    {
        if (button < 0 || button >= Poketch.ScreenColors || button == context.Poketch.ScreenColor) return;
        context.Poketch.ScreenColor = button;
        // Task_UpdateColor clicks as the colour changes
        context.Sound("poketch");
    }
}
