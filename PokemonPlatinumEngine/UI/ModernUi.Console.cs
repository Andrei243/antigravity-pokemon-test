using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The developer's console and the frame's time (plan 16 · T6; style guide, "Field menus and notices", "The console").</summary>
internal static partial class ModernUi
{
    /// <summary>The console's height in layout units, its text's size and the space between its lines.</summary>
    public const float ConsoleHeight = 520f, ConsoleText = 24f, ConsoleLeading = 30f;

    /// <summary>
    /// The console over the top of the screen: a dark sheet the width of the screen with a Selection rule under it, the
    /// output from the bottom up (what was typed pale, answers white, refusals red, a script's trace in the Selection
    /// colour), and the line being typed under a rule of its own with a caret that blinks.
    /// </summary>
    public static void ConsoleSheet(int sw, int sh, DevConsole console, bool caretOn)
    {
        var sheet = new Rectangle(0, 0, sw, ConsoleHeight);
        UiShapes.Fill(sheet, 0f, new Color(16, 20, 32, 232));
        UiShapes.Fill(new Rectangle(0, ConsoleHeight - 4, sw, 4), 0f, Selection);

        float left = 32f, width = sw - left * 2;
        float inputY = ConsoleHeight - 26f - ConsoleLeading / 2f;
        UiShapes.Fill(new Rectangle(left, inputY - ConsoleLeading / 2f - 10f, width, 2f), 0f, new Color(255, 255, 255, 50));
        string typed = "> " + console.Line;
        UiFonts.DrawCentered(FitConsoleLine(typed, width - 24f), left, inputY, ConsoleText, Color.White, UiWeight.Bold);
        if (caretOn)
        {
            float x = left + Math.Min(UiFonts.Measure(typed, ConsoleText, UiWeight.Bold), width - 20f) + 3f;
            UiShapes.Fill(new Rectangle(x, inputY - ConsoleText / 2f, 3f, ConsoleText), 0f, Selection);
        }

        // The output from the newest up, as many lines as the sheet holds
        float y = inputY - ConsoleLeading - 18f;
        for (int i = console.Output.Count - 1; i >= 0 && y > ConsoleLeading / 2f; i--, y -= ConsoleLeading)
        {
            var line = console.Output[i];
            var color = line.Tone switch
            {
                ConsoleTone.Typed => new Color(160, 172, 200, 255),
                ConsoleTone.Refused => new Color(255, 120, 110, 255),
                ConsoleTone.Trace => Selection,
                _ => Color.White
            };
            UiFonts.DrawCentered(FitConsoleLine(line.Text, width), left, y, ConsoleText, color, UiWeight.Bold);
        }
    }

    /// <summary>The frame's time in the top right corner, and under it the profiler's passes when they are measured.</summary>
    public static void FrameTimes(int sw, string line, string? passes)
    {
        float w = UiFonts.Measure(line, 26f, UiWeight.ExtraBold) + 40f;
        var r = new Rectangle(sw - w - 24f, 24f, w, 48f);
        UiShapes.Fill(r, 24f, new Color(16, 20, 32, 210));
        UiFonts.DrawCentered(line, r.X + 20f, r.Y + r.Height / 2f, 26f, Color.White, UiWeight.ExtraBold);
        if (string.IsNullOrEmpty(passes)) return;
        float pw = Math.Min(sw - 48f, UiFonts.Measure(passes, 20f, UiWeight.Bold) + 32f);
        var p = new Rectangle(sw - pw - 24f, r.Y + r.Height + 10f, pw, 38f);
        UiShapes.Fill(p, 19f, new Color(16, 20, 32, 210));
        UiFonts.DrawCentered(FitConsoleLine(passes, pw - 32f, 20f), p.X + 16f, p.Y + p.Height / 2f, 20f, Selection, UiWeight.Bold);
    }

    /// <summary>A line cut at the end to fit a width, with an ellipsis where it was cut.</summary>
    private static string FitConsoleLine(string text, float width, float size = ConsoleText)
    {
        if (UiFonts.Measure(text, size, UiWeight.Bold) <= width) return text;
        int keep = text.Length;
        while (keep > 0 && UiFonts.Measure(text[..keep] + "…", size, UiWeight.Bold) > width) keep -= Math.Max(1, keep / 16);
        return text[..Math.Max(0, keep)] + "…";
    }
}
