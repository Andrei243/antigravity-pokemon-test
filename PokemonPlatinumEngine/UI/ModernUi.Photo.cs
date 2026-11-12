using System;
using System.Globalization;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

// Photo mode in the field (plan 12 · Q16; style guide, "Field menus and notices", "Photo mode")
internal static partial class ModernUi
{
    /// <summary>The panel's place and size, and a row's height.</summary>
    public const float PhotoPanelX = 32, PhotoPanelY = 32, PhotoPanelWidth = 440, PhotoRowHeight = 56;

    /// <summary>The mat's width round the picture and its colours.</summary>
    public const float PhotoMatWidth = 48;

    private static readonly Color MatPaper = new(246, 242, 232, 255);
    private static readonly Color MatRule = new(200, 192, 176, 255);

    /// <summary>The Mat frame: a border round the picture with a hair rule inside it.</summary>
    public static void PhotoMat(int sw, int sh)
    {
        float m = PhotoMatWidth;
        Raylib.DrawRectangleRec(new Rectangle(0, 0, sw, m), MatPaper);
        Raylib.DrawRectangleRec(new Rectangle(0, sh - m, sw, m), MatPaper);
        Raylib.DrawRectangleRec(new Rectangle(0, m, m, sh - 2 * m), MatPaper);
        Raylib.DrawRectangleRec(new Rectangle(sw - m, m, m, sh - 2 * m), MatPaper);
        float r = m - 12;
        Raylib.DrawRectangleLinesEx(new Rectangle(r, r, sw - 2 * r, sh - 2 * r), 2, MatRule);
    }

    /// <summary>A row's value as the panel writes it.</summary>
    public static string PhotoValue(PhotoView p, PhotoRow row)
    {
        switch (row)
        {
            case PhotoRow.Zoom:
                string zoom = p.Zoom.ToString("0.00", CultureInfo.InvariantCulture);
                return Text.Of("photo.zoom", ("zoom", zoom));
            case PhotoRow.Time:
                if (p.Time is not { } time) return Text.Of("photo.time.now");
                return Text.Of($"photo.time.{time}");
            case PhotoRow.Weather:
                if (!p.WeatherAllowed) return Text.Of("photo.notHere");
                var weather = p.BorrowedWeather;
                return Text.Of($"photo.weather.{weather}");
            case PhotoRow.Filter:
                var filter = p.Filter;
                return Text.Of($"photo.filter.{filter}");
            // The player's pose and face (plan 43 · LF4)
            case PhotoRow.Pose:
                if (p.Pose is not { } pose) return Text.Of("photo.pose.none");
                return Text.Of($"cardDesign.pose.{pose}");
            case PhotoRow.Face:
                var face = p.Face;
                return Text.Of($"cardDesign.face.{face}");
            default:
                var frame = p.Frame;
                return Text.Of($"photo.frame.{frame}");
        }
    }

    /// <summary>
    /// Photo mode's panel at the top left: open, a row a value with the chosen row a Selection pill; shut, the caption
    /// over the five values in a column.
    /// </summary>
    public static void PhotoPanel(PhotoView p)
    {
        var rows = p.Rows;
        var names = new string[rows.Length];
        var values = new string[rows.Length];
        var here = new bool[rows.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            var rowName = rows[i];
            names[i] = Text.Of($"photo.row.{rowName}");
            values[i] = PhotoValue(p, rows[i]);
            here[i] = rows[i] != PhotoRow.Weather || p.WeatherAllowed;
        }
        PhotoRows(names, values, here, p.PanelOpen, Array.IndexOf(rows, p.Row));
    }

