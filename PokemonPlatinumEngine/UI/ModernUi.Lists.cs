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

    /// <summary>
    /// A small menu on a panel of its own (the PC's menus): a row of 70 to an entry, the chosen one a filled pill,
    /// as the party's menu has them. Its top left corner is <paramref name="x"/>, <paramref name="y"/>; returns where it is.
    /// </summary>
    public static Rectangle MenuPanel(float x, float y, IReadOnlyList<string> rows, int selected, float width = 340)
    {
        var panel = new Rectangle(x, y, width, MenuPanelHeight(rows.Count));
        Panel(panel, 30);
        for (int i = 0; i < rows.Count; i++)
            ListRow(new Rectangle(panel.X + 14, panel.Y + 16 + i * 70, width - 28, 62), i == selected, rows[i], nameX: 40, size: 32);
        return panel;
    }

    public static float MenuPanelHeight(int rows) => 20 + rows * 70 + 12;

    // ------------------------------------------------------------------ signs

    /// <summary>A five-pointed star, its points <paramref name="radius"/> from its middle.</summary>
    public static void Star(Vector2 c, float radius, Color color)
    {
        float inner = radius * 0.45f;
        UiShapes.Circle(c, inner, color);
        for (int k = 0; k < 5; k++)
        {
            float a = -MathF.PI / 2f + k * MathF.PI * 2f / 5f, half = MathF.PI / 5f;
            var tip = c + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius;
            var left = c + new Vector2(MathF.Cos(a - half), MathF.Sin(a - half)) * inner;
            var right = c + new Vector2(MathF.Cos(a + half), MathF.Sin(a + half)) * inner;
            UiShapes.Triangle(tip, left, right, color);
        }
    }

    /// <summary>How many marks the boxes put on a Pokémon: circle, triangle, square, heart, star, diamond (<see cref="Markings"/>).</summary>
    public const int MarkCount = 6;

    /// <summary>One of the six marks (circle, triangle, square, heart, star, diamond), filling a square <paramref name="size"/> across.</summary>
    public static void MarkShape(Vector2 c, float size, int mark, Color color)
    {
        float h = size / 2f;
        switch (mark)
        {
            case 0:
                UiShapes.Circle(c, h * 0.84f, color);
                break;
            case 1:
                UiShapes.Triangle(new Vector2(c.X, c.Y - h * 0.84f), new Vector2(c.X + h * 0.9f, c.Y + h * 0.74f), new Vector2(c.X - h * 0.9f, c.Y + h * 0.74f), color, h * 0.06f);
                break;
            case 2:
                UiShapes.Fill(new Rectangle(c.X - h * 0.74f, c.Y - h * 0.74f, h * 1.48f, h * 1.48f), h * 0.18f, color);
                break;
            case 3:
                UiShapes.Circle(new Vector2(c.X - h * 0.42f, c.Y - h * 0.26f), h * 0.46f, color);
                UiShapes.Circle(new Vector2(c.X + h * 0.42f, c.Y - h * 0.26f), h * 0.46f, color);
                UiShapes.Triangle(new Vector2(c.X - h * 0.85f, c.Y - h * 0.1f), new Vector2(c.X + h * 0.85f, c.Y - h * 0.1f), new Vector2(c.X, c.Y + h * 0.84f), color);
                break;
            case 4:
                Star(c, h * 0.98f, color);
                break;
            default:
                // A square turned on its corner, a little taller than wide
                Rlgl.PushMatrix();
                Rlgl.Translatef(c.X, c.Y, 0f);
                Rlgl.Scalef(0.86f, 1.14f, 1f);
                UiShapes.Turned(Vector2.Zero, h * 0.62f, h * 0.62f, h * 0.1f, 45f, color);
                Rlgl.PopMatrix();
                break;
        }
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

    /// <summary>
    /// The one Pokémon a screen looks at (plan 09 · L5; style guide, "Pokémon in menus"): its strip breathing, a frame
    /// by <see cref="FrameClock"/>, at the same whole-number scale and place as <see cref="PixelArt"/> draws the still,
    /// which stands in for it until the strip is baked. Only the Pokédex's entry, the summary and the chosen starter
    /// call it; a list never does.
    /// </summary>
    public static void Breathing(Texture2D still, PokemonSprites.Strip? strip, Vector2 c, int scale, float hop = 0f)
    {
        if (strip is not { } s)
        {
            PixelArt(still, c, scale, hop);
            return;
        }
        float w = PokemonSprites.Size * scale, h = PokemonSprites.Size * scale;
        Raylib.DrawTexturePro(s.Texture, s.Source(s.FrameAt(FrameClock.Now)),
            new Rectangle(MathF.Round(c.X - w / 2f), MathF.Round(c.Y - h / 2f - hop), w, h), Vector2.Zero, 0f, Color.White);
    }

    /// <summary>How far a Pokémon's icon or sprite is lifted now, in its own pixels: the chosen one hops quickly and high, the rest gently.</summary>
    public static int Hop(bool selected, bool still = false)
    {
        if (still) return 0;
        float t = (float)FrameClock.Now;
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

    /// <summary>
    /// A setting as a card: its name on the left and its value on a pill between two arrows on the right (the
    /// options' rows, the Pokédex's search).
    /// </summary>
    public static void ValueRow(Rectangle r, string label, string value, bool selected, float pillWidth = 480)
    {
        Card(r, 30, selected);
        UiFonts.DrawCentered(label, r.X + 52, r.Y + r.Height / 2f, 36, Ink, UiWeight.ExtraBold);
        var pill = new Rectangle(r.X + r.Width - 140 - pillWidth, r.Y + 19, pillWidth, r.Height - 38);
        UiShapes.Fill(pill, pill.Height / 2f, selected ? Frame : new Color(214, 222, 236, 255));
        float vw = UiFonts.Measure(value, 32, UiWeight.Black);
        UiFonts.DrawCentered(value, pill.X + (pill.Width - vw) / 2f, pill.Y + pill.Height / 2f, 32, selected ? Color.White : Ink, UiWeight.Black);
        var arrow = selected ? Selection : new Color(170, 180, 200, 255);
        float cy = r.Y + r.Height / 2f;
        UiIcons.ArrowH(new Vector2(pill.X - 44, cy), 22, -1, arrow);
        UiIcons.ArrowH(new Vector2(pill.X + pill.Width + 44, cy), 22, 1, arrow);
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

    // ------------------------------------------------------------------ what to do next (plan 12 · Q14)

    /// <summary>The size and line height of the line that says what to do next, and the most lines it takes.</summary>
    public const float NextSize = 24, NextLineHeight = 32;
    public const int NextMaxLines = 4;

    private static List<string> NextLines(string line, float width)
    {
        var lines = Wrap(line, width, NextSize, UiWeight.ExtraBold);
        return lines.Count > NextMaxLines ? lines.GetRange(0, NextMaxLines) : lines;
    }

    /// <summary>The height of a panel that holds the line under its NEXT caption, for a text <paramref name="width"/> wide.</summary>
    public static float NextPanelHeight(string line, float width) => 28 + 30 + NextLines(line, width).Count * NextLineHeight + 12;

    /// <summary>
    /// What to do next, as the start menu, the Trainer Card and the save's summary show it: the caption NEXT in small
    /// capitals and the line under it, wrapped to <paramref name="r"/>'s width and cut at <see cref="NextMaxLines"/>.
    /// </summary>
    public static void NextLine(Rectangle r, string line)
    {
        Label(Text.Of("start.next"), r.X, r.Y);
        float y = r.Y + 30;
        foreach (var text in NextLines(line, r.Width))
        {
            UiFonts.Draw(text, r.X, y, NextSize, Ink, UiWeight.ExtraBold);
            y += NextLineHeight;
        }
    }

    /// <summary>
    /// A saved game at a glance, as the title's CONTINUE card and the save panel show it: who and where, time
    /// played, the Pokédex, badges won and the party. It draws its content only; the caller draws what it is on.
    /// </summary>
    /// <param name="slot">The slot the save is in (plan 12 · Q2), named on a small tag after the heading; nought for none.</param>
    public static void SaveSummary(Rectangle r, SaveData save, string heading, int slot = 0)
    {
        float x = r.X + 48;
        UiFonts.DrawCentered(heading, x, r.Y + 58, 44, Ink, UiWeight.Black);
        float headingEnd = x + UiFonts.Measure(heading, 44, UiWeight.Black);
        if (slot > 0) headingEnd += 20 + Tag(headingEnd + 20, r.Y + 43, SlotLabel(slot), Muted, 30);
        string where = PlaceOf(save);
        // A long place's name gives way to the heading and its tag rather than run into them
        float room = r.X + r.Width - 48 - headingEnd - 28, size = 28;
        float ww = UiFonts.Measure(where, size, UiWeight.ExtraBold);
        if (ww > room && ww > 0)
        {
            size = MathF.Max(20, MathF.Floor(size * room / ww));
            ww = UiFonts.Measure(where, size, UiWeight.ExtraBold);
        }
        UiFonts.DrawCentered(where, r.X + r.Width - 48 - ww, r.Y + 60, size, Muted, UiWeight.ExtraBold);
        UiShapes.Fill(new Rectangle(x, r.Y + 100, r.Width - 96, 3), 1.5f, Rule);

        Label(Text.Of("saving.player"), x, r.Y + 122);
        UiFonts.Draw(save.PlayerName, x, r.Y + 146, 40, Ink, UiWeight.Black);

        Label(Text.Of("saving.timePlayed"), x + 280, r.Y + 122);
        UiFonts.Draw(TitleScreen.FormatPlayTime(save.PlayTimeSeconds), x + 280, r.Y + 146, 40, Ink, UiWeight.Black);

        Label(Text.Of("saving.pokedex"), x + 540, r.Y + 122);
        UiFonts.Draw(save.CaughtSpecies.Count.ToString(), x + 540, r.Y + 146, 40, Ink, UiWeight.Black);

        // The badges of the region the game was saved in (plan 18 · W2), a medallion for each it has
        var (badges, of) = save.BadgesHere();
        Label(Text.Of("saving.badges", ("count", TitleScreen.CountBadges(badges)), ("of", of)), x, r.Y + 216);
        for (int b = 0; b < of; b++)
            Badge(new Vector2(x + 34 + b * 92, r.Y + 286), 34, b, (badges & (1 << b)) != 0, BadgeNameOf(save, b));
        // How the game is played, when it isn't Platinum's way (plan 13 · V1): a mark at the badges' row's right end
        if (SaveMark(save) is var (runMark, markColour))
            Tag(r.X + r.Width - 48 - UiFonts.Measure(runMark, 22, UiWeight.Black) - 31, r.Y + 204, runMark, markColour, 38);

        // The party as 2D sprites, like the main games' save panel
        Label(Text.Of("saving.party"), x, r.Y + 340);
        for (int i = 0; i < save.Party.Count && i < 6; i++)
        {
            var icon = PixelArtGenerator.GetPokemonIcon(PokemonLook.Of(save.Party[i].Form ?? save.Party[i].SpeciesName, save.Party[i].Gender == Gender.Female, save.Party[i].IsShiny));
            Raylib.DrawTexturePro(icon, new Rectangle(0, 0, icon.Width, icon.Height),
                new Rectangle(x + 96 + i * 100, r.Y + 346, icon.Width * 2, icon.Height * 2), Vector2.Zero, 0f, Color.White);
        }    }

    /// <summary>A slot as its tag and its caption name it: SLOT 2.</summary>
    public static string SlotLabel(int slot) => Text.Of("saving.slot", ("slot", slot));

    /// <summary>The place a game was saved in, as its card names it.</summary>
    /// <summary>The name of the badge in that place of the row of the region a save was made in, for its own look.</summary>
    private static string? BadgeNameOf(SaveData save, int place) =>
        RegionDatabase.Get(save.RegionId)?.BadgeNames is { } names && place < names.Count ? names[place] : null;

    public static string PlaceOf(SaveData save)
    {
        var place = save.Place();
        return MapDatabase.Get(place.Map).ShownNameAt(place.X, place.Y);
    }

    /// <summary>
    /// One of several saves on the title (plan 12 · Q2; style guide, "The slots"): the short of
    /// <see cref="SaveSummary"/> on a card 144 tall. Its slot over the player's name, the time played, the badges
    /// won, and along its foot the place it was saved in. A slot with no game (<paramref name="save"/> null: it
    /// can't be read) says so instead. It draws its content only; the caller draws the card.
    /// </summary>
    public static void SaveCard(Rectangle r, int slot, SaveData? save)
    {
        // (48 from the card's edges, as the rows' labels and the lone card's content are)
        float x = r.X + 48, right = r.X + r.Width - 48;
        Label(SlotLabel(slot), x, r.Y + 22);
        if (save == null)
        {
            UiFonts.Draw(Text.Of("saving.unreadable.heading"), x, r.Y + 48, 32, Ink, UiWeight.Black);
            UiFonts.Draw(Text.Of("saving.unreadable.body"), x, r.Y + 100, 24, Muted, UiWeight.ExtraBold);
            return;
        }
        UiFonts.Draw(save.PlayerName, x, r.Y + 44, 40, Ink, UiWeight.Black);

        Label(Text.Of("saving.timePlayed"), x + 300, r.Y + 22);
        UiFonts.Draw(TitleScreen.FormatPlayTime(save.PlayTimeSeconds), x + 300, r.Y + 44, 40, Ink, UiWeight.Black);

        // (The eighth medallion ends at the card's right margin: 504 + 13 + 7 × 28 + 13 = 726 of the 728 between them)
        var (badges, of) = save.BadgesHere();
        Label(Text.Of("saving.badges", ("count", TitleScreen.CountBadges(badges)), ("of", of)), x + 504, r.Y + 22);
        for (int b = 0; b < of; b++)
            Badge(new Vector2(x + 517 + b * 28, r.Y + 68), 13, b, (badges & (1 << b)) != 0, BadgeNameOf(save, b));

        // Along the foot: where it was saved (every game is played by the modern rules since plan 06 · R19, so no card
        // says whose rules it is)
        float room = right - x;
        string where = PlaceOf(save);
        float size = 24, ww = UiFonts.Measure(where, size, UiWeight.ExtraBold);
        if (ww > room && ww > 0) size = MathF.Max(18, MathF.Floor(size * room / ww));
        UiFonts.Draw(where, x, r.Y + 100, size, Muted, UiWeight.ExtraBold);
        // How it is played, when it isn't Platinum's way (plan 13 · V1), at the foot's right end
        if (SaveMark(save) is var (runMark, markColour))
            Tag(right - UiFonts.Measure(runMark, 20, UiWeight.Black) - 26, r.Y + 98, runMark, markColour, 34);
    }
}
