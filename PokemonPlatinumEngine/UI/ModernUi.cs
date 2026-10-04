using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The interface kit (plan 04 · G3): crisp anti-aliased panels with soft shadows, Nunito, and Platinum's colour
/// language (red FIGHT, gold BAG, green POKÉMON, blue RUN; light panels framed in slate). Pokémon appear as 2D
/// pixel sprites, as in the main games. This file holds the tokens and the components every screen shares; the
/// other parts lay out the battle, the party and summary, and the field's menus. See docs/art/style-guide.md.
/// </summary>
internal static partial class ModernUi
{
    // Design tokens
    public static readonly Color Ink = new(36, 44, 68, 255);
    public static readonly Color Muted = new(110, 120, 148, 255);
    public static readonly Color Frame = new(52, 64, 96, 255);
    public static readonly Color PanelTop = new(255, 255, 255, 255);
    public static readonly Color PanelBottom = new(234, 240, 248, 255);
    public static readonly Color ShadowColor = new(14, 22, 46, 80);
    public static readonly Color Red = new(232, 72, 76, 255);
    public static readonly Color Gold = new(240, 176, 56, 255);
    public static readonly Color Green = new(56, 182, 104, 255);
    public static readonly Color Blue = new(70, 136, 232, 255);
    public static readonly Color Track = new(62, 72, 98, 255);
    public static readonly Color Selection = new(240, 104, 70, 255);
    public static readonly Color Rule = new(206, 214, 230, 255);

    internal static Color Lighter(Color c, float t) => PixelCanvas.Mix(c, Color.White, t);
    internal static Color Darker(Color c, float t) => PixelCanvas.Mix(c, new Color(20, 16, 40, 255), t);

    // ------------------------------------------------------------------ panels and buttons

    public static void Panel(Rectangle r, float radius, float skew = 0f, float border = 4f, Color? frame = null)
    {
        UiShapes.Shadow(r, radius, 22f, new Vector2(0, 8), ShadowColor, skew);
        UiShapes.Shape(r, radius, PanelTop, PanelBottom, frame ?? Frame, border, skew);
    }

    /// <summary>A light panel that can be chosen: the selected one gets a thick border and a glow in the accent colour.</summary>
    public static void Card(Rectangle r, float radius, bool selected, Color? accent = null)
    {
        var glow = accent ?? Selection;
        if (selected) UiShapes.Shadow(r, radius, radius, Vector2.Zero, glow with { A = 190 });
        else UiShapes.Shadow(r, radius, 22f, new Vector2(0, 9), new Color(6, 14, 34, 105));
        UiShapes.Shape(r, radius, PanelTop, PanelBottom, selected ? glow : Frame, selected ? 6f : 4f);
    }

    /// <summary>An empty slot where a card would be: a faint outline on the backdrop.</summary>
    public static void EmptySlot(Rectangle r, float radius) =>
        UiShapes.Shape(r, radius, new Color(255, 255, 255, 30), new Color(255, 255, 255, 18), new Color(255, 255, 255, 60), 3);

    /// <summary>A coloured pill button: gradient body, bright top edge, white label; selected ones get a white ring and glow.</summary>
    public static void Button(Rectangle r, float radius, Color color, string label, float size, bool selected)
    {
        if (selected)
        {
            UiShapes.Shadow(r, radius, 30f, Vector2.Zero, color with { A = 150 });
            var ring = new Rectangle(r.X - 6, r.Y - 6, r.Width + 12, r.Height + 12);
            UiShapes.Fill(ring, radius + 6, Color.White);
        }
        else
        {
            UiShapes.Shadow(r, radius, 14f, new Vector2(0, 6), ShadowColor);
        }
        UiShapes.Shape(r, radius, Lighter(color, 0.18f), Darker(color, 0.12f), Darker(color, 0.35f), 3f);
        var shine = new Rectangle(r.X + 10, r.Y + 6, r.Width - 20, r.Height * 0.38f);
        UiShapes.Fill(shine, radius - 6, new Color(255, 255, 255, 46));
        float w = UiFonts.Measure(label, size, UiWeight.Black);
        UiFonts.DrawCentered(label, r.X + (r.Width - w) / 2f, r.Y + r.Height / 2f + 2, size, new Color(0, 0, 0, 50), UiWeight.Black);
        UiFonts.DrawCentered(label, r.X + (r.Width - w) / 2f, r.Y + r.Height / 2f, size, Color.White, UiWeight.Black);
    }

