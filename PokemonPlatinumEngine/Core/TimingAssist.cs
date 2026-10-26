using System;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// How the game helps with a press that must come in time (plan 12 · Q24): not at all, as the original; a window twice
/// as long; or the press made for the player as the window opens.
/// </summary>
public enum TimingAssist { Off, Generous, Automatic }

/// <summary>
/// The one rule every timed press asks (plan 12 · Q24; <c>docs/mechanics/rulings.md</c>, "Timing assists"). It is worked
/// out from the timed thing's own state, with no chance drawn and no clock read: Generous doubles the window,
/// Automatic presses as the window opens. Where a score is set against others (a contest, a race), Automatic counts as
/// Generous, so no prize is won by the assist. GPU-free. Today the rod's bite is the one press that asks it; each
/// later timed press (the Poffins' pot, the dance, the Arcade's board, the Pokéathlon) reads the same setting.
/// </summary>
public static class TimingAssists
{
    /// <summary>The assist as a press scored against others takes it: Automatic is held to Generous.</summary>
    public static TimingAssist Scored(TimingAssist assist) => assist == TimingAssist.Automatic ? TimingAssist.Generous : assist;

    /// <summary>How long a window of <paramref name="seconds"/> stays open under the assist.</summary>
    public static float Window(float seconds, TimingAssist assist) => assist == TimingAssist.Off ? seconds : seconds * 2f;

    /// <summary>
    /// Whether the press counts this frame: the player's own, or the assist's once the window is open under
    /// Automatic.
    /// </summary>
    public static bool Pressed(bool pressed, bool windowOpen, TimingAssist assist) =>
        pressed || (windowOpen && assist == TimingAssist.Automatic);

    /// <summary>
    /// Whether a press before the window opens still spoils the attempt: not under Automatic, where the game makes the
    /// press itself and the player's own early one is passed over.
    /// </summary>
    public static bool EarlyPressCounts(TimingAssist assist) => assist != TimingAssist.Automatic;
}
