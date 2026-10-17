using System.Text.Json;
using System.Text.Json.Serialization;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI;
using Raylib_cs;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 12 · Q4: the pad and the binding set. Everything but the polling of raylib is plain data and rules, so these
/// tests hand <see cref="InputBindings.Resolve"/> a made-up keyboard and a made-up pad. No pad was plugged in while
/// this was written: what a real controller reports is what these snapshots stand for. It shares the process's
/// binding set, language and last device with the whole run, so it runs alone and puts them back.
/// </summary>
[Collection("Shared clocks and dice")]
public class PadInputTests
{
    private static readonly JsonSerializerOptions Json = new() { Converters = { new JsonStringEnumConverter() } };

    private static ActionFrame Read(InputBindings bindings, IEnumerable<KeyboardKey>? keysDown = null, IEnumerable<KeyboardKey>? keysPressed = null,
        PadSnapshot? pad = null, StickDirs held = StickDirs.None, StickDirs pressed = StickDirs.None)
    {
        var down = (keysDown ?? Array.Empty<KeyboardKey>()).ToHashSet();
        var fresh = (keysPressed ?? Array.Empty<KeyboardKey>()).ToHashSet();
        return bindings.Resolve(down.Contains, fresh.Contains, pad ?? PadSnapshot.None, held, pressed);
    }

    // ----------------------------------------------------------------------------- the presets

    [Fact]
    public void EveryActionIsBoundInEveryPreset()
    {
        foreach (var mode in Enum.GetValues<ButtonMode>())
        {
            var bindings = InputBindings.For(mode);
            Assert.Empty(bindings.Unbound);
            // The keyboard can do everything whatever the pad's mode is
            foreach (var action in Enum.GetValues<GameAction>())
                Assert.True(bindings[action].Keys.Length > 0, $"{action} has no key in {mode}");
        }
    }

    [Fact]
    public void NoPresetBindsTwoActionsToOneInputBeyondTheOnesThatAreReadInDifferentPlaces()
    {
        foreach (var mode in Enum.GetValues<ButtonMode>())
            Assert.Empty(InputBindings.For(mode).Conflicts());
    }

    [Fact]
    public void TheChecksFindADoubleBinding()
    {
        var bindings = InputBindings.For(ButtonMode.Normal)
            .With(GameAction.Menu, new ActionInputs(new[] { KeyboardKey.Z }, Array.Empty<GamepadButton>()));
        var found = bindings.Conflicts();
        Assert.Contains(found, c => c is { First: GameAction.Confirm, Second: GameAction.Menu, Input: "Z" });

        // The same button on two actions the game reads at once is found too
        var pad = InputBindings.For(ButtonMode.Normal)
            .With(GameAction.Item, new ActionInputs(Array.Empty<KeyboardKey>(), new[] { GamepadButton.RightFaceDown }));
        Assert.Contains(pad.Conflicts(), c => c.First == GameAction.Confirm && c.Second == GameAction.Item);
    }

    [Fact]
    public void NormalIsTodaysKeysAndButtons()
    {
        var normal = InputBindings.For(ButtonMode.Normal);
        Assert.Equal(new[] { KeyboardKey.Up, KeyboardKey.W }, normal[GameAction.Up].Keys);
        Assert.Equal(new[] { KeyboardKey.Z, KeyboardKey.Space }, normal[GameAction.Confirm].Keys);
        Assert.Equal(new[] { KeyboardKey.X, KeyboardKey.Escape }, normal[GameAction.Cancel].Keys);
        Assert.Equal(new[] { KeyboardKey.Enter, KeyboardKey.Tab }, normal[GameAction.Menu].Keys);
        Assert.Equal(new[] { KeyboardKey.LeftShift, KeyboardKey.X }, normal[GameAction.Run].Keys);
        Assert.Equal(new[] { GamepadButton.RightFaceDown }, normal[GameAction.Confirm].Pad);
        Assert.Equal(new[] { GamepadButton.RightFaceRight }, normal[GameAction.Cancel].Pad);
        Assert.Equal(new[] { GamepadButton.MiddleRight }, normal[GameAction.Menu].Pad);
        Assert.Equal(new[] { GamepadButton.RightFaceLeft }, normal[GameAction.Item].Pad);
        // The three reads that used to be stray ones are actions now
        Assert.Equal(new[] { KeyboardKey.F11 }, normal[GameAction.Fullscreen].Keys);
        Assert.Equal(new[] { KeyboardKey.M }, normal[GameAction.Mute].Keys);
        Assert.Equal(new[] { KeyboardKey.F12 }, normal[GameAction.Screenshot].Keys);
        Assert.Equal(new[] { KeyboardKey.Q }, normal[GameAction.PageLeft].Keys);
        Assert.Equal(new[] { KeyboardKey.E }, normal[GameAction.PageRight].Keys);
    }

