using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Raylib_cs;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The names of the controls a prompt shows (plan 12 · Q4): a key on the keyboard, the pad's button once the player's
/// last touch was the pad (<see cref="InputManager.LastDevice"/>), through the string table so every language says
/// them. <see cref="Text.Of"/> asks it for the key names the screens' hints use (<c>shared.keys.z</c> and its kin)
/// and for the placeholders a line may carry (<c>{key:confirm}</c>, upper case <c>{KEY:confirm}</c>), so no screen
/// writes a device's name itself and every prompt follows the player's hand. The keyboard's names stay the strings
/// the table has; plan 12 · Q5 makes them follow a rebinding. GPU-free.
/// </summary>
public static class Prompts
{
    // The hints' key names, and the action each stands for
    private static readonly (string Key, GameAction Action)[] Named =
    {
        ("shared.keys.z", GameAction.Confirm),
        ("shared.keys.x", GameAction.Cancel),
        ("shared.keys.esc", GameAction.Cancel),
        ("shared.keys.enter", GameAction.Menu),
        ("shared.keys.shift", GameAction.Run),
        ("capsules.keyC", GameAction.Item)
    };

    private static readonly Regex Placeholder = new(@"\{(key|KEY):(confirm|cancel|menu|item|run)\}", RegexOptions.Compiled);

    /// <summary>
    /// The pad's word for a hint's key, if the player is on the pad and the key stands for an action (or is the arrows);
    /// null when the keyboard's own name should be shown.
    /// </summary>
    public static string? ForPad(string key)
    {
        if (InputManager.LastDevice != InputDevice.Pad) return null;
        if (key == "shared.keys.arrows") return Button(GamepadButton.LeftFaceUp, dpad: true);
        foreach (var (named, action) in Named)
            if (named == key) return PadName(action);
        return null;
    }

    /// <summary>A line's <c>{key:confirm}</c> and its kin put in: the keyboard's key, or the pad's button once the player is on the pad.</summary>
    public static string Fill(string text)
    {
        if (text.IndexOf('{') < 0) return text;
        return Placeholder.Replace(text, m =>
        {
            var action = m.Groups[2].Value switch
            {
                "confirm" => GameAction.Confirm,
                "cancel" => GameAction.Cancel,
                "menu" => GameAction.Menu,
                "item" => GameAction.Item,
                _ => GameAction.Run
            };
            string name = NameOf(action);
            return m.Groups[1].Value == "KEY" ? name.ToUpperInvariant() : name;
        });
    }

    /// <summary>The name of the control that does an action, on the device the player touched last.</summary>
    public static string NameOf(GameAction action) =>
        InputManager.LastDevice == InputDevice.Pad && PadName(action) is { } pad ? pad : KeyOf(action);

    /// <summary>The keyboard's name for an action: its first key.</summary>
    public static string KeyOf(GameAction action)
    {
        var keys = InputManager.Active[action].Keys;
        return keys.Length > 0 ? InputManager.KeyName(keys[0]) : Text.Of("controls.unbound");
    }

    /// <summary>The pad's name for an action: its first button, or null when it has none.</summary>
    public static string? PadName(GameAction action)
    {
        var pad = InputManager.Active[action].Pad;
        return pad.Length > 0 ? Button(pad[0], dpad: false) : null;
    }

    /// <summary>A button's short name by where it lies, lettered as an Xbox pad is (the face buttons A, B, X and Y).</summary>
    public static string Button(GamepadButton button, bool dpad)
    {
        if (dpad) return Text.Of("padButtons.dpad");
        return button switch
        {
            GamepadButton.LeftFaceUp => Text.Of("padButtons.dpadUp"),
            GamepadButton.LeftFaceDown => Text.Of("padButtons.dpadDown"),
            GamepadButton.LeftFaceLeft => Text.Of("padButtons.dpadLeft"),
            GamepadButton.LeftFaceRight => Text.Of("padButtons.dpadRight"),
            _ => Face(button)
        };
    }

    private static string Face(GamepadButton button) => button switch
    {
        GamepadButton.RightFaceDown => Text.Of("padButtons.bottom"),
        GamepadButton.RightFaceRight => Text.Of("padButtons.right"),
        GamepadButton.RightFaceLeft => Text.Of("padButtons.left"),
        GamepadButton.RightFaceUp => Text.Of("padButtons.top"),
        GamepadButton.MiddleRight => Text.Of("padButtons.start"),
        GamepadButton.MiddleLeft => Text.Of("padButtons.select"),
        GamepadButton.LeftTrigger1 => Text.Of("padButtons.l1"),
        GamepadButton.RightTrigger1 => Text.Of("padButtons.r1"),
        GamepadButton.LeftTrigger2 => Text.Of("padButtons.l2"),
        GamepadButton.RightTrigger2 => Text.Of("padButtons.r2"),
        _ => button.ToString()
    };

}
