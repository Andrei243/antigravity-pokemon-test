using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;
using Raylib_cs;

namespace PokemonPlatinumEngine.UI;

// The card the player designs (plan 43 · LF1; style guide, "Trainer Card" and "The card designer"): the designed face
// the card's front and the designer both draw, the signature on the back, and the designer's own screen.
internal static partial class ModernUi
{
    /// <summary>How wide and tall the designed face is on the card (the part between the band and the badges).</summary>
    public const float CardFaceWidth = 1468, CardFaceHeight = 420;

    /// <summary>The card's posed figure's picture, in pixels: what the engine renders it at.</summary>
    public const int CardFigureWidth = 400, CardFigureHeight = 480;

    // ------------------------------------------------------------------ the face

    private static Color Hex(string hex, byte alpha = 255)
    {
        var (r, g, b) = Data.Garment.Rgb(hex);
        return new Color(r, g, b, alpha);
    }

    /// <summary>
    /// A designed card's face in <paramref name="r"/>, at <paramref name="k"/> of its size on the card: the background and
    /// its pattern, the frame, the posed figure on the left, the greeting and the six shown in the middle.
    /// </summary>
    public static void CardFaceArt(Rectangle r, TrainerCardFace face, float k, bool withTitle = false)
    {
        float radius = 24 * k;
        if (face.Backdrop is { } backdrop) BackdropArt(r, backdrop);
        else if (face.Background is { } bg)
        {
            UiShapes.Shape(r, radius, Hex(bg.Top), Hex(bg.Bottom));
            CardPatternArt(r, bg.Pattern, Hex(bg.Ink, 120), k);
        }
        else UiShapes.Shape(r, radius, Disc, Disc);
        CardFrameArt(r, face.Frame, k);

        // The figure on a soft spot of light at the left
        var figureBox = new Rectangle(r.X + 24 * k, r.Y + 10 * k, 360 * k, r.Height - 20 * k);
        UiShapes.Fill(new Rectangle(figureBox.X + 50 * k, figureBox.Y + figureBox.Height - 46 * k, figureBox.Width - 100 * k, 22 * k), 11 * k, new Color(0, 0, 0, 40));
        if (face.Figure is { } figure)
        {
            float scale = Math.Min(figureBox.Width / figure.Width, figureBox.Height / figure.Height);
            float w = figure.Width * scale, h = figure.Height * scale;
            Raylib.DrawTexturePro(figure, new Rectangle(0, 0, figure.Width, -figure.Height),
                new Rectangle(figureBox.X + (figureBox.Width - w) / 2f, figureBox.Y + figureBox.Height - h, w, h), Vector2.Zero, 0f, Color.White);
        }

        // The greeting, in quotes, and the six shown under it
        float x = r.X + 410 * k;
        string greeting = face.Greeting.Length > 0 ? "“" + face.Greeting + "”" : "";
        if (greeting.Length > 0)
        {
            var plate = new Rectangle(x - 12 * k, r.Y + 36 * k, Math.Min(640 * k, UiFonts.Measure(greeting, 44 * k, UiWeight.Black) + 48 * k), 84 * k);
            UiShapes.Fill(plate, 30 * k, new Color(255, 255, 255, 170));
            UiFonts.DrawCentered(greeting, x + 12 * k, plate.Y + plate.Height / 2f, 44 * k, Ink, UiWeight.Black);
        }
        if (withTitle && face.Title.Length > 0)
            UiFonts.Draw(face.Title.ToUpperInvariant(), x, r.Y + 136 * k, 26 * k, Darker(Ink, 0.1f), UiWeight.Black);
        float step = 106 * k;
        for (int i = 0; i < face.Shown.Count && i < Party.MaxSize; i++)
        {
            var c = new Vector2(x + 44 * k + i * step, r.Y + r.Height - 96 * k);
            UiShapes.Circle(c, 48 * k, new Color(255, 255, 255, 190));
            UiShapes.Ring(c, 48 * k, 3 * k, new Color(0, 0, 0, 36));
            var icon = PixelArtGenerator.IconOf(face.Shown[i]);
            float w = icon.Width * 2 * k, h = icon.Height * 2 * k;
            Raylib.DrawTexturePro(icon, new Rectangle(0, 0, icon.Width, icon.Height), new Rectangle(MathF.Round(c.X - w / 2f), MathF.Round(c.Y - h / 2f), w, h), Vector2.Zero, 0f, Color.White);
        }
    }

