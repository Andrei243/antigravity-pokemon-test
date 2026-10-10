using System.Collections.Generic;

namespace PokemonPlatinumEngine.Models.PoketchApps;

/// <summary>
/// The analog watch (<c>analog_watch/main.c</c>): a dial with the hour and the minute hands, and the whole screen a
/// button that brightens it while it is touched (<c>TouchCallback</c>: bright on the press, dim on the release).
/// With no stylus to hold down, a touch here brightens it for <see cref="BrightSeconds"/>.
/// </summary>
public sealed class AnalogWatchApp : PoketchAppState
{
    public const float BrightSeconds = 0.5f;

    private static readonly PoketchButton[] screen = { new(0, 0, 0, Columns, Rows) };

    public override PoketchApp App => PoketchApp.AnalogWatch;

    /// <summary>Seconds left of the dial lit up by a touch.</summary>
    public float Bright { get; private set; }

    public override IReadOnlyList<PoketchButton> Buttons(PoketchContext context) => screen;

    public override void Press(int button, PoketchContext context) => Bright = BrightSeconds;

    public override void Update(float dt, PoketchContext context) => Bright = System.Math.Max(0f, Bright - dt);

    /// <summary>
    /// Where the hands point, in sixtieths of a turn from twelve o'clock: the minute hand at the minute, the hour
    /// hand at five to the hour and a fifth more for each twelve minutes, as the original's sprite steps through
    /// its 60 frames once a minute.
    /// </summary>
    public static (int Hour, int Minute) Hands(System.DateTime now) => (now.Hour % 12 * 5 + now.Minute / 12, now.Minute);
}
