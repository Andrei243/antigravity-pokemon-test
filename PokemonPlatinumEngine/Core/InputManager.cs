using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Core;

public enum GameAction
{
    Up,
    Down,
    Left,
    Right,
    Confirm,  // A button (Z / Space / Enter)
    Cancel,   // B button (X / Esc / Shift)
    Menu,     // Start button (Enter / Tab)
    Run,      // Hold B / Shift
    Item,     // Y button: the registered key item (C)
    Poketch,  // The Pokétch, the handheld's lower screen: out or away (P)
    PoketchApp, // The Pokétch's side button: the next app (O)
    PoketchTouch // The Pokétch taken in hand to touch its screen, and let go (I)
}

/// <summary>An action's keys and the pad's button for it, with the name the help page gives it.</summary>
public readonly record struct Binding(GameAction Action, string Name, KeyboardKey[] Keys, GamepadButton Pad);

public static class InputManager
{
    /// <summary>Every action's keys and button: what the game reads and what the help page shows (plan 12 · Q10).</summary>
    public static readonly Binding[] Bindings =
    {
        new(GameAction.Up, "Up", new[] { KeyboardKey.Up, KeyboardKey.W }, GamepadButton.LeftFaceUp),
        new(GameAction.Down, "Down", new[] { KeyboardKey.Down, KeyboardKey.S }, GamepadButton.LeftFaceDown),
        new(GameAction.Left, "Left", new[] { KeyboardKey.Left, KeyboardKey.A }, GamepadButton.LeftFaceLeft),
        new(GameAction.Right, "Right", new[] { KeyboardKey.Right, KeyboardKey.D }, GamepadButton.LeftFaceRight),
        new(GameAction.Confirm, "Confirm, talk, read", new[] { KeyboardKey.Z, KeyboardKey.Space }, GamepadButton.RightFaceDown),
        new(GameAction.Cancel, "Back", new[] { KeyboardKey.X, KeyboardKey.Escape }, GamepadButton.RightFaceRight),
        new(GameAction.Menu, "Menu", new[] { KeyboardKey.Enter, KeyboardKey.Tab }, GamepadButton.MiddleRight),
        new(GameAction.Run, "Run (held)", new[] { KeyboardKey.LeftShift, KeyboardKey.X }, GamepadButton.RightFaceRight),
        new(GameAction.Item, "Registered item", new[] { KeyboardKey.C }, GamepadButton.RightFaceLeft),
        new(GameAction.Poketch, "Pokétch out or away", new[] { KeyboardKey.P }, GamepadButton.RightFaceUp),
        new(GameAction.PoketchApp, "Next Pokétch app", new[] { KeyboardKey.O }, GamepadButton.RightTrigger1),
        new(GameAction.PoketchTouch, "Touch the Pokétch", new[] { KeyboardKey.I }, GamepadButton.LeftTrigger1)
    };

    private static Binding Of(GameAction action) => Bindings[(int)action];

    public static bool IsActionPressed(GameAction action)
    {
        // Running is a key held, so it counts as pressed for as long as it is down
        if (action == GameAction.Run) return IsActionDown(action);
        var binding = Of(action);
        foreach (var key in binding.Keys)
            if (Raylib.IsKeyPressed(key)) return true;
        return Raylib.IsGamepadButtonPressed(0, binding.Pad);
    }

    public static bool IsActionDown(GameAction action)
    {
        var binding = Of(action);
        foreach (var key in binding.Keys)
            if (Raylib.IsKeyDown(key)) return true;
        return Raylib.IsGamepadButtonDown(0, binding.Pad);
    }

    /// <summary>A key's name as the help page writes it.</summary>
    public static string KeyName(KeyboardKey key) => key switch
    {
        KeyboardKey.Escape => "Esc",
        KeyboardKey.LeftShift => "Shift",
        KeyboardKey.Up => "Up arrow",
        KeyboardKey.Down => "Down arrow",
        KeyboardKey.Left => "Left arrow",
        KeyboardKey.Right => "Right arrow",
        _ => key.ToString()
    };

    /// <summary>A pad's button as the help page writes it (the face buttons by where they lie, as on any pad).</summary>
    public static string PadName(GamepadButton button) => button switch
    {
        GamepadButton.LeftFaceUp => "D-pad up",
        GamepadButton.LeftFaceDown => "D-pad down",
        GamepadButton.LeftFaceLeft => "D-pad left",
        GamepadButton.LeftFaceRight => "D-pad right",
        GamepadButton.RightFaceDown => "Bottom button",
        GamepadButton.RightFaceRight => "Right button",
        GamepadButton.RightFaceLeft => "Left button",
        GamepadButton.RightFaceUp => "Top button",
        GamepadButton.MiddleRight => "Start",
        GamepadButton.RightTrigger1 => "R",
        GamepadButton.LeftTrigger1 => "L",
        _ => button.ToString()
    };

    /// <summary>-1, 0 or 1 from two opposite actions: pressed this frame, or (<paramref name="held"/>) down now.</summary>
    public static int Axis(GameAction negative, GameAction positive, bool held = false)
    {
        bool Is(GameAction action) => held ? IsActionDown(action) : IsActionPressed(action);
        return (Is(positive) ? 1 : 0) - (Is(negative) ? 1 : 0);
    }

    public static Vector2 GetMovementVector()
    {
        Vector2 v = Vector2.Zero;
        if (IsActionDown(GameAction.Left)) v.X -= 1f;
        if (IsActionDown(GameAction.Right)) v.X += 1f;
        if (IsActionDown(GameAction.Up)) v.Y -= 1f;
        if (IsActionDown(GameAction.Down)) v.Y += 1f;
        return v;
    }
}
