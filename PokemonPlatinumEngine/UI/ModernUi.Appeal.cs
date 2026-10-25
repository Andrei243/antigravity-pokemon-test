using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Contest;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// A Hoenn contest on screen (plan 21 · H11; style guide, "Hoenn's contests"): the four contestants in the order of their
/// turns, each with its hearts of the round, its points, the stars of its condition and the judge's eye; the crowd's
/// meter of five lights; the player's four moves with what each does; the lines of the appeals in a box at the foot; and
/// the results' board. Our own pictures: a Pokémon is its menu sprite on a disc, a heart a heart, the crowd a row of
/// lights, the judge's eye an eye over the card.
/// </summary>
internal static partial class ModernUi
{
    private const float AppealRowHeight = 168;

    private static void AppealFrame(int sw, int sh, AppealScreen screen, params HintItem[] hints)
    {
        var round = screen.Round!;
        Backdrop(sw, sh);
        string title = Text.Of("appeal.title");
        ScreenTitle(title);
        float x = 64 + UiFonts.Measure(title, 52, UiWeight.Black) + 32;
        x += Tag(x, 50, Text.Of($"appeal.rank.{round.Rank}"), Frame, 42) + 12;
        x += Tag(x, 50, Text.Of($"condition.quality.{round.Category}"), QualityColor(round.Category), 42) + 12;
        if (screen.Phase is AppealPhase.Choose or AppealPhase.Turn or AppealPhase.RoundEnd)
            Tag(x, 50, Text.Of("appeal.roundTag", ("n", screen.RoundNumber), ("of", AppealRound.Appeals)), Selection, 42);
        if (hints.Length > 0) Hints(sw - Margin, 44, hints);
    }

    // A place in the order: "1st"… by the interface table
    private static string TurnTag(int turn) => Text.Of($"appeal.turn.{turn}");

    public static void DrawAppeals(int sw, int sh, AppealScreen screen)
    {
        var round = screen.Round!;
        switch (screen.Phase)
        {
            case AppealPhase.Lineup:
                AppealFrame(sw, sh, screen, Hint("shared.keys.z", "shared.hints.next"));
                AppealLineup(sw, screen);
                break;
            case AppealPhase.Choose:
                AppealFrame(sw, sh, screen, Hint("shared.keys.upDown", "shared.hints.move"), Hint("shared.keys.z", "shared.hints.choose"));
                AppealStage(sw, screen);
                AppealMoves(sw, screen);
                break;
            case AppealPhase.Turn or AppealPhase.RoundEnd:
                AppealFrame(sw, sh, screen, Hint("shared.keys.z", "shared.hints.next"));
                AppealStage(sw, screen);
                AppealMover(sw, screen);
                break;
            default:
                AppealFrame(sw, sh, screen, Hint("shared.keys.z", "shared.hints.next"));
                AppealResults(sw, screen);
                break;
        }
        ContestLine(sw, screen.Line, screen.Phase != AppealPhase.Choose);
    }

    // ------------------------------------------------------------------ the line-up

