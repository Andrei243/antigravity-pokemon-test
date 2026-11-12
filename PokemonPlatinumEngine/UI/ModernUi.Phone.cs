using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The travel phone (plan 43 · LF6; style guide, "Menu screens", "The travel phone"): the menu screens' backdrop, the
/// cards as tabs, and under them the card on show: the numbers saved beside the chosen contact's card, the day's news
/// on a cream sheet, or the region's map with the player's cell.
/// </summary>
internal static partial class ModernUi
{
    /// <summary>The travel phone's colour in a region (its chip, the tabs' accents): Unova's slate blue, Kalos's rose.</summary>
    public static Color PhoneColour(string? region) => region switch
    {
        RegionDatabase.Kalos => new Color(214, 92, 128, 255),
        RegionDatabase.Unova => new Color(70, 110, 170, 255),
        _ => new Color(60, 150, 140, 255),
    };

    private static (string Label, UiIcon Icon, Color Color) PhoneTab(Models.PhoneCard card) => card switch
    {
        Models.PhoneCard.Calls => (Text.Of("travelPhone.card.Calls"), UiIcon.Phone, Green),
        Models.PhoneCard.News => (Text.Of("travelPhone.card.News"), UiIcon.Letter, Gold),
        _ => (Text.Of("travelPhone.card.Map"), UiIcon.MapCard, Blue)
    };