    /// <summary>A background's pattern, in its ink, kept inside the face (nothing is cut: every mark is placed within it).</summary>
    private static void CardPatternArt(Rectangle r, CardPattern pattern, Color ink, float k)
    {
        float pad = 18 * k;
        switch (pattern)
        {
            case CardPattern.Stripes:
                for (float y = r.Y + 40 * k; y < r.Y + r.Height - 20 * k; y += 56 * k)
                    UiShapes.Fill(new Rectangle(r.X + pad, y, r.Width - pad * 2, 14 * k), 7 * k, ink);
                break;
            case CardPattern.Dots:
                for (float y = r.Y + 34 * k; y < r.Y + r.Height - 20 * k; y += 52 * k)
                    for (float x = r.X + 34 * k + ((int)((y - r.Y) / (52 * k)) % 2) * 26 * k; x < r.X + r.Width - 20 * k; x += 52 * k)
                        UiShapes.Circle(new Vector2(x, y), 7 * k, ink);
                break;
            case CardPattern.Waves:
                for (float y = r.Y + 60 * k; y < r.Y + r.Height - 30 * k; y += 70 * k)
                    for (float x = r.X + 40 * k; x < r.X + r.Width - 40 * k; x += 64 * k)
                        UiShapes.Ring(new Vector2(x, y), 22 * k, 5 * k, ink);
                break;
            case CardPattern.Checks:
                for (int row = 0; r.Y + 20 * k + row * 60 * k < r.Y + r.Height - 60 * k; row++)
                    for (int col = row % 2; r.X + 20 * k + col * 60 * k < r.X + r.Width - 60 * k; col += 2)
                        UiShapes.Fill(new Rectangle(r.X + 20 * k + col * 60 * k, r.Y + 20 * k + row * 60 * k, 60 * k, 60 * k), 6 * k, ink with { A = (byte)(ink.A / 2) });
                break;
            case CardPattern.Rays:
            {
                var from = new Vector2(r.X + r.Width * 0.5f, r.Y + r.Height - pad);
                for (int i = 0; i < 9; i++)
                {
                    float a = MathF.PI * (0.12f + 0.76f * i / 8f);
                    float len = Math.Min(r.Height - pad * 2, r.Width * 0.5f - pad) * 0.95f;
                    UiShapes.Line(from + new Vector2(-MathF.Cos(a), -MathF.Sin(a)) * 60 * k, from + new Vector2(-MathF.Cos(a), -MathF.Sin(a)) * len, 10 * k, ink);
                }
                break;
            }
            case CardPattern.Stars:
                for (int i = 0; i < 24; i++)
                {
                    float x = r.X + 30 * k + SoftCanvas.Rand(43, i, 1) * (r.Width - 60 * k);
                    float y = r.Y + 30 * k + SoftCanvas.Rand(43, i, 2) * (r.Height - 60 * k);
                    CardStar(new Vector2(x, y), (6 + 6 * SoftCanvas.Rand(43, i, 3)) * k, true, ink, ink);
                }
                break;
            case CardPattern.Leaves:
                for (int i = 0; i < 22; i++)
                {
                    float x = r.X + 36 * k + SoftCanvas.Rand(71, i, 1) * (r.Width - 72 * k);
                    float y = r.Y + 30 * k + SoftCanvas.Rand(71, i, 2) * (r.Height - 60 * k);
                    UiShapes.Turned(new Vector2(x, y), 18 * k, 7 * k, 7 * k, -30 + 60 * SoftCanvas.Rand(71, i, 3), ink);
                }
                break;
            case CardPattern.Hills:
                for (int i = 0; i < 4; i++)
                {
                    float w = r.Width * (0.34f + 0.06f * i), h = r.Height * (0.28f - 0.04f * i);
                    float x = r.X + pad + (r.Width - pad * 2 - w) * (i / 3f);
                    UiShapes.Fill(new Rectangle(x, r.Y + r.Height - pad - h, w, h), h * 0.9f, ink with { A = (byte)(ink.A * (0.5f + 0.15f * i)) });
                }
                break;
        }
    }

