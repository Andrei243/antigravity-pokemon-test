using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The parts the menu screens share (style guide, "Menu screens"): list rows, scroll bars, tabs, the pale disc
/// things are shown on, money, the stepper, and the summary of a save.
/// </summary>
internal static partial class ModernUi
{
    public static readonly Color Disc = new(226, 234, 246, 255);

    /// <summary>Where a full-screen menu's content begins and ends, and its margins.</summary>
    public const float ContentTop = 132, ContentBottom = 1032, Margin = 64, Gutter = 32;

    /// <summary>How tall a list row is, and how far apart two are from top to top.</summary>
    public const float RowHeight = 84, RowPitch = 92;

    /// <summary>How many whole rows fit in a list panel of this height (with its 24 of padding above and below).</summary>
    public static int RowsIn(float panelHeight) => Math.Max(1, (int)((panelHeight - 48 + (RowPitch - RowHeight)) / RowPitch));

    /// <summary>The rectangle of a list's row, counted from the first one showing.</summary>
    public static Rectangle RowRect(Rectangle panel, int row, float top = 24, bool scrolls = false) =>
        new(panel.X + 20, panel.Y + top + row * RowPitch, panel.Width - 40 - (scrolls ? 22 : 0), RowHeight);

    // ------------------------------------------------------------------ lists

    /// <summary>
    /// One row of a list: the chosen one a Selection-coloured pill with white text, the others bare. Returns the
    /// colour its text is drawn in, for whatever the caller adds at its right.
    /// </summary>
    public static Color ListRow(Rectangle r, bool selected, string name, bool enabled = true, float nameX = 104, float size = 34)
    {
        Color ink = selected ? Color.White : enabled ? Ink : Muted;
        if (selected)
        {
            UiShapes.Shadow(r, 26, 18, new Vector2(0, 5), Selection with { A = 120 });
            UiShapes.Shape(r, 26, Lighter(Selection, 0.14f), Darker(Selection, 0.06f), Darker(Selection, 0.3f), 3);
        }
        UiFonts.DrawCentered(name, r.X + nameX, r.Y + r.Height / 2f, size, ink, UiWeight.Black);
        return ink;
    }

    /// <summary>What a row is about, on a pale disc at its left: pixel art at a whole-number scale.</summary>
    public static void RowIcon(Rectangle r, Texture2D icon, float scale, bool selected, bool faded = false, float hop = 0f)
    {
        var c = new Vector2(r.X + 52, r.Y + r.Height / 2f);
        UiShapes.Circle(c, 34, selected ? Color.White : Disc);
        float w = icon.Width * scale, h = icon.Height * scale;
        Raylib.DrawTexturePro(icon, new Rectangle(0, 0, icon.Width, icon.Height),
            new Rectangle(MathF.Round(c.X - w / 2f), MathF.Round(c.Y - h / 2f - hop), w, h), Vector2.Zero, 0f,
            faded ? new Color(255, 255, 255, 110) : Color.White);
    }

    /// <summary>A row's value, right-aligned.</summary>
    public static void RowValue(Rectangle r, string text, Color ink, float size = 30)
    {
        float w = UiFonts.Measure(text, size, UiWeight.Black);
        UiFonts.DrawCentered(text, r.X + r.Width - 32 - w, r.Y + r.Height / 2f, size, ink, UiWeight.Black);
    }

    /// <summary>Where a list's window is, when there is more than fits: a thin track at the panel's right edge with a thumb.</summary>
    public static void ScrollBar(Rectangle panel, int first, int visible, int count, float top = 24)
    {
        if (count <= visible) return;
        var track = new Rectangle(panel.X + panel.Width - 26, panel.Y + top, 10, visible * RowPitch - (RowPitch - RowHeight));
        UiShapes.Fill(track, 5, Rule);
        float size = Math.Max(40f, track.Height * visible / count);
        float at = (track.Height - size) * first / Math.Max(1, count - visible);
        UiShapes.Fill(new Rectangle(track.X, track.Y + at, track.Width, size), 5, Frame);
    }

    /// <summary>A line in the middle of an empty list.</summary>
    public static void EmptyNote(Rectangle panel, string text)
    {
        float w = UiFonts.Measure(text, 32, UiWeight.ExtraBold);
        UiFonts.DrawCentered(text, panel.X + (panel.Width - w) / 2f, panel.Y + panel.Height / 2f, 32, Muted, UiWeight.ExtraBold);
    }

    // ------------------------------------------------------------------ tabs

