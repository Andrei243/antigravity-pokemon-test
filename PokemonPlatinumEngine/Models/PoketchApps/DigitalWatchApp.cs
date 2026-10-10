using System.Collections.Generic;

namespace PokemonPlatinumEngine.Models.PoketchApps;

/// <summary>
/// The digital watch (<c>digital_watch/main.c</c>): the hour and the minute, and the whole screen a button that
/// turns the backlight on and off (<c>ToggleBacklight</c>).
/// </summary>
public sealed class DigitalWatchApp : PoketchAppState
{
    private static readonly PoketchButton[] screen = { new(0, 0, 0, Columns, Rows) };

    public override PoketchApp App => PoketchApp.DigitalWatch;

    /// <summary>Whether the backlight is on: the screen drawn lighter.</summary>
    public bool Backlight { get; private set; }

    public override IReadOnlyList<PoketchButton> Buttons(PoketchContext context) => screen;

    public override void Press(int button, PoketchContext context)
    {
        Backlight = !Backlight;
        context.Sound("poketch");
    }
}
