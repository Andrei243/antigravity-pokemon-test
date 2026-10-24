using System;
using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// The two rows plan 16 · T3 added to the options: whether the game waits behind another window, and the most
/// frames it draws a second. Both default to what the game always did: it runs on, at sixty.
/// </summary>
public class FocusAndFrameCapTests
{
    private static readonly OptionRow[] Rows = Enum.GetValues<OptionRow>();

    [Fact]
    public void TheDefaultsAreTheGameAsItAlwaysRan()
    {
        var settings = new GameSettings();
        Assert.False(settings.FocusPause);
        Assert.Equal(60, settings.FrameCap);
        // Sixty is what Program.Main always asked of raylib
        Assert.Equal(60, WindowSettings.TargetFps(settings.FrameCap));

        // A settings file from before the rows reads as the same
        var json = new JsonSerializerOptions { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
        var old = JsonSerializer.Deserialize<GameSettings>("{ \"Quality\": \"Medium\", \"VSync\": false }", json)!;
        Assert.Equal((false, 60, GraphicsQuality.Medium), (old.FocusPause, old.FrameCap, old.Quality));
    }

    [Fact]
    public void TheRowsStandAtTheEndOfTheSettingsBeforeHelp()
    {
        // (The rows after them are plan 12 · Q20's, plan 08 · P1's, plan 12 · Q4's and plan 08 · P10's and P23's, plan 12 · Q14's and plan 22 · U3's, which went in before HELP as every new row does)
        Assert.Equal(new[] { OptionRow.FocusPause, OptionRow.FrameCap, OptionRow.SkipScenes, OptionRow.BattleScene, OptionRow.BattleStyle }, Rows[^20..^15]);
        // The screen still shows nine at a time and scrolls to the rest
        Assert.Equal(9, OptionsScreen.VisibleRows);
        var screen = new OptionsScreen();
        screen.Open();
        foreach (var row in new[] { OptionRow.FocusPause, OptionRow.FrameCap, OptionRow.SkipScenes, OptionRow.BattleScene, OptionRow.BattleStyle })
        {
            while (Rows[screen.SelectedIndex] != row) screen.Move(1);
            Assert.InRange(screen.SelectedIndex, screen.FirstRow, screen.FirstRow + OptionsScreen.VisibleRows - 1);
        }
        while (Rows[screen.SelectedIndex] != OptionRow.Help) screen.Move(1);
        // On HELP the window is at its end
        Assert.Equal(Rows.Length - OptionsScreen.VisibleRows, screen.FirstRow);
    }

    [Fact]
    public void PauseInTheBackgroundGoesOnAndOff()
    {
        var settings = new GameSettings();
        OptionsScreen.Change(settings, OptionRow.FocusPause, 1);
        Assert.True(settings.FocusPause);
        OptionsScreen.Change(settings, OptionRow.FocusPause, -1);
        Assert.False(settings.FocusPause);
    }

    [Fact]
    public void TheFrameLimitGoesRoundItsFourValues()
    {
        Assert.Equal(new[] { 30, 60, 120, 0 }, GameSettings.FrameCaps);
        var settings = new GameSettings();
        var seen = new int[5];
        for (int i = 0; i < seen.Length; i++)
        {
            OptionsScreen.Change(settings, OptionRow.FrameCap, 1);
            seen[i] = settings.FrameCap;
        }
        Assert.Equal(new[] { 120, 0, 30, 60, 120 }, seen);
        OptionsScreen.Change(settings, OptionRow.FrameCap, -1);
        Assert.Equal(60, settings.FrameCap);
        OptionsScreen.Change(settings, OptionRow.FrameCap, -1);
        Assert.Equal(30, settings.FrameCap);
        OptionsScreen.Change(settings, OptionRow.FrameCap, -1);
        Assert.Equal(0, settings.FrameCap);

        // A number written into the file by hand counts as sixty when the row is next changed
        settings.FrameCap = 45;
        OptionsScreen.Change(settings, OptionRow.FrameCap, 1);
        Assert.Equal(120, settings.FrameCap);
    }

    [Theory]
    [InlineData(30, 30)]
    [InlineData(60, 60)]
    [InlineData(120, 120)]
    [InlineData(0, 0)]      // no limit
    [InlineData(-5, 0)]     // nonsense is no limit either, never a negative rate
    public void RaylibIsAskedForTheLimitAndForNoneWhereThereIsNone(int frameCap, int target)
    {
        Assert.Equal(target, WindowSettings.TargetFps(frameCap));
    }

    // ------------------------------------------------------------------ waiting behind another window

    [Fact]
    public void TheGameWaitsOnlyWithTheOptionOnAndItsWindowNotInFront()
    {
        foreach (var state in Enum.GetValues<GameState>())
        {
            // Off, which it is unless chosen, nothing ever waits: in front or not
            Assert.False(GameEngine.WaitsForFocus(pauseInBackground: false, windowFocused: false, state));
            Assert.False(GameEngine.WaitsForFocus(pauseInBackground: false, windowFocused: true, state));
            // On, the game goes on while its window is in front
            Assert.False(GameEngine.WaitsForFocus(pauseInBackground: true, windowFocused: true, state));
            // ... and waits while it isn't, but for the close button's question, which is never kept waiting
            Assert.Equal(state != GameState.WindowClosing, GameEngine.WaitsForFocus(pauseInBackground: true, windowFocused: false, state));
        }
    }

    private static float[] Render(AudioMixer mixer, double seconds)
    {
        var buffer = new float[(int)(seconds * Synthesizer.SampleRate) * 2];
        for (int done = 0; done < buffer.Length; done += 2048)
            mixer.Render(buffer.AsSpan(done, Math.Min(2048, buffer.Length - done)));
        return buffer;
    }

    [Fact]
    public void BehindAnotherWindowEverythingPlaysAtAQuarterAndComesBack()
    {
        var song = MusicLibrary.Get("sinnoh/twinleaf")!;
        var heard = new AudioMixer();
        heard.Play(song, night: false);
        var away = new AudioMixer();
        away.Play(song, night: false);
        Assert.False(away.Away);
        Assert.Equal(1f, away.AwayGain);

        // A second in front: the same samples, bit for bit, as a mixer that knows nothing of it
        Assert.Equal(Render(heard, 1.0), Render(away, 1.0));

        // The window loses the focus: the sound glides down, and after a moment plays at a quarter
        away.Away = true;
        var falling = Render(away, 0.5);
        var reference = Render(heard, 0.5);
        Assert.Equal(AudioMixer.AwayLevel, away.AwayGain);
        Assert.Equal(0.25f, AudioMixer.AwayLevel);
        // (no click as it begins: the first samples are still as loud as they were)
        Assert.True(MathF.Abs(falling[2] - reference[2]) < 1e-3f);
        var quiet = Render(away, 1.0);
        var loud = Render(heard, 1.0);
        Assert.Contains(loud, x => MathF.Abs(x) > 0.02f);
        // (under the limiter's knee, where a sample is itself: the quiet one is never near it)
        for (int i = 0; i < loud.Length; i++)
            if (MathF.Abs(loud[i]) < 0.75f) Assert.True(MathF.Abs(quiet[i] - loud[i] * AudioMixer.AwayLevel) < 1e-5f, $"sample {i}: {quiet[i]} against {loud[i]}");

        // The player comes back: it glides up again and is what it always was
        away.Away = false;
        Render(away, 0.5);
        Render(heard, 0.5);
        Assert.Equal(1f, away.AwayGain);
        Assert.Equal(Render(heard, 0.5), Render(away, 0.5));
    }

    [Fact]
    public void TheSoundManagerHandsTheWaitToTheMixer()
    {
        Assert.False(AudioManager.Away);
        try
        {
            AudioManager.Away = true;
            Assert.True(AudioManager.Away);
        }
        finally
        {
            AudioManager.Away = false;
        }
    }
}

/// <summary>
/// Safe mode reads no hand-made file (plan 16 · T3). With the other tests that change where model files are looked
/// for, which run alone: every model built meanwhile would be built without its override.
/// </summary>
[Collection("Model files")]
public class OverridesSwitchTests
{
    [Fact]
    public void WithOverridesOffNoModelFileIsFound()
    {
        string folder = System.IO.Directory.CreateTempSubdirectory("pokemon-overrides-").FullName;
        var saved = ModelOverrides.Folders.ToList();
        try
        {
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, "Bidoof.glb"), new byte[] { 1, 2, 3 });
            ModelOverrides.Folders.Clear();
            ModelOverrides.Folders.Add(folder);
            ModelOverrides.Refresh();
            Assert.True(Overrides.Enabled);
            Assert.NotNull(ModelOverrides.Find("Bidoof"));

            Overrides.Enabled = false;
            Assert.Null(ModelOverrides.Find("Bidoof"));
        }
        finally
        {
            Overrides.Enabled = true;
            ModelOverrides.Folders.Clear();
            ModelOverrides.Folders.AddRange(saved);
            ModelOverrides.Refresh();
            System.IO.Directory.Delete(folder, recursive: true);
        }
    }
}
