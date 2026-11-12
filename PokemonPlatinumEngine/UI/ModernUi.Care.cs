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
/// The care close-up on screen (plan 35 · BN2; style guide, "Menu screens (G10)", "The care close-up"): the camp's
/// clearing veiled behind, the Pokémon front on as its model (its still sprite until the model is built), the marks of
/// soil on its body, the hand, the tray of tools and the panel with its fullness and mood.
/// </summary>
internal static partial class ModernUi
{
    /// <summary>The picture's square on the screen, in layout units.</summary>
    public static Rectangle CarePicture => new(510, 70, 900, 900);

    /// <summary>The tray's chips along the foot, in the tools' order.</summary>
    public static Rectangle CareToolChip(int index)
    {
        const float width = 200, height = 64, gap = 16;
        int count = Enum.GetValues<CareTool>().Length;
        float left = (GameEngine.VirtualWidth - (count * width + (count - 1) * gap)) / 2f;
        return new Rectangle(left + index * (width + gap), 984, width, height);
    }

    private static Color SoilColour(SoilMark mark) => mark switch
    {
        SoilMark.Dust => new Color(196, 170, 120, 255),
        SoilMark.Mud => new Color(112, 82, 56, 255),
        SoilMark.Leaves => new Color(98, 168, 82, 255),
        SoilMark.Wet => new Color(120, 176, 230, 255),
        _ => new Color(236, 240, 248, 255)
    };

    private static Color MoodColour(CareMood mood) => mood switch
    {
        CareMood.Glum => Red,
        CareMood.Low => Darker(Gold, 0.25f),
        CareMood.Cheerful => Green,
        CareMood.Elated => Gold,
        _ => Muted
    };

    /// <summary>A point of the picture (0 to 1 across and down) on the screen.</summary>
    public static Vector2 CareAt(Vector2 share)
    {
        var r = CarePicture;
        return new Vector2(MathF.Round(r.X + share.X * r.Width), MathF.Round(r.Y + share.Y * r.Height));
    }

    public static void DrawCare(int sw, int sh, CareScreen screen)
    {
        Backdrop(sw, sh);
        CampGround(CampClearing);
        UiShapes.Fill(new Rectangle(0, 0, sw, sh), 0, new Color(Ink.R, Ink.G, Ink.B, (byte)102));
        ScreenTitle(Text.Of("care.title"));
        Hints(sw - Margin, 44, Hint("shared.keys.arrows", "shared.hints.move"), Hint("shared.keys.z", "care.hint.hold"),
            Hint("care.keys.tool", "care.hint.tool"), Hint("shared.keys.esc", "shared.hints.back"));
        if (screen.Pokemon is not { } p) return;

        // ---- The Pokémon, front on
        var pic = CarePicture;
        if (screen.PictureShows == p.LookName && screen.Picture is { } picture)
            Raylib.DrawTexturePro(picture, new Rectangle(0, 0, picture.Width, -picture.Height), pic, Vector2.Zero, 0f, Color.White);
        else if (PokemonSprites.GetBaked(p.LookName, SpriteView.Front) is { } still)
        {
            float size = still.Width * 4f;
            var c = CareAt(new Vector2(0.5f, 0.55f));
            Raylib.DrawTexturePro(still, new Rectangle(0, 0, still.Width, still.Height),
                new Rectangle(MathF.Round(c.X - size / 2f), MathF.Round(c.Y - size / 2f), size, size), Vector2.Zero, 0f, Color.White);
        }

        var layout = screen.Layout;
        var page = screen.Page;
        // ---- The liked spot glows while the hand is near it
        if (screen.Tool == CareTool.Hand && Vector2.Distance(screen.Hand, layout.Liked) <= CareStage.Reach * 1.6f)
            UiShapes.Ring(CareAt(layout.Liked), CareStage.Reach * pic.Width, 5, new Color(Gold.R, Gold.G, Gold.B, (byte)77));
        // ---- The marks of soil, worn away dot by dot
        if (page != null)
            for (int i = 0; i < page.Marks.Count; i++)
            {
                var at = CareAt(layout.Mark(i));
                int left = Math.Max(1, (int)MathF.Ceiling(5f * (1f - screen.WearOf(i) / CareScreen.MarkWear)));
                for (int d = 0; d < left; d++)
                {
                    float angle = d * 1.3f + i;
                    UiShapes.Circle(at + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (d == 0 ? 0f : 26f), 12, SoilColour(page.Marks[i]));
                }
            }
        // ---- A status beside the head
        if (p.Status is not (StatusCondition.None or StatusCondition.Faint))
        {
            var h = CareAt(screen.Layout.Head);
            StatusPill(h.X + 90, h.Y - 30, p.Status, 30);
        }

        // ---- The hand: a glove of the kit's shapes, pressed while held
        var hand = CareAt(screen.Hand);
        float s = screen.Holding ? 0.9f : 1f;
        for (int f = -1; f <= 1; f++) UiShapes.Circle(hand + new Vector2(f * 18f * s, -30f * s), 12 * s, Color.White);
        UiShapes.Circle(hand, 28 * s, Color.White);
        UiShapes.Ring(hand, 28 * s, 3, Ink);

        // ---- The panel: the name, fullness and mood
        var box = new Rectangle(64, 132, 420, 230);
        Panel(box, 30);
        NameWithGender(p, box.X + 34, box.Y + 46, 34);
        Label(Text.Of("care.fullness"), box.X + 34, box.Y + 86);
        int full = page != null ? CareRules.FullnessOn(page, screen.Day) : 0;
        for (int i = 0; i < CareRules.Fullness; i++) UiShapes.Circle(new Vector2(box.X + 50 + i * 40, box.Y + 130), 14, i < full ? Gold : Rule);
        Label(Text.Of("care.mood"), box.X + 34, box.Y + 160);
        var mood = page != null ? CareRules.MoodOf(page.Mood) : CareMood.Calm;
        UiFonts.Draw(Text.Of($"care.moodWord.{mood}"), box.X + 34, box.Y + 184, 32, MoodColour(mood), UiWeight.Black);

        // ---- The tray of tools
        foreach (var tool in Enum.GetValues<CareTool>())
        {
            var chip = CareToolChip((int)tool);
            bool chosen = tool == screen.Tool;
            if (chosen) UiShapes.Shape(chip, chip.Height / 2f, Lighter(Selection, 0.14f), Darker(Selection, 0.06f), Darker(Selection, 0.3f), 3);
            else Panel(chip, chip.Height / 2f);
            string label = Text.Of($"care.tool.{tool}");
            float w = UiFonts.Measure(label, 26, UiWeight.Black);
            UiFonts.DrawCentered(label, MathF.Round(chip.X + (chip.Width - w) / 2f), chip.Y + chip.Height / 2f, 26, chosen ? Color.White : Ink, UiWeight.Black);
        }

        // ---- What the last thing done came to
        DrawWrapped(screen.Note, 1440, 160, 420, 30, Color.White, 42, UiWeight.ExtraBold);
    }
}
