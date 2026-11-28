using System;
using System.Collections.Generic;
using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.UI.Kit;
using Raylib_cs;

namespace PokemonPlatinumEngine.UI;

// The cards of the people beaten (plan 43 · LF2; style guide, "Menu screens", "Trainer Card", "The cards of the
// people beaten"): the Trainer Card's last page, nine cards to a sheet, and one of them opened large.
internal static partial class ModernUi
{
    /// <summary>A small card's bust and an opened card's, in layout units (square).</summary>
    public const float AlbumBustSize = 166, AlbumLargeBustSize = 600;

    /// <summary>The most lines of a trainer's words an opened card holds.</summary>
    public const int AlbumLineRows = 4;

    /// <summary>
    /// Where the small cards of a sheet stand once the Trainer Card has risen into place, three to a row: what the page
    /// draws and the pointer asks (GPU-free).
    /// </summary>
    public static Rectangle[] AlbumCells(int sw, int count)
    {
        var card = new Rectangle(200, 150, sw - 400, 840);
        const float edge = 48, gapX = 24, gapY = 16, tall = 190, top = 200;
        float wide = (card.Width - edge * 2 - gapX * (TrainerCardScreen.CardColumns - 1)) / TrainerCardScreen.CardColumns;
        var cells = new Rectangle[Math.Min(count, TrainerCardScreen.CardsPerSheet)];
        for (int i = 0; i < cells.Length; i++)
            cells[i] = new Rectangle(card.X + edge + (i % TrainerCardScreen.CardColumns) * (wide + gapX),
                card.Y + top + (i / TrainerCardScreen.CardColumns) * (tall + gapY), wide, tall);
        return cells;
    }

    private static void CardAlbumPage(Rectangle card, IReadOnlyList<AlbumEntry> album, AlbumShown shown)
    {
        int cursor = Math.Clamp(shown.Cursor, 0, album.Count - 1);
        if (shown.Opened)
        {
            AlbumLarge(card, album[cursor], shown.Bust?.Invoke(album[cursor].Look, true));
            return;
        }
        float x = card.X + 48, right = card.X + card.Width - 48;
        UiFonts.Draw(Text.Of("cardAlbum.title"), x, card.Y + 154, 30, Ink, UiWeight.Black);
        string count = Text.Of("cardAlbum.count", ("count", album.Count));
        int sheets = (album.Count + TrainerCardScreen.CardsPerSheet - 1) / TrainerCardScreen.CardsPerSheet;
        if (sheets > 1)
            count = Text.Of("cardAlbum.sheet", ("page", cursor / TrainerCardScreen.CardsPerSheet + 1), ("pages", sheets)) + "   " + count;
        UiFonts.Draw(count, right - UiFonts.Measure(count, 26, UiWeight.ExtraBold), card.Y + 158, 26, Muted, UiWeight.ExtraBold);

        int first = cursor / TrainerCardScreen.CardsPerSheet * TrainerCardScreen.CardsPerSheet;
        // The cells are laid on the card where it stands, which is below its place while it rises
        var cells = AlbumCells((int)card.Width + 400, Math.Min(TrainerCardScreen.CardsPerSheet, album.Count - first));
        float risen = card.Y - 150;
        for (int i = 0; i < cells.Length; i++)
        {
            var cell = cells[i] with { Y = cells[i].Y + risen };
            AlbumSmall(cell, album[first + i], first + i == cursor, shown.Bust?.Invoke(album[first + i].Look, false));
        }
    }

    private static void AlbumSmall(Rectangle r, AlbumEntry entry, bool selected, Texture2D? bust)
    {
        var band = entry.Band;
        if (selected) UiShapes.Shadow(r, 26, 26f, Vector2.Zero, Selection with { A = 220 });
        UiShapes.Shape(r, 26, band.Base, band.Deep, selected ? Selection : Darker(band.Deep, 0.2f), selected ? 6 : 3);
        var box = new Rectangle(r.X + 12, r.Y + 12, AlbumBustSize, AlbumBustSize);
        AlbumBust(box, 18, band, bust);

        float x = box.X + box.Width + 16, wide = r.X + r.Width - 14 - x;
        float size = FitSize(entry.Name, wide, 36, UiWeight.Black);
        UiFonts.DrawCentered(entry.Name, x, r.Y + 42, size, Color.White, UiWeight.Black);
        UiFonts.Draw(entry.Rank, x, r.Y + 70, FitSize(entry.Rank, wide, 22, UiWeight.Black), Lighter(band.Light, 0.25f), UiWeight.Black);
        UiFonts.Draw(entry.Region, x, r.Y + 98, 20, new Color(255, 255, 255, 190), UiWeight.ExtraBold);

        // The team's icons on a pale strip along the card's foot, closer together the more there are
        var strip = new Rectangle(x - 6, r.Y + r.Height - 62, wide + 8, 50);
        UiShapes.Fill(strip, 14, new Color(255, 255, 255, 200));
        float step = Math.Min(44, (strip.Width - 44) / Math.Max(1, entry.Team.Count - 1));
        for (int i = 0; i < entry.Team.Count && i < Models.Party.MaxSize; i++)
            AlbumIcon(entry.Team[i].Model, new Vector2(strip.X + 22 + i * step, strip.Y + strip.Height / 2f), 1);
    }

