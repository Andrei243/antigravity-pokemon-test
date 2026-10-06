using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>What sits over the field: dialogue, the start menu, the location sign and notices.</summary>
internal static partial class ModernUi
{
    public static void DrawDialogue(int sw, int sh, string speaker, string visibleText, bool complete)
    {
        var r = new Rectangle(96, sh - 262, sw - 192, 222);
        Panel(r, 32);
        if (!string.IsNullOrEmpty(speaker))
        {
            float w = UiFonts.Measure(speaker, 30, UiWeight.Black) + 56;
            var tag = new Rectangle(r.X + 44, r.Y - 30, w, 58);
            UiShapes.Shadow(tag, 29, 12, new Vector2(0, 5), ShadowColor);
            UiShapes.Shape(tag, 29, Lighter(Red, 0.12f), Darker(Red, 0.08f), Darker(Red, 0.3f), 3);
            UiFonts.DrawCentered(speaker, tag.X + 28, tag.Y + 29, 30, Color.White, UiWeight.Black);
        }
        DrawWrapped(visibleText, r.X + 60, r.Y + 58, r.Width - 170, 40, Ink, 58, UiWeight.ExtraBold);
        if (complete) AdvanceArrow(r.X + r.Width - 80, r.Y + r.Height - 60);
    }

    /// <summary>
    /// The answers to a question asked in the field (style guide, "Questions in the field"): a panel that slides in
    /// from the right and stands on the right end of the text box, a row to an answer, the one under the cursor a
    /// filled pill as in the start menu.
    /// </summary>
    public static void DrawChoices(int sw, int sh, IReadOnlyList<string> options, int selected, float shown)
    {
        const float row = 72, pad = 16, size = 34;
        float widest = 0f;
        foreach (string option in options) widest = Math.Max(widest, UiFonts.Measure(option, size, UiWeight.Black));
        float width = Math.Max(260, widest + 116);
        float height = pad * 2 + options.Count * row;
        // Its right edge is the text box's (96 from the screen's), and it stands 22 above it
        var panel = new Rectangle(sw - 96 - width + (1f - shown) * (width + 140), sh - 262 - 22 - height, width, height);
        Panel(panel, 32);

        for (int i = 0; i < options.Count; i++)
        {
            var r = new Rectangle(panel.X + 14, panel.Y + pad + i * row + 4, width - 28, row - 8);
            if (i == selected)
            {
                UiShapes.Shadow(r, r.Height / 2f, 18, new Vector2(0, 5), Selection with { A = 120 });
                UiShapes.Shape(r, r.Height / 2f, Lighter(Selection, 0.14f), Darker(Selection, 0.06f), Darker(Selection, 0.3f), 3);
            }
            UiFonts.DrawCentered(options[i], r.X + 36, r.Y + r.Height / 2f, size, i == selected ? Color.White : Ink, UiWeight.Black);
        }
    }

    /// <summary>
    /// The start menu: a panel on the right that slides in, one row per entry with its icon on a coloured chip.
    /// The selected row is a filled pill; the last entry (leaving the game) sits under a rule.
    /// </summary>
    public static void DrawStartMenu(int sw, IReadOnlyList<(string Label, UiIcon Icon, Color Color)> entries, int selected, float shown)
    {
        const float width = 440, row = 74;
        float height = 40 + entries.Count * row + 18;
        var panel = new Rectangle(sw - 40 - width + (1f - shown) * (width + 80), 40, width, height);
        Panel(panel, 34);

        for (int i = 0; i < entries.Count; i++)
        {
            bool last = i == entries.Count - 1;
            float y = panel.Y + 20 + i * row + (last ? 18 : 0);
            if (last) UiShapes.Fill(new Rectangle(panel.X + 36, y - 11, width - 72, 3), 1.5f, Rule);

            var (label, icon, color) = entries[i];
            var r = new Rectangle(panel.X + 16, y, width - 32, row - 8);
            var chip = new Vector2(r.X + 40, r.Y + r.Height / 2f);
            if (i == selected)
            {
                UiShapes.Shadow(r, r.Height / 2f, 18, new Vector2(0, 5), Selection with { A = 120 });
                UiShapes.Shape(r, r.Height / 2f, Lighter(Selection, 0.14f), Darker(Selection, 0.06f), Darker(Selection, 0.3f), 3);
                UiShapes.Circle(chip, 25, Color.White);
                UiIcons.Draw(icon, chip, 13, color, Color.White);
                UiFonts.DrawCentered(label, r.X + 84, r.Y + r.Height / 2f, 32, Color.White, UiWeight.Black);
            }
            else
            {
                UiShapes.Circle(chip, 25, color);
                UiIcons.Draw(icon, chip, 13, Color.White, color);
                UiFonts.DrawCentered(label, r.X + 84, r.Y + r.Height / 2f, 32, Ink, UiWeight.Black);
            }
        }
    }

