using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Models.PoketchApps;

/// <summary>
/// The coin toss (<c>coin_toss/main.c</c>, its flight <c>coin_toss/graphics.c</c>): the coin touched is thrown
/// up, comes down bouncing lower each time and lands heads or tails, one or the other as likely
/// (<c>MTRNG_Next() % 2</c> in <c>State_UpdateApp</c>, drawn as it is thrown). It starts heads up.
///
/// The flight is the original's, frame by frame at its thirty frames a second (<c>Task_TossCoin</c>): thrown up at
/// 10.5 pixels a frame, falling back at 0.6875 more each frame, and at every landing bouncing up again at 56
/// hundredths of the speed it came down at, until a landing slower than 2 pixels a frame leaves it lying. The
/// numbers are the original's fixed point (a pixel is 4096). A touch while it flies does nothing.
/// </summary>
public sealed class CoinTossApp : PoketchAppState
{
    public const float FrameSeconds = 1f / 30f;
    private const int One = 4096;
    private const int ThrowSpeed = -43008, Gravity = 2816, StopSpeed = -2 * One;

    /// <summary>The coin, in blocks: a disc 11 across lying in the lower middle of the screen.</summary>
    public static readonly PoketchButton Coin = new(0, 17, 23, 11, 11);

    private static readonly PoketchButton[] buttons = { Coin };

    private int y, speed;
    private float clock;

    public override PoketchApp App => PoketchApp.CoinToss;

    /// <summary>The face last thrown (the face shown once the coin lies still).</summary>
    public bool Heads { get; private set; } = true;

    /// <summary>Whether the coin is in the air (or bouncing): it shows its edge spinning.</summary>
    public bool Flying { get; private set; }

    /// <summary>How high the coin is above where it lies, in the original's pixels.</summary>
    public float Height => -y / (float)One;

    /// <summary>Frames since it was thrown, for its spin.</summary>
    public int Frames { get; private set; }

    public override IReadOnlyList<PoketchButton> Buttons(PoketchContext context) => buttons;

    public override void Press(int button, PoketchContext context)
    {
        if (Flying) return;
        Heads = context.Rng.Next(2) == 1;
        // Task_TossCoin, state 0
        context.Sound("coin_flip");
        Flying = true;
        y = 0;
        speed = ThrowSpeed;
        Frames = 0;
        clock = 0f;
    }

    public override void Update(float dt, PoketchContext context)
    {
        if (!Flying) return;
        clock += dt;
        while (Flying && clock >= FrameSeconds)
        {
            clock -= FrameSeconds;
            Step(context);
        }
    }

    // Task_TossCoin, state 1
    private void Step(PoketchContext context)
    {
        Frames++;
        y += speed;
        speed += Gravity;
        if (speed > 0 && y >= 0)
        {
            speed = -(speed * 56 / 100);
            context.Sound("coin_land");
            y = 0;
            if (speed >= StopSpeed) Flying = false;
        }
    }
}
