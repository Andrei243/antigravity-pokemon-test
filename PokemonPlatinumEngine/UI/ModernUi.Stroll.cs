using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The Stroll Counter's screen (plan 35 · BN7; style guide, "The Stroll Counter"): a device of ours in two tones.</summary>
internal static partial class ModernUi
{
    /// <summary>The counter's two tones: its screen's pale green and the dark green it is drawn in, and the shell round it.</summary>
    internal static readonly Color StrollLight = new(196, 214, 158, 255), StrollInk = new(44, 66, 40, 255), StrollMid = new(126, 152, 96, 255),
        StrollShell = new(232, 120, 72, 255), StrollShellDark = new(176, 78, 46, 255);

    public static void DrawStroll(int sw, int sh, StrollCounterScreen screen, float appear)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("stroll.title"));
        string move = screen.Page is StrollPage.Seek or StrollPage.Meet ? "shared.keys.leftRight" : "shared.keys.upDown";
        Hints(sw - Margin, 44, Hint(move, "shared.hints.choose"), Hint("shared.keys.z", "shared.hints.doIt"), Hint("shared.keys.esc", "shared.hints.back"));
        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;

        // The shell, rounded like a pebble, and its screen
        var shell = new Rectangle(sw / 2f - 520, ContentTop + slide, 1040, ContentBottom - ContentTop - 20);
        UiShapes.Shape(shell, 120, StrollShell, StrollShellDark, StrollShellDark, 6);
        var lcd = new Rectangle(shell.X + 90, shell.Y + 70, shell.Width - 180, shell.Height - 230);
        UiShapes.Shape(lcd, 26, StrollLight, StrollLight, StrollInk, 4);

        var save = screen.Save;
        float x = lcd.X + 40, y = lcd.Y + 30;
        // The top line: the course, the strides and the sparks
        string course = StrollCounter.Course(save.Course) is { } c ? Text.Of($"stroll.course.{c.Id}") : Text.Of("stroll.noCourse");
        UiFonts.Draw(course, x, y, 30, StrollInk, UiWeight.Black);
        string counts = Text.Of("stroll.counts", ("strides", StrollCounter.Strides(save)), ("sparks", save.Sparks));
        UiFonts.Draw(counts, lcd.X + lcd.Width - 40 - UiFonts.Measure(counts, 28, UiWeight.ExtraBold), y + 2, 28, StrollInk, UiWeight.ExtraBold);
        UiShapes.Fill(new Rectangle(x, y + 48, lcd.Width - 80, 4), 2, StrollMid);

        float body = y + 70;
        switch (screen.Page)
        {
            case StrollPage.Home:
            {
                // The rider on the left, the menu on the right, and what it holds under the menu
                if (screen.Rider is { } rider) StrollSprite(PixelArtGenerator.SpriteOf(rider), new Vector2(x + 150, body + 140));
                else UiFonts.Draw(Text.Of("stroll.nobody"), x, body + 120, 28, StrollMid, UiWeight.ExtraBold);
                var rows = screen.Rows;
                for (int i = 0; i < rows.Count; i++)
                {
                    float ry = body + 10 + i * 62;
                    bool on = screen.Cursor == i;
                    if (on) UiShapes.Fill(new Rectangle(x + 330, ry - 6, 360, 52), 12, StrollInk);
                    string row = rows[i].ToString().ToLowerInvariant();
                    UiFonts.Draw(Text.Of($"stroll.row.{row}"), x + 352, ry, 34, on ? StrollLight : StrollInk, UiWeight.Black);
                }
                string held = Text.Of("stroll.held", ("caught", save.Caught.Count), ("found", save.Found.Count), ("most", StrollCounter.MostHeld));
                UiFonts.Draw(held, x + 330, body + 270, 26, StrollInk, UiWeight.ExtraBold);
                break;
            }
            case StrollPage.Seek when screen.Seek is { } seek:
            {
                // Three bushes; one looked in shows what was there
                for (int i = 0; i < StrollCounter.Bushes; i++)
                {
                    var at = new Vector2(lcd.X + lcd.Width * (i + 1) / 4f, body + 150);
                    bool looked = seek.Looked.Contains(i);
                    UiShapes.Circle(at + new Vector2(-38, 18), 44, looked ? StrollMid : StrollInk);
                    UiShapes.Circle(at + new Vector2(38, 18), 44, looked ? StrollMid : StrollInk);
                    UiShapes.Circle(at + new Vector2(0, -16), 52, looked ? StrollMid : StrollInk);
                    if (looked && i == seek.Hidden) UiShapes.Circle(at, 18, StrollLight);
                    if (screen.Cursor == i && !seek.Over)
                        UiShapes.Triangle(at + new Vector2(-18, -110), at + new Vector2(18, -110), at + new Vector2(0, -84), StrollInk);
                }
                break;
            }
            case StrollPage.Meet when screen.Meet is { } meet:
            {
                if (screen.Rider is { } rider) StrollSprite(PixelArtGenerator.SpriteOf(rider), new Vector2(x + 150, body + 140));
                if (meet.End != StrollMeetEnd.Caught || screen.Caught == null)
                    StrollSprite(PixelArtGenerator.GetPokemonSprite(meet.Species, false), new Vector2(lcd.X + lcd.Width - 190, body + 120));
                StrollHearts(new Vector2(x + 70, body + 270), meet.OwnHearts);
                StrollHearts(new Vector2(lcd.X + lcd.Width - 260, body + 250), meet.WildHearts);
                for (int i = 0; i < StrollCounterScreen.Moves.Count; i++)
                {
                    float mx = lcd.X + 330 + i * 150;
                    bool on = screen.Cursor == i && meet.End == StrollMeetEnd.Going;
                    if (on) UiShapes.Fill(new Rectangle(mx - 12, body + 300, 140, 48), 12, StrollInk);
                    string moveWord = StrollCounterScreen.Moves[i].ToString().ToLowerInvariant();
                    UiFonts.Draw(Text.Of($"stroll.move.{moveWord}"), mx, body + 306, 28, on ? StrollLight : StrollInk, UiWeight.Black);
                }
                break;
            }
            case StrollPage.Courses:
            {
                var courses = screen.Courses;
                for (int i = 0; i < courses.Count; i++)
                {
                    float ry = body + 10 + i * 56;
                    bool on = screen.Cursor == i;
                    if (on) UiShapes.Fill(new Rectangle(x - 10, ry - 6, lcd.Width - 60, 48), 12, StrollInk);
                    string id = courses[i].Id;
                    UiFonts.Draw(Text.Of($"stroll.course.{id}"), x + 10, ry, 30, on ? StrollLight : StrollInk, UiWeight.Black);
                }
                break;
            }
        }

        // The line under the screen
        if (screen.Message is { } message)
            DrawWrapped(message, shell.X + 120, lcd.Y + lcd.Height + 36, shell.Width - 240, 28, screen.Refused ? StrollShellDark : Color.White, 38, UiWeight.ExtraBold);
    }

    /// <summary>A menu sprite drawn in the counter's ink, as its screen shows everything.</summary>
    private static void StrollSprite(Texture2D sprite, Vector2 c)
    {
        int scale = sprite.Height * 2 <= 260 ? 2 : 1;
        Raylib.DrawTexturePro(sprite, new Rectangle(0, 0, sprite.Width, sprite.Height),
            new Rectangle(MathF.Round(c.X - sprite.Width * scale / 2f), MathF.Round(c.Y - sprite.Height * scale / 2f), sprite.Width * scale, sprite.Height * scale),
            Vector2.Zero, 0f, StrollInk);
    }

    /// <summary>A meet's hearts: filled while kept, hollow once lost.</summary>
    private static void StrollHearts(Vector2 at, int hearts)
    {
        for (int i = 0; i < StrollCounter.Hearts; i++)
        {
            var c = at + new Vector2(i * 54, 0);
            if (i < hearts) UiShapes.Circle(c, 18, StrollInk);
            else UiShapes.Ring(c, 18, 4, StrollInk);
        }
    }
}
