using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Battle Hall on screen (plan 06 · R18c; style guide, "The Battle Hall and the Battle Castle"): the board of
/// types with the rank each stands at, the entered Pokémon's place and the Hall Matron's.
/// </summary>
internal static partial class ModernUi
{
    /// <summary>
    /// The board as it stands: seventeen cards in the types' own colours, four to a row, each with its rank and a
    /// pip for every win over it; the entered Pokémon across two places of the last row and the Hall Matron's
    /// beside it; along the foot what is asked, the battle of the round and the streak; and a prompt over it all
    /// for a yes or a no.
    /// </summary>
    public static void DrawHallBoard(int sw, int sh, HallBoardScreen screen, float appear = 1f)
    {
        if (screen.Challenge is not { } challenge) return;
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("hallBoard.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.z", "shared.hints.choose"), Hint("shared.keys.arrows", "shared.hints.move"));
        float rise = (1f - UiMotion.EaseOut(appear)) * 60f;

        const float gap = 20, top = ContentTop, rowH = 136;
        float width = (sw - Margin * 2 - gap * (HallBoardScreen.Columns - 1)) / HallBoardScreen.Columns;
        bool live = screen.Step == HallBoardStep.Pick;
        for (int cell = 0; cell < HallBoardScreen.Cells; cell++)
        {
            if (cell == HallBoardScreen.SummaryCell + 1) continue;
            int column = cell % HallBoardScreen.Columns, row = cell / HallBoardScreen.Columns;
            bool wide = cell == HallBoardScreen.SummaryCell;
            var r = new Rectangle(Margin + column * (width + gap), top + rise + row * (rowH + gap), wide ? width * 2 + gap : width, rowH);
            bool under = screen.Cursor == cell || wide && HallBoardScreen.IsSummary(screen.Cursor);
            if (wide) EntrantCard(r, screen, under && live);
            else if (cell == HallBoardScreen.MatronCell) MatronCard(r, screen, under && live);
            else TypeCard(r, challenge, cell, under, live, screen.CanChoose(cell), screen.Saved == cell);
        }

        // The foot: what is asked, the battle of the round and the streak
        var foot = new Rectangle(Margin, 916 + rise, sw - Margin * 2, 116);
        Panel(foot, 34);
        UiFonts.DrawCentered(screen.Step == HallBoardStep.Pick ? screen.Prompt : Text.Of("hallBoard.prompt.choose"), foot.X + 48, foot.Y + foot.Height / 2f, 38, Ink, UiWeight.ExtraBold);
        string streak = $"{challenge.Streak:N0}", battle = $"{challenge.Battle + 1} / {BattleHallChallenge.BattlesPerRound}";
        float sx = foot.X + foot.Width - 48 - Math.Max(UiFonts.Measure(streak, 44, UiWeight.Black), UiFonts.Measure(Text.Of("records.streak"), 20, UiWeight.Black));
        Label(Text.Of("records.streak"), sx, foot.Y + 20);
        UiFonts.Draw(streak, sx, foot.Y + 46, 44, Ink, UiWeight.Black);
        float bx = sx - 56 - Math.Max(UiFonts.Measure(battle, 44, UiWeight.Black), UiFonts.Measure(Text.Of("records.battle"), 20, UiWeight.Black));
        Label(Text.Of("records.battle"), bx, foot.Y + 20);
        UiFonts.Draw(battle, bx, foot.Y + 46, 44, Ink, UiWeight.Black);