    private static void AppealLineup(int sw, AppealScreen screen)
    {
        var round = screen.Round!;
        float top = ContentTop, height = ContentBottom - ContestBox - Gutter - top;
        float w = (sw - Margin * 2 - Gutter * 3) / 4f;
        float t = (float)FrameClock.Now;
        for (int turn = 0; turn < AppealRound.Contestants; turn++)
        {
            int who = round.ContestantAtTurn(turn);
            var a = round.Appealers[who];
            var r = new Rectangle(Margin + turn * (w + Gutter), top, w, height);
            bool on = screen.Showing == turn;
            bool shown = screen.Showing >= turn;
            Card(r, 30, on, QualityColor(round.Category));
            Tag(r.X + 24, r.Y + 22, TurnTag(turn), a.IsPlayer ? Selection : Frame, 38);
            var c = new Vector2(r.X + r.Width / 2f, r.Y + 60 + (r.Height - 250) / 2f);
            float radius = MathF.Min(r.Width * 0.38f, (r.Height - 250) / 2f);
            BallDisc(c, radius);
            PixelArt(PixelArtGenerator.SpriteOf(a.Pokemon), c, radius >= 150 ? 2 : 1, on ? MathF.Abs(MathF.Sin(t * 7f)) * 14f : 0f);
            string name = a.Pokemon.DisplayName;
            UiFonts.DrawCentered(name, r.X + (r.Width - UiFonts.Measure(name, 34, UiWeight.Black)) / 2f, r.Y + r.Height - 138, 34, Ink, UiWeight.Black);
            UiFonts.DrawCentered(a.Trainer, r.X + (r.Width - UiFonts.Measure(a.Trainer, 24, UiWeight.ExtraBold)) / 2f, r.Y + r.Height - 98, 24, Muted, UiWeight.ExtraBold);
            if (shown)
            {
                string cond = Text.Of("appeal.conditionPoints", ("n", a.Round1Points));
                UiFonts.DrawCentered(cond, r.X + (r.Width - UiFonts.Measure(cond, 28, UiWeight.Black)) / 2f, r.Y + r.Height - 50, 28, QualityColor(round.Category), UiWeight.Black);
            }
        }
    }

    // ------------------------------------------------------------------ the stage

    // The four rows, in the order of this round's turns, on the left; the crowd's meter on the right
    private static void AppealStage(int sw, AppealScreen screen)
    {
        var round = screen.Round!;
        float top = ContentTop;
        float width = 1160;
        for (int turn = 0; turn < AppealRound.Contestants; turn++)
        {
            int who = round.Appealers.Select((_, i) => i).First(i => screen.ShownOrder[i] == turn);
            var a = round.Appealers[who];
            var r = new Rectangle(Margin, top + turn * (AppealRowHeight + 10), width, AppealRowHeight);
            bool moving = screen.Mover == who && screen.Phase is AppealPhase.Turn;
            Card(r, 26, moving, a.IsPlayer ? Selection : QualityColor(round.Category));
            Tag(r.X + 20, r.Y + 18, TurnTag(turn), a.IsPlayer ? Selection : Frame, 36);
            var icon = PixelArtGenerator.IconOf(a.Pokemon);
            PixelArt(icon, new Vector2(r.X + 190, r.Y + r.Height / 2f), 1, moving ? MathF.Abs(MathF.Sin((float)FrameClock.Now * 7f)) * 8f : 0f);
            UiFonts.DrawCentered(a.Pokemon.DisplayName, r.X + 250, r.Y + 44, 34, Ink, UiWeight.Black);
            UiFonts.DrawCentered(a.Trainer, r.X + 250, r.Y + 84, 24, Muted, UiWeight.ExtraBold);
            // The condition's stars, and the judge's eye if it has begun a combo
            for (int s = 0; s < 3; s++) Star(new Vector2(r.X + 262 + s * 38, r.Y + 130), 15, s < screen.ShownStars[who] ? Gold : Rule);
            if (screen.ShownEye[who]) AppealEye(new Vector2(r.X + 400, r.Y + 130), 18);
            if (screen.ShownNervous[who]) Tag(r.X + 440, r.Y + 112, Text.Of("appeal.nervous"), Red, 34);
            else if (round.Status[who].TurnDisabled && screen.Phase != AppealPhase.Lineup) Tag(r.X + 440, r.Y + 112, Text.Of("appeal.sitsOut"), Muted, 34);

            // The hearts of the round, and the points
            int appeal = screen.ShownAppeal[who];
            int hearts = AppealRules.HeartsOf(appeal);
            Label(Text.Of("appeal.thisRound"), r.X + 650, r.Y + 20);
            HeartRow(r.X + 650, r.Y + 76, hearts, AppealRules.HeartsOf(80), 17f);
            string pts = appeal < 0 ? $"{appeal}" : appeal.ToString();
            UiFonts.DrawCentered(pts, r.X + 650, r.Y + 130, 34, appeal < 0 ? Red : Ink, UiWeight.Black);
            Label(Text.Of("appeal.total"), r.X + r.Width - 190, r.Y + 20);
            string total = screen.ShownTotal[who].ToString();
            UiFonts.Draw(total, r.X + r.Width - 190, r.Y + 54, 56, Ink, UiWeight.Black);
        }

        // The crowd
        var crowd = new Rectangle(Margin + width + Gutter, top, sw - Margin * 2 - width - Gutter, 150);
        Panel(crowd, 30);
        Label(Text.Of("appeal.crowd"), crowd.X + 36, crowd.Y + 24);
        for (int i = 0; i < AppealRound.ApplauseSize; i++)
        {
            var lamp = new Rectangle(crowd.X + 36 + i * 74, crowd.Y + 66, 60, 60);
            bool lit = i < screen.ShownCrowd;
            UiShapes.Fill(lamp, 16, lit ? Gold : Rule);
            if (lit) UiShapes.Fill(new Rectangle(lamp.X + 10, lamp.Y + 10, 20, 12), 6, Lighter(Gold, 0.6f));
        }
        if (round.CrowdFrozen)
            Tag(crowd.X + crowd.Width - 36 - UiFonts.Measure(Text.Of("appeal.crowdHeld"), 20, UiWeight.Black) - 28, crowd.Y + 20, Text.Of("appeal.crowdHeld"), Muted, 34);
    }

