using System;
using System.Collections.Generic;
using System.Linq;
using Raylib_cs;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Platinum's Button Mode (plan 12 · Q4): Normal; Start=X, where the Start button and the left face button change
/// places (the menu on the face button, the registered item on Start); and L=A, where the left shoulder answers as
/// the A button does. Each is a preset of the binding set, so a rebinding (plan 12 · Q5) is laid over one.
/// </summary>
public enum ButtonMode { Normal, StartIsX, LIsA }

/// <summary>Which device the player touched last: what a prompt names the controls of.</summary>
public enum InputDevice { Keyboard, Pad }

/// <summary>The directions a pad's stick is held in, as the four direction actions.</summary>
[Flags]
public enum StickDirs { None = 0, Up = 1, Down = 2, Left = 4, Right = 8 }

/// <summary>One action's keys and pad buttons.</summary>
public sealed record ActionInputs(KeyboardKey[] Keys, GamepadButton[] Pad)
{
    public static readonly ActionInputs None = new(Array.Empty<KeyboardKey>(), Array.Empty<GamepadButton>());

    public bool IsBound => Keys.Length > 0 || Pad.Length > 0;
}

/// <summary>An action's inputs as <c>settings.json</c> keeps them: names, so a file stays readable and a name a later build drops is passed over.</summary>
public sealed class SavedBinding
{
    public string[] Keys { get; set; } = Array.Empty<string>();
    public string[] Pad { get; set; } = Array.Empty<string>();
}

/// <summary>Two actions that read the same key or button in the same state (<see cref="InputBindings.Conflicts"/>).</summary>
public readonly record struct BindingConflict(GameAction First, GameAction Second, string Input);

/// <summary>What the pad was doing this frame, as the polling layer reports it (the only part that asks raylib).</summary>
/// <param name="Down">Buttons held, a bit to each <see cref="GamepadButton"/> by its number.</param>
/// <param name="Pressed">Buttons that went down this frame.</param>
/// <param name="StickX">The left stick, -1 (left) to 1 (right).</param>
/// <param name="StickY">The left stick, -1 (up) to 1 (down), as raylib reports it.</param>
public readonly record struct PadSnapshot(bool Connected, uint Down, uint Pressed, float StickX, float StickY)
{
    public static readonly PadSnapshot None = new(false, 0, 0, 0, 0);

    private static uint Bit(GamepadButton button) => 1u << (int)button;

    public bool IsDown(GamepadButton button) => (Down & Bit(button)) != 0;
    public bool IsPressed(GamepadButton button) => (Pressed & Bit(button)) != 0;

    /// <summary>A snapshot with the given buttons held (and pressed, when <paramref name="fresh"/>).</summary>
    public static PadSnapshot Holding(bool fresh, params GamepadButton[] buttons)
    {
        uint mask = 0;
        foreach (var button in buttons) mask |= Bit(button);
        return new PadSnapshot(true, mask, fresh ? mask : 0, 0, 0);
    }

    /// <summary>A snapshot with the stick pushed to a place.</summary>
    public static PadSnapshot Pushed(float x, float y) => new(true, 0, 0, x, y);
}

/// <summary>
/// Turns a pad's stick into the four direction actions (plan 12 · Q4). A circular dead zone with a little
/// hysteresis (let go at 0.85 of what it took to push), and eight ways: an axis counts when it is at least half the
/// stronger one, so pushing mostly right never walks up as well. GPU-free; one per player.
/// </summary>
public sealed class StickReader
{
    /// <summary>How much of the stronger axis the weaker one needs before it counts too.</summary>
    public const float DiagonalShare = 0.5f;

    private StickDirs held;

    /// <summary>The directions held now.</summary>
    public StickDirs Held => held;

    /// <summary>Reads one frame of the stick; returns the directions held, and the ones that came on this frame.</summary>
    public StickDirs Update(float x, float y, float deadZone, out StickDirs pressed)
    {
        deadZone = Math.Clamp(deadZone, 0.05f, 0.95f);
        float threshold = held != StickDirs.None ? deadZone * 0.85f : deadZone;
        float ax = Math.Abs(x), ay = Math.Abs(y), strongest = Math.Max(ax, ay);
        var now = StickDirs.None;
        if (MathF.Sqrt(x * x + y * y) >= threshold)
        {
            if (ax >= strongest * DiagonalShare) now |= x < 0 ? StickDirs.Left : StickDirs.Right;
            if (ay >= strongest * DiagonalShare) now |= y < 0 ? StickDirs.Up : StickDirs.Down;
        }
        pressed = now & ~held;
        held = now;
        return now;
    }

    /// <summary>Lets go: the pad was unplugged, or another one is read.</summary>
    public void Reset() => held = StickDirs.None;
}

/// <summary>
/// A set of bindings (plan 12 · Q4): for each <see cref="GameAction"/> the keys and the pad buttons that do it. It is
/// data, so a preset, a player's own overrides and the checks on both are all GPU-free; only reading a keyboard and a
/// pad is not (<see cref="InputManager"/>).
/// </summary>
public sealed class InputBindings
{
    private readonly ActionInputs[] inputs;