    [Fact]
    public void StartEqualsXSwapsWhatPlatinumSwaps()
    {
        var normal = InputBindings.For(ButtonMode.Normal);
        var swapped = InputBindings.For(ButtonMode.StartIsX);
        // The menu and the registered item change buttons; the keyboard is not touched
        Assert.Equal(normal[GameAction.Menu].Pad, swapped[GameAction.Item].Pad);
        Assert.Equal(normal[GameAction.Item].Pad, swapped[GameAction.Menu].Pad);
        Assert.Equal(normal[GameAction.Menu].Keys, swapped[GameAction.Menu].Keys);
        foreach (var action in Enum.GetValues<GameAction>().Except(new[] { GameAction.Menu, GameAction.Item }))
            Assert.Equal(normal[action].Pad, swapped[action].Pad);

        // Pressed on the pad: the left face button opens the menu, Start the registered item
        var onX = Read(swapped, pad: PadSnapshot.Holding(true, GamepadButton.RightFaceLeft));
        Assert.True(onX.IsPressed(GameAction.Menu));
        Assert.False(onX.IsPressed(GameAction.Item));
        var onStart = Read(swapped, pad: PadSnapshot.Holding(true, GamepadButton.MiddleRight));
        Assert.True(onStart.IsPressed(GameAction.Item));
        Assert.False(onStart.IsPressed(GameAction.Menu));
    }

    [Fact]
    public void LEqualsAMakesTheLeftShoulderConfirm()
    {
        var bindings = InputBindings.For(ButtonMode.LIsA);
        var frame = Read(bindings, pad: PadSnapshot.Holding(true, GamepadButton.LeftTrigger1));
        Assert.True(frame.IsPressed(GameAction.Confirm));
        // L gave up the Pokétch's touch and the left page, which keep their keys
        Assert.False(frame.IsPressed(GameAction.PoketchTouch));
        Assert.False(frame.IsPressed(GameAction.PageLeft));
        Assert.True(Read(bindings, keysPressed: new[] { KeyboardKey.I }).IsPressed(GameAction.PoketchTouch));
        // A still confirms
        Assert.True(Read(bindings, pad: PadSnapshot.Holding(true, GamepadButton.RightFaceDown)).IsPressed(GameAction.Confirm));
    }

    // ----------------------------------------------------------------------------- reading a frame

    [Fact]
    public void EnterStillAnswersABattlesMenuAndAPadsStartDoesToo()
    {
        var bindings = InputBindings.For(ButtonMode.Normal);
        Assert.True(Read(bindings, keysPressed: new[] { KeyboardKey.Enter }).IsPressed(GameAction.Menu));
        Assert.True(Read(bindings, pad: PadSnapshot.Holding(true, GamepadButton.MiddleRight)).IsPressed(GameAction.Menu));
        // A held button is down but not pressed again
        var held = Read(bindings, pad: PadSnapshot.Holding(false, GamepadButton.RightFaceDown));
        Assert.True(held.IsDown(GameAction.Confirm));
        Assert.False(held.IsPressed(GameAction.Confirm));
    }

    [Fact]
    public void ABoundButtonOfAPadThatIsNotThereDoesNothing()
    {
        var gone = new PadSnapshot(false, uint.MaxValue, uint.MaxValue, 1, 1);
        var frame = Read(InputBindings.For(ButtonMode.Normal), pad: gone);
        Assert.Equal(ActionFrame.None, frame);
    }

