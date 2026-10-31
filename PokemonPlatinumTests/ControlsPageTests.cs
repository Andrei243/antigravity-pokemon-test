using System.Text.Json;
using System.Text.Json.Serialization;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using Raylib_cs;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 12 · Q5: hints drawn by action, the Controls page and the page buttons. No pad was plugged in while this was
/// written: the pad is a made-up <see cref="PadSnapshot"/> or a button handed straight to the page. It shares the
/// process's binding set, language and last device, so it runs with the other tests that do and puts them back.
/// </summary>
[Collection("Shared clocks and dice")]
public class ControlsPageTests
{
    private static readonly JsonSerializerOptions Json = new() { Converters = { new JsonStringEnumConverter() } };

    private static void WithBindings(GameSettings settings, Action body)
    {
        try
        {
            InputManager.Reset();
            Text.Use("en");
            InputManager.Use(settings);
            body();
        }
        finally
        {
            InputManager.Feed(null);
            InputManager.Reset();
            Text.Use("en");
        }
    }

    private static GameSettings Rebound(GameAction action, KeyboardKey[]? keys = null, GamepadButton[]? pad = null)
    {
        var settings = new GameSettings();
        var own = InputBindings.For(ButtonMode.Normal)[action];
        settings.Bindings[action.ToString()] = new SavedBinding
        {
            Keys = (keys ?? own.Keys).Select(k => k.ToString()).ToArray(),
            Pad = (pad ?? own.Pad).Select(p => p.ToString()).ToArray()
        };
        return settings;
    }

    private static ControlsScreen Page(GameSettings settings, GameAction? at = null, ControlsColumn column = ControlsColumn.Keyboard)
    {
        var screen = new ControlsScreen();
        screen.Open(settings);
        while (at != null && screen.SelectedAction != at) screen.Move(1);
        if (column == ControlsColumn.Pad) screen.MoveColumn(1);
        return screen;
    }

    // ----------------------------------------------------------------------------- hints by action

    [Fact]
    public void AHintFollowsARebindingOfItsKey()
    {
        // Confirm moved from Z (and Space) to Q alone: the Z hint names Q, the ones left alone stay as they were
        var settings = Rebound(GameAction.Confirm, keys: new[] { KeyboardKey.Q });
        WithBindings(settings, () =>
        {
            Assert.Equal("Q", Text.Of("shared.keys.z"));
            Assert.Equal("X", Text.Of("shared.keys.x"));
            Assert.Equal("Esc", Text.Of("shared.keys.esc"));
            Assert.Equal("Enter", Text.Of("shared.keys.enter"));
        });

        // Cancel without X or Esc: both of its hints name its first key
        WithBindings(Rebound(GameAction.Cancel, keys: new[] { KeyboardKey.B }), () =>
        {
            Assert.Equal("B", Text.Of("shared.keys.x"));
            Assert.Equal("B", Text.Of("shared.keys.esc"));
        });

        // An action with its key left (a second one added) keeps the table's name
        WithBindings(Rebound(GameAction.Confirm, keys: new[] { KeyboardKey.Q, KeyboardKey.Z }), () => Assert.Equal("Z", Text.Of("shared.keys.z")));

        // Nothing bound: the word for it, not a blank
        WithBindings(Rebound(GameAction.Item, keys: Array.Empty<KeyboardKey>()), () => Assert.Equal("None", Text.Of("shared.keys.c")));
    }

    [Fact]
    public void TheDirectionHintsFollowARebindingToo()
    {
        var settings = Rebound(GameAction.Left, keys: new[] { KeyboardKey.J });
        settings.Bindings[nameof(GameAction.Right)] = new SavedBinding { Keys = new[] { nameof(KeyboardKey.L) }, Pad = new[] { nameof(GamepadButton.LeftFaceRight) } };
        WithBindings(settings, () =>
        {
            Assert.Equal("J / L", Text.Of("shared.keys.leftRight"));
            Assert.Equal("Up / Down", Text.Of("shared.keys.upDown"));
            Assert.Equal("Up arrow J Down arrow L", Text.Of("shared.keys.arrows"));
        });
        WithBindings(new GameSettings(), () => Assert.Equal("Left / Right", Text.Of("shared.keys.leftRight")));
    }