    // The judge's eye: a lens with a pupil, over the card of whoever has begun a combo
    private static void AppealEye(Vector2 c, float r)
    {
        UiShapes.Circle(c, r, Color.White);
        UiShapes.Ring(c, r, 3, Frame);
        UiShapes.Circle(c + new Vector2(2, 0), r * 0.5f, Frame);
        UiShapes.Circle(c + new Vector2(0, -r * 0.2f), r * 0.16f, Color.White);
    }

    // The one whose turn it is, large, on the right under the crowd
    private static void AppealMover(int sw, AppealScreen screen)
    {
        var round = screen.Round!;
        float left = Margin + 1160 + Gutter;
        var box = new Rectangle(left, ContentTop + 150 + Gutter, sw - Margin - left, ContentBottom - ContestBox - Gutter - (ContentTop + 150 + Gutter));
        Panel(box, 34);
        if (screen.Mover < 0) return;
        var a = round.Appealers[screen.Mover];
        var c = new Vector2(box.X + box.Width / 2f, box.Y + box.Height / 2f - 20);
        BallDisc(c, 150);
        PixelArt(PixelArtGenerator.SpriteOf(a.Pokemon), c, 2, screen.Phase == AppealPhase.Turn ? MathF.Abs(MathF.Sin((float)FrameClock.Now * 5f)) * 10f : 0f);
        if (screen.MoveShown.Length > 0)
        {
            var data = MoveDatabase.Has(screen.MoveShown) ? MoveDatabase.Get(screen.MoveShown) : null;
            string move = LocalNames.Move(screen.MoveShown);
            float tw = UiFonts.Measure(move, 36, UiWeight.Black);
            UiFonts.DrawCentered(move, box.X + (box.Width - tw) / 2f, box.Y + box.Height - 70, 36, Ink, UiWeight.Black);
            if (data != null)
                Tag(box.X + (box.Width - 150) / 2f, box.Y + box.Height - 44 - 4, Text.Of($"condition.quality.{data.HoennContest.Category}"), QualityColor(data.HoennContest.Category), 34);
        }
    }

    // ------------------------------------------------------------------ choosing a move