        // A yes or a no, or the Hall Matron's coming, over everything
        float asked = Math.Clamp(screen.StepAge / 0.2f, 0f, 1f);
        (string, Color)[] yesNo = { (Text.Of("records.answer.yes"), Green), (Text.Of("records.answer.no"), Red) };
        if (screen.Step == HallBoardStep.Confirm && screen.MatronOpen)
            Prompt(sw, sh, Text.Of("hallBoard.matron"), Text.Of("hallBoard.matronChooses", ("prompt", screen.Prompt)), yesNo, screen.MenuCursor, asked);
        else if (screen.Step == HallBoardStep.Confirm)
        {
            var type = BattleHallChallenge.Board[screen.Cursor];
            Prompt(sw, sh, Text.Of("hallBoard.typeRank", ("type", Words.Type(type)), ("rank", challenge.ShownRank(screen.Cursor))), screen.Prompt, yesNo, screen.MenuCursor, asked);
        }
        else if (screen.Step == HallBoardStep.MatronNotice)
            Prompt(sw, sh, Text.Of("hallBoard.matron"), Text.Of("hallBoard.matronNotice", ("prompt", screen.Prompt)), new[] { (Text.Of("shared.ok"), Blue) }, 0, asked);
    }

    // A type's card: its colour down the left edge, its name, the rank it stands at, and ten pips of which the wins over it are filled
    private static void TypeCard(Rectangle r, BattleHallChallenge challenge, int cell, bool under, bool live, bool open, bool saved)
    {
        var type = BattleHallChallenge.Board[cell];
        var colour = Palette.GetTypeColor(type.ToString());
        Card(r, 28, under && live || saved, saved ? Green : under && !open ? Muted : null);
        var strip = new Rectangle(r.X + 14, r.Y + 14, 22, r.Height - 28);
        UiShapes.Shape(strip, 11, Lighter(colour, 0.12f), Darker(colour, 0.1f));
        UiFonts.DrawCentered(Words.Type(type).ToUpperInvariant(), r.X + 54, r.Y + 44, 36, open ? Ink : Muted, UiWeight.Black);

        int wins = Math.Min(challenge.Ranks[cell], BattleHallChallenge.MaxRank);
        string rank = challenge.ShownRank(cell).ToString();
        float rw = UiFonts.Measure(rank, 54, UiWeight.Black);
        UiFonts.DrawCentered(rank, r.X + r.Width - 28 - rw, r.Y + 48, 54, open ? Ink : Muted, UiWeight.Black);
        UiFonts.DrawCentered(Text.Of("hallBoard.rank"), r.X + r.Width - 36 - rw - UiFonts.Measure(Text.Of("hallBoard.rank"), 20, UiWeight.Black), r.Y + 56, 20, Muted, UiWeight.Black);

        // Ten pips along the foot of the card
        const float pip = 16, between = 8;
        float px = r.X + 54, py = r.Y + r.Height - 34;
        for (int n = 0; n < BattleHallChallenge.MaxRank; n++)
        {
            var c = new Vector2(px + n * (pip + between) + pip / 2f, py + pip / 2f);
            if (n < wins) UiShapes.Circle(c, pip / 2f, open ? colour : Lighter(colour, 0.45f));
            else UiShapes.Ring(c, pip / 2f, 3, Rule);
        }
        // (clear of the tenth pip, which ends 286 units in)
        if (challenge.Beaten(cell)) Tag(r.X + r.Width - 124, r.Y + r.Height - 46, Text.Of("hallBoard.cleared"), Muted, 30);
        if (!open) UiShapes.Fill(r, 28, new Color(226, 232, 244, 120));
    }

    // The entered Pokémon across two places: its picture (both, in a Double challenge), its name and level, and what choosing it shows
    private static void EntrantCard(Rectangle r, HallBoardScreen screen, bool under)
    {
        Card(r, 28, under, Blue);
        if (screen.Entered.Count == 0) return;
        float x = r.X + 24;
        foreach (var p in screen.Entered)
        {
            var c = new Vector2(x + 52, r.Y + r.Height / 2f);
            UiShapes.Circle(c, 52, new Color(226, 234, 246, 255));
            var sprite = PixelArtGenerator.SpriteOf(p);
            float scale = Math.Min(1f, 104f / Math.Max(sprite.Width, sprite.Height));
            Raylib.DrawTexturePro(sprite, new Rectangle(0, 0, sprite.Width, sprite.Height),
                new Rectangle(MathF.Round(c.X - sprite.Width * scale / 2f), MathF.Round(c.Y - sprite.Height * scale / 2f), MathF.Round(sprite.Width * scale), MathF.Round(sprite.Height * scale)),
                Vector2.Zero, 0f, Color.White);
            x += 120;
        }
        var first = screen.Entered[0];
        NameWithGender(first, x + 8, r.Y + 46, 40);
        string level = screen.Entered.Count > 1 ? Text.Of("hallBoard.levels", ("first", first.Level), ("second", screen.Entered[1].Level)) : Text.Of("hallBoard.level", ("level", first.Level));
        UiFonts.DrawCentered(level, x + 8, r.Y + 92, 26, Muted, UiWeight.Black);
        TypePills(x + 8 + UiFonts.Measure(level, 26, UiWeight.Black) + 20, r.Y + 76, first, 32);
        Tag(r.X + r.Width - 190, r.Y + r.Height / 2f - 19, Text.Of("hallBoard.summary"), Blue, 38);
    }

    // The Hall Matron's place: dark until she is announced
    private static void MatronCard(Rectangle r, HallBoardScreen screen, bool under)
    {
        bool open = screen.MatronOpen;
        Card(r, 28, under, open ? Gold : Muted);
        UiFonts.DrawCentered("? ? ?", r.X + 40, r.Y + 50, 48, open ? Ink : Muted, UiWeight.Black);
        UiFonts.DrawCentered(open ? Text.Of("hallBoard.hallMatron") : Text.Of("hallBoard.nobodyYet"), r.X + 40, r.Y + 100, 24, open ? Gold : Muted, UiWeight.Black);
        if (!open) UiShapes.Fill(r, 28, new Color(226, 232, 244, 120));
    }
}
