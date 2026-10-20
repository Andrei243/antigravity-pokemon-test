using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The phrase picker over the field (plan 08 · P9; style guide, "Field menus and notices", "Phrases"): a script's
/// <c>phrase</c> opens it on a <see cref="PhraseEntry"/>, and it closes on OK with a whole phrase or on B from the
/// phrase itself. Its logic takes no input (<see cref="Move"/>, <see cref="Confirm"/>, <see cref="Cancel"/>,
/// <see cref="ToDone"/>, <see cref="Advance"/>), so tests and the harness drive it.
/// </summary>
public sealed class PhraseScreen
{
    /// <summary>How long the panel takes to rise into place.</summary>
    public const float AppearTime = 0.2f;

    public bool IsActive { get; private set; }
    public PhraseEntry? Entry { get; private set; }

    /// <summary>The name the phrase is kept under (<c>INTERVIEW</c>).</summary>
    public string Name { get; private set; } = "";

    /// <summary>Seconds since the picker opened.</summary>
    public float Age { get; private set; }

    /// <summary>The phrase chosen once the picker has closed on OK; null while it is open or when it was backed out of.</summary>
    public IReadOnlyList<string>? Outcome => IsActive ? null : Entry?.Result;

    public void Open(string name, PhraseGroups groups, IReadOnlyList<string>? initial)
    {
        Name = name;
        Entry = new PhraseEntry(groups, initial);
        IsActive = true;
        Age = 0f;
    }

    public void Move(int dx, int dy)
    {
        if (!IsActive || Entry == null || (dx == 0 && dy == 0)) return;
        Entry.Move(dx, dy);
        AudioManager.PlaySound("cursor");
    }

    public void Confirm()
    {
        if (!IsActive || Entry == null) return;
        switch (Entry.Press())
        {
            case PhrasePress.Refused:
                AudioManager.PlaySound("error");
                break;
            case PhrasePress.Done:
                AudioManager.PlaySound("select");
                IsActive = false;
                break;
            case PhrasePress.Opened:
            case PhrasePress.Chose:
                AudioManager.PlaySound("select");
                break;
        }
    }

    public void Cancel()
    {
        if (!IsActive || Entry == null) return;
        AudioManager.PlaySound("cancel");
        if (Entry.Back()) IsActive = false;
    }

    /// <summary>The Start button: the cursor to OK.</summary>
    public void ToDone()
    {
        if (!IsActive || Entry == null) return;
        Entry.ToDone();
        AudioManager.PlaySound("cursor");
    }

    public void Advance(float dt)
    {
        if (IsActive) Age += dt;
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        Advance(dt);
        int dx = InputManager.Axis(GameAction.Left, GameAction.Right), dy = InputManager.Axis(GameAction.Up, GameAction.Down);
        if (dx != 0 || dy != 0) Move(dx, dx != 0 ? 0 : dy);
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Menu)) ToDone();
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive || Entry == null) return;
        ModernUi.DrawPhrases(screenWidth, screenHeight, Entry, Math.Clamp(Age / AppearTime, 0f, 1f));
    }
}
