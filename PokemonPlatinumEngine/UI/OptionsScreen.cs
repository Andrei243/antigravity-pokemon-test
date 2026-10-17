using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The settings the options screen offers, in the order of its rows. A new row goes at the end of the settings,
/// before <see cref="Help"/>, which stays the last: it is no setting but the way into the help pages.
/// </summary>
public enum OptionRow
{
    Language, TextSpeed, Quality, WindowSize, Fullscreen, VSync, TimeOfDay,
    Sound, MusicVolume, SoundVolume, CryVolume, AmbienceVolume, Speakers,
    MoveHints, TrainerLines,
    FocusPause, FrameCap,
    SkipScenes,
    BattleScene, BattleStyle,
    Help
}

/// <summary>
/// The options screen: the language (plan 17 · N1), text speed, graphics quality, window size, full screen, V-Sync, time of day, and the sound:
/// on or off, a volume for each of the mixer's buses (the music with its fanfares, the sound effects, the cries,
/// the ambience) and the speakers it plays through; then the helps of plan 12 (the move hints), the trainers' lines in battle (plan 11 · C12),
/// the two rows of plan 16 · T3 (whether the game waits behind another window, and the most frames it draws a second),
/// which scenes of the story may be skipped (plan 12 · Q20), Platinum's Battle Scene and Battle Style (plan 08 · P1) and the HELP pages
/// (<see cref="HelpScreen"/>), which open over the options. Changes apply at once; the caller saves the settings
/// when the screen reports a change. More rows than fit scroll in a window (<see cref="VisibleRows"/>).
/// </summary>
public class OptionsScreen
{
    private static readonly OptionRow[] Rows = Enum.GetValues<OptionRow>();

    /// <summary>The rows the screen shows at once, above the help panel.</summary>
    public const int VisibleRows = 9;

    public bool IsActive { get; private set; }
    public int SelectedIndex { get; set; }

    /// <summary>The first row in the window.</summary>
    public int FirstRow { get; private set; }

    /// <summary>The help pages, open over the options while <see cref="HelpScreen.IsActive"/>.</summary>
    public HelpScreen Help { get; } = new();

    public void Open()
    {
        IsActive = true;
        SelectedIndex = 0;
        FirstRow = 0;
        Help.Close();
    }

    /// <summary>One step of the cursor up or down the rows, wrapping round; the window follows it.</summary>
    public void Move(int dy)
    {
        SelectedIndex = (SelectedIndex + dy + Rows.Length) % Rows.Length;
        FirstRow = UiNav.Window(FirstRow, SelectedIndex, Rows.Length, VisibleRows);
        AudioManager.PlaySound("cursor");
    }

