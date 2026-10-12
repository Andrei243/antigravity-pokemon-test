using System;
using System.Collections.Generic;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>An answer to the window's close button.</summary>
public enum CloseChoice { None, KeepPlaying, SaveAndQuit, Quit }

/// <summary>
/// The question the window's close button asks (plan 12 · Q1; style guide, "Field menus and notices", "Leaving the
/// game"): shown over whatever is on screen, its answers those of the start menu's QUIT GAME, with SAVE AND QUIT only
/// where the game can be saved. Its logic takes no input (<see cref="Open"/>, <see cref="Move"/>,
/// <see cref="Confirm"/>, <see cref="Cancel"/>), so tests and the harness answer it.
/// </summary>
public sealed class ClosePrompt
{
    private static readonly CloseChoice[] WithSave = { CloseChoice.KeepPlaying, CloseChoice.SaveAndQuit, CloseChoice.Quit };
    private static readonly CloseChoice[] WithoutSave = { CloseChoice.KeepPlaying, CloseChoice.Quit };

    private readonly UiReveal reveal = new(0.18f, 0.1f);

    /// <summary>True while the question is up.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>Whether SAVE AND QUIT is offered: the game is in the field with nothing under way.</summary>
    public bool CanSave { get; private set; }

    /// <summary>The answer under the cursor; KEEP PLAYING, the first, when it opens.</summary>
    public int Index { get; private set; }

    /// <summary>The answers in button order.</summary>
    public IReadOnlyList<CloseChoice> Answers => CanSave ? WithSave : WithoutSave;

    public bool Visible => reveal.Visible;

    /// <summary>
    /// Whether the close button ends the game at once, with no question: on the title screen (and the options opened
    /// from it) before any game has begun, where nothing can be lost.
    /// </summary>
    public static bool QuitsAtOnce(bool gameBegun, GameState state) =>
        !gameBegun && state is GameState.Title or GameState.Options;

    public static string Label(CloseChoice choice) => choice switch
    {
        CloseChoice.SaveAndQuit => StartMenu.QuitAnswers[1],
        CloseChoice.Quit => StartMenu.QuitAnswers[2],
        _ => StartMenu.QuitAnswers[0]
    };

    /// <summary>What the question says under its heading.</summary>
    public string Body => CanSave
        ? "Anything since your last save will be lost unless you save first."
        : "The game can't be saved right now: anything since your last save will be lost.";

    public void Open(bool canSave)
    {
        CanSave = canSave;
        Index = 0;
        IsOpen = true;
        reveal.Open();
        AudioManager.PlaySound("menu_open");
    }

    /// <summary>Left and right (or up and down) between the answers.</summary>
    public void Move(int step)
    {
        if (!IsOpen || step == 0) return;
        Index = Math.Clamp(Index + step, 0, Answers.Count - 1);
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The A button: the answer under the cursor. The question closes whatever it is.</summary>
    public CloseChoice Confirm()
    {
        if (!IsOpen) return CloseChoice.None;
        var choice = Answers[Index];
        AudioManager.PlaySound(choice == CloseChoice.KeepPlaying ? "cancel" : "select");
        Shut();
        return choice;
    }

    /// <summary>The B button: keeps playing.</summary>
    public CloseChoice Cancel()
    {
        if (!IsOpen) return CloseChoice.None;
        AudioManager.PlaySound("cancel");
        Shut();
        return CloseChoice.KeepPlaying;
    }

    private void Shut()
    {
        IsOpen = false;
        reveal.Close();
    }

    /// <summary>Moves the slide-in and slide-out along; call every frame, open or not.</summary>
    public void Animate(float dt) => reveal.Update(dt);

    /// <summary>The keys, while it is open: the answer given this frame, if any.</summary>
    public CloseChoice Update()
    {
        if (!IsOpen) return CloseChoice.None;
        if (InputManager.IsActionPressed(GameAction.Up) || InputManager.IsActionPressed(GameAction.Left)) Move(-1);
        else if (InputManager.IsActionPressed(GameAction.Down) || InputManager.IsActionPressed(GameAction.Right)) Move(1);
        else if (InputManager.IsActionPressed(GameAction.Cancel)) return Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) return Confirm();
        return CloseChoice.None;
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!reveal.Visible) return;
        var buttons = new (string, Color)[Answers.Count];
        for (int i = 0; i < Answers.Count; i++)
            buttons[i] = (Label(Answers[i]), Answers[i] switch
            {
                CloseChoice.SaveAndQuit => ModernUi.Green,
                CloseChoice.Quit => ModernUi.Red,
                _ => ModernUi.Blue
            });
        ModernUi.Prompt(screenWidth, screenHeight, "Close the game?", Body, buttons, Index, reveal.Shown);
    }
}
