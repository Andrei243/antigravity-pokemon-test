using System;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// A sitting at one of Mauville's roulette tables (plan 21 · H12, part o; style guide, "Mauville's roulette"): the clock and
/// the buttons of <see cref="RouletteWheel"/>, which is Emerald's table frame by frame at sixty a second. The arrows move
/// the cursor over the board, the confirm button places the wager (and hurries the result along, closes a line, answers
/// yes), cancel closes a line and answers no. Its steps take no input of their own (<see cref="Press"/>,
/// <see cref="Advance"/>, <see cref="Frames"/>), so tests and the harness play it.
/// </summary>
public sealed class RouletteScreen
{
    private const float Frame = 1f / RouletteWheel.FramesPerSecond, AppearTime = 0.3f;

    public bool IsActive { get; private set; }
    public RouletteWheel? Wheel { get; private set; }
    public float Appear { get; private set; }
    /// <summary>Seconds the sitting has run, for what on the screen moves by itself.</summary>
    public float Time { get; private set; }

    private float owed;
    private RouletteKeys pressed;

    public void Open(int minBet, int coins, int hour, bool hasShroomish, bool hasTaillow, Random chance)
    {
        Wheel = new RouletteWheel(minBet, coins, hour, hasShroomish, hasTaillow, chance);
        IsActive = true;
        Appear = 0f;
        Time = 0f;
        owed = 0f;
        pressed = RouletteKeys.None;
    }

    /// <summary>A button pressed, for the table's next frame.</summary>
    public void Press(RouletteKeys keys) => pressed |= keys;

    /// <summary>Time passes: the table runs its frames, the first of them with what was pressed.</summary>
    public void Advance(float dt)
    {
        if (!IsActive || Wheel == null) return;
        Appear = Math.Min(1f, Appear + dt / AppearTime);
        Time += dt;
        owed += dt;
        while (owed >= Frame && IsActive)
        {
            owed -= Frame;
            Wheel.Update(pressed);
            pressed = RouletteKeys.None;
            foreach (var cue in Wheel.Cues) Play(cue);
            if (Wheel.Finished) IsActive = false;
        }
    }

    /// <summary>Runs whole frames of the table at once (for tests and the harness).</summary>
    public void Frames(int frames)
    {
        for (int i = 0; i < frames && IsActive; i++) Advance(Frame);
    }

    // The table's sounds are the game's own for now
    private static void Play(RouletteCue cue)
    {
        switch (cue)
        {
            case RouletteCue.Move: AudioManager.PlaySound("cursor"); break;
            case RouletteCue.Refused: AudioManager.PlaySound("error"); break;
            case RouletteCue.Bet: AudioManager.PlaySound("coin_land"); break;
            case RouletteCue.Roll: AudioManager.PlaySound("gears"); break;
            case RouletteCue.Bounce: AudioManager.PlaySound("poketch_beep"); break;
            case RouletteCue.Land: AudioManager.PlaySound("cursor"); break;
            case RouletteCue.Hit: AudioManager.PlaySound("select"); break;
            case RouletteCue.Jackpot: AudioManager.PlaySound("levelup"); break;
            case RouletteCue.Coin: AudioManager.PlaySound("coin_land"); break;
            case RouletteCue.ShroomishCry:
            case RouletteCue.TaillowCry:
            case RouletteCue.Falling:
                AudioManager.PlaySound("exclaim");
                break;
            case RouletteCue.Thud: AudioManager.PlaySound("menu_close"); break;
        }
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        if (InputManager.IsActionPressed(GameAction.Up)) Press(RouletteKeys.Up);
        if (InputManager.IsActionPressed(GameAction.Down)) Press(RouletteKeys.Down);
        if (InputManager.IsActionPressed(GameAction.Left)) Press(RouletteKeys.Left);
        if (InputManager.IsActionPressed(GameAction.Right)) Press(RouletteKeys.Right);
        if (InputManager.IsActionPressed(GameAction.Confirm)) Press(RouletteKeys.Confirm);
        if (InputManager.IsActionPressed(GameAction.Cancel)) Press(RouletteKeys.Cancel);
        Advance(dt);
    }

    public void Draw(int sw, int sh)
    {
        if (Wheel != null) ModernUi.DrawRouletteTable(sw, sh, Wheel, Time, Appear);
    }
}
