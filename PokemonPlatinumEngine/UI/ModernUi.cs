using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
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
        ratio > 0.5f ? new Color(70, 214, 110, 255) : ratio > Battle.BattleAnimator.LowHpRatio ? new Color(246, 196, 50, 255) : new Color(240, 72, 64, 255);

    public static void HpBar(float x, float y, float w, float h, float ratio)
    {
        ratio = Math.Clamp(ratio, 0f, 1f);
        float tagW = h * 2.3f;
        var tag = new Rectangle(x, y, tagW, h);
        UiShapes.Shape(tag, h / 2f, new Color(255, 200, 80, 255), new Color(236, 160, 40, 255));
        float ts = h * 0.72f;
        string hp = Text.Of("party.hp");
        UiFonts.DrawCentered(hp, x + (tagW - UiFonts.Measure(hp, ts, UiWeight.Black)) / 2f, y + h / 2f, ts, new Color(110, 60, 16, 255), UiWeight.Black);
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
        string lv = Text.Of("party.level");
        float lw = UiFonts.Measure(lv, size * 0.62f, UiWeight.ExtraBold);
        UiFonts.DrawCentered(lv, rightX - nw - lw - 4, centerY + size * 0.12f, size * 0.62f, Muted, UiWeight.ExtraBold);
        UiFonts.DrawCentered(n, rightX - nw, centerY, size, Ink, UiWeight.Black);
    }

    /// <summary>The name with the gender mark after it; returns the width used.</summary>
    public static float NameWithGender(Pokemon p, float x, float centerY, float size)
    {
        UiFonts.DrawCentered(p.DisplayName, x, centerY, size, Ink, UiWeight.Black);
        float w = UiFonts.Measure(p.DisplayName, size, UiWeight.Black);
        // An Egg shows no gender (plan 06 · R15)
        if (p.IsEgg || p.Gender is not (Gender.Male or Gender.Female)) return w;
        UiIcons.GenderMark(new Vector2(x + w + size * 0.5f, centerY), size * 0.74f, p.Gender);
        return w + size * 0.9f;
    }

    /// <summary>A type's name on a pill of its colour; returns the pill's width.</summary>
    public static float TypePill(float x, float y, PokemonType type, float height = 34f, float? width = null)
    {
        var color = Palette.GetTypeColor(type.ToString());
        string name = Words.Type(type).ToUpperInvariant();
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
        yield return p.PrimaryType;
        if (p.SecondaryType.HasValue) yield return p.SecondaryType.Value;
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
        string code = Words.Status(status);
        var color = status switch
        {
            StatusCondition.Poison or StatusCondition.Toxic => Palette.StatusPoison,
            StatusCondition.Burn => Palette.StatusBurn,
            StatusCondition.Paralyze => Palette.StatusParalyze,
            StatusCondition.Sleep => Palette.StatusSleep,
            StatusCondition.Freeze => Palette.StatusFreeze,
            _ => Palette.StatusFaint
        };
        float size = height * 0.6f;
        float textW = UiFonts.Measure(code, size, UiWeight.Black);
        float w = textW + height * 0.9f;
        UiShapes.Shape(new Rectangle(x, y, w, height), height / 2f, Lighter(color, 0.1f), Darker(color, 0.12f), Darker(color, 0.4f), 2f);
        UiFonts.DrawCentered(code, x + (w - textW) / 2f, y + height / 2f, size, Color.White, UiWeight.Black);
        return w;
    }

    /// <summary>Pokérus's colour: the tag of a Pokémon that carries it (style guide, "The party's menus").</summary>
    public static readonly Color PokerusColor = new(16, 132, 124, 255);

    /// <summary>A PKRS tag while the Pokémon carries Pokérus (plan 06 · R10); returns its width, 0 for none.</summary>
    public static float PokerusTag(float x, float y, Pokemon p, float height = 34f) =>
        PokerusRules.Tag(p) is { } text ? Tag(x, y, text, PokerusColor, height) : 0f;

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
        var icon = PixelArtGenerator.IconOf(p);
        float t = (float)FrameClock.Now;
        // An Egg sits still but for a slow rock (plan 06 · R15)
        int hop = p.IsEgg ? ((int)(t / 0.8f) % 2) * scale : p.IsFainted ? 0 : selected ? ((int)(t / 0.16f) % 2) * 3 * scale : ((int)(t / 0.4f) % 2) * scale;
        Raylib.DrawTexturePro(icon, new Rectangle(0, 0, icon.Width, icon.Height),
            new Rectangle(MathF.Round(c.X - icon.Width * scale / 2f), MathF.Round(c.Y - icon.Height * scale / 2f - 1.5f * scale - hop), icon.Width * scale, icon.Height * scale),
            Vector2.Zero, 0, p.IsFainted && !p.IsEgg ? new Color(170, 170, 190, 255) : Color.White);
    }

    // ------------------------------------------------------------------ text

    /// <summary>Breaks text into lines no wider than <paramref name="maxWidth"/>; "\n" forces a break.</summary>
    public static List<string> Wrap(string text, float maxWidth, float size, UiWeight weight = UiWeight.Bold) =>
        // The one way of wrapping, which a check with no window uses with a measure of its own (plan 16 · T8)
        TextMetrics.Wrap(text, maxWidth, line => UiFonts.Measure(line, size, weight));

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
    public static float HintPill(float x, float y, string key, string label) => HintPill(x, y, new HintItem(key, label));

    /// <summary>A key cap (the key's name, or the pad's sign once the player is on the pad) and what it does; returns its width.</summary>
    public static float HintPill(float x, float y, HintItem hint)
    {
        float width = HintWidth(hint);
        float kw = CapWidth(hint);
        UiShapes.Fill(new Rectangle(x, y, width, 52), 26, new Color(10, 30, 60, 90));
        var k = new Rectangle(x + 6, y + 6, kw, 40);
        UiShapes.Fill(k, 20, Color.White);
        if (hint.Glyph is { } glyph) PadGlyphArt.Draw(glyph, new Vector2(k.X + kw / 2f, k.Y + 20));
        else UiFonts.DrawCentered(hint.Key, k.X + (kw - UiFonts.Measure(hint.Key, 22, UiWeight.Black)) / 2f, k.Y + 20, 22, Ink, UiWeight.Black);
        UiFonts.DrawCentered(hint.Label, k.X + kw + 12, y + 26, 24, Color.White, UiWeight.ExtraBold);
        return width;
    }

    private static float CapWidth(HintItem hint) => hint.Glyph is { } glyph
        ? PadGlyphArt.Width(glyph) + 8
        : Math.Max(44, UiFonts.Measure(hint.Key, 22, UiWeight.Black) + 24);

    private static float HintWidth(HintItem hint) => CapWidth(hint) + UiFonts.Measure(hint.Label, 24, UiWeight.ExtraBold) + 34;

    /// <summary>A key hint whose key and label are both keys of the string table (<c>shared.keys.z</c>, <c>shared.hints.back</c>); the key follows a rebinding and the pad.</summary>
    public static HintItem Hint(string key, string label) => new(Text.Of(key), Text.Of(label), Prompts.GlyphFor(key));

    /// <summary>A key hint whose label is a text already worked out (the key is a key of the table).</summary>
    public static HintItem HintOf(string key, string label) => new(Text.Of(key), label, Prompts.GlyphFor(key));

    /// <summary>The key hints of a screen, right-aligned so the last one ends at <paramref name="rightX"/>.</summary>
    public static void Hints(float rightX, float y, params HintItem[] hints)
    {
        float x = rightX;
        for (int i = hints.Length - 1; i >= 0; i--)
        {
            x -= HintWidth(hints[i]);
            HintPill(x, y, hints[i]);
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

    /// <summary>
    /// A notice to be read and put away (plan 12 · Q1: the title's word on a damaged save, which plan 16 · T3's word
    /// after a crash shares): the prompt's panel, room for three lines of words, and one button, highlighted, at its
    /// bottom right.
    /// </summary>
    public static void Notice(int sw, int sh, string title, string body, string button, float appear = 1f)
    {
        Dim(sw, sh, (int)(150 * appear));
        const float width = 1040, height = 380, bw = 280;
        var r = new Rectangle(sw / 2f - width / 2f, sh / 2f - height / 2f + (1f - appear) * 60f, width, height);
        Panel(r, 36);
        UiFonts.Draw(title, r.X + 56, r.Y + 44, 48, Ink, UiWeight.Black);
        DrawWrapped(body, r.X + 56, r.Y + 116, r.Width - 112, 30, Muted, 40, UiWeight.ExtraBold);
        Button(new Rectangle(r.X + r.Width - 56 - bw, r.Y + height - 124, bw, 84), 42, Blue, button, 32, true);
    }

    // ------------------------------------------------------------------ badges

    private static readonly Color[] BadgeColors =
    {
        new(150, 112, 92, 255), new(72, 172, 98, 255), new(218, 138, 76, 255), new(70, 138, 216, 255),
        new(150, 104, 200, 255), new(144, 156, 178, 255), new(120, 204, 232, 255), new(246, 198, 70, 255)
    };

    /// <summary>
    /// A badge as a small enamel medallion in the badge's colour with a simple mark (our own design, not the games'
    /// badge art); a badge not yet won is an empty socket.
    /// </summary>
    public static void Badge(Vector2 center, float r, int index, bool earned) => Badge(center, r, index, earned, null);

    /// <summary>The marks a region's own badges carry (style guide, "Badges"): a badge whose name is here has its own look.</summary>
    public enum BadgeMark { Stone, Drop, Bolt, Rings, Heart, Twins, Flame, Leaf }

    /// <summary>The badges with a look of their own, by the name their region's file gives them (plan 19 · K4: Kanto's eight).</summary>
    public static readonly IReadOnlyDictionary<string, (Color Color, BadgeMark Mark)> BadgeLooks =
        new Dictionary<string, (Color, BadgeMark)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Boulder"] = (new Color(150, 146, 140, 255), BadgeMark.Stone),
            ["Cascade"] = (new Color(64, 150, 222, 255), BadgeMark.Drop),
            ["Thunder"] = (new Color(236, 160, 52, 255), BadgeMark.Bolt),
            ["Rainbow"] = (new Color(156, 110, 214, 255), BadgeMark.Rings),
            ["Soul"] = (new Color(222, 92, 140, 255), BadgeMark.Heart),
            ["Marsh"] = (new Color(214, 178, 68, 255), BadgeMark.Twins),
            ["Volcano"] = (new Color(220, 72, 54, 255), BadgeMark.Flame),
            ["Earth"] = (new Color(82, 160, 86, 255), BadgeMark.Leaf)
        };

    /// <summary>
    /// A badge as <see cref="Badge(Vector2, float, int, bool)"/> draws it, in its own colour and with its own mark where its
    /// <paramref name="name"/> has a look of its own (<see cref="BadgeLooks"/>).
    /// </summary>
    public static void Badge(Vector2 center, float r, int index, bool earned, string? name)
    {
        var slot = new Rectangle(center.X - r, center.Y - r, r * 2, r * 2);
        if (!earned)
        {
            UiShapes.Shape(slot, r, new Color(190, 200, 218, 255), new Color(206, 214, 230, 255), new Color(160, 172, 196, 255), 3f);
            return;
        }

        bool own = name != null && BadgeLooks.ContainsKey(name);
        var color = own ? BadgeLooks[name!].Color : BadgeColors[index % BadgeColors.Length];
        UiShapes.Shadow(slot, r, 8f, new Vector2(0, 3), ShadowColor);
        UiShapes.Shape(slot, r, Lighter(color, 0.35f), Darker(color, 0.15f), Darker(color, 0.5f), 3f);
        var white = new Color(255, 255, 255, 235);
        float m = r * 0.42f;
        if (own)
        {
            DrawBadgeMark(center, m, BadgeLooks[name!].Mark, white, Darker(color, 0.1f));
            UiShapes.Circle(center + new Vector2(-r * 0.42f, -r * 0.45f), r * 0.16f, new Color(255, 255, 255, 150));
            return;
        }
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

    /// <summary>A region's own badge mark (style guide, "Badges"), <paramref name="m"/> its half size, in white over the enamel.</summary>
    private static void DrawBadgeMark(Vector2 c, float m, BadgeMark mark, Color white, Color enamel)
    {
        switch (mark)
        {
            case BadgeMark.Stone:
                UiShapes.Fill(new Rectangle(c.X - m, c.Y - m * 0.15f, m * 2, m * 0.9f), m * 0.3f, white);
                UiShapes.Fill(new Rectangle(c.X - m * 0.55f, c.Y - m * 0.85f, m * 1.1f, m * 0.8f), m * 0.28f, white);
                break;
            case BadgeMark.Drop:
                UiShapes.Circle(c + new Vector2(0, m * 0.3f), m * 0.62f, white);
                UiShapes.Triangle(c + new Vector2(0, -m * 0.95f), c + new Vector2(-m * 0.55f, m * 0.05f), c + new Vector2(m * 0.55f, m * 0.05f), white, m * 0.05f);
                break;
            case BadgeMark.Bolt:
                UiShapes.Line(c + new Vector2(m * 0.45f, -m * 0.95f), c + new Vector2(-m * 0.3f, m * 0.05f), m * 0.42f, white);
                UiShapes.Line(c + new Vector2(-m * 0.3f, m * 0.05f), c + new Vector2(m * 0.3f, -m * 0.05f), m * 0.42f, white);
                UiShapes.Line(c + new Vector2(m * 0.3f, -m * 0.05f), c + new Vector2(-m * 0.45f, m * 0.95f), m * 0.42f, white);
                break;
            case BadgeMark.Rings:
                UiShapes.Ring(c, m, m * 0.24f, white);
                UiShapes.Ring(c, m * 0.62f, m * 0.22f, white);
                UiShapes.Circle(c, m * 0.24f, white);
                break;
            case BadgeMark.Heart:
                UiShapes.Circle(c + new Vector2(-m * 0.42f, -m * 0.25f), m * 0.5f, white);
                UiShapes.Circle(c + new Vector2(m * 0.42f, -m * 0.25f), m * 0.5f, white);
                UiShapes.Triangle(c + new Vector2(-m * 0.9f, -m * 0.08f), c + new Vector2(m * 0.9f, -m * 0.08f), c + new Vector2(0, m * 0.95f), white, m * 0.04f);
                break;
            case BadgeMark.Twins:
                UiShapes.Circle(c + new Vector2(-m * 0.4f, 0), m * 0.58f, white);
                UiShapes.Circle(c + new Vector2(m * 0.4f, 0), m * 0.58f, white);
                UiShapes.Circle(c + new Vector2(m * 0.4f, 0), m * 0.24f, enamel);
                break;
            case BadgeMark.Flame:
                UiShapes.Circle(c + new Vector2(0, m * 0.35f), m * 0.6f, white);
                UiShapes.Triangle(c + new Vector2(m * 0.1f, -m), c + new Vector2(-m * 0.58f, m * 0.25f), c + new Vector2(m * 0.58f, m * 0.25f), white, m * 0.05f);
                UiShapes.Circle(c + new Vector2(0, m * 0.45f), m * 0.26f, enamel);
                break;
            default:
                UiShapes.Turned(c, m * 0.62f, m * 0.62f, m * 0.12f, 45f, white);
                UiShapes.Line(c + new Vector2(-m * 0.5f, m * 0.5f), c + new Vector2(m * 0.5f, -m * 0.5f), m * 0.12f, enamel);
                break;
        }
    }
}

/// <summary>A key hint as the screens hand it over: the cap's name, what it does and, on the pad, the sign to draw in the cap (plan 12 · Q5).</summary>
internal readonly record struct HintItem(string Key, string Label, PadGlyph? Glyph = null)
{
    public static implicit operator HintItem((string Key, string Label) pair) => new(pair.Key, pair.Label);
}
