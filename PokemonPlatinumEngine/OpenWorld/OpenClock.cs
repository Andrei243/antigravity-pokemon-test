using System;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>
/// The open world's own clock (plan 32 · OW1, decision 2): the field's rules advance at a fixed
/// <see cref="TicksPerSecond"/>, however fast the frame, and the renderer draws between the last two ticks
/// (<see cref="Between"/>). A frame's time is counted in whole millionths of a tick (<see cref="Units"/>), each frame's
/// rounded once as it comes in, so no remainder builds up in a float and the same frames give the same ticks on any
/// machine: a recording, a test and the harness see the same positions. A frame longer than
/// <see cref="MostTicksAFrame"/> ticks runs only those and drops the rest, as a hitch, so a frame held up for seconds
/// (a debugger, a window dragged) never runs the field on for seconds at once. GPU-free; no clock of its own but the
/// frames it is handed.
/// </summary>
public sealed class OpenClock
{
    /// <summary>The field's ticks to a second.</summary>
    public const int TicksPerSecond = 120;

    /// <summary>A tick's time in the clock's own units (millionths of a tick).</summary>
    public const long Units = 1_000_000;

    /// <summary>The most ticks one frame runs (a quarter of a second); the rest of a longer frame is dropped.</summary>
    public const int MostTicksAFrame = 30;

    /// <summary>The ticks run since the clock was made.</summary>
    public long Tick { get; private set; }

    /// <summary>What has come in toward the next tick, in <see cref="Units"/> (0 to just under a tick).</summary>
    public long Pending { get; private set; }

    /// <summary>The time dropped by frames too long to run whole, in ticks (what a hitch cost).</summary>
    public long Dropped { get; private set; }

    public OpenClock() { }

    /// <summary>A clock taken up where another stood (a loaded game's, a replay's).</summary>
    public OpenClock(long tick, long pending = 0)
    {
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
        if (pending < 0 || pending >= Units) throw new ArgumentOutOfRangeException(nameof(pending));
        Tick = tick;
        Pending = pending;
    }

    /// <summary>A frame's time in the clock's units: <paramref name="seconds"/> times the ticks a second, in millionths, rounded once.</summary>
    public static long UnitsOf(double seconds) =>
        seconds <= 0 || double.IsNaN(seconds) ? 0 : (long)Math.Round(seconds * TicksPerSecond * Units, MidpointRounding.AwayFromZero);

    /// <summary>
    /// A frame of <paramref name="seconds"/> come in: the ticks it brings, which the caller runs one after another
    /// (each already counted in <see cref="Tick"/> once this returns). Nought or less brings none.
    /// </summary>
    public int Advance(double seconds)
    {
        long total = Pending + UnitsOf(seconds);
        long ticks = total / Units;
        Pending = total % Units;
        if (ticks > MostTicksAFrame)
        {
            Dropped += ticks - MostTicksAFrame;
            ticks = MostTicksAFrame;
        }
        Tick += ticks;
        return (int)ticks;
    }

    /// <summary>How far the next tick has come, from 0 to just under 1: what the renderer draws between the last two by.</summary>
    public float Between => (float)(Pending / (double)Units);

    /// <summary>The field's time in seconds, by its ticks.</summary>
    public double Seconds => Tick / (double)TicksPerSecond;

    /// <summary>A number of seconds as whole ticks, rounded to the nearest (a timer of the field's is set in these).</summary>
    public static long TicksIn(double seconds) => (long)Math.Round(seconds * TicksPerSecond, MidpointRounding.AwayFromZero);
}