    [Fact]
    public void OnThePadAHintIsTheButtonOfItsActionAndDrawsItsSign()
    {
        WithBindings(new GameSettings(), () =>
        {
            Assert.Null(Prompts.GlyphFor("shared.keys.z"));
            InputManager.LastDevice = InputDevice.Pad;
            Assert.Equal(PadGlyph.A, Prompts.GlyphFor("shared.keys.z"));
            Assert.Equal(PadGlyph.B, Prompts.GlyphFor("shared.keys.x"));
            Assert.Equal(PadGlyph.B, Prompts.GlyphFor("shared.keys.esc"));
            Assert.Equal(PadGlyph.Start, Prompts.GlyphFor("shared.keys.enter"));
            Assert.Equal(PadGlyph.X, Prompts.GlyphFor("capsules.keyC"));
            Assert.Equal(PadGlyph.DPad, Prompts.GlyphFor("shared.keys.arrows"));
            Assert.Equal(PadGlyph.DPadHorizontal, Prompts.GlyphFor("shared.keys.leftRight"));
            Assert.Equal(PadGlyph.DPadVertical, Prompts.GlyphFor("shared.keys.upDown"));
            // A key that stands for no action has no sign
            Assert.Null(Prompts.GlyphFor("shared.keys.nothing"));
        });
    }

    [Fact]
    public void ThePadSignsFollowTheButtonModeAndAPlayersOwnButtons()
    {
        var startIsX = new GameSettings { ButtonMode = ButtonMode.StartIsX };
        WithBindings(startIsX, () =>
        {
            InputManager.LastDevice = InputDevice.Pad;
            Assert.Equal(PadGlyph.X, Prompts.GlyphFor("shared.keys.enter"));
            Assert.Equal("X", Text.Of("shared.keys.enter"));
        });
        // Confirm on the top button: the hint for A shows Y's sign
        WithBindings(Rebound(GameAction.Confirm, pad: new[] { GamepadButton.RightFaceUp }), () =>
        {
            InputManager.LastDevice = InputDevice.Pad;
            Assert.Equal(PadGlyph.Y, Prompts.GlyphFor("shared.keys.z"));
            Assert.Equal("Y", Text.Of("shared.keys.z"));
        });
    }

    [Fact]
    public void EveryPadButtonTheBindingsUseHasASignAndAName()
    {
        foreach (var mode in Enum.GetValues<ButtonMode>())
            foreach (var (action, inputs) in InputBindings.For(mode).All)
                foreach (var button in inputs.Pad)
                {
                    var glyph = PadGlyphs.Of(button);
                    Assert.True(glyph != null, $"{button} of {action} in {mode} has no sign");
                    Assert.False(string.IsNullOrWhiteSpace(PadGlyphs.Name(glyph!.Value)));
                }
    }

    [Fact]
    public void TheNewHintsHaveARomanianTextAndTheFilesAreComplete()
    {
        string[] keys =
        {
            "controls.page.title", "controls.page.keyboard", "controls.page.pad", "controls.page.resetAll", "controls.page.clash",
            "controls.page.help", "controls.page.pressKey", "controls.page.pressButton", "controls.page.choose", "controls.page.resetAsk",
            "controls.page.bound", "controls.page.swapped", "controls.page.kept", "controls.page.cancelled", "controls.page.reset",
            "controls.page.allReset", "controls.page.needOne", "controls.page.hint.change", "controls.page.hint.reset",
            "controls.page.hint.column", "controls.page.hint.swap", "controls.page.hint.keep", "controls.page.hint.cancel",
            "controls.page.hint.yes", "controls.page.hint.no", "options.controls.label", "options.controls.value", "options.controls.help"
        };
        var ro = Text.KeysOf("ro");
        var en = Text.KeysOf("en");
        foreach (var key in keys)
        {
            Assert.Contains(key, en);
            Assert.Contains(key, ro);
        }
    }

    // ----------------------------------------------------------------------------- capturing

    [Fact]
    public void ThePageListsEveryActionAndTheNextKeyPressedBecomesTheActions()
    {
        WithBindings(new GameSettings(), () =>
        {
            var page = Page(new GameSettings(), GameAction.Mute);
            var settings = new GameSettings();
            Assert.Equal(GameAction.Mute, page.SelectedAction);
            Assert.Equal(Enum.GetValues<GameAction>(), ControlsScreen.Actions);

            page.Confirm();
            Assert.Equal(ControlsPhase.Capturing, page.Phase);
            Assert.True(InputManager.CaptureRequested);
            Assert.True(page.Capture(new CapturedInput(KeyboardKey.N, null)));
            Assert.Equal(ControlsPhase.Browsing, page.Phase);
            Assert.False(InputManager.CaptureRequested);
            Assert.Equal(new[] { KeyboardKey.N, KeyboardKey.M }, page.Working[GameAction.Mute].Keys);
            Assert.True(page.IsChanged(GameAction.Mute));

            // Saved with the settings, read back as the same bindings
            page.WriteTo(settings);
            var read = JsonSerializer.Deserialize<GameSettings>(settings.ToFile(), Json)!;
            Assert.Equal(new[] { KeyboardKey.N, KeyboardKey.M }, InputBindings.From(read)[GameAction.Mute].Keys);
            Assert.Single(read.Bindings);
        });
    }

