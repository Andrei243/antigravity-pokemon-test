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

/// <summary>The photo studio (plan 43 · LF5; style guide, "The photo studio").</summary>
internal static partial class ModernUi
{
    public const int StudioFigureWidth = 400, StudioFigureHeight = 480;

    private const float StudioPanelWidth = 640, StudioRowHeight = 68;

    /// <summary>The stage as it is drawn beside the panel: sixteen to nine.</summary>
    public static Rectangle StudioStage => new(Margin, ContentTop + 8, 1120, 630);

    /// <summary>The rows' panel at the right (the drawing's and the pointer's).</summary>
    public static Rectangle StudioPanel(int sw) =>
        new(sw - Margin - StudioPanelWidth, ContentTop + 8, StudioPanelWidth, Enum.GetValues<StudioRow>().Length * StudioRowHeight + 36);

    /// <summary>A row's rectangle in the panel.</summary>
    public static Rectangle StudioRowRect(int sw, StudioRow row)
    {
        var panel = StudioPanel(sw);
        return new Rectangle(panel.X + 18, panel.Y + 18 + (int)row * StudioRowHeight, panel.Width - 36, StudioRowHeight - 6);
    }

    /// <summary>The middle of the <paramref name="i"/>th Seal shown under the stage while one is chosen, and its radius.</summary>
    public static Vector2 StudioSealSpot(int i)
    {
        var stage = StudioStage;
        return new Vector2(stage.X + 46 + i * 104, stage.Y + stage.Height + 56 + 90);
    }

    public const float StudioSealRadius = 44;

    /// <summary>A row's value in words.</summary>
    public static string StudioRowValue(StudioScreen screen, StudioRow row)
    {
        var s = screen.Shot;
        return row switch
        {
            StudioRow.Backdrop => CardLooks.BackdropOf(s.Backdrop) is { } n
                ? Text.Of("cardDesign.background.backdrop", ("name", Data.SuperContestDatabase.BackdropName(n)))
                : Text.Of($"photoStudio.backdrop.{s.Backdrop}"),
            StudioRow.Pose => StudioPoseWord(s),
            StudioRow.Face => Text.Of($"cardDesign.face.{s.Mood}"),
            StudioRow.Pokemon => screen.Chosen is { } c ? c.Name : Text.Of("photoStudio.pokemon.none"),
            StudioRow.Place => Text.Of($"photoStudio.place.{s.Place}"),
            StudioRow.Stickers => Text.Of("photoStudio.stickers.count", ("count", s.Stickers.Count), ("most", StudioShot.MostStickers)),
            StudioRow.Frame => Text.Of($"photoStudio.frame.{s.Frame}"),
            _ => Text.Of("photoStudio.take.action")
        };
    }

    private static string StudioPoseWord(StudioShot s)
    {
        string pose = s.Pose == CardPoseKind.Gesture && s.Gesture is { } g ? g.ToString() : s.Pose.ToString();
        return Text.Of($"cardDesign.pose.{pose}");
    }

    /// <summary>
    /// The stage in <paramref name="r"/>, at <paramref name="k"/> of the full picture's size: the backdrop and its
    /// pattern, the floor's shadow, the player (<paramref name="figure"/>, null while the look is made), the Pokémon beside
    /// them, the stickers and the frame over all. The same drawing is the picture that is taken.
    /// </summary>
    public static void StudioStageArt(Rectangle r, StudioShot shot, Texture2D? figure, Pokemon? pokemon, float k)
    {
        float radius = 24 * k;
        if (CardLooks.BackdropOf(shot.Backdrop) is { } fashion) BackdropArt(r, fashion);
        else if (StudioLooks.Backdrop(shot.Backdrop) is { } bg)
        {
            UiShapes.Shape(r, radius, Hex(bg.Top), Hex(bg.Bottom));
            CardPatternArt(r, bg.Pattern, Hex(bg.Ink, 120), k);
        }
        else UiShapes.Shape(r, radius, Disc, Disc);

        // The floor, a soft ellipse under the feet
        float floorY = r.Y + r.Height - 70 * k;
        float playerX = r.X + r.Width * (shot.Place == StudioPlace.Right ? 0.36f : 0.64f);
        float pokemonX = r.X + r.Width * (shot.Place == StudioPlace.Right ? 0.68f : 0.32f);
        UiShapes.Fill(new Rectangle(r.X + r.Width * 0.12f, floorY - 14 * k, r.Width * 0.76f, 44 * k), 22 * k, new Color(0, 0, 0, 28));

        if (figure is { } f)
        {
            float h = r.Height * 0.82f, w = h * f.Width / f.Height;
            Raylib.DrawTexturePro(f, new Rectangle(0, 0, f.Width, -f.Height),
                new Rectangle(MathF.Round(playerX - w / 2f), MathF.Round(floorY + 18 * k - h), MathF.Round(w), MathF.Round(h)), Vector2.Zero, 0f, Color.White);
        }
        if (pokemon != null)
        {
            var sprite = PixelArtGenerator.SpriteOf(pokemon);
            float h = r.Height * 0.46f, w = h * sprite.Width / sprite.Height;
            Raylib.DrawTexturePro(sprite, new Rectangle(0, 0, sprite.Width, sprite.Height),
                new Rectangle(MathF.Round(pokemonX - w / 2f), MathF.Round(floorY + 10 * k - h), MathF.Round(w), MathF.Round(h)), Vector2.Zero, 0f, Color.White);
        }
        foreach (var sticker in shot.Stickers)
            SealSticker(new Vector2(r.X + sticker.X * r.Width, r.Y + sticker.Y * r.Height), 34 * k, sticker.Seal);
        CardFrameArt(r, StudioLooks.Frame(shot.Frame), k);
    }