    /// <summary>A frame round the face, by its style.</summary>
    private static void CardFrameArt(Rectangle r, CardFrame? frame, float k)
    {
        if (frame == null) return;
        var colour = Hex(frame.Color);
        var accent = Hex(frame.Accent);
        float radius = 24 * k;
        var clear = colour with { A = 0 };
        void Edge(Rectangle e, Color c, float width) => UiShapes.Shape(e, radius, clear, clear, c, width);
        Rectangle Inset(float by) => new(r.X + by, r.Y + by, r.Width - by * 2, r.Height - by * 2);
        switch (frame.Style)
        {
            case CardFrameStyle.Plain:
                Edge(r, colour, 8 * k);
                Edge(Inset(8 * k), accent, 2 * k);
                break;
            case CardFrameStyle.Double:
                Edge(r, colour, 6 * k);
                Edge(Inset(12 * k), accent, 3 * k);
                Edge(Inset(20 * k), accent, 3 * k);
                break;
            case CardFrameStyle.Scallop:
                Edge(r, colour, 8 * k);
                for (float x = r.X + 34 * k; x < r.X + r.Width - 20 * k; x += 44 * k)
                {
                    UiShapes.Circle(new Vector2(x, r.Y + 14 * k), 10 * k, accent);
                    UiShapes.Circle(new Vector2(x, r.Y + r.Height - 14 * k), 10 * k, accent);
                }
                break;
            case CardFrameStyle.Corners:
                Edge(r, colour, 6 * k);
                foreach (var (cx, cy, sx, sy) in new[] { (r.X, r.Y, 1, 1), (r.X + r.Width, r.Y, -1, 1), (r.X, r.Y + r.Height, 1, -1), (r.X + r.Width, r.Y + r.Height, -1, -1) })
                {
                    float x0 = cx + sx * 14 * k, y0 = cy + sy * 14 * k;
                    UiShapes.Fill(new Rectangle(sx > 0 ? x0 : x0 - 70 * k, sy > 0 ? y0 : y0 - 10 * k, 70 * k, 10 * k), 5 * k, accent);
                    UiShapes.Fill(new Rectangle(sx > 0 ? x0 : x0 - 10 * k, sy > 0 ? y0 : y0 - 70 * k, 10 * k, 70 * k), 5 * k, accent);
                }
                break;
            case CardFrameStyle.Beads:
                Edge(r, colour, 6 * k);
                for (float x = r.X + 30 * k; x < r.X + r.Width - 20 * k; x += 30 * k)
                {
                    UiShapes.Circle(new Vector2(x, r.Y + 12 * k), 6 * k, accent);
                    UiShapes.Circle(new Vector2(x, r.Y + r.Height - 12 * k), 6 * k, accent);
                }
                for (float y = r.Y + 42 * k; y < r.Y + r.Height - 32 * k; y += 30 * k)
                {
                    UiShapes.Circle(new Vector2(r.X + 12 * k, y), 6 * k, accent);
                    UiShapes.Circle(new Vector2(r.X + r.Width - 12 * k, y), 6 * k, accent);
                }
                break;
            case CardFrameStyle.Ribbon:
                Edge(r, colour, 8 * k);
                UiShapes.Fill(new Rectangle(r.X + r.Width - 230 * k, r.Y + 8 * k, 46 * k, r.Height - 16 * k), 4 * k, accent with { A = 210 });
                UiShapes.Fill(new Rectangle(r.X + 8 * k, r.Y + 64 * k, r.Width - 16 * k, 14 * k), 4 * k, accent with { A = 150 });
                break;
            case CardFrameStyle.Laurel:
                Edge(r, colour, 8 * k);
                for (int i = 0; i < 7; i++)
                    foreach (int side in new[] { -1, 1 })
                    {
                        float x = side < 0 ? r.X + (34 + i * 26) * k : r.X + r.Width - (34 + i * 26) * k;
                        UiShapes.Turned(new Vector2(x, r.Y + r.Height - (28 + i * 10) * k), 16 * k, 6 * k, 6 * k, side * (40 - i * 6), accent);
                    }
                break;
            case CardFrameStyle.Gilt:
                Edge(r, accent, 12 * k);
                Edge(Inset(12 * k), colour, 4 * k);
                Edge(Inset(18 * k), Darker(accent, 0.2f), 2 * k);
                break;
        }
    }

