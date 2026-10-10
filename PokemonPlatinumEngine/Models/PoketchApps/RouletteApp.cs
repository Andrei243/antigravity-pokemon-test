using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Models.PoketchApps;

/// <summary>
/// The roulette (<c>roulette/main.c</c>, its arrow <c>roulette/graphics.c</c>): an arrow in the middle of a page
/// the player draws on (the wheel's sections and what each stands for), and three buttons down the right, START,
/// STOP and CLEAR. START sets the arrow spinning, STOP slows it to a halt wherever it comes to, CLEAR wipes the
/// page; a button that does nothing now beeps (<c>State_UpdateApp</c>). The page can be drawn on only while the
/// arrow is still, with a pen and no eraser, and starts blank every time the app comes up.
///
/// The arrow turns as the original's does, frame by frame at thirty a second, in its angle's units of 65536 to the
/// turn (<c>Task_RunSpinner</c>): 336 faster each frame up to 12288; at STOP held to 6656 at most, on for a frame
/// or two more by the one roll the original makes (<c>MTRNG_Next() % 8</c>, whose only effect, as it is written,
/// is one more frame on a roll of nought), then 80 slower each frame until it stops. Where it stops is otherwise
/// the player's timing.
/// </summary>
public sealed class RouletteApp : PoketchAppState
{
    public const float FrameSeconds = 1f / 30f;
    public const int Start = 0, Stop = 1, Clear = 2;
    public const int Width = PoketchCanvas.Width, Height = PoketchCanvas.Height;

    public static readonly PoketchButton StartButton = new(Start, 38, 4, 6, 8);
    public static readonly PoketchButton StopButton = new(Stop, 38, 15, 6, 8);
    public static readonly PoketchButton ClearButton = new(Clear, 38, 26, 6, 8);

    private static readonly IReadOnlyList<PoketchButton> buttons = PoketchCanvas.WithTools(StartButton, StopButton, ClearButton);

    private enum Spin { Still, Starting, Speeding, Full, Delay, Slowing }

    private readonly bool[,] dots = new bool[Width, Height];
    private Spin spin = Spin.Still;
    private bool stopAsked;
    private int speed;
    private uint delay;
    private float clock;

    public override PoketchApp App => PoketchApp.Roulette;

    /// <summary>The arrow's angle in the original's units, 65536 to the turn, clockwise from straight up.</summary>
    public ushort Angle { get; private set; }

    /// <summary>The arrow's speed in the same units a frame.</summary>
    public int Speed => speed;

    /// <summary>Whether the arrow is turning (or coming to a stop).</summary>
    public bool Spinning => spin != Spin.Still;

    /// <summary>
    /// Which buttons are shown pressed in, as the original's (<c>playButtonPressed</c> and the rest): STOP while the
    /// arrow is still, START and CLEAR while it turns, all three while it comes to a stop.
    /// </summary>
    public bool StartDown => spin != Spin.Still;
    public bool StopDown => spin == Spin.Still || stopAsked;
    public bool ClearDown => spin != Spin.Still || clearFor > 0f;

    private float clearFor;

    /// <summary>Whether a block of the page is drawn on.</summary>
    public bool Dot(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height && dots[x, y];

    public override IReadOnlyList<PoketchButton> Buttons(PoketchContext context) => buttons;

    public override void Press(int button, PoketchContext context)
    {
        if (spin == Spin.Still)
        {
            switch (button)
            {
                case Start:
                    context.Sound("poketch");
                    spin = Spin.Starting;
                    stopAsked = false;
                    clock = 0f;
                    return;
                case Stop:
                    context.Sound("poketch_beep");
                    return;
                case Clear:
                    context.Sound("poketch");
                    Array.Clear(dots);
                    clearFor = 0.15f;
                    return;
            }
            // GetTouchStartPosition: the pen only ever adds to the page
            if (PoketchCanvas.CellOf(button) is { } cell) dots[cell.X, cell.Y] = true;
            return;
        }
        if (stopAsked) return;
        switch (button)
        {
            case Start:
            case Clear:
                context.Sound("poketch_beep");
                break;
            case Stop:
                context.Sound("poketch");
                stopAsked = true;
                break;
        }
    }

    public override void Update(float dt, PoketchContext context)
    {
        clearFor = Math.Max(0f, clearFor - dt);
        if (spin == Spin.Still) return;
        clock += dt;
        while (spin != Spin.Still && clock >= FrameSeconds)
        {
            clock -= FrameSeconds;
            Step(context);
        }
    }

    // Task_RunSpinner, a frame
    private void Step(PoketchContext context)
    {
        switch (spin)
        {
            case Spin.Starting:
                Angle = (ushort)(Angle + 336);
                speed = 336;
                spin = Spin.Speeding;
                break;
            case Spin.Speeding:
                Angle = (ushort)(Angle + speed);
                speed += 336;
                if (speed >= 12288)
                {
                    speed = 12288;
                    spin = Spin.Full;
                }
                break;
            case Spin.Full:
                Angle = (ushort)(Angle + speed);
                if (stopAsked)
                {
                    delay = (uint)context.Rng.Next(8);
                    if (speed > 6656) speed = 6656;
                    spin = Spin.Delay;
                }
                break;
            case Spin.Delay:
                Angle = (ushort)(Angle + speed);
                // As the original has it: a roll of nought is counted down past nought, so it waits one frame more
                if (delay == 0) delay--;
                else spin = Spin.Slowing;
                break;
            case Spin.Slowing:
                if (speed > 80)
                {
                    speed -= 80;
                    Angle = (ushort)(Angle + speed);
                }
                else
                {
                    speed = 0;
                    // Task_StopSpinner: the arrow at rest
                    context.Sound("roulette_spin");
                    spin = Spin.Still;
                    stopAsked = false;
                }
                break;
        }
    }
}