    /// <summary>
    /// Tabs in a row across <paramref name="strip"/>: each a pill with its sign on a chip in its own colour and
    /// its name; the chosen one is filled with its colour.
    /// </summary>
    public static void Tabs(Rectangle strip, IReadOnlyList<(string Label, UiIcon Icon, Color Color)> tabs, int selected)
    {
        const float gap = 12;
        float w = (strip.Width - gap * (tabs.Count - 1)) / tabs.Count;
        for (int i = 0; i < tabs.Count; i++)
        {
            var (label, icon, color) = tabs[i];
            var r = new Rectangle(strip.X + i * (w + gap), strip.Y, w, strip.Height);
            var chip = new Vector2(r.X + 12 + 26, r.Y + r.Height / 2f);
            if (i == selected)
            {
                UiShapes.Shadow(r, r.Height / 2f, 20, new Vector2(0, 5), color with { A = 130 });
                UiShapes.Shape(r, r.Height / 2f, Lighter(color, 0.16f), Darker(color, 0.08f), Darker(color, 0.32f), 3);
                UiShapes.Circle(chip, 26, Color.White);
                UiIcons.Draw(icon, chip, 13, color, Color.White);
            }
            else
            {
                UiShapes.Shadow(r, r.Height / 2f, 14, new Vector2(0, 5), new Color(6, 14, 34, 80));
                UiShapes.Shape(r, r.Height / 2f, PanelTop, PanelBottom, Frame, 3);
                UiShapes.Circle(chip, 26, color);
                UiIcons.Draw(icon, chip, 13, Color.White, color);
            }
            float size = 22, room = w - 76 - 16, textW = UiFonts.Measure(label, size, UiWeight.Black);
            if (textW > room) size = MathF.Floor(size * room / textW);
            UiFonts.DrawCentered(label, r.X + 76, r.Y + r.Height / 2f, size, i == selected ? Color.White : Ink, UiWeight.Black);
        }
    }

    // ------------------------------------------------------------------ what is shown large

    /// <summary>The pale disc with a faint Poké Ball line that a Pokémon's sprite stands on.</summary>
    public static void BallDisc(Vector2 c, float radius)
    {
        UiShapes.Circle(c, radius, Disc);
        float line = MathF.Max(4f, radius * 0.04f);
        UiShapes.Fill(new Rectangle(c.X - radius, c.Y - line / 2f, radius * 2, line), line / 2f, Rule);
        UiShapes.Circle(c, radius * 0.27f, Rule);
        UiShapes.Circle(c, radius * 0.18f, Disc);
    }

    /// <summary>Pixel art centred on a point at a whole-number scale, lifted by <paramref name="hop"/> layout units.</summary>
    public static void PixelArt(Texture2D art, Vector2 c, int scale, float hop = 0f, Color? tint = null)
    {
        float w = art.Width * scale, h = art.Height * scale;
        Raylib.DrawTexturePro(art, new Rectangle(0, 0, art.Width, art.Height),
            new Rectangle(MathF.Round(c.X - w / 2f), MathF.Round(c.Y - h / 2f - hop), w, h), Vector2.Zero, 0f, tint ?? Color.White);
    }

    /// <summary>How far a Pokémon's icon or sprite is lifted now, in its own pixels: the chosen one hops quickly and high, the rest gently.</summary>
    public static int Hop(bool selected, bool still = false)
    {
        if (still) return 0;
        float t = (float)Raylib.GetTime();
        return selected ? ((int)(t / 0.16f) % 2) * 3 : (int)(t / 0.4f) % 2;
    }

    /// <summary>An item's icon on a pale disc, six times its art's size.</summary>
    public static void ItemDisc(Vector2 c, ItemData item)
    {
        UiShapes.Circle(c, 96, Disc);
        UiShapes.Ring(c, 96, 5, Rule);
        var icon = PixelArtGenerator.GetItemIcon(item);
        // The icon's texture is its 20 by 20 art doubled: three times that is six times the art
        PixelArt(icon, c, 3);
    }

    // ------------------------------------------------------------------ money and numbers

    /// <summary>Money: our own mark (a P crossed by two bars) and the amount. Returns the width drawn.</summary>
    public static float Money(float x, float centerY, int amount, float size, Color color)
    {
        float markW = UiFonts.Measure("P", size, UiWeight.Black);
        UiFonts.DrawCentered("P", x, centerY, size, color, UiWeight.Black);
        float bar = MathF.Max(2f, size * 0.075f);
        for (int i = 0; i < 2; i++)
        {
            float y = centerY + size * (0.1f + i * 0.17f);
            UiShapes.Line(new Vector2(x - size * 0.1f, y), new Vector2(x + size * 0.3f, y), bar, color);
        }
        string text = amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        UiFonts.DrawCentered(text, x + markW + size * 0.14f, centerY, size, color, UiWeight.Black);
        return MoneyWidth(amount, size);
    }