    private static void AppealMoves(int sw, AppealScreen screen)
    {
        var round = screen.Round!;
        var player = round.Appealers[screen.Player];
        float left = Margin + 1160 + Gutter;
        var box = new Rectangle(left, ContentTop + 150 + Gutter, sw - Margin - left, ContentBottom - ContestBox - Gutter - (ContentTop + 150 + Gutter));
        Panel(box, 34);
        Label(Text.Of("appeal.yourMoves"), box.X + 36, box.Y + 26);
        var status = round.Status[screen.Player];
        for (int i = 0; i < player.Moves.Count; i++)
        {
            var move = player.Moves[i];
            var r = new Rectangle(box.X + 24, box.Y + 66 + i * 112, box.Width - 48, 100);
            Card(r, 22, i == screen.MoveCursor, QualityColor(move.Contest.Category));
            UiFonts.DrawCentered(move.Move.DisplayName, r.X + 24, r.Y + 32, 32, Ink, UiWeight.Black);
            var effect = HoennContestDatabase.Effect(move.Contest.Effect);
            HeartRow(r.X + 24, r.Y + 72, Math.Min(effect.Appeal / 10, 8), 8, 13f);
            float x = r.X + r.Width - 20;
            string cat = Text.Of($"condition.quality.{move.Contest.Category}");
            x -= UiFonts.Measure(cat, 20, UiWeight.Black) + 28;
            Tag(x, r.Y + 14, cat, QualityColor(move.Contest.Category), 32);
            bool finishes = status.PrevMove != null && HoennContestDatabase.AreCombo(status.PrevMove.Contest, move.Contest);
            if (finishes && status.HasJudgesAttention) Tag(x - 150, r.Y + 14, Text.Of("appeal.comboTag"), Gold, 32);
            else if (move.Contest.ComboStarter != 0) Tag(x - 150, r.Y + 14, Text.Of("appeal.starterTag"), Muted, 32);
        }
        // What the move under the cursor does
        if (screen.CurrentMove is { } shown)
        {
            var effect = HoennContestDatabase.Effect(shown.Contest.Effect);
            DrawWrapped(Text.Of($"appeal.effect.{effect.Name}"), box.X + 36, box.Y + 66 + player.Moves.Count * 112 + 10, box.Width - 72, 26, Muted, 34, UiWeight.ExtraBold);
        }
    }

    // ------------------------------------------------------------------ the results

    private static void AppealResults(int sw, AppealScreen screen)
    {
        var round = screen.Round!;
        var panel = new Rectangle(Margin, ContentTop, sw - Margin * 2, ContentBottom - ContestBox - Gutter - ContentTop);
        Panel(panel, 34);
        
        float[] col = { panel.X + 700, panel.X + 1000, panel.X + 1300 };
        Label(Text.Of("appeal.colCondition"), col[0], panel.Y + 28);
        if (screen.ResultStage >= 1) Label(Text.Of("appeal.colAppeals"), col[1], panel.Y + 28);
        if (screen.ResultStage >= 2) Label(Text.Of("appeal.colTotal"), col[2], panel.Y + 28);
        // The contestants, in the order of their places once those are known
        var order = Enumerable.Range(0, AppealRound.Contestants).OrderBy(i => screen.ResultStage >= 2 ? round.FinalStandings[i] : i).ToArray();
        for (int row = 0; row < order.Length; row++)
        {
            int who = order[row];
            var a = round.Appealers[who];
            var r = new Rectangle(panel.X + 24, panel.Y + 64 + row * 190, panel.Width - 48, 176);
            bool winner = screen.ResultStage >= 3 && round.FinalStandings[who] == 0;
            Card(r, 26, winner, a.IsPlayer ? Selection : QualityColor(round.Category));
            if (screen.ResultStage >= 2) Tag(r.X + 20, r.Y + 18, AppealScreen.PlaceName(round.FinalStandings[who]), winner ? Gold : Frame, 38);
            PixelArt(PixelArtGenerator.IconOf(a.Pokemon), new Vector2(r.X + 190, r.Y + r.Height / 2f), 1);
            UiFonts.DrawCentered(a.Pokemon.DisplayName, r.X + 260, r.Y + 62, 38, Ink, UiWeight.Black);
            UiFonts.DrawCentered(a.Trainer, r.X + 260, r.Y + 112, 26, Muted, UiWeight.ExtraBold);
            UiFonts.DrawCentered(a.Round1Points.ToString(), col[0], r.Y + 88, 52, Ink, UiWeight.Black);
            if (screen.ResultStage >= 1)
                UiFonts.DrawCentered(round.Round2Points[who].ToString(), col[1], r.Y + 88, 52, Ink, UiWeight.Black);
            if (screen.ResultStage >= 2)
                UiFonts.DrawCentered(round.TotalPoints[who].ToString(), col[2], r.Y + 88, 56, winner ? Selection : Ink, UiWeight.Black);
        }
    }
}
