using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Battle Dome's bracket on screen (plan 14 · B8; style guide, "Menu screens (G10)", "The bracket"): the tree of
/// sixteen, an entrant's card, and the pick of two to send in.
/// </summary>
internal static partial class ModernUi
{
    private const float BracketSeatW = 320, BracketSeatH = 76, BracketTop = ContentTop + 56, BracketRow = 92;

    /// <summary>
    /// The bracket as it stands: eight seats down each side, the lines of every match decided drawn in the winner's
    /// colour toward the final in the middle, the player's seat in blue and their next opponent's in gold; the round
    /// under way over it; along the foot what is asked and the streak; and over it all the card or the pick.
    /// </summary>
    public static void DrawBracket(int sw, int sh, BracketScreen screen, float appear = 1f, float stepAppear = 1f)
    {
        if (screen.Dome is not { } dome) return;
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("bracketScreen.title"));
        if (screen.Step == BracketStep.Pick)
            Hints(sw - Margin, 44, Hint("shared.keys.z", "shared.hints.choose"), Hint("shared.keys.x", "bracketScreen.hint.bracket"));
        else
            Hints(sw - Margin, 44, Hint("shared.keys.z", "shared.hints.look"), Hint("shared.keys.x", "shared.hints.back"), Hint("shared.keys.arrows", "shared.hints.move"));
        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;

        int round = dome.Round;
        string roundName = round >= dome.Bracket.Rounds ? Text.Of("bracketScreen.over") : Text.Of($"bracketScreen.round.{round}");
        UiFonts.DrawCentered(roundName, sw / 2f - UiFonts.Measure(roundName, 34, UiWeight.Black) / 2f, ContentTop + 16 + rise, 34, Color.White, UiWeight.Black);

        var bracket = dome.Bracket;
        int opponent = bracket.OpponentOf(Math.Min(round, bracket.Rounds - 1), BattleDomeChallenge.Player);
        float halfGap = (sw / 2f - (Margin + BracketSeatW)) / 4f;

        // Where a seat's line leaves it, and where each match of the first three rounds of a half meets
        Vector2 SeatEnd(int seat)
        {
            bool left = seat < BracketScreen.HalfSeats;
            float y = BracketTop + rise + seat % BracketScreen.HalfSeats * BracketRow + BracketSeatH / 2f;
            return new Vector2(left ? Margin + BracketSeatW : sw - Margin - BracketSeatW, y);
        }
        Vector2 Node(int r, int match)
        {
            // The seats a match of round r gathers, and their middle
            int span = 2 << r, first = match * span;
            bool left = first < BracketScreen.HalfSeats;
            float y = (SeatEnd(first).Y + SeatEnd(first + span - 1).Y) / 2f;
            float x = r >= bracket.Rounds - 1 ? sw / 2f : left ? Margin + BracketSeatW + (r + 1) * halfGap : sw - Margin - BracketSeatW - (r + 1) * halfGap;
            return new Vector2(x, y);
        }