    private InputBindings(ActionInputs[] inputs) => this.inputs = inputs;

    /// <summary>The inputs of an action.</summary>
    public ActionInputs this[GameAction action] => inputs[(int)action];

    /// <summary>Every action in the order of <see cref="GameAction"/>.</summary>
    public IEnumerable<(GameAction Action, ActionInputs Inputs)> All =>
        Enum.GetValues<GameAction>().Select(a => (a, inputs[(int)a]));

    /// <summary>
    /// Actions that may share an input because the game reads them in different places: B and Run (Run is read in
    /// the field only), the Pokétch's next app and the right page (the field only), the Pokétch's touch and the left page.
    /// </summary>
    private static readonly (GameAction, GameAction)[] MayShare =
    {
        (GameAction.Cancel, GameAction.Run),
        (GameAction.PoketchApp, GameAction.PageRight),
        (GameAction.PoketchTouch, GameAction.PageLeft)
    };

    private static ActionInputs Of(KeyboardKey[] keys, params GamepadButton[] pad) => new(keys, pad);

    private static KeyboardKey[] K(params KeyboardKey[] keys) => keys;

    /// <summary>The keys and buttons the game has always had, with the new actions' (the defaults are today's keys).</summary>
    private static ActionInputs[] Normal()
    {
        var table = new ActionInputs[Enum.GetValues<GameAction>().Length];
        void Set(GameAction action, ActionInputs value) => table[(int)action] = value;
        Set(GameAction.Up, Of(K(KeyboardKey.Up, KeyboardKey.W), GamepadButton.LeftFaceUp));
        Set(GameAction.Down, Of(K(KeyboardKey.Down, KeyboardKey.S), GamepadButton.LeftFaceDown));
        Set(GameAction.Left, Of(K(KeyboardKey.Left, KeyboardKey.A), GamepadButton.LeftFaceLeft));
        Set(GameAction.Right, Of(K(KeyboardKey.Right, KeyboardKey.D), GamepadButton.LeftFaceRight));
        Set(GameAction.Confirm, Of(K(KeyboardKey.Z, KeyboardKey.Space), GamepadButton.RightFaceDown));
        Set(GameAction.Cancel, Of(K(KeyboardKey.X, KeyboardKey.Escape), GamepadButton.RightFaceRight));
        Set(GameAction.Menu, Of(K(KeyboardKey.Enter, KeyboardKey.Tab), GamepadButton.MiddleRight));
        Set(GameAction.Run, Of(K(KeyboardKey.LeftShift, KeyboardKey.X), GamepadButton.RightFaceRight));
        Set(GameAction.Item, Of(K(KeyboardKey.C), GamepadButton.RightFaceLeft));
        Set(GameAction.Poketch, Of(K(KeyboardKey.P), GamepadButton.RightFaceUp));
        Set(GameAction.PoketchApp, Of(K(KeyboardKey.O), GamepadButton.RightTrigger1));
        Set(GameAction.PoketchTouch, Of(K(KeyboardKey.I), GamepadButton.LeftTrigger1));
        Set(GameAction.PageLeft, Of(K(KeyboardKey.Q), GamepadButton.LeftTrigger1));
        Set(GameAction.PageRight, Of(K(KeyboardKey.E), GamepadButton.RightTrigger1));
        Set(GameAction.Fullscreen, Of(K(KeyboardKey.F11)));
        Set(GameAction.Mute, Of(K(KeyboardKey.M)));
        Set(GameAction.Screenshot, Of(K(KeyboardKey.F12)));
        return table;
    }

    /// <summary>The preset a Button Mode names.</summary>
    public static InputBindings For(ButtonMode mode)
    {
        var table = Normal();
        switch (mode)
        {
            case ButtonMode.StartIsX:
                // The Start button and the left face button change places
                table[(int)GameAction.Menu] = table[(int)GameAction.Menu] with { Pad = new[] { GamepadButton.RightFaceLeft } };
                table[(int)GameAction.Item] = table[(int)GameAction.Item] with { Pad = new[] { GamepadButton.MiddleRight } };
                break;
            case ButtonMode.LIsA:
                // L answers as A does; the Pokétch's touch gives up its button (it keeps I), and the left page moves to the second trigger
                table[(int)GameAction.Confirm] = table[(int)GameAction.Confirm] with { Pad = new[] { GamepadButton.RightFaceDown, GamepadButton.LeftTrigger1 } };
                table[(int)GameAction.PoketchTouch] = table[(int)GameAction.PoketchTouch] with { Pad = Array.Empty<GamepadButton>() };
                table[(int)GameAction.PageLeft] = table[(int)GameAction.PageLeft] with { Pad = new[] { GamepadButton.LeftTrigger2 } };
                break;
        }
        return new InputBindings(table);
    }

    /// <summary>These bindings with one action's inputs replaced (a player's own rebinding).</summary>
    public InputBindings With(GameAction action, ActionInputs value)
    {
        var copy = (ActionInputs[])inputs.Clone();
        copy[(int)action] = value;
        return new InputBindings(copy);
    }