    // ------------------------------------------------------------------ bars

    public static Color HpColor(float ratio) =>
        ratio > 0.5f ? new Color(70, 214, 110, 255) : ratio > 0.2f ? new Color(246, 196, 50, 255) : new Color(240, 72, 64, 255);

    public static void HpBar(float x, float y, float w, float h, float ratio)
    {
        ratio = Math.Clamp(ratio, 0f, 1f);
        float tagW = h * 2.3f;
        var tag = new Rectangle(x, y, tagW, h);
        UiShapes.Shape(tag, h / 2f, new Color(255, 200, 80, 255), new Color(236, 160, 40, 255));
        float ts = h * 0.72f;
        UiFonts.DrawCentered("HP", x + (tagW - UiFonts.Measure("HP", ts, UiWeight.Black)) / 2f, y + h / 2f, ts, new Color(110, 60, 16, 255), UiWeight.Black);
        Bar(new Rectangle(x + tagW + 6, y, w - tagW - 6, h), ratio, HpColor(ratio));
    }

    /// <summary>A pill-shaped track with a fill that keeps round ends however short it is.</summary>
    public static void Bar(Rectangle track, float ratio, Color color)
    {
        float h = track.Height;
        UiShapes.Fill(track, h / 2f, Track);
        float fw = (track.Width - 6) * Math.Clamp(ratio, 0f, 1f);
        if (fw <= 0.5f) return;
        var fill = new Rectangle(track.X + 3, track.Y + 3, Math.Max(fw, h - 6), h - 6);
        UiShapes.Shape(fill, (h - 6) / 2f, Lighter(color, 0.3f), color);
    }

    public static void ExpBar(Rectangle r, float ratio)
    {
        UiShapes.Fill(r, r.Height / 2f, Track);
        float w = r.Width * Math.Clamp(ratio, 0f, 1f);
        if (w > 1f) UiShapes.Shape(new Rectangle(r.X, r.Y, Math.Max(w, r.Height), r.Height), r.Height / 2f, new Color(120, 204, 255, 255), new Color(72, 176, 250, 255));
    }

    // ------------------------------------------------------------------ Pokémon labels

    /// <summary>The level, right-aligned at <paramref name="rightX"/>: a small muted "Lv" before a large number.</summary>
    public static void Level(float rightX, float centerY, int level, float size)
    {
        string n = level.ToString();
        float nw = UiFonts.Measure(n, size, UiWeight.Black);
        float lw = UiFonts.Measure("Lv", size * 0.62f, UiWeight.ExtraBold);
        UiFonts.DrawCentered("Lv", rightX - nw - lw - 4, centerY + size * 0.12f, size * 0.62f, Muted, UiWeight.ExtraBold);
        UiFonts.DrawCentered(n, rightX - nw, centerY, size, Ink, UiWeight.Black);
    }

    /// <summary>The name with the gender mark after it; returns the width used.</summary>
    public static float NameWithGender(Pokemon p, float x, float centerY, float size)
    {
        UiFonts.DrawCentered(p.DisplayName, x, centerY, size, Ink, UiWeight.Black);
        float w = UiFonts.Measure(p.DisplayName, size, UiWeight.Black);
        if (p.Gender is not (Gender.Male or Gender.Female)) return w;
        UiIcons.GenderMark(new Vector2(x + w + size * 0.5f, centerY), size * 0.74f, p.Gender);
        return w + size * 0.9f;
    }

