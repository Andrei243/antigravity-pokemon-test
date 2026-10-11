using System;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// A sitting at one of the Game Corner's machines (plan 06 · R16b; style guide, "The slot machine"): the clock and
/// the buttons of <see cref="SlotMachine"/>, which is the original's game frame by frame at thirty a second. The
/// confirm button bets and pulls the lever (and pays a win out at once, or twice as fast while held), Left, Down and
/// Right stop the three reels, and the cancel or menu button leaves between games. Its steps take no input of their
/// own (<see cref="Press"/>, <see cref="Advance"/>), so tests and the harness play it.
/// </summary>
public sealed class SlotMachineScreen
{
    private const float Frame = 1f / SlotMachine.FramesPerSecond, AppearTime = 0.3f;

    public bool IsActive { get; private set; }
    public SlotMachine? Machine { get; private set; }
    /// <summary>Which of the hall's twelve machines this is.</summary>
    public int Number { get; private set; }
    public float Appear { get; private set; }
    /// <summary>Seconds the sitting has run, for what on the screen moves by itself.</summary>
    public float Time { get; private set; }

    private float owed;
    private SlotKeys pressed;
    /// <summary>Kept down between frames: a tool (or the confirm button) counting the coins out faster.</summary>
    public bool Held { get; set; }

    public void Open(int machine, int coins, int mood, Random chance)
    {
        Number = machine;
        Machine = new SlotMachine(coins, mood, chance);
        IsActive = true;
        Appear = 0f;
        Time = 0f;
        owed = 0f;
        pressed = SlotKeys.None;
        Held = false;
    }

    /// <summary>A button pressed, for the machine's next frame.</summary>
    public void Press(SlotKeys keys) => pressed |= keys;

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
            Machine.Update(pressed | (Held ? SlotKeys.Held : SlotKeys.None));
            pressed = SlotKeys.None;
            foreach (var cue in Machine.Cues) Play(cue);
            if (Machine.Finished) IsActive = false;
        }
    }

    /// <summary>Runs whole frames of the machine at once (for tests and the harness).</summary>
    public void Frames(int frames)
    {
        for (int i = 0; i < frames && IsActive; i++) Advance(Frame);
    }

    // The machine's sounds are the game's own for now (the Game Corner's are plan 05's)
    private static void Play(SlotCue cue)
    {
        switch (cue)
        {
            case SlotCue.Bet: AudioManager.PlaySound("select"); break;
            case SlotCue.Start: AudioManager.PlaySound("gears"); break;
            case SlotCue.Stop: AudioManager.PlaySound("cursor"); break;
            case SlotCue.StopBonus: AudioManager.PlaySound("select"); break;
            case SlotCue.Coin: AudioManager.PlaySound("coin_land"); break;
            case SlotCue.Tell: AudioManager.PlaySound("poketch_beep"); break;
            case SlotCue.BonusBegins: AudioManager.PlaySound("exclaim"); break;
            case SlotCue.Jackpot: AudioManager.PlaySound("levelup"); break;
            case SlotCue.MoonRed: AudioManager.PlaySound("error"); break;
            case SlotCue.MoonBright: AudioManager.PlaySound("heal"); break;
            case SlotCue.BonusEnds: AudioManager.PlaySound("menu_close"); break;
            case SlotCue.RoundBegins:
            case SlotCue.Cry:
                if (Data.PokemonDatabase.Get("Clefairy") is { } clefairy) AudioManager.PlayCry(clefairy);
                break;
        }
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        if (InputManager.IsActionPressed(GameAction.Confirm)) Press(SlotKeys.Bet | SlotKeys.Confirm);
        if (InputManager.IsActionPressed(GameAction.Left)) Press(SlotKeys.Stop1);
        if (InputManager.IsActionPressed(GameAction.Down)) Press(SlotKeys.Stop2);
        if (InputManager.IsActionPressed(GameAction.Right)) Press(SlotKeys.Stop3);
        if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Menu)) Press(SlotKeys.Quit | SlotKeys.Confirm);
        Held = InputManager.IsActionDown(GameAction.Confirm);
        Advance(dt);
    }

    public void Draw(int sw, int sh)
    {
        if (Machine != null) ModernUi.DrawSlotMachine(sw, sh, Machine, Number, Time, Appear);
    }
}
