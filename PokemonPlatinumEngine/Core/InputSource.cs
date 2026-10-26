using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Core;

/// <summary>What happened to the window in a frame that changes the game (plan 16 · T19), kept with the frame's actions.</summary>
[Flags]
public enum WindowEvents : byte
{
    None = 0,
    /// <summary>The window's close button was pressed: the game asks its question (<see cref="GameEngine.RequestClose"/>).</summary>
    CloseAsked = 1,
    /// <summary>The window was not the one in front (the options' "Pause in the background" holds the game then).</summary>
    Unfocused = 2
}

/// <summary>
/// One frame of play as a recording keeps it (plan 16 · T19): the seconds <c>Program.Main</c> handed the engine
/// (after its tenth of a second's cap), the computer's clock read at the frame's start (UTC ticks), the actions held
/// and pressed (<see cref="ActionFrame"/>, two bitmasks) and what happened to the window. Twenty-one bytes.
/// </summary>
public readonly record struct InputFrame(float Dt, long UtcTicks, ActionFrame Actions, WindowEvents Window = WindowEvents.None)
{
    public DateTime Utc => new(UtcTicks, DateTimeKind.Utc);
}

/// <summary>
/// Where a frame of play comes from (plan 16 · T19): the keys and the clock (<see cref="LiveInput"/>), a recording
/// played back (<see cref="MovieSource"/>), or a test's list (<see cref="ListInput"/>). A frame it gives is handed to
/// the engine by <see cref="Movie.Play"/>, which holds its clock reading (<see cref="WallClock.Hold"/>) and feeds its
/// actions to this thread's reads (<see cref="InputManager.Feed"/>, kept per thread, so tests side by side feed their
/// own).
/// </summary>
public interface IInputSource
{
    /// <summary>The next frame, given the seconds the loop measured (which a recording ignores); null once there are none.</summary>
    InputFrame? Next(float dt);
}

/// <summary>The keys, the pad and the computer's clock, read now: the game's own source.</summary>
public sealed class LiveInput : IInputSource
{
    /// <summary>What the window did this frame, which the loop sets before asking for the frame.</summary>
    public WindowEvents Window { get; set; }

    public InputFrame? Next(float dt) => new InputFrame(dt, WallClock.Reading().Ticks, InputManager.ReadKeys(), Window);
}

/// <summary>A recording's frames, one after another, as they were played (plan 16 · T19).</summary>
public sealed class MovieSource : IInputSource
{
    private readonly IReadOnlyList<InputFrame> frames;

    public MovieSource(InputRecording recording) => frames = recording.Frames;

    /// <summary>How many frames have been given.</summary>
    public int Position { get; private set; }

    public bool Done => Position >= frames.Count;

    public InputFrame? Next(float dt) => Position < frames.Count ? frames[Position++] : null;
}

/// <summary>
/// A test's source: actions from a function of the frame's number, each frame <see cref="Dt"/> long, the clock
/// moving on from a start by the frames' time.
/// </summary>
public sealed class ListInput : IInputSource
{
    private readonly Func<int, ActionFrame?> actions;
    private readonly DateTime start;
    private int frame;
    private double seconds;

    public ListInput(Func<int, ActionFrame?> actions, DateTime start, float dt = 1f / 60f)
    {
        this.actions = actions;
        this.start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        Dt = dt;
    }

    public float Dt { get; }

    public InputFrame? Next(float dt)
    {
        if (actions(frame) is not { } a) return null;
        var at = start.AddSeconds(seconds);
        frame++;
        seconds += Dt;
        return new InputFrame(Dt, at.Ticks, a);
    }
}
