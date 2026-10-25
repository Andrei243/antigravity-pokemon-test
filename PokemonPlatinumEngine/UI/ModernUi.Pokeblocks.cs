using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// Pokéblocks on screen (plan 21 · H11; style guide, "Pokéblocks and the Berry Blender"): the Pokéblock Case, a
/// Pokéblock given to a Pokémon, and the Berry Blender's drum. Our own pictures: a Pokéblock is a rounded square in
/// its colour with a lighter top, the drum a bowl seen from above with a pocket for each seat and an arm turning.
/// </summary>
internal static partial class ModernUi
{
    /// <summary>A Pokéblock's colour.</summary>
    public static Color PokeblockTint(PokeblockColor color) => color switch
    {
        PokeblockColor.Red => new Color(230, 86, 74, 255),
        PokeblockColor.Blue => new Color(74, 132, 228, 255),
        PokeblockColor.Pink => new Color(238, 118, 172, 255),
        PokeblockColor.Green => new Color(76, 174, 96, 255),
        PokeblockColor.Yellow => new Color(232, 190, 50, 255),
        PokeblockColor.Purple => new Color(160, 100, 210, 255),
        PokeblockColor.Indigo => new Color(88, 84, 190, 255),
        PokeblockColor.Brown => new Color(150, 104, 72, 255),
        PokeblockColor.LiteBlue => new Color(130, 200, 240, 255),
        PokeblockColor.Olive => new Color(140, 150, 60, 255),
        PokeblockColor.Gray => new Color(150, 152, 164, 255),
        PokeblockColor.Black => new Color(54, 50, 66, 255),
        PokeblockColor.White => new Color(244, 244, 250, 255),
        PokeblockColor.Gold => new Color(240, 200, 70, 255),
        _ => new Color(190, 190, 200, 255)
    };

    /// <summary>A Pokéblock about 2 × <paramref name="r"/> across: a rounded square with a lighter top and a glint.</summary>
    public static void PokeblockIcon(Vector2 c, float r, PokeblockColor color)
    {
        var body = PokeblockTint(color);
        var outer = new Rectangle(c.X - r, c.Y - r * 0.9f, r * 2, r * 1.8f);
        UiShapes.Fill(outer, r * 0.45f, Darker(body, 0.3f));
        var inner = new Rectangle(c.X - r * 0.88f, c.Y - r * 0.78f, r * 1.76f, r * 1.5f);
        UiShapes.Fill(inner, r * 0.4f, body);
        UiShapes.Fill(new Rectangle(c.X - r * 0.7f, c.Y - r * 0.68f, r * 1.4f, r * 0.5f), r * 0.25f, Lighter(body, 0.4f));
        UiShapes.Circle(c + new Vector2(-r * 0.38f, -r * 0.36f), r * 0.1f, Color.White);
    }

    /// <summary>How many places of the case the list shows under its prompt.</summary>
    private const float PokeblockPrompt = 80;

