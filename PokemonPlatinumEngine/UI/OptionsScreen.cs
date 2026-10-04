using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The settings the options screen offers, in the order of its rows.</summary>
public enum OptionRow { TextSpeed, Quality, WindowSize, Fullscreen, VSync, TimeOfDay, Sound }

/// <summary>
/// The options screen: text speed, graphics quality, window size, full screen, V-Sync, time of day and sound.
/// Changes apply at once; the caller saves the settings when the screen reports a change. The volumes of music
/// and effects come with the mixer's buses (plan 05).
/// </summary>
public class OptionsScreen
{
    private static readonly OptionRow[] Rows = Enum.GetValues<OptionRow>();

    public bool IsActive { get; private set; }
    public int SelectedIndex { get; set; }

    public void Open()
    {
        IsActive = true;
        SelectedIndex = 0;
    }

    /// <summary>Handles input; returns true when a setting changed and should be applied and saved.</summary>
    public bool Update(GameSettings settings)
    {
        if (!IsActive) return false;

        if (InputManager.IsActionPressed(GameAction.Up))
        {
            SelectedIndex = (SelectedIndex - 1 + Rows.Length) % Rows.Length;
            AudioManager.PlaySound("cursor");
        }
        else if (InputManager.IsActionPressed(GameAction.Down))
        {
            SelectedIndex = (SelectedIndex + 1) % Rows.Length;
            AudioManager.PlaySound("cursor");
        }
        else if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Menu))
        {
            IsActive = false;
            AudioManager.PlaySound("cancel");
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
        _ => ("Sound", s.Muted ? "Off" : "On", "Music and sound effects (also the M key).")
    };

    public void Draw(int sw, int sh, GameSettings settings)
    {
        if (!IsActive) return;
        ModernUi.Backdrop(sw, sh);
        ModernUi.ScreenTitle("OPTIONS");
        ModernUi.Hints(sw - 64, 44, ("Left / Right", "Change"), ("Esc", "Back"));

        const float pitch = 110, height = 98;
        for (int i = 0; i < Rows.Length; i++)
        {
            var (label, value, _) = Describe(settings, Rows[i]);
            ModernUi.ValueRow(new Rectangle(64, ModernUi.ContentTop + i * pitch, sw - 128, height), label, value, i == SelectedIndex);
        }

        float top = ModernUi.ContentTop + Rows.Length * pitch + 6;
        var help = new Rectangle(64, top, sw - 128, ModernUi.ContentBottom - top);
        ModernUi.Panel(help, 30);
        ModernUi.DrawWrapped(Describe(settings, Rows[SelectedIndex]).Help, help.X + 52, help.Y + (help.Height - 30) / 2f - 2, help.Width - 104, 30, ModernUi.Ink, 40);
    }
}