    private static void AlbumLarge(Rectangle card, AlbumEntry entry, Texture2D? bust)
    {
        var band = entry.Band;
        var left = new Rectangle(card.X + 48, card.Y + 170, AlbumLargeBustSize + 24, AlbumLargeBustSize + 36);
        UiShapes.Shape(left, 30, band.Base, band.Deep, Darker(band.Deep, 0.2f), 4);
        AlbumBust(new Rectangle(left.X + 12, left.Y + 18, AlbumLargeBustSize, AlbumLargeBustSize), 24, band, bust);

        float x = left.X + left.Width + 40, right = card.X + card.Width - 48, wide = right - x;
        float y = card.Y + 172;
        UiFonts.Draw(entry.Name, x, y, FitSize(entry.Name, wide, 72, UiWeight.Black), Ink, UiWeight.Black);
        string under = entry.Rank + " · " + entry.Region;
        UiFonts.Draw(under, x, y + 90, FitSize(under, wide, 30, UiWeight.Black), Muted, UiWeight.Black);
        UiShapes.Fill(new Rectangle(x, y + 138, wide, 8), 4, band.Base);

        if (entry.Line.Length > 0)
        {
            var plate = new Rectangle(x, y + 160, wide, 170);
            UiShapes.Shape(plate, 24, PanelTop, PanelBottom, Rule, 3);
            var lines = Wrap("“" + entry.Line + "”", wide - 48, 30, UiWeight.ExtraBold);
            for (int i = 0; i < lines.Count && i < AlbumLineRows; i++)
                UiFonts.Draw(lines[i], x + 24, plate.Y + 16 + i * 36, 30, Ink, UiWeight.ExtraBold);
        }

        Label(Text.Of("cardAlbum.team"), x, y + 346);
        float cellWide = wide / 3f;
        for (int i = 0; i < entry.Team.Count && i < Models.Party.MaxSize; i++)
        {
            // (two rows end well inside the card's foot: the second row's names at 800 under the card's top)
            var c = new Vector2(x + (i % 3) * cellWide + cellWide / 2f, y + 416 + (i / 3) * 130);
            UiShapes.Circle(c, 48, Disc);
            UiShapes.Ring(c, 48, 3, band.Base);
            AlbumIcon(entry.Team[i].Model, c, 2);
            string species = entry.Team[i].Species;
            float size = FitSize(species, cellWide - 12, 22, UiWeight.Black);
            UiFonts.DrawCentered(species, c.X - UiFonts.Measure(species, size, UiWeight.Black) / 2f, c.Y + 64, size, Ink, UiWeight.Black);
        }
    }

    /// <summary>The largest size, from <paramref name="size"/> down, at which a text is no wider than <paramref name="wide"/>.</summary>
    private static float FitSize(string text, float wide, float size, UiWeight weight)
    {
        float measured = UiFonts.Measure(text, size, weight);
        return measured <= wide ? size : MathF.Floor(size * wide / measured);
    }

    private static void AlbumIcon(string model, Vector2 middle, int scale)
    {
        var icon = PixelArtGenerator.GetPokemonIcon(model);
        float w = icon.Width * scale, h = icon.Height * scale;
        Raylib.DrawTexturePro(icon, new Rectangle(0, 0, icon.Width, icon.Height),
            new Rectangle(MathF.Round(middle.X - w / 2f), MathF.Round(middle.Y - h / 2f), w, h), Vector2.Zero, 0f, Color.White);
    }

    /// <summary>A card's bust on its backing; a plain head and shoulders in the band's deep colour while there is no picture.</summary>
    private static void AlbumBust(Rectangle box, float radius, BattleBand band, Texture2D? bust)
    {
        UiShapes.Fill(box, radius, band.Backing);
        if (bust is { } picture)
        {
            Raylib.DrawTexturePro(picture, new Rectangle(0, 0, picture.Width, -picture.Height), box, Vector2.Zero, 0f, Color.White);
            return;
        }
        var plain = Darker(band.Deep, 0.2f) with { A = 200 };
        UiShapes.Circle(new Vector2(box.X + box.Width / 2f, box.Y + box.Height * 0.42f), box.Width * 0.2f, plain);
        UiShapes.Fill(new Rectangle(box.X + box.Width * 0.2f, box.Y + box.Height * 0.68f, box.Width * 0.6f, box.Height * 0.32f), box.Width * 0.14f, plain);
    }
}
