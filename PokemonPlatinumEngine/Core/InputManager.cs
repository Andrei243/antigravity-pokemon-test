using System;
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
public readonly record struct Binding(GameAction Action, KeyboardKey[] Keys, GamepadButton Pad)
{
    /// <summary>The action's name on the help page, in the language of the moment.</summary>
    public string Name => Text.Of($"controls.action.{Action}");
}

public static class InputManager
{
    /// <summary>Every action's keys and button: what the game reads and what the help page shows (plan 12 · Q10).</summary>
    public static readonly Binding[] Bindings =
    {
        new(GameAction.Up, new[] { KeyboardKey.Up, KeyboardKey.W }, GamepadButton.LeftFaceUp),
        new(GameAction.Down, new[] { KeyboardKey.Down, KeyboardKey.S }, GamepadButton.LeftFaceDown),
        new(GameAction.Left, new[] { KeyboardKey.Left, KeyboardKey.A }, GamepadButton.LeftFaceLeft),
        new(GameAction.Right, new[] { KeyboardKey.Right, KeyboardKey.D }, GamepadButton.LeftFaceRight),
        new(GameAction.Confirm, new[] { KeyboardKey.Z, KeyboardKey.Space }, GamepadButton.RightFaceDown),
        new(GameAction.Cancel, new[] { KeyboardKey.X, KeyboardKey.Escape }, GamepadButton.RightFaceRight),
        new(GameAction.Menu, new[] { KeyboardKey.Enter, KeyboardKey.Tab }, GamepadButton.MiddleRight),
        new(GameAction.Run, new[] { KeyboardKey.LeftShift, KeyboardKey.X }, GamepadButton.RightFaceRight),
        new(GameAction.Item, new[] { KeyboardKey.C }, GamepadButton.RightFaceLeft),
        new(GameAction.Poketch, new[] { KeyboardKey.P }, GamepadButton.RightFaceUp),
        new(GameAction.PoketchApp, new[] { KeyboardKey.O }, GamepadButton.RightTrigger1),
        new(GameAction.PoketchTouch, new[] { KeyboardKey.I }, GamepadButton.LeftTrigger1)
    };

    private static Binding Of(GameAction action) => Bindings[(int)action];

    /// <summary>
    /// The actions a tool, a test or a headless game hands this thread in place of the keys (plan 16 · T11c): while
    /// one is set, every read below answers from it and no key or button is looked at. Null in the game itself. Kept
    /// per thread, so tests that run side by side don't feed each other.
    /// </summary>
    [ThreadStatic] private static ActionFrame? fed;

    /// <summary>Hands this thread's reads a frame of actions in place of the keys; null gives the keys back.</summary>
    public static void Feed(ActionFrame? frame) => fed = frame;

    /// <summary>The frame this thread is fed, if any.</summary>
    public static ActionFrame? Fed => fed;

    /// <summary>
    /// The keys and the pad as a frame (plan 16 · T11f): every action down now, and every action whose key or button
    /// went down this frame, as raylib reports them. The engine reads it once a frame and feeds it to its update.
    /// </summary>
    public static ActionFrame ReadKeys()
    {
        uint down = 0, pressed = 0;
        foreach (var binding in Bindings)
        {
            uint bit = 1u << (int)binding.Action;
            bool isDown = Raylib.IsGamepadButtonDown(0, binding.Pad), isPressed = Raylib.IsGamepadButtonPressed(0, binding.Pad);
            foreach (var key in binding.Keys)
            {
                isDown |= Raylib.IsKeyDown(key);
                isPressed |= Raylib.IsKeyPressed(key);
            }
            if (isDown) down |= bit;
            if (isPressed) pressed |= bit;
        }
        return new ActionFrame(down, pressed);
    }

    public static bool IsActionPressed(GameAction action)
    {
        // Running is a key held, so it counts as pressed for as long as it is down
        if (action == GameAction.Run) return IsActionDown(action);
        if (fed is { } frame) return frame.IsPressed(action);
        var binding = Of(action);
        foreach (var key in binding.Keys)
            if (Raylib.IsKeyPressed(key)) return true;
        return Raylib.IsGamepadButtonPressed(0, binding.Pad);
    }

    public static bool IsActionDown(GameAction action)
    {
        if (fed is { } frame) return frame.IsDown(action);
        var binding = Of(action);
        foreach (var key in binding.Keys)
            if (Raylib.IsKeyDown(key)) return true;
        return Raylib.IsGamepadButtonDown(0, binding.Pad);
    }

    /// <summary>A key's name as the help page writes it.</summary>
    public static string KeyName(KeyboardKey key) => key switch
    {
        KeyboardKey.Escape => Text.Of("controls.key.esc"),
        KeyboardKey.LeftShift => Text.Of("controls.key.shift"),
        KeyboardKey.Up => Text.Of("controls.key.up"),
        KeyboardKey.Down => Text.Of("controls.key.down"),
        KeyboardKey.Left => Text.Of("controls.key.left"),
        KeyboardKey.Right => Text.Of("controls.key.right"),
        _ => key.ToString()
    };

    /// <summary>A pad's button as the help page writes it (the face buttons by where they lie, as on any pad).</summary>
    public static string PadName(GamepadButton button) => button switch
    {
        GamepadButton.LeftFaceUp => Text.Of("controls.pad.up"),
        GamepadButton.LeftFaceDown => Text.Of("controls.pad.down"),
        GamepadButton.LeftFaceLeft => Text.Of("controls.pad.left"),
        GamepadButton.LeftFaceRight => Text.Of("controls.pad.right"),
        GamepadButton.RightFaceDown => Text.Of("controls.pad.bottom"),
        GamepadButton.RightFaceRight => Text.Of("controls.pad.rightButton"),
        GamepadButton.RightFaceLeft => Text.Of("controls.pad.leftButton"),
        GamepadButton.RightFaceUp => Text.Of("controls.pad.top"),
        GamepadButton.MiddleRight => Text.Of("controls.pad.start"),
        GamepadButton.RightTrigger1 => Text.Of("controls.pad.r"),
        GamepadButton.LeftTrigger1 => Text.Of("controls.pad.l"),
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
