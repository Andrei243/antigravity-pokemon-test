using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Models.PoketchApps;

/// <summary>
/// The kitchen timer (<c>kitchen_timer/main.c</c>): minutes and seconds set figure by figure from 00:00 to 99:59
/// with an arrow over and under each figure (the minutes' figures and the seconds' last going 0 to 9 and round,
/// the seconds' first 0 to 5), then START, STOP and RESET along the bottom. Started, it counts down a second at a
/// time; at nought it rings, two bells of our own swinging and sounding every eight frames of the original's thirty
/// a second (<c>Task_BeatSnorlaxHands</c>), until STOP or RESET. The arrows are there only while the time is
/// being set. A button that does nothing now beeps, and a time of nought doesn't start.
///
/// The states are the original's (<c>State_EditTimer</c>, <c>_TimerRunning</c>, <c>_TimerPaused</c>,
/// <c>_TimerSounding</c>, <c>_SoundingPaused</c>), and so is which of START and STOP is shown pressed in each.
/// The original times its count by the console's clock; here by the frames' own time, which goes on while the
/// Pokétch is put away, as the app runs on then.
/// </summary>
public sealed class KitchenTimerApp : PoketchAppState
{
    public const float FrameSeconds = 1f / 30f;

    /// <summary>The buttons, by the original's numbers.</summary>
    public const int Start = 0, Stop = 1, Reset = 2,
        MinutesTensUp = 3, MinutesOnesUp = 4, MinutesTensDown = 5, MinutesOnesDown = 6,
        SecondsTensUp = 7, SecondsOnesUp = 8, SecondsTensDown = 9, SecondsOnesDown = 10;

    /// <summary>The longest time it can be set to, in seconds: 99:59.</summary>
    public const int MostSeconds = 99 * 60 + 59;

    /// <summary>Frames between one swing of the bells and the next.</summary>
    public const int BeatFrames = 8;

    /// <summary>Where each figure stands, in blocks: four seven-segment figures 7 wide, the two pairs 3 apart for the colon.</summary>
    public static readonly int[] FigureX = { 6, 14, 24, 32 };
    public const int FigureY = 10;

    public static readonly PoketchButton StartButton = new(Start, 2, 30, 13, 6);
    public static readonly PoketchButton StopButton = new(Stop, 16, 30, 13, 6);
    public static readonly PoketchButton ResetButton = new(Reset, 30, 30, 13, 6);

    /// <summary>The arrows over and under the figure at a place (0 the minutes' tens to 3 the seconds' ones).</summary>
    public static PoketchButton Arrow(int figure, bool up)
    {
        int id = figure switch
        {
            0 => up ? MinutesTensUp : MinutesTensDown,
            1 => up ? MinutesOnesUp : MinutesOnesDown,
            2 => up ? SecondsTensUp : SecondsTensDown,
            _ => up ? SecondsOnesUp : SecondsOnesDown
        };
        return new PoketchButton(id, FigureX[figure], up ? FigureY - 4 : FigureY + 14, 7, 3);
    }

    private static readonly PoketchButton[] editing =
        new[] { StartButton, StopButton, ResetButton }
            .Concat(Enumerable.Range(0, 4).Select(f => Arrow(f, true)))
            .Concat(Enumerable.Range(0, 4).Select(f => Arrow(f, false)))
            .ToArray();

    private static readonly PoketchButton[] counting = { StartButton, StopButton, ResetButton };

    public enum Mode { Editing, Running, Paused, Sounding, SoundingPaused }

    private readonly int[] figures = new int[4];
    private int setSeconds, elapsedShown, beatFrames;
    private double elapsed;
    private float beatClock;

    public override PoketchApp App => PoketchApp.KitchenTimer;

    public Mode State { get; private set; } = Mode.Editing;

    /// <summary>The figures shown, minutes' tens, minutes' ones, seconds' tens, seconds' ones.</summary>
    public IReadOnlyList<int> Figures => figures;

    /// <summary>The time shown, in seconds.</summary>
    public int SecondsShown => (figures[0] * 10 + figures[1]) * 60 + figures[2] * 10 + figures[3];

    /// <summary>Whether START and STOP are shown pressed in (<c>UpdateButtonPosition</c>): STOP as the app opens.</summary>
    public bool StartDown { get; private set; }
    public bool StopDown { get; private set; } = true;

    /// <summary>The button last touched and the seconds left of it shown pressed in while it is touched.</summary>
    public int Touched { get; private set; } = -1;
    public float TouchedFor { get; private set; }

    /// <summary>Which way the bells lean: they swing from one side to the other at every beat while it rings.</summary>
    public bool BellsLeft { get; private set; }

