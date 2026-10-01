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