    /// <summary>A type's name on a pill of its colour; returns the pill's width.</summary>
    public static float TypePill(float x, float y, PokemonType type, float height = 34f, float? width = null)
    {
        var color = Palette.GetTypeColor(type.ToString());
        string name = type.ToString().ToUpperInvariant();
        float size = height * 0.6f;
        float textW = UiFonts.Measure(name, size, UiWeight.Black);
        float w = width ?? textW + height * 1.05f;
        var pill = new Rectangle(x, y, w, height);
        UiShapes.Shape(pill, height / 2f, Lighter(color, 0.12f), Darker(color, 0.1f));
        UiFonts.DrawCentered(name, x + (w - textW) / 2f, y + height / 2f, size, Color.White, UiWeight.Black);
        return w;
    }

    public static IEnumerable<PokemonType> TypesOf(Pokemon p)
    {
        yield return p.Species.PrimaryType;
        if (p.Species.SecondaryType.HasValue) yield return p.Species.SecondaryType.Value;
    }

    /// <summary>Draws the type pills side by side; returns the x after the last one.</summary>
    public static float TypePills(float x, float y, Pokemon p, float height = 34f)
    {
        foreach (var type in TypesOf(p)) x += TypePill(x, y, type, height) + 10;
        return x;
    }

    /// <summary>A status condition as a three-letter pill (PSN, BRN, PAR, SLP, FRZ, FNT); returns its width, 0 for none.</summary>
    public static float StatusPill(float x, float y, StatusCondition status, float height = 34f)
    {
        if (status == StatusCondition.None) return 0f;
        var (code, color) = status switch
        {
            StatusCondition.Poison or StatusCondition.Toxic => ("PSN", Palette.StatusPoison),
            StatusCondition.Burn => ("BRN", Palette.StatusBurn),
            StatusCondition.Paralyze => ("PAR", Palette.StatusParalyze),
            StatusCondition.Sleep => ("SLP", Palette.StatusSleep),
            StatusCondition.Freeze => ("FRZ", Palette.StatusFreeze),
            _ => ("FNT", Palette.StatusFaint)
        };
        float size = height * 0.6f;
        float textW = UiFonts.Measure(code, size, UiWeight.Black);
        float w = textW + height * 0.9f;
        UiShapes.Shape(new Rectangle(x, y, w, height), height / 2f, Lighter(color, 0.1f), Darker(color, 0.12f), Darker(color, 0.4f), 2f);
        UiFonts.DrawCentered(code, x + (w - textW) / 2f, y + height / 2f, size, Color.White, UiWeight.Black);
        return w;
    }

    /// <summary>A small grey tag for a short word ("LEAD", "IN BATTLE"); returns its width.</summary>
    public static float Tag(float x, float y, string text, Color color, float height = 34f)
    {
        float size = height * 0.58f;
        float textW = UiFonts.Measure(text, size, UiWeight.Black);
        float w = textW + height * 0.9f;
        UiShapes.Fill(new Rectangle(x, y, w, height), height / 2f, color);
        UiFonts.DrawCentered(text, x + (w - textW) / 2f, y + height / 2f, size, Color.White, UiWeight.Black);
        return w;
    }

    /// <summary>
    /// A Pokémon's menu icon on a pale disc with a faint Poké Ball line, at a whole-number scale. Like the main
    /// games the icons hop: the selected one quickly and high, the rest gently, fainted ones not at all.
    /// </summary>
    public static void Portrait(Vector2 c, float radius, Pokemon p, int scale, bool selected)
    {
        UiShapes.Circle(c, radius, new Color(226, 234, 246, 255));
        UiShapes.Fill(new Rectangle(c.X - radius, c.Y - 3, radius * 2, 6), 3, Rule);
        UiShapes.Circle(c, radius * 0.27f, Rule);
        UiShapes.Circle(c, radius * 0.18f, new Color(226, 234, 246, 255));
        var icon = PixelArtGenerator.GetPokemonIcon(p.Species.Name);
        float t = (float)FrameClock.Now;
        int hop = p.IsFainted ? 0 : selected ? ((int)(t / 0.16f) % 2) * 3 * scale : ((int)(t / 0.4f) % 2) * scale;
        Raylib.DrawTexturePro(icon, new Rectangle(0, 0, icon.Width, icon.Height),
            new Rectangle(MathF.Round(c.X - icon.Width * scale / 2f), MathF.Round(c.Y - icon.Height * scale / 2f - 1.5f * scale - hop), icon.Width * scale, icon.Height * scale),
            Vector2.Zero, 0, p.IsFainted ? new Color(170, 170, 190, 255) : Color.White);
    }

