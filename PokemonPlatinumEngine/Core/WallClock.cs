using System;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The one reader of the computer's clock (plan 24 · X1; plan 15 · E3): everything that asks the real date or time
/// asks it here, never <see cref="DateTime.Now"/>, so a tool can fix it and its pictures repeat. The hour of the day
/// and the game's date are <see cref="GameClock"/>'s, which reads them through this.
/// </summary>
public static class WallClock
{
    /// <summary>
    /// A moment a tool or a test has set, standing for both readings below whatever the machine's time zone; null
    /// follows the computer's clock.
    /// </summary>
    public static DateTime? Fixed { get; set; }

#pragma warning disable RS0030 // the one place the system clock is read
    /// <summary>The time where the player is, as the DS's clock reads it.</summary>
    public static DateTime Local => Fixed is { } at ? DateTime.SpecifyKind(at, DateTimeKind.Local) : DateTime.Now;

    /// <summary>The time in UTC, for what counts real time passing (the time a save was written).</summary>
    public static DateTime UtcNow => Fixed is { } at ? DateTime.SpecifyKind(at, DateTimeKind.Utc) : DateTime.UtcNow;
#pragma warning restore RS0030
}
