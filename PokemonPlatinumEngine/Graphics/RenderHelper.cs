using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Graphics;

public static class RenderHelper
{
    // The older screens share the interface typeface until G3 rebuilds them with the UI kit
    private static Font UiFont => UI.Kit.UiFonts.Get(UI.Kit.UiWeight.ExtraBold);

    private const float Spacing = 0.5f;

    public static void DrawText(string text, int x, int y, int fontSize, Color color)
    {
        var font = UiFont;
        Raylib.DrawTextEx(font, text, new Vector2(x, y), fontSize, Spacing, color);
    }

    public static int MeasureText(string text, int fontSize)
    {
        var font = UiFont;
        return (int)MathF.Ceiling(Raylib.MeasureTextEx(font, text, fontSize, Spacing).X);
    }

    public static void DrawPlatinumPanel(int x, int y, int width, int height, Color? bgColor = null)
    {
        Color bg = bgColor ?? Palette.UiPanelBg;
        float radius = Math.Min(14f, Math.Min(width, height) / 2f);
        Rectangle Rect(float inset) => new(x + inset, y + inset, width - inset * 2, height - inset * 2);
        float Round(Rectangle r, float rad) => Math.Clamp(rad * 2f / Math.Max(1f, Math.Min(r.Width, r.Height)), 0f, 1f);

        // Drop shadow, dark rim, light inner bevel, then the fill
        var shadow = new Rectangle(x + 4, y + 5, width, height);
        Raylib.DrawRectangleRounded(shadow, Round(shadow, radius), 8, new Color(0, 0, 0, 70));

        var outer = Rect(0);
        Raylib.DrawRectangleRounded(outer, Round(outer, radius), 8, Palette.UiDarkBorder);

        var bevel = Rect(3);
        Raylib.DrawRectangleRounded(bevel, Round(bevel, radius - 3), 8, new Color(250, 252, 255, 255));

        var fill = Rect(5);
        Raylib.DrawRectangleRounded(fill, Round(fill, radius - 5), 8, bg);

        // Soft gloss fading down from the top edge
        if (fill.Height > 16)
        {
            int inset = (int)MathF.Max(4f, radius - 4f);
            int glossH = (int)Math.Min(fill.Height * 0.5f, 48f);
            Raylib.DrawRectangleGradientV((int)fill.X + inset, (int)fill.Y + 2, (int)fill.Width - inset * 2, glossH,
                new Color(255, 255, 255, 90), new Color(255, 255, 255, 0));
        }
    }

    public static void DrawTextWithShadow(string text, int x, int y, int fontSize, Color textColor, Color? shadowColor = null)
    {
        Color shadow = shadowColor ?? Palette.TextShadow;
        int offset = fontSize >= 24 ? 2 : 1;
        DrawText(text, x + offset, y + offset, fontSize, shadow);
        DrawText(text, x, y, fontSize, textColor);
    }

    public static void DrawHPBar(int x, int y, int width, int height, int currentHP, int maxHP)
    {
        float ratio = maxHP > 0 ? Math.Clamp((float)currentHP / maxHP, 0f, 1f) : 0f;

        var frame = new Rectangle(x, y, width, height);
        Raylib.DrawRectangleRounded(frame, 1f, 8, new Color(44, 48, 62, 255));

        // "HP" tag
        int tagWidth = (int)(height * 1.9f);
        var tag = new Rectangle(x + 3, y + 3, tagWidth, height - 6);
        Raylib.DrawRectangleRounded(tag, 1f, 8, new Color(250, 186, 56, 255));
        int labelSize = height - 4;
        int labelWidth = MeasureText("HP", labelSize);
        DrawText("HP", x + 3 + (tagWidth - labelWidth) / 2, y + 1, labelSize, new Color(92, 52, 20, 255));

        // Track and fill
        int barX = x + tagWidth + 6;
        int barW = width - tagWidth - 9;
        var track = new Rectangle(barX, y + 4, barW, height - 8);
        Raylib.DrawRectangleRounded(track, 1f, 8, new Color(84, 90, 108, 255));

        Color barColor = ratio > 0.5f ? Palette.HpGreen : ratio > 0.2f ? Palette.HpYellow : Palette.HpRed;
        int fillW = (int)(barW * ratio);
        if (fillW > 0)
        {
            var fill = new Rectangle(barX, y + 4, Math.Max(fillW, height - 8), height - 8);
            Raylib.DrawRectangleRounded(fill, 1f, 8, barColor);
            var shine = new Rectangle(barX + 3, y + 5, Math.Max(0, fill.Width - 6), Math.Max(2, (height - 8) / 3f));
            Raylib.DrawRectangleRounded(shine, 1f, 8, new Color(255, 255, 255, 110));
        }
    }

    public static void DrawExpBar(int x, int y, int width, int height, float expRatio)
    {
        expRatio = Math.Clamp(expRatio, 0f, 1f);
        Raylib.DrawRectangleRounded(new Rectangle(x, y, width, height), 1f, 8, new Color(44, 48, 62, 255));
        int fillWidth = (int)((width - 4) * expRatio);
        if (fillWidth > 0)
        {
            Raylib.DrawRectangleRounded(new Rectangle(x + 2, y + 2, Math.Max(fillWidth, height - 4), height - 4), 1f, 8, Palette.ExpBlue);
        }
    }