    [Fact]
    public void TheSticksDirectionsAreTheDirectionActionsAndWalkThePlayer()
    {
        var reader = new StickReader();
        var held = reader.Update(0.9f, 0f, 0.35f, out var pressed);
        Assert.Equal(StickDirs.Right, held);
        var frame = Read(InputBindings.For(ButtonMode.Normal), held: held, pressed: pressed);
        Assert.True(frame.IsDown(GameAction.Right));
        Assert.True(frame.IsPressed(GameAction.Right));
        Assert.False(frame.IsDown(GameAction.Up));

        // The same push a frame on is held, not pressed
        held = reader.Update(0.9f, 0f, 0.35f, out pressed);
        frame = Read(InputBindings.For(ButtonMode.Normal), held: held, pressed: pressed);
        Assert.True(frame.IsDown(GameAction.Right));
        Assert.False(frame.IsPressed(GameAction.Right));
    }

    [Fact]
    public void TheDeadZoneKeepsADriftingStickStill()
    {
        var reader = new StickReader();
        // Inside the dead zone, however it leans
        Assert.Equal(StickDirs.None, reader.Update(0.2f, 0.2f, 0.35f, out _));
        Assert.Equal(StickDirs.None, reader.Update(-0.3f, 0f, 0.35f, out _));
        // Past it, at the zone's own edge
        Assert.Equal(StickDirs.Left, reader.Update(-0.36f, 0f, 0.35f, out _));
        // A wider zone holds what a narrower one lets through
        Assert.Equal(StickDirs.None, new StickReader().Update(0.4f, 0f, 0.5f, out _));
        // The zone is a circle: a diagonal push of 0.3 on each axis is 0.42 from the middle, past 0.35
        Assert.Equal(StickDirs.Up | StickDirs.Left, new StickReader().Update(-0.3f, -0.3f, 0.35f, out _));
    }

    [Fact]
    public void ThePushesLeaningDoesNotWalkASecondWay()
    {
        // Mostly right with a little up is right alone; an even push is both
        Assert.Equal(StickDirs.Right, new StickReader().Update(0.9f, -0.2f, 0.35f, out _));
        Assert.Equal(StickDirs.Right | StickDirs.Up, new StickReader().Update(0.8f, -0.6f, 0.35f, out _));
        Assert.Equal(StickDirs.Down, new StickReader().Update(0.1f, 0.9f, 0.35f, out _));
    }

    [Fact]
    public void ALetGoStickDoesntFlickerAtTheEdge()
    {
        var reader = new StickReader();
        Assert.Equal(StickDirs.Right, reader.Update(0.40f, 0f, 0.35f, out _));
        // Just under the zone, but within the hysteresis: still held
        Assert.Equal(StickDirs.Right, reader.Update(0.32f, 0f, 0.35f, out var pressed));
        Assert.Equal(StickDirs.None, pressed);
        Assert.Equal(StickDirs.None, reader.Update(0.20f, 0f, 0.35f, out _));
        // And pressed afresh once it is pushed again
        reader.Update(0.40f, 0f, 0.35f, out pressed);
        Assert.Equal(StickDirs.Right, pressed);
    }

    // ----------------------------------------------------------------------------- the device memory

    [Fact]
    public void TheDeviceIsTheLastOneTouched()
    {
        var onKeys = InputBindings.DeviceAfter(InputDevice.Pad, keyActivity: true, padActivity: false);
        Assert.Equal(InputDevice.Keyboard, onKeys);
        Assert.Equal(InputDevice.Pad, InputBindings.DeviceAfter(onKeys, keyActivity: false, padActivity: true));
        Assert.Equal(InputDevice.Pad, InputBindings.DeviceAfter(InputDevice.Pad, false, false));
        Assert.Equal(InputDevice.Keyboard, InputBindings.DeviceAfter(InputDevice.Keyboard, false, false));

        var bindings = InputBindings.For(ButtonMode.Normal);
        Assert.True(bindings.PadActivity(PadSnapshot.Holding(true, GamepadButton.RightFaceDown), StickDirs.None));
        Assert.True(bindings.PadActivity(PadSnapshot.Pushed(1, 0), StickDirs.Right));
        Assert.False(bindings.PadActivity(PadSnapshot.Holding(false, GamepadButton.RightFaceDown), StickDirs.None));
        Assert.False(bindings.PadActivity(PadSnapshot.None, StickDirs.None));
    }