    /// <summary>
    /// The designed front's own part (plan 43 · LF1): the face across the card between the band and the badges, and the
    /// records in a column at its right.
    /// </summary>
    private static void CardFrontDesigned(Rectangle card, TrainerCardInfo info, TrainerCardFace face)
    {
        var area = new Rectangle(card.X + 26, card.Y + 150, CardFaceWidth, CardFaceHeight);
        CardFaceArt(area, face, 1f);

        // The records on a pale plate at the face's right
        bool dex = info.Colour != TrainerCardRules.CardColour.NoPokedex;
        var plate = new Rectangle(area.X + area.Width - 430, area.Y + 26, 400, area.Height - 52);
        UiShapes.Fill(plate, 24, new Color(255, 255, 255, 214));
        float x = plate.X + 28, y = plate.Y + 22;
        Label(Text.Of("card.money"), x, y);
        Money(x, y + 24 + 34 * 0.56f, info.Money, 34, Ink);
        Field(Text.Of("card.timePlayed"), TitleScreen.FormatPlayTime(info.PlayTimeSeconds), x + 200, y, 34, 48);
        Field(Text.Of("card.caught"), dex ? info.Caught.ToString() : "—", x, y + 106, 34, 48);
        Field(Text.Of("card.seen"), dex ? info.Seen.ToString() : "—", x + 200, y + 106, 34, 48);
        Field(Text.Of("card.score"), info.Score.ToString("N0", CultureInfo.InvariantCulture), x, y + 212, 34, 48);
    }

    /// <summary>The signature as ink on its card, each written cell a square of <paramref name="r"/>'s cells.</summary>
    public static void CardSignatureArt(Rectangle r, SignaturePad pad, Color ink)
    {
        float cw = r.Width / SignaturePad.Width, ch = r.Height / SignaturePad.Height;
        for (int y = 0; y < SignaturePad.Height; y++)
        {
            // A run of cells in a row is one rectangle
            int x = 0;
            while (x < SignaturePad.Width)
            {
                if (!pad[x, y]) { x++; continue; }
                int from = x;
                while (x < SignaturePad.Width && pad[x, y]) x++;
                Raylib.DrawRectangleRec(new Rectangle(r.X + from * cw, r.Y + y * ch, (x - from) * cw + 0.5f, ch + 0.5f), ink);
            }
        }
    }

    /// <summary>The back's signature (plan 43 · LF1), in the corner its stars' note had: a line to sign on and the ink over it.</summary>
    private static void CardBackSignature(float right, float footY, SignaturePad pad)
    {
        var box = new Rectangle(right - 480, footY + 46, 480, 144);
        UiShapes.Fill(new Rectangle(box.X, box.Y + box.Height - 10, box.Width, 3), 1.5f, Rule);
        CardSignatureArt(box, pad, new Color(30, 40, 90, 255));
    }

    // ------------------------------------------------------------------ the designer

    private const float DesignRowHeight = 88, DesignPanelWidth = 620;

    /// <summary>The designer's rows where they stand (the pointer finds them here).</summary>
    public static List<Rectangle> CardDesignRows()
    {
        var rows = new List<Rectangle>();
        int n = Enum.GetValues<CardDesignRow>().Length;
        for (int i = 0; i < n; i++)
            rows.Add(new Rectangle(Margin + 16, ContentTop + 24 + i * DesignRowHeight, DesignPanelWidth - 32, DesignRowHeight - 10));
        return rows;
    }

    /// <summary>Where the preview of the face stands.</summary>
    private static Rectangle CardPreview() => new(Margin + DesignPanelWidth + Gutter, ContentTop, 1920 - Margin * 2 - DesignPanelWidth - Gutter, (1920 - Margin * 2 - DesignPanelWidth - Gutter) * CardFaceHeight / CardFaceWidth);

    private static float PreviewScale => CardPreview().Width / CardFaceWidth;