    public static void DrawTypeBadge(int x, int y, PokemonType type, int width = 54, int height = 18)
    {
        string typeName = type.ToString().ToUpperInvariant();
        Color bg = Palette.GetTypeColor(typeName);

        var rect = new Rectangle(x, y, width, height);
        Raylib.DrawRectangleRounded(rect, 0.6f, 8, PixelCanvas.Shadow(bg, 0.45f));
        var inner = new Rectangle(x + 2, y + 2, width - 4, height - 4);
        Raylib.DrawRectangleRounded(inner, 0.6f, 8, bg);
        Raylib.DrawRectangleRounded(new Rectangle(x + 4, y + 3, width - 8, (height - 6) / 2f), 0.6f, 8, new Color(255, 255, 255, 60));

        int fontSize = Math.Max(10, (int)(height * 0.62f));
        int textWidth = MeasureText(typeName, fontSize);
        int tx = x + (width - textWidth) / 2;
        int ty = y + (height - fontSize) / 2;
        DrawTextWithShadow(typeName, tx, ty, fontSize, Color.White, new Color(0, 0, 0, 120));
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

        int w = 52;
        int h = 24;
        Raylib.DrawRectangleRounded(new Rectangle(x, y, w, h), 0.5f, 8, PixelCanvas.Shadow(bg, 0.4f));
        Raylib.DrawRectangleRounded(new Rectangle(x + 2, y + 2, w - 4, h - 4), 0.5f, 8, bg);
        int tw = MeasureText(code, 16);
        DrawText(code, x + (w - tw) / 2, y + 4, 16, Color.White);
    }

    /// <summary>Draws ♂ / ♀ as shapes (the UI font may not include those glyphs).</summary>
    public static void DrawGenderSymbol(int x, int y, int size, Gender gender)
    {
        if (gender != Gender.Male && gender != Gender.Female) return;

        bool male = gender == Gender.Male;
        Color color = male ? new Color(56, 120, 240, 255) : new Color(240, 88, 136, 255);
        float thickness = Math.Max(2f, size / 8f);
        float r = size * 0.28f;

        if (male)
        {
            var center = new Vector2(x + r + thickness, y + size - r - thickness);
            Raylib.DrawRing(center, r - thickness / 2f, r + thickness / 2f, 0, 360, 24, color);
            var tip = new Vector2(x + size - thickness, y + thickness);
            var start = center + Vector2.Normalize(tip - center) * r;
            Raylib.DrawLineEx(start, tip, thickness, color);
            Raylib.DrawLineEx(tip, tip + new Vector2(-size * 0.32f, 0), thickness, color);
            Raylib.DrawLineEx(tip, tip + new Vector2(0, size * 0.32f), thickness, color);
        }
        else
        {
            var center = new Vector2(x + size / 2f, y + r + thickness);
            Raylib.DrawRing(center, r - thickness / 2f, r + thickness / 2f, 0, 360, 24, color);
            var bottom = new Vector2(center.X, y + size);
            Raylib.DrawLineEx(center + new Vector2(0, r), bottom, thickness, color);
            float barY = center.Y + r + (bottom.Y - center.Y - r) * 0.45f;
            Raylib.DrawLineEx(new Vector2(center.X - size * 0.2f, barY), new Vector2(center.X + size * 0.2f, barY), thickness, color);
        }
    }

    public static void DrawPartyBallStatus(int x, int y, Party party)
    {
        for (int i = 0; i < 6; i++)
        {
            int bx = x + i * 22;
            if (i < party.Count)
            {
                var pkmn = party.Members[i];
                Color c = pkmn.IsFainted ? Color.DarkGray : (pkmn.Status != StatusCondition.None ? Color.Orange : new Color(228, 56, 56, 255));
                Raylib.DrawCircle(bx + 8, y + 8, 8, Palette.UiDarkBorder);
                Raylib.DrawCircleSector(new Vector2(bx + 8, y + 8), 6.5f, 180, 360, 12, c);
                Raylib.DrawCircleSector(new Vector2(bx + 8, y + 8), 6.5f, 0, 180, 12, Color.White);
                Raylib.DrawRectangle(bx + 1, y + 7, 14, 2, Palette.UiDarkBorder);
                Raylib.DrawCircle(bx + 8, y + 8, 2.5f, Color.White);
            }
            else
            {
                Raylib.DrawCircleLines(bx + 8, y + 8, 7, Color.Gray);
            }
        }
    }

    public static void DrawWrappedText(string text, int x, int y, int maxWidth, int fontSize, Color textColor, int lineSpacing = 6)
    {
        if (string.IsNullOrEmpty(text)) return;
        int currentY = y;

        foreach (var paragraph in text.Split('\n'))
        {
            string currentLine = "";
            foreach (var word in paragraph.Split(' '))
            {
                string testLine = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";
                if (MeasureText(testLine, fontSize) > maxWidth && !string.IsNullOrEmpty(currentLine))
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

            DrawTextWithShadow(currentLine, x, currentY, fontSize, textColor);
            currentY += fontSize + lineSpacing;
        }
    }
}
