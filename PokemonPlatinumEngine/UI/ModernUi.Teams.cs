using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>Saved teams and the judge on the PC (plan 12 · Q27; style guide, "PC boxes", "Teams" and "The judge").</summary>
internal static partial class ModernUi
{
    private const float TeamCardHeight = 84, TeamCardPitch = 90, TeamDisc = 30;

    /// <summary>The key hints while the teams' panel, its menu or the judge has the screen; false when none has.</summary>
    private static bool TeamHints(int sw, PCScreen pc)
    {
        if (pc.TeamMenu != null || pc.TeamsOpen)
            Hints(sw - Margin, 44, Hint("shared.keys.upDown", "shared.hints.choose"), Hint("shared.keys.z", "shared.hints.doIt"), Hint("shared.keys.esc", "shared.hints.back"));
        else if (pc.Judging && !pc.Busy && pc.Held == null)
            Hints(sw - Margin, 44, Hint("shared.keys.arrows", "shared.hints.look"), Hint("shared.keys.esc", "shared.hints.back"));
        else return false;
        return true;
    }

    /// <summary>The teams' panel over the box and the detail column: each team kept with its six, then SAVE THIS TEAM; and its menu.</summary>
    private static void DrawTeams(int sw, PCScreen pc, Party party, PcBoxes stored, Rectangle box)
    {
        if (!pc.TeamsOpen) return;
        var panel = new Rectangle(box.X, ContentTop, sw - Margin - box.X, ContentBottom - ContentTop);
        Panel(panel, 34);
        Label(Text.Of("boxTeams.title"), panel.X + 40, panel.Y + 30);
        string count = Text.Of("boxTeams.count", ("count", pc.Teams.Count), ("most", SavedTeams.Most));
        UiFonts.Draw(count, panel.X + panel.Width - 40 - UiFonts.Measure(count, 20, UiWeight.Black), panel.Y + 30, 20, Muted, UiWeight.Black);

        float top = panel.Y + 64;
        Rectangle Row(int i) => new(panel.X + 24, top + i * TeamCardPitch, panel.Width - 48, TeamCardHeight);
        for (int i = 0; i < pc.Teams.Count; i++)
        {
            var team = pc.Teams[i];
            var r = Row(i);
            bool selected = pc.TeamIndex == i;
            Card(r, 26, selected);
            UiFonts.DrawCentered(team.Name, r.X + 36, r.Y + r.Height / 2f, 30, Ink, UiWeight.Black);
            // The six places from the right: a member's icon on its disc, or a Rule ring where it is no longer stored
            const float step = TeamDisc * 2 + 14;
            for (int k = 0; k < Party.MaxSize; k++)
            {
                var c = new Vector2(r.X + r.Width - 36 - TeamDisc - (Party.MaxSize - 1 - k) * step, r.Y + r.Height / 2f);
                if (k >= team.Members.Count) continue;
                var (at, place) = SavedTeams.Find(team.Members[k], party, stored);
                Pokemon? p = at >= 0 ? party.Members[at] : place is { } bp ? stored[bp.Box, bp.Slot] : null;
                if (p == null)
                {
                    UiShapes.Ring(c, TeamDisc - 2, 3, Rule);
                    continue;
                }
                UiShapes.Circle(c, TeamDisc, Disc);
                PixelArt(PixelArtGenerator.IconOf(p), c + new Vector2(0, -2), 1);
            }
        }
        if (pc.Teams.Count < SavedTeams.Most)
        {
            var r = Row(pc.Teams.Count);
            string save = Text.Of("boxTeams.save");
            ListRow(r, pc.OnSaveRow, save, enabled: party.Count > 0, nameX: (r.Width - UiFonts.Measure(save, 30, UiWeight.Black)) / 2f, size: 30);
        }

        if (pc.TeamMenu is { } menu)
        {
            var r = Row(Math.Clamp(pc.TeamIndex, 0, pc.TeamRows - 1));
            var rows = menu.Select(a => Text.Of($"boxTeams.teamMenu.{a}")).ToList();
            MenuPanel(r.X + 420, Math.Clamp(r.Y + r.Height - 8, ContentTop, ContentBottom - MenuPanelHeight(rows.Count)), rows, pc.MenuIndex);
        }
    }