    /// <summary>The panel's rows, the field's and the battle's (plan 12 · Q17): names, values, whether each can change here.</summary>
    private static void PhotoRows(string[] names, string[] values, bool[] hereAt, bool open, int chosenRow)
    {
        int count = names.Length;
        float height = open ? 56 + count * PhotoRowHeight + 16 : 56 + count * 28 + 12;
        var panel = new Rectangle(PhotoPanelX, PhotoPanelY, PhotoPanelWidth, height);
        Panel(panel, 24);
        float x = panel.X + 24, right = panel.X + panel.Width - 24;
        Label(Text.Of("photo.title"), x, panel.Y + 20);
        float y = panel.Y + 56;
        for (int row = 0; row < count; row++)
        {
            string value = values[row];
            if (!open)
            {
                UiFonts.Draw(value, x, y, 20, Muted, UiWeight.Bold);
                y += 28;
                continue;
            }
            bool chosen = row == chosenRow;
            var pill = new Rectangle(panel.X + 12, y + 4, panel.Width - 24, PhotoRowHeight - 8);
            if (chosen) UiShapes.Fill(pill, 20, Selection);
            var ink = chosen ? Color.White : Ink;
            float middle = y + PhotoRowHeight / 2f;
            UiFonts.Draw(names[row], x, middle - 14, 24, ink, UiWeight.Bold);
            bool here = hereAt[row];
            var valueInk = chosen ? Color.White : here ? Ink : Muted;
            float w = UiFonts.Measure(value, 24, UiWeight.ExtraBold);
            float valueX = right - 22 - w;
            UiFonts.Draw(value, valueX, middle - 14, 24, valueInk, UiWeight.ExtraBold);
            var arrow = chosen ? Color.White : Muted;
            // The two triangles either side of the value
            UiShapes.Triangle(new Vector2(valueX - 8, middle - 9), new Vector2(valueX - 18, middle), new Vector2(valueX - 8, middle + 9), arrow);
            UiShapes.Triangle(new Vector2(right - 10, middle - 9), new Vector2(right - 10, middle + 9), new Vector2(right, middle), arrow);
            y += PhotoRowHeight;
        }
    }

    /// <summary>The hint at the bottom: what the buttons do, the keys as the prompts name them.</summary>
    public static void PhotoHint(int sw, int sh, bool panelOpen)
    {
        string line = Text.Of(panelOpen ? "photo.hint.panel" : "photo.hint.compose",
            ("out", Prompts.NameOf(GameAction.PageLeft)), ("in", Prompts.NameOf(GameAction.PageRight)));
        float w = UiFonts.Measure(line, 22, UiWeight.Bold) + 56;
        var pill = new Rectangle((sw - w) / 2f, sh - 32 - 52, w, 52);
        UiShapes.Fill(pill, 26, new Color(16, 20, 32, 200));
        UiFonts.Draw(line, pill.X + 28, pill.Y + 13, 22, Color.White, UiWeight.Bold);
    }

    // ---------------------------------------------------------------- photo mode in battle (plan 12 · Q17)

    /// <summary>A row of the battle's panel as the panel writes it.</summary>
    public static string BattlePhotoValue(BattlePhotoView p, BattlePhotoRow row)
    {
        switch (row)
        {
            case BattlePhotoRow.Zoom:
                string zoom = p.Zoom.ToString("0.00", CultureInfo.InvariantCulture);
                return Text.Of("photo.zoom", ("zoom", zoom));
            case BattlePhotoRow.Time:
                if (p.Time is not { } time) return Text.Of("photo.time.now");
                return Text.Of($"photo.time.{time}");
            case BattlePhotoRow.Pose:
                if (p.Pose == 0) return Text.Of("photo.pose.still");
                string seconds = p.PoseStep.ToString("0.0", CultureInfo.InvariantCulture);
                return Text.Of("photo.pose.step", ("seconds", seconds));
            case BattlePhotoRow.Filter:
                var filter = p.Filter;
                return Text.Of($"photo.filter.{filter}");
            default:
                var frame = p.Frame;
                return Text.Of($"photo.frame.{frame}");
        }
    }

    /// <summary>The battle's panel: the field's, with POSE in place of WEATHER.</summary>
    public static void BattlePhotoPanel(BattlePhotoView p)
    {
        var rows = Enum.GetValues<BattlePhotoRow>();
        var names = new string[rows.Length];
        var values = new string[rows.Length];
        var here = new bool[rows.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            var rowName = rows[i];
            names[i] = Text.Of($"photo.row.{rowName}");
            values[i] = BattlePhotoValue(p, rows[i]);
            here[i] = true;
        }
        PhotoRows(names, values, here, p.PanelOpen, Array.IndexOf(rows, p.Row));
    }

    /// <summary>The Cinema frame over a battle's stage: the camera kit's bars, 120 high and black.</summary>
    public static void PhotoBars(int sw, int sh)
    {
        float bar = CameraRig.BarHeight;
        Raylib.DrawRectangleRec(new Rectangle(0, 0, sw, bar), Color.Black);
        Raylib.DrawRectangleRec(new Rectangle(0, sh - bar, sw, bar), Color.Black);
    }
}