    public static void DrawPokeblockCase(int sw, int sh, PokeblockCaseScreen screen, float appear)
    {
        switch (screen.Step)
        {
            case CaseStep.Choose:
                DrawPartyChoice(sw, sh, screen.Team, screen.Target, screen.Prompt, _ => null, Math.Clamp(screen.StepAge / 0.35f, 0f, 1f), Text.Of("shared.hints.give"));
                return;
            case CaseStep.Condition or CaseStep.Eating or CaseStep.After:
                PokeblockFeeding(sw, sh, screen, screen.Team.Members[screen.Target]);
                return;
        }

        Backdrop(sw, sh);
        ScreenTitle(Text.Of("pokeblockCase.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.upDown", "shared.hints.move"), Hint("shared.keys.z", "shared.hints.choose"), Hint("shared.keys.esc", "shared.hints.back"));

        float slide = (1f - UiMotion.EaseOut(appear)) * 60f;
        const float top = ContentTop;
        var listed = screen.Listed;

        // ---- The list
        var list = new Rectangle(Margin - slide, top, 920, ContentBottom - top - PokeblockPrompt - Gutter);
        Panel(list, 34);
        bool scrolls = listed.Count > PokeblockCaseScreen.VisibleRows;
        if (listed.Count == 0) EmptyNote(list, Text.Of("pokeblockCase.empty"));
        for (int row = 0; row < PokeblockCaseScreen.VisibleRows && screen.First + row < listed.Count; row++)
        {
            int index = screen.First + row;
            var block = listed[index].Block;
            var r = RowRect(list, row, 26, scrolls);
            bool selected = index == screen.Cursor;
            var ink = ListRow(r, selected, block.Name);
            var disc = new Vector2(r.X + 52, r.Y + r.Height / 2f);
            UiShapes.Circle(disc, 34, selected ? Color.White : Disc);
            PokeblockIcon(disc, 22, block.Color);
            RowValue(r, Text.Of("pokeblockCase.level", ("level", block.Level), ("feel", block.ShownFeel)), ink);
        }
        ScrollBar(list, screen.First, PokeblockCaseScreen.VisibleRows, listed.Count, 26);

        // ---- The prompt
        var box = new Rectangle(Margin - slide, ContentBottom - PokeblockPrompt, 920, PokeblockPrompt);
        Panel(box, 30);
        UiFonts.DrawCentered(screen.Prompt, box.X + 52, box.Y + box.Height / 2f, 36, Ink, UiWeight.ExtraBold);

        // ---- The chosen Pokéblock
        var detail = new Rectangle(Margin + 920 + Gutter + slide, top, sw - Margin * 2 - 920 - Gutter, ContentBottom - top);
        Panel(detail, 34);
        string count = $"{screen.Case.Count} / {PokeblockCase.Capacity}";
        float cw = UiFonts.Measure(count, 24, UiWeight.Black) + 34;
        Tag(detail.X + detail.Width - 44 - cw, detail.Y + 30, count, screen.Case.IsFull ? Red : Frame, 38);
        if (screen.Current is { } chosen) PokeblockDetail(detail, chosen);

        if (screen.Step == CaseStep.Actions)
        {
            var rows = PokeblockCaseScreen.ActionNames;
            float height = MenuPanelHeight(rows.Length);
            MenuPanel(detail.X + detail.Width - 360 - 28, detail.Y + detail.Height - height - 28, rows, screen.ActionIndex, 360);
        }
        if (screen.Step == CaseStep.Trash)
            Prompt(sw, sh, Text.Of("pokeblockCase.trashQuestion"), Text.Of("pokeblockCase.trashNote", ("block", screen.Current?.Name)),
                new[] { (Text.Of("poffinCase.answer.yes"), Red), (Text.Of("poffinCase.answer.no"), Blue) }, screen.TrashChoice);
    }

    // A Pokéblock large: its block on a disc, name and level, its feel, and its five flavours as bars with what each raises
    private static void PokeblockDetail(Rectangle detail, Pokeblock block)
    {
        var disc = new Vector2(detail.X + 44 + 96, detail.Y + 44 + 96 + 40);
        UiShapes.Circle(disc, 96, Disc);
        PokeblockIcon(disc, 62, block.Color);
        float x = detail.X + 44 + 192 + 36;
        float size = 44, room = detail.X + detail.Width - 44 - x;
        float nameW = UiFonts.Measure(block.Name, size, UiWeight.Black);
        if (nameW > room) size = MathF.Floor(size * room / nameW);
        UiFonts.DrawCentered(block.Name, x, detail.Y + 124, size, Ink, UiWeight.Black);
        float tx = x + Tag(x, detail.Y + 166, Text.Of("pokeblockCase.levelTag", ("level", block.Level)), PokeblockTint(block.Color), 38) + 12;
        Tag(tx, detail.Y + 166, Text.Of("pokeblockCase.feelTag", ("feel", block.ShownFeel)), Frame, 38);

        float y = detail.Y + 300;
        UiShapes.Fill(new Rectangle(detail.X + 44, y, detail.Width - 88, 3), 1.5f, Rule);
        y += 34;
        for (int i = 0; i < 5; i++)
        {
            var flavor = (Flavor)i;
            int value = block[flavor];
            float cy = y + i * 74;
            UiFonts.DrawCentered(Text.Of($"poffinCase.flavour.{flavor}"), detail.X + 44, cy, 28, value > 0 ? Ink : Muted, UiWeight.Black);
            UiFonts.DrawCentered(Text.Of($"poffinCase.raises.{flavor}"), detail.X + 44, cy + 28, 20, Muted, UiWeight.ExtraBold);
            string n = value.ToString();
            UiFonts.DrawCentered(n, detail.X + 330 - UiFonts.Measure(n, 30, UiWeight.Black), cy + 8, 30, Ink, UiWeight.Black);
            Bar(new Rectangle(detail.X + 350, cy - 4, detail.Width - 350 - 44, 24), value / 60f, FlavorColor(flavor));
        }
    }