    /// <summary>The judge's colours for each word: the pill's fill and its ink.</summary>
    private static (Color Fill, Color Ink) JudgeColours(IvGrade grade) => grade switch
    {
        IvGrade.Nothing or IvGrade.Meagre or IvGrade.Fair => (Rule, Ink),
        IvGrade.Good => (Blue, Color.White),
        IvGrade.Splendid => (Green, Color.White),
        _ => (Gold, Ink)
    };

    /// <summary>The detail column as the judge's page: the Pokémon as the column shows it, then each stat's word.</summary>
    private static void JudgePage(Rectangle detail, Pokemon shown)
    {
        var disc = new Vector2(detail.X + detail.Width / 2f, detail.Y + 24 + 112);
        BallDisc(disc, 112);
        PixelArt(PixelArtGenerator.SpriteOf(shown), disc, 2);
        const float markStep = 42;
        for (int m = 0; m < MarkCount; m++)
        {
            bool set = (shown.Marks & (1 << m)) != 0;
            MarkShape(new Vector2(detail.X + detail.Width / 2f + (m - 2.5f) * markStep, detail.Y + 268), 24, m, set ? Ink : Rule);
        }
        float x = detail.X + 36, y = detail.Y;
        float wide = detail.Width - 72;
        NameWithGender(shown, x, y + 318, 38);
        UiShapes.Fill(new Rectangle(x, y + 352, wide, 3), 1.5f, Rule);
        Label(Text.Of("boxTeams.judge.title"), x, y + 368);
        if (shown.IsEgg)
        {
            UiFonts.Draw(Text.Of("boxTeams.judge.egg"), x, y + 404, 26, Muted, UiWeight.ExtraBold);
            return;
        }
        for (int i = 0; i < IvJudge.Stats.Count; i++)
        {
            var stat = IvJudge.Stats[i];
            float ry = y + 428 + i * 56;
            UiFonts.DrawCentered(Words.Stat(stat), x, ry, 26, Ink, UiWeight.ExtraBold);
            var grade = IvJudge.Grade(IvJudge.IvOf(shown, stat));
            string word = Text.Of($"boxTeams.judge.grade.{grade}");
            var (fill, ink) = JudgeColours(grade);
            float pw = Math.Max(150, UiFonts.Measure(word, 24, UiWeight.Black) + 36);
            var pill = new Rectangle(x + wide - pw, ry - 20, pw, 40);
            UiShapes.Fill(pill, 20, fill);
            UiFonts.DrawCentered(word, pill.X + (pw - UiFonts.Measure(word, 24, UiWeight.Black)) / 2f, ry, 24, ink, UiWeight.Black);
        }
        string best = IvJudge.Best(shown) is { } top ? Text.Of("boxTeams.judge.best", ("stat", Words.Stat(top))) : Text.Of("boxTeams.judge.alike");
        UiFonts.DrawCentered(best, x, y + 428 + 6 * 56 + 8, 26, Muted, UiWeight.ExtraBold);
    }

    /// <summary>The keyboard for a team's name, with the team's six icons on the left plate in place of a wallpaper.</summary>
    public static void DrawTeamNaming(int sw, int sh, NameEntry entry, Party party)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("boxTeams.naming.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.z", "shared.hints.pick"), Hint("shared.keys.x", "shared.hints.delete"), Hint("shared.keys.enter", "shared.hints.toOk"));

        var who = new Rectangle(Margin, ContentTop, 500, ContentBottom - ContentTop);
        Panel(who, 34);
        var plate = new Rectangle(who.X + 50, who.Y + 50, who.Width - 100, 470);
        UiShapes.Shape(plate, 30, Disc, Disc, Rule, 4);
        for (int i = 0; i < party.Count && i < Party.MaxSize; i++)
        {
            var c = new Vector2(plate.X + plate.Width / 4f + (i % 2) * plate.Width / 2f, plate.Y + 84 + (i / 2) * 150);
            UiShapes.Circle(c, 56, new Color(255, 255, 255, 215));
            PixelArt(PixelArtGenerator.IconOf(party.Members[i]), c + new Vector2(0, -3), 2);
        }
        DrawWrapped(Text.Of("boxTeams.naming.note"), who.X + 50, plate.Y + plate.Height + 36, who.Width - 100, 28, Muted, 40, UiWeight.ExtraBold);

        NameBoard(new Rectangle(who.X + who.Width + Gutter, ContentTop, sw - Margin - (who.X + who.Width + Gutter), ContentBottom - ContentTop), entry);
    }
}
