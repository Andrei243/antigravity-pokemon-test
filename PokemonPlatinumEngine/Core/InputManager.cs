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
    Run       // Hold B / Shift
}

public static class InputManager
{
    public static bool IsActionPressed(GameAction action)
    {
        return action switch
        {
            GameAction.Up => Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W) || Raylib.IsGamepadButtonPressed(0, GamepadButton.LeftFaceUp),
            GameAction.Down => Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S) || Raylib.IsGamepadButtonPressed(0, GamepadButton.LeftFaceDown),
            GameAction.Left => Raylib.IsKeyPressed(KeyboardKey.Left) || Raylib.IsKeyPressed(KeyboardKey.A) || Raylib.IsGamepadButtonPressed(0, GamepadButton.LeftFaceLeft),
            GameAction.Right => Raylib.IsKeyPressed(KeyboardKey.Right) || Raylib.IsKeyPressed(KeyboardKey.D) || Raylib.IsGamepadButtonPressed(0, GamepadButton.LeftFaceRight),
            GameAction.Confirm => Raylib.IsKeyPressed(KeyboardKey.Z) || Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsGamepadButtonPressed(0, GamepadButton.RightFaceDown),
            GameAction.Cancel => Raylib.IsKeyPressed(KeyboardKey.X) || Raylib.IsKeyPressed(KeyboardKey.Escape) || Raylib.IsGamepadButtonPressed(0, GamepadButton.RightFaceRight),
            GameAction.Menu => Raylib.IsKeyPressed(KeyboardKey.Enter) || Raylib.IsKeyPressed(KeyboardKey.Tab) || Raylib.IsGamepadButtonPressed(0, GamepadButton.MiddleRight),
            GameAction.Run => Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.X) || Raylib.IsGamepadButtonDown(0, GamepadButton.RightFaceRight),
            _ => false
        };
    }

    public static bool IsActionDown(GameAction action)
    {
        return action switch
        {
            GameAction.Up => Raylib.IsKeyDown(KeyboardKey.Up) || Raylib.IsKeyDown(KeyboardKey.W) || Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceUp),
            GameAction.Down => Raylib.IsKeyDown(KeyboardKey.Down) || Raylib.IsKeyDown(KeyboardKey.S) || Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceDown),
            GameAction.Left => Raylib.IsKeyDown(KeyboardKey.Left) || Raylib.IsKeyDown(KeyboardKey.A) || Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceLeft),
            GameAction.Right => Raylib.IsKeyDown(KeyboardKey.Right) || Raylib.IsKeyDown(KeyboardKey.D) || Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceRight),
            GameAction.Confirm => Raylib.IsKeyDown(KeyboardKey.Z) || Raylib.IsKeyDown(KeyboardKey.Space) || Raylib.IsGamepadButtonDown(0, GamepadButton.RightFaceDown),
            GameAction.Cancel => Raylib.IsKeyDown(KeyboardKey.X) || Raylib.IsKeyDown(KeyboardKey.Escape) || Raylib.IsGamepadButtonDown(0, GamepadButton.RightFaceRight),
            GameAction.Menu => Raylib.IsKeyDown(KeyboardKey.Enter) || Raylib.IsKeyDown(KeyboardKey.Tab) || Raylib.IsGamepadButtonDown(0, GamepadButton.MiddleRight),
            GameAction.Run => Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.X) || Raylib.IsGamepadButtonDown(0, GamepadButton.RightFaceRight),
            _ => false
        };
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
