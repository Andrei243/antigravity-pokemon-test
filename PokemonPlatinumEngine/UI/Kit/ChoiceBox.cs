using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.UI.Kit;

/// <summary>
/// The answers to a question asked in the field: "Yes" and "No", or the entries of a short menu (plan 02 · S1).
/// It is the cursor and its rules only, with no drawing and no keys of its own, so tests answer questions with
/// <see cref="Move"/>, <see cref="Confirm"/> and <see cref="Back"/>; <c>ModernUi.DrawChoices</c> draws it.
/// </summary>
public sealed class ChoiceBox
{
    private readonly UiReveal reveal = new(0.16f, 0.1f);

    public IReadOnlyList<string> Options { get; private set; } = Array.Empty<string>();
    public int Cursor { get; private set; }

    /// <summary>The answer the cancel button gives; -1 for a question that has to be answered.</summary>
    public int Cancel { get; private set; } = -1;

    /// <summary>True while it waits for an answer.</summary>
    public bool IsOpen => reveal.IsOpen;

    /// <summary>True while any of it is on screen: it slides away after the answer.</summary>
    public bool Visible => reveal.Visible;
    public float Shown => reveal.Shown;

    /// <summary>The answer given, until someone takes it.</summary>
    public int? Picked { get; private set; }

    public void Open(IReadOnlyList<string> options, int cancel = -1)
    {
        Options = options;
        Cancel = cancel >= 0 && cancel < options.Count ? cancel : -1;
        Cursor = 0;
        Picked = null;
        reveal.Open();
    }

    /// <summary>Up or down the answers; past the last comes the first.</summary>
    public void Move(int step)
    {
        if (!IsOpen || step == 0 || Options.Count < 2) return;
        Cursor = UiNav.Wrap(Cursor, step, Options.Count);
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The A button: the answer under the cursor.</summary>
    public void Confirm()
    {
        if (!IsOpen) return;
        Picked = Cursor;
        reveal.Close();
        AudioManager.PlaySound("select");
    }

    /// <summary>The B button: the answer that means "no", where the question has one.</summary>
    public void Back()
    {
        if (!IsOpen || Cancel < 0) return;
        Cursor = Cancel;
        Picked = Cancel;
        reveal.Close();
        AudioManager.PlaySound("cancel");
    }

    /// <summary>The answer given since the last call, once.</summary>
    public int? Take()
    {
        var picked = Picked;
        Picked = null;
        return picked;
    }

    /// <summary>Puts it away with no answer given.</summary>
    public void Dismiss()
    {
        Picked = null;
        reveal.Snap(false);
    }

    public void Update(float dt) => reveal.Update(dt);

    /// <summary>One frame of the keys.</summary>
    public void ReadKeys()
    {
        if (!IsOpen) return;
        if (InputManager.IsActionPressed(GameAction.Up)) Move(-1);
        else if (InputManager.IsActionPressed(GameAction.Down)) Move(1);
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Back();
    }
}

/// <summary>
/// A script's own fade: the field goes to black and stays there until it is told to come back, with the text box
/// still over it.
/// </summary>
public sealed class ScreenFade
{
    private float target, rate;

    /// <summary>0 for the field as it is, 1 for black.</summary>
    public float Level { get; private set; }

    public bool IsMoving => Level != target;

    public void To(bool black, float seconds)
    {
        target = black ? 1f : 0f;
        if (seconds <= 0f) Level = target;
        else rate = 1f / seconds;
    }

    public void Update(float dt) => Level = UiMotion.Toward(Level, target, rate * dt);

    public void Clear() => Level = target = 0f;
}
