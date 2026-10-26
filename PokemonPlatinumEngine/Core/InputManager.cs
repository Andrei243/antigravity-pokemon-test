using System;
using System.Linq;
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
    PoketchTouch, // The Pokétch taken in hand to touch its screen, and let go (I)
    PageLeft,     // The left shoulder (Q): a page back, for the bag's letter jump, the boxes and the Pokédex's pages (plan 12 · Q4)
    PageRight,    // The right shoulder (E): a page on
    Fullscreen,   // F11 (Alt+Enter too)
    Mute,         // M
    Screenshot,   // F12 (plan 12 · Q16 takes the picture)
    Report        // F8: a report of the session, to be replayed (plan 16 · T20)
}

/// <summary>An action's keys and the pad's buttons for it, with the name the help page gives it.</summary>
public readonly record struct Binding(GameAction Action, KeyboardKey[] Keys, GamepadButton[] Pad)
{
    /// <summary>The action's name on the help page, in the language of the moment.</summary>
    public string Name => Text.Of($"controls.action.{Action}");
}

/// <summary>A key or a pad button read while the Controls page waits for the player's new binding (plan 12 · Q5).</summary>
public readonly record struct CapturedInput(KeyboardKey? Key, GamepadButton? Button)
{
    public string Name => Key is { } k ? InputManager.KeyName(k) : Button is { } b ? InputManager.PadName(b) : "";
}

public static class InputManager
{
    private static InputBindings active = InputBindings.For(ButtonMode.Normal);
    private static float deadZone = DefaultDeadZone;
    private static int padIndex;
    private static volatile int lastDevice;
    private static readonly StickReader stick = new();

    /// <summary>The left stick's dead zone when nothing was chosen (plan 12 · Q4).</summary>
    public const float DefaultDeadZone = 0.35f;

    /// <summary>The binding set in force: what the game reads, what the help page shows.</summary>
    public static InputBindings Active => active;

    /// <summary>Every action's keys and buttons in force, in the order of <see cref="GameAction"/>.</summary>
    public static Binding[] Bindings => active.All.Select(a => new Binding(a.Action, a.Inputs.Keys, a.Inputs.Pad)).ToArray();

    /// <summary>The stick's dead zone in force, 0.05 to 0.95.</summary>
    public static float DeadZone => deadZone;

    /// <summary>The pad read, 0 to 3 (raylib's own numbering).</summary>
    public static int PadIndex => padIndex;

    /// <summary>
    /// Which device the player touched last (plan 12 · Q4): what a prompt names the controls of, and the pad that
    /// plan 12 · Q24's pulses go to. Set from the keys and the pad as they are read each frame, never by a fed frame.
    /// </summary>
    public static InputDevice LastDevice
    {
        get => (InputDevice)lastDevice;
        set => lastDevice = (int)value;
    }

    /// <summary>Puts a player's options in force: the Button Mode's preset with their own overrides, the stick's dead zone and the pad.</summary>
    public static void Use(GameSettings settings)
    {
        active = InputBindings.From(settings);
        deadZone = Math.Clamp(settings.StickDeadZone / 100f, 0.05f, 0.95f);
        int index = Math.Clamp(settings.GamepadIndex, 0, 3);
        if (index != padIndex) stick.Reset();
        padIndex = index;
    }

    /// <summary>Puts a binding set in force (a test's, or the Controls page's while a binding is tried).</summary>
    public static void Use(InputBindings bindings) => active = bindings;

    /// <summary>Back to the defaults: today's keys, the stick's usual dead zone, the first pad, the keyboard.</summary>
    public static void Reset()
    {
        active = InputBindings.For(ButtonMode.Normal);
        deadZone = DefaultDeadZone;
        padIndex = 0;
        lastDevice = (int)InputDevice.Keyboard;
        CaptureRequested = false;
        Captured = null;
        stick.Reset();
    }

    /// <summary>Actions bound and kept in the settings that nothing in the game reads yet: none since the screenshot key (plan 12 · Q16).</summary>
    public static readonly IReadOnlySet<GameAction> NotReadYet = new HashSet<GameAction>();

    /// <summary>Set by the Controls page while it waits for a new binding: <see cref="ReadKeys"/> then reports the first key or button pressed in <see cref="Captured"/>.</summary>
    public static volatile bool CaptureRequested;

    /// <summary>The key or pad button pressed this frame while a capture was asked for (null otherwise); fed by <see cref="ReadKeys"/> or, in a test, by <see cref="Inject"/>.</summary>
    public static CapturedInput? Captured { get; private set; }

    /// <summary>Sets what this frame captured (a test's or a tool's stand-in for the keyboard).</summary>
    public static void Inject(CapturedInput? input) => Captured = input;