    // ----------------------------------------------------------------------------- prompts

    [Fact]
    public void ThePromptsNameTheDeviceInUse()
    {
        try
        {
            InputManager.Reset();
            Text.Use("en");
            Assert.Equal("Z", Text.Of("shared.keys.z"));
            Assert.Equal("Esc", Text.Of("shared.keys.esc"));
            Assert.Equal("Enter", Text.Of("shared.keys.enter"));
            Assert.Equal("PRESS  Z  OR  ENTER", Text.Of("title.press"));

            InputManager.LastDevice = InputDevice.Pad;
            Assert.Equal("A", Text.Of("shared.keys.z"));
            Assert.Equal("B", Text.Of("shared.keys.esc"));
            Assert.Equal("B", Text.Of("shared.keys.x"));
            Assert.Equal("Start", Text.Of("shared.keys.enter"));
            Assert.Equal("D-pad", Text.Of("shared.keys.arrows"));
            Assert.Equal("X", Text.Of("capsules.keyC"));
            Assert.Equal("PRESS  A  OR  START", Text.Of("title.press"));
            Assert.Equal("Press A to stop the cursor!", Text.Of("arcadeBoard.running"));
            // The directions are the d-pad on the pad (plan 12 · Q5)
            Assert.Equal("D-pad", Text.Of("shared.keys.leftRight"));

            // The prompt follows the preset: with Start = X, the menu is on the X button and Start is the item
            InputManager.Use(InputBindings.For(ButtonMode.StartIsX));
            Assert.Equal("X", Text.Of("shared.keys.enter"));
            Assert.Equal("Start", Text.Of("capsules.keyC"));

            InputManager.LastDevice = InputDevice.Keyboard;
            Assert.Equal("Enter", Text.Of("shared.keys.enter"));
            Assert.Equal("C", Text.Of("capsules.keyC"));
        }
        finally { InputManager.Reset(); Text.Use("en"); }
    }

    [Fact]
    public void RomanianNamesThePadAsWell()
    {
        try
        {
            InputManager.Reset();
            Text.Use("ro");
            Assert.Equal("APASĂ  Z  SAU  ENTER", Text.Of("title.press"));
            InputManager.LastDevice = InputDevice.Pad;
            Assert.Equal("APASĂ  A  SAU  START", Text.Of("title.press"));
            Assert.Equal("Cruce", Text.Of("shared.keys.arrows"));
            Assert.Equal("Apasă A ca să oprești cursorul!", Text.Of("arcadeBoard.running"));
        }
        finally { InputManager.Reset(); Text.Use("en"); }
    }

    [Fact]
    public void EveryNewStringHasItsRomanianWithTheSamePlaceholders()
    {
        string[] keys =
        {
            "options.buttonMode.label", "options.buttonMode.value.Normal", "options.buttonMode.value.StartIsX", "options.buttonMode.value.LIsA",
            "options.buttonMode.help.Normal", "options.buttonMode.help.StartIsX", "options.buttonMode.help.LIsA",
            "options.stickDeadZone.label", "options.stickDeadZone.value", "options.stickDeadZone.help",
            "options.gamepad.label", "options.gamepad.value", "options.gamepad.help",
            "title.press", "arcadeBoard.running", "controls.unbound",
            "controls.action.PageLeft", "controls.action.PageRight", "controls.action.Fullscreen", "controls.action.Mute", "controls.action.Screenshot",
            "padButtons.bottom", "padButtons.right", "padButtons.left", "padButtons.top", "padButtons.start", "padButtons.select",
            "padButtons.l1", "padButtons.r1", "padButtons.l2", "padButtons.r2", "padButtons.dpad",
            "padButtons.dpadUp", "padButtons.dpadDown", "padButtons.dpadLeft", "padButtons.dpadRight"
        };
        var ro = Text.KeysOf("ro");
        foreach (var key in keys) Assert.Contains(key, ro);
    }

