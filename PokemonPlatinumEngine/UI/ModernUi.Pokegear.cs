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
/// The Pokégear (plan 20 · J3; style guide, "Menu screens", "The Pokégear"): the menu screens' backdrop, the clock at
/// the top right, the cards as tabs, and under them the card on show: the phone's list beside its contact's card,
/// the map card's map of the region, the radio's round dial with its stations, the cursor and what the station says.
/// </summary>
internal static partial class ModernUi
{
    /// <summary>The Pokégear's own colour, the teal of its shell.</summary>
    public static readonly Color GearTeal = new(40, 168, 186, 255);

    private static (string Label, UiIcon Icon, Color Color) CardTab(PokegearCard card) => card switch
    {
        PokegearCard.Phone => (Text.Of("pokegear.card.Phone"), UiIcon.Phone, Green),
        PokegearCard.Map => (Text.Of("pokegear.card.Map"), UiIcon.MapCard, Blue),
        _ => (Text.Of("pokegear.card.Radio"), UiIcon.Radio, Gold)
    };

    /// <summary>The rectangle the radio's dial is drawn in, and a unit of the original's touch screen in layout units.</summary>
    public static readonly Rectangle DialRect = new(240, 300, 600, 600);
    public const float DialScale = 600f / (2 * (RadioDial.Reach + 18));

