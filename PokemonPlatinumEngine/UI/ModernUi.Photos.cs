using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>Cameron's photo album on screen (plan 20, part q; style guide, "Menu screens", "The photo album").</summary>
internal static partial class ModernUi
{
    public static void DrawPhotoAlbum(int sw, int sh, PhotoAlbumScreen screen, float appear)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("photoAlbum.title"));
        if (screen.AskingToRemove)
            Hints(sw - Margin, 44, Hint("shared.keys.z", "photoAlbum.hint.remove"), Hint("shared.keys.esc", "photoAlbum.hint.keep"));
        else
            Hints(sw - Margin, 44, Hint("shared.keys.leftRight", "shared.hints.page"), Hint("shared.keys.z", "photoAlbum.hint.throwAway"), Hint("shared.keys.esc", "shared.hints.back"));
        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;
        var page = new Rectangle(Margin + slide, ContentTop, sw - Margin * 2, ContentBottom - ContentTop);
        Panel(page, 34);
        var album = screen.Album;
        string count = Text.Of("photoAlbum.count", ("n", album.Count), ("of", PhotoAlbum.Capacity));
        Tag(page.X + page.Width - 44 - UiFonts.Measure(count, 24, UiWeight.Black) - 34, page.Y + 26, count, Frame, 40);

        if (screen.Shown is not { } photo)
        {
            string empty = Text.Of("photoAlbum.empty");
            UiFonts.DrawCentered(empty, page.X + (page.Width - UiFonts.Measure(empty, 30, UiWeight.ExtraBold)) / 2f, page.Y + page.Height / 2f - 20, 30, Muted, UiWeight.ExtraBold);
            return;
        }

        // The photograph: a print on a white border, the team's lead on a sky over its ground
        Label(Text.Of("photoAlbum.page", ("n", screen.Page + 1), ("of", album.Count)), page.X + 44, page.Y + 30);
        const float printW = 760, printH = 520;
        var border = new Rectangle(page.X + (page.Width - printW) / 2f - 24, page.Y + 96, printW + 48, printH + 120);
        UiShapes.Fill(border, 10, new Color(246, 244, 236, 255));
        var print = new Rectangle(border.X + 24, border.Y + 24, printW, printH);
        UiShapes.Fill(print, 4, new Color(150, 196, 232, 255));
        UiShapes.Fill(new Rectangle(print.X, print.Y + printH * 0.62f, printW, printH * 0.38f), 4, new Color(126, 170, 102, 255));
        if (photo.Pokemon is { Length: > 0 } look)
        {
            PixelArt(PixelArtGenerator.GetPokemonSprite(look, false), new Vector2(print.X + printW / 2f, print.Y + printH * 0.55f), 3, 0f);
        }
        else
        {
            string nobody = Text.Of("photoAlbum.nobody");
            UiFonts.DrawCentered(nobody, print.X + (printW - UiFonts.Measure(nobody, 28, UiWeight.ExtraBold)) / 2f, print.Y + printH / 2f, 28, Ink, UiWeight.ExtraBold);
        }

        // The caption written under it: the place and the day
        string place = screen.PlaceOf(photo.Place);
        UiFonts.DrawCentered(place, border.X + (border.Width - UiFonts.Measure(place, 34, UiWeight.Black)) / 2f, print.Y + printH + 22, 34, Ink, UiWeight.Black);
        string day = photo.Date() is { } when ? Text.Of("photoAlbum.taken", ("day", when.Day), ("month", Words.Month(when)), ("year", when.Year)) : "";
        UiFonts.DrawCentered(day, border.X + (border.Width - UiFonts.Measure(day, 24, UiWeight.ExtraBold)) / 2f, print.Y + printH + 66, 24, Muted, UiWeight.ExtraBold);

        if (screen.AskingToRemove)
        {
            string ask = Text.Of("photoAlbum.removeAsk");
            var bar = new Rectangle(page.X + 44, page.Y + page.Height - 96, page.Width - 88, 64);
            UiShapes.Fill(bar, 18, Frame);
            UiFonts.DrawCentered(ask, bar.X + (bar.Width - UiFonts.Measure(ask, 28, UiWeight.Black)) / 2f, bar.Y + 16, 28, Ink, UiWeight.Black);
        }
    }
}