    /// <summary>The bindings a player's options give: the Button Mode's preset with their own overrides laid over it.</summary>
    public static InputBindings From(GameSettings settings)
    {
        var bindings = For(settings.ButtonMode);
        foreach (var (name, saved) in settings.Bindings)
        {
            if (!Enum.TryParse<GameAction>(name, out var action) || !Enum.IsDefined(action)) continue;
            bindings = bindings.With(action, new ActionInputs(
                saved.Keys.Select(k => Enum.TryParse<KeyboardKey>(k, out var key) ? key : KeyboardKey.Null).Where(k => k != KeyboardKey.Null).Distinct().ToArray(),
                saved.Pad.Select(p => Enum.TryParse<GamepadButton>(p, out var button) ? button : GamepadButton.Unknown).Where(b => b != GamepadButton.Unknown).Distinct().ToArray()));
        }
        return bindings;
    }

    /// <summary>The overrides that make these bindings from <paramref name="mode"/>'s preset: what <c>settings.json</c> keeps.</summary>
    public Dictionary<string, SavedBinding> OverridesOn(ButtonMode mode)
    {
        var preset = For(mode);
        var saved = new Dictionary<string, SavedBinding>();
        foreach (var (action, own) in All)
        {
            var base_ = preset[action];
            if (own.Keys.SequenceEqual(base_.Keys) && own.Pad.SequenceEqual(base_.Pad)) continue;
            saved[action.ToString()] = new SavedBinding { Keys = own.Keys.Select(k => k.ToString()).ToArray(), Pad = own.Pad.Select(p => p.ToString()).ToArray() };
        }
        return saved;
    }

    /// <summary>Actions with neither a key nor a button.</summary>
    public IEnumerable<GameAction> Unbound => All.Where(a => !a.Inputs.IsBound).Select(a => a.Action);

    /// <summary>
    /// Every pair of actions that read the same key, or the same pad button, in the same state (plan 12 · Q5 shows
    /// them in red). Pairs the game reads in different places (<see cref="MayShare"/>) are not conflicts.
    /// </summary>
    public IReadOnlyList<BindingConflict> Conflicts()
    {
        var found = new List<BindingConflict>();
        var actions = Enum.GetValues<GameAction>();
        for (int i = 0; i < actions.Length; i++)
            for (int j = i + 1; j < actions.Length; j++)
            {
                var (a, b) = (actions[i], actions[j]);
                if (MayShare.Any(p => p == (a, b) || p == (b, a))) continue;
                foreach (var key in this[a].Keys.Intersect(this[b].Keys)) found.Add(new BindingConflict(a, b, key.ToString()));
                foreach (var button in this[a].Pad.Intersect(this[b].Pad)) found.Add(new BindingConflict(a, b, button.ToString()));
            }
        return found;
    }

    /// <summary>
    /// What the keyboard and the pad did this frame as an <see cref="ActionFrame"/>. The sources are handed in, so this
    /// is the whole of the reading but the asking of raylib: the stick's directions count as the d-pad's.
    /// </summary>
    public ActionFrame Resolve(Func<KeyboardKey, bool> keyDown, Func<KeyboardKey, bool> keyPressed, PadSnapshot pad, StickDirs stickHeld, StickDirs stickPressed)
    {
        uint down = 0, pressed = 0;
        foreach (var (action, own) in All)
        {
            uint bit = 1u << (int)action;
            bool isDown = false, isPressed = false;
            foreach (var key in own.Keys)
            {
                isDown |= keyDown(key);
                isPressed |= keyPressed(key);
            }
            if (pad.Connected)
                foreach (var button in own.Pad)
                {
                    isDown |= pad.IsDown(button);
                    isPressed |= pad.IsPressed(button);
                }
            var stick = StickOf(action);
            if (stick != StickDirs.None)
            {
                isDown |= (stickHeld & stick) != 0;
                isPressed |= (stickPressed & stick) != 0;
            }
            if (isDown) down |= bit;
            if (isPressed) pressed |= bit;
        }
        return new ActionFrame(down, pressed);
    }

    /// <summary>Whether this frame's pad activity (a bound button pressed, or the stick pushed) says the player is on the pad.</summary>
    public bool PadActivity(PadSnapshot pad, StickDirs stickPressed) =>
        pad.Connected && (stickPressed != StickDirs.None || All.Any(a => a.Inputs.Pad.Any(pad.IsPressed)));

    /// <summary>The direction a stick direction is.</summary>
    public static StickDirs StickOf(GameAction action) => action switch
    {
        GameAction.Up => StickDirs.Up,
        GameAction.Down => StickDirs.Down,
        GameAction.Left => StickDirs.Left,
        GameAction.Right => StickDirs.Right,
        _ => StickDirs.None
    };

    /// <summary>Which device the player is on after a frame: the pad if it was touched, else the keyboard if it was, else as before.</summary>
    public static InputDevice DeviceAfter(InputDevice before, bool keyActivity, bool padActivity) =>
        padActivity ? InputDevice.Pad : keyActivity ? InputDevice.Keyboard : before;
}