    /// <summary>The six's list's rows where they stand.</summary>
    public static List<Rectangle> CardPickRows(int count)
    {
        var p = CardPreview();
        var rows = new List<Rectangle>();
        for (int i = 0; i < count; i++) rows.Add(new Rectangle(p.X, p.Y + p.Height + 74 + i * 66, p.Width, 60));
        return rows;
    }

    /// <summary>The signature's pad where it stands, a cell to six units.</summary>
    public static Rectangle CardPadRect()
    {
        var p = CardPreview();
        return new Rectangle(p.X + (p.Width - SignaturePad.Width * 6) / 2f, p.Y + p.Height + 120, SignaturePad.Width * 6, SignaturePad.Height * 6);
    }

    /// <summary>A design's row's value as the designer writes it.</summary>
    public static string CardRowValue(CardDesignScreen screen, CardDesignRow row)
    {
        var d = screen.Design;
        return row switch
        {
            CardDesignRow.Background => CardLooks.BackdropOf(d.Background) is { } n
                ? Text.Of("cardDesign.background.backdrop", ("name", Data.SuperContestDatabase.BackdropName(n)))
                : Text.Of($"cardDesign.background.{d.Background}"),
            CardDesignRow.Frame => Text.Of($"cardDesign.frame.{d.Frame}"),
            CardDesignRow.Pose => CardPoseWord(d),
            CardDesignRow.Face => Text.Of($"cardDesign.face.{d.Mood}"),
            CardDesignRow.Title => screen.TitleIndex is var t and >= 0 ? screen.Titles[t].Words : Text.Of("cardDesign.title.none"),
            CardDesignRow.Shown => d.Shown == null ? Text.Of("cardDesign.shown.team") : Text.Of("cardDesign.shown.chosen", ("count", d.Shown.Count)),
            CardDesignRow.Greeting => screen.Greeting.Length > 0 ? screen.Greeting : Text.Of("cardDesign.greeting.none"),
            CardDesignRow.Signature => screen.Pad.IsEmpty ? Text.Of("cardDesign.signature.none") : Text.Of("cardDesign.signature.signed"),
            _ => Text.Of("cardDesign.print.action")
        };
    }

    private static string CardPoseWord(CardDesign d)
    {
        string pose = d.Pose == CardPoseKind.Gesture && d.Gesture is { } g ? g.ToString() : d.Pose.ToString();
        return Text.Of($"cardDesign.pose.{pose}");
    }

    /// <summary>The designer: its rows at the left, the face as it stands at the right, and under it the six's list, the pad or the signature.</summary>
    public static void DrawCardDesigner(int sw, int sh, CardDesignScreen screen, TrainerCardFace face, float appear)
    {
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("cardDesign.heading"));
        if (screen.Mode == CardDesignMode.Pad)
            Hints(sw - Margin, 44, Hint("shared.keys.z", "cardDesign.hints.pen"), Hint("shared.keys.c", "cardDesign.hints.eraser"),
                Hint("cardDesign.keyQ", "cardDesign.hints.clear"), Hint("shared.keys.esc", "shared.hints.back"));
        else
            Hints(sw - Margin, 44, Hint("shared.keys.leftRight", "shared.hints.change"), Hint("shared.keys.z", "shared.hints.choose"), Hint("shared.keys.esc", "shared.hints.back"));

        float rise = (1f - UiMotion.EaseOut(appear)) * 50f;
        var panel = new Rectangle(Margin, ContentTop + rise, DesignPanelWidth, Enum.GetValues<CardDesignRow>().Length * DesignRowHeight + 40);
        Panel(panel, 34);
        var rows = CardDesignRows();
        foreach (var row in Enum.GetValues<CardDesignRow>())
        {
            var rr = rows[(int)row];
            rr.Y += rise;
            bool selected = screen.Mode == CardDesignMode.Rows && screen.Row == row;
            var ink = ListRow(rr, selected, Text.Of($"cardDesign.row.{row}"), nameX: 28, size: 26);
            string value = CardRowValue(screen, row);
            float size = 26;
            while (size > 16 && UiFonts.Measure(value, size, UiWeight.Black) > rr.Width * 0.55f) size -= 2;
            UiFonts.DrawCentered(value, rr.X + rr.Width - 28 - UiFonts.Measure(value, size, UiWeight.Black), rr.Y + rr.Height / 2f, size, ink, UiWeight.Black);
        }

