using System;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Battle Arcade's game board on screen (plan 06 · R18d; style guide, "The Battle Arcade"): the clock and the one
/// button of <see cref="ArcadeBoard"/>, which is the original's board frame by frame at thirty a second. The panels
/// count down, the events are shown, the cursor runs, and the confirm button stops it; nothing else is asked, and
/// there is no way out but an event. Its steps take no input of their own (<see cref="PressStop"/>,
/// <see cref="Advance"/>, <see cref="Frames"/>), so tests and the harness play it and stop it by the frame.
/// </summary>
public sealed class ArcadeBoardScreen
{
    /// <summary>A frame of the board: a thirtieth of a second, as the original's.</summary>
    public const float Frame = 1f / 30f;

    private const float AppearTime = 0.3f;

    public bool IsActive { get; private set; }
    public BattleArcadeChallenge? Challenge { get; private set; }
    public ArcadeBoard? Board { get; private set; }

    public float Appear { get; private set; }

    /// <summary>Seconds the board has been up, for what on the screen moves by itself.</summary>
    public float Time { get; private set; }

    private float owed;
    private bool pressed;
    private readonly Kit.CursorTick tick = new();

    /// <summary>The board the challenge has brought up is shown (<c>OpenBattleArcadeApp</c>).</summary>
    public void Open(BattleArcadeChallenge challenge)
    {
        Challenge = challenge;
        Board = challenge.Board;
        IsActive = Board != null;
        Appear = 0f;
        Time = 0f;
        owed = 0f;
        pressed = false;
        AudioManager.PlaySound("select");
    }

    /// <summary>The button, for the board's next frame: it stops a cursor that runs and does nothing before one does.</summary>
    public void PressStop()
    {
        if (IsActive && Board is { Step: ArcadeBoardStep.Running }) pressed = true;
    }

    /// <summary>Time passes: the board plays its frames, the first of them with the button if it was pressed.</summary>
    public void Advance(float dt)
    {
        if (!IsActive || Board == null) return;
        Appear = Math.Min(1f, Appear + dt / AppearTime);
        Time += dt;
        owed += dt;
        while (owed >= Frame && IsActive)
        {
            owed -= Frame;
            Board.Advance(pressed);
            pressed = false;
            Play(Board);
            if (Board.IsDone) IsActive = false;
        }
    }

    /// <summary>Plays whole frames of the board at once (for tests and the harness).</summary>
    public void Frames(int frames)
    {
        for (int i = 0; i < frames && IsActive; i++) Advance(Frame);
    }

    // The board's sounds are the game's own for now (the Battle Arcade's are plan 05's). A cursor at its fastest moves
    // every frame: its tick sounds no oftener than a held key's in a list
    private void Play(ArcadeBoard board)
    {
        tick.Update(Frame);
        if (board.Ticked && tick.Sounds(held: true)) AudioManager.PlaySound("cursor");
        if (!board.Stepped) return;
        switch (board.Step)
        {
            case ArcadeBoardStep.Three or ArcadeBoardStep.Two or ArcadeBoardStep.One:
                AudioManager.PlaySound("poketch_beep");
                break;
            case ArcadeBoardStep.Running:
                AudioManager.PlaySound("exclaim");
                break;
            case ArcadeBoardStep.Stopped:
                AudioManager.PlaySound("select");
                break;
        }
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        if (InputManager.IsActionPressed(GameAction.Confirm)) PressStop();
        Advance(dt);
    }

    public void Draw(int sw, int sh)
    {
        if (Board != null && Challenge != null) ModernUi.DrawArcadeBoard(sw, sh, Challenge, Board, Time, Appear);
    }
}
