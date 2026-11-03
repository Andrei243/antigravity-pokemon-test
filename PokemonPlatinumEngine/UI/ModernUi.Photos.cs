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
        // The scene of the place (plan 20, part t): its sky, its ground and the plain shapes standing in it
        var scene = screen.Scene ?? PhotoScenes.Look(PhotoSetting.Route);
        UiShapes.Fill(print, 4, scene.Sky);
        float horizon = print.Y + printH * scene.Horizon;
        foreach (var shape in scene.Shapes) PhotoShapeAt(print, shape);
        UiShapes.Fill(new Rectangle(print.X, horizon, printW, print.Y + printH - horizon), 4, scene.Ground);
        foreach (var shape in scene.Shapes)
            if (shape.Kind == PhotoShapeKind.Rect && shape.Y >= scene.Horizon) PhotoShapeAt(print, shape);

        // The player's figure beside the lead, in the colours of the look the photograph was taken with
        var (hat, coat) = PhotoScenes.FigureOf(photo.Player);
        float fx = print.X + printW * (photo.Pokemon is { Length: > 0 } ? 0.34f : 0.5f), fy = horizon + 18;
        UiShapes.Fill(new Rectangle(fx - 22, fy - 96, 44, 70), 16, coat);
        UiShapes.Fill(new Rectangle(fx - 18, fy - 30, 14, 34), 6, new Color(52, 56, 72, 255));
        UiShapes.Fill(new Rectangle(fx + 4, fy - 30, 14, 34), 6, new Color(52, 56, 72, 255));
        UiShapes.Circle(new Vector2(fx, fy - 118), 24, new Color(246, 214, 182, 255));
        UiShapes.Fill(new Rectangle(fx - 26, fy - 146, 52, 22), 10, hat);

        // The place card in the print's corner: the region the place is in
        string region = Text.Of(screen.InKantoOf(photo.Place) ? "photoAlbum.region.kanto" : "photoAlbum.region.johto");
        float cardW = UiFonts.Measure(region, 22, UiWeight.Black) + 40;
        var card = new Rectangle(print.X + 18, print.Y + 18, cardW, 40);
        UiShapes.Fill(card, 12, new Color(246, 244, 236, 230));
        UiShapes.Circle(new Vector2(card.X + 16, card.Y + 20), 6, new Color(206, 66, 76, 255));
        UiFonts.Draw(region, card.X + 28, card.Y + 8, 22, Ink, UiWeight.Black);

        if (photo.Pokemon is { Length: > 0 } look)
        {
            PixelArt(PixelArtGenerator.GetPokemonSprite(look, false), new Vector2(print.X + printW * 0.58f, horizon - printH * 0.07f), 3, 0f);
        }
        else
        {
            string nobody = Text.Of("photoAlbum.nobody");
            UiFonts.DrawCentered(nobody, print.X + (printW - UiFonts.Measure(nobody, 28, UiWeight.ExtraBold)) / 2f, print.Y + printH - 60, 28, Ink, UiWeight.ExtraBold);
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

    /// <summary>One shape of a photograph's scene, laid on the print by its shares (Models/PhotoScenes.cs).</summary>
    private static void PhotoShapeAt(Rectangle print, PhotoShape shape)
    {
        float x = print.X + shape.X * print.Width, y = print.Y + shape.Y * print.Height;
        switch (shape.Kind)
        {
            case PhotoShapeKind.Circle:
                UiShapes.Circle(new Vector2(x, y), shape.W * print.Width, shape.Color);
                break;
            case PhotoShapeKind.Triangle:
                float w = shape.W * print.Width, h = shape.H * print.Height;
                UiShapes.Triangle(new Vector2(x, y), new Vector2(x + w / 2f, y - h), new Vector2(x + w, y), shape.Color);
                break;
            default:
                UiShapes.Fill(new Rectangle(x, y, shape.W * print.Width, shape.H * print.Height), 3, shape.Color);
                break;
        }
    }
}