        // The face as the card will have it
        var preview = CardPreview();
        preview.Y += rise;
        CardFaceArt(preview, face, PreviewScale, withTitle: true);

        switch (screen.Mode)
        {
            case CardDesignMode.PickShown:
            {
                UiFonts.Draw(Text.Of("cardDesign.shown.heading"), preview.X, preview.Y + preview.Height + 28, 28, Color.White, UiWeight.Black);
                UiFonts.Draw(Text.Of("cardDesign.shown.note"), preview.X + 380, preview.Y + preview.Height + 32, 22, new Color(220, 228, 246, 255), UiWeight.Bold);
                var list = CardPickRows(Math.Min(CardDesignScreen.VisiblePicks, screen.Candidates.Count - screen.FirstPick));
                for (int i = 0; i < list.Count; i++)
                {
                    int at = screen.FirstPick + i;
                    var c = screen.Candidates[at];
                    var rr = list[i];
                    rr.Y += rise;
                    bool shown = screen.IsShown(c.Id);
                    var ink = ListRow(rr, at == screen.PickIndex, c.Name, !c.Pokemon.IsEgg, nameX: 96, size: 28);
                    var icon = PixelArtGenerator.IconOf(c.Pokemon);
                    Raylib.DrawTexturePro(icon, new Rectangle(0, 0, icon.Width, icon.Height), new Rectangle(rr.X + 20, rr.Y + rr.Height / 2f - icon.Height, icon.Width * 2, icon.Height * 2), Vector2.Zero, 0f, Color.White);
                    string level = Text.Of("battleRoom.level", ("level", c.Level));
                    UiFonts.DrawCentered(level, rr.X + rr.Width - 150, rr.Y + rr.Height / 2f, 24, ink, UiWeight.Black);
                    if (shown) UiShapes.Circle(new Vector2(rr.X + rr.Width - 40, rr.Y + rr.Height / 2f), 14, Green);
                    else UiShapes.Ring(new Vector2(rr.X + rr.Width - 40, rr.Y + rr.Height / 2f), 14, 3, Rule);
                }
                break;
            }
            case CardDesignMode.Pad:
            {
                var pad = CardPadRect();
                pad.Y += rise;
                UiFonts.Draw(Text.Of("cardDesign.signature.heading"), pad.X, pad.Y - 60, 30, Color.White, UiWeight.Black);
                UiShapes.Shape(new Rectangle(pad.X - 16, pad.Y - 16, pad.Width + 32, pad.Height + 32), 22, new Color(252, 250, 244, 255), new Color(240, 236, 226, 255), Rule, 3);
                UiShapes.Fill(new Rectangle(pad.X, pad.Y + pad.Height - 40, pad.Width, 3), 1.5f, Rule);
                CardSignatureArt(pad, screen.Pad, new Color(30, 40, 90, 255));
                var pen = new Vector2(pad.X + (screen.PenX + 0.5f) * 6, pad.Y + (screen.PenY + 0.5f) * 6);
                UiShapes.Ring(pen, 12, 3, screen.Eraser ? Red : Blue);
                if (screen.PenDown) UiShapes.Circle(pen, 5, screen.Eraser ? Red : Blue);
                string tool = Text.Of(screen.Eraser ? "cardDesign.signature.eraser" : "cardDesign.signature.pen") + " · "
                    + Text.Of(screen.PenDown ? "cardDesign.signature.down" : "cardDesign.signature.up");
                UiFonts.Draw(tool, pad.X, pad.Y + pad.Height + 34, 24, new Color(220, 228, 246, 255), UiWeight.Black);
                break;
            }
            default:
                if (!screen.Pad.IsEmpty)
                {
                    var sig = new Rectangle(preview.X + preview.Width - 480, preview.Y + preview.Height + 40, 480, 144);
                    UiShapes.Fill(new Rectangle(sig.X - 20, sig.Y - 10, sig.Width + 40, sig.Height + 20), 18, new Color(252, 250, 244, 235));
                    CardSignatureArt(sig, screen.Pad, new Color(30, 40, 90, 255));
                }
                break;
        }
    }
}
