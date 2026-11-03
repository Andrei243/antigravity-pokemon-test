using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;
using EncounterMark = PokemonPlatinumEngine.Models.Mark;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// The marks of an encounter on screen (plan 10 · F9; style guide, "Summary", "Marks and titles"): our own badge, a
/// rosette in the colour of the mark's kind, never the games' pictures; the summary's row with the title, and the small
/// badge on a party card.
/// </summary>
internal static partial class ModernUi
{
    /// <summary>The colour of a mark's kind: rare gold, a personality violet, a time of day blue, a weather teal, the rod sea green, an alpha red.</summary>
    public static Color MarkColour(EncounterMark mark) => mark switch
    {
        EncounterMark.Rare => new Color(232, 176, 40, 255),
        >= EncounterMark.Rowdy and <= EncounterMark.Slump => new Color(150, 96, 206, 255),
        >= EncounterMark.Lunchtime and <= EncounterMark.Dawn => new Color(70, 120, 222, 255),
        >= EncounterMark.Cloudy and <= EncounterMark.Misty => new Color(28, 156, 164, 255),
        EncounterMark.Fishing => new Color(40, 140, 110, 255),
        _ => new Color(214, 64, 64, 255)
    };

    /// <summary>
    /// A mark's badge about 2 × <paramref name="r"/> across: a disc in the kind's colour with a darker rim and a white
    /// ring inside it, a star in the middle for the rare mark and a dot for every other, over two short tails.
    /// </summary>
    public static void MarkBadge(Vector2 c, float r, EncounterMark mark)
    {
        var body = MarkColour(mark);
        var deep = Darker(body, 0.3f);
        UiShapes.Triangle(c + new Vector2(-0.62f, 0.2f) * r, c + new Vector2(-0.08f, 0.4f) * r, c + new Vector2(-0.52f, 1.12f) * r, deep);
        UiShapes.Triangle(c + new Vector2(0.62f, 0.2f) * r, c + new Vector2(0.52f, 1.12f) * r, c + new Vector2(0.08f, 0.4f) * r, deep);
        UiShapes.Circle(c, r * 0.86f, deep);
        UiShapes.Circle(c, r * 0.74f, body);
        UiShapes.Ring(c, r * 0.5f, r * 0.08f, Color.White);
        if (mark == EncounterMark.Rare) Star(c, r * 0.34f, Color.White);
        else UiShapes.Circle(c, r * 0.16f, Color.White);
    }

    /// <summary>
    /// The summary's row of the marks a Pokémon was met with, under a rule in the lower half of the markings' panel:
    /// MET WITH, each mark as its badge and name (the cursor's place after the six markings is a Selection rim round it,
    /// the title a Gold tag beside it), then TITLE and the line it is sent out under, or that none is chosen.
    /// </summary>
    private static void EncounterMarksRow(Rectangle panel, Pokemon p, IReadOnlyList<EncounterMark> met, int markCursor)
    {
        float x = panel.X + 44, wide = panel.Width - 88;
        UiShapes.Fill(new Rectangle(x, panel.Y + 226, wide, 3), 1.5f, Rule);
        Label(Text.Of("marks.heading"), x, panel.Y + 250);
        float cx = x;
        for (int i = 0; i < met.Count; i++)
        {
            var mark = met[i];
            string name = EncounterMarks.Name(mark);
            bool titled = p.Title == mark.ToString();
            float nameW = UiFonts.Measure(name, 30, UiWeight.Black);
            string tag = Text.Of("marks.title");
            float tagW = titled ? UiFonts.Measure(tag, 20, UiWeight.Black) + 40 : 0;
            float w = 92 + nameW + tagW + 28;
            var cell = new Rectangle(cx, panel.Y + 284, w, 80);
            if (markCursor == MarkCount + i)
                UiShapes.Shape(cell, 24, Lighter(Selection, 0.88f), Lighter(Selection, 0.8f), Selection, 4);
            MarkBadge(new Vector2(cell.X + 46, cell.Y + 36), 30, mark);
            UiFonts.DrawCentered(name, cell.X + 92, cell.Y + 40, 30, Ink, UiWeight.Black);
            if (titled) Tag(cell.X + 92 + nameW + 14, cell.Y + 24, tag, Gold, 32);
            cx += w + 16;
        }
        Label(Text.Of("marks.title"), x, panel.Y + 392);
        string line = p.Title != null ? Text.Of("marks.titleLine", ("name", EncounterMarks.SentOutAs(p, p.DisplayName))) : Text.Of("marks.noTitle");
        UiFonts.Draw(line, x, panel.Y + 418, 28, p.Title != null ? Ink : Muted, UiWeight.ExtraBold);
    }

    /// <summary>The mark a card shows for a Pokémon (plan 10 · F9): its title's if one is chosen, its first otherwise, none for a Pokémon met with none.</summary>
    public static EncounterMark? CardMarkOf(Pokemon p)
    {
        EncounterMark? shown = null;
        foreach (var m in EncounterMarks.Of(p))
        {
            shown ??= m;
            if (p.Title == m.ToString()) return m;
        }
        return shown;
    }

    /// <summary>The small badge of a Pokémon met with a mark, on its party card or beside its sprite in the PC (plan 10 · F9).</summary>
    private static void CardMark(Vector2 c, Pokemon p, float r = 20)
    {
        if (!p.IsEgg && CardMarkOf(p) is { } mark) MarkBadge(c, r, mark);
    }
}