    /// <summary>A short notice ("Game saved.") on a dark pill that drops in from the top edge.</summary>
    public static void DrawToast(int sw, string text, float shown)
    {
        float w = Math.Min(sw - 160, UiFonts.Measure(text, 30, UiWeight.ExtraBold) + 96);
        var r = new Rectangle((sw - w) / 2f, 28 - (1f - shown) * 130, w, 68);
        UiShapes.Shadow(r, 34, 22, new Vector2(0, 8), ShadowColor);
        UiShapes.Shape(r, 34, new Color(58, 70, 104, 245), new Color(36, 44, 68, 245), new Color(255, 255, 255, 60), 3);
        float tw = UiFonts.Measure(text, 30, UiWeight.ExtraBold);
        UiFonts.DrawCentered(text, r.X + (w - tw) / 2f, r.Y + r.Height / 2f, 30, Color.White, UiWeight.ExtraBold);
    }

    /// <summary>The colour of a place's sign: green for routes, blue for lakes and water, gold for towns and cities.</summary>
    public static Color PlaceColor(string name) =>
        name.StartsWith("Route", StringComparison.OrdinalIgnoreCase) ? Green
        : name.Contains("Lake", StringComparison.OrdinalIgnoreCase) ? Blue
        : name.Contains("Town", StringComparison.OrdinalIgnoreCase) || name.Contains("City", StringComparison.OrdinalIgnoreCase) ? Gold
        : Muted;

    /// <summary>The sign that drops in at the top left on arriving somewhere, as in the games: the place's name on a slanted plate.</summary>
    public static void DrawLocationSign(string name, float shown)
    {
        float textW = UiFonts.Measure(name, 44, UiWeight.Black);
        var r = new Rectangle(48, 40 - (1f - shown) * 190, Math.Max(380, textW + 150), 96);
        Panel(r, 24, skew: -0.2f);
        var color = PlaceColor(name);
        UiShapes.Shape(new Rectangle(r.X + 26, r.Y + 16, 22, r.Height - 32), 11, Lighter(color, 0.15f), Darker(color, 0.1f), skew: -0.2f);
        UiFonts.DrawCentered(name, r.X + 76, r.Y + r.Height / 2f, 44, Ink, UiWeight.Black);
    }

    /// <summary>
    /// A field move's cut-in (style guide, "A field move's cut-in"): a dark band across the screen edged with the
    /// Selection colour, the Pokémon's sprite running through it and the move's name on a slanted plate beside it.
    /// </summary>
    public static void DrawCutIn(int sw, int sh, Texture2D sprite, string move, float band, float slide, float label)
    {
        if (band <= 0f) return;
        const float full = 300f, edge = 4f, size = 256f;
        float middle = sh * 0.44f, half = full / 2f * band;
        var dark = new Color(36, 44, 68, 236);
        Raylib.DrawRectangleRec(new Rectangle(0, middle - half, sw, half * 2f), dark);
        if (band >= 0.5f)
        {
            Raylib.DrawRectangleRec(new Rectangle(0, middle - half, sw, edge), Selection);
            Raylib.DrawRectangleRec(new Rectangle(0, middle + half - edge, sw, edge), Selection);
        }

        // The Pokémon holds a little left of the middle, and comes from beyond the right edge and leaves past the left
        float hold = sw * 0.42f - size / 2f;
        float x = slide >= 0f ? hold + slide * (sw - hold + 40f) : hold + slide * (hold + size + 40f);
        float top = middle - size / 2f - 8f;
        // Only what is inside the band shows: the sprite is cut to it, top and bottom
        float y0 = Math.Max(top, middle - half), y1 = Math.Min(top + size, middle + half);
        if (y1 > y0)
        {
            float texel = sprite.Height / size;
            var source = new Rectangle(0, (y0 - top) * texel, sprite.Width, (y1 - y0) * texel);
            Raylib.DrawTexturePro(sprite, source, new Rectangle(MathF.Round(x), y0, size, y1 - y0), Vector2.Zero, 0f, Color.White);
        }

        if (label <= 0f) return;
        float tw = UiFonts.Measure(move, 52, UiWeight.Black);
        var plate = new Rectangle(sw * 0.42f + size / 2f + 24f + (1f - label) * 60f, middle - 44f, tw + 96f, 88f);
        var fill = new Color(255, 255, 255, (int)(255 * label));
        UiShapes.Shape(plate, 18, fill, new Color(234, 240, 248, (int)(255 * label)), new Color(52, 64, 96, (int)(255 * label)), 3, skew: -0.2f);
        UiFonts.DrawCentered(move, plate.X + 48f, plate.Y + plate.Height / 2f, 52, Ink with { A = (byte)(255 * label) }, UiWeight.Black);
    }
}
