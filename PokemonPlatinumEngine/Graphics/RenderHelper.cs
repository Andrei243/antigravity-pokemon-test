using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Graphics;

public static class RenderHelper
{
    public static void DrawPlatinumPanel(int x, int y, int width, int height, Color? bgColor = null)
    {
        Color bg = bgColor ?? Palette.UiPanelBg;
        Color borderDark = Palette.UiDarkBorder;
        Color borderLight = Palette.UiLightBorder;
        Color accent = Palette.UiAccent;

        // Outer drop shadow
        Raylib.DrawRectangle(x + 2, y + 2, width, height, new Color(0, 0, 0, 80));

        // Outer border
        Raylib.DrawRectangle(x, y, width, height, borderDark);

        // Inner frame
        Raylib.DrawRectangle(x + 2, y + 2, width - 4, height - 4, borderLight);
        Raylib.DrawRectangle(x + 4, y + 4, width - 8, height - 8, borderDark);

        // Fill
        Raylib.DrawRectangle(x + 5, y + 5, width - 10, height - 10, bg);

        // Platinum top-left corner accent
        Raylib.DrawRectangle(x + 5, y + 5, 8, 2, accent);
        Raylib.DrawRectangle(x + 5, y + 5, 2, 8, accent);
    }

    public static void DrawTextWithShadow(string text, int x, int y, int fontSize, Color textColor, Color? shadowColor = null)
    {
        Color shadow = shadowColor ?? Palette.TextShadow;
        Raylib.DrawText(text, x + 1, y + 1, fontSize, shadow);
        Raylib.DrawText(text, x, y, fontSize, textColor);
    }

    public static void DrawHPBar(int x, int y, int width, int height, int currentHP, int maxHP)
    {
        float ratio = maxHP > 0 ? Math.Clamp((float)currentHP / maxHP, 0f, 1f) : 0f;
        int fillWidth = (int)((width - 4) * ratio);

        // Background / Border
        Raylib.DrawRectangle(x, y, width, height, Palette.HpBg);
        Raylib.DrawRectangle(x + 1, y + 1, width - 2, height - 2, new Color(32, 32, 40, 255));

        // HP Label Tag
        Raylib.DrawRectangle(x + 2, y + 2, 16, height - 4, new Color(248, 208, 48, 255));
        Raylib.DrawText("HP", x + 3, y + 2, height - 4, new Color(48, 48, 48, 255));

        // Bar Fill Color
        Color barColor = ratio > 0.5f ? Palette.HpGreen : ratio > 0.2f ? Palette.HpYellow : Palette.HpRed;
        int barStartX = x + 19;
        int barAvailWidth = width - 21;
        int barActualWidth = (int)(barAvailWidth * ratio);

        if (barActualWidth > 0)
        {
            Raylib.DrawRectangle(barStartX, y + 2, barActualWidth, height - 4, barColor);
            Raylib.DrawRectangle(barStartX, y + 2, barActualWidth, 1, Color.White); // Highlight
        }
    }

    public static void DrawExpBar(int x, int y, int width, int height, float expRatio)
    {
        expRatio = Math.Clamp(expRatio, 0f, 1f);
        Raylib.DrawRectangle(x, y, width, height, Palette.HpBg);
        int fillWidth = (int)((width - 2) * expRatio);
        if (fillWidth > 0)
        {
            Raylib.DrawRectangle(x + 1, y + 1, fillWidth, height - 2, Palette.ExpBlue);
        }
    }

    public static void DrawTypeBadge(int x, int y, PokemonType type, int width = 54, int height = 18)
    {
        string typeName = type.ToString().ToUpperInvariant();
        Color bg = Palette.GetTypeColor(typeName);

        Raylib.DrawRectangle(x, y, width, height, new Color(32, 32, 40, 255));
        Raylib.DrawRectangle(x + 1, y + 1, width - 2, height - 2, bg);
        Raylib.DrawRectangle(x + 1, y + 1, width - 2, 2, new Color(255, 255, 255, 100));

        int textWidth = Raylib.MeasureText(typeName, 10);
        int tx = x + (width - textWidth) / 2;
        int ty = y + (height - 10) / 2;
        DrawTextWithShadow(typeName, tx, ty, 10, Color.White, new Color(0, 0, 0, 150));
    }

    public static void DrawStatusBadge(int x, int y, StatusCondition status)
    {
        if (status == StatusCondition.None) return;

        string code = status switch
        {
            StatusCondition.Poison or StatusCondition.Toxic => "PSN",
            StatusCondition.Burn => "BRN",
            StatusCondition.Paralyze => "PAR",
            StatusCondition.Sleep => "SLP",
            StatusCondition.Freeze => "FRZ",
            StatusCondition.Faint => "FNT",
            _ => ""
        };

        Color bg = status switch
        {
            StatusCondition.Poison or StatusCondition.Toxic => Palette.StatusPoison,
            StatusCondition.Burn => Palette.StatusBurn,
            StatusCondition.Paralyze => Palette.StatusParalyze,
            StatusCondition.Sleep => Palette.StatusSleep,
            StatusCondition.Freeze => Palette.StatusFreeze,
            StatusCondition.Faint => Palette.StatusFaint,
            _ => Color.Gray
        };

        int w = 32;
        int h = 14;
        Raylib.DrawRectangle(x, y, w, h, bg);
        Raylib.DrawText(code, x + 4, y + 2, 10, Color.White);
    }

    public static void DrawPartyBallStatus(int x, int y, Party party)
    {
        for (int i = 0; i < 6; i++)
        {
            int bx = x + i * 14;
            if (i < party.Count)
            {
                var pkmn = party.Members[i];
                Color c = pkmn.IsFainted ? Color.DarkGray : (pkmn.Status != StatusCondition.None ? Color.Orange : Color.Red);
                Raylib.DrawCircle(bx + 5, y + 5, 5, Color.Black);
                Raylib.DrawCircle(bx + 5, y + 5, 4, c);
                Raylib.DrawCircle(bx + 5, y + 5, 2, Color.White);
            }
            else
            {
                // Empty slot outline
                Raylib.DrawCircleLines(bx + 5, y + 5, 4, Color.Gray);
            }
        }
    }

    public static int MeasureText(string text, int fontSize) => Raylib.MeasureText(text, fontSize);

    public static void DrawWrappedText(string text, int x, int y, int maxWidth, int fontSize, Color textColor, int lineSpacing = 6)
    {
        if (string.IsNullOrEmpty(text)) return;
        string[] words = text.Split(' ');
        string currentLine = "";
        int currentY = y;

        foreach (var word in words)
        {
            string testLine = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";
            int testWidth = Raylib.MeasureText(testLine, fontSize);
            if (testWidth > maxWidth && !string.IsNullOrEmpty(currentLine))
            {
                DrawTextWithShadow(currentLine, x, currentY, fontSize, textColor);
                currentY += fontSize + lineSpacing;
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
        {
            DrawTextWithShadow(currentLine, x, currentY, fontSize, textColor);
        }
    }
}