    public bool Ringing => State == Mode.Sounding;

    public override IReadOnlyList<PoketchButton> Buttons(PoketchContext context) => State == Mode.Editing ? editing : counting;

    public override void Press(int button, PoketchContext context)
    {
        Touched = button;
        TouchedFor = 0.15f;
        switch (State)
        {
            case Mode.Editing: Edit(button, context); break;
            case Mode.Running:
                if (button == Start) context.Sound("poketch_beep");
                else if (button == Stop)
                {
                    context.Sound("poketch");
                    StopDown = true;
                    StartDown = false;
                    State = Mode.Paused;
                }
                else if (button == Reset) ToEditing(context, clear: true);
                break;
            case Mode.Paused:
                if (button == Start)
                {
                    context.Sound("poketch");
                    StopDown = false;
                    StartDown = true;
                    State = Mode.Running;
                }
                else if (button == Stop) context.Sound("poketch_beep");
                else if (button == Reset) ToEditing(context, clear: true);
                break;
            case Mode.Sounding:
                if (button == Start) context.Sound("poketch_beep");
                else if (button == Stop)
                {
                    context.Sound("poketch");
                    StartDown = false;
                    StopDown = true;
                    State = Mode.SoundingPaused;
                }
                else if (button == Reset) ToEditing(context, clear: false);
                break;
            case Mode.SoundingPaused:
                if (button == Start)
                {
                    context.Sound("poketch");
                    StartDown = true;
                    StopDown = false;
                    StartBeating();
                    State = Mode.Sounding;
                }
                else if (button == Stop) context.Sound("poketch_beep");
                else if (button == Reset) ToEditing(context, clear: true);
                break;
        }
    }

    // State_EditTimer
    private void Edit(int button, PoketchContext context)
    {
        switch (button)
        {
            case Reset:
                ClearFigures();
                context.Sound("poketch");
                return;
            case Start:
                context.Sound("poketch");
                // StartTimer: a time of nought starts nothing
                setSeconds = SecondsShown;
                elapsed = 0;
                elapsedShown = 0;
                if (setSeconds == 0) return;
                ShowSeconds(setSeconds);
                StartDown = true;
                StopDown = false;
                State = Mode.Running;
                return;
            case Stop:
                context.Sound("poketch_beep");
                return;
            case MinutesTensUp: figures[0] = (figures[0] + 1) % 10; break;
            case MinutesOnesUp: figures[1] = (figures[1] + 1) % 10; break;
            case MinutesTensDown: figures[0] = (figures[0] + 9) % 10; break;
            case MinutesOnesDown: figures[1] = (figures[1] + 9) % 10; break;
            case SecondsTensUp: figures[2] = (figures[2] + 1) % 6; break;
            case SecondsOnesUp: figures[3] = (figures[3] + 1) % 10; break;
            case SecondsTensDown: figures[2] = (figures[2] + 5) % 6; break;
            case SecondsOnesDown: figures[3] = (figures[3] + 9) % 10; break;
        }
    }

    private void ToEditing(PoketchContext context, bool clear)
    {
        context.Sound("poketch");
        StartDown = false;
        StopDown = true;
        if (clear) ClearFigures();
        State = Mode.Editing;
    }

    // ResetTimer
    private void ClearFigures() => Array.Clear(figures);

    // UpdateDisplayDigits
    private void ShowSeconds(int seconds)
    {
        int minutes = seconds / 60, rest = seconds % 60;
        figures[0] = minutes / 10;
        figures[1] = minutes % 10;
        figures[2] = rest / 10;
        figures[3] = rest % 10;
    }

    private void StartBeating()
    {
        beatFrames = 0;
        beatClock = 0f;
    }

    public override void Update(float dt, PoketchContext context)
    {
        TouchedFor = Math.Max(0f, TouchedFor - dt);
        if (State == Mode.Running)
        {
            // UpdateElapsedTime: whole seconds gone since START, the pauses left out
            elapsed += dt;
            int gone = (int)Math.Floor(elapsed);
            if (gone >= setSeconds)
            {
                ClearFigures();
                StartDown = true;
                StartBeating();
                State = Mode.Sounding;
            }
            else if (gone != elapsedShown)
            {
                ShowSeconds(setSeconds - gone);
                elapsedShown = gone;
            }
        }
        if (State == Mode.Sounding)
        {
            beatClock += dt;
            while (beatClock >= FrameSeconds)
            {
                beatClock -= FrameSeconds;
                if (++beatFrames >= BeatFrames)
                {
                    beatFrames = 0;
                    BellsLeft = !BellsLeft;
                    context.Sound("timer_alarm");
                }
            }
        }
    }
}
