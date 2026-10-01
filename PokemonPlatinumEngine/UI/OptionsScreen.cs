using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The options screen: graphics quality, window size, full screen, V-Sync, time of day and sound. Changes apply
/// at once; the caller saves the settings when the screen reports a change.
/// </summary>
public class OptionsScreen
{
    private enum Row { Quality, WindowSize, Fullscreen, VSync, TimeOfDay, Sound }

    private static readonly Row[] Rows = Enum.GetValues<Row>();

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
    internal static void Change(GameSettings s, int rowIndex, int step) => Change(s, Rows[rowIndex], step);

    private static void Change(GameSettings s, Row row, int step)
    {
        switch (row)
        {
            case Row.Quality:
                s.Quality = (GraphicsQuality)Wrap((int)s.Quality + step, 3);
                break;
            case Row.WindowSize:
                var sizes = GameSettings.WindowSizes;
                int current = Array.FindIndex(sizes, z => z.Width == s.WindowWidth && z.Height == s.WindowHeight);
                var next = sizes[Wrap((current < 0 ? 2 : current) + step, sizes.Length)];
                s.WindowWidth = next.Width;
                s.WindowHeight = next.Height;
                break;
            case Row.Fullscreen:
                s.Fullscreen = !s.Fullscreen;
                break;
            case Row.VSync:
                s.VSync = !s.VSync;
                break;
            case Row.TimeOfDay:
                // Clock, then the five fixed times in the order of the day
                int index = s.TimeOfDay.HasValue ? (int)s.TimeOfDay.Value + 1 : 0;
                index = Wrap(index + step, 6);
                s.TimeOfDay = index == 0 ? null : (TimeOfDay)(index - 1);
                break;
            case Row.Sound:
                s.Muted = !s.Muted;
                break;
        }
    }

    private static int Wrap(int value, int count) => ((value % count) + count) % count;

    private static (string Label, string Value, string Help) Describe(GameSettings s, Row row) => row switch
    {
        Row.Quality => ("Graphics quality", s.Quality.ToString(), s.Quality switch
        {
            GraphicsQuality.High => "Sharpest picture: renders at double resolution with soft shadows and ambient occlusion.",
            GraphicsQuality.Medium => "Renders at screen resolution with anti-aliasing; keeps soft shadows and ambient occlusion.",
            _ => "Fastest: screen resolution, simple shadows, no ambient occlusion or depth of field."
        }),
        Row.WindowSize => ("Window size", $"{s.WindowWidth} × {s.WindowHeight}", "Size of the game window when it isn't full screen."),
        Row.Fullscreen => ("Full screen", s.Fullscreen ? "On" : "Off", "Fills the whole display (also F11)."),
        Row.VSync => ("V-Sync", s.VSync ? "On" : "Off", "Matches the display's refresh rate to avoid tearing."),
        Row.TimeOfDay => ("Time of day", s.TimeOfDay switch
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
        UiFonts.Draw("OPTIONS", 64, 36, 52, Color.White, UiWeight.Black);
        ModernUi.HintPill(1370, 44, "Left / Right", "Change");
        ModernUi.HintPill(1710, 44, "Esc", "Back");

        for (int i = 0; i < Rows.Length; i++)
        {
            var (label, value, _) = Describe(settings, Rows[i]);
            var r = new Rectangle(64, 136 + i * 124, sw - 128, 108);
            bool selected = i == SelectedIndex;
            var accent = new Color(240, 104, 70, 255);
            if (selected) UiShapes.Shadow(r, 30, 30, Vector2.Zero, accent with { A = 170 });
            else UiShapes.Shadow(r, 30, 20, new Vector2(0, 8), new Color(6, 14, 34, 100));
            UiShapes.Shape(r, 30, ModernUi.PanelTop, ModernUi.PanelBottom, selected ? accent : ModernUi.Frame, selected ? 6f : 4f);
            UiFonts.DrawCentered(label, r.X + 52, r.Y + r.Height / 2f, 38, ModernUi.Ink, UiWeight.ExtraBold);

            // The value sits in a pill between two arrows
            var pill = new Rectangle(r.X + r.Width - 620, r.Y + 22, 480, r.Height - 44);
            UiShapes.Fill(pill, pill.Height / 2f, selected ? ModernUi.Frame : new Color(214, 222, 236, 255));
            float vw = UiFonts.Measure(value, 32, UiWeight.Black);
            UiFonts.DrawCentered(value, pill.X + (pill.Width - vw) / 2f, pill.Y + pill.Height / 2f, 32,
                selected ? Color.White : ModernUi.Ink, UiWeight.Black);
            var arrow = selected ? accent : new Color(170, 180, 200, 255);
            float cy = r.Y + r.Height / 2f;
            Arrow(pill.X - 44, cy, -1, arrow);
            Arrow(pill.X + pill.Width + 44, cy, 1, arrow);
        }

        var help = new Rectangle(64, 136 + Rows.Length * 124 + 12, sw - 128, 104);
        ModernUi.Panel(help, 30);
        ModernUi.DrawWrapped(Describe(settings, Rows[SelectedIndex]).Help, help.X + 52, help.Y + 32, help.Width - 104, 30, ModernUi.Ink, 40);
    }

    private static void Arrow(float x, float cy, int direction, Color color)
    {
        var tip = new Vector2(x + 14 * direction, cy);
        var a = new Vector2(x - 10 * direction, cy - 18);
        var b = new Vector2(x - 10 * direction, cy + 18);
        // Both windings, so it shows whichever way the batch culls
        Raylib.DrawTriangle(tip, a, b, color);
        Raylib.DrawTriangle(tip, b, a, color);
    }
}