    // ----------------------------------------------------------------------------- the settings and the options

    [Fact]
    public void TheSettingsRoundTripAndAnOldFileReadsAsToday()
    {
        var settings = new GameSettings { ButtonMode = ButtonMode.LIsA, StickDeadZone = 20, GamepadIndex = 2 };
        settings.Bindings["Mute"] = new SavedBinding { Keys = new[] { "N" }, Pad = Array.Empty<string>() };
        var read = JsonSerializer.Deserialize<GameSettings>(settings.ToFile(), Json)!;
        Assert.Equal((ButtonMode.LIsA, 20, 2), (read.ButtonMode, read.StickDeadZone, read.GamepadIndex));
        Assert.Equal(new[] { "N" }, read.Bindings["Mute"].Keys);

        var old = JsonSerializer.Deserialize<GameSettings>("{ \"Quality\": \"Medium\" }", Json)!;
        Assert.Equal((ButtonMode.Normal, 35, 0), (old.ButtonMode, old.StickDeadZone, old.GamepadIndex));
        Assert.Empty(old.Bindings);
        // ...and its bindings are today's
        var bindings = InputBindings.From(old);
        Assert.Equal(InputBindings.For(ButtonMode.Normal)[GameAction.Confirm].Keys, bindings[GameAction.Confirm].Keys);
    }

    [Fact]
    public void ThePlayersOwnBindingsLieOverThePresetAndRoundTrip()
    {
        var own = InputBindings.For(ButtonMode.Normal)
            .With(GameAction.Mute, new ActionInputs(new[] { KeyboardKey.N }, new[] { GamepadButton.MiddleLeft }));
        var settings = new GameSettings { ButtonMode = ButtonMode.StartIsX, Bindings = own.OverridesOn(ButtonMode.StartIsX) };
        // The override on top of Start = X is only what differs from that preset: Mute, and the two the preset moved (undone by "own", which was made on Normal)
        Assert.Contains("Mute", settings.Bindings.Keys);
        var again = InputBindings.From(JsonSerializer.Deserialize<GameSettings>(settings.ToFile(), Json)!);
        Assert.Equal(own[GameAction.Mute].Keys, again[GameAction.Mute].Keys);
        Assert.Equal(own[GameAction.Mute].Pad, again[GameAction.Mute].Pad);
        Assert.Equal(own[GameAction.Menu].Pad, again[GameAction.Menu].Pad);
    }

    [Fact]
    public void ANameAFileHasThatTheGameDoesntKnowIsPassedOver()
    {
        var settings = new GameSettings();
        settings.Bindings["Teleport"] = new SavedBinding { Keys = new[] { "Z" } };
        settings.Bindings["Mute"] = new SavedBinding { Keys = new[] { "NotAKey", "N" }, Pad = new[] { "NotAButton" } };
        var bindings = InputBindings.From(settings);
        Assert.Equal(new[] { KeyboardKey.N }, bindings[GameAction.Mute].Keys);
        Assert.Empty(bindings[GameAction.Mute].Pad);
        Assert.Equal(InputBindings.For(ButtonMode.Normal)[GameAction.Confirm].Keys, bindings[GameAction.Confirm].Keys);
    }

