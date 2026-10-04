using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The seconds that the picture's small motions keep time by, where they are no part of the game's rules: the
/// sway of the grass, the hop of a menu icon, a blinking cursor. It is the time since the window opened, unless a
/// tool has set it (<see cref="Fixed"/>): the screenshot harness counts its own frames, so that a shot comes out
/// the same every run.
/// </summary>
public static class FrameClock
{
    /// <summary>The time a tool has set; null follows the real clock.</summary>
    public static double? Fixed { get; set; }

    public static double Now => Fixed ?? Raylib.GetTime();
}