    public static void Phone(PhoneScreen screen, int sw, int sh)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("travelPhone.title"));

        var tabs = new List<(string, UiIcon, Color)>();
        foreach (var card in screen.Cards) tabs.Add(PhoneTab(card));
        float tabW = MathF.Min(300, (sw - 2 * Margin - 12 * (screen.Cards.Count - 1)) / screen.Cards.Count);
        Tabs(new Rectangle(Margin, ContentTop, tabW * screen.Cards.Count + 12 * (screen.Cards.Count - 1), 72), tabs, screen.CardIndex);

        var body = new Rectangle(Margin, ContentTop + 104, sw - 2 * Margin, ContentBottom - ContentTop - 104 - 76);
        switch (screen.Card)
        {
            case Models.PhoneCard.Calls: PhoneCalls(screen, body); break;
            case Models.PhoneCard.News: PhoneNews(screen, body); break;
            default: PhoneMap(screen, body); break;
        }

        var hints = new List<HintItem>();
        if (screen.Card == Models.PhoneCard.Map) hints.Add(Hint("shared.keys.z", "travelPhone.hints.open"));
        if (screen.Card == Models.PhoneCard.Calls && screen.Contacts.Count > 0) hints.Add(Hint("shared.keys.z", "travelPhone.hints.call"));
        if (screen.Cards.Count > 1) hints.Add(Hint("shared.keys.leftRight", "travelPhone.hints.cards"));
        hints.Add(Hint("shared.keys.x", "shared.hints.back"));
        Hints(sw - Margin, ContentBottom - 52, hints.ToArray());
    }

    private static void PhoneCalls(PhoneScreen screen, Rectangle body)
    {
        var list = new Rectangle(body.X, body.Y, 1060, body.Height);
        Panel(list, 32);
        if (screen.Contacts.Count == 0)
        {
            EmptyNote(list, Text.Of("travelPhone.calls.empty"));
            return;
        }
        var colour = PhoneColour(screen.RegionName);
        int first = screen.FirstRow;
        bool scrolls = screen.Contacts.Count > PhoneScreen.VisibleRows;
        for (int row = 0; row < PhoneScreen.VisibleRows && first + row < screen.Contacts.Count; row++)
        {
            var contact = screen.Contacts[first + row];
            var r = RowRect(list, row, scrolls: scrolls);
            bool selected = first + row == screen.ContactIndex;
            ListRow(r, selected, contact.Name);
            var chip = new Vector2(r.X + 52, r.Y + r.Height / 2f);
            UiShapes.Circle(chip, 30, selected ? Color.White : colour);
            UiIcons.Draw(UiIcon.Phone, chip, 15, selected ? colour : Color.White, selected ? Color.White : colour);
        }
        ScrollBar(list, first, PhoneScreen.VisibleRows, screen.Contacts.Count);

        var chosen = screen.Contacts[screen.ContactIndex];
        var card = new Rectangle(list.X + list.Width + Gutter, body.Y, body.Width - list.Width - Gutter, body.Height);
        Panel(card, 32);
        float x = card.X + 48;
        UiFonts.Draw(chosen.Name, x, card.Y + 44, 44, Ink, UiWeight.Black);
        UiShapes.Fill(new Rectangle(x, card.Y + 112, card.Width - 96, 3), 1.5f, Rule);
        Label(Text.Of("travelPhone.calls.where"), x, card.Y + 140);
        DrawWrapped(screen.PlaceOf?.Invoke(chosen.Area) ?? "", x, card.Y + 166, card.Width - 96, 32, Ink, 40, UiWeight.ExtraBold);
        if (screen.Waiting?.Invoke(chosen) == true)
            UiFonts.Draw(Text.Of("travelPhone.calls.waiting"), x, card.Y + 280, 26, Red, UiWeight.Black);
    }

    private static void PhoneNews(PhoneScreen screen, Rectangle body)
    {
        Panel(body, 32);
        var sheet = new Rectangle(body.X + 40, body.Y + 40, body.Width - 80, body.Height - 80);
        UiShapes.Fill(sheet, 18, new Color(252, 248, 238, 255));
        float x = sheet.X + 48, y = sheet.Y + 40;
        UiFonts.Draw(Text.Of("travelPhone.news.heading"), x, y, 40, Ink, UiWeight.Black);
        UiShapes.Fill(new Rectangle(x, y + 64, sheet.Width - 96, 3), 1.5f, Rule);
        if (screen.News.Count == 0)
        {
            UiFonts.Draw(Text.Of("travelPhone.news.none"), x, y + 100, 32, Muted, UiWeight.ExtraBold);
            return;
        }
        y += 100;
        foreach (string line in screen.News)
        {
            var rows = Wrap(line, sheet.Width - 96, 32, UiWeight.ExtraBold);
            foreach (string row in rows)
            {
                if (y > sheet.Y + sheet.Height - 50) return;
                UiFonts.Draw(row, x, y, 32, Ink, UiWeight.ExtraBold);
                y += 44;
            }
            y += 14;
        }
    }

    private static void PhoneMap(PhoneScreen screen, Rectangle body)
    {
        Panel(body, 32);
        if (screen.RegionName is not { } region || Data.RegionMap.Of(region) is not { } map)
        {
            EmptyNote(body, Text.Of("travelPhone.map.none"));
            return;
        }
        var inner = new Rectangle(body.X + 40, body.Y + 40, body.Width - 520, body.Height - 80);
        float cell = MathF.Floor(MathF.Min(inner.Width / map.View.Width, inner.Height / map.View.Height));
        var here = screen.PlayerCell;
        RegionMap(inner, cell, here is { } h ? new HashSet<(int, int)> { h } : new HashSet<(int, int)>(), here, RegionDatabase.Get(region));
        float x = body.X + body.Width - 440;
        UiFonts.Draw(Text.Of("travelPhone.map.heading"), x, body.Y + 60, 40, Ink, UiWeight.Black);
        DrawWrapped(Text.Of("travelPhone.map.body"), x, body.Y + 130, 380, 30, Muted, 40, UiWeight.ExtraBold);
        if (screen.PlaceHere is { Length: > 0 } place)
        {
            Label(Text.Of("townMap.youAreIn"), x, body.Y + 300);
            DrawWrapped(place, x, body.Y + 326, 380, 34, Ink, 44, UiWeight.Black);
        }
    }
}