    [Fact]
    public void TheOptionsRowsStandBeforeHelpAndChange()
    {
        var rows = Enum.GetValues<OptionRow>();
        Assert.Equal(new[] { OptionRow.ButtonMode, OptionRow.StickDeadZone, OptionRow.Gamepad, OptionRow.Controls, OptionRow.Help }, rows[^5..]);

        var s = new GameSettings();
        OptionsScreen.Change(s, OptionRow.ButtonMode, 1);
        Assert.Equal(ButtonMode.StartIsX, s.ButtonMode);
        OptionsScreen.Change(s, OptionRow.ButtonMode, 1);
        Assert.Equal(ButtonMode.LIsA, s.ButtonMode);
        OptionsScreen.Change(s, OptionRow.ButtonMode, 1);
        Assert.Equal(ButtonMode.Normal, s.ButtonMode);
        OptionsScreen.Change(s, OptionRow.ButtonMode, -1);
        Assert.Equal(ButtonMode.LIsA, s.ButtonMode);

        // The dead zone steps through its list and stops at the ends
        Assert.Equal(35, s.StickDeadZone);
        OptionsScreen.Change(s, OptionRow.StickDeadZone, 1);
        Assert.Equal(40, s.StickDeadZone);
        for (int i = 0; i < 20; i++) OptionsScreen.Change(s, OptionRow.StickDeadZone, 1);
        Assert.Equal(GameSettings.StickDeadZones[^1], s.StickDeadZone);
        for (int i = 0; i < 20; i++) OptionsScreen.Change(s, OptionRow.StickDeadZone, -1);
        Assert.Equal(GameSettings.StickDeadZones[0], s.StickDeadZone);
        // A value typed into the file by hand counts as the nearest
        s.StickDeadZone = 33;
        OptionsScreen.Change(s, OptionRow.StickDeadZone, 1);
        Assert.Equal(40, s.StickDeadZone);

        // The pad is 1 to 4 and round again
        OptionsScreen.Change(s, OptionRow.Gamepad, 1);
        Assert.Equal(1, s.GamepadIndex);
        OptionsScreen.Change(s, OptionRow.Gamepad, -2);
        Assert.Equal(3, s.GamepadIndex);
        OptionsScreen.Change(s, OptionRow.Gamepad, 1);
        Assert.Equal(0, s.GamepadIndex);
    }

    [Fact]
    public void TheSettingsReachTheInputLayer()
    {
        try
        {
            InputManager.Reset();
            InputManager.Use(new GameSettings { ButtonMode = ButtonMode.StartIsX, StickDeadZone = 50, GamepadIndex = 1 });
            Assert.Equal(new[] { GamepadButton.RightFaceLeft }, InputManager.Active[GameAction.Menu].Pad);
            Assert.Equal(0.5f, InputManager.DeadZone);
            Assert.Equal(1, InputManager.PadIndex);
            // A file's nonsense is brought into range
            InputManager.Use(new GameSettings { StickDeadZone = 400, GamepadIndex = 9 });
            Assert.Equal(0.95f, InputManager.DeadZone);
            Assert.Equal(3, InputManager.PadIndex);
        }
        finally { InputManager.Reset(); }
    }

    [Fact]
    public void TheHelpPageListsWhatTheGameReadsWithTheirPadButtons()
    {
        var shown = InputManager.Bindings.Where(b => !InputManager.NotReadYet.Contains(b.Action)).Select(b => b.Action).ToArray();
        Assert.Contains(GameAction.Fullscreen, shown);
        Assert.Contains(GameAction.Mute, shown);
        Assert.DoesNotContain(GameAction.Screenshot, shown);
        // Every action that is shown has something the page can name
        Assert.All(InputManager.Bindings, b => Assert.NotEmpty(b.Keys));
    }

    // ----------------------------------------------------------------------------- the mute in a game with no window

    [Fact]
    public void TheMuteActionTogglesTheSoundInAGameWithNoWindow()
    {
        string folder = Path.Combine(Path.GetTempPath(), "pad-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            var settings = new GameSettings();
            var engine = new GameEngine(settings);
            engine.InitializeHeadless();
            Assert.False(settings.Muted);
            InputManager.Feed(ActionFrame.Press(GameAction.Mute));
            engine.Update(1f / 60f);
            Assert.True(settings.Muted);
            InputManager.Feed(ActionFrame.None);
            engine.Update(1f / 60f);
            InputManager.Feed(ActionFrame.Press(GameAction.Mute));
            engine.Update(1f / 60f);
            Assert.False(settings.Muted);
            engine.Close();
        }
        finally
        {
            InputManager.Feed(null);
            InputManager.Reset();
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }
}
