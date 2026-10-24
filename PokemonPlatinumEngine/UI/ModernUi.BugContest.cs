using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Bug-Catching Contest (plan 15 · E5; style guide, "Field menus and notices", "The Bug-Catching Contest's
/// corner", and "Menu screens", "The Bug-Catching Contest's results"): the corner over the field with the Sport Balls
/// and the minutes left, in the Great Marsh's panel's place and shape, and the board of the results.
/// </summary>
internal static partial class ModernUi
{
    /// <summary>The leaf green of the contest's board: its title band and the winner's disc.</summary>
    public static readonly Color ContestLeaf = new(92, 168, 84, 255);

    /// <summary>
    /// The corner over the field while a contest is under way: the Sport Balls on the left and the time left as
    /// minutes and seconds on the right, each red once a fifth or less is left. The Great Marsh's panel, at its place.
    /// </summary>
    public static void BugContestCount(int sw, int balls, float secondsLeft, int startBalls, float length)
    {
        var r = new Rectangle(sw - 32 - 300, 32, 300, 96);
        Panel(r, 24);
        Label(Text.Of("bugContest.sportBalls"), r.X + 28, r.Y + 18);
        UiFonts.Draw(balls.ToString(), r.X + 28, r.Y + 44, 36, balls <= startBalls / 5 ? Red : Ink, UiWeight.Black);
        Label(Text.Of("bugContest.timeLeft"), r.X + 184, r.Y + 18);
        int seconds = (int)MathF.Ceiling(secondsLeft);
        UiFonts.Draw($"{seconds / 60}:{seconds % 60:D2}", r.X + 184, r.Y + 44, 36, secondsLeft <= length / 5 ? Red : Ink, UiWeight.Black);
    }

    /// <summary>
    /// The results' board: a row for each of the six, best first, with the place, who (their class and name, or the
    /// player), the Pokémon caught and the score; a place not told yet is a blank row; the player's own row in the
    /// selection's colour; the winner's catch on a leaf-green disc at the right once first place is told.
    /// </summary>
    public static void DrawBugContestResults(int sw, int sh, BugContestResultsScreen screen, float appear = 1f)
    {
        if (screen.Contest is not { } contest) return;
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("bugContest.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.z", screen.Revealed < BugContestResultsScreen.Told ? "shared.hints.next" : "shared.hints.close"));
        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;

        var board = new Rectangle(Margin, ContentTop + rise, 1180, 6 * RowPitch + 48);
        Panel(board, 30);
        for (int place = 0; place < contest.Ranking.Count; place++)
        {
            var row = RowRect(board, place);
            var entry = contest.Ranking[place];
            bool mine = entry.Id == BugContest.Player;
            if (!screen.Shows(place))
            {
                UiShapes.Fill(new Rectangle(row.X + 88, row.Y + row.Height / 2f - 4, row.Width - 120, 8), 4, Rule);
                UiFonts.DrawCentered(PlaceWord(place), row.X + 24, row.Y + row.Height / 2f, 30, Muted, UiWeight.Black);
                continue;
            }
            string who = mine ? PlayerIdentity.Name : Text.Of("battle.fullTitle", ("class", Trainer.ClassName(contest.ClassOf(entry.Id))), ("name", contest.NameOf(entry.Id)));
            var ink = ListRow(row, mine, who, nameX: 104, size: 32);
            UiFonts.DrawCentered(PlaceWord(place), row.X + 24, row.Y + row.Height / 2f, 30, mine ? Color.White : place < 3 ? Gold : Muted, UiWeight.Black);
            string caught = entry.Species.Length > 0 ? LocalNames.Species(entry.Species) : Text.Of("bugContest.nothing");
            UiFonts.DrawCentered(caught, row.X + 620, row.Y + row.Height / 2f, 30, ink, UiWeight.Bold);
            RowValue(row, entry.Score.ToString(), ink, 32);
        }

        // The winner's catch, once first place is told
        if (screen.Shows(0) && contest.Ranking.Count > 0 && contest.Ranking[0].Species.Length > 0)
        {
            var c = new Vector2(Margin + 1180 + 300, ContentTop + 260 + rise);
            float pop = UiMotion.EaseOut(Math.Clamp(screen.RevealAge / 0.35f, 0f, 1f));
            UiShapes.Circle(c, 190 * pop, ContestLeaf);
            UiShapes.Circle(c, 170 * pop, Lighter(ContestLeaf, 0.55f));
            var sprite = PixelArtGenerator.GetPokemonSprite(contest.Ranking[0].Species, false);
            float scale = 2f, w = sprite.Width * scale, h = sprite.Height * scale;
            if (pop >= 1f)
                Raylib.DrawTexturePro(sprite, new Rectangle(0, 0, sprite.Width, sprite.Height),
                    new Rectangle(MathF.Round(c.X - w / 2f), MathF.Round(c.Y - h / 2f), w, h), Vector2.Zero, 0f, Color.White);
            UiFonts.DrawCentered(Text.Of("bugContest.winner"), c.X - UiFonts.Measure(Text.Of("bugContest.winner"), 34, UiWeight.Black) / 2f,
                c.Y + 230, 34, Color.White, UiWeight.Black);
        }

        // Along the foot: the player's own score
        var foot = new Rectangle(Margin, 940 + rise, sw - Margin * 2, 92);
        Panel(foot, 30);
        string line = screen.Revealed < BugContestResultsScreen.Told ? Text.Of("bugContest.judging")
            : Text.Of("bugContest.yourScore", ("score", contest.Score));
        UiFonts.DrawCentered(line, foot.X + 44, foot.Y + foot.Height / 2f, 36, Ink, UiWeight.ExtraBold);
    }

    private static string PlaceWord(int place) => Text.Of("bugContest.place", ("n", place + 1));
}