    public static void Pokegear(PokegearScreen screen, int sw, int sh)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("pokegear.title"));
        // The clock: the day and the hour, as the Pokégear shows it on every card
        string clock = Text.Of("pokegear.clock", ("day", Words.Weekday(screen.Now)),
            ("time", $"{screen.Now.Hour:00}:{screen.Now.Minute:00}"));
        float cw = UiFonts.Measure(clock, 40, UiWeight.Black);
        var clockPill = new Rectangle(sw - Margin - cw - 56, 34, cw + 56, 64);
        UiShapes.Fill(clockPill, 32, new Color(10, 30, 60, 110));
        UiFonts.DrawCentered(clock, clockPill.X + 28, clockPill.Y + 32, 40, Color.White, UiWeight.Black);

        var tabs = new List<(string, UiIcon, Color)>();
        foreach (var card in screen.Cards) tabs.Add(CardTab(card));
        Tabs(new Rectangle(Margin, ContentTop, 300 * screen.Cards.Count + 12 * (screen.Cards.Count - 1), 72), tabs, screen.CardIndex);

        var body = new Rectangle(Margin, ContentTop + 104, sw - 2 * Margin, ContentBottom - ContentTop - 104 - 76);
        switch (screen.Card)
        {
            case PokegearCard.Phone: PhoneCard(screen, body); break;
            case PokegearCard.Map: MapCard(screen, body); break;
            default: RadioCard(screen, body); break;
        }

        var hints = new List<HintItem>();
        if (screen.Card == PokegearCard.Phone && screen.Contacts.Count > 0) hints.Add(Hint("shared.keys.z", "pokegear.hints.call"));
        if (screen.Card == PokegearCard.Map) hints.Add(Hint("shared.keys.z", "pokegear.hints.open"));
        if (screen.Card == PokegearCard.Radio) hints.Add(Hint("shared.keys.arrows", "pokegear.hints.tune"));
        if (screen.Cards.Count > 1)
            hints.Add(screen.Card == PokegearCard.Radio ? Hint("pokegear.keys.shoulders", "pokegear.hints.cards") : Hint("shared.keys.leftRight", "pokegear.hints.cards"));
        hints.Add(Hint("shared.keys.x", "shared.hints.back"));
        Hints(sw - Margin, ContentBottom - 52, hints.ToArray());
    }

    private static void PhoneCard(PokegearScreen screen, Rectangle body)
    {
        var list = new Rectangle(body.X, body.Y, 1060, body.Height);
        Panel(list, 32);
        if (screen.Contacts.Count == 0)
        {
            EmptyNote(list, Text.Of("pokegear.phone.empty"));
            return;
        }
        int first = screen.FirstRow;
        for (int row = 0; row < PokegearScreen.VisibleRows && first + row < screen.Contacts.Count; row++)
        {
            var contact = screen.Contacts[first + row];
            var r = RowRect(list, row, scrolls: screen.Contacts.Count > PokegearScreen.VisibleRows);
            bool selected = first + row == screen.ContactIndex;
            ListRow(r, selected, contact.Name);
            var chip = new Vector2(r.X + 52, r.Y + r.Height / 2f);
            UiShapes.Circle(chip, 30, selected ? Color.White : Green);
            UiIcons.Draw(UiIcon.Phone, chip, 15, selected ? Green : Color.White, selected ? Color.White : Green);
        }
        ScrollBar(list, first, PokegearScreen.VisibleRows, screen.Contacts.Count);

        // The contact under the cursor on a card of its own: who, where they are met, and when they want a rematch
        var chosen = screen.Contacts[screen.ContactIndex];
        var card = new Rectangle(list.X + list.Width + Gutter, body.Y, body.Width - list.Width - Gutter, body.Height);
        Panel(card, 32);
        float x = card.X + 48;
        UiFonts.Draw(chosen.Name, x, card.Y + 44, 44, Ink, UiWeight.Black);
        UiShapes.Fill(new Rectangle(x, card.Y + 112, card.Width - 96, 3), 1.5f, Rule);
        Label(Text.Of("pokegear.phone.where"), x, card.Y + 140);
        DrawWrapped(screen.PlaceOf?.Invoke(chosen.Area) ?? "", x, card.Y + 166, card.Width - 96, 32, Ink, 40, UiWeight.ExtraBold);
        if (chosen.RematchDay is { } day && chosen.Trainer != null)
        {
            Label(Text.Of("pokegear.phone.rematch"), x, card.Y + 270);
            string when = Text.Of("pokegear.phone.when", ("day", Words.Weekday(NextDay(day))), ("time", Text.Of($"pokegear.time.{chosen.RematchTime}")));
            DrawWrapped(when, x, card.Y + 296, card.Width - 96, 32, Ink, 40, UiWeight.ExtraBold);
        }
        if (screen.Waiting?.Invoke(chosen) == true)
            Tag(x, card.Y + card.Height - 90, Text.Of("pokegear.phone.waiting"), Selection, 40);
    }

    /// <summary>A date that falls on a day of the week, for its name in the language of the moment.</summary>
    private static DateTime NextDay(string day) =>
        Enum.TryParse<DayOfWeek>(day, true, out var d) ? new DateTime(2009, 3, 1).AddDays((int)d) : new DateTime(2009, 3, 1);

    private static void MapCard(PokegearScreen screen, Rectangle body)
    {
        Panel(body, 32);
        if (screen.RegionName is not { } region || Data.RegionMap.Of(region) is not { } map)
        {
            EmptyNote(body, Text.Of("pokegear.map.none"));
            return;
        }
        var inner = new Rectangle(body.X + 40, body.Y + 40, body.Width - 520, body.Height - 80);
        float cell = MathF.Floor(MathF.Min(inner.Width / map.View.Width, inner.Height / map.View.Height));
        var here = screen.PlayerCell;
        RegionMap(inner, cell, here is { } h ? new HashSet<(int, int)> { h } : new HashSet<(int, int)>(), here, RegionDatabase.Get(region));
        float x = body.X + body.Width - 440;
        UiFonts.Draw(Text.Of("pokegear.map.heading"), x, body.Y + 60, 40, Ink, UiWeight.Black);
        DrawWrapped(Text.Of("pokegear.map.body"), x, body.Y + 130, 380, 30, Muted, 40, UiWeight.ExtraBold);
        if (screen.PlaceHere is { Length: > 0 } place)
        {
            Label(Text.Of("townMap.youAreIn"), x, body.Y + 300);
            DrawWrapped(place, x, body.Y + 326, 380, 34, Ink, 44, UiWeight.Black);
        }
    }

    private static void RadioCard(PokegearScreen screen, Rectangle body)
    {
        Panel(body, 32);
        var dial = new Rectangle(body.X + 60, body.Y + (body.Height - DialRect.Height) / 2f, DialRect.Width, DialRect.Height);
        var c = new Vector2(dial.X + dial.Width / 2f, dial.Y + dial.Height / 2f);
        Vector2 At(int x, int y) => c + new Vector2(x - RadioDial.CenterX, y - RadioDial.CenterY) * DialScale;

        // The dial: a dark disc with rings, and a spot for each station received here
        UiShapes.Circle(c, (RadioDial.Reach + 14) * DialScale, Darker(GearTeal, 0.55f));
        UiShapes.Ring(c, (RadioDial.Reach + 14) * DialScale, 6, Lighter(GearTeal, 0.2f));
        for (int ring = 1; ring <= 3; ring++)
            UiShapes.Ring(c, RadioDial.Reach * DialScale * ring / 3f, 2, new Color(255, 255, 255, 40));
        var tuned = screen.Dial.Tuned();
        foreach (var spot in RadioDial.SpotsOf(screen.Dial.Signal))
        {
            bool on = tuned is { } t && t.Spot == spot;
            UiShapes.Circle(At(spot.X, spot.Y), spot.Heard * DialScale, (on ? Gold : Color.White) with { A = 46 });
            UiShapes.Circle(At(spot.X, spot.Y), spot.Clear * DialScale + 6, on ? Gold : new Color(255, 255, 255, 150));
        }
        // The cursor: a ring with a dot
        var cursor = At(screen.Dial.X, screen.Dial.Y);
        UiShapes.Ring(cursor, 22, 6, Selection);
        UiShapes.Circle(cursor, 7, Color.White);

        // What comes in
        float x = dial.X + dial.Width + 80, w = body.X + body.Width - 60 - x;
        if (screen.Dial.Signal == RadioSignal.NoSignal)
        {
            UiFonts.Draw(Text.Of("pokegear.radio.noSignal"), x, body.Y + 80, 44, Ink, UiWeight.Black);
            return;
        }
        if (screen.Station is not { } station)
        {
            UiFonts.Draw(Text.Of("pokegear.radio.static"), x, body.Y + 80, 44, Muted, UiWeight.Black);
            DrawWrapped(Text.Of("pokegear.radio.hint"), x, body.Y + 150, w, 30, Muted, 40, UiWeight.ExtraBold);
            return;
        }
        UiFonts.Draw(Text.Of($"pokegear.station.{station}"), x, body.Y + 80, 44, Ink, UiWeight.Black);
        if (!screen.Clear) Tag(x, body.Y + 146, Text.Of("pokegear.radio.faint"), Muted, 36);
        if (screen.Playing != RadioSong.None)
            Tag(x + (screen.Clear ? 0 : 200), body.Y + 146, Text.Of($"pokegear.song.{screen.Playing}"), Gold, 36);
        // The station's words in a strip of their own
        var strip = new Rectangle(x, body.Y + 220, w, 300);
        UiShapes.Fill(strip, 24, new Color(234, 240, 248, 255));
        DrawWrapped(screen.StationLine, strip.X + 32, strip.Y + 30, strip.Width - 64, 32, Ink, 44, UiWeight.ExtraBold);
    }
}