    private static CapturedInput? ReadCapture(PadSnapshot pad)
    {
        // The queue of keys pressed is emptied every frame, so a key pressed before a capture was asked for never answers it
        int code = 0, first = 0;
        while ((code = Raylib.GetKeyPressed()) != 0) if (first == 0) first = code;
        if (!CaptureRequested) return null;
        if (first != 0 && Enum.IsDefined((KeyboardKey)first)) return new CapturedInput((KeyboardKey)first, null);
        if (pad.Connected)
            foreach (var button in Enum.GetValues<GamepadButton>())
                if (button != GamepadButton.Unknown && pad.IsPressed(button)) return new CapturedInput(null, button);
        return null;
    }

    private static Binding Of(GameAction action) => new(action, active[action].Keys, active[action].Pad);

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
        var pad = ReadPad();
        var held = stick.Update(pad.StickX, pad.StickY, deadZone, out var stickPressed);
        // Alt+Enter is full screen too, and not the menu's Enter
        bool altEnter = Raylib.IsKeyDown(KeyboardKey.LeftAlt) && Raylib.IsKeyPressed(KeyboardKey.Enter);
        var frame = active.Resolve(key => Raylib.IsKeyDown(key), key => Raylib.IsKeyPressed(key), pad, held, stickPressed);
        if (altEnter) frame = new ActionFrame(frame.Down & ~Bit(GameAction.Menu) | Bit(GameAction.Fullscreen), frame.Pressed & ~Bit(GameAction.Menu) | Bit(GameAction.Fullscreen));

        Captured = ReadCapture(pad);

        // The device touched last: the pad once a bound button or the stick is pressed, the keyboard once a bound key is
        LastDevice = InputBindings.DeviceAfter(LastDevice, KeyActivity(), active.PadActivity(pad, stickPressed));
        return frame;
    }

    private static uint Bit(GameAction action) => 1u << (int)action;

    /// <summary>Whether a bound key went down this frame (the only keyboard read of the device memory).</summary>
    private static bool KeyActivity()
    {
        foreach (var (_, inputs) in active.All)
            foreach (var key in inputs.Keys)
                if (Raylib.IsKeyPressed(key)) return true;
        return false;
    }

    /// <summary>
    /// Trembles the pad in force (plan 12 · Q24, <see cref="Haptics"/>): raylib times it. Nothing without a window (a
    /// test's, a headless game's) or with no pad plugged in.
    /// </summary>
    public static void Vibrate(float left, float right, float seconds)
    {
        if (!Raylib.IsWindowReady() || !Raylib.IsGamepadAvailable(padIndex)) return;
        Raylib.SetGamepadVibration(padIndex, left, right, seconds);
    }

    /// <summary>The pad as raylib reports it (the thin layer: everything made of it is <see cref="InputBindings"/>'s).</summary>
    private static PadSnapshot ReadPad()
    {
        if (!Raylib.IsGamepadAvailable(padIndex)) return PadSnapshot.None;
        uint down = 0, pressed = 0;
        foreach (var button in Enum.GetValues<GamepadButton>())
        {
            if (button == GamepadButton.Unknown) continue;
            uint bit = 1u << (int)button;
            if (Raylib.IsGamepadButtonDown(padIndex, button)) down |= bit;
            if (Raylib.IsGamepadButtonPressed(padIndex, button)) pressed |= bit;
        }
        return new PadSnapshot(true, down, pressed,
            Raylib.GetGamepadAxisMovement(padIndex, GamepadAxis.LeftX), Raylib.GetGamepadAxisMovement(padIndex, GamepadAxis.LeftY));
    }

    public static bool IsActionPressed(GameAction action)
    {
        // Running is a key held, so it counts as pressed for as long as it is down
        if (action == GameAction.Run) return IsActionDown(action);
        if (fed is { } frame) return frame.IsPressed(action);
        // Outside the engine's own frame (the title's first moments, the harness's windows) the keys are asked for directly
        var binding = Of(action);
        foreach (var key in binding.Keys)
            if (Raylib.IsKeyPressed(key)) return true;
        foreach (var button in binding.Pad)
            if (Raylib.IsGamepadButtonPressed(padIndex, button)) return true;
        return false;
    }

    public static bool IsActionDown(GameAction action)
    {
        if (fed is { } frame) return frame.IsDown(action);
        var binding = Of(action);
        foreach (var key in binding.Keys)
            if (Raylib.IsKeyDown(key)) return true;
        foreach (var button in binding.Pad)
            if (Raylib.IsGamepadButtonDown(padIndex, button)) return true;
        return false;
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
