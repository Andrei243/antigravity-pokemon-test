using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The photo book (plan 43 · LF4; style guide, "Menu screens", "The photo book").</summary>
internal static partial class ModernUi
{
    /// <summary>A region's name as the book's tab writes it: in capitals.</summary>
    public static string PhotoRegionName(string region) =>
        RegionDatabase.Get(region) is { } r ? Text.Of($"dexText.region.{r.Generation}").ToUpperInvariant() : region.ToUpperInvariant();

    /// <summary>Where a photograph's place stands in words: a spot's name, else the map's place.</summary>
    public static string PhotoPlace(PhotoPrint print) =>
        print.Spot != null ? Text.Of($"photoBook.spot.{print.Spot}") : print.Place;

    /// <summary>
    /// The book: tabs (ALL, then each region), the page's six photographs three across on cream mats with their place
    /// under each, the page and the count at the foot; a photograph opened large over the dimmed page, and the
    /// throwing away's question over that. <paramref name="pictureOf"/> gives a loaded picture by file (the small copy on
    /// the page, the whole one opened), or null for one not loaded or lost, drawn as a stand-in card.
    /// </summary>
    public static void DrawPhotoBook(int sw, int sh, PhotoBookScreen screen, Func<string, Texture2D?> pictureOf)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("photoBook.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.arrows", "shared.hints.move"), Hint("shared.keys.z", "shared.hints.open"),
            Hint("shared.keys.c", "photoBook.throwHint"), Hint("shared.keys.esc", "shared.hints.back"));
        var tabs = new List<(string, UiIcon, Color)> { (Text.Of("photoBook.all"), UiIcon.Star, Blue) };
        foreach (string region in screen.Tabs) tabs.Add((PhotoRegionName(region), UiIcon.Pin, Green));
        Tabs(new Rectangle(Margin, ContentTop, sw - Margin * 2, 76), tabs.ToArray(), screen.Tab);

        var page = screen.PageNow;
        const float top = ContentTop + 100, cellW = 560, cellH = 392, gap = 32, rowGap = 20;
        float left = (sw - (cellW * PhotoBookScreen.Columns + gap * (PhotoBookScreen.Columns - 1))) / 2f;
        if (page.Count == 0)
            UiFonts.DrawCentered(Text.Of("photoBook.empty"), Margin + 40, top + 200, 34, Color.White, UiWeight.Black);
        for (int i = 0; i < page.Count; i++)
        {
            int at = screen.Page * PhotoBookScreen.PerPage + i;
            var cell = new Rectangle(left + (i % PhotoBookScreen.Columns) * (cellW + gap), top + (i / PhotoBookScreen.Columns) * (cellH + rowGap), cellW, cellH);
            bool selected = at == screen.Index && !screen.Opened;
            UiShapes.Shape(cell, 18, new Color(252, 248, 238, 255), new Color(240, 234, 220, 255), selected ? Selection : Rule, selected ? 6 : 3);
            var picture = new Rectangle(cell.X + 20, cell.Y + 20, cellW - 40, (cellW - 40) * 9f / 16f);
            PhotoPicture(picture, page[i], pictureOf(page[i].SmallFile));
            string place = PhotoPlace(page[i]);
            UiFonts.Draw(place, cell.X + 22, picture.Y + picture.Height + 10, 26, Ink, UiWeight.Black);
            UiFonts.Draw(Words.Date(page[i].Day), cell.X + 22, picture.Y + picture.Height + 44, 20, Muted, UiWeight.Bold);
            if (page[i].Spot != null) Tag(cell.X + cellW - 22 - UiFonts.Measure(Text.Of("photoBook.spotTag"), 20, UiWeight.Black) - 34, picture.Y + picture.Height + 12, Text.Of("photoBook.spotTag"), Gold, 34);
        }
        string foot = Text.Of("photoBook.foot", ("page", screen.Page + 1), ("pages", screen.Pages), ("count", screen.Total), ("of", PhotoBook.Capacity));
        UiFonts.Draw(foot, sw - Margin - UiFonts.Measure(foot, 24, UiWeight.Black), ContentBottom - 20, 24, new Color(220, 228, 246, 255), UiWeight.Black);

        if (screen.Opened && screen.Selected is { } open)
        {
            Dim(sw, sh, 190);
            var big = new Rectangle(sw / 2f - 760, 120, 1520, 855);
            UiShapes.Fill(new Rectangle(big.X - 18, big.Y - 18, big.Width + 36, big.Height + 96), 22, new Color(252, 248, 238, 255));
            PhotoPicture(big, open, pictureOf(open.File));
            UiFonts.Draw(PhotoPlace(open), big.X, big.Y + big.Height + 22, 34, Ink, UiWeight.Black);
            string day = Words.Date(open.Day);
            UiFonts.Draw(day, big.X + big.Width - UiFonts.Measure(day, 28, UiWeight.Bold), big.Y + big.Height + 26, 28, Muted, UiWeight.Bold);
            if (screen.Asking)
            {
                var r = new Rectangle(sw / 2f - 480, sh / 2f - 150, 960, 300);
                Panel(r, 36);
                UiFonts.Draw(Text.Of("photoBook.throwQuestion"), r.X + 56, r.Y + 50, 40, Ink, UiWeight.Black);
                float bw = (r.Width - 112 - 32) / 2f;
                string[] answers = { Text.Of("photoBook.throwYes"), Text.Of("outfits.answer.no") };
                for (int i = 0; i < 2; i++)
                    Button(new Rectangle(r.X + 56 + i * (bw + 32), r.Y + 170, bw, 84), 42, i == 0 ? Red : Blue, answers[i], 32, i == screen.AskIndex);
            }
        }
    }

    /// <summary>A photograph's picture in its place: the picture, or for one lost a stand-in card of sky and ground with a camera on it.</summary>
    private static void PhotoPicture(Rectangle r, PhotoPrint print, Texture2D? picture)
    {
        if (picture is { } t)
        {
            Raylib.DrawTexturePro(t, new Rectangle(0, 0, t.Width, t.Height), r, Vector2.Zero, 0f, Color.White);
            return;
        }
        UiShapes.Shape(new Rectangle(r.X, r.Y, r.Width, r.Height * 0.62f), 0, new Color(150, 196, 236, 255), new Color(196, 224, 246, 255));
        UiShapes.Shape(new Rectangle(r.X, r.Y + r.Height * 0.62f, r.Width, r.Height * 0.38f), 0, new Color(126, 186, 110, 255), new Color(98, 160, 90, 255));
        var c = new Vector2(r.X + r.Width / 2f, r.Y + r.Height / 2f);
        UiShapes.Fill(new Rectangle(c.X - r.Width * 0.09f, c.Y - r.Height * 0.1f, r.Width * 0.18f, r.Height * 0.2f), r.Height * 0.03f, new Color(60, 64, 82, 220));
        UiShapes.Circle(c, r.Height * 0.06f, new Color(220, 228, 246, 255));
        if (print.File.Length > 0) UiFonts.Draw(Text.Of("photoBook.lost"), r.X + 12, r.Y + r.Height - 34, 20, Color.White, UiWeight.Black);
    }
}
