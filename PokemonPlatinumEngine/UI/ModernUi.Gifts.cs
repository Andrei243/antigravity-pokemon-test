using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The gift box and the trade board on screen (plan 40 · GE7, GE8; style guide, "Menu screens", "The gift box", "The trade board").</summary>
internal static partial class ModernUi
{
    /// <summary>The Gift Runners' scarf: the gift box's colour.</summary>
    public static readonly Color GiftYellow = new(240, 196, 70, 255);

    /// <summary>The link desks' teal: the trade board's colour.</summary>
    public static readonly Color BoardTeal = new(60, 170, 180, 255);

    public const float GiftCardWidth = 420, GiftCardHeight = 300, GiftCardPitch = 460, GiftRowPitch = 330;

    private static (string Label, UiIcon Icon, Color Color)[] GiftTabs(int waiting) => new[]
    {
        (Text.Of("giftBox.tab.waiting", ("n", waiting)), UiIcon.Bag, GiftYellow),
        (Text.Of("giftBox.tab.cards"), UiIcon.Pin, new Color(236, 110, 170, 255)),
        (Text.Of("giftBox.tab.words"), UiIcon.Hat, new Color(70, 136, 232, 255))
    };

    public static void DrawGiftBox(int sw, int sh, GiftBoxScreen screen, float appear)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("giftBox.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.z", screen.Tab == GiftBoxScreen.GiftTab.Words ? "giftBox.hint.words" : "giftBox.hint.choose"),
            Hint("shared.keys.esc", "shared.hints.back"));
        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;
        Tabs(new Rectangle(Margin + slide, ContentTop, sw - Margin * 2, 64), GiftTabs(screen.Due.Count), (int)screen.Tab);
        float top = ContentTop + 100;
        float left = (sw - (GiftCardPitch * (GiftBoxScreen.Columns - 1) + GiftCardWidth)) / 2f + slide;

        if (screen.Tab == GiftBoxScreen.GiftTab.Words)
        {
            var r = new Rectangle((sw - GiftCardWidth) / 2f + slide, top + 120, GiftCardWidth, GiftCardHeight);
            Card(r, 22, true, GiftYellow);
            UiShapes.Fill(new Rectangle(r.X + 4, r.Y + 4, r.Width - 8, 48), 18, GiftYellow);
            UiFonts.DrawCentered(Text.Of("giftBox.words.title"), r.X + 24, r.Y + 28, 26, Color.White, UiWeight.Black);
            var bubble = new Rectangle(r.X + r.Width / 2f - 70, r.Y + 76, 140, 86);
            UiShapes.Fill(bubble, 30, GiftYellow);
            UiShapes.Triangle(new Vector2(bubble.X + 30, bubble.Y + bubble.Height - 2), new Vector2(bubble.X + 64, bubble.Y + bubble.Height - 2), new Vector2(bubble.X + 26, bubble.Y + bubble.Height + 26), GiftYellow, 2);
            for (int k = 0; k < 3; k++) UiShapes.Circle(new Vector2(bubble.X + 40 + k * 30, bubble.Y + bubble.Height / 2f), 8, Color.White);
            float y = r.Y + 196;
            foreach (string line in Wrap(Text.Of("giftBox.words.body"), r.Width - 48, 22, UiWeight.ExtraBold))
            {
                UiFonts.Draw(line, r.X + 24, y, 22, Muted, UiWeight.ExtraBold);
                y += 30;
            }
            return;
        }

        int count = screen.Count;
        if (count == 0)
        {
            string empty = Text.Of(screen.Tab == GiftBoxScreen.GiftTab.Waiting ? "giftBox.empty" : "giftBox.album.empty");
            UiFonts.DrawCentered(empty, (sw - UiFonts.Measure(empty, 30, UiWeight.ExtraBold)) / 2f, (top + ContentBottom) / 2f, 30, Muted, UiWeight.ExtraBold);
            return;
        }
        for (int k = 0; k < count; k++)
        {
            int row = k / GiftBoxScreen.Columns - screen.Top, col = k % GiftBoxScreen.Columns;
            if (row < 0 || row >= GiftBoxScreen.VisibleRows) continue;
            var r = new Rectangle(left + col * GiftCardPitch, top + row * GiftRowPitch, GiftCardWidth, GiftCardHeight);
            bool selected = k == screen.Cursor;
            if (screen.Tab == GiftBoxScreen.GiftTab.Waiting) GiftCard(r, screen.Due[k], null, selected);
            else GiftCard(r, screen.Album[k].Gift, screen.Album[k].Day, selected);
        }
    }

    /// <summary>A gift's card: the sender on a yellow band, its picture in a well, its title and its words.</summary>
    public static void GiftCard(Rectangle r, Gift gift, DateTime? taken, bool selected)
    {
        Card(r, 22, selected, GiftYellow);
        if (selected) UiShapes.Shape(new Rectangle(r.X + 6, r.Y + 6, r.Width - 12, r.Height - 12), 18, Color.Blank, Color.Blank, GiftYellow, 3);
        UiShapes.Fill(new Rectangle(r.X + 4, r.Y + 4, r.Width - 8, 48), 18, GiftYellow);
        string sender = Text.Of($"giftBox.gift.{gift.Id}.sender");
        UiFonts.DrawCentered(sender, r.X + 20, r.Y + 28, 26, Color.White, UiWeight.Black);
        if (taken is { } day)
        {
            string when = $"{day.Day} {Words.Month(day)} {day.Year}";
            UiFonts.DrawCentered(when, r.X + r.Width - 20 - UiFonts.Measure(when, 22, UiWeight.Black), r.Y + 28, 22, Color.White, UiWeight.Black);
        }
        var well = new Rectangle(r.X + (r.Width - 200) / 2f, r.Y + 62, 200, 120);
        UiShapes.Fill(well, 14, new Color(236, 240, 248, 255));
        GiftPictureArt(well, gift.Picture);
        UiFonts.Draw(Text.Of($"giftBox.gift.{gift.Id}.title"), r.X + 20, r.Y + 192, 28, Ink, UiWeight.Black);
        float y = r.Y + 232;
        foreach (string line in Wrap(Text.Of($"giftBox.gift.{gift.Id}.card"), r.Width - 40, 22, UiWeight.ExtraBold))
        {
            if (y > r.Y + r.Height - 24) break;
            UiFonts.Draw(line, r.X + 20, y, 22, Muted, UiWeight.ExtraBold);
            y += 28;
        }
    }

    /// <summary>What a gift's card shows in its well.</summary>
    private static void GiftPictureArt(Rectangle well, GiftPicture picture)
    {
        var c = new Vector2(well.X + well.Width / 2f, well.Y + well.Height / 2f);
        switch (picture.Kind)
        {
            case "pokemon":
                PixelArt(PixelArtGenerator.GetPokemonIcon(picture.Name), c, 3);
                break;
            case "egg":
                PixelArt(PixelArtGenerator.EggIcon, c, 3);
                break;
            case "item" when ItemDatabase.Get(picture.Name) is { } item:
                PixelArt(PixelArtGenerator.GetItemIcon(item), c, 3);
                break;
            case "backdrop":
                BackdropArt(new Rectangle(well.X + 8, well.Y + 8, well.Width - 16, well.Height - 16), SuperContestDatabase.Backdrop(picture.Name));
                break;
            default:
                // A Seal or a ribbon: a rosette in the box's colour
                for (int k = 0; k < 8; k++)
                {
                    float a = k * MathF.Tau / 8f;
                    UiShapes.Circle(c + new Vector2(MathF.Cos(a), MathF.Sin(a)) * 30, 16, GiftYellow);
                }
                UiShapes.Circle(c, 26, Color.White);
                UiShapes.Circle(c, 18, GiftYellow);
                break;
        }
    }

    public const float BoardCardWidth = 560, BoardCardHeight = 380, BoardCardPitch = 600;

    public static void DrawTradeBoard(int sw, int sh, TradeBoardScreen screen, float appear)
    {
        Backdrop(sw, sh);
        string title = Text.Of("tradeBoard.title");
        ScreenTitle(title);
        string region = (RegionDatabase.Get(screen.Region)?.Name ?? screen.Region).ToUpperInvariant();
        Tag(64 + UiFonts.Measure(title, 52, UiWeight.Black) + 24, 50, region, Muted, 40);
        Hints(sw - Margin, 44, Hint("shared.keys.z", "tradeBoard.hint.choose"), Hint("shared.keys.esc", "shared.hints.back"));
        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;
        int n = screen.Requests.Count;
        float left = (sw - (BoardCardPitch * Math.Max(0, n - 1) + BoardCardWidth)) / 2f + slide;
        float top = ContentTop + 40;
        for (int k = 0; k < n; k++)
            BoardCard(new Rectangle(left + k * BoardCardPitch, top, BoardCardWidth, BoardCardHeight), screen.Requests[k],
                k < screen.Met.Count && screen.Met[k], k == screen.Cursor, screen.Seen);

        var row = new Rectangle((sw - 1720) / 2f + slide, top + BoardCardHeight + 60, 1720, 120);
        bool blind = screen.Cursor == screen.BlindRow;
        Card(row, 22, blind, BoardTeal);
        var mark = new Vector2(row.X + 70, row.Y + row.Height / 2f);
        UiShapes.Circle(mark, 40, BoardTeal);
        UiFonts.DrawCentered("?", mark.X - UiFonts.Measure("?", 52, UiWeight.Black) / 2f, mark.Y, 52, Color.White, UiWeight.Black);
        UiFonts.DrawCentered(Text.Of("tradeBoard.blind"), row.X + 140, mark.Y, 32, Ink, UiWeight.Black);
        string left2 = Text.Of("tradeBoard.blindLeft", ("n", screen.BlindLeft), ("of", TradeBoard.BlindADay));
        UiFonts.DrawCentered(left2, row.X + row.Width - 40 - UiFonts.Measure(left2, 28, UiWeight.Black), mark.Y, 28, screen.BlindLeft > 0 ? BoardTeal : Muted, UiWeight.Black);
    }

    /// <summary>A request's card: the trainer on a teal band, what they want and what they offer, and MET once met.</summary>
    private static void BoardCard(Rectangle r, BoardRequest request, bool met, bool selected, Func<string, bool> seen)
    {
        Card(r, 22, selected, BoardTeal);
        if (selected) UiShapes.Shape(new Rectangle(r.X + 6, r.Y + 6, r.Width - 12, r.Height - 12), 18, Color.Blank, Color.Blank, BoardTeal, 3);
        UiShapes.Fill(new Rectangle(r.X + 4, r.Y + 4, r.Width - 8, 52), 18, BoardTeal);
        UiFonts.DrawCentered(request.Trainer.Name, r.X + 22, r.Y + 30, 28, Color.White, UiWeight.Black);
        if (request.Trainer.Language is { } language)
        {
            string tag = Text.Of($"tradeBoard.language.{language}");
            float w = UiFonts.Measure(tag, 18, UiWeight.Black) + 26;
            UiShapes.Fill(new Rectangle(r.X + r.Width - 22 - w, r.Y + 16, w, 30), 15, Color.White);
            UiFonts.DrawCentered(tag, r.X + r.Width - 22 - w + 13, r.Y + 31, 18, BoardTeal, UiWeight.Black);
        }
        bool known = seen(request.Wants);
        BoardWell(new Rectangle(r.X + 30, r.Y + 80, 220, 180), Text.Of("tradeBoard.wants"), request.Wants, known, null);
        BoardWell(new Rectangle(r.X + r.Width - 250, r.Y + 80, 220, 180), Text.Of("tradeBoard.offers"), request.Offers, true, request.Item);
        UiShapes.Triangle(new Vector2(r.X + r.Width / 2f - 18, r.Y + 156), new Vector2(r.X + r.Width / 2f - 18, r.Y + 184), new Vector2(r.X + r.Width / 2f + 16, r.Y + 170), BoardTeal, 4);
        string wants = known ? Data.LocalNames.Species(request.Wants) : Text.Of("tradeBoard.unknown");
        UiFonts.Draw(wants, r.X + 30, r.Y + 276, 26, Ink, UiWeight.Black);
        UiFonts.Draw(Text.Of("tradeBoard.level", ("n", request.AtLeast)), r.X + 30, r.Y + 312, 22, Muted, UiWeight.ExtraBold);
        string offers = Data.LocalNames.Species(request.Offers);
        UiFonts.Draw(offers, r.X + r.Width - 250, r.Y + 276, 26, Ink, UiWeight.Black);
        if (met)
        {
            UiShapes.Fill(r, 22, new Color(240, 244, 250, 170));
            string stamp = Text.Of("tradeBoard.met");
            float w = UiFonts.Measure(stamp, 48, UiWeight.Black) + 60;
            var s = new Rectangle(r.X + (r.Width - w) / 2f, r.Y + r.Height / 2f - 40, w, 80);
            UiShapes.Shape(s, 16, Color.Blank, Color.Blank, BoardTeal, 6);
            UiFonts.DrawCentered(stamp, s.X + 30, s.Y + 40, 48, BoardTeal, UiWeight.Black);
        }
    }

    private static void BoardWell(Rectangle well, string label, string species, bool known, string? item)
    {
        UiShapes.Fill(well, 14, new Color(236, 240, 248, 255));
        Label(label, well.X + 12, well.Y + 8);
        var c = new Vector2(well.X + well.Width / 2f, well.Y + well.Height / 2f + 10);
        PixelArt(PixelArtGenerator.GetPokemonIcon(species), c, 3, tint: known ? null : Frame);
        if (item != null && ItemDatabase.Get(item) is { } data) PixelArt(PixelArtGenerator.GetItemIcon(data), new Vector2(well.X + well.Width - 30, well.Y + well.Height - 30), 2);
    }
}
