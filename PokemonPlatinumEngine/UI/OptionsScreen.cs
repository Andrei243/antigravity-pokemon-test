using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The settings the options screen offers, in the order of its rows.</summary>
public enum OptionRow
{
    TextSpeed, Quality, WindowSize, Fullscreen, VSync, TimeOfDay,
    Sound, MusicVolume, SoundVolume, CryVolume, AmbienceVolume, Speakers,
    MoveHints, Help
}

/// <summary>
/// The options screen: text speed, graphics quality, window size, full screen, V-Sync, time of day, and the sound:
/// on or off, a volume for each of the mixer's buses (the music with its fanfares, the sound effects, the cries,
/// the ambience) and the speakers it plays through; then the helps of plan 12 (the move hints) and the HELP pages
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
                s.Speakers = s.Speakers == SpeakerMode.Stereo ? SpeakerMode.Handheld : SpeakerMode.Stereo;
                break;
            case OptionRow.MoveHints:
                s.MoveHints = (RulesDefault)Wrap((int)s.MoveHints + step, 3);
                break;
        }
    }

    private static int Wrap(int value, int count) => ((value % count) + count) % count;

    private static (string Label, string Value, string Help) Describe(GameSettings s, OptionRow row) => row switch
    {
        OptionRow.TextSpeed => ("Text speed", s.TextSpeed.ToString(), "How fast what people say is written out. The A button always finishes a line at once."),
        OptionRow.Quality => ("Graphics quality", s.Quality.ToString(), s.Quality switch
        {
            GraphicsQuality.High => "Sharpest picture: the 3D scenes render at 4K (3840 × 2160) with soft shadows and ambient occlusion.",
            GraphicsQuality.Medium => "3D scenes render at 2880 × 1620 with anti-aliasing; keeps soft shadows and ambient occlusion.",
            _ => "Fastest: 3D scenes render at 1920 × 1080 with simple shadows, no ambient occlusion or depth of field."
        }),
        OptionRow.WindowSize => ("Window size", $"{s.WindowWidth} × {s.WindowHeight}", "Size of the game window when it isn't full screen."),
        OptionRow.Fullscreen => ("Full screen", s.Fullscreen ? "On" : "Off", "Fills the whole display (also F11)."),
        OptionRow.VSync => ("V-Sync", s.VSync ? "On" : "Off", "Matches the display's refresh rate to avoid tearing."),
        OptionRow.TimeOfDay => ("Time of day", s.TimeOfDay switch
        {
            null => "Clock",
            TimeOfDay.LateNight => "Late night",
            var t => t.ToString()!
        }, s.TimeOfDay.HasValue
            ? "The world stays at this time of day."
            : "Follows your computer's clock, like the original: morning from 4, day from 10, twilight from 17, night from 20."),
        OptionRow.Sound => ("Sound", s.Muted ? "Off" : "On", "Music and sound effects (also the M key)."),
        OptionRow.MusicVolume => ("Music", $"{s.MusicVolume}%", "How loud the music and the fanfares play."),
        OptionRow.SoundVolume => ("Sound effects", $"{s.SoundVolume}%", "How loud the sound effects play: menus, doors, footsteps, the moves in battle."),
        OptionRow.CryVolume => ("Cries", $"{s.CryVolume}%", "How loud the Pokémon's cries play."),
        OptionRow.AmbienceVolume => ("Ambience", $"{s.AmbienceVolume}%", "How loud the field's background sounds play: rain, wind, waterfalls, the sea, caves."),
        OptionRow.Speakers => ("Speakers", s.Speakers == SpeakerMode.Handheld ? "Handheld" : "Stereo", s.Speakers == SpeakerMode.Handheld
            ? "Sounds as the handheld's own small speakers would: no deep bass, a narrow stereo and ten-bit sound."
            : "Full sound, for speakers or headphones."),
        OptionRow.MoveHints => ("Move hints", Value(s.MoveHints), "In battle, a move's card says whether it is super effective, not very effective or has no effect on a Pokémon you have seen before. "
            + RulesLine(s.MoveHints, "shows them", "goes without")),
        _ => ("Help", "Open", "The type chart, the controls and a few notes for the road.")
    };

    /// <summary>A three-way row's value (style guide, "Options").</summary>
    private static string Value(RulesDefault value) => value switch
    {
        RulesDefault.Off => "Off",
        RulesDefault.On => "On",
        _ => "Rules"
    };

    /// <summary>What Rules gives in the game in progress, for a three-way row's help line.</summary>
    private static string RulesLine(RulesDefault value, string modern, string platinum) => value switch
    {
        RulesDefault.Rules => Data.Ruleset.Current.Preset == Data.RulesPreset.Modern
            ? $"Rules: this game is played by the modern rules, so it {modern}."
            : $"Rules: this game is played by Platinum's rules, so it {platinum}.",
        _ => "Rules follows the rules the game was begun under."
    };

    public void Draw(int sw, int sh, GameSettings settings)
    {
        if (!IsActive) return;
        if (Help.IsActive)
        {
            Help.Draw(sw, sh);
            return;
        }
        ModernUi.Backdrop(sw, sh);
        ModernUi.ScreenTitle("OPTIONS");
        if (Rows[SelectedIndex] == OptionRow.Help) ModernUi.Hints(sw - 64, 44, ("Z", "Open"), ("Esc", "Back"));
        else ModernUi.Hints(sw - 64, 44, ("Left / Right", "Change"), ("Esc", "Back"));

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