    // The team member being fed: its card on the left, its condition (and the change) on the right
    private static void PokeblockFeeding(int sw, int sh, PokeblockCaseScreen screen, Pokemon p)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("pokeblockCase.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.z", screen.Step == CaseStep.Condition ? "shared.hints.give" : "shared.hints.next"), Hint("shared.keys.esc", "shared.hints.back"));
        const float top = ContentTop;

        var who = new Rectangle(Margin, top, 600, 640);
        Panel(who, 34);
        var c = new Vector2(who.X + who.Width / 2f, who.Y + 230);
        BallDisc(c, 180);
        float hop = screen.Step == CaseStep.Eating ? MathF.Abs(MathF.Sin(screen.StepAge * 9f)) * 14f : 0f;
        PixelArt(PixelArtGenerator.SpriteOf(p), c, 3, hop);
        NameWithGender(p, who.X + 44, who.Y + 470, 48);
        Level(who.X + who.Width - 44, who.Y + 470, p.Level, 40);
        Label(Text.Of("summary.nature"), who.X + 44, who.Y + 528);
        UiFonts.Draw(Words.Nature(p.Nature), who.X + 44, who.Y + 552, 34, Ink, UiWeight.Black);

        if (screen.Chosen is { } block)
        {
            var card = new Rectangle(Margin, top + 640 + Gutter, 600, ContentBottom - PokeblockPrompt - Gutter - (top + 640 + Gutter));
            Panel(card, 34);
            var disc = new Vector2(card.X + 44 + 50, card.Y + card.Height / 2f);
            UiShapes.Circle(disc, 50, Disc);
            PokeblockIcon(disc, 32, block.Color);
            UiFonts.DrawCentered(block.Name, card.X + 170, card.Y + card.Height / 2f - 16, 32, Ink, UiWeight.Black);
            UiFonts.DrawCentered(Text.Of("pokeblockCase.levelAndFeel", ("level", block.Level), ("feel", block.ShownFeel)), card.X + 170, card.Y + card.Height / 2f + 22, 24, Muted, UiWeight.ExtraBold);
        }

        var chart = new Rectangle(Margin + 600 + Gutter, top, sw - Margin * 2 - 600 - Gutter, ContentBottom - PokeblockPrompt - Gutter - top);
        float grow = screen.Step == CaseStep.Condition ? 1f : Math.Clamp(screen.StepAge / 0.8f, 0f, 1f);
        var before = screen.Step == CaseStep.Condition ? null : screen.Before;
        ConditionPanel(chart, ConditionOf(p), before, grow);

        var box = new Rectangle(Margin, ContentBottom - PokeblockPrompt, sw - Margin * 2, PokeblockPrompt);
        Panel(box, 30);
        UiFonts.DrawCentered(screen.Prompt, box.X + 52, box.Y + box.Height / 2f, 36, Ink, UiWeight.ExtraBold);
        if (screen.Step != CaseStep.Condition || screen.Message != null) AdvanceArrow(box.X + box.Width - 80, box.Y + box.Height / 2f - 10);
    }

    // ------------------------------------------------------------------ the Berry Blender

    private static readonly Vector2 DrumMiddle = new(960, 560);
    private const float DrumRadius = 330;

    private static Vector2 OnDrum(float degrees, float radius)
    {
        float a = (degrees - 90f) * MathF.PI / 180f;
        return DrumMiddle + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius;
    }

    public static void DrawBlender(int sw, int sh, BlenderScreen screen)
    {
        var game = screen.Game!;
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("blender.title"));
        if (screen.Phase == BlenderPhase.Play) Hints(sw - Margin, 44, Hint("shared.keys.z", "blender.hint.press"));
        else if (screen.Phase is BlenderPhase.Ready or BlenderPhase.Result) Hints(sw - Margin, 44, Hint("shared.keys.z", "shared.hints.next"));

        // The drum: a bowl seen from above, the pockets round it
        UiShapes.Shadow(new Rectangle(DrumMiddle.X - DrumRadius - 40, DrumMiddle.Y - DrumRadius - 40, (DrumRadius + 40) * 2, (DrumRadius + 40) * 2), DrumRadius + 40, 36, new Vector2(0, 14), ShadowColor);
        UiShapes.Circle(DrumMiddle, DrumRadius + 40, new Color(170, 118, 84, 255));
        UiShapes.Circle(DrumMiddle, DrumRadius + 20, new Color(204, 150, 106, 255));
        UiShapes.Circle(DrumMiddle, DrumRadius - 20, new Color(120, 82, 60, 255));
        for (int seat = 0; seat < game.Seats; seat++)
        {
            float angle = game.PocketAngle(seat);
            bool mine = seat == 0;
            var at = OnDrum(angle, DrumRadius - 80);
            var berry = screen.Berries[seat];
            UiShapes.Circle(at, 62, mine ? new Color(250, 226, 150, 255) : new Color(230, 210, 186, 255));
            UiShapes.Ring(at, 62, 5, mine ? Selection : Frame);
            // The berry's colour from its strongest flavour
            var flavor = (Flavor)Enumerable.Range(0, 5).OrderByDescending(i => berry.Flavor(i)).First();
            UiShapes.Circle(at, 28, FlavorColor(flavor));
            UiShapes.Circle(at + new Vector2(-8, -9), 7, Color.White);
            string name = seat < screen.Names.Count ? screen.Names[seat] : "";
            UiFonts.DrawCentered(name, at.X - UiFonts.Measure(name, 24, UiWeight.Black) / 2f, at.Y + 92, 24, Color.White, UiWeight.Black);
            string berryName = LocalNames.Item(berry.Name);
            UiFonts.DrawCentered(berryName, at.X - UiFonts.Measure(berryName, 20, UiWeight.ExtraBold) / 2f, at.Y + 118, 20, new Color(250, 238, 214, 255), UiWeight.ExtraBold);
        }
        // The arm
        if (screen.Phase is BlenderPhase.Play or BlenderPhase.Result or BlenderPhase.Count)
        {
            var tip = OnDrum(game.Angle, DrumRadius - 30);
            UiShapes.Line(DrumMiddle, tip, 22, new Color(250, 242, 224, 255));
            UiShapes.Circle(tip, 26, new Color(250, 242, 224, 255));
            UiShapes.Circle(DrumMiddle, 40, new Color(250, 242, 224, 255));
            UiShapes.Circle(DrumMiddle, 18, Frame);
        }

        // The speed, and the time left
        var side = new Rectangle(Margin, ContentTop + 20, 340, 380);
        Panel(side, 30);
        Label(Text.Of("blender.speed"), side.X + 36, side.Y + 26);
        UiFonts.Draw(BlenderGame.RpmText(game.Rpm), side.X + 36, side.Y + 54, 56, Ink, UiWeight.Black);
        Label(Text.Of("blender.fastest"), side.X + 36, side.Y + 140);
        UiFonts.Draw(BlenderGame.RpmText(game.MaxRpm), side.X + 36, side.Y + 168, 44, Selection, UiWeight.Black);
        Label(Text.Of("blender.time"), side.X + 36, side.Y + 252);
        Bar(new Rectangle(side.X + 36, side.Y + 290, side.Width - 72, 26), 1f - game.Frame / (float)BlenderGame.Duration, game.Frame > BlenderGame.Duration * 0.8f ? Red : Green);
        Tag(side.X + 36, side.Y + 328, Text.Of("blender.hits", ("best", game.Bests), ("good", game.Goods), ("miss", game.Misses)), Frame, 34);

        if (game.LastGrade is { } grade && game.LastGradeAge < 20)
        {
            string word = Text.Of($"blender.grade.{grade}");
            var color = grade switch { BlenderGrade.Best => Gold, BlenderGrade.Good => Green, _ => Red };
            UiFonts.DrawCentered(word, DrumMiddle.X - UiFonts.Measure(word, 64, UiWeight.Black) / 2f, DrumMiddle.Y - 20 - game.LastGradeAge * 2f, 64, color, UiWeight.Black);
        }
        if (screen.Phase == BlenderPhase.Count)
        {
            string n = screen.CountNumber.ToString();
            UiFonts.DrawCentered(n, DrumMiddle.X - UiFonts.Measure(n, 160, UiWeight.Black) / 2f, DrumMiddle.Y, 160, Color.White, UiWeight.Black);
        }
        if (screen.Phase == BlenderPhase.Result && screen.Result is { } block)
        {
            Dim(sw, sh, 120);
            var card = new Rectangle(sw / 2f - 380, DrumMiddle.Y - 250, 760, 460);
            Panel(card, 40, 0f, 6f, PokeblockTint(block.Color));
            var disc = new Vector2(card.X + 150, card.Y + 150);
            UiShapes.Circle(disc, 100, Disc);
            PokeblockIcon(disc, 64, block.Color);
            UiFonts.DrawCentered(block.Name, card.X + 280, card.Y + 90, 50, Ink, UiWeight.Black);
            Tag(card.X + 280, card.Y + 132, Text.Of("pokeblockCase.levelTag", ("level", block.Level)), PokeblockTint(block.Color), 38);
            for (int i = 0; i < 5; i++)
            {
                var flavor = (Flavor)i;
                float cy = card.Y + 230 + i * 44;
                UiFonts.DrawCentered(Text.Of($"poffinCase.flavour.{flavor}"), card.X + 50, cy, 24, Ink, UiWeight.Black);
                Bar(new Rectangle(card.X + 250, cy - 10, card.Width - 330, 20), block[flavor] / 60f, FlavorColor(flavor));
            }
        }
        ContestLine(sw, screen.Line, screen.Phase is BlenderPhase.Ready or BlenderPhase.Result);
    }
}
