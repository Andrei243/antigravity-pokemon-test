using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// A frame of play handed to the engine (plan 16 · T19): the frame's clock reading held for the whole frame
/// (<see cref="WallClock.Hold"/>), the picture's clock at the movie's own time where asked, what the window did, and
/// the frame's actions fed to this thread's reads for one <see cref="GameEngine.Update"/>. Recording and playing back
/// both go through here, so a frame is given to the game the same way each time.
/// </summary>
public static class Movie
{
    /// <summary>Plays one frame on the engine.</summary>
    /// <param name="offset">The time zone's offset the frame's reading is read by: the recording's.</param>
    /// <param name="frameClock">The movie's time before this frame, for <see cref="FrameClock.Fixed"/>; null leaves the picture's clock alone.</param>
    public static void Play(GameEngine engine, InputFrame frame, TimeSpan offset, double? frameClock)
    {
        WallClock.Hold(frame.Utc, offset);
        if (frameClock is { } at) FrameClock.Fixed = at;
        engine.WindowFocused = (frame.Window & WindowEvents.Unfocused) == 0;
        if ((frame.Window & WindowEvents.CloseAsked) != 0) engine.RequestClose();
        InputManager.Feed(frame.Actions);
        try { engine.Update(frame.Dt); }
        finally { InputManager.Feed(null); }
    }

    /// <summary>Lets go of everything a frame held: the clock, the picture's clock and the fed actions.</summary>
    public static void Release()
    {
        WallClock.LetGo();
        FrameClock.Fixed = null;
        InputManager.Feed(null);
    }
}

/// <summary>
/// Records a session (plan 16 · T19): made before the engine, it draws a seed (or takes one) and hands it to
/// <see cref="Dice.Seed"/>, so the field's chance and every map the start loads draw from it, and writes the header;
/// then each frame goes through <see cref="Step"/>, which plays it and keeps it, with a row of <see cref="StateHash"/>
/// at every second of the movie's time. The live game's chance stays chance: only where it comes from is written down.
/// </summary>
public sealed class MovieRecorder
{
    private double seconds;
    private int nextSecond = 1;
    private readonly bool fixFrameClock;

    public InputRecording Recording { get; }

    private MovieRecorder(InputRecording recording, bool fixFrameClock)
    {
        Recording = recording;
        this.fixFrameClock = fixFrameClock;
    }

    /// <summary>
    /// Begins a recording, before the engine is made: the seed handed to the dice, the options in force, the files the
    /// title will read from <paramref name="saveFolder"/>.
    /// </summary>
    /// <param name="seed">The seed; null draws one by chance (<see cref="Dice.DrawSeed"/>).</param>
    /// <param name="fixFrameClock">Whether the picture's clock is set to the movie's time each frame, as playback sets it: a
    /// headless game's recording does, so that even what keeps time by the picture repeats; the live game keeps its own.</param>
    public static MovieRecorder Begin(GameSettings settings, string saveFolder, string? region, int? seed = null, bool fixFrameClock = false)
    {
        int s = seed ?? Dice.DrawSeed();
        Dice.Seed(s);
        var recording = new InputRecording
        {
            GameVersion = Diagnostics.GameVersion.Describe(),
            DataVersion = GameDataFiles.Version,
            Seed = s,
            Settings = settings.InForce(),
            Offset = WallClock.Offset(),
            Region = region,
            Saves = InputRecording.SavesIn(saveFolder)
        };
        return new MovieRecorder(recording, fixFrameClock);
    }

    /// <summary>Plays a frame on the engine and keeps it, with the hash's row once a second has passed.</summary>
    public void Step(GameEngine engine, InputFrame frame)
    {
        Movie.Play(engine, frame, Recording.Offset, fixFrameClock ? seconds : null);
        Recording.Frames.Add(frame);
        seconds += frame.Dt;
        if (seconds >= nextSecond)
        {
            Recording.Checkpoints.Add(new MovieCheckpoint(Recording.Frames.Count - 1, seconds, StateHash.Of(engine.Drive.Flow)));
            nextSecond = (int)Math.Floor(seconds) + 1;
        }
    }
}

