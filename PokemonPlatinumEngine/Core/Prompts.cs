using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Raylib_cs;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The names of the controls a prompt shows (plan 12 · Q4): a key on the keyboard, the pad's button once the player's
/// last touch was the pad (<see cref="InputManager.LastDevice"/>), through the string table so every language says
/// them. <see cref="Text.Of"/> asks it for the key names the screens' hints use (<c>shared.keys.z</c> and its kin)
/// and for the placeholders a line may carry (<c>{key:confirm}</c>, upper case <c>{KEY:confirm}</c>), so no screen
/// writes a device's name itself and every prompt follows the player's hand. The keyboard's names are the strings
/// the table has while the key is still among the action's keys, and follow a rebinding after that (plan 12 · Q5). GPU-free.
/// </summary>
public static class Prompts
{
    // The hints' key names, the action each stands for and the key that name is by default
    private static readonly (string Key, GameAction Action, KeyboardKey Default)[] Named =
    {
        ("shared.keys.z", GameAction.Confirm, KeyboardKey.Z),
        ("shared.keys.x", GameAction.Cancel, KeyboardKey.X),
        ("shared.keys.esc", GameAction.Cancel, KeyboardKey.Escape),
        ("shared.keys.enter", GameAction.Menu, KeyboardKey.Enter),
        ("shared.keys.shift", GameAction.Run, KeyboardKey.LeftShift),
        ("capsules.keyC", GameAction.Item, KeyboardKey.C),
        ("shared.keys.c", GameAction.Item, KeyboardKey.C)
    };

    private static readonly Regex Placeholder = new(@"\{(key|KEY):(confirm|cancel|menu|item|run)\}", RegexOptions.Compiled);

    /// <summary>
    /// The name of the control a hint's key stands for when it is not the table's own string (plan 12 · Q4, Q5): on the
    /// pad the button of that action, on the keyboard the key the player has bound in its place; null when the table's
    /// string is right (the keyboard with the key left as it was).
    /// </summary>
    public static string? ForHand(string key)
    {
        bool pad = InputManager.LastDevice == InputDevice.Pad;
        if (key is "shared.keys.arrows" or "shared.keys.leftRight" or "shared.keys.upDown")
            return pad ? Text.Of("padButtons.dpad") : ArrowsOnKeyboard(key);
        foreach (var (named, action, own) in Named)
        {
            if (named != key) continue;
            if (pad) return PadName(action);
            return InputManager.Active[action].Keys.Contains(own) ? null : KeyOf(action);
        }
        return null;
    }

    /// <summary>The sign a hint's cap shows on the pad, or null (the keyboard, or a key that stands for no action).</summary>
    public static PadGlyph? GlyphFor(string key)
    {
        if (InputManager.LastDevice != InputDevice.Pad) return null;
        if (key is "shared.keys.arrows") return PadGlyph.DPad;
        if (key is "shared.keys.leftRight") return PadGlyph.DPadHorizontal;
        if (key is "shared.keys.upDown") return PadGlyph.DPadVertical;
        foreach (var (named, action, _) in Named)
            if (named == key)
            {
                var pad = InputManager.Active[action].Pad;
                return pad.Length > 0 ? PadGlyphs.Of(pad[0]) : null;
            }
        return null;
    }

    private static string? ArrowsOnKeyboard(string key)
    {
        var a = InputManager.Active;
        bool Has(GameAction action, KeyboardKey own) => a[action].Keys.Contains(own);
        string First(GameAction action) => KeyOf(action);
        switch (key)
        {
            case "shared.keys.leftRight":
                return Has(GameAction.Left, KeyboardKey.Left) && Has(GameAction.Right, KeyboardKey.Right) ? null
                    : $"{First(GameAction.Left)} / {First(GameAction.Right)}";
            case "shared.keys.upDown":
                return Has(GameAction.Up, KeyboardKey.Up) && Has(GameAction.Down, KeyboardKey.Down) ? null
                    : $"{First(GameAction.Up)} / {First(GameAction.Down)}";
            default:
                return Has(GameAction.Up, KeyboardKey.Up) && Has(GameAction.Down, KeyboardKey.Down) && Has(GameAction.Left, KeyboardKey.Left) && Has(GameAction.Right, KeyboardKey.Right) ? null
                    : $"{First(GameAction.Up)} {First(GameAction.Left)} {First(GameAction.Down)} {First(GameAction.Right)}";
        }
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
