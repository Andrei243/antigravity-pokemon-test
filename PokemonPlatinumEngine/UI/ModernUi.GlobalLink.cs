using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Online;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

// The Global Link counter (plan 07 · O5, O7; style guide, "Menu screens", "The Global Link counter")
internal static partial class ModernUi
{
    /// <summary>A link code as the counter shows it: two groups of four.</summary>
    public static string LinkCodeShown(string code) => code.Length == 8 ? $"{code[..4]} {code[4..]}" : code;

    public static void DrawGlobalLink(int sw, int sh, GlobalLinkScreen screen, float appear)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("globalLink.title"));
        var session = screen.Session;
        Hints(sw - Margin, 44, GlobalLinkHints(screen));

        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;
        var list = new Rectangle(Margin - slide, ContentTop, 1000, ContentBottom - ContentTop);
        var detail = new Rectangle(Margin + 1000 + Gutter + slide, ContentTop, sw - Margin * 2 - 1000 - Gutter, ContentBottom - ContentTop);

        switch (screen.Phase)
        {
            case LinkPhase.Menu:
                Panel(list, 34);
                Panel(detail, 34);
                GlobalLinkMenu(list, screen);
                GlobalLinkCard(detail, screen);
                break;
            case LinkPhase.EnterCode:
                Panel(list, 34);
                Panel(detail, 34);
                GlobalLinkKeypad(list, screen);
                GlobalLinkTyped(detail, session);
                break;
            case LinkPhase.Room:
            case LinkPhase.Trading:
            case LinkPhase.Traded:
                Panel(list, 34);
                Panel(detail, 34);
                GlobalLinkOfferable(list, screen);
                GlobalLinkOffers(detail, screen);
                if (screen.Phase == LinkPhase.Traded && screen.Received is { } received) GlobalLinkTraded(sw, sh, session, received);
                break;
            default:
                GlobalLinkWaiting(sw, session);
                break;
        }
    }

    private static HintItem[] GlobalLinkHints(GlobalLinkScreen screen)
    {
        var keys = new List<HintItem>();
        switch (screen.Phase)
        {
            case LinkPhase.Menu:
                keys.Add(Hint("shared.keys.upDown", "shared.hints.move"));
                keys.Add(Hint("shared.keys.z", "shared.hints.choose"));
                keys.Add(Hint("shared.keys.esc", "shared.hints.back"));
                break;
            case LinkPhase.EnterCode:
                keys.Add(Hint("shared.keys.arrows", "shared.hints.move"));
                keys.Add(Hint("shared.keys.z", "globalLink.hint.type"));
                keys.Add(Hint("shared.keys.esc", "globalLink.hint.erase"));
                break;
            case LinkPhase.Room:
                keys.Add(Hint("shared.keys.upDown", "shared.hints.move"));
                keys.Add(Hint("shared.keys.z", screen.SelectedIsOffered && screen.Session.MayConfirm ? "globalLink.hint.trade" : "globalLink.hint.offer"));
                keys.Add(Hint("shared.keys.esc", screen.Session.MyOffer != null || screen.Session.TheirOffer != null ? "globalLink.hint.callOff" : "globalLink.hint.leave"));
                break;
            case LinkPhase.Traded:
            case LinkPhase.Failed:
                keys.Add(Hint("shared.keys.z", "shared.hints.choose"));
                break;
            default:
                keys.Add(Hint("shared.keys.esc", "shared.hints.back"));
                break;
        }
        return keys.ToArray();
    }

    private static void GlobalLinkMenu(Rectangle list, GlobalLinkScreen screen)
    {
        var rows = new[] { "globalLink.menu.makeCode", "globalLink.menu.enterCode", "globalLink.menu.leave" };
        for (int row = 0; row < rows.Length; row++)
        {
            var r = RowRect(list, row);
            bool selected = row == (int)screen.MenuRow;
            ListRow(r, selected, Text.Of(rows[row]), true, 48);
        }
        float y = list.Y + 24 + rows.Length * RowPitch + 24;
        UiShapes.Fill(new Rectangle(list.X + 36, y, list.Width - 72, 3), 1.5f, Rule);
        DrawWrapped(screen.Session.LineText, list.X + 44, y + 28, list.Width - 88, 30, Ink, 42, UiWeight.ExtraBold);
    }

    private static void GlobalLinkCard(Rectangle detail, GlobalLinkScreen screen)
    {
        float x = detail.X + 44, y = detail.Y + 60;
        Field(Text.Of("globalLink.friendCode"), screen.Session.FriendCode ?? "", x, y, 52, 64);
        y += 150;
        if (screen.ServerShown is { } server) Field(Text.Of("globalLink.server"), server, x, y, 32, 48);
        y += 120;
        DrawWrapped(Text.Of("globalLink.about"), x, y, detail.Width - 88, 26, Muted, 38, UiWeight.ExtraBold);
    }

    private static void GlobalLinkKeypad(Rectangle list, GlobalLinkScreen screen)
    {
        const float key = 180, gap = 24;
        float left = list.X + (list.Width - (key * 3 + gap * 2)) / 2f, top = list.Y + 60;
        for (int k = 0; k < GlobalLinkScreen.KeypadKeys; k++)
        {
            int row = k / GlobalLinkScreen.KeypadColumns, column = k % GlobalLinkScreen.KeypadColumns;
            var r = new Rectangle(left + column * (key + gap), top + row * (key * 0.8f + gap), key, key * 0.8f);
            bool selected = k == screen.Key;
            int digit = GlobalLinkScreen.DigitOf(k);
            string label = digit >= 0 ? digit.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : Text.Of(k == GlobalLinkScreen.OkKey ? "globalLink.key.ok" : "globalLink.key.erase");
            if (selected)
            {
                UiShapes.Shadow(r, 26, 18, new Vector2(0, 5), Selection with { A = 120 });
                UiShapes.Shape(r, 26, Lighter(Selection, 0.14f), Darker(Selection, 0.06f), Darker(Selection, 0.3f), 3);
            }
            else UiShapes.Fill(r, 26, Disc);
            float size = digit >= 0 ? 64 : 34, w = UiFonts.Measure(label, size, UiWeight.Black);
            UiFonts.DrawCentered(label, r.X + (r.Width - w) / 2f, r.Y + r.Height / 2f, size, selected ? Color.White : Ink, UiWeight.Black);
        }
    }

    private static void GlobalLinkTyped(Rectangle detail, GlobalLinkSession session)
    {
        float x = detail.X + 44, y = detail.Y + 60;
        Label(Text.Of("globalLink.theirCode"), x, y);
        const float box = 76, gap = 12;
        for (int i = 0; i < OnlineRules.CodeDigits; i++)
        {
            float bx = x + i * (box + gap) + (i >= 4 ? 28 : 0);
            var r = new Rectangle(bx, y + 40, box, box * 1.2f);
            UiShapes.Fill(r, 16, Disc);
            if (i < session.Typed.Length)
            {
                string d = session.Typed[i].ToString();
                float w = UiFonts.Measure(d, 60, UiWeight.Black);
                UiFonts.DrawCentered(d, r.X + (r.Width - w) / 2f, r.Y + r.Height / 2f, 60, Ink, UiWeight.Black);
            }
            else if (i == session.Typed.Length) UiShapes.Fill(new Rectangle(r.X + 14, r.Y + r.Height - 18, r.Width - 28, 6), 3, Selection);
        }
        DrawWrapped(session.LineText, x, y + 200, detail.Width - 88, 30, Ink, 42, UiWeight.ExtraBold);
    }

    private static void GlobalLinkOfferable(Rectangle list, GlobalLinkScreen screen)
    {
        var all = screen.Offerable;
        bool scrolls = all.Count > GlobalLinkScreen.VisibleRows;
        for (int row = 0; row < GlobalLinkScreen.VisibleRows && screen.First + row < all.Count; row++)
        {
            int index = screen.First + row;
            var pokemon = all[index];
            var r = RowRect(list, row, 24, scrolls);
            bool selected = index == screen.Cursor;
            bool offered = screen.Session.MyOffer?.Id == pokemon.Id;
            var ink = ListRow(r, selected, pokemon.DisplayName);
            RowIcon(r, PixelArtGenerator.IconOf(pokemon), 1f, selected, hop: Hop(selected));
            RowValue(r, offered ? Text.Of("globalLink.offered") : Text.Of("jobs.level", ("level", pokemon.Level)), offered && !selected ? Selection : ink, 28);
        }
        ScrollBar(list, screen.First, GlobalLinkScreen.VisibleRows, all.Count, 24);
    }

    private static void GlobalLinkOffers(Rectangle detail, GlobalLinkScreen screen)
    {
        var session = screen.Session;
        float x = detail.X + 44, room = detail.Width - 88;
        float half = (detail.Height - 260) / 2f;
        GlobalLinkOffer(new Rectangle(x, detail.Y + 36, room, half), Text.Of("globalLink.youOffer"),
            session.MyOffer != null ? OfferedOf(screen.Offerable, session.MyOffer) : null, session.MineConfirmed);
        GlobalLinkOffer(new Rectangle(x, detail.Y + 36 + half, room, half), Text.Of("globalLink.theyOffer", ("name", session.OtherName ?? "")),
            screen.Theirs, session.TheirsConfirmed);
        float y = detail.Y + detail.Height - 210;
        UiShapes.Fill(new Rectangle(x, y, room, 3), 1.5f, Rule);
        if (session.OtherFriendCode is { } code) UiFonts.Draw(Text.Of("globalLink.otherCode", ("code", code)), x, y + 22, 24, Muted, UiWeight.Black);
        DrawWrapped(session.LineText, x, y + 64, room, 28, Ink, 38, UiWeight.ExtraBold);
        // A Pokémon refused for what it is: the server's faults, a line each, under the line that says so
        if (session.Details is { Length: > 0 } details) DrawWrapped(details, x, y + 140, room, 22, Muted, 30, UiWeight.ExtraBold);
    }

    private static Pokemon? OfferedOf(IReadOnlyList<Pokemon> all, SavedPokemonData offer)
    {
        foreach (var p in all) if (p.Id == offer.Id) return p;
        return null;
    }

    private static void GlobalLinkOffer(Rectangle r, string caption, Pokemon? pokemon, bool confirmed)
    {
        Label(caption, r.X, r.Y);
        var disc = new Vector2(r.X + 110, r.Y + 40 + 110);
        BallDisc(disc, 100);
        if (pokemon == null)
        {
            UiFonts.Draw(Text.Of("globalLink.nothingYet"), disc.X + 140, disc.Y - 18, 32, Muted, UiWeight.Black);
            return;
        }
        PixelArt(PixelArtGenerator.SpriteOf(pokemon), disc, 1);
        float tx = disc.X + 140;
        UiFonts.Draw(pokemon.DisplayName, tx, disc.Y - 80, 40, Ink, UiWeight.Black);
        UiFonts.Draw(Text.Of("jobs.level", ("level", pokemon.Level)), tx, disc.Y - 30, 28, Muted, UiWeight.Black);
        string trainer = pokemon.OriginalTrainer?.Name is { Length: > 0 } ot ? ot : "";
        if (trainer.Length > 0) UiFonts.Draw(Text.Of("globalLink.trainer", ("name", trainer)), tx, disc.Y + 10, 26, Muted, UiWeight.ExtraBold);
        if (pokemon.HeldItem is { } item) UiFonts.Draw(Text.Of("globalLink.holding", ("item", item.DisplayName)), tx, disc.Y + 46, 26, Muted, UiWeight.ExtraBold);
        if (confirmed) UiFonts.Draw(Text.Of("globalLink.ready"), tx, disc.Y + 86, 28, Green, UiWeight.Black);
    }

    private static void GlobalLinkTraded(int sw, int sh, GlobalLinkSession session, Pokemon received)
    {
        Dim(sw, sh, 150);
        var r = new Rectangle(sw / 2f - 550, sh / 2f - 240, 1100, 480);
        Panel(r, 36);
        var disc = new Vector2(r.X + 56 + 130, r.Y + 56 + 130);
        BallDisc(disc, 130);
        PixelArt(PixelArtGenerator.SpriteOf(received), disc, 2);
        DrawWrapped(session.LineText, disc.X + 170, r.Y + 90, r.Width - 380, 34, Ink, 48, UiWeight.Black);
    }

    private static void GlobalLinkWaiting(int sw, GlobalLinkSession session)
    {
        var r = new Rectangle(sw / 2f - 640, ContentTop + 120, 1280, 560);
        Panel(r, 36);
        float x = r.X + 64, y = r.Y + 64, room = r.Width - 128;
        if (session.Phase == LinkPhase.WaitingForOther && session.Code is { } code)
        {
            Label(Text.Of("globalLink.yourCode"), x, y);
            UiFonts.Draw(LinkCodeShown(code), x, y + 36, 110, Ink, UiWeight.Black);
            int seconds = (int)Math.Ceiling(session.CodeSecondsLeft);
            UiFonts.Draw(Text.Of("globalLink.codeLeft", ("minutes", seconds / 60), ("seconds", (seconds % 60).ToString("00", System.Globalization.CultureInfo.InvariantCulture))),
                x, y + 170, 30, Muted, UiWeight.Black);
            y += 240;
        }
        DrawWrapped(session.LineText, x, y, room, 34, session.Phase == LinkPhase.Failed ? Red : Ink, 48, UiWeight.Black);
        if (session.Phase == LinkPhase.Failed && session.Details is { Length: > 0 } details)
            DrawWrapped(details, x, y + 120, room, 24, Muted, 34, UiWeight.ExtraBold);
    }
}
