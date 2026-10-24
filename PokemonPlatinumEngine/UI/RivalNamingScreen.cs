using System;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// Naming a region's rival over the field (plan 18 · W4, part b): a script's <c>namerival</c> opens the introduction's
/// keyboard (<see cref="NameEntry"/>, drawn by <see cref="ModernUi.DrawNameEntry"/>) for the rival of the region the
/// script plays in, on the name that region's rival has now. OK closes it with the name given, or the region's own
/// when it was left empty. Its logic takes no input (<see cref="Move"/>, <see cref="Confirm"/>, <see cref="Cancel"/>,
/// <see cref="ToDone"/>, <see cref="Advance"/>), so tests and the harness drive it.
/// </summary>
public sealed class RivalNamingScreen
{
    /// <summary>How long the keyboard takes to slide into place.</summary>
    public const float AppearTime = 0.3f;

    public bool IsActive { get; private set; }
    public NameEntry? Entry { get; private set; }

    /// <summary>The region whose rival is named, by id.</summary>
    public string Region { get; private set; } = "";

    /// <summary>The name taken when the keyboard is left empty: the region's own rival's.</summary>
    public string Fallback { get; private set; } = "";

    /// <summary>Seconds since the keyboard opened.</summary>
    public float Age { get; private set; }

    /// <summary>The name given once the keyboard has closed; null while it is open.</summary>
    public string? Outcome => IsActive || Entry == null ? null : Entry.Result(Fallback);

    /// <summary>Opens the keyboard for a region's rival, on the name they have now (empty for none yet).</summary>
    public void Open(string region, string fallback, string? current)
    {
        Region = region;
        Fallback = fallback;
        Entry = new NameEntry(current);
        IsActive = true;
        Age = 0f;
    }

    public void Move(int dx, int dy)
    {
        if (!IsActive || Entry == null || (dx == 0 && dy == 0)) return;
        Entry.Move(dx, dy);
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The A button: the key under the cursor; OK closes the keyboard.</summary>
    public void Confirm()
    {
        if (!IsActive || Entry == null) return;
        var key = Entry.Press();
        AudioManager.PlaySound(key == NameKey.Delete ? "cancel" : "select");
        if (Entry.Done) IsActive = false;
    }

    /// <summary>The B button: a letter deleted. The keyboard is closed only by OK, since a rival must be called something.</summary>
    public void Cancel()
    {
        if (!IsActive || Entry == null) return;
        if (Entry.Backspace()) AudioManager.PlaySound("cancel");
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
        ModernUi.DrawNameEntry(screenWidth, screenHeight, Entry, null, PlayerIdentity.Look,
            UiMotion.EaseOut(Math.Clamp(Age / AppearTime, 0f, 1f)), Text.Of("crossing.rivalName"), Fallback);
    }
}
