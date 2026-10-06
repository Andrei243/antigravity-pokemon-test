using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The Pokétch over the field (style guide, "The Pokétch"): a watch's body and an LCD of our own, its apps drawn in blocks.</summary>
internal static partial class ModernUi
{
    // The LCD's three greens and the size of its blocks
    private static readonly Color LcdPaper = new(176, 204, 160, 255), LcdInk = new(40, 62, 48, 255), LcdMid = new(112, 146, 104, 255);
    private const int Block = 8;

    /// <param name="shown">0 put away to 1 out: the watch slides up from below the screen's edge.</param>
    public static void DrawPoketch(int sw, int sh, Poketch poketch, Party party, DateTime now, float shown)
    {
        const float width = 456, height = 368;
        var body = new Rectangle(sw - 32 - width, sh - 32 - height + (1f - shown) * (height + 48), width, height);
        UiShapes.Shadow(body, 40, 24, new Vector2(0, 8), ShadowColor);
        UiShapes.Shape(body, 40, Lighter(Frame, 0.12f), Frame, Lighter(Frame, 0.3f), 4);
        // The side button that changes the app
        UiShapes.Shape(new Rectangle(body.X + body.Width - 6, body.Y + 120, 18, 92), 9, Lighter(Frame, 0.2f), Darker(Frame, 0.1f));

        var screen = new Rectangle(body.X + 40, body.Y + 36, 360, 296);
        Raylib.DrawRectangleRec(screen, LcdPaper);
        Raylib.DrawRectangleLinesEx(screen, 4, Darker(Frame, 0.25f));

        switch (poketch.Current)
        {
            case PoketchApp.DigitalWatch:
                DigitalWatch(screen, now);
                break;
            case PoketchApp.Pedometer:
                Pedometer(screen, poketch.Steps);
                break;
            case PoketchApp.PartyStatus:
                PartyStatus(screen, party);
                break;
            default:
                UiFonts.DrawCentered("No apps yet.", screen.X + 40, screen.Y + screen.Height / 2f, 28, LcdInk, UiWeight.Black);
                break;
        }
    }

    private static void LcdBlock(float x, float y, int w, int h, Color c) => Raylib.DrawRectangleRec(new Rectangle(x, y, w * Block, h * Block), c);

    /// <summary>
    /// A seven-segment figure, 7 blocks by 13: the segments lit in the ink, the others faintly in the middle green,
    /// as an LCD's unlit segments show.
    /// </summary>
    private static void SegmentDigit(float x, float y, int digit)
    {
        // Segments a to g: top, upper right, lower right, bottom, lower left, upper left, middle
        int[] lit = { 0b0111111, 0b0000110, 0b1011011, 0b1001111, 0b1100110, 0b1101101, 0b1111101, 0b0000111, 0b1111111, 0b1101111 };
        int mask = lit[Math.Clamp(digit, 0, 9)];
        Color On(int bit) => (mask & (1 << bit)) != 0 ? LcdInk : LcdMid with { A = 70 };
        LcdBlock(x + Block, y, 5, 1, On(0));
        LcdBlock(x + 6 * Block, y + Block, 1, 5, On(1));
        LcdBlock(x + 6 * Block, y + 7 * Block, 1, 5, On(2));
        LcdBlock(x + Block, y + 12 * Block, 5, 1, On(3));
        LcdBlock(x, y + 7 * Block, 1, 5, On(4));
        LcdBlock(x, y + Block, 1, 5, On(5));
        LcdBlock(x + Block, y + 6 * Block, 5, 1, On(6));
    }

    private static readonly string[] Days = { "SUNDAY", "MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY", "SATURDAY" };

    /// <summary>The digital watch: hours and minutes, a colon that blinks each second, the day under them.</summary>
    private static void DigitalWatch(Rectangle screen, DateTime now)
    {
        const float digit = 7 * Block, gap = Block;
        float total = 4 * digit + 2 * gap + 3 * Block;
        float x = screen.X + MathF.Round((screen.Width - total) / 2f / Block) * Block, y = screen.Y + 72;
        SegmentDigit(x, y, now.Hour / 10);
        SegmentDigit(x + digit + gap, y, now.Hour % 10);
        float colon = x + 2 * digit + gap + Block;
        if (FrameClock.Now % 1.0 < 0.5)
        {
            LcdBlock(colon, y + 3 * Block, 1, 1, LcdInk);
            LcdBlock(colon, y + 9 * Block, 1, 1, LcdInk);
        }
        float minutes = colon + 2 * Block;
        SegmentDigit(minutes, y, now.Minute / 10);
        SegmentDigit(minutes + digit + gap, y, now.Minute % 10);

        string day = Days[(int)now.DayOfWeek];
        float w = UiFonts.Measure(day, 28, UiWeight.Black);
        UiFonts.DrawCentered(day, screen.X + (screen.Width - w) / 2f, screen.Y + 240, 28, LcdInk, UiWeight.Black);
    }

    /// <summary>The pedometer: the steps taken, in five figures.</summary>
    private static void Pedometer(Rectangle screen, int steps)
    {
        const float digit = 7 * Block, gap = Block;
        float total = 5 * digit + 4 * gap;
        float x = screen.X + MathF.Round((screen.Width - total) / 2f / Block) * Block, y = screen.Y + 64;
        string figures = Math.Clamp(steps, 0, Poketch.MostSteps).ToString("D5");
        for (int i = 0; i < figures.Length; i++) SegmentDigit(x + i * (digit + gap), y, figures[i] - '0');
        float w = UiFonts.Measure("STEPS", 28, UiWeight.Black);
        UiFonts.DrawCentered("STEPS", screen.X + (screen.Width - w) / 2f, screen.Y + 232, 28, LcdInk, UiWeight.Black);
    }

    /// <summary>The team: two rows of three, each icon over a bar of its HP; a fainted one dark, a status marked.</summary>
    private static void PartyStatus(Rectangle screen, Party party)
    {
        for (int i = 0; i < Party.MaxSize; i++)
        {
            float cx = screen.X + 24 + (i % 3) * 112, cy = screen.Y + 24 + (i / 3) * 136;
            if (i >= party.Count)
            {
                LcdBlock(cx + 32, cy + 40, 4, 1, LcdMid);
                continue;
            }
            var p = party.Members[i];
            var icon = PixelArtGenerator.GetPokemonIcon(p.ModelName);
            var tint = p.IsFainted ? LcdInk : new Color(150, 186, 140, 255);
            Raylib.DrawTexturePro(icon, new Rectangle(0, 0, icon.Width, icon.Height), new Rectangle(cx + 8, cy, 80, 80), Vector2.Zero, 0f, tint);
            if (p.Status != StatusCondition.None && !p.IsFainted) LcdBlock(cx + 80, cy + 4, 1, 1, LcdInk);

            // The bar: an ink frame of blocks, filled in the middle green as far as the HP goes
            float bx = cx + 8, by = cy + 96;
            LcdBlock(bx, by, 10, 1, LcdInk);
            LcdBlock(bx, by + 2 * Block, 10, 1, LcdInk);
            LcdBlock(bx, by + Block, 1, 1, LcdInk);
            LcdBlock(bx + 9 * Block, by + Block, 1, 1, LcdInk);
            int filled = p.MaxHP <= 0 ? 0 : (int)MathF.Ceiling(8f * p.CurrentHP / p.MaxHP);
            if (filled > 0) LcdBlock(bx + Block, by + Block, filled, 1, LcdMid);
        }
    }
}