    [Fact]
    public void EscapeCancelsAKeyboardCaptureAndAButtonCancelsToo()
    {
        WithBindings(new GameSettings(), () =>
        {
            var page = Page(new GameSettings(), GameAction.Mute);
            page.Confirm();
            Assert.False(page.Capture(new CapturedInput(KeyboardKey.Escape, null)));
            Assert.Equal(ControlsPhase.Browsing, page.Phase);
            Assert.False(page.IsChanged(GameAction.Mute));
            Assert.True(page.IsActive);

            // The pad's player has no Esc: a button cancels a keyboard capture
            page.Confirm();
            Assert.False(page.Capture(new CapturedInput(null, GamepadButton.RightFaceRight)));
            Assert.Equal(ControlsPhase.Browsing, page.Phase);
            Assert.False(page.IsChanged(GameAction.Mute));

            // Back at the list, Cancel leaves the page
            page.Cancel();
            Assert.False(page.IsActive);
        });
    }

    [Fact]
    public void APadCaptureTakesAButtonAndPassesOverKeysButEscape()
    {
        WithBindings(new GameSettings(), () =>
        {
            var page = Page(new GameSettings(), GameAction.Poketch, ControlsColumn.Pad);
            page.Confirm();
            Assert.False(page.Capture(new CapturedInput(KeyboardKey.K, null)));
            Assert.Equal(ControlsPhase.Capturing, page.Phase);
            Assert.True(page.Capture(new CapturedInput(null, GamepadButton.RightTrigger2)));
            Assert.Equal(new[] { GamepadButton.RightTrigger2, GamepadButton.RightFaceUp }, page.Working[GameAction.Poketch].Pad);
            Assert.Equal(new[] { KeyboardKey.P }, page.Working[GameAction.Poketch].Keys);

            page.Confirm();
            page.Capture(new CapturedInput(KeyboardKey.Escape, null));
            Assert.Equal(ControlsPhase.Browsing, page.Phase);
        });
    }

    [Fact]
    public void ABindingKeepsAtMostThreeInputs()
    {
        WithBindings(new GameSettings(), () =>
        {
            var page = Page(new GameSettings(), GameAction.Confirm);
            foreach (var key in new[] { KeyboardKey.F1, KeyboardKey.F2 })
            {
                page.Confirm();
                page.Capture(new CapturedInput(key, null));
            }
            Assert.Equal(new[] { KeyboardKey.F2, KeyboardKey.F1, KeyboardKey.Z }, page.Working[GameAction.Confirm].Keys);
        });
    }

    // ----------------------------------------------------------------------------- conflicts

    [Fact]
    public void AKeyThatIsAnothersAsksAndASwapGivesThemEachOthersKey()
    {
        WithBindings(new GameSettings(), () =>
        {
            // Mute takes M; Fullscreen is asked for Mute's M: swap
            var page = Page(new GameSettings(), GameAction.Fullscreen);
            page.Confirm();
            Assert.False(page.Capture(new CapturedInput(KeyboardKey.M, null)));
            Assert.Equal(ControlsPhase.Choosing, page.Phase);
            Assert.Equal(new[] { GameAction.Mute }, page.Pending!.Holders);
            Assert.Equal(new[] { KeyboardKey.F11 }, page.Working[GameAction.Fullscreen].Keys); // nothing yet

            Assert.True(page.Resolve(swap: true));
            Assert.Equal(new[] { KeyboardKey.M }, page.Working[GameAction.Fullscreen].Keys.Take(1));
            Assert.Equal(new[] { KeyboardKey.F11 }, page.Working[GameAction.Mute].Keys);
            Assert.Empty(page.Working.Conflicts());
        });
    }

