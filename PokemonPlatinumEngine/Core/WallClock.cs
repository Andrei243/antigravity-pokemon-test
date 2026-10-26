using System;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The one reader of the computer's clock (plan 24 · X1; plan 15 · E3): everything that asks the real date or time
/// asks it here, never <see cref="DateTime.Now"/>, so a tool can fix it and its pictures repeat. The hour of the day
/// and the game's date are <see cref="GameClock"/>'s, which reads them through this.
/// <para>
/// A recorded game (plan 16 · T19) reads the clock once a frame, at the frame's start (<see cref="Reading"/>), writes
/// the reading down and holds it for the whole frame (<see cref="Hold"/>); playing the recording back holds the same
/// readings, so whatever the game asks of the time is what it asked when it was played.
/// </para>
/// </summary>
public static class WallClock
{
    /// <summary>
    /// A moment a tool or a test has set, standing for both readings below whatever the machine's time zone; null
    /// follows the computer's clock.
    /// </summary>
    public static DateTime? Fixed { get; set; }

    private static DateTime? heldUtc;
    private static TimeSpan heldOffset;

    /// <summary>The reading held for this frame (UTC), or null while none is: a recorded or replayed game's alone.</summary>
    public static DateTime? Held => heldUtc;

    /// <summary>
    /// Holds a reading for the frame to come: the time in UTC and the time zone's offset the local time is read by.
    /// <see cref="Fixed"/> still wins over it, as the options and the tools set it.
    /// </summary>
    public static void Hold(DateTime utc, TimeSpan offset)
    {
        heldUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        heldOffset = offset;
    }

    /// <summary>Lets go of a held reading: the clock is the computer's again.</summary>
    public static void LetGo() => heldUtc = null;

#pragma warning disable RS0030 // the one place the system clock is read
    /// <summary>The time where the player is, as the DS's clock reads it.</summary>
    public static DateTime Local => Fixed is { } at ? DateTime.SpecifyKind(at, DateTimeKind.Local)
        : heldUtc is { } utc ? DateTime.SpecifyKind(utc + heldOffset, DateTimeKind.Local) : DateTime.Now;

    /// <summary>The time in UTC, for what counts real time passing (the time a save was written).</summary>
    public static DateTime UtcNow => Fixed is { } at ? DateTime.SpecifyKind(at, DateTimeKind.Utc) : heldUtc ?? DateTime.UtcNow;

    /// <summary>The computer's clock read now, held or not: what a recording writes down for a frame (plan 16 · T19).</summary>
    public static DateTime Reading() => DateTime.UtcNow;

    /// <summary>The time zone's offset from UTC now, which a recording keeps in its header.</summary>
    public static TimeSpan Offset() => TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
#pragma warning restore RS0030
}
