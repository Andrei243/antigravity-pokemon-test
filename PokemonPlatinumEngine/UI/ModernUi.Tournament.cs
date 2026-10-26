using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The World Tournament on screen (plan 14 · B11; style guide, "Menu screens (G10)", "The World Tournament"): the events
/// to enter, the bracket of eight and an entrant's card.
/// </summary>
internal static partial class ModernUi
{
    private const float TournamentSeatW = 380, TournamentSeatH = 96, TournamentTop = ContentTop + 90, TournamentRow = 168;
    private const float TournamentEventH = 196;

    public static void DrawTournament(int sw, int sh, TournamentScreen screen, float appear = 1f, float stepAppear = 1f)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("tournamentScreen.title"));
        if (screen.Step == TournamentStep.Events)
            Hints(sw - Margin, 44, Hint("shared.keys.z", "shared.hints.choose"), Hint("shared.keys.x", "shared.hints.back"), Hint("shared.keys.arrows", "shared.hints.move"));
        else
            Hints(sw - Margin, 44, Hint("shared.keys.z", "shared.hints.look"), Hint("shared.keys.x", "shared.hints.back"), Hint("shared.keys.arrows", "shared.hints.move"));
        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;

        if (screen.Step == TournamentStep.Events) TournamentEvents(sw, screen, rise);
        else if (screen.Tournament is { } t) TournamentTree(sw, screen, t, rise);

        // The foot: what is asked
        var foot = new Rectangle(Margin, 936 + rise, sw - Margin * 2, 96);
        Panel(foot, 32);
        UiFonts.DrawCentered(screen.Prompt, foot.X + 48, foot.Y + foot.Height / 2f, 34, Ink, UiWeight.ExtraBold);

        if (screen.Step == TournamentStep.Card && screen.Tournament is { } shown) TournamentCard(sw, sh, shown, screen.EntrantAt(screen.Cursor), stepAppear);
    }

    // The events, a card each: the name, what it is, the titles won, and what opens a locked one
    private static void TournamentEvents(int sw, TournamentScreen screen, float rise)
    {
        float w = sw - Margin * 2 - 400;
        for (int i = 0; i < screen.Events.Count; i++)
        {
            var ev = screen.Events[i];
            bool open = screen.Open[i], under = screen.Cursor == i;
            var r = new Rectangle(Margin + 200, ContentTop + 60 + rise + i * (TournamentEventH + 28), w, TournamentEventH);
            Card(r, 32, under, under ? Gold : null);
            string id = ev.Id;
            UiFonts.DrawCentered(Text.Of($"tournamentScreen.event.{id}"), r.X + 48, r.Y + 52, 40, open ? Ink : Muted, UiWeight.Black);
            UiFonts.DrawCentered(Text.Of($"tournamentScreen.note.{id}"), r.X + 48, r.Y + 104, 26, Muted, UiWeight.ExtraBold);
            if (open)
            {
                string titles = Text.Of("tournamentScreen.titles", ("count", screen.Titles[i]));
                UiFonts.DrawCentered(titles, r.X + 48, r.Y + 150, 24, Ink, UiWeight.Black);
                string points = Text.Of("tournamentScreen.points", ("points", ev.Points));
                Tag(r.X + r.Width - 48 - UiFonts.Measure(points, 26, UiWeight.Black) - 40, r.Y + 34, points, Gold, 36);
            }
            else
            {
                string after = ev.After is { } a ? Text.Of("tournamentScreen.after", ("event", Text.Of($"tournamentScreen.event.{a}"))) : "";
                UiFonts.DrawCentered(after, r.X + 48, r.Y + 150, 24, Muted, UiWeight.Black);
                Tag(r.X + r.Width - 200, r.Y + 34, Text.Of("tournamentScreen.locked"), Muted, 36);
                UiShapes.Fill(r, 32, new Color(226, 232, 244, 110));
            }
        }
    }

    // The bracket of eight: four seats down each side, the lines of every match decided in the winner's colour, the final in the middle
    private static void TournamentTree(int sw, TournamentScreen screen, Tournament t, float rise)
    {
        var bracket = t.Bracket;
        int round = t.Round;
        string roundName = round >= bracket.Rounds ? Text.Of("tournamentScreen.over") : Text.Of($"tournamentScreen.round.{round}");
        string eventId = t.Event.Id;
        string eventName = Text.Of($"tournamentScreen.event.{eventId}");
        UiFonts.DrawCentered(eventName, sw / 2f - UiFonts.Measure(eventName, 26, UiWeight.Black) / 2f, ContentTop + 6 + rise, 26, Muted, UiWeight.Black);
        UiFonts.DrawCentered(roundName, sw / 2f - UiFonts.Measure(roundName, 34, UiWeight.Black) / 2f, ContentTop + 44 + rise, 34, Color.White, UiWeight.Black);

        int opponent = bracket.IsOver ? -1 : bracket.OpponentOf(round, Tournament.Player);
        float halfGap = (sw / 2f - (Margin + TournamentSeatW)) / 3f;

        Vector2 SeatEnd(int seat)
        {
            bool left = seat < TournamentScreen.HalfSeats;
            float y = TournamentTop + rise + seat % TournamentScreen.HalfSeats * TournamentRow + TournamentSeatH / 2f;
            return new Vector2(left ? Margin + TournamentSeatW : sw - Margin - TournamentSeatW, y);
        }
        Vector2 Node(int r, int match)
        {
            int span = 2 << r, first = match * span;
            bool left = first < TournamentScreen.HalfSeats;
            float y = (SeatEnd(first).Y + SeatEnd(first + span - 1).Y) / 2f;
            float x = r >= bracket.Rounds - 1 ? sw / 2f : left ? Margin + TournamentSeatW + (r + 1) * halfGap : sw - Margin - TournamentSeatW - (r + 1) * halfGap;
            return new Vector2(x, y);
        }

        for (int r = 0; r < bracket.Rounds; r++)
            for (int m = 0; m < bracket.MatchesIn(r); m++)
            {
                var node = Node(r, m);
                int winner = bracket.Winners[r][m];
                var (a, b) = bracket.Match(r, m);
                for (int side = 0; side < 2; side++)
                {
                    int who = side == 0 ? a : b;
                    var from = r == 0 ? SeatEnd(m * 2 + side) : Node(r - 1, m * 2 + side);
                    bool lit = winner >= 0 && winner == who;
                    var colour = lit ? (who == Tournament.Player ? Blue : Gold) : new Color(255, 255, 255, 90);
                    float thick = lit ? 7f : 4f;
                    UiShapes.Line(from, new Vector2(node.X, from.Y), thick, colour);
                    UiShapes.Line(new Vector2(node.X, from.Y), node, thick, colour);
                }
            }

        for (int seat = 0; seat < Tournament.Entrants; seat++)
        {
            int entrant = bracket.Seats[seat];
            var end = SeatEnd(seat);
            bool left = seat < TournamentScreen.HalfSeats;
            var r = new Rectangle(left ? end.X - TournamentSeatW : end.X, end.Y - TournamentSeatH / 2f, TournamentSeatW, TournamentSeatH);
            bool under = screen.Cursor == seat && screen.Step == TournamentStep.Tree;
            Color? accent = entrant == Tournament.Player ? Blue : entrant == opponent ? Gold : null;
            Card(r, 26, under || accent != null, under ? null : accent);
            var who = t.Field[entrant];
            string name = who.IsPlayer ? PlayerIdentity.Name : who.Record!.Name;
            string title = who.IsPlayer ? Text.Of("tournamentScreen.you") : Trainer.ClassName(who.Record!.Class);
            UiFonts.DrawCentered(name, r.X + 28, r.Y + 36, 34, Ink, UiWeight.Black);
            UiFonts.DrawCentered(title.ToUpperInvariant(), r.X + 28, r.Y + 70, 20, Muted, UiWeight.Black);
            if (entrant == opponent && t.PlayerStillIn)
                Tag(r.X + r.Width - 104, r.Y + r.Height / 2f - 16, Text.Of("tournamentScreen.next"), Gold, 32);
            if (!bracket.StillIn(entrant)) UiShapes.Fill(r, 26, new Color(226, 232, 244, 140));
        }

        var final = Node(bracket.Rounds - 1, 0);
        var crown = new Rectangle(final.X - 140, final.Y - 50, 280, 100);
        Panel(crown, 28, frame: bracket.IsOver ? Gold : null);
        string middle = bracket.IsOver
            ? (t.Field[bracket.Champion].IsPlayer ? PlayerIdentity.Name : t.Field[bracket.Champion].Record!.Name)
            : Text.Of("tournamentScreen.round.2");
        UiFonts.DrawCentered(middle, crown.X + crown.Width / 2f - UiFonts.Measure(middle, 32, UiWeight.Black) / 2f, crown.Y + 40, 32, Ink, UiWeight.Black);
        if (bracket.IsOver)
        {
            string champion = Text.Of("tournamentScreen.champion");
            UiFonts.DrawCentered(champion, crown.X + crown.Width / 2f - UiFonts.Measure(champion, 20, UiWeight.Black) / 2f, crown.Y + 74, 20, Gold, UiWeight.Black);
        }
    }

    // An entrant's card over the bracket: who they are, how far they have come, and their three at level 50
    private static void TournamentCard(int sw, int sh, Tournament t, int entrant, float appear)
    {
        if (entrant < 0) return;
        UiShapes.Fill(new Rectangle(0, 0, sw, sh), 0, new Color(8, 14, 30, (int)(150 * appear)));
        float rise = (1f - UiMotion.EaseOut(appear)) * 40f;
        var r = new Rectangle(sw / 2f - 460, 160 + rise, 920, 620);
        Panel(r, 36);
        var who = t.Field[entrant];
        string name = who.IsPlayer ? PlayerIdentity.Name : who.Title;
        UiFonts.DrawCentered(name, r.X + 48, r.Y + 52, 42, Ink, UiWeight.Black);
        int reached = t.Bracket.ReachedRound(entrant);
        string state = reached >= t.Bracket.Rounds ? Text.Of("tournamentScreen.champion")
            : t.Bracket.StillIn(entrant) ? Text.Of($"tournamentScreen.round.{reached}")
            : Text.Of("tournamentScreen.outIn", ("round", Text.Of($"tournamentScreen.round.{reached}")));
        Label(state, r.X + 48, r.Y + 88);
        BracketThree(new Rectangle(r.X + 40, r.Y + 130, r.Width - 80, 400), who.Three);
    }
}