    /// <summary>Handles input; returns true when a setting changed and should be applied and saved.</summary>
    public bool Update(GameSettings settings)
    {
        if (!IsActive) return false;
        if (Help.IsActive)
        {
            Help.Update();
            return false;
        }

        if (InputManager.IsActionPressed(GameAction.Up)) Move(-1);
        else if (InputManager.IsActionPressed(GameAction.Down)) Move(1);
        else if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Menu))
        {
            IsActive = false;
            AudioManager.PlaySound("cancel");
        }
        else if (Rows[SelectedIndex] == OptionRow.Help)
        {
            if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        }
        else
        {
            int step = InputManager.IsActionPressed(GameAction.Left) ? -1
                : InputManager.IsActionPressed(GameAction.Right) || InputManager.IsActionPressed(GameAction.Confirm) ? 1 : 0;
            if (step != 0)
            {
                Change(settings, Rows[SelectedIndex], step);
                AudioManager.PlaySound("select");
                return true;
            }
        }
        return false;
    }

    /// <summary>The A button on the row the cursor is on: HELP opens the help pages; any other row is changed by Update.</summary>
    public void Confirm()
    {
        if (Rows[SelectedIndex] != OptionRow.Help) return;
        Help.Open();
        AudioManager.PlaySound("select");
    }

    /// <summary>Moves one setting to its next or previous value, wrapping round.</summary>
    public static void Change(GameSettings s, OptionRow row, int step)
    {
        switch (row)
        {
            case OptionRow.Language:
                // Only the languages a player can read with the faces the game has, and never the pseudo-language
                var offered = Core.Language.Offered;
                int at = offered.ToList().FindIndex(l => string.Equals(l.Tag, s.Language, StringComparison.OrdinalIgnoreCase));
                s.Language = offered[Wrap((at < 0 ? 0 : at) + step, offered.Count)].Tag;
                break;
            case OptionRow.TextSpeed:
                s.TextSpeed = (TextSpeed)Wrap((int)s.TextSpeed + step, 3);
                break;
            case OptionRow.Quality:
                s.Quality = (GraphicsQuality)Wrap((int)s.Quality + step, 3);
                break;
            case OptionRow.WindowSize:
                var sizes = GameSettings.WindowSizes;
                int current = Array.FindIndex(sizes, z => z.Width == s.WindowWidth && z.Height == s.WindowHeight);
                var next = sizes[Wrap((current < 0 ? 2 : current) + step, sizes.Length)];
                s.WindowWidth = next.Width;
                s.WindowHeight = next.Height;
                break;
            case OptionRow.Fullscreen:
                s.Fullscreen = !s.Fullscreen;
                break;
            case OptionRow.VSync:
                s.VSync = !s.VSync;
                break;
            case OptionRow.TimeOfDay:
                // Clock, then the five fixed times in the order of the day
                int index = s.TimeOfDay.HasValue ? (int)s.TimeOfDay.Value + 1 : 0;
                index = Wrap(index + step, 6);
                s.TimeOfDay = index == 0 ? null : (TimeOfDay)(index - 1);
                break;
            case OptionRow.Sound:
                s.Muted = !s.Muted;
                break;
            case OptionRow.MusicVolume:
                s.MusicVolume = Math.Clamp(s.MusicVolume + step * GameSettings.VolumeStep, 0, 100);
                break;
            case OptionRow.SoundVolume:
                s.SoundVolume = Math.Clamp(s.SoundVolume + step * GameSettings.VolumeStep, 0, 100);
                break;
            case OptionRow.CryVolume:
                s.CryVolume = Math.Clamp(s.CryVolume + step * GameSettings.VolumeStep, 0, 100);
                break;
            case OptionRow.AmbienceVolume:
                s.AmbienceVolume = Math.Clamp(s.AmbienceVolume + step * GameSettings.VolumeStep, 0, 100);
                break;
            case OptionRow.Speakers:
                // Stereo, the handheld's speakers, one channel (plan 08 · P1), and round again
                s.Speakers = (SpeakerMode)Wrap((int)s.Speakers + step, 3);
                break;
            case OptionRow.MoveHints:
                s.MoveHints = s.MoveHints == RulesDefault.Off ? RulesDefault.On : RulesDefault.Off;
                break;
            case OptionRow.TrainerLines:
                s.TrainerLines = s.TrainerLines == RulesDefault.Off ? RulesDefault.On : RulesDefault.Off;
                break;
            case OptionRow.FocusPause:
                s.FocusPause = !s.FocusPause;
                break;
            case OptionRow.FrameCap:
                // 30, 60, 120, none, and round again; a number the file was given by hand counts as 60
                var caps = GameSettings.FrameCaps;
                int cap = Array.IndexOf(caps, s.FrameCap);
                s.FrameCap = caps[Wrap((cap < 0 ? Array.IndexOf(caps, 60) : cap) + step, caps.Length)];
                break;
            case OptionRow.SkipScenes:
                s.SkipScenes = (SkipScenes)Wrap((int)s.SkipScenes + step, 3);
                break;
            case OptionRow.BattleScene:
                s.BattleScene = !s.BattleScene;
                break;
            case OptionRow.BattleStyle:
                s.BattleStyle = s.BattleStyle == BattleStyle.Shift ? BattleStyle.Set : BattleStyle.Shift;
                break;
        }
    }

    private static int Wrap(int value, int count) => ((value % count) + count) % count;

    private static (string Label, string Value, string Help) Describe(GameSettings s, OptionRow row) => row switch
    {
        OptionRow.Language => (Text.Of("options.language.label"), (Core.Language.Of(s.Language) ?? Text.Language).Name, Text.Of("options.language.help")),
        OptionRow.TextSpeed => (Text.Of("options.textSpeed.label"), Text.Of($"options.textSpeed.value.{s.TextSpeed}"), Text.Of("options.textSpeed.help")),
        OptionRow.Quality => (Text.Of("options.quality.label"), Text.Of($"options.quality.value.{s.Quality}"), Text.Of($"options.quality.help.{s.Quality}")),
        OptionRow.WindowSize => (Text.Of("options.windowSize.label"), Text.Of("options.windowSize.value", ("width", s.WindowWidth), ("height", s.WindowHeight)), Text.Of("options.windowSize.help")),
        OptionRow.Fullscreen => (Text.Of("options.fullscreen.label"), OnOff(s.Fullscreen), Text.Of("options.fullscreen.help")),
        OptionRow.VSync => (Text.Of("options.vsync.label"), OnOff(s.VSync), Text.Of("options.vsync.help")),
        OptionRow.TimeOfDay => (Text.Of("options.timeOfDay.label"), s.TimeOfDay is { } time ? Text.Of($"options.timeOfDay.value.{time}") : Text.Of("options.timeOfDay.value.Clock"),
            Text.Of(s.TimeOfDay.HasValue ? "options.timeOfDay.help.fixed" : "options.timeOfDay.help.clock")),
        OptionRow.Sound => (Text.Of("options.sound.label"), OnOff(!s.Muted), Text.Of("options.sound.help")),
        OptionRow.MusicVolume => (Text.Of("options.music.label"), Percent(s.MusicVolume), Text.Of("options.music.help")),
        OptionRow.SoundVolume => (Text.Of("options.soundEffects.label"), Percent(s.SoundVolume), Text.Of("options.soundEffects.help")),
        OptionRow.CryVolume => (Text.Of("options.cries.label"), Percent(s.CryVolume), Text.Of("options.cries.help")),
        OptionRow.AmbienceVolume => (Text.Of("options.ambience.label"), Percent(s.AmbienceVolume), Text.Of("options.ambience.help")),
        OptionRow.Speakers => (Text.Of("options.speakers.label"), Text.Of($"options.speakers.value.{s.Speakers}"), Text.Of($"options.speakers.help.{s.Speakers}")),
        OptionRow.MoveHints => (Text.Of("options.moveHints.label"), OnOff(s.MoveHints != RulesDefault.Off), Text.Of("options.moveHints.help")),
        OptionRow.TrainerLines => (Text.Of("options.trainerLines.label"), OnOff(s.TrainerLines != RulesDefault.Off), Text.Of("options.trainerLines.help")),
        OptionRow.FocusPause => (Text.Of("options.focusPause.label"), OnOff(s.FocusPause), Text.Of(s.FocusPause ? "options.focusPause.help.on" : "options.focusPause.help.off")),
        OptionRow.FrameCap => (Text.Of("options.frameCap.label"),
            s.FrameCap > 0 ? Text.Of("options.frameCap.value", ("n", s.FrameCap)) : Text.Of("options.frameCap.none"),
            Text.Of(s.FrameCap > 0 ? "options.frameCap.help.capped" : "options.frameCap.help.none")),
        OptionRow.SkipScenes => (Text.Of("options.skipScenes.label"), Text.Of($"options.skipScenes.value.{s.SkipScenes}"), Text.Of($"options.skipScenes.help.{s.SkipScenes}")),
        OptionRow.BattleScene => (Text.Of("options.battleScene.label"), OnOff(s.BattleScene), Text.Of(s.BattleScene ? "options.battleScene.help.on" : "options.battleScene.help.off")),
        OptionRow.BattleStyle => (Text.Of("options.battleStyle.label"), Text.Of($"options.battleStyle.value.{s.BattleStyle}"), Text.Of($"options.battleStyle.help.{s.BattleStyle}")),
        _ => (Text.Of("options.help.label"), Text.Of("options.help.value"), Text.Of("options.help.help"))
    };

    private static string OnOff(bool on) => Text.Of(on ? "shared.on" : "shared.off");

    private static string Percent(int volume) => Text.Of("shared.percent", ("n", volume));

    public void Draw(int sw, int sh, GameSettings settings)
    {
        if (!IsActive) return;
        if (Help.IsActive)
        {
            Help.Draw(sw, sh);
            return;
        }
        ModernUi.Backdrop(sw, sh);
        ModernUi.ScreenTitle(Text.Of("options.title"));
        if (Rows[SelectedIndex] == OptionRow.Help) ModernUi.Hints(sw - 64, 44, (Text.Of("shared.keys.z"), Text.Of("shared.hints.open")), (Text.Of("shared.keys.esc"), Text.Of("shared.hints.back")));
        else ModernUi.Hints(sw - 64, 44, (Text.Of("shared.keys.leftRight"), Text.Of("shared.hints.change")), (Text.Of("shared.keys.esc"), Text.Of("shared.hints.back")));

        const float pitch = 86, height = 78;
        bool scrolls = Rows.Length > VisibleRows;
        float width = sw - 128 - (scrolls ? 36 : 0);
        for (int k = 0; k < Math.Min(VisibleRows, Rows.Length); k++)
        {
            int i = FirstRow + k;
            var (label, value, _) = Describe(settings, Rows[i]);
            ModernUi.ValueRow(new Rectangle(64, ModernUi.ContentTop + k * pitch, width, height), label, value, i == SelectedIndex);
        }
        if (scrolls)
        {
            // Where the window is among all the rows: a thin track with a thumb at the right
            var track = new Rectangle(sw - 64 - 12, ModernUi.ContentTop, 12, VisibleRows * pitch - (pitch - height));
            UiShapes.Fill(track, 6, ModernUi.Rule);
            float size = track.Height * VisibleRows / Rows.Length;
            float at = (track.Height - size) * FirstRow / (Rows.Length - VisibleRows);
            UiShapes.Fill(new Rectangle(track.X, track.Y + at, track.Width, size), 6, ModernUi.Frame);
        }

        float top = ModernUi.ContentTop + Math.Min(VisibleRows, Rows.Length) * pitch + 6;
        var help = new Rectangle(64, top, sw - 128, ModernUi.ContentBottom - top);
        ModernUi.Panel(help, 30);
        ModernUi.DrawWrapped(Describe(settings, Rows[SelectedIndex]).Help, help.X + 52, help.Y + (help.Height - 30) / 2f - 2, help.Width - 104, 30, ModernUi.Ink, 40);
    }
}