    /// <summary>
    /// The studio: the stage at the upper left with the carried Seal over it, the rows at the right and under the stage the
    /// case's Seals while one is chosen. While the picture is <see cref="StudioScreen.Taking"/> the stage fills the screen alone.
    /// </summary>
    public static void DrawStudio(int sw, int sh, StudioScreen screen, Texture2D? figure, Pokemon? pokemon)
    {
        if (screen.Taking)
        {
            StudioStageArt(new Rectangle(0, 0, sw, sh), screen.Shot, figure, pokemon, 1.75f);
            return;
        }
        Backdrop(sw, sh);
        ScreenTitle(Text.Of("photoStudio.heading"));
        if (screen.Mode == StudioMode.PlaceSeal)
            Hints(sw - Margin, 44, Hint("shared.keys.arrows", "shared.hints.move"), Hint("shared.keys.z", "photoStudio.hints.put"), Hint("shared.keys.esc", "shared.hints.back"));
        else
            Hints(sw - Margin, 44, Hint("shared.keys.leftRight", "shared.hints.change"), Hint("shared.keys.z", "shared.hints.choose"), Hint("shared.keys.esc", "shared.hints.back"));

        var stage = StudioStage;
        StudioStageArt(stage, screen.Shot, figure, pokemon, stage.Width / 1920f * 1.75f);
        if (screen.Mode == StudioMode.PlaceSeal && screen.Seals.Count > screen.SealIndex)
        {
            var at = new Vector2(stage.X + screen.CursorX * stage.Width, stage.Y + screen.CursorY * stage.Height);
            SealSticker(at, 34 * stage.Width / 1920f * 1.75f, screen.Seals[screen.SealIndex], 0.8f);
            UiShapes.Ring(at, 30, 3, Selection);
        }

        var panel = StudioPanel(sw);
        Panel(panel, 34);
        foreach (var row in Enum.GetValues<StudioRow>())
        {
            var rr = StudioRowRect(sw, row);
            bool selected = screen.Mode == StudioMode.Rows && screen.Row == row;
            var ink = ListRow(rr, selected, Text.Of($"photoStudio.row.{row}"), nameX: 26, size: 26);
            string value = StudioRowValue(screen, row);
            float size = 26;
            while (size > 16 && UiFonts.Measure(value, size, UiWeight.Black) > rr.Width * 0.55f) size -= 2;
            UiFonts.DrawCentered(value, rr.X + rr.Width - 26 - UiFonts.Measure(value, size, UiWeight.Black), rr.Y + rr.Height / 2f, size, ink, UiWeight.Black);
        }
        if (screen.Refusal is { } why)
            UiFonts.Draw(Text.Of($"photoStudio.refusal.{why}"), stage.X, stage.Y + stage.Height + 20, 26, new Color(255, 214, 214, 255), UiWeight.Black);

        // The case's Seals while one is chosen
        if (screen.Mode != StudioMode.Rows)
        {
            float top = stage.Y + stage.Height + 56;
            UiFonts.Draw(Text.Of("photoStudio.seals.heading"), stage.X, top, 28, Color.White, UiWeight.Black);
            int shown = Math.Min(StudioScreen.VisibleSeals, screen.Seals.Count - screen.FirstSeal);
            for (int i = 0; i < shown; i++)
            {
                int at = screen.FirstSeal + i;
                var c = StudioSealSpot(i);
                UiShapes.Circle(c, StudioSealRadius,new Color(255, 255, 255, at == screen.SealIndex ? 235 : 130));
                if (at == screen.SealIndex) UiShapes.Ring(c, 46, 4, Selection);
                SealSticker(c, 30, screen.Seals[at]);
            }
        }
    }
}