        // The lines: each side of every match from where it comes to where the match meets, lit for the side that won
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
                    var colour = winner >= 0 && winner == who ? (who == BattleDomeChallenge.Player ? Blue : Gold) : new Color(255, 255, 255, 90);
                    float thick = winner >= 0 && winner == who ? 7f : 4f;
                    UiShapes.Line(from, new Vector2(node.X, from.Y), thick, colour);
                    UiShapes.Line(new Vector2(node.X, from.Y), node, thick, colour);
                }
            }

        // The seats
        for (int seat = 0; seat < BattleDomeChallenge.Entrants; seat++)
        {
            int entrant = bracket.Seats[seat];
            var end = SeatEnd(seat);
            bool left = seat < BracketScreen.HalfSeats;
            var r = new Rectangle(left ? end.X - BracketSeatW : end.X, end.Y - BracketSeatH / 2f, BracketSeatW, BracketSeatH);
            bool under = screen.Cursor == seat && screen.Step != BracketStep.Pick;
            Color? accent = entrant == BattleDomeChallenge.Player ? Blue : entrant == opponent ? Gold : null;
            Card(r, 22, under || accent != null, under ? null : accent);
            var who = dome.Field[entrant];
            string name = who.IsPlayer ? PlayerIdentity.Name : who.Opponent!.Record.Name;
            string title = who.IsPlayer ? Text.Of("bracketScreen.you") : who.Opponent!.Record.Class;
            UiFonts.DrawCentered(name, r.X + 24, r.Y + 28, 30, Ink, UiWeight.Black);
            UiFonts.DrawCentered(title.ToUpperInvariant(), r.X + 24, r.Y + 56, 18, Muted, UiWeight.Black);
            if (entrant == opponent && dome.PlayerStillIn && !bracket.IsOver)
                Tag(r.X + r.Width - 94, r.Y + r.Height / 2f - 15, Text.Of("bracketScreen.next"), Gold, 30);
            if (!bracket.StillIn(entrant)) UiShapes.Fill(r, 22, new Color(226, 232, 244, 140));
        }

        // The final's place in the middle: its winner once there is one
        var final = Node(bracket.Rounds - 1, 0);
        var crown = new Rectangle(final.X - 130, final.Y - 46, 260, 92);
        Panel(crown, 26, frame: bracket.IsOver ? Gold : null);
        string middle = bracket.IsOver
            ? (dome.Field[bracket.Champion].IsPlayer ? PlayerIdentity.Name : dome.Field[bracket.Champion].Opponent!.Record.Name)
            : Text.Of("bracketScreen.round.3");
        UiFonts.DrawCentered(middle, crown.X + crown.Width / 2f - UiFonts.Measure(middle, 30, UiWeight.Black) / 2f, crown.Y + 36, 30, Ink, UiWeight.Black);
        if (bracket.IsOver)
        {
            string champion = Text.Of("bracketScreen.champion");
            UiFonts.DrawCentered(champion, crown.X + crown.Width / 2f - UiFonts.Measure(champion, 20, UiWeight.Black) / 2f, crown.Y + 68, 20, Gold, UiWeight.Black);
        }

        // The foot: what is asked, and the streak
        var foot = new Rectangle(Margin, 936 + rise, sw - Margin * 2, 96);
        Panel(foot, 32);
        UiFonts.DrawCentered(screen.Prompt, foot.X + 48, foot.Y + foot.Height / 2f, 34, Ink, UiWeight.ExtraBold);
        string streak = $"{dome.StreakBefore:N0}";
        float sx = foot.X + foot.Width - 48 - Math.Max(UiFonts.Measure(streak, 40, UiWeight.Black), UiFonts.Measure(Text.Of("records.streak"), 20, UiWeight.Black));
        Label(Text.Of("records.streak"), sx, foot.Y + 14);
        UiFonts.Draw(streak, sx, foot.Y + 38, 40, Ink, UiWeight.Black);

        if (screen.Step == BracketStep.Card) BracketCard(sw, sh, screen, dome, screen.EntrantAt(screen.Cursor), stepAppear);
        else if (screen.Step == BracketStep.Pick) BracketPick(sw, sh, screen, dome, stepAppear);
    }

    // The three of an entrant, a row each: the icon, the name, the level and the types
    private static void BracketThree(Rectangle r, IReadOnlyList<Pokemon> three, IReadOnlyList<int>? chosen = null, int under = -1)
    {
        for (int i = 0; i < three.Count; i++)
        {
            var p = three[i];
            var row = new Rectangle(r.X, r.Y + i * 132, r.Width, 116);
            bool picked = chosen != null && chosen.Contains(i);
            Card(row, 24, under == i || picked, picked ? Green : null);
            Portrait(new Vector2(row.X + 62, row.Y + row.Height / 2f), 44, p, 2, under == i);
            float nx = row.X + 124;
            NameWithGender(p, nx, row.Y + 38, 34);
            string level = Text.Of("bracketScreen.level", ("level", Math.Min(p.Level, FrontierRules.Level)));
            UiFonts.DrawCentered(level, nx, row.Y + 84, 24, Muted, UiWeight.Black);
            TypePills(nx + UiFonts.Measure(level, 24, UiWeight.Black) + 18, row.Y + 68, p, 32);
            if (picked) Tag(row.X + row.Width - 150, row.Y + row.Height / 2f - 17, Text.Of("bracketScreen.sentIn"), Green, 34);
        }
    }

    // An entrant's card over the bracket: who they are, how far they have come, and their three
    private static void BracketCard(int sw, int sh, BracketScreen screen, BattleDomeChallenge dome, int entrant, float appear)
    {
        if (entrant < 0) return;
        UiShapes.Fill(new Rectangle(0, 0, sw, sh), 0, new Color(8, 14, 30, (int)(150 * appear)));
        float rise = (1f - UiMotion.EaseOut(appear)) * 40f;
        var r = new Rectangle(sw / 2f - 460, 160 + rise, 920, 620);
        Panel(r, 36);
        var who = dome.Field[entrant];
        string name = who.IsPlayer ? PlayerIdentity.Name : who.Title;
        UiFonts.DrawCentered(name, r.X + 48, r.Y + 52, 42, Ink, UiWeight.Black);
        int reached = dome.Bracket.ReachedRound(entrant);
        string state = reached >= dome.Bracket.Rounds ? Text.Of("bracketScreen.champion")
            : dome.Bracket.StillIn(entrant) ? Text.Of($"bracketScreen.round.{reached}")
            : Text.Of("bracketScreen.outIn", ("round", Text.Of($"bracketScreen.round.{reached}")));
        Label(state, r.X + 48, r.Y + 88);
        BracketThree(new Rectangle(r.X + 40, r.Y + 130, r.Width - 80, 400), who.IsPlayer ? screen.Own : who.Opponent!.Pokemon);
    }

    // The pick: the opponent's card on the left, the player's three on the right with BATTLE under them
    private static void BracketPick(int sw, int sh, BracketScreen screen, BattleDomeChallenge dome, float appear)
    {
        UiShapes.Fill(new Rectangle(0, 0, sw, sh), 0, new Color(8, 14, 30, (int)(150 * appear)));
        float rise = (1f - UiMotion.EaseOut(appear)) * 40f;
        float w = (sw - Margin * 2 - Gutter) / 2f;
        var theirs = new Rectangle(Margin, 150 + rise, w, 680);
        var mine = new Rectangle(Margin + w + Gutter, 150 + rise, w, 680);
        Panel(theirs, 36);
        Panel(mine, 36);
        if (dome.Opponent is { } opponent)
        {
            UiFonts.DrawCentered(opponent.Title, theirs.X + 40, theirs.Y + 48, 36, Ink, UiWeight.Black);
            Label(Text.Of("bracketScreen.theirThree"), theirs.X + 40, theirs.Y + 80);
            BracketThree(new Rectangle(theirs.X + 32, theirs.Y + 128, theirs.Width - 64, 400), opponent.Opponent!.Pokemon);
        }
        UiFonts.DrawCentered(Text.Of("bracketScreen.yourThree"), mine.X + 40, mine.Y + 48, 36, Ink, UiWeight.Black);
        Label(Text.Of("bracketScreen.chooseTwo"), mine.X + 40, mine.Y + 80);
        BracketThree(new Rectangle(mine.X + 32, mine.Y + 128, mine.Width - 64, 400), screen.Own, screen.Chosen, screen.PickCursor);
        var go = new Rectangle(mine.X + mine.Width / 2f - 150, mine.Y + 560, 300, 80);
        bool goUnder = screen.PickCursor == BracketScreen.GoPlace;
        UiShapes.Shape(go, 40, screen.Ready ? Lighter(Red, 0.12f) : Muted, screen.Ready ? Darker(Red, 0.1f) : Muted, goUnder ? Color.White : default, goUnder ? 6f : 0f);
        string battle = Text.Of("bracketScreen.battle");
        UiFonts.DrawCentered(battle, go.X + go.Width / 2f - UiFonts.Measure(battle, 36, UiWeight.Black) / 2f, go.Y + go.Height / 2f, 36, Color.White, UiWeight.Black);
    }
}