    [Fact]
    public void RefusingAClashLeavesEverythingAsItWas()
    {
        WithBindings(new GameSettings(), () =>
        {
            var page = Page(new GameSettings(), GameAction.Confirm);
            page.Confirm();
            page.Capture(new CapturedInput(KeyboardKey.Enter, null)); // Menu's
            Assert.Equal(ControlsPhase.Choosing, page.Phase);
            Assert.False(page.Resolve(swap: false));
            Assert.Equal(ControlsPhase.Browsing, page.Phase);
            Assert.False(page.IsChanged(GameAction.Confirm));
            Assert.False(page.IsChanged(GameAction.Menu));

            // B in the question does the same
            page.Confirm();
            page.Capture(new CapturedInput(KeyboardKey.Enter, null));
            page.Cancel();
            Assert.Equal(ControlsPhase.Browsing, page.Phase);
            Assert.False(page.IsChanged(GameAction.Confirm));
        });
    }

    [Fact]
    public void ThePairsTheGameReadsInDifferentPlacesAreNotAsked()
    {
        WithBindings(new GameSettings(), () =>
        {
            // Run and Cancel both have X
            var page = Page(new GameSettings(), GameAction.Run);
            page.Confirm();
            Assert.True(page.Capture(new CapturedInput(KeyboardKey.X, null)));
            Assert.Equal(KeyboardKey.X, page.Working[GameAction.Run].Keys[0]);
            Assert.Contains(KeyboardKey.X, page.Working[GameAction.Cancel].Keys);
        });
    }

    [Fact]
    public void ASwapThatWouldLeaveTheGameWithNoWayToAnswerItIsRefused()
    {
        // Confirm has a button and no key; Menu has Enter alone and no button. Taking Enter for Confirm would strand Menu
        var settings = new GameSettings();
        settings.Bindings[nameof(GameAction.Confirm)] = new SavedBinding { Keys = Array.Empty<string>(), Pad = new[] { nameof(GamepadButton.RightFaceDown) } };
        settings.Bindings[nameof(GameAction.Menu)] = new SavedBinding { Keys = new[] { nameof(KeyboardKey.Enter) }, Pad = Array.Empty<string>() };
        WithBindings(settings, () =>
        {
            var page = Page(settings, GameAction.Confirm);
            page.Confirm();
            page.Capture(new CapturedInput(KeyboardKey.Enter, null));
            Assert.Equal(ControlsPhase.Choosing, page.Phase);
            Assert.False(page.Resolve(swap: true));
            Assert.Equal("controls.page.needOne", page.Notice);
            Assert.Equal(new[] { KeyboardKey.Enter }, page.Working[GameAction.Menu].Keys);
            Assert.Empty(page.Working[GameAction.Confirm].Keys);
        });
    }

    [Fact]
    public void AClashAlreadyInTheFileIsShownAgainstBothActions()
    {
        var settings = Rebound(GameAction.Mute, keys: new[] { KeyboardKey.F11 });
        WithBindings(settings, () =>
        {
            var page = Page(settings);
            Assert.Equal(new[] { GameAction.Fullscreen }, page.ClashesOf(GameAction.Mute));
            Assert.Equal(new[] { GameAction.Mute }, page.ClashesOf(GameAction.Fullscreen));
            Assert.Empty(page.ClashesOf(GameAction.Confirm));
        });
    }

    // ----------------------------------------------------------------------------- resetting

    [Fact]
    public void OneActionAndThenEveryActionGoBackToTheButtonModesOwn()
    {
        var settings = new GameSettings { ButtonMode = ButtonMode.StartIsX };
        WithBindings(settings, () =>
        {
            var page = Page(settings, GameAction.Item);
            page.MoveColumn(1);
            page.Confirm();
            page.Capture(new CapturedInput(null, GamepadButton.LeftTrigger2));
            page.Move(-1);
            page.MoveColumn(-1);
            page.Confirm();
            page.Capture(new CapturedInput(KeyboardKey.F5, null));
            Assert.True(page.IsChanged(GameAction.Item));
            Assert.True(page.IsChanged(GameAction.Run) || page.IsChanged(GameAction.Poketch) || page.IsChanged(GameAction.Menu));

            page.Move(1);
            Assert.True(page.ResetSelected());
            Assert.False(page.IsChanged(GameAction.Item));
            // The preset in force is the Button Mode's: Start is the registered item's here
            Assert.Equal(new[] { GamepadButton.MiddleRight }, page.Working[GameAction.Item].Pad);

            while (page.Selected != ControlsScreen.ResetRow) page.Move(1);
            page.Confirm();
            Assert.Equal(ControlsPhase.ResettingAll, page.Phase);
            page.Cancel();
            Assert.Equal(ControlsPhase.Browsing, page.Phase);
            page.Confirm();
            page.Confirm();
            Assert.All(ControlsScreen.Actions, a => Assert.False(page.IsChanged(a)));
            page.WriteTo(settings);
            Assert.Empty(settings.Bindings);
        });
    }

