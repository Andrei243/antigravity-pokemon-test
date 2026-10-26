using System;
using System.Numerics;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI.Kit;
using Raylib_cs;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The Battle Palace's closed commands and the Battle Arena's judge's card (plan 14 · B9; style guide, "Battle panels",
/// "The Palace's closed menu" and "The judge's card").
/// </summary>
internal static partial class ModernUi
{
    /// <summary>The Palace's closed commands: a plate at the top right, its four commands greyed under a lock bar.</summary>
    public static readonly Rectangle PalacePlate = new(1340, 52, 524, 184);

    /// <summary>The judge's card, over the middle of the stage.</summary>
    public static readonly Rectangle JudgeRect = new(540, 150, 840, 470);

    /// <summary>
    /// The Battle Palace's closed menu: while its Pokémon choose for themselves the four commands stand greyed on a plate
    /// of their own, with the line that says why across its head. Nothing on it can be chosen.
    /// </summary>
    public static void PalaceClosed()
    {
        var r = PalacePlate;
        Panel(r, 26);
        UiFonts.Draw(Text.Of("venueScreen.closed"), r.X + 28, r.Y + 20, 28, Ink, UiWeight.Black);
        UiFonts.Draw(Text.Of("venueScreen.closedNote"), r.X + 28, r.Y + 58, 22, Muted, UiWeight.Bold);
        string[] labels = { Text.Of("battleMenu.fight"), Text.Of("battleMenu.bag"), Text.Of("battleMenu.pokemon"), Text.Of("battleMenu.run") };
        for (int i = 0; i < 4; i++)
        {
            var pill = new Rectangle(r.X + 28 + (i % 2) * 238, r.Y + 98 + (i / 2) * 42, 226, 34);
            UiShapes.Fill(pill, 17, Closed);
            UiFonts.DrawCentered(labels[i], pill.X + (pill.Width - UiFonts.Measure(labels[i], 20, UiWeight.Black)) / 2f, pill.Y + pill.Height / 2f, 20, Color.White, UiWeight.Black);
        }
    }

    /// <summary>
    /// The judge's card (Emerald's judgment window, our own face): the two names, and for Mind, Skill and Body a mark under
    /// each (a ring ahead, a triangle level, a cross behind), then the two scores, the winner's in gold.
    /// </summary>
    public static void JudgeCard(ArenaVerdict verdict, string mine, string theirs)
    {
        var r = JudgeRect;
        Panel(r, 30);
        string title = Text.Of("venueScreen.judgment");
        UiFonts.Draw(title, r.X + (r.Width - UiFonts.Measure(title, 40, UiWeight.Black)) / 2f, r.Y + 26, 40, Red, UiWeight.Black);
        float left = r.X + 330, right = r.X + 650;
        UiFonts.Draw(mine, left - UiFonts.Measure(mine, 28, UiWeight.Black) / 2f, r.Y + 92, 28, Blue, UiWeight.Black);
        UiFonts.Draw(theirs, right - UiFonts.Measure(theirs, 28, UiWeight.Black) / 2f, r.Y + 92, 28, Red, UiWeight.Black);
        (string Key, int Own, int Other)[] rows =
        {
            ("venueScreen.mind", verdict.MindMine, verdict.MindTheirs),
            ("venueScreen.skill", verdict.SkillMine, verdict.SkillTheirs),
            ("venueScreen.body", verdict.BodyMine, verdict.BodyTheirs)
        };
        for (int i = 0; i < rows.Length; i++)
        {
            float y = r.Y + 172 + i * 76;
            UiFonts.Draw(Text.Of(rows[i].Key), r.X + 48, y - 18, 30, Ink, UiWeight.Black);
            Mark(new Vector2(left, y), ArenaVerdict.Points(rows[i].Own, rows[i].Other));
            Mark(new Vector2(right, y), ArenaVerdict.Points(rows[i].Other, rows[i].Own));
        }
        UiShapes.Line(new Vector2(r.X + 40, r.Y + 384), new Vector2(r.X + r.Width - 40, r.Y + 384), 3, Rule);
        UiFonts.Draw(Text.Of("venueScreen.total"), r.X + 48, r.Y + 400, 30, Ink, UiWeight.Black);
        Score(left, r.Y + 398, verdict.ScoreMine, verdict.Winner == BattleSide.Player);
        Score(right, r.Y + 398, verdict.ScoreTheirs, verdict.Winner == BattleSide.Enemy);
    }

    // A count's mark: a ring for two points, a triangle for one, a cross for none
    private static void Mark(Vector2 at, int points)
    {
        switch (points)
        {
            case 2:
                UiShapes.Ring(at, 24, 7, Green);
                break;
            case 1:
                UiShapes.Triangle(new Vector2(at.X, at.Y - 24), new Vector2(at.X - 26, at.Y + 20), new Vector2(at.X + 26, at.Y + 20), Gold, 4);
                break;
            default:
                UiShapes.Line(new Vector2(at.X - 20, at.Y - 20), new Vector2(at.X + 20, at.Y + 20), 8, Red);
                UiShapes.Line(new Vector2(at.X - 20, at.Y + 20), new Vector2(at.X + 20, at.Y - 20), 8, Red);
                break;
        }
    }

    private static void Score(float x, float y, int score, bool won)
    {
        string text = score.ToString();
        UiFonts.Draw(text, x - UiFonts.Measure(text, 40, UiWeight.Black) / 2f, y, 40, won ? Gold : Ink, UiWeight.Black);
    }
}