    // ------------------------------------------------------------------ text

    /// <summary>Breaks text into lines no wider than <paramref name="maxWidth"/>; "\n" forces a break.</summary>
    public static List<string> Wrap(string text, float maxWidth, float size, UiWeight weight = UiWeight.Bold)
    {
        var lines = new List<string>();
        foreach (var paragraph in text.Split('\n'))
        {
            string line = "";
            foreach (var word in paragraph.Split(' '))
            {
                string test = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && UiFonts.Measure(test, size, weight) > maxWidth)
                {
                    lines.Add(line);
                    line = word;
                }
                else line = test;
            }
            lines.Add(line);
        }
        return lines;
    }

    public static void DrawWrapped(string text, float x, float y, float maxWidth, float size, Color color, float lineHeight, UiWeight weight = UiWeight.Bold)
    {
        foreach (var line in Wrap(text, maxWidth, size, weight))
        {
            if (line.Length > 0) UiFonts.Draw(line, x, y, size, color, weight);
            y += lineHeight;
        }
    }

    /// <summary>A small caption in capitals above a value.</summary>
    public static void Label(string text, float x, float y) => UiFonts.Draw(text, x, y, 20, Muted, UiWeight.Black);

    /// <summary>The bobbing red arrow that says a message is waiting for the A button.</summary>
    public static void AdvanceArrow(float x, float y)
    {
        float bob = MathF.Sin((float)FrameClock.Now * 6f) * 3f;
        UiIcons.ArrowDown(new Vector2(x + 14, y + 10 + bob), 18, Red);
    }

    // ------------------------------------------------------------------ screens

    /// <summary>Full-screen menu backdrop: deep teal with faint diagonal stripes.</summary>
    public static void Backdrop(int sw, int sh)
    {
        Raylib.DrawRectangleGradientV(0, 0, sw, sh, new Color(44, 112, 146, 255), new Color(24, 60, 98, 255));
        for (int i = -10; i < 40; i++)
            Raylib.DrawRectanglePro(new Rectangle(i * 90, -200, 34, 1600), Vector2.Zero, 28f, new Color(255, 255, 255, 12));
    }

    public static void ScreenTitle(string title) => UiFonts.Draw(title, 64, 36, 52, Color.White, UiWeight.Black);

    /// <summary>Darkens what is behind a prompt or a panel that needs the player's attention.</summary>
    public static void Dim(int sw, int sh, int alpha) => Raylib.DrawRectangle(0, 0, sw, sh, new Color(8, 12, 30, alpha));

    /// <summary>A key cap and what it does; returns its width.</summary>
    public static float HintPill(float x, float y, string key, string label)
    {
        float width = HintWidth(key, label);
        float kw = Math.Max(44, UiFonts.Measure(key, 22, UiWeight.Black) + 24);
        UiShapes.Fill(new Rectangle(x, y, width, 52), 26, new Color(10, 30, 60, 90));
        var k = new Rectangle(x + 6, y + 6, kw, 40);
        UiShapes.Fill(k, 20, Color.White);
        UiFonts.DrawCentered(key, k.X + (kw - UiFonts.Measure(key, 22, UiWeight.Black)) / 2f, k.Y + 20, 22, Ink, UiWeight.Black);
        UiFonts.DrawCentered(label, k.X + kw + 12, y + 26, 24, Color.White, UiWeight.ExtraBold);
        return width;
    }

    private static float HintWidth(string key, string label) =>
        Math.Max(44, UiFonts.Measure(key, 22, UiWeight.Black) + 24) + UiFonts.Measure(label, 24, UiWeight.ExtraBold) + 34;

    /// <summary>The key hints of a screen, right-aligned so the last one ends at <paramref name="rightX"/>.</summary>
    public static void Hints(float rightX, float y, params (string Key, string Label)[] hints)
    {
        float x = rightX;
        for (int i = hints.Length - 1; i >= 0; i--)
        {
            x -= HintWidth(hints[i].Key, hints[i].Label);
            HintPill(x, y, hints[i].Key, hints[i].Label);
            x -= 20;
        }
    }

    /// <summary>
    /// A question over the dimmed screen with a row of buttons to answer it (leaving a save behind, closing the
    /// game). <paramref name="appear"/> slides it up into place.
    /// </summary>
    public static void Prompt(int sw, int sh, string title, string body, (string Label, Color Color)[] buttons, int selected, float appear = 1f)
    {
        Dim(sw, sh, (int)(150 * appear));
        float width = buttons.Length > 2 ? 1320 : 1040;
        var r = new Rectangle(sw / 2f - width / 2f, sh / 2f - 170 + (1f - appear) * 60f, width, 340);
        Panel(r, 36);
        UiFonts.Draw(title, r.X + 56, r.Y + 44, 48, Ink, UiWeight.Black);
        DrawWrapped(body, r.X + 56, r.Y + 116, r.Width - 112, 30, Muted, 40, UiWeight.ExtraBold);

        const float gap = 32;
        float bw = (r.Width - 112 - gap * (buttons.Length - 1)) / buttons.Length;
        for (int i = 0; i < buttons.Length; i++)
            Button(new Rectangle(r.X + 56 + i * (bw + gap), r.Y + 216, bw, 84), 42, buttons[i].Color, buttons[i].Label, 32, i == selected);
    }

    // ------------------------------------------------------------------ badges

    /// <summary>Sinnoh's eight Gym Badges in order (bit 0 of the badge mask is the Coal Badge).</summary>
    public static readonly string[] BadgeNames = { "Coal", "Forest", "Cobble", "Fen", "Relic", "Mine", "Icicle", "Beacon" };

    private static readonly Color[] BadgeColors =
    {
        new(150, 112, 92, 255), new(72, 172, 98, 255), new(218, 138, 76, 255), new(70, 138, 216, 255),
        new(150, 104, 200, 255), new(144, 156, 178, 255), new(120, 204, 232, 255), new(246, 198, 70, 255)
    };

    /// <summary>
    /// A badge as a small enamel medallion in the badge's colour with a simple mark (our own design, not the games'
    /// badge art); a badge not yet won is an empty socket.
    /// </summary>
    public static void Badge(Vector2 center, float r, int index, bool earned)
    {
        var slot = new Rectangle(center.X - r, center.Y - r, r * 2, r * 2);
        if (!earned)
        {
            UiShapes.Shape(slot, r, new Color(190, 200, 218, 255), new Color(206, 214, 230, 255), new Color(160, 172, 196, 255), 3f);
            return;
        }

        var color = BadgeColors[index % BadgeColors.Length];
        UiShapes.Shadow(slot, r, 8f, new Vector2(0, 3), ShadowColor);
        UiShapes.Shape(slot, r, Lighter(color, 0.35f), Darker(color, 0.15f), Darker(color, 0.5f), 3f);
        var white = new Color(255, 255, 255, 235);
        float m = r * 0.42f;
        switch (index % 4)
        {
            case 0: UiShapes.Circle(center, m * 0.7f, white); break;
            case 1: UiShapes.Fill(new Rectangle(center.X - m, center.Y - m * 0.32f, m * 2, m * 0.64f), m * 0.32f, white); break;
            case 2: UiShapes.Fill(new Rectangle(center.X - m * 0.32f, center.Y - m, m * 0.64f, m * 2), m * 0.32f, white); break;
            default:
                UiShapes.Circle(center, m * 0.9f, white);
                UiShapes.Circle(center, m * 0.45f, Darker(color, 0.1f));
                break;
        }
        // A glint on the upper left
        UiShapes.Circle(center + new Vector2(-r * 0.42f, -r * 0.45f), r * 0.16f, new Color(255, 255, 255, 150));
    }
}