    public static float MoneyWidth(int amount, float size) =>
        UiFonts.Measure("P", size, UiWeight.Black) + size * 0.14f
        + UiFonts.Measure(amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), size, UiWeight.Black);

    /// <summary>Money ending at <paramref name="rightX"/>.</summary>
    public static void MoneyRight(float rightX, float centerY, int amount, float size, Color color) =>
        Money(rightX - MoneyWidth(amount, size), centerY, amount, size, color);

    /// <summary>A number on a Frame-coloured pill between two arrows, centred on a point.</summary>
    public static void Stepper(Vector2 c, string text, float width = 240, float height = 84)
    {
        var pill = new Rectangle(c.X - width / 2f, c.Y - height / 2f, width, height);
        UiShapes.Fill(pill, height / 2f, Frame);
        float w = UiFonts.Measure(text, 44, UiWeight.Black);
        UiFonts.DrawCentered(text, c.X - w / 2f, c.Y, 44, Color.White, UiWeight.Black);
        UiIcons.ArrowH(new Vector2(pill.X - 44, c.Y), 24, -1, Selection);
        UiIcons.ArrowH(new Vector2(pill.X + pill.Width + 44, c.Y), 24, 1, Selection);
    }

    /// <summary>A caption in capitals with its value under it; returns the x where the next one can go.</summary>
    public static float Field(string caption, string value, float x, float y, float size = 40, float gap = 56)
    {
        Label(caption, x, y);
        UiFonts.Draw(value, x, y + 24, size, Ink, UiWeight.Black);
        return x + Math.Max(UiFonts.Measure(caption, 20, UiWeight.Black), UiFonts.Measure(value, size, UiWeight.Black)) + gap;
    }

    // ------------------------------------------------------------------ a save at a glance

    public const float SaveSummaryHeight = 460;

    /// <summary>
    /// A saved game at a glance, as the title's CONTINUE card and the save panel show it: who and where, time
    /// played, the Pokédex, badges won and the party. It draws its content only; the caller draws what it is on.
    /// </summary>
    public static void SaveSummary(Rectangle r, SaveData save, string heading)
    {
        float x = r.X + 48;
        UiFonts.DrawCentered(heading, x, r.Y + 58, 44, Ink, UiWeight.Black);
        var place = save.Place();
        string where = MapDatabase.Get(place.Map).DisplayNameAt(place.X, place.Y);
        float ww = UiFonts.Measure(where, 28, UiWeight.ExtraBold);
        UiFonts.DrawCentered(where, r.X + r.Width - 48 - ww, r.Y + 60, 28, Muted, UiWeight.ExtraBold);
        UiShapes.Fill(new Rectangle(x, r.Y + 100, r.Width - 96, 3), 1.5f, Rule);

        Label("PLAYER", x, r.Y + 122);
        UiFonts.Draw(save.PlayerName, x, r.Y + 146, 40, Ink, UiWeight.Black);

        Label("TIME PLAYED", x + 280, r.Y + 122);
        UiFonts.Draw(TitleScreen.FormatPlayTime(save.PlayTimeSeconds), x + 280, r.Y + 146, 40, Ink, UiWeight.Black);

        Label("POKÉDEX", x + 540, r.Y + 122);
        UiFonts.Draw(save.CaughtSpecies.Count.ToString(), x + 540, r.Y + 146, 40, Ink, UiWeight.Black);

        Label($"BADGES  {TitleScreen.CountBadges(save.Badges)} / 8", x, r.Y + 216);
        for (int b = 0; b < 8; b++)
            Badge(new Vector2(x + 34 + b * 92, r.Y + 286), 34, b, (save.Badges & (1 << b)) != 0);

        // The party as 2D sprites, like the main games' save panel
        Label("PARTY", x, r.Y + 340);
        for (int i = 0; i < save.Party.Count && i < 6; i++)
        {
            var icon = PixelArtGenerator.GetPokemonIcon(save.Party[i].SpeciesName);
            Raylib.DrawTexturePro(icon, new Rectangle(0, 0, icon.Width, icon.Height),
                new Rectangle(x + 96 + i * 100, r.Y + 346, icon.Width * 2, icon.Height * 2), Vector2.Zero, 0f, Color.White);
        }
    }
}
