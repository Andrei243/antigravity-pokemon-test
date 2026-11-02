using System;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// A sitting at one of Mauville's machines (plan 21 · H12, part n; style guide, "Mauville's slot machines"): the clock and
/// the buttons of <see cref="ReelMachine"/>, which is Emerald's game frame by frame at sixty a second. Down puts a coin in,
/// the item button three at once, the confirm button starts the reels and stops them one by one (and pays a win out
/// twice as fast while held), the menu button pays it all at once, and cancel asks to leave between games. Its steps take
/// no input of their own (<see cref="Press"/>, <see cref="Advance"/>), so tests and the harness play it.
/// </summary>
public sealed class ReelMachineScreen
{
    private const float Frame = 1f / ReelMachine.FramesPerSecond, AppearTime = 0.3f;

    public bool IsActive { get; private set; }
    public ReelMachine? Machine { get; private set; }
    /// <summary>Which of the hall's twelve machines this is.</summary>
    public int Number { get; private set; }
    public float Appear { get; private set; }
    /// <summary>Seconds the sitting has run, for what on the screen moves by itself.</summary>
    public float Time { get; private set; }

    private float owed;
    private ReelKeys pressed;
    /// <summary>Kept down between frames: a tool (or the confirm button) counting the coins out faster.</summary>
    public bool Held { get; set; }

    public void Open(int machine, int coins, int luck, Random chance)
    {
        Number = machine;
        Machine = new ReelMachine(coins, luck, chance);
        IsActive = true;
        Appear = 0f;
        Time = 0f;
        owed = 0f;
        pressed = ReelKeys.None;
        Held = false;
    }

    /// <summary>A button pressed, for the machine's next frame.</summary>
    public void Press(ReelKeys keys) => pressed |= keys;

    /// <summary>Time passes: the machine runs its frames, the first of them with what was pressed.</summary>
    public void Advance(float dt)
    {
        if (!IsActive || Machine == null) return;
        Appear = Math.Min(1f, Appear + dt / AppearTime);
        Time += dt;
        owed += dt;
        while (owed >= Frame && IsActive)
        {
            owed -= Frame;
            Machine.Update(pressed | (Held ? ReelKeys.Held : ReelKeys.None));
            pressed = ReelKeys.None;
            foreach (var cue in Machine.Cues) Play(cue);
            if (Machine.Finished) IsActive = false;
        }
    }

    /// <summary>Runs whole frames of the machine at once (for tests and the harness).</summary>
    public void Frames(int frames)
    {
        for (int i = 0; i < frames && IsActive; i++) Advance(Frame);
    }

    // The machine's sounds are the game's own for now
    private static void Play(ReelCue cue)
    {
        switch (cue)
        {
            case ReelCue.Coin: AudioManager.PlaySound("coin_land"); break;
            case ReelCue.Spin: AudioManager.PlaySound("gears"); break;
            case ReelCue.Stop: AudioManager.PlaySound("cursor"); break;
            case ReelCue.Win: AudioManager.PlaySound("select"); break;
            case ReelCue.Jackpot: AudioManager.PlaySound("levelup"); break;
            case ReelCue.Lose: break;
            case ReelCue.Bolt: AudioManager.PlaySound("poketch_beep"); break;
            case ReelCue.ReelTimeBegins: AudioManager.PlaySound("exclaim"); break;
            case ReelCue.ReelTimeWon: AudioManager.PlaySound("heal"); break;
            case ReelCue.ReelTimeLost: AudioManager.PlaySound("error"); break;
            case ReelCue.Explosion: AudioManager.PlaySound("menu_close"); break;
        }
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        if (InputManager.IsActionPressed(GameAction.Confirm)) Press(ReelKeys.Confirm);
        if (InputManager.IsActionPressed(GameAction.Down)) Press(ReelKeys.Bet);
        if (InputManager.IsActionPressed(GameAction.Item)) Press(ReelKeys.MaxBet);
        if (InputManager.IsActionPressed(GameAction.Cancel)) Press(ReelKeys.Cancel);
        if (InputManager.IsActionPressed(GameAction.Menu)) Press(ReelKeys.PayAll);
        Held = InputManager.IsActionDown(GameAction.Confirm);
        Advance(dt);
    }

    public void Draw(int sw, int sh)
    {
        if (Machine != null) ModernUi.DrawReelMachine(sw, sh, Machine, Number, Time, Appear);
    }
}
