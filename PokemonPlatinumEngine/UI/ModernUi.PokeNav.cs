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
/// The PokéNav (plan 21 · H2, part b; style guide, "Menu screens", "The PokéNav"): the menu screens' backdrop, the
/// pages as tabs, and under them the page on show: the map of Hoenn with the player's cell, the team's contest
/// condition round a pentagon, Match Call's list beside its contact's card, and the team's ribbons counted.
/// </summary>
internal static partial class ModernUi
{
    /// <summary>The PokéNav's own colour, the orange of its case.</summary>
    public static readonly Color NavOrange = new(236, 132, 46, 255);

    private static (string Label, UiIcon Icon, Color Color) NavTab(PokeNavPage page) => page switch
    {
        PokeNavPage.Map => (Text.Of("pokenav.page.Map"), UiIcon.MapCard, Blue),
        PokeNavPage.Condition => (Text.Of("pokenav.page.Condition"), UiIcon.Boost, Gold),
        PokeNavPage.MatchCall => (Text.Of("pokenav.page.MatchCall"), UiIcon.Phone, Green),
        _ => (Text.Of("pokenav.page.Ribbons"), UiIcon.Pin, Red)
    };

    public static void PokeNav(PokeNavScreen screen, int sw, int sh)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("pokenav.title"));
        // The case's colour as a band under the title, as the device's own frame
        UiShapes.Fill(new Rectangle(sw - Margin - 220, 50, 220, 36), 18, NavOrange);

        var tabs = new List<(string, UiIcon, Color)>();
        foreach (var page in screen.Pages) tabs.Add(NavTab(page));
        float tabW = MathF.Min(300, (sw - 2 * Margin - 12 * (screen.Pages.Count - 1)) / screen.Pages.Count);
        Tabs(new Rectangle(Margin, ContentTop, tabW * screen.Pages.Count + 12 * (screen.Pages.Count - 1), 72), tabs, screen.PageIndex);

        var body = new Rectangle(Margin, ContentTop + 104, sw - 2 * Margin, ContentBottom - ContentTop - 104 - 76);
        switch (screen.Page)
        {
            case PokeNavPage.Map: NavMapPage(screen, body); break;
            case PokeNavPage.Condition: NavConditionPage(screen, body); break;
            case PokeNavPage.MatchCall: NavMatchCallPage(screen, body); break;
            default: NavRibbonsPage(screen, body); break;
        }

        var hints = new List<HintItem>();
        if (screen.Page == PokeNavPage.Map) hints.Add(Hint("shared.keys.z", "pokenav.hints.open"));
        if (screen.Page == PokeNavPage.MatchCall && screen.Contacts.Count > 0) hints.Add(Hint("shared.keys.z", "pokenav.hints.call"));
        if (screen.Page is PokeNavPage.Condition or PokeNavPage.Ribbons && screen.Team.Count > 1) hints.Add(Hint("shared.keys.upDown", "pokenav.hints.pokemon"));
        if (screen.Pages.Count > 1) hints.Add(Hint("shared.keys.leftRight", "pokenav.hints.pages"));
        hints.Add(Hint("shared.keys.x", "shared.hints.back"));
        Hints(sw - Margin, ContentBottom - 52, hints.ToArray());
    }

    private static void NavMapPage(PokeNavScreen screen, Rectangle body)
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
        UiFonts.Draw(Text.Of("pokenav.page.Map"), x, body.Y + 60, 40, Ink, UiWeight.Black);
        DrawWrapped(Text.Of("pokenav.map.body"), x, body.Y + 130, 380, 30, Muted, 40, UiWeight.ExtraBold);
        if (screen.PlaceHere is { Length: > 0 } place)
        {
            Label(Text.Of("townMap.youAreIn"), x, body.Y + 300);
            DrawWrapped(place, x, body.Y + 326, 380, 34, Ink, 44, UiWeight.Black);
        }
    }

    /// <summary>A Pokémon of the team on a card of its own: its sprite on a disc, its name, its place in the team.</summary>
    private static void NavPokemonCard(PokeNavScreen screen, Rectangle card)
    {
        Panel(card, 32);
        var p = screen.Team[screen.PokemonIndex];
        var c = new Vector2(card.X + card.Width / 2f, card.Y + 220);
        UiShapes.Circle(c, 150, new Color(226, 234, 246, 255));
        var sprite = PixelArtGenerator.SpriteOf(p);
        const int scale = 3;
        Raylib.DrawTexturePro(sprite, new Rectangle(0, 0, sprite.Width, sprite.Height),
            new Rectangle(MathF.Round(c.X - sprite.Width * scale / 2f), MathF.Round(c.Y - sprite.Height * scale / 2f), sprite.Width * scale, sprite.Height * scale),
            Vector2.Zero, 0f, Color.White);
        NameWithGender(p, card.X + 44, card.Y + 420, 48);
        if (screen.Team.Count > 1)
        {
            string place = Text.Of("summary.place", ("index", screen.PokemonIndex + 1), ("count", screen.Team.Count));
            UiFonts.Draw(place, card.X + 44, card.Y + 500, 24, Muted, UiWeight.Black);
        }
    }

    private static void NavConditionPage(PokeNavScreen screen, Rectangle body)
    {
        if (screen.Team.Count == 0)
        {
            Panel(body, 32);
            EmptyNote(body, Text.Of("pokenav.condition.empty"));
            return;
        }
        var card = new Rectangle(body.X, body.Y, 560, body.Height);
        NavPokemonCard(screen, card);
        var chart = new Rectangle(card.X + card.Width + Gutter, body.Y, body.Width - card.Width - Gutter, body.Height);
        Panel(chart, 32);
        var p = screen.Team[screen.PokemonIndex];
        int[] values = Models.PokeNav.Condition(p);
        string[] names = { "Cool", "Beauty", "Cute", "Smart", "Tough" };
        Color[] colors = { new(232, 84, 74, 255), new(84, 140, 232, 255), new(236, 120, 176, 255), new(96, 186, 104, 255), new(232, 196, 72, 255) };
        var centre = new Vector2(chart.X + chart.Width / 2f - 120, chart.Y + chart.Height / 2f + 10);
        const float reach = 250;
        Vector2 Corner(int i, float share) =>
            centre + new Vector2(MathF.Sin(i * MathF.Tau / 5f), -MathF.Cos(i * MathF.Tau / 5f)) * reach * share;
        // The pentagon's frame, its rings, and the condition laid over it
        for (int ring = 1; ring <= 4; ring++)
            for (int i = 0; i < 5; i++)
                UiShapes.Line(Corner(i, ring / 4f), Corner((i + 1) % 5, ring / 4f), ring == 4 ? 4 : 2, ring == 4 ? Frame : Rule);
        var points = new Vector2[5];
        for (int i = 0; i < 5; i++) points[i] = Corner(i, Math.Clamp(values[i], 0, 255) / 255f);
        for (int i = 0; i < 5; i++)
        {
            UiShapes.Triangle(centre, points[i], points[(i + 1) % 5], NavOrange with { A = 150 });
            UiShapes.Line(points[i], points[(i + 1) % 5], 4, NavOrange);
        }
        for (int i = 0; i < 5; i++)
        {
            var at = Corner(i, 1.2f);
            UiShapes.Circle(Corner(i, 1f), 10, colors[i]);
            UiFonts.DrawCentered(Text.Of($"condition.quality.{names[i]}"), at.X, at.Y, 26, colors[i], UiWeight.Black);
        }
        // The numbers beside it, and the sheen
        float x = chart.X + chart.Width - 300;
        for (int i = 0; i < 5; i++)
        {
            float y = chart.Y + 70 + i * 66;
            UiFonts.Draw(Text.Of($"condition.quality.{names[i]}"), x, y, 30, colors[i], UiWeight.Black);
            UiFonts.Draw(values[i].ToString(), x + 200, y, 30, Ink, UiWeight.Black);
        }
        UiShapes.Fill(new Rectangle(x, chart.Y + 410, 240, 3), 1.5f, Rule);
        UiFonts.Draw(Text.Of("condition.sheen"), x, chart.Y + 436, 30, Muted, UiWeight.Black);
        UiFonts.Draw(p.Sheen.ToString(), x + 200, chart.Y + 436, 30, Ink, UiWeight.Black);
    }

    private static void NavMatchCallPage(PokeNavScreen screen, Rectangle body)
    {
        var list = new Rectangle(body.X, body.Y, 1060, body.Height);
        Panel(list, 32);
        if (screen.Contacts.Count == 0)
        {
            EmptyNote(list, Text.Of("pokenav.matchCall.empty"));
            return;
        }
        int first = screen.FirstRow(screen.ContactIndex, screen.Contacts.Count);
        bool scrolls = screen.Contacts.Count > PokeNavScreen.VisibleRows;
        for (int row = 0; row < PokeNavScreen.VisibleRows && first + row < screen.Contacts.Count; row++)
        {
            var contact = screen.Contacts[first + row];
            var r = RowRect(list, row, scrolls: scrolls);
            bool selected = first + row == screen.ContactIndex;
            ListRow(r, selected, contact.Name);
            var chip = new Vector2(r.X + 52, r.Y + r.Height / 2f);
            UiShapes.Circle(chip, 30, selected ? Color.White : NavOrange);
            UiIcons.Draw(UiIcon.Phone, chip, 15, selected ? NavOrange : Color.White, selected ? Color.White : NavOrange);
        }
        ScrollBar(list, first, PokeNavScreen.VisibleRows, screen.Contacts.Count);

        var chosen = screen.Contacts[screen.ContactIndex];
        var card = new Rectangle(list.X + list.Width + Gutter, body.Y, body.Width - list.Width - Gutter, body.Height);
        Panel(card, 32);
        float x = card.X + 48;
        UiFonts.Draw(chosen.Name, x, card.Y + 44, 44, Ink, UiWeight.Black);
        UiShapes.Fill(new Rectangle(x, card.Y + 112, card.Width - 96, 3), 1.5f, Rule);
        Label(Text.Of("pokenav.matchCall.where"), x, card.Y + 140);
        DrawWrapped(screen.PlaceOf?.Invoke(chosen.Area) ?? "", x, card.Y + 166, card.Width - 96, 32, Ink, 40, UiWeight.ExtraBold);
    }

    private static void NavRibbonsPage(PokeNavScreen screen, Rectangle body)
    {
        var list = new Rectangle(body.X, body.Y, 1060, body.Height);
        Panel(list, 32);
        for (int row = 0; row < screen.Team.Count && row < PokeNavScreen.VisibleRows; row++)
        {
            var p = screen.Team[row];
            var r = RowRect(list, row);
            bool selected = row == screen.PokemonIndex;
            ListRow(r, selected, p.DisplayName);
            int count = p.Ribbons.Count;
            string told = count == 0 ? Text.Of("pokenav.ribbons.none") : count == 1 ? Text.Of("pokenav.ribbons.one") : Text.Of("pokenav.ribbons.count", ("count", count));
            UiFonts.Draw(told, r.X + r.Width - 40 - UiFonts.Measure(told, 30, UiWeight.Black), r.Y + r.Height / 2f - 17, 30, selected ? Color.White : Muted, UiWeight.Black);
        }
        if (screen.Team.Count == 0) return;
        NavPokemonCard(screen, new Rectangle(list.X + list.Width + Gutter, body.Y, body.Width - list.Width - Gutter, body.Height));
    }
}