    // ----------------------------------------------------------------------------- the page in the options, by the buttons

    [Fact]
    public void TheOptionsOpenThePageAndItsBindingsGoIntoForce()
    {
        WithBindings(new GameSettings(), () =>
        {
            var settings = new GameSettings();
            var options = new OptionsScreen();
            options.Open();
            while (Enum.GetValues<OptionRow>()[options.SelectedIndex] != OptionRow.Controls) options.Move(1);
            options.Confirm(settings);
            Assert.True(options.Controls.IsActive);

            // Down to Screenshot is far: use the page's own cursor and the made-up frames of the buttons
            while (options.Controls.SelectedAction != GameAction.Mute) options.Controls.Move(1);
            bool changed = false;
            InputManager.Feed(ActionFrame.Press(GameAction.Confirm));
            changed |= options.Update(settings);
            Assert.Equal(ControlsPhase.Capturing, options.Controls.Phase);
            Assert.True(InputManager.CaptureRequested);

            // While waiting, the game's own buttons are not read: Cancel in the frame is no cancel
            InputManager.Feed(ActionFrame.Press(GameAction.Cancel));
            InputManager.Inject(new CapturedInput(KeyboardKey.K, null));
            changed |= options.Update(settings);
            InputManager.Inject(null);
            Assert.True(changed);
            Assert.Equal(new[] { KeyboardKey.K, KeyboardKey.M }, InputBindings.From(settings)[GameAction.Mute].Keys);

            // And the settings, once put in force, are what the game reads
            InputManager.Use(settings);
            Assert.Equal(KeyboardKey.K, InputManager.Active[GameAction.Mute].Keys[0]);

            InputManager.Feed(ActionFrame.Press(GameAction.Cancel));
            options.Update(settings);
            Assert.False(options.Controls.IsActive);
            Assert.True(options.IsActive);
        });
    }

    [Fact]
    public void APadCaptureGivesUpAfterTenSecondsOfFrames()
    {
        WithBindings(new GameSettings(), () =>
        {
            var settings = new GameSettings();
            var page = Page(settings, GameAction.Mute, ControlsColumn.Pad);
            page.Confirm();
            InputManager.Feed(ActionFrame.None);
            for (int i = 0; i < ControlsScreen.PadCaptureFrames; i++) Assert.False(page.Update(settings));
            Assert.Equal(ControlsPhase.Capturing, page.Phase);
            page.Update(settings);
            Assert.Equal(ControlsPhase.Browsing, page.Phase);
            Assert.False(InputManager.CaptureRequested);
        });
    }

    // ----------------------------------------------------------------------------- the page buttons

    [Fact]
    public void ThePageButtonsTurnTheBagsPocketsAndTheBoxes()
    {
        WithBindings(new GameSettings(), () =>
        {
            var inventory = new Inventory();
            var party = new Party();
            var bag = new BagScreen();
            bag.Open();
            var first = bag.CurrentPocket;
            InputManager.Feed(ActionFrame.Press(GameAction.PageRight));
            bag.Update(inventory, party, _ => { });
            Assert.NotEqual(first, bag.CurrentPocket);
            InputManager.Feed(ActionFrame.Press(GameAction.PageLeft));
            bag.Update(inventory, party, _ => { });
            Assert.Equal(first, bag.CurrentPocket);

            var pc = new PCScreen();
            var boxes = new PcBoxes();
            pc.Open(boxes);
            pc.Box = 0;
            InputManager.Feed(ActionFrame.Press(GameAction.PageRight));
            pc.Update(party, boxes, _ => { });
            Assert.Equal(1, pc.Box);
            InputManager.Feed(ActionFrame.Press(GameAction.PageLeft));
            pc.Update(party, boxes, _ => { });
            pc.Update(party, boxes, _ => { });
            Assert.Equal(pc.BoxCount - 1, pc.Box);
        });
    }

    [Fact]
    public void ThePageButtonsAreNoLongerWaitingForAReader()
    {
        Assert.DoesNotContain(GameAction.PageLeft, InputManager.NotReadYet);
        Assert.DoesNotContain(GameAction.PageRight, InputManager.NotReadYet);
    }
}