/// <summary>Where a replay parted from its recording (plan 16 · T19): the frame, the second and the groups of <see cref="StateHash"/>.</summary>
public sealed record MovieParting(int Frame, double Seconds, IReadOnlyList<string> Groups)
{
    public override string ToString() => $"The replay parts from the recording at {Seconds:0.##} s (frame {Frame}): {string.Join(", ", Groups)}.";
}

/// <summary>Thrown when a recording can't be played by this build (<see cref="InputRecording.Refusal"/>).</summary>
public sealed class RecordingRefusedException : Exception
{
    public RecordingRefusedException(string why) : base(why) { }
}

/// <summary>
/// Plays a recording back (plan 16 · T19): the title's files laid in a folder of the player's choosing (never over the
/// player's own saves), the seed in the dice, an engine made on the recorded options, and then the frames one at a
/// time with the picture's clock at the movie's own time, each second's hash compared as it goes. It stops at the
/// first second that parts and names the groups (<see cref="Parted"/>).
/// </summary>
public sealed class MoviePlayer
{
    private readonly InputRecording recording;
    private readonly MovieSource source;
    private double seconds;
    private int checkpoint;

    /// <summary>Refuses a recording this build can't play (other data, a newer shape) with its reason.</summary>
    public MoviePlayer(InputRecording recording)
    {
        if (recording.Refusal() is { } why) throw new RecordingRefusedException(why);
        this.recording = recording;
        source = new MovieSource(recording);
    }

    /// <summary>Where the replay parted, or null while it hasn't.</summary>
    public MovieParting? Parted { get; private set; }

    /// <summary>Whether every frame has been played.</summary>
    public bool Done => source.Done;

    /// <summary>The frames played so far.</summary>
    public int Position => source.Position;

    /// <summary>The rows of the hash this replay came to, as the recording keeps its own.</summary>
    public List<MovieCheckpoint> Checkpoints { get; } = new();

    /// <summary>
    /// The game the recording began from: its files laid in <paramref name="saveFolder"/>, which becomes
    /// <see cref="SavePaths.Current"/>, the seed in the dice and an engine on the recorded options, headless unless a
    /// window is asked for (whoever asks initialises it).
    /// </summary>
    public GameEngine Prepare(string saveFolder, bool headless = true)
    {
        recording.LaySaves(saveFolder);
        SavePaths.Current = SavePaths.In(saveFolder, "A recording is being played back here.");
        Dice.Seed(recording.Seed);
        var engine = new GameEngine(GameSettings.FromJson(recording.Settings)) { NewGameRegion = recording.Region };
        if (headless) engine.InitializeHeadless();
        return engine;
    }

    /// <summary>Plays the next frame; false once the recording is over or the replay has parted from it.</summary>
    public bool Step(GameEngine engine)
    {
        if (Parted != null || source.Next(0) is not { } frame) return false;
        Movie.Play(engine, frame, recording.Offset, seconds);
        seconds += frame.Dt;
        int index = source.Position - 1;
        while (checkpoint < recording.Checkpoints.Count && recording.Checkpoints[checkpoint].Frame == index)
        {
            var expected = recording.Checkpoints[checkpoint++];
            var found = StateHash.Of(engine.Drive.Flow);
            Checkpoints.Add(new MovieCheckpoint(index, seconds, found));
            var parted = StateHash.Parted(expected.Hash, found).ToList();
            if (parted.Count > 0)
            {
                Parted = new MovieParting(index, expected.Seconds, parted);
                Log.Warn(Parted.ToString());
                return false;
            }
        }
        return !source.Done;
    }

    /// <summary>Plays every frame that is left, or to the parting; <paramref name="beforeFrame"/> is told each frame's number first.</summary>
    public void PlayToEnd(GameEngine engine, Action<int>? beforeFrame = null)
    {
        while (true)
        {
            beforeFrame?.Invoke(source.Position);
            if (!Step(engine)) break;
        }
    }
}
